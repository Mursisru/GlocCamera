using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace GlocCamera_Engine
{
    /// <summary>
    /// Cockpit-only URP post on the game's main <see cref="Volume"/>:
    /// cinematic bloom (halation), color punch, vignette, chromatic aberration.
    /// Snapshots baselines on enter, lerps day/night targets, restores on leave.
    /// </summary>
    internal static class GlocCameraAtmosphereDriver
    {
        private static Bloom _bloom;
        private static ColorAdjustments _colorAdjustments;
        private static Vignette _vignette;
        private static ChromaticAberration _chromatic;
        private static bool _armed;
        private static bool _loggedMissingComponents;

        private static float _bloomIntensityBase;
        private static float _bloomThresholdBase;
        private static float _bloomScatterBase;
        private static Color _bloomTintBase;

        private static float _postExposureBase;
        private static float _saturationBase;
        private static float _contrastBase;
        private static Color _colorFilterBase;

        private static float _vignetteIntensityBase;
        private static float _chromaticIntensityBase;

        private static float _bloomIntensityDisplay;
        private static float _bloomThresholdDisplay;
        private static float _bloomScatterDisplay;
        private static Color _bloomTintDisplay;

        private static float _postExposureDisplay;
        private static float _saturationDisplay;
        private static float _contrastDisplay;
        private static Color _colorFilterDisplay;

        private static float _vignetteIntensityDisplay;
        private static float _chromaticIntensityDisplay;

        internal static void ForceRestore()
        {
            ApplySnapshotToVolume();
            ClearArmedState();
        }

        internal static void Tick(CameraStateManager cam)
        {
            if (!GlocCameraPlugin.Enabled.Value || !GlocCameraPlugin.AtmosphereEnabled.Value)
            {
                if (_armed)
                    ForceRestore();
                return;
            }

            if (cam == null || cam.currentState != cam.cockpitState)
            {
                if (_armed)
                    ForceRestore();
                return;
            }

            if (!GlocFrameContext.HasLocalAircraft)
            {
                if (_armed)
                    ForceRestore();
                return;
            }

            Aircraft local = GlocFrameContext.LocalAircraft;
            if (!GlocFrameContext.IsLocalPilotReady(cam))
            {
                if (_armed)
                    ForceRestore();
                return;
            }

            var vol = cam.GetPostProcessVolume();
            if (vol == null)
                return;

            if (GlocNightVisionUtil.IsActive())
            {
                if (_armed)
                    ForceRestore();
                return;
            }

            VolumeProfile profile = vol.profile != null ? vol.profile : vol.sharedProfile;
            if (profile == null)
                return;

            if (!_armed)
                TryArm(profile);

            if (!_armed)
                return;

            float night01 = GlocNightFactor.ComputeNight01();
            float dt = Time.unscaledDeltaTime;
            float speed = Mathf.Max(0.01f, GlocCameraPlugin.AtmosphereLerpSpeed.Value);
            float t = 1f - Mathf.Exp(-speed * dt);
            bool deferColorAndVignette = GlocCameraPlugin.AtmosphereDeferDuringVanillaGloc.Value
                && IsVanillaGlocVisuallyActive(cam);

            if (_bloom != null)
            {
                float intensityTarget = _bloomIntensityBase
                    + Mathf.Lerp(
                        GlocCameraPlugin.AtmosphereBloomAddDay.Value,
                        GlocCameraPlugin.AtmosphereBloomAddNight.Value,
                        night01);
                _bloomIntensityDisplay = Mathf.Lerp(_bloomIntensityDisplay, intensityTarget, t);
                _bloom.intensity.Override(_bloomIntensityDisplay);

                float thresholdTarget = _bloomThresholdBase
                    + Mathf.Lerp(
                        GlocCameraPlugin.AtmosphereBloomThresholdAddDay.Value,
                        GlocCameraPlugin.AtmosphereBloomThresholdAddNight.Value,
                        night01);
                _bloomThresholdDisplay = Mathf.Lerp(_bloomThresholdDisplay, thresholdTarget, t);
                _bloom.threshold.Override(_bloomThresholdDisplay);

                float scatterTarget = _bloomScatterBase
                    + Mathf.Lerp(
                        GlocCameraPlugin.AtmosphereBloomScatterAddDay.Value,
                        GlocCameraPlugin.AtmosphereBloomScatterAddNight.Value,
                        night01);
                _bloomScatterDisplay = Mathf.Lerp(_bloomScatterDisplay, scatterTarget, t);
                _bloom.scatter.Override(Mathf.Clamp01(_bloomScatterDisplay));

                float tintBlend = Mathf.Lerp(
                    GlocCameraPlugin.AtmosphereBloomTintBlendDay.Value,
                    GlocCameraPlugin.AtmosphereBloomTintBlendNight.Value,
                    night01);
                Color tintTarget = Color.Lerp(_bloomTintBase, ReadBloomTintColor(), tintBlend);
                _bloomTintDisplay = Color.Lerp(_bloomTintDisplay, tintTarget, t);
                _bloom.tint.Override(_bloomTintDisplay);
            }

            if (_colorAdjustments != null)
            {
                float exposureTarget = _postExposureBase
                    + Mathf.Lerp(
                        GlocCameraPlugin.AtmospherePostExposureAddDay.Value,
                        GlocCameraPlugin.AtmospherePostExposureAddNight.Value,
                        night01);
                _postExposureDisplay = Mathf.Lerp(_postExposureDisplay, exposureTarget, t);
                _colorAdjustments.postExposure.Override(_postExposureDisplay);

                if (!deferColorAndVignette)
                {
                    float satTarget = _saturationBase
                        + Mathf.Lerp(
                            GlocCameraPlugin.AtmosphereSaturationAddDay.Value,
                            GlocCameraPlugin.AtmosphereSaturationAddNight.Value,
                            night01);
                    _saturationDisplay = Mathf.Lerp(_saturationDisplay, satTarget, t);
                    _colorAdjustments.saturation.Override(_saturationDisplay);

                    float contrastTarget = _contrastBase
                        + Mathf.Lerp(
                            GlocCameraPlugin.AtmosphereContrastAddDay.Value,
                            GlocCameraPlugin.AtmosphereContrastAddNight.Value,
                            night01);
                    _contrastDisplay = Mathf.Lerp(_contrastDisplay, contrastTarget, t);
                    _colorAdjustments.contrast.Override(_contrastDisplay);

                    float filterBlend = Mathf.Lerp(
                        GlocCameraPlugin.AtmosphereColorFilterBlendDay.Value,
                        GlocCameraPlugin.AtmosphereColorFilterBlendNight.Value,
                        night01);
                    Color filterTarget = Color.Lerp(_colorFilterBase, ReadColorFilter(), filterBlend);
                    _colorFilterDisplay = Color.Lerp(_colorFilterDisplay, filterTarget, t);
                    _colorAdjustments.colorFilter.Override(_colorFilterDisplay);
                }
            }

            if (_vignette != null && !deferColorAndVignette)
            {
                float vignetteTarget = _vignetteIntensityBase
                    + Mathf.Lerp(
                        GlocCameraPlugin.AtmosphereVignetteIntensityAddDay.Value,
                        GlocCameraPlugin.AtmosphereVignetteIntensityAddNight.Value,
                        night01);
                _vignetteIntensityDisplay = Mathf.Lerp(_vignetteIntensityDisplay, vignetteTarget, t);
                _vignette.intensity.Override(Mathf.Clamp01(_vignetteIntensityDisplay));
            }

            if (_chromatic != null)
            {
                float chromaTarget = _chromaticIntensityBase
                    + Mathf.Lerp(
                        GlocCameraPlugin.AtmosphereChromaticAddDay.Value,
                        GlocCameraPlugin.AtmosphereChromaticAddNight.Value,
                        night01);
                _chromaticIntensityDisplay = Mathf.Lerp(_chromaticIntensityDisplay, chromaTarget, t);
                _chromatic.intensity.Override(Mathf.Clamp01(_chromaticIntensityDisplay));
            }
        }

        private static void TryArm(VolumeProfile profile)
        {
            _bloom = null;
            _colorAdjustments = null;
            _vignette = null;
            _chromatic = null;

            profile.TryGet(out _bloom);
            profile.TryGet(out _colorAdjustments);
            profile.TryGet(out _vignette);
            profile.TryGet(out _chromatic);

            if (_bloom == null && _colorAdjustments == null && _vignette == null && _chromatic == null)
            {
                if (!_loggedMissingComponents)
                {
                    _loggedMissingComponents = true;
                    GlocCameraPlugin.ModLog?.LogDebug(
                        "G-LOC Camera atmosphere: Volume profile has no Bloom/ColorAdjustments/Vignette/ChromaticAberration.");
                }
                return;
            }

            if (_bloom != null)
            {
                _bloomIntensityBase = _bloom.intensity.value;
                _bloomThresholdBase = _bloom.threshold.value;
                _bloomScatterBase = _bloom.scatter.value;
                _bloomTintBase = _bloom.tint.value;
                _bloomIntensityDisplay = _bloomIntensityBase;
                _bloomThresholdDisplay = _bloomThresholdBase;
                _bloomScatterDisplay = _bloomScatterBase;
                _bloomTintDisplay = _bloomTintBase;
            }

            if (_colorAdjustments != null)
            {
                _postExposureBase = _colorAdjustments.postExposure.value;
                _saturationBase = _colorAdjustments.saturation.value;
                _contrastBase = _colorAdjustments.contrast.value;
                _colorFilterBase = _colorAdjustments.colorFilter.value;
                _postExposureDisplay = _postExposureBase;
                _saturationDisplay = _saturationBase;
                _contrastDisplay = _contrastBase;
                _colorFilterDisplay = _colorFilterBase;
            }

            if (_vignette != null)
            {
                _vignetteIntensityBase = _vignette.intensity.value;
                _vignetteIntensityDisplay = _vignetteIntensityBase;
            }

            if (_chromatic != null)
            {
                _chromaticIntensityBase = _chromatic.intensity.value;
                _chromaticIntensityDisplay = _chromaticIntensityBase;
            }

            _armed = true;
        }

        private static bool IsVanillaGlocVisuallyActive(CameraStateManager cam)
        {
            Image blackout = cam.GetBlackoutImage();
            if (blackout == null || !blackout.enabled)
                return false;
            return blackout.color.a > 0.04f;
        }

        private static Color ReadBloomTintColor()
        {
            if (ColorUtility.TryParseHtmlString(GlocCameraPlugin.AtmosphereBloomTintHtml.Value, out Color c))
                return c;
            return new Color(1f, 0.96f, 0.9f, 1f);
        }

        private static Color ReadColorFilter()
        {
            if (ColorUtility.TryParseHtmlString(GlocCameraPlugin.AtmosphereColorFilterHtml.Value, out Color c))
                return c;
            return new Color(1f, 0.98f, 0.94f, 1f);
        }

        private static void ApplySnapshotToVolume()
        {
            if (_bloom != null)
            {
                _bloom.intensity.Override(_bloomIntensityBase);
                _bloom.threshold.Override(_bloomThresholdBase);
                _bloom.scatter.Override(_bloomScatterBase);
                _bloom.tint.Override(_bloomTintBase);
            }

            if (_colorAdjustments != null)
            {
                _colorAdjustments.postExposure.Override(_postExposureBase);
                _colorAdjustments.saturation.Override(_saturationBase);
                _colorAdjustments.contrast.Override(_contrastBase);
                _colorAdjustments.colorFilter.Override(_colorFilterBase);
            }

            if (_vignette != null)
                _vignette.intensity.Override(_vignetteIntensityBase);

            if (_chromatic != null)
                _chromatic.intensity.Override(_chromaticIntensityBase);
        }

        private static void ClearArmedState()
        {
            _bloom = null;
            _colorAdjustments = null;
            _vignette = null;
            _chromatic = null;
            _armed = false;
        }
    }
}

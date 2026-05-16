using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GlocCamera_Engine
{
    /// <summary>
    /// Realistic Gen-III NVG grade on the game's NVG <see cref="Volume"/>:
    /// P43 green phosphor, near-monochrome, tube vignette, halation, light grain.
    /// Vanilla <see cref="NightVision.UpdateGain"/> stays authoritative for auto-gain.
    /// </summary>
    internal static class GlocNightVisionDriver
    {
        private static Volume _volume;
        private static Bloom _bloom;
        private static ColorAdjustments _color;
        private static Vignette _vignette;
        private static FilmGrain _filmGrain;
        private static ChromaticAberration _chromatic;
        private static bool _armed;
        private static bool _hasFilmGrain;

        private static float _bloomIntensityBase;
        private static float _bloomThresholdBase;
        private static float _bloomScatterBase;
        private static Color _bloomTintBase;

        private static float _saturationBase;
        private static float _contrastBase;
        private static Color _colorFilterBase;
        private static float _vignetteIntensityBase;
        private static float _vignetteSmoothnessBase;
        private static float _filmGrainIntensityBase;
        private static float _chromaticIntensityBase;

        private static float _vanillaPostExposure;
        private static float _vanillaBloomThreshold;
        private static bool _hasVanillaGainSample;

        private static float _ambientSmoothed;
        private static float _targetExposure;
        private static float _displayExposure;
        private static float _exposureSmoothVel;
        private static float _targetBloomThreshold;
        private static float _displayBloomThreshold;
        private static float _thresholdSmoothVel;

        private static Light _nvLight;
        private static float _nvLightIntensityBase;
        private static float _nvLightRangeBase;
        private static bool _nvLightWasEnabled;

        private static Light _sceneIlluminator;
        private static float _sceneIntensityBase;
        private static bool _sceneWasEnabled;

        internal static void ForceRestore()
        {
            RestoreVolume();
            RestoreLights();
            ClearState();
        }

        internal static void Tick(CameraStateManager cam)
        {
            if (!GlocCameraPlugin.Enabled.Value || !GlocCameraPlugin.NightVisionEnabled.Value)
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

            if (!GlocNightVisionUtil.IsActive() || !GlocNightVisionUtil.TryGetVolume(out Volume vol))
            {
                if (_armed)
                    ForceRestore();
                return;
            }

            if (!_armed)
                TryArm(vol, cam);

            if (!_armed)
                return;

            if (!_hasVanillaGainSample && _color != null)
            {
                _vanillaPostExposure = _color.postExposure.value;
                _hasVanillaGainSample = true;
            }

            if (_bloom != null && _vanillaBloomThreshold <= 0f)
                _vanillaBloomThreshold = _bloom.threshold.value;

            float ambientRaw = 0.15f;
            var level = NetworkSceneSingleton<LevelInfo>.i;
            if (level != null)
                ambientRaw = Mathf.Max(0.001f, level.GetAmbientLight());

            float ambientSmooth = GlocCameraPlugin.NightVisionAmbientSmoothTimeSec.Value;
            if (ambientSmooth <= 0.01f)
                _ambientSmoothed = ambientRaw;
            else
            {
                float ka = 1f - Mathf.Exp(-Time.unscaledDeltaTime / ambientSmooth);
                _ambientSmoothed = Mathf.Lerp(_ambientSmoothed, ambientRaw, ka);
            }

            if (GlocNightVisionUtil.TryComputeVanillaGain(_ambientSmoothed, out float vanillaEv, out float vanillaTh))
            {
                _vanillaPostExposure = vanillaEv;
                _vanillaBloomThreshold = vanillaTh;
                _hasVanillaGainSample = true;
            }

            RecomputeTargets();
            StepSmoothValues(GlocCameraPlugin.NightVisionSmoothTimeSec.Value);
            ApplyOverridesToVolume();

            if (GlocCameraPlugin.NightVisionBoostSceneLights.Value)
            {
                float dark01 = ComputeDark01(_ambientSmoothed);
                ApplyLightBoost(dark01);
            }
        }

        private static float ComputeDark01(float ambient)
        {
            return 1f - Mathf.InverseLerp(
                GlocCameraPlugin.NightVisionAmbientBright.Value,
                GlocCameraPlugin.NightVisionAmbientDark.Value,
                ambient);
        }

        private static void RecomputeTargets()
        {
            if (!_hasVanillaGainSample)
                return;

            float dark01 = ComputeDark01(_ambientSmoothed);
            float exposureAdd = Mathf.Lerp(
                GlocCameraPlugin.NightVisionPostExposureAddBright.Value,
                GlocCameraPlugin.NightVisionPostExposureAddDark.Value,
                dark01);
            float scaledEv = _vanillaPostExposure * GlocCameraPlugin.NightVisionPostExposureScale.Value;
            _targetExposure = Mathf.Clamp(
                scaledEv + exposureAdd,
                GlocCameraPlugin.NightVisionPostExposureMin.Value,
                GlocCameraPlugin.NightVisionPostExposureMax.Value);
            _targetBloomThreshold = Mathf.Max(
                0.05f,
                _vanillaBloomThreshold + GlocCameraPlugin.NightVisionBloomThresholdAdd.Value);
        }

        private static void StepSmoothValues(float smoothTime)
        {
            if (smoothTime <= 0.01f)
            {
                _displayExposure = _targetExposure;
                _displayBloomThreshold = _targetBloomThreshold;
                _exposureSmoothVel = 0f;
                _thresholdSmoothVel = 0f;
                return;
            }

            _displayExposure = Mathf.SmoothDamp(
                _displayExposure, _targetExposure, ref _exposureSmoothVel, smoothTime);
            _displayBloomThreshold = Mathf.SmoothDamp(
                _displayBloomThreshold, _targetBloomThreshold, ref _thresholdSmoothVel, smoothTime);
        }

        private static void ApplyOverridesToVolume()
        {
            if (_color != null)
            {
                _color.postExposure.Override(_displayExposure);

                if (GlocCameraPlugin.NightVisionAbsoluteGrade.Value)
                {
                    _color.saturation.Override(GlocCameraPlugin.NightVisionSaturation.Value);
                    _color.contrast.Override(GlocCameraPlugin.NightVisionContrast.Value);
                }

                float filterStrength = GlocCameraPlugin.NightVisionColorFilterStrength.Value;
                if (filterStrength > 0.001f)
                {
                    Color filter = ReadPhosphorFilter();
                    _color.colorFilter.Override(Color.Lerp(_colorFilterBase, filter, filterStrength));
                }
                else
                    _color.colorFilter.Override(_colorFilterBase);
            }

            if (_bloom != null)
            {
                _bloom.threshold.Override(_displayBloomThreshold);
                _bloom.intensity.Override(
                    Mathf.Max(0f, _bloomIntensityBase + GlocCameraPlugin.NightVisionBloomIntensityAdd.Value));
                _bloom.scatter.Override(
                    Mathf.Clamp01(_bloomScatterBase + GlocCameraPlugin.NightVisionBloomScatterAdd.Value));

                float tintBlend = GlocCameraPlugin.NightVisionBloomTintBlend.Value;
                if (tintBlend > 0.001f)
                {
                    Color tint = ReadPhosphorFilter();
                    _bloom.tint.Override(Color.Lerp(_bloomTintBase, tint, tintBlend));
                }
                else
                    _bloom.tint.Override(_bloomTintBase);
            }

            if (_vignette != null)
            {
                _vignette.intensity.Override(GlocCameraPlugin.NightVisionVignetteIntensity.Value);
                _vignette.smoothness.Override(GlocCameraPlugin.NightVisionVignetteSmoothness.Value);
            }

            if (_filmGrain != null && _hasFilmGrain)
            {
                float grain = GlocCameraPlugin.NightVisionFilmGrainIntensity.Value;
                _filmGrain.intensity.Override(Mathf.Clamp01(grain));
            }

            if (_chromatic != null)
            {
                float chroma = GlocCameraPlugin.NightVisionChromaticIntensity.Value;
                _chromatic.intensity.Override(Mathf.Clamp01(chroma));
            }
        }

        private static void TryArm(Volume vol, CameraStateManager cam)
        {
            _volume = vol;
            VolumeProfile profile = vol.profile != null ? vol.profile : vol.sharedProfile;
            if (profile == null)
                return;

            profile.TryGet(out _bloom);
            profile.TryGet(out _color);
            profile.TryGet(out _vignette);
            _hasFilmGrain = profile.TryGet(out _filmGrain);
            profile.TryGet(out _chromatic);

            if (_bloom == null && _color == null)
                return;

            if (_bloom != null)
            {
                _bloomIntensityBase = _bloom.intensity.value;
                _bloomThresholdBase = _bloom.threshold.value;
                _bloomScatterBase = _bloom.scatter.value;
                _bloomTintBase = _bloom.tint.value;
                _vanillaBloomThreshold = _bloomThresholdBase;
                _displayBloomThreshold = _bloomThresholdBase;
                _targetBloomThreshold = _bloomThresholdBase;
            }

            if (_color != null)
            {
                _vanillaPostExposure = _color.postExposure.value;
                _displayExposure = _vanillaPostExposure;
                _targetExposure = _vanillaPostExposure;
                _hasVanillaGainSample = true;
                _saturationBase = _color.saturation.value;
                _contrastBase = _color.contrast.value;
                _colorFilterBase = _color.colorFilter.value;
            }

            if (_vignette != null)
            {
                _vignetteIntensityBase = _vignette.intensity.value;
                _vignetteSmoothnessBase = _vignette.smoothness.value;
            }

            if (_filmGrain != null)
                _filmGrainIntensityBase = _filmGrain.intensity.value;

            if (_chromatic != null)
                _chromaticIntensityBase = _chromatic.intensity.value;

            _nvLight = cam.nightVisLight;
            if (_nvLight != null)
            {
                _nvLightIntensityBase = _nvLight.intensity;
                _nvLightRangeBase = _nvLight.range;
                _nvLightWasEnabled = _nvLight.enabled;
            }

            var levelInfo = NetworkSceneSingleton<LevelInfo>.i;
            if (levelInfo != null && levelInfo.nightVisionIlluminator != null)
            {
                _sceneIlluminator = levelInfo.nightVisionIlluminator;
                _sceneIntensityBase = _sceneIlluminator.intensity;
                _sceneWasEnabled = _sceneIlluminator.enabled;
            }

            SampleAmbientImmediate();
            if (GlocNightVisionUtil.TryComputeVanillaGain(_ambientSmoothed, out float vanillaEv, out float vanillaTh))
            {
                _vanillaPostExposure = vanillaEv;
                _vanillaBloomThreshold = vanillaTh;
                _hasVanillaGainSample = true;
            }

            RecomputeTargets();
            if (GlocCameraPlugin.NightVisionSnapExposureOnArm.Value)
            {
                _displayExposure = _targetExposure;
                _displayBloomThreshold = _targetBloomThreshold;
                _exposureSmoothVel = 0f;
                _thresholdSmoothVel = 0f;
            }

            _armed = true;
        }

        private static void SampleAmbientImmediate()
        {
            _ambientSmoothed = 0.15f;
            var level = NetworkSceneSingleton<LevelInfo>.i;
            if (level != null)
                _ambientSmoothed = Mathf.Max(0.001f, level.GetAmbientLight());
        }

        private static void ApplyLightBoost(float dark01)
        {
            float mul = Mathf.Lerp(
                GlocCameraPlugin.NightVisionLightMulBright.Value,
                GlocCameraPlugin.NightVisionLightMulDark.Value,
                dark01);

            if (_nvLight != null)
            {
                _nvLight.enabled = true;
                _nvLight.intensity = _nvLightIntensityBase * mul;
                _nvLight.range = _nvLightRangeBase * GlocCameraPlugin.NightVisionLightRangeMul.Value;
            }

            if (_sceneIlluminator != null)
            {
                _sceneIlluminator.enabled = true;
                _sceneIlluminator.intensity = _sceneIntensityBase * mul * GlocCameraPlugin.NightVisionSceneIlluminatorMul.Value;
            }
        }

        private static Color ReadPhosphorFilter()
        {
            if (ColorUtility.TryParseHtmlString(GlocCameraPlugin.NightVisionColorFilterHtml.Value, out Color c))
                return c;
            return new Color(0.55f, 1f, 0.68f, 1f);
        }

        private static void RestoreVolume()
        {
            if (_bloom != null)
            {
                _bloom.intensity.Override(_bloomIntensityBase);
                _bloom.threshold.Override(_bloomThresholdBase);
                _bloom.scatter.Override(_bloomScatterBase);
                _bloom.tint.Override(_bloomTintBase);
            }

            if (_color != null)
            {
                _color.postExposure.Override(_vanillaPostExposure > 0f || _hasVanillaGainSample ? _vanillaPostExposure : _color.postExposure.value);
                _color.saturation.Override(_saturationBase);
                _color.contrast.Override(_contrastBase);
                _color.colorFilter.Override(_colorFilterBase);
            }

            if (_vignette != null)
            {
                _vignette.intensity.Override(_vignetteIntensityBase);
                _vignette.smoothness.Override(_vignetteSmoothnessBase);
            }

            if (_filmGrain != null && _hasFilmGrain)
                _filmGrain.intensity.Override(_filmGrainIntensityBase);

            if (_chromatic != null)
                _chromatic.intensity.Override(_chromaticIntensityBase);
        }

        private static void RestoreLights()
        {
            if (_nvLight != null)
            {
                _nvLight.intensity = _nvLightIntensityBase;
                _nvLight.range = _nvLightRangeBase;
                _nvLight.enabled = _nvLightWasEnabled;
            }

            if (_sceneIlluminator != null)
            {
                _sceneIlluminator.intensity = _sceneIntensityBase;
                _sceneIlluminator.enabled = _sceneWasEnabled;
            }
        }

        private static void ClearState()
        {
            _volume = null;
            _bloom = null;
            _color = null;
            _vignette = null;
            _filmGrain = null;
            _chromatic = null;
            _hasFilmGrain = false;
            _nvLight = null;
            _sceneIlluminator = null;
            _hasVanillaGainSample = false;
            _ambientSmoothed = 0f;
            _exposureSmoothVel = 0f;
            _thresholdSmoothVel = 0f;
            _armed = false;
        }
    }
}

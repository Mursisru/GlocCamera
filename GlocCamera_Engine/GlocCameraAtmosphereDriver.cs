using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GlocCamera_Engine
{
    /// <summary>
    /// Cockpit-only tweaks to URP post on the game's main <see cref="Volume"/>:
    /// snapshots baseline Bloom / Color Adjustments on enter, lerps toward day/night targets, restores on leave.
    /// Does not swap <see cref="VolumeProfile"/> instances so other systems (e.g. G-LOC) keep valid component refs.
    /// </summary>
    internal static class GlocCameraAtmosphereDriver
    {
        private static Bloom _bloom;
        private static ColorAdjustments _colorAdjustments;
        private static bool _armed;
        private static bool _loggedMissingComponents;
        private static float _bloomIntensityBase;
        private static float _bloomThresholdBase;
        private static float _postExposureBase;
        private static float _bloomDisplay;
        private static float _bloomThresholdDisplay;
        private static float _postExposureDisplay;

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

            Aircraft local;
            if (!GameManager.GetLocalAircraft(out local))
            {
                if (_armed)
                    ForceRestore();
                return;
            }

            var hud = SceneSingleton<CombatHUD>.i;
            var ac = hud != null ? hud.aircraft : null;
            if (ac == null || ac != local || cam.followingUnit != local)
            {
                if (_armed)
                    ForceRestore();
                return;
            }

            var vol = cam.GetPostProcessVolume();
            if (vol == null)
                return;

            VolumeProfile profile = vol.profile != null ? vol.profile : vol.sharedProfile;
            if (profile == null)
                return;

            if (!_armed)
            {
                _bloom = null;
                _colorAdjustments = null;
                if (profile.TryGet(out Bloom b))
                    _bloom = b;
                if (profile.TryGet(out ColorAdjustments c))
                    _colorAdjustments = c;

                if (_bloom == null && _colorAdjustments == null)
                {
                    if (!_loggedMissingComponents)
                    {
                        _loggedMissingComponents = true;
                        GlocCameraPlugin.ModLog?.LogDebug(
                            "G-LOC Camera atmosphere: Volume profile has no Bloom or ColorAdjustments; atmosphere disabled for this session.");
                    }
                    return;
                }

                _bloomIntensityBase = _bloom != null ? ReadBloomIntensity(_bloom) : 0f;
                _bloomThresholdBase = _bloom != null ? ReadBloomThreshold(_bloom) : 0f;
                _postExposureBase = _colorAdjustments != null ? _colorAdjustments.postExposure.value : 0f;
                _bloomDisplay = _bloomIntensityBase;
                _bloomThresholdDisplay = _bloomThresholdBase;
                _postExposureDisplay = _postExposureBase;
                _armed = true;
            }

            float night01 = ComputeNight01();
            float dt = Time.unscaledDeltaTime;
            float speed = Mathf.Max(0.01f, GlocCameraPlugin.AtmosphereLerpSpeed.Value);
            float t = 1f - Mathf.Exp(-speed * dt);

            if (_bloom != null)
            {
                float target = _bloomIntensityBase
                    + Mathf.Lerp(
                        GlocCameraPlugin.AtmosphereBloomAddDay.Value,
                        GlocCameraPlugin.AtmosphereBloomAddNight.Value,
                        night01);
                _bloomDisplay = Mathf.Lerp(_bloomDisplay, target, t);
                WriteBloomIntensity(_bloom, _bloomDisplay);

                // Lower Bloom.threshold at night => more cockpit emissive elements participate in bloom.
                float thresholdTarget = _bloomThresholdBase
                    + Mathf.Lerp(
                        GlocCameraPlugin.AtmosphereBloomThresholdAddDay.Value,
                        GlocCameraPlugin.AtmosphereBloomThresholdAddNight.Value,
                        night01);
                _bloomThresholdDisplay = Mathf.Lerp(_bloomThresholdDisplay, thresholdTarget, t);
                WriteBloomThreshold(_bloom, _bloomThresholdDisplay);
            }

            if (_colorAdjustments != null)
            {
                float target = _postExposureBase
                    + Mathf.Lerp(
                        GlocCameraPlugin.AtmospherePostExposureAddDay.Value,
                        GlocCameraPlugin.AtmospherePostExposureAddNight.Value,
                        night01);
                _postExposureDisplay = Mathf.Lerp(_postExposureDisplay, target, t);
                _colorAdjustments.postExposure.Override(_postExposureDisplay);
            }
        }

        private static void ApplySnapshotToVolume()
        {
            if (_bloom != null)
            {
                WriteBloomThreshold(_bloom, _bloomThresholdBase);
                WriteBloomIntensity(_bloom, _bloomIntensityBase);
            }
            if (_colorAdjustments != null)
                _colorAdjustments.postExposure.Override(_postExposureBase);
        }

        private static void ClearArmedState()
        {
            _bloom = null;
            _colorAdjustments = null;
            _armed = false;
        }

        private static float ComputeNight01()
        {
            var sun = RenderSettings.sun;
            if (sun != null)
            {
                float d = Vector3.Dot(-sun.transform.forward.normalized, Vector3.up);
                float dayDot = GlocCameraPlugin.AtmosphereSunDotDay.Value;
                float nightDot = GlocCameraPlugin.AtmosphereSunDotNight.Value;
                float lo = Mathf.Min(dayDot, nightDot);
                float hi = Mathf.Max(dayDot, nightDot);
                return Mathf.Clamp01(1f - Mathf.InverseLerp(lo, hi, d));
            }

            return Mathf.Clamp01(GlocCameraPlugin.AtmosphereNightNoSun.Value);
        }

        private static float ReadBloomIntensity(Bloom bloom)
        {
            return bloom.intensity.value;
        }

        private static float ReadBloomThreshold(Bloom bloom)
        {
            return bloom.threshold.value;
        }

        private static void WriteBloomIntensity(Bloom bloom, float value)
        {
            bloom.intensity.Override(value);
        }

        private static void WriteBloomThreshold(Bloom bloom, float value)
        {
            bloom.threshold.Override(value);
        }
    }
}

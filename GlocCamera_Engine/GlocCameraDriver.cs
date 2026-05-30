using UnityEngine;

namespace GlocCamera_Engine
{
    /// <summary>
    /// Smooths longitudinal acceleration (accel · forward) into FOV offset and optional cockpit dolly. Total maneuver G is not used.
    /// </summary>
    internal static class GlocCameraDriver
    {
        private static float _smoothedFovDelta;
        private static float _smoothedDollyZ;
        private static float _fovSmoothVel;
        private static float _dollySmoothVel;

        internal static void Tick(CameraStateManager cam)
        {
            if (!GlocCameraPlugin.Enabled.Value || cam == null)
            {
                Decay(Time.unscaledDeltaTime);
                return;
            }

            if (cam.currentState != cam.cockpitState)
            {
                Decay(Time.unscaledDeltaTime);
                return;
            }

            Aircraft local;
            if (!GlocFrameContext.HasLocalAircraft)
            {
                Decay(Time.unscaledDeltaTime);
                return;
            }

            local = GlocFrameContext.LocalAircraft;
            if (!GlocFrameContext.IsLocalPilotReady(cam))
            {
                Decay(Time.unscaledDeltaTime);
                return;
            }

            float longG = Vector3.Dot(GlocFrameContext.HudAircraft.accel, GlocFrameContext.HudAircraft.transform.forward);
            float dzL = GlocCameraPlugin.DeadZoneLongG.Value;
            if (Mathf.Abs(longG) < dzL)
                longG = 0f;

            float rawFov = longG * GlocCameraPlugin.FovPerLongitudinalG.Value;
            float maxD = GlocCameraPlugin.MaxFovDelta.Value;
            rawFov = Mathf.Clamp(rawFov, -maxD, maxD);

            float dt = Time.unscaledDeltaTime;
            float fovT = Mathf.Max(0.04f, GlocCameraPlugin.FovSmoothTimeSec.Value);
            float dollyT = Mathf.Max(0.04f, GlocCameraPlugin.DollySmoothTimeSec.Value);
            float fovMax = GlocCameraPlugin.FovSmoothMaxDegPerSec.Value;
            float dollyMax = GlocCameraPlugin.DollySmoothMaxMetersPerSec.Value;

            _smoothedFovDelta = Mathf.SmoothDamp(
                _smoothedFovDelta,
                rawFov,
                ref _fovSmoothVel,
                fovT,
                fovMax > 0f ? fovMax : Mathf.Infinity,
                dt);

            float rawDolly = -longG * GlocCameraPlugin.DollyMetersPerLongG.Value;
            float dMax = GlocCameraPlugin.DollyMaxMeters.Value;
            rawDolly = Mathf.Clamp(rawDolly, -dMax, dMax);

            _smoothedDollyZ = Mathf.SmoothDamp(
                _smoothedDollyZ,
                rawDolly,
                ref _dollySmoothVel,
                dollyT,
                dollyMax > 0f ? dollyMax : Mathf.Infinity,
                dt);
        }

        internal static float GetSmoothedFovDelta() => _smoothedFovDelta;

        internal static float GetSmoothedDollyZ() => _smoothedDollyZ;

        private static void Decay(float dt)
        {
            float fovT = Mathf.Max(0.04f, GlocCameraPlugin.FovSmoothTimeSec.Value);
            float dollyT = Mathf.Max(0.04f, GlocCameraPlugin.DollySmoothTimeSec.Value);
            float fovMax = GlocCameraPlugin.FovSmoothMaxDegPerSec.Value;
            float dollyMax = GlocCameraPlugin.DollySmoothMaxMetersPerSec.Value;

            _smoothedFovDelta = Mathf.SmoothDamp(
                _smoothedFovDelta,
                0f,
                ref _fovSmoothVel,
                fovT,
                fovMax > 0f ? fovMax : Mathf.Infinity,
                dt);

            _smoothedDollyZ = Mathf.SmoothDamp(
                _smoothedDollyZ,
                0f,
                ref _dollySmoothVel,
                dollyT,
                dollyMax > 0f ? dollyMax : Mathf.Infinity,
                dt);
        }
    }
}

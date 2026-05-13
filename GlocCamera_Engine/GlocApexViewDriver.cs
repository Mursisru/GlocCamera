using UnityEngine;

namespace GlocCamera_Engine
{
    /// <summary>
    /// Smooth cockpit camera local offset from pitch / roll / yaw control inputs (TrackIR-off “apex peek”).
    /// </summary>
    internal static class GlocApexViewDriver
    {
        private static float _smX;
        private static float _smY;
        private static float _smZ;
        private static float _velX;
        private static float _velY;
        private static float _velZ;

        internal static void Tick(CameraStateManager cam)
        {
            float dt = Time.unscaledDeltaTime;
            if (!GlocCameraPlugin.Enabled.Value || !GlocCameraPlugin.ApexViewEnabled.Value || cam == null)
            {
                DecayToZero(dt);
                return;
            }

            if (cam.currentState != cam.cockpitState)
            {
                DecayToZero(dt);
                return;
            }

            if (!GameManager.flightControlsEnabled)
            {
                DecayToZero(dt);
                return;
            }

            Aircraft local;
            if (!GameManager.GetLocalAircraft(out local))
            {
                DecayToZero(dt);
                return;
            }

            var hud = SceneSingleton<CombatHUD>.i;
            var ac = hud != null ? hud.aircraft : null;
            if (ac == null || ac != local || cam.followingUnit != local)
            {
                DecayToZero(dt);
                return;
            }

            if (local.pilots == null || local.pilots.Length == 0 || local.pilots[0] == null || local.pilots[0].dead)
            {
                DecayToZero(dt);
                return;
            }

            if (local.cockpit == null || local.cockpit.IsDetached())
            {
                DecayToZero(dt);
                return;
            }

            var inputs = local.GetInputs();
            float pitch = Mathf.Clamp(inputs.pitch * GlocCameraPlugin.ApexViewPitchInputSign.Value, -1f, 1f);
            float roll = Mathf.Clamp(inputs.roll * GlocCameraPlugin.ApexViewRollInputSign.Value, -1f, 1f);
            float yaw = Mathf.Clamp(inputs.yaw * GlocCameraPlugin.ApexViewYawInputSign.Value, -1f, 1f);
            float dz = GlocCameraPlugin.ApexViewDeadZone.Value;
            if (Mathf.Abs(pitch) < dz)
                pitch = 0f;
            if (Mathf.Abs(roll) < dz)
                roll = 0f;
            if (Mathf.Abs(yaw) < dz)
                yaw = 0f;

            float tx = roll * GlocCameraPlugin.ApexViewLateralRollScale.Value
                + yaw * GlocCameraPlugin.ApexViewLateralYawScale.Value
                + pitch * GlocCameraPlugin.ApexViewLateralPitchScale.Value;
            float ty = roll * GlocCameraPlugin.ApexViewVerticalRollScale.Value
                + yaw * GlocCameraPlugin.ApexViewVerticalYawScale.Value
                + pitch * GlocCameraPlugin.ApexViewVerticalPitchScale.Value;
            float tz = roll * GlocCameraPlugin.ApexViewDepthRollScale.Value
                + yaw * GlocCameraPlugin.ApexViewDepthYawScale.Value
                + pitch * GlocCameraPlugin.ApexViewDepthPitchScale.Value;
            tx = Mathf.Clamp(tx, -1f, 1f);
            ty = Mathf.Clamp(ty, -1f, 1f);
            tz = Mathf.Clamp(tz, -1f, 1f);

            float targetX = tx * GlocCameraPlugin.ApexViewMaxLateralMeters.Value;
            float targetY = ty * GlocCameraPlugin.ApexViewMaxVerticalMeters.Value;
            float targetZ = tz * GlocCameraPlugin.ApexViewMaxDepthMeters.Value;

            float smoothT = Mathf.Max(0.04f, GlocCameraPlugin.ApexViewSmoothTimeSec.Value);
            float maxSpeed = GlocCameraPlugin.ApexViewSmoothMaxMetersPerSec.Value;
            float maxSp = maxSpeed > 0f ? maxSpeed : Mathf.Infinity;
            _smX = Mathf.SmoothDamp(_smX, targetX, ref _velX, smoothT, maxSp, dt);
            _smY = Mathf.SmoothDamp(_smY, targetY, ref _velY, smoothT, maxSp, dt);
            _smZ = Mathf.SmoothDamp(_smZ, targetZ, ref _velZ, smoothT, maxSp, dt);
        }

        internal static Vector3 GetSmoothedLocalOffset() => new Vector3(_smX, _smY, _smZ);

        internal static void ResetState()
        {
            _smX = 0f;
            _smY = 0f;
            _smZ = 0f;
            _velX = 0f;
            _velY = 0f;
            _velZ = 0f;
        }

        private static void DecayToZero(float dt)
        {
            float smoothT = Mathf.Max(0.04f, GlocCameraPlugin.ApexViewSmoothTimeSec.Value);
            float maxSpeed = GlocCameraPlugin.ApexViewSmoothMaxMetersPerSec.Value;
            float maxSp = maxSpeed > 0f ? maxSpeed : Mathf.Infinity;
            _smX = Mathf.SmoothDamp(_smX, 0f, ref _velX, smoothT, maxSp, dt);
            _smY = Mathf.SmoothDamp(_smY, 0f, ref _velY, smoothT, maxSp, dt);
            _smZ = Mathf.SmoothDamp(_smZ, 0f, ref _velZ, smoothT, maxSp, dt);
        }
    }
}

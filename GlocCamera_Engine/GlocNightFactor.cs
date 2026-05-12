using UnityEngine;

namespace GlocCamera_Engine
{
    /// <summary>
    /// Shared day/night blend (0 = day, 1 = night) using the same sun heuristic as atmosphere.
    /// </summary>
    internal static class GlocNightFactor
    {
        internal static float ComputeNight01()
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
    }
}

using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace GlocCamera_Engine
{
    internal static class GlocNightVisionUtil
    {
        private static readonly FieldInfo NightVisActiveField =
            AccessTools.Field(typeof(NightVision), "nightVisActive");

        private static readonly FieldInfo NightVisVolumeField =
            AccessTools.Field(typeof(NightVision), "postProcessing");

        private static readonly FieldInfo GainMinField =
            AccessTools.Field(typeof(NightVision), "gainMin");

        private static readonly FieldInfo GainMaxField =
            AccessTools.Field(typeof(NightVision), "gainMax");

        private static readonly FieldInfo BloomThresholdMinField =
            AccessTools.Field(typeof(NightVision), "bloomThresholdMin");

        private static readonly FieldInfo BloomThresholdMaxField =
            AccessTools.Field(typeof(NightVision), "bloomThresholdMax");

        internal static bool TryGetNightVision(out NightVision nv)
        {
            nv = NightVision.i;
            return nv != null;
        }

        internal static bool IsActive()
        {
            if (!TryGetNightVision(out NightVision nv))
                return false;
            if (NightVisActiveField == null)
                return false;
            return NightVisActiveField.GetValue(nv) is bool on && on;
        }

        internal static bool TryGetVolume(out Volume volume)
        {
            volume = null;
            if (!TryGetNightVision(out NightVision nv) || NightVisVolumeField == null)
                return false;
            volume = NightVisVolumeField.GetValue(nv) as Volume;
            return volume != null && volume.enabled;
        }

        /// <summary>Same formula as vanilla <see cref="NightVision"/> UpdateGain, using smoothed ambient.</summary>
        internal static bool TryComputeVanillaGain(float ambientLight, out float postExposure, out float bloomThreshold)
        {
            postExposure = 0f;
            bloomThreshold = 0f;
            if (!TryGetNightVision(out NightVision nv))
                return false;
            if (GainMinField == null || GainMaxField == null
                || BloomThresholdMinField == null || BloomThresholdMaxField == null)
                return false;

            float gainMin = (float)GainMinField.GetValue(nv);
            float gainMax = (float)GainMaxField.GetValue(nv);
            float bloomMin = (float)BloomThresholdMinField.GetValue(nv);
            float bloomMax = (float)BloomThresholdMaxField.GetValue(nv);

            float t = Mathf.InverseLerp(0.01f, 0.4f, ambientLight);
            postExposure = Mathf.Lerp(gainMax, gainMin, t);
            bloomThreshold = Mathf.Lerp(bloomMin, bloomMax, t);
            return true;
        }
    }
}

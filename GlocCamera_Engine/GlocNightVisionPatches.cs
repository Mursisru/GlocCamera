using HarmonyLib;

namespace GlocCamera_Engine
{
    /// <summary>
    /// Vanilla <see cref="NightVision.UpdateGain"/> only runs once per second and causes visible pulsing.
    /// Gloc applies the same formula every frame with smoothed ambient instead.
    /// </summary>
    [HarmonyPatch(typeof(NightVision), "UpdateGain")]
    internal static class GlocNightVisionPatches
    {
        [HarmonyPrefix]
        private static bool Prefix()
        {
            if (!GlocCameraPlugin.Enabled.Value || !GlocCameraPlugin.NightVisionEnabled.Value)
                return true;
            if (!GlocNightVisionUtil.IsActive())
                return true;
            return false;
        }
    }
}

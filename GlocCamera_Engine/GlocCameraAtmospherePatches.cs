using HarmonyLib;

namespace GlocCamera_Engine
{
    [HarmonyPatch(typeof(CameraCockpitState), nameof(CameraCockpitState.LeaveState))]
    internal static class GlocCameraAtmospherePatches
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            GlocCameraAtmosphereDriver.ForceRestore();
            GlocCockpitLightingDriver.ForceRestore();
            GlocCameraShakeDriver.ResetState();
        }
    }
}

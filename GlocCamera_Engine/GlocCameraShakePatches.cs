using HarmonyLib;

namespace GlocCamera_Engine
{
    [HarmonyPatch(typeof(CameraCockpitState), nameof(CameraCockpitState.FixedUpdateState))]
    internal static class GlocCameraShakePatches
    {
        [HarmonyPostfix]
        private static void Postfix(CameraCockpitState __instance, CameraStateManager cam)
        {
            GlocCameraShakeDriver.Apply(__instance, cam);
        }
    }
}

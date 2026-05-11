using HarmonyLib;

namespace GlocCamera_Engine
{
    // External shake triggers (damage). Gear / runway / ground roll are sampled in GlocCameraShakeDriver.Apply.
    [HarmonyPatch]
    internal static class GlocCameraShakeSourcesPatches
    {
        [HarmonyPatch(typeof(Aircraft), nameof(Aircraft.RpcDamage))]
        private static class AircraftRpcDamagePatch
        {
            [HarmonyPostfix]
            private static void Postfix(Aircraft __instance, byte index, DamageInfo damageInfo)
            {
                GlocCameraShakeDriver.ReportRocketHit(__instance, damageInfo);
            }
        }
    }
}

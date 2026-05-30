using HarmonyLib;
using UnityEngine;

namespace GlocCamera_Engine
{
    /// <summary>One GetLocalAircraft / CombatHUD lookup per frame for all G-LOC drivers.</summary>
    internal static class GlocFrameContext
    {
        private static int _frame = -1;

        internal static bool HasLocalAircraft { get; private set; }
        internal static Aircraft LocalAircraft { get; private set; }
        internal static Aircraft HudAircraft { get; private set; }

        internal static void Refresh()
        {
            int frame = Time.frameCount;
            if (frame == _frame)
                return;

            _frame = frame;
            Aircraft local;
            HasLocalAircraft = GameManager.GetLocalAircraft(out local);
            LocalAircraft = local;
            CombatHUD hud = SceneSingleton<CombatHUD>.i;
            HudAircraft = hud != null ? hud.aircraft : null;
        }

        internal static bool IsLocalPilotReady(CameraStateManager cam)
        {
            return HasLocalAircraft
                && LocalAircraft != null
                && HudAircraft == LocalAircraft
                && cam != null
                && cam.followingUnit == LocalAircraft;
        }
    }
}

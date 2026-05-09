using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace GlocCamera_Engine
{
    [DefaultExecutionOrder(1)]
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class GlocCameraPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.at747.gloccamera";
        public const string PluginName = "G-LOC Camera";
        public const string PluginVersion = "1.2.0";

        internal static GlocCameraPlugin Instance { get; private set; }

        internal static ConfigEntry<bool> Enabled { get; private set; }
        internal static ConfigEntry<float> FovPerLongitudinalG { get; private set; }
        internal static ConfigEntry<float> DeadZoneLongG { get; private set; }
        internal static ConfigEntry<float> MaxFovDelta { get; private set; }
        internal static ConfigEntry<float> FovSmoothTimeSec { get; private set; }
        internal static ConfigEntry<float> FovSmoothMaxDegPerSec { get; private set; }
        internal static ConfigEntry<float> CockpitFovLerp { get; private set; }
        internal static ConfigEntry<float> DollyMetersPerLongG { get; private set; }
        internal static ConfigEntry<float> DollyMaxMeters { get; private set; }
        internal static ConfigEntry<float> DollySmoothTimeSec { get; private set; }
        internal static ConfigEntry<float> DollySmoothMaxMetersPerSec { get; private set; }

        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;
            Enabled = Config.Bind("General", "Enabled", true,
                "Cockpit FOV/dolly from throttle-style acceleration only (accel · forward). Maneuver G is ignored.");
            FovPerLongitudinalG = Config.Bind("FOV", "DegreesPerLongitudinalG", 5f,
                "FOV delta per longitudinal G (accel · nose forward). Positive → wider FOV when accelerating, narrower when braking along the nose. (~2× moderate strength vs old 10× defaults.)");
            DeadZoneLongG = Config.Bind("FOV", "DeadZoneLongitudinalG", 0.025f,
                "Longitudinal G below this magnitude is treated as zero.");
            MaxFovDelta = Config.Bind("FOV", "MaxDeltaDegrees", 40f,
                "Clamp magnitude of smoothed FOV offset (game still clamps final FOV to cockpit min/max).");
            FovSmoothTimeSec = Config.Bind("FOV", "SmoothTimeSeconds", 0.72f,
                "Approx. seconds to settle FOV offset (SmoothDamp). Higher = smoother, slower.");
            FovSmoothMaxDegPerSec = Config.Bind("FOV", "SmoothMaxDegreesPerSec", 18f,
                "Caps FOV offset change speed (°/s); use 0 for no cap. Lower = extra gentle.");
            CockpitFovLerp = Config.Bind("FOV", "CockpitLerpBlend", 0.11f,
                "Per-frame lerp toward cockpit FOV target (vanilla ~0.2). Lower = smoother overall FOV/zoom; higher = snappier.");

            DollyMetersPerLongG = Config.Bind("Dolly", "MetersPerLongitudinalG", 0.044f,
                "Local Z dolly per longitudinal G; applied as −(longG × this) so forward accel pulls the view back (TrackIR off). Flip sign here if it feels inverted.");
            DollyMaxMeters = Config.Bind("Dolly", "MaxMeters", 0.22f,
                "Clamp |dolly| along local Z.");
            DollySmoothTimeSec = Config.Bind("Dolly", "SmoothTimeSeconds", 0.68f,
                "Approx. seconds to settle dolly (SmoothDamp). Higher = smoother.");
            DollySmoothMaxMetersPerSec = Config.Bind("Dolly", "SmoothMaxMetersPerSec", 0.055f,
                "Caps dolly speed (m/s); use 0 for no cap. Lower = extra gentle.");

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(GlocCameraPatches));
            Logger.LogInfo($"{PluginName} {PluginVersion} loaded.");
        }

        /// <summary>
        /// Runs before <see cref="CameraStateManager.LateUpdate"/> (execution order 2) so smoothed G-FOV is ready for cockpit <c>UpdateState</c>.
        /// </summary>
        private void LateUpdate()
        {
            GlocCameraDriver.Tick(SceneSingleton<CameraStateManager>.i);
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}

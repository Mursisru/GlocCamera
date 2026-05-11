using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
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
        public const string PluginVersion = "1.4.3";

        internal static GlocCameraPlugin Instance { get; private set; }

        /// <summary>Log source usable from other <c>internal</c> types (base <see cref="BaseUnityPlugin.Logger"/> is not visible outside the plugin class).</summary>
        internal static ManualLogSource ModLog { get; private set; }

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

        internal static ConfigEntry<bool> ShakeEnabled { get; private set; }
        internal static ConfigEntry<float> ShakeManeuverScale { get; private set; }
        internal static ConfigEntry<float> ShakeLongJerkScale { get; private set; }
        internal static ConfigEntry<float> ShakeMaxLowShakeAdd { get; private set; }
        internal static ConfigEntry<float> ShakeMaxHighShakeAdd { get; private set; }
        internal static ConfigEntry<float> ShakeDeadZoneManeuverG { get; private set; }
        internal static ConfigEntry<float> ShakeDeadZoneLongJerk { get; private set; }
        internal static ConfigEntry<float> ShakeTrackIRScale { get; private set; }
        internal static ConfigEntry<float> ShakeManeuverSmoothTimeSec { get; private set; }
        internal static ConfigEntry<float> ShakeWeakPhaseSeverityExponent { get; private set; }

        internal static ConfigEntry<bool> ShakeStallEnabled { get; private set; }
        internal static ConfigEntry<float> ShakeStallStartRatio { get; private set; }
        internal static ConfigEntry<float> ShakeStallFullRatio { get; private set; }
        internal static ConfigEntry<float> ShakeStallLowMaxAdd { get; private set; }
        internal static ConfigEntry<float> ShakeStallHighMaxAdd { get; private set; }
        internal static ConfigEntry<float> ShakeStallSmoothTimeSec { get; private set; }

        internal static ConfigEntry<bool> ShakeOverspeedEnabled { get; private set; }
        internal static ConfigEntry<float> ShakeOverspeedStartRatio { get; private set; }
        internal static ConfigEntry<float> ShakeOverspeedFullRatio { get; private set; }
        internal static ConfigEntry<float> ShakeOverspeedLowMaxAdd { get; private set; }
        internal static ConfigEntry<float> ShakeOverspeedHighMaxAdd { get; private set; }
        internal static ConfigEntry<float> ShakeOverspeedSmoothTimeSec { get; private set; }

        internal static ConfigEntry<bool> ShakeVerticalGEnabled { get; private set; }
        internal static ConfigEntry<float> ShakeVerticalGStart { get; private set; }
        internal static ConfigEntry<float> ShakeVerticalGFull { get; private set; }
        internal static ConfigEntry<float> ShakeVerticalLowMaxAdd { get; private set; }
        internal static ConfigEntry<float> ShakeVerticalHighMaxAdd { get; private set; }
        internal static ConfigEntry<float> ShakeVerticalSmoothTimeSec { get; private set; }

        internal static ConfigEntry<bool> ShakeVrsEnabled { get; private set; }
        internal static ConfigEntry<float> ShakeVrsStartFactor { get; private set; }
        internal static ConfigEntry<float> ShakeVrsFullFactor { get; private set; }
        internal static ConfigEntry<float> ShakeVrsLowMaxAdd { get; private set; }
        internal static ConfigEntry<float> ShakeVrsHighMaxAdd { get; private set; }
        internal static ConfigEntry<float> ShakeVrsSmoothTimeSec { get; private set; }

        // Extra shake sources (gear / airbrakes / runway & ground / incoming rocket hits)
        internal static ConfigEntry<bool> ShakeGearEnabled { get; private set; }
        internal static ConfigEntry<float> ShakeGearMinSpeedKmh { get; private set; }
        internal static ConfigEntry<float> ShakeGearIntensityFullKmh { get; private set; }
        internal static ConfigEntry<float> ShakeGearBlendVibrationEndKmh { get; private set; }
        internal static ConfigEntry<float> ShakeGearBlendShakeEndKmh { get; private set; }
        internal static ConfigEntry<float> ShakeGearVibrationLowMaxAdd { get; private set; }
        internal static ConfigEntry<float> ShakeGearVibrationHighMaxAdd { get; private set; }
        internal static ConfigEntry<float> ShakeGearShakeLowMaxAdd { get; private set; }
        internal static ConfigEntry<float> ShakeGearShakeHighMaxAdd { get; private set; }
        internal static ConfigEntry<float> ShakeGearSmoothTimeSec { get; private set; }

        internal static ConfigEntry<bool> ShakeAirbrakeEnabled { get; private set; }
        internal static ConfigEntry<float> ShakeAirbrakeMinSpeedKmh { get; private set; }
        internal static ConfigEntry<float> ShakeAirbrakeOpenThreshold { get; private set; }
        internal static ConfigEntry<float> ShakeAirbrakeIntensityFullKmh { get; private set; }
        internal static ConfigEntry<float> ShakeAirbrakeBlendVibrationEndKmh { get; private set; }
        internal static ConfigEntry<float> ShakeAirbrakeBlendShakeEndKmh { get; private set; }
        internal static ConfigEntry<float> ShakeAirbrakeVibrationLowMaxAdd { get; private set; }
        internal static ConfigEntry<float> ShakeAirbrakeVibrationHighMaxAdd { get; private set; }
        internal static ConfigEntry<float> ShakeAirbrakeShakeLowMaxAdd { get; private set; }
        internal static ConfigEntry<float> ShakeAirbrakeShakeHighMaxAdd { get; private set; }
        internal static ConfigEntry<float> ShakeAirbrakeSmoothTimeSec { get; private set; }

        internal static ConfigEntry<bool> ShakeTouchdownEnabled { get; private set; }
        internal static ConfigEntry<float> ShakeTouchdownMinVsMps { get; private set; }
        internal static ConfigEntry<float> ShakeTouchdownVsFullMps { get; private set; }
        internal static ConfigEntry<float> ShakeTouchdownSpeedFullKmh { get; private set; }
        internal static ConfigEntry<float> ShakeTouchdownLowMaxAdd { get; private set; }
        internal static ConfigEntry<float> ShakeTouchdownHighMaxAdd { get; private set; }
        internal static ConfigEntry<float> ShakeTouchdownDecayTimeSec { get; private set; }

        internal static ConfigEntry<bool> ShakeRunwayRollEnabled { get; private set; }
        internal static ConfigEntry<float> ShakeRunwayMinSpeedKmh { get; private set; }
        internal static ConfigEntry<float> ShakeRunwayFullSpeedKmh { get; private set; }
        internal static ConfigEntry<float> ShakeRunwayVibrationFocusStartKmh { get; private set; }
        internal static ConfigEntry<float> ShakeRunwayVibrationLowMaxAdd { get; private set; }
        internal static ConfigEntry<float> ShakeRunwayVibrationHighMaxAdd { get; private set; }
        internal static ConfigEntry<float> ShakeRunwayShakeLowMaxAdd { get; private set; }
        internal static ConfigEntry<float> ShakeRunwayShakeHighMaxAdd { get; private set; }
        internal static ConfigEntry<float> ShakeRunwayBlendVibrationEndKmh { get; private set; }
        internal static ConfigEntry<float> ShakeRunwayBlendShakeEndKmh { get; private set; }
        internal static ConfigEntry<float> ShakeRunwaySmoothTimeSec { get; private set; }

        internal static ConfigEntry<bool> ShakeGroundRollEnabled { get; private set; }
        internal static ConfigEntry<float> ShakeGroundMinSpeedKmh { get; private set; }
        internal static ConfigEntry<float> ShakeGroundFullSpeedKmh { get; private set; }
        internal static ConfigEntry<float> ShakeGroundVibrationFocusStartKmh { get; private set; }
        internal static ConfigEntry<float> ShakeGroundVibrationLowMaxAdd { get; private set; }
        internal static ConfigEntry<float> ShakeGroundVibrationHighMaxAdd { get; private set; }
        internal static ConfigEntry<float> ShakeGroundShakeLowMaxAdd { get; private set; }
        internal static ConfigEntry<float> ShakeGroundShakeHighMaxAdd { get; private set; }
        internal static ConfigEntry<float> ShakeGroundBlendVibrationEndKmh { get; private set; }
        internal static ConfigEntry<float> ShakeGroundBlendShakeEndKmh { get; private set; }
        internal static ConfigEntry<float> ShakeGroundSmoothTimeSec { get; private set; }

        internal static ConfigEntry<bool> ShakeRocketHitEnabled { get; private set; }
        internal static ConfigEntry<float> ShakeRocketHitMinBlast { get; private set; }
        internal static ConfigEntry<float> ShakeRocketHitDamageScale { get; private set; }
        internal static ConfigEntry<float> ShakeRocketHitImpactScale { get; private set; }
        internal static ConfigEntry<float> ShakeRocketHitLowMaxAdd { get; private set; }
        internal static ConfigEntry<float> ShakeRocketHitHighMaxAdd { get; private set; }
        internal static ConfigEntry<float> ShakeRocketHitDecayTimeSec { get; private set; }

        internal static ConfigEntry<bool> AtmosphereEnabled { get; private set; }
        internal static ConfigEntry<float> AtmosphereLerpSpeed { get; private set; }
        internal static ConfigEntry<float> AtmosphereBloomAddDay { get; private set; }
        internal static ConfigEntry<float> AtmosphereBloomAddNight { get; private set; }
        internal static ConfigEntry<float> AtmosphereBloomThresholdAddDay { get; private set; }
        internal static ConfigEntry<float> AtmosphereBloomThresholdAddNight { get; private set; }
        internal static ConfigEntry<float> AtmospherePostExposureAddDay { get; private set; }
        internal static ConfigEntry<float> AtmospherePostExposureAddNight { get; private set; }
        internal static ConfigEntry<float> AtmosphereSunDotDay { get; private set; }
        internal static ConfigEntry<float> AtmosphereSunDotNight { get; private set; }
        internal static ConfigEntry<float> AtmosphereNightNoSun { get; private set; }

        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;
            ModLog = Logger;
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

            ShakeEnabled = Config.Bind("Shake", "Enabled", true,
                "Extra cockpit shake via vanilla CameraCockpitState.AddShake from lateral maneuver G and longitudinal jerk (accel · nose). Conservative by default.");
            ShakeManeuverScale = Config.Bind("Shake", "ManeuverScale", 0.018f,
                "High-frequency shake add per unit lateral G (|accel − (accel·f)f|) after dead zone.");
            ShakeLongJerkScale = Config.Bind("Shake", "LongJerkScale", 0.0009f,
                "Low-frequency shake add per unit of |Δ(longG)|/s (longG = Dot(accel, forward)).");
            ShakeMaxLowShakeAdd = Config.Bind("Shake", "MaxLowShakeAdd", 0.06f,
                "Per FixedUpdate cap on extra low-frequency shake added (vanilla also drives low shake from jerk).");
            ShakeMaxHighShakeAdd = Config.Bind("Shake", "MaxHighShakeAdd", 0.07f,
                "Per FixedUpdate cap on extra high-frequency shake from maneuvers.");
            ShakeDeadZoneManeuverG = Config.Bind("Shake", "DeadZoneManeuverG", 0.06f,
                "Lateral G magnitude below this does not add maneuver shake.");
            ShakeDeadZoneLongJerk = Config.Bind("Shake", "DeadZoneLongJerk", 18f,
                "|Δ(longG)|/s below this does not add longitudinal jerk shake.");
            ShakeTrackIRScale = Config.Bind("Shake", "TrackIRScale", 0.22f,
                "When TrackIR is on, extra shake is multiplied by this (0 = off, 1 = full).");
            ShakeManeuverSmoothTimeSec = Config.Bind("Shake", "ManeuverSmoothTimeSeconds", 0.08f,
                "SmoothDamp time on lateral maneuver magnitude (0 = no smoothing).");
            ShakeWeakPhaseSeverityExponent = Config.Bind("Shake", "WeakPhaseSeverityExponent", 1.2f,
                "For stall / overspeed / vertical G / VRS / rocket hit / touchdown: low severity stays **vibration-heavy** (more high-freq); blend toward your configured low/high caps as severity approaches 1. Higher exponent = weak phases stay buzzier longer.");

            // Event shakes: stall / overspeed / vertical maneuver / vortex ring (helicopter).
            ShakeStallEnabled = Config.Bind("Shake", "StallEnabled", true,
                "Add extra cockpit shake when speed falls below aircraft stall speed (severity-based).");
            ShakeStallStartRatio = Config.Bind("Shake", "StallStartRatio", 1.05f,
                "Stall severity starts when speed/stallSpeed <= this ratio.");
            ShakeStallFullRatio = Config.Bind("Shake", "StallFullRatio", 0.85f,
                "Stall severity reaches 1 when speed/stallSpeed <= this ratio.");
            ShakeStallLowMaxAdd = Config.Bind("Shake", "StallLowMaxAdd", 0.04f,
                "Max extra low-frequency shake from stall at full severity (0-1 scale).");
            ShakeStallHighMaxAdd = Config.Bind("Shake", "StallHighMaxAdd", 0.02f,
                "Max extra high-frequency shake from stall at full severity (0-1 scale).");
            ShakeStallSmoothTimeSec = Config.Bind("Shake", "StallSmoothTimeSeconds", 0.12f,
                "SmoothDamp time for stall severity (0 = no smoothing).");

            ShakeOverspeedEnabled = Config.Bind("Shake", "OverspeedEnabled", true,
                "Add extra cockpit shake when speed exceeds aircraft max speed (severity-based).");
            ShakeOverspeedStartRatio = Config.Bind("Shake", "OverspeedStartRatio", 1.02f,
                "Overspeed severity starts when speed/maxSpeed >= this ratio.");
            ShakeOverspeedFullRatio = Config.Bind("Shake", "OverspeedFullRatio", 1.15f,
                "Overspeed severity reaches 1 when speed/maxSpeed >= this ratio.");
            ShakeOverspeedLowMaxAdd = Config.Bind("Shake", "OverspeedLowMaxAdd", 0.03f,
                "Max extra low-frequency shake from overspeed at full severity (0-1 scale).");
            ShakeOverspeedHighMaxAdd = Config.Bind("Shake", "OverspeedHighMaxAdd", 0.04f,
                "Max extra high-frequency shake from overspeed at full severity (0-1 scale).");
            ShakeOverspeedSmoothTimeSec = Config.Bind("Shake", "OverspeedSmoothTimeSeconds", 0.12f,
                "SmoothDamp time for overspeed severity (0 = no smoothing).");

            ShakeVerticalGEnabled = Config.Bind("Shake", "VerticalGEnabled", true,
                "Add extra cockpit shake when vertical acceleration (Dot(accel, up)) is large (pull/push).");
            ShakeVerticalGStart = Config.Bind("Shake", "VerticalGStart", 3.0f,
                "Vertical G severity starts when |verticalG| >= this value.");
            ShakeVerticalGFull = Config.Bind("Shake", "VerticalGFull", 7.0f,
                "Vertical G severity reaches 1 when |verticalG| >= this value.");
            ShakeVerticalLowMaxAdd = Config.Bind("Shake", "VerticalLowMaxAdd", 0.02f,
                "Max extra low-frequency shake from vertical G at full severity (0-1 scale).");
            ShakeVerticalHighMaxAdd = Config.Bind("Shake", "VerticalHighMaxAdd", 0.05f,
                "Max extra high-frequency shake from vertical G at full severity (0-1 scale).");
            ShakeVerticalSmoothTimeSec = Config.Bind("Shake", "VerticalSmoothTimeSeconds", 0.12f,
                "SmoothDamp time for vertical G severity (0 = no smoothing).");

            ShakeVrsEnabled = Config.Bind("Shake", "VrsEnabled", true,
                "Add extra cockpit shake when helicopter is in vortex ring state (VRS) (severity-based).");
            ShakeVrsStartFactor = Config.Bind("Shake", "VrsStartFactor", 0.10f,
                "VRS severity starts when RotorShaft VRS factor >= this value.");
            ShakeVrsFullFactor = Config.Bind("Shake", "VrsFullFactor", 0.35f,
                "VRS severity reaches 1 when RotorShaft VRS factor >= this value.");
            ShakeVrsLowMaxAdd = Config.Bind("Shake", "VrsLowMaxAdd", 0.05f,
                "Max extra low-frequency shake from VRS at full severity (0-1 scale).");
            ShakeVrsHighMaxAdd = Config.Bind("Shake", "VrsHighMaxAdd", 0.01f,
                "Max extra high-frequency shake from VRS at full severity (0-1 scale).");
            ShakeVrsSmoothTimeSec = Config.Bind("Shake", "VrsSmoothTimeSeconds", 0.12f,
                "SmoothDamp time for VRS severity (0 = no smoothing).");

            // Gear: no cockpit effect until min speed (km/h); then magnitude ramps; low/high split blends from vibration→shake with speed.
            ShakeGearEnabled = Config.Bind("Shake", "GearShakeEnabled", true,
                "Extra cockpit shake/vibration when landing gear is deployed and indicated airspeed is high (avoids runway/spawn jitter).");
            ShakeGearMinSpeedKmh = Config.Bind("Shake", "GearMinSpeedKmh", 500f,
                "No gear-driven shake below this indicated speed (km/h); game speed is m/s internally — this value is converted.");
            ShakeGearIntensityFullKmh = Config.Bind("Shake", "GearIntensityFullKmh", 620f,
                "Indicated speed (km/h) at which gear-driven magnitude reaches 1 (between GearMinSpeedKmh and this it ramps up).");
            ShakeGearBlendVibrationEndKmh = Config.Bind("Shake", "GearBlendVibrationEndKmh", 520f,
                "Up to this speed (km/h) the gear effect is mostly high-frequency vibration (after min speed).");
            ShakeGearBlendShakeEndKmh = Config.Bind("Shake", "GearBlendShakeEndKmh", 720f,
                "By this speed (km/h) the gear effect shifts toward low-frequency rumble/shake; between VibrationEnd and this it lerps smoothly.");
            ShakeGearVibrationLowMaxAdd = Config.Bind("Shake", "GearVibrationLowMaxAdd", 0.008f,
                "Max extra low-frequency AddShake at the vibration end of the gear blend.");
            ShakeGearVibrationHighMaxAdd = Config.Bind("Shake", "GearVibrationHighMaxAdd", 0.034f,
                "Max extra high-frequency AddShake at the vibration end of the gear blend.");
            ShakeGearShakeLowMaxAdd = Config.Bind("Shake", "GearShakeLowMaxAdd", 0.048f,
                "Max extra low-frequency AddShake at the shake end of the gear blend.");
            ShakeGearShakeHighMaxAdd = Config.Bind("Shake", "GearShakeHighMaxAdd", 0.014f,
                "Max extra high-frequency AddShake at the shake end of the gear blend.");
            ShakeGearSmoothTimeSec = Config.Bind("Shake", "GearSmoothTimeSeconds", 0.14f,
                "SmoothDamp time for gear magnitude and vibration/shake blend (0 = no smoothing).");

            // Airbrakes / speedbrakes (Airbrake.openAmount when throttle at 0): same vibration↔shake blend idea, gated by speed so parked idle does not rumble.
            ShakeAirbrakeEnabled = Config.Bind("Shake", "AirbrakeShakeEnabled", true,
                "Extra cockpit shake/vibration when airbrakes/spoilers are extended (vanilla Airbrake), scaled by deployment and speed.");
            ShakeAirbrakeMinSpeedKmh = Config.Bind("Shake", "AirbrakeMinSpeedKmh", 90f,
                "No airbrake-driven shake below this indicated speed (km/h).");
            ShakeAirbrakeOpenThreshold = Config.Bind("Shake", "AirbrakeOpenThreshold", 0.06f,
                "Treat airbrake as closed below this openAmount (0–1).");
            ShakeAirbrakeIntensityFullKmh = Config.Bind("Shake", "AirbrakeIntensityFullKmh", 260f,
                "Indicated speed (km/h) at which airbrake magnitude reaches 1 (when fully open); ramps from AirbrakeMinSpeedKmh.");
            ShakeAirbrakeBlendVibrationEndKmh = Config.Bind("Shake", "AirbrakeBlendVibrationEndKmh", 140f,
                "Mostly high-frequency vibration below this speed (km/h) once airbrake effect is active.");
            ShakeAirbrakeBlendShakeEndKmh = Config.Bind("Shake", "AirbrakeBlendShakeEndKmh", 420f,
                "Shifts toward low-frequency shake by this speed (km/h); lerps between VibrationEnd and this.");
            ShakeAirbrakeVibrationLowMaxAdd = Config.Bind("Shake", "AirbrakeVibrationLowMaxAdd", 0.005f,
                "Max extra low-frequency AddShake at the vibration end of the airbrake blend.");
            ShakeAirbrakeVibrationHighMaxAdd = Config.Bind("Shake", "AirbrakeVibrationHighMaxAdd", 0.022f,
                "Max extra high-frequency AddShake at the vibration end of the airbrake blend.");
            ShakeAirbrakeShakeLowMaxAdd = Config.Bind("Shake", "AirbrakeShakeLowMaxAdd", 0.028f,
                "Max extra low-frequency AddShake at the shake end of the airbrake blend.");
            ShakeAirbrakeShakeHighMaxAdd = Config.Bind("Shake", "AirbrakeShakeHighMaxAdd", 0.012f,
                "Max extra high-frequency AddShake at the shake end of the airbrake blend.");
            ShakeAirbrakeSmoothTimeSec = Config.Bind("Shake", "AirbrakeSmoothTimeSeconds", 0.12f,
                "SmoothDamp time for airbrake magnitude and blend (0 = no smoothing).");

            // Gear touchdown + taxi rumble (runway strip vs off-runway ground). Uses vanilla Airbase.AircraftIsOnRunway when near an airbase.
            ShakeTouchdownEnabled = Config.Bind("Shake", "TouchdownShakeEnabled", true,
                "One-shot cockpit jolt when landing gear touches after a real flight (airborne → on ground), scaled by sink rate and ground speed.");
            ShakeTouchdownMinVsMps = Config.Bind("Shake", "TouchdownMinVsMps", 0.35f,
                "Minimum downward vertical speed (m/s, world up) to count a touchdown for shake (filters tiny transitions).");
            ShakeTouchdownVsFullMps = Config.Bind("Shake", "TouchdownVsFullMps", 7.5f,
                "Downward speed (m/s) at which touchdown severity reaches full from the vertical axis alone (higher = gentler ramp).");
            ShakeTouchdownSpeedFullKmh = Config.Bind("Shake", "TouchdownSpeedFullKmh", 115f,
                "Ground speed (km/h, from aircraft.speed×3.6) at which horizontal motion contributes full touchdown severity (higher = gentler ramp).");
            ShakeTouchdownLowMaxAdd = Config.Bind("Shake", "TouchdownLowMaxAdd", 0.038f,
                "Max extra low-frequency AddShake at full touchdown severity.");
            ShakeTouchdownHighMaxAdd = Config.Bind("Shake", "TouchdownHighMaxAdd", 0.019f,
                "Max extra high-frequency AddShake at full touchdown severity.");
            ShakeTouchdownDecayTimeSec = Config.Bind("Shake", "TouchdownDecayTimeSeconds", 0.16f,
                "Decay time for touchdown shake back to zero.");

            ShakeRunwayRollEnabled = Config.Bind("Shake", "RunwayRollShakeEnabled", true,
                "Continuous rumble on runway strip (gear down, vanilla Airbase.AircraftIsOnRunway): mostly high-frequency vibration up to BlendVibrationEndKmh, then lerps toward low-frequency shake by BlendShakeEndKmh.");
            ShakeRunwayMinSpeedKmh = Config.Bind("Shake", "RunwayRollMinSpeedKmh", 4f,
                "No runway roll below this ground speed (km/h).");
            ShakeRunwayFullSpeedKmh = Config.Bind("Shake", "RunwayRollFullSpeedKmh", 105f,
                "Runway roll magnitude reaches 1 at this ground speed (km/h); sublinear Pow curve below full.");
            ShakeRunwayVibrationFocusStartKmh = Config.Bind("Shake", "RunwayRollVibrationFocusStartKmh", 100f,
                "Below this km/h, roll magnitude is scaled by speed/start (so parking / slow taxi stays light).");
            ShakeRunwayVibrationLowMaxAdd = Config.Bind("Shake", "RunwayRollVibrationLowMaxAdd", 0.0028f,
                "Low-frequency AddShake cap in the vibration-dominated band (≤ BlendVibrationEndKmh).");
            ShakeRunwayVibrationHighMaxAdd = Config.Bind("Shake", "RunwayRollVibrationHighMaxAdd", 0.0085f,
                "High-frequency AddShake cap in the vibration-dominated band.");
            ShakeRunwayShakeLowMaxAdd = Config.Bind("Shake", "RunwayRollShakeLowMaxAdd", 0.0095f,
                "Low-frequency AddShake cap at full shake (≥ BlendShakeEndKmh).");
            ShakeRunwayShakeHighMaxAdd = Config.Bind("Shake", "RunwayRollShakeHighMaxAdd", 0.0038f,
                "High-frequency AddShake cap at full shake.");
            ShakeRunwayBlendVibrationEndKmh = Config.Bind("Shake", "RunwayRollBlendVibrationEndKmh", 350f,
                "At and below this km/h (with magnitude already on), character stays vibration-heavy; blend toward shake starts above this.");
            ShakeRunwayBlendShakeEndKmh = Config.Bind("Shake", "RunwayRollBlendShakeEndKmh", 520f,
                "By this km/h the runway roll character reaches full shake end of the lerp.");
            ShakeRunwaySmoothTimeSec = Config.Bind("Shake", "RunwayRollSmoothTimeSeconds", 0.12f,
                "SmoothDamp time for runway roll magnitude and vibration↔shake blend (0 = no smoothing).");

            ShakeGroundRollEnabled = Config.Bind("Shake", "GroundRollShakeEnabled", true,
                "Same vibration→shake idea off-runway (grass, dirt, etc.); separate caps and blend speeds.");
            ShakeGroundMinSpeedKmh = Config.Bind("Shake", "GroundRollMinSpeedKmh", 3f,
                "No ground roll below this ground speed (km/h).");
            ShakeGroundFullSpeedKmh = Config.Bind("Shake", "GroundRollFullSpeedKmh", 72f,
                "Ground roll magnitude reaches 1 at this km/h; sublinear Pow below full.");
            ShakeGroundVibrationFocusStartKmh = Config.Bind("Shake", "GroundRollVibrationFocusStartKmh", 100f,
                "Below this km/h, ground roll magnitude is scaled by speed/start.");
            ShakeGroundVibrationLowMaxAdd = Config.Bind("Shake", "GroundRollVibrationLowMaxAdd", 0.0045f,
                "Low-frequency cap in vibration-dominated band (≤ GroundRollBlendVibrationEndKmh).");
            ShakeGroundVibrationHighMaxAdd = Config.Bind("Shake", "GroundRollVibrationHighMaxAdd", 0.013f,
                "High-frequency cap in vibration-dominated band.");
            ShakeGroundShakeLowMaxAdd = Config.Bind("Shake", "GroundRollShakeLowMaxAdd", 0.0145f,
                "Low-frequency cap at full shake (≥ GroundRollBlendShakeEndKmh).");
            ShakeGroundShakeHighMaxAdd = Config.Bind("Shake", "GroundRollShakeHighMaxAdd", 0.0055f,
                "High-frequency cap at full shake.");
            ShakeGroundBlendVibrationEndKmh = Config.Bind("Shake", "GroundRollBlendVibrationEndKmh", 350f,
                "Same role as runway: vibration-heavy at/under this km/h.");
            ShakeGroundBlendShakeEndKmh = Config.Bind("Shake", "GroundRollBlendShakeEndKmh", 500f,
                "Full shake character by this km/h.");
            ShakeGroundSmoothTimeSec = Config.Bind("Shake", "GroundRollSmoothTimeSeconds", 0.12f,
                "SmoothDamp time for ground roll magnitude and blend (0 = no smoothing).");

            // Incoming rocket / explosive hit shake
            ShakeRocketHitEnabled = Config.Bind("Shake", "RocketHitShakeEnabled", true,
                "Extra strong cockpit jolt on local aircraft damage caused by explosive hits (uses DamageInfo blast/impact).");
            ShakeRocketHitMinBlast = Config.Bind("Shake", "RocketHitMinBlast", 1.2f,
                "Minimum decompressed blastDamage required to treat damage as rocket/explosive shake.");
            ShakeRocketHitDamageScale = Config.Bind("Shake", "RocketHitDamageScale", 25f,
                "Damage scaling for rocket hit severity: severity01 = clamp01((blast-minBlast + impact*impactScale) / RocketHitDamageScale).");
            ShakeRocketHitImpactScale = Config.Bind("Shake", "RocketHitImpactScale", 0.25f,
                "Impact contribution to rocket hit severity (multiplies decompressed impactDamage).");
            ShakeRocketHitLowMaxAdd = Config.Bind("Shake", "RocketHitLowMaxAdd", 0.12f,
                "Maximum extra low-frequency shake at full rocket-hit severity.");
            ShakeRocketHitHighMaxAdd = Config.Bind("Shake", "RocketHitHighMaxAdd", 0.04f,
                "Maximum extra high-frequency shake at full rocket-hit severity.");
            ShakeRocketHitDecayTimeSec = Config.Bind("Shake", "RocketHitDecayTimeSeconds", 0.22f,
                "Time constant for rocket-hit shake decay to zero.");

            AtmosphereEnabled = Config.Bind("Atmosphere", "Enabled", true,
                "Cockpit-only: gently boost URP Bloom and post-exposure at night on the game's post volume; restores on leaving cockpit.");
            AtmosphereLerpSpeed = Config.Bind("Atmosphere", "LerpSpeed", 5f,
                "Approach speed toward day/night targets (higher = snappier).");
            AtmosphereBloomAddDay = Config.Bind("Atmosphere", "BloomIntensityAddDay", 0f,
                "Bloom intensity add at full day (relative to snapshot at cockpit enter).");
            AtmosphereBloomAddNight = Config.Bind("Atmosphere", "BloomIntensityAddNight", 0.32f,
                "Bloom intensity add at full night.");
            AtmosphereBloomThresholdAddDay = Config.Bind("Atmosphere", "BloomThresholdAddDay", 0f,
                "Bloom threshold add at full day (positive raises threshold => less bloom).");
            AtmosphereBloomThresholdAddNight = Config.Bind("Atmosphere", "BloomThresholdAddNight", -0.25f,
                "Bloom threshold add at full night (negative lowers threshold => more bloom, helps instrument glow).");
            AtmospherePostExposureAddDay = Config.Bind("Atmosphere", "PostExposureAddDay", 0f,
                "Color adjustments post-exposure add at full day (EV-style).");
            AtmospherePostExposureAddNight = Config.Bind("Atmosphere", "PostExposureAddNight", 0.45f,
                "Post-exposure add at full night.");
            AtmosphereSunDotDay = Config.Bind("Atmosphere", "SunDotDay", 0.32f,
                "Sun direction heuristic: Dot(-sun.forward, up) at/above this reads as day (see SunDotNight).");
            AtmosphereSunDotNight = Config.Bind("Atmosphere", "SunDotNight", -0.08f,
                "Dot(-sun.forward, up) at/below this reads as night; between SunDotNight and SunDotDay blends.");
            AtmosphereNightNoSun = Config.Bind("Atmosphere", "NightBlendNoSun", 0.45f,
                "Night blend (0–1) when RenderSettings.sun is null.");

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(GlocCameraPatches));
            _harmony.PatchAll(typeof(GlocCameraShakePatches));
            _harmony.PatchAll(typeof(GlocCameraShakeSourcesPatches));
            _harmony.PatchAll(typeof(GlocCameraAtmospherePatches));
            Logger.LogInfo($"{PluginName} {PluginVersion} loaded.");
        }

        /// <summary>
        /// Runs before <see cref="CameraStateManager.LateUpdate"/> (execution order 2) so smoothed G-FOV is ready for cockpit <c>UpdateState</c>.
        /// </summary>
        private void LateUpdate()
        {
            var cam = SceneSingleton<CameraStateManager>.i;
            GlocCameraDriver.Tick(cam);
            GlocCameraAtmosphereDriver.Tick(cam);
        }

        private void OnDestroy()
        {
            GlocCameraAtmosphereDriver.ForceRestore();
            GlocCameraShakeDriver.ResetState();
            _harmony?.UnpatchSelf();
        }
    }
}

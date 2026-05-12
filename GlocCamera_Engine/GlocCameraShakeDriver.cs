using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace GlocCamera_Engine
{
    /// <summary>
    /// Adds extra cockpit shake via vanilla <see cref="CameraCockpitState.AddShake"/> from lateral maneuver G
    /// and sharp longitudinal jerk (change in accel · forward), gated like FOV/dolly.
    /// </summary>
    internal static class GlocCameraShakeDriver
    {
        private static readonly FieldInfo PilotField = AccessTools.Field(typeof(CameraCockpitState), "pilot");
        private static readonly FieldInfo AirbrakeOpenAmountField = AccessTools.Field(typeof(Airbrake), "openAmount");

        private static float _prevLongG;
        private static bool _prevLongGValid;
        private static float _smoothedManeuverG;
        private static float _maneuverSmoothVel;

        private static float _stallSmoothed;
        private static float _stallSmoothVel;
        private static float _overspeedSmoothed;
        private static float _overspeedSmoothVel;
        private static float _verticalSmoothed;
        private static float _verticalSmoothVel;
        private static float _vrsSmoothed;
        private static float _vrsSmoothVel;

        private static float _gearDownSmoothed;
        private static float _gearDownSmoothVel;
        private static float _gearSpeedMagSmoothed;
        private static float _gearSpeedMagSmoothVel;
        private static float _gearBlendSmoothed;
        private static float _gearBlendSmoothVel;

        private static float _airbrakeMagSmoothed;
        private static float _airbrakeMagSmoothVel;
        private static float _airbrakeBlendSmoothed;
        private static float _airbrakeBlendSmoothVel;

        private static float _touchdownBumpSmoothed;
        private static float _touchdownBumpSmoothVel;
        private static float _runwayRollSmoothed;
        private static float _runwayRollSmoothVel;
        private static float _runwayRollBlendSmoothed;
        private static float _runwayRollBlendSmoothVel;
        private static float _groundRollSmoothed;
        private static float _groundRollSmoothVel;
        private static float _groundRollBlendSmoothed;
        private static float _groundRollBlendSmoothVel;

        private static bool _wasAirborne;
        private static bool _touchdownArmReady;

        private static float _rocketHitSmoothed;
        private static float _rocketHitSmoothVel;

        private static Aircraft _vrsAircraft;
        private static List<RotorShaft> _vrsSources;

        internal static void ResetState()
        {
            _prevLongGValid = false;
            _smoothedManeuverG = 0f;
            _maneuverSmoothVel = 0f;

            _stallSmoothed = 0f;
            _stallSmoothVel = 0f;
            _overspeedSmoothed = 0f;
            _overspeedSmoothVel = 0f;
            _verticalSmoothed = 0f;
            _verticalSmoothVel = 0f;
            _vrsSmoothed = 0f;
            _vrsSmoothVel = 0f;

            _gearDownSmoothed = 0f;
            _gearDownSmoothVel = 0f;
            _gearSpeedMagSmoothed = 0f;
            _gearSpeedMagSmoothVel = 0f;
            _gearBlendSmoothed = 0f;
            _gearBlendSmoothVel = 0f;

            _airbrakeMagSmoothed = 0f;
            _airbrakeMagSmoothVel = 0f;
            _airbrakeBlendSmoothed = 0f;
            _airbrakeBlendSmoothVel = 0f;

            _touchdownBumpSmoothed = 0f;
            _touchdownBumpSmoothVel = 0f;
            _runwayRollSmoothed = 0f;
            _runwayRollSmoothVel = 0f;
            _runwayRollBlendSmoothed = 0f;
            _runwayRollBlendSmoothVel = 0f;
            _groundRollSmoothed = 0f;
            _groundRollSmoothVel = 0f;
            _groundRollBlendSmoothed = 0f;
            _groundRollBlendSmoothVel = 0f;

            _wasAirborne = false;
            _touchdownArmReady = false;

            _rocketHitSmoothed = 0f;
            _rocketHitSmoothVel = 0f;

            _vrsAircraft = null;
            _vrsSources = null;
        }

        internal static void Apply(CameraCockpitState cockpitState, CameraStateManager cam)
        {
            if (!GlocCameraPlugin.Enabled.Value || !GlocCameraPlugin.ShakeEnabled.Value || cam == null || cockpitState == null)
                return;

            if (cam.currentState != cam.cockpitState)
            {
                _prevLongGValid = false;
                return;
            }

            var pilot = PilotField.GetValue(cockpitState) as Pilot;
            if (pilot == null || pilot.dead)
            {
                _prevLongGValid = false;
                return;
            }

            if (!GameManager.flightControlsEnabled)
                return;

            Aircraft local;
            if (!GameManager.GetLocalAircraft(out local))
                return;

            var hud = SceneSingleton<CombatHUD>.i;
            var ac = hud != null ? hud.aircraft : null;
            if (ac == null || ac != local || cam.followingUnit != local)
            {
                _prevLongGValid = false;
                return;
            }

            Vector3 forward = ac.transform.forward;
            Vector3 a = ac.accel;
            float longG = Vector3.Dot(a, forward);
            Vector3 lateral = a - forward * longG;
            float maneuverG = lateral.magnitude;

            float dt = Mathf.Max(Time.fixedDeltaTime, 1e-5f);
            float longJerk = 0f;
            if (_prevLongGValid)
                longJerk = (longG - _prevLongG) / dt;
            _prevLongG = longG;
            _prevLongGValid = true;

            float smoothT = Mathf.Max(0f, GlocCameraPlugin.ShakeManeuverSmoothTimeSec.Value);
            if (smoothT > 1e-4f)
            {
                _smoothedManeuverG = Mathf.SmoothDamp(
                    _smoothedManeuverG,
                    maneuverG,
                    ref _maneuverSmoothVel,
                    smoothT,
                    Mathf.Infinity,
                    dt);
            }
            else
            {
                _smoothedManeuverG = maneuverG;
            }

            float dzM = GlocCameraPlugin.ShakeDeadZoneManeuverG.Value;
            float dzJ = GlocCameraPlugin.ShakeDeadZoneLongJerk.Value;
            float maneuverUse = Mathf.Max(0f, _smoothedManeuverG - dzM);
            float jerkUse = Mathf.Abs(longJerk) > dzJ ? Mathf.Abs(longJerk) : 0f;

            float trackMul = PlayerSettings.useTrackIR
                ? Mathf.Clamp01(GlocCameraPlugin.ShakeTrackIRScale.Value)
                : 1f;
            float lowRaw = jerkUse * GlocCameraPlugin.ShakeLongJerkScale.Value * trackMul;
            float highRaw = maneuverUse * GlocCameraPlugin.ShakeManeuverScale.Value * trackMul;

            // Event shake: stall / overspeed / vertical G / VRS.
            float stallSeverity = 0f;
            if (GlocCameraPlugin.ShakeStallEnabled.Value)
            {
                // Match the game's own AoA feedback gating (AoAFeedback.RunAoAFeedback):
                // stall shakes should not trigger at speed ~= 0 on the runway.
                var aoaEffects = ac.GetAircraftParameters().AoAEffects;
                if (aoaEffects != null && ac.cockpit != null && ac.cockpit.rb != null && ac.cockpit.xform != null)
                {
                    // Local velocity in cockpit space, same angle convention as AoAFeedback.
                    Vector3 vLocal = ac.cockpit.xform.InverseTransformDirection(ac.cockpit.rb.velocity);
                    float aoaDeg = Mathf.Atan2(vLocal.y, vLocal.z) * 57.29578f;

                    float denomSpeed = Mathf.Max(1e-4f, aoaEffects.FullVolumeSpeed - aoaEffects.OnsetSpeed);
                    float denomAlpha = Mathf.Max(1e-4f, aoaEffects.FullVolumeAlpha - aoaEffects.OnsetAlpha);

                    float numSpeed = Mathf.Max(ac.speed - aoaEffects.OnsetSpeed, 0f) / denomSpeed;
                    float numAlpha = Mathf.Max(Mathf.Abs(aoaDeg) - aoaEffects.OnsetAlpha, 0f) / denomAlpha;
                    stallSeverity = Mathf.Clamp01(numSpeed) * Mathf.Clamp01(numAlpha);
                }
            }

            float speedScale = Mathf.Max(0.01f, GlocCameraPlugin.ShakeSpeedReadingScale.Value);
            float speedKmh = ac.speed * 3.6f * speedScale;

            float overspeedSeverity = 0f;
            if (GlocCameraPlugin.ShakeOverspeedEnabled.Value)
            {
                float maxInfo = ac.definition?.aircraftInfo?.maxSpeed ?? 0f;
                float maxParams = 0f;
                var ap = ac.GetAircraftParameters();
                if (ap != null)
                    maxParams = ap.maxSpeed;
                float maxSpeedKmh = Mathf.Max(maxInfo, maxParams);
                if (maxSpeedKmh > 1e-4f)
                {
                    float maxSpeedMs = maxSpeedKmh / 3.6f;
                    float speedMs = ac.speed * speedScale;
                    if (GlocCameraPlugin.ShakeOverspeedUseApproximatedIas.Value)
                    {
                        float rho = ac.GetAirDensity();
                        float rho0 = Mathf.Max(1e-6f, GlocCameraPlugin.ShakeOverspeedIasReferenceDensity.Value);
                        speedMs *= Mathf.Sqrt(Mathf.Max(1e-6f, rho) / rho0);
                    }

                    float ratio = maxSpeedMs > 1e-4f ? speedMs / maxSpeedMs : 0f;
                    float start = Mathf.Max(0.01f, GlocCameraPlugin.ShakeOverspeedStartRatio.Value);
                    float full = Mathf.Max(0.01f, GlocCameraPlugin.ShakeOverspeedFullRatio.Value);
                    overspeedSeverity = Severity01FromRatio(ratio, start, full);
                }
            }

            float verticalSeverity = 0f;
            if (GlocCameraPlugin.ShakeVerticalGEnabled.Value)
            {
                float verticalG = Mathf.Abs(Vector3.Dot(a, ac.transform.up)) / 9.81f;
                float startG = Mathf.Max(0f, GlocCameraPlugin.ShakeVerticalGStart.Value);
                float fullG = Mathf.Max(startG + 1e-4f, GlocCameraPlugin.ShakeVerticalGFull.Value);
                verticalSeverity = Mathf.Clamp01((verticalG - startG) / (fullG - startG));
            }

            float vrsSeverity = 0f;
            if (GlocCameraPlugin.ShakeVrsEnabled.Value)
            {
                // Similar gating as VRSWarning: ignore near the ground / spawn offset area.
                float minAlt = ac.definition?.spawnOffset.y ?? float.NegativeInfinity;
                if (ac.radarAlt > minAlt + 1f)
                {
                    float vrsFactor = ComputeVrsFactor(ac);
                    float startF = Mathf.Max(0f, GlocCameraPlugin.ShakeVrsStartFactor.Value);
                    float fullF = Mathf.Max(startF + 1e-4f, GlocCameraPlugin.ShakeVrsFullFactor.Value);
                    vrsSeverity = Mathf.Clamp01((vrsFactor - startF) / (fullF - startF));
                }
            }

            if (GlocCameraPlugin.ShakeStallEnabled.Value)
                _stallSmoothed = Smooth01(_stallSmoothed, stallSeverity, ref _stallSmoothVel, GlocCameraPlugin.ShakeStallSmoothTimeSec.Value, dt);
            else
                _stallSmoothed = Mathf.SmoothDamp(_stallSmoothed, 0f, ref _stallSmoothVel, 0.04f, Mathf.Infinity, dt);

            if (GlocCameraPlugin.ShakeOverspeedEnabled.Value)
                _overspeedSmoothed = Smooth01(_overspeedSmoothed, overspeedSeverity, ref _overspeedSmoothVel, GlocCameraPlugin.ShakeOverspeedSmoothTimeSec.Value, dt);
            else
                _overspeedSmoothed = Mathf.SmoothDamp(_overspeedSmoothed, 0f, ref _overspeedSmoothVel, 0.04f, Mathf.Infinity, dt);

            if (GlocCameraPlugin.ShakeVerticalGEnabled.Value)
                _verticalSmoothed = Smooth01(_verticalSmoothed, verticalSeverity, ref _verticalSmoothVel, GlocCameraPlugin.ShakeVerticalSmoothTimeSec.Value, dt);
            else
                _verticalSmoothed = Mathf.SmoothDamp(_verticalSmoothed, 0f, ref _verticalSmoothVel, 0.04f, Mathf.Infinity, dt);

            if (GlocCameraPlugin.ShakeVrsEnabled.Value)
                _vrsSmoothed = Smooth01(_vrsSmoothed, vrsSeverity, ref _vrsSmoothVel, GlocCameraPlugin.ShakeVrsSmoothTimeSec.Value, dt);
            else
                _vrsSmoothed = Mathf.SmoothDamp(_vrsSmoothed, 0f, ref _vrsSmoothVel, 0.04f, Mathf.Infinity, dt);

            // Gear: only when deployed and above min indicated speed; blend high-frequency "vibration" into low-frequency "shake" as speed increases.
            if (GlocCameraPlugin.ShakeGearEnabled.Value)
            {
                float gearDownTarget = ac.gearDeployed ? 1f : 0f;
                _gearDownSmoothed = Smooth01(_gearDownSmoothed, gearDownTarget, ref _gearDownSmoothVel, GlocCameraPlugin.ShakeGearSmoothTimeSec.Value, dt);

                float minGk = GlocCameraPlugin.ShakeGearMinSpeedKmh.Value;
                float fullGk = Mathf.Max(minGk + 1f, GlocCameraPlugin.ShakeGearIntensityFullKmh.Value);
                float gearSpeedMagTarget = speedKmh < minGk
                    ? 0f
                    : Mathf.Clamp01((speedKmh - minGk) / (fullGk - minGk));

                _gearSpeedMagSmoothed = Smooth01(_gearSpeedMagSmoothed, gearSpeedMagTarget, ref _gearSpeedMagSmoothVel, GlocCameraPlugin.ShakeGearSmoothTimeSec.Value, dt);

                float vEnd = Mathf.Max(minGk, GlocCameraPlugin.ShakeGearBlendVibrationEndKmh.Value);
                float sEnd = Mathf.Max(vEnd + 1f, GlocCameraPlugin.ShakeGearBlendShakeEndKmh.Value);
                float gearBlendTarget = speedKmh <= vEnd ? 0f : (speedKmh >= sEnd ? 1f : Mathf.Clamp01((speedKmh - vEnd) / (sEnd - vEnd)));
                _gearBlendSmoothed = Smooth01(_gearBlendSmoothed, gearBlendTarget, ref _gearBlendSmoothVel, GlocCameraPlugin.ShakeGearSmoothTimeSec.Value, dt);

                float gearMag = _gearDownSmoothed * _gearSpeedMagSmoothed;
                float gearLow = Mathf.Lerp(
                    GlocCameraPlugin.ShakeGearVibrationLowMaxAdd.Value,
                    GlocCameraPlugin.ShakeGearShakeLowMaxAdd.Value,
                    _gearBlendSmoothed);
                float gearHigh = Mathf.Lerp(
                    GlocCameraPlugin.ShakeGearVibrationHighMaxAdd.Value,
                    GlocCameraPlugin.ShakeGearShakeHighMaxAdd.Value,
                    _gearBlendSmoothed);

                lowRaw += gearMag * gearLow * trackMul;
                highRaw += gearMag * gearHigh * trackMul;
            }
            else
            {
                _gearDownSmoothed = Mathf.SmoothDamp(_gearDownSmoothed, 0f, ref _gearDownSmoothVel, 0.04f, Mathf.Infinity, dt);
                _gearSpeedMagSmoothed = Mathf.SmoothDamp(_gearSpeedMagSmoothed, 0f, ref _gearSpeedMagSmoothVel, 0.04f, Mathf.Infinity, dt);
                _gearBlendSmoothed = Mathf.SmoothDamp(_gearBlendSmoothed, 0f, ref _gearBlendSmoothVel, 0.04f, Mathf.Infinity, dt);
            }

            // Airbrakes / spoilers (vanilla Airbrake): scaled by deployment (openAmount) and speed; same vibration↔shake blend.
            if (GlocCameraPlugin.ShakeAirbrakeEnabled.Value && AirbrakeOpenAmountField != null)
            {
                float maxOpen = GetMaxAirbrakeOpen(ac);
                float thr = Mathf.Clamp01(GlocCameraPlugin.ShakeAirbrakeOpenThreshold.Value);
                float openEff = maxOpen <= thr ? 0f : Mathf.Clamp01((maxOpen - thr) / Mathf.Max(1e-4f, 1f - thr));

                float minAk = GlocCameraPlugin.ShakeAirbrakeMinSpeedKmh.Value;
                float fullAk = Mathf.Max(minAk + 1f, GlocCameraPlugin.ShakeAirbrakeIntensityFullKmh.Value);
                float airMagTarget = openEff <= 1e-4f || speedKmh < minAk
                    ? 0f
                    : openEff * Mathf.Clamp01((speedKmh - minAk) / (fullAk - minAk));

                _airbrakeMagSmoothed = Smooth01(_airbrakeMagSmoothed, airMagTarget, ref _airbrakeMagSmoothVel, GlocCameraPlugin.ShakeAirbrakeSmoothTimeSec.Value, dt);

                float avEnd = Mathf.Max(minAk, GlocCameraPlugin.ShakeAirbrakeBlendVibrationEndKmh.Value);
                float asEnd = Mathf.Max(avEnd + 1f, GlocCameraPlugin.ShakeAirbrakeBlendShakeEndKmh.Value);
                float airBlendTarget = speedKmh <= avEnd ? 0f : (speedKmh >= asEnd ? 1f : Mathf.Clamp01((speedKmh - avEnd) / (asEnd - avEnd)));
                _airbrakeBlendSmoothed = Smooth01(_airbrakeBlendSmoothed, airBlendTarget, ref _airbrakeBlendSmoothVel, GlocCameraPlugin.ShakeAirbrakeSmoothTimeSec.Value, dt);

                float airLow = Mathf.Lerp(
                    GlocCameraPlugin.ShakeAirbrakeVibrationLowMaxAdd.Value,
                    GlocCameraPlugin.ShakeAirbrakeShakeLowMaxAdd.Value,
                    _airbrakeBlendSmoothed);
                float airHigh = Mathf.Lerp(
                    GlocCameraPlugin.ShakeAirbrakeVibrationHighMaxAdd.Value,
                    GlocCameraPlugin.ShakeAirbrakeShakeHighMaxAdd.Value,
                    _airbrakeBlendSmoothed);

                lowRaw += _airbrakeMagSmoothed * airLow * trackMul;
                highRaw += _airbrakeMagSmoothed * airHigh * trackMul;
            }
            else
            {
                _airbrakeMagSmoothed = Mathf.SmoothDamp(_airbrakeMagSmoothed, 0f, ref _airbrakeMagSmoothVel, 0.04f, Mathf.Infinity, dt);
                _airbrakeBlendSmoothed = Mathf.SmoothDamp(_airbrakeBlendSmoothed, 0f, ref _airbrakeBlendSmoothVel, 0.04f, Mathf.Infinity, dt);
            }

            bool wasAirborne = _wasAirborne;
            bool airborneNow = IsAirborneByRadarAlt(ac);
            bool onRunwayStrip = TryIsOnRunwayStrip(ac);

            // Touchdown bump: airborne → on ground with gear, after we've been airborne at least once (no spawn false trigger).
            if (GlocCameraPlugin.ShakeTouchdownEnabled.Value
                && _touchdownArmReady
                && wasAirborne
                && !airborneNow
                && ac.gearDeployed)
            {
                Vector3 velWorld = GetCockpitVelocityWorld(ac);
                float vsDown = Mathf.Max(0f, -Vector3.Dot(velWorld, Vector3.up));
                float minVs = Mathf.Max(0f, GlocCameraPlugin.ShakeTouchdownMinVsMps.Value);
                float vsFull = Mathf.Max(minVs + 1e-4f, GlocCameraPlugin.ShakeTouchdownVsFullMps.Value);
                float spdKmh = speedKmh;
                float spdFull = Mathf.Max(1f, GlocCameraPlugin.ShakeTouchdownSpeedFullKmh.Value);
                if (vsDown >= minVs)
                {
                    float vs01 = Mathf.Clamp01((vsDown - minVs) / (vsFull - minVs));
                    float sp01 = Mathf.Clamp01(spdKmh / spdFull);
                    // Softer than linear in both sink and speed; still 0 when either is ~0.
                    float combined = Mathf.Sqrt(Mathf.Max(1e-8f, vs01 * Mathf.Lerp(0.38f, 1f, sp01)));
                    float touch01 = Mathf.Clamp01(combined * 0.72f);
                    _touchdownBumpSmoothed = Mathf.Max(_touchdownBumpSmoothed, touch01);
                }
            }

            if (!airborneNow && ac.gearDeployed)
            {
                if (GlocCameraPlugin.ShakeRunwayRollEnabled.Value && onRunwayStrip)
                {
                    float rMin = GlocCameraPlugin.ShakeRunwayMinSpeedKmh.Value;
                    float rFull = Mathf.Max(rMin + 1f, GlocCameraPlugin.ShakeRunwayFullSpeedKmh.Value);
                    float rLinear = speedKmh <= rMin ? 0f : Mathf.Clamp01((speedKmh - rMin) / (rFull - rMin));
                    float rPow = rLinear <= 0f ? 0f : Mathf.Pow(rLinear, 1.32f);
                    float vStart = Mathf.Max(1f, GlocCameraPlugin.ShakeRunwayVibrationFocusStartKmh.Value);
                    float slowScale = speedKmh < vStart ? Mathf.Clamp01(speedKmh / vStart) : 1f;
                    float rTarget = rPow * slowScale;
                    _runwayRollSmoothed = Smooth01(_runwayRollSmoothed, rTarget, ref _runwayRollSmoothVel, GlocCameraPlugin.ShakeRunwaySmoothTimeSec.Value, dt);

                    float vEnd = Mathf.Max(0f, GlocCameraPlugin.ShakeRunwayBlendVibrationEndKmh.Value);
                    float sEnd = Mathf.Max(vEnd + 1f, GlocCameraPlugin.ShakeRunwayBlendShakeEndKmh.Value);
                    float blendTarget = speedKmh <= vEnd ? 0f : (speedKmh >= sEnd ? 1f : Mathf.Clamp01((speedKmh - vEnd) / (sEnd - vEnd)));
                    _runwayRollBlendSmoothed = Smooth01(_runwayRollBlendSmoothed, blendTarget, ref _runwayRollBlendSmoothVel, GlocCameraPlugin.ShakeRunwaySmoothTimeSec.Value, dt);
                }
                else
                {
                    _runwayRollSmoothed = Mathf.SmoothDamp(_runwayRollSmoothed, 0f, ref _runwayRollSmoothVel, 0.05f, Mathf.Infinity, dt);
                    _runwayRollBlendSmoothed = Mathf.SmoothDamp(_runwayRollBlendSmoothed, 0f, ref _runwayRollBlendSmoothVel, 0.05f, Mathf.Infinity, dt);
                }

                if (GlocCameraPlugin.ShakeGroundRollEnabled.Value && !onRunwayStrip)
                {
                    float gMin = GlocCameraPlugin.ShakeGroundMinSpeedKmh.Value;
                    float gFull = Mathf.Max(gMin + 1f, GlocCameraPlugin.ShakeGroundFullSpeedKmh.Value);
                    float gLinear = speedKmh <= gMin ? 0f : Mathf.Clamp01((speedKmh - gMin) / (gFull - gMin));
                    float gPow = gLinear <= 0f ? 0f : Mathf.Pow(gLinear, 1.22f);
                    float gvStart = Mathf.Max(1f, GlocCameraPlugin.ShakeGroundVibrationFocusStartKmh.Value);
                    float gSlowScale = speedKmh < gvStart ? Mathf.Clamp01(speedKmh / gvStart) : 1f;
                    float gTarget = gPow * gSlowScale;
                    _groundRollSmoothed = Smooth01(_groundRollSmoothed, gTarget, ref _groundRollSmoothVel, GlocCameraPlugin.ShakeGroundSmoothTimeSec.Value, dt);

                    float gvEnd = Mathf.Max(0f, GlocCameraPlugin.ShakeGroundBlendVibrationEndKmh.Value);
                    float gsEnd = Mathf.Max(gvEnd + 1f, GlocCameraPlugin.ShakeGroundBlendShakeEndKmh.Value);
                    float gBlendTarget = speedKmh <= gvEnd ? 0f : (speedKmh >= gsEnd ? 1f : Mathf.Clamp01((speedKmh - gvEnd) / (gsEnd - gvEnd)));
                    _groundRollBlendSmoothed = Smooth01(_groundRollBlendSmoothed, gBlendTarget, ref _groundRollBlendSmoothVel, GlocCameraPlugin.ShakeGroundSmoothTimeSec.Value, dt);
                }
                else
                {
                    _groundRollSmoothed = Mathf.SmoothDamp(_groundRollSmoothed, 0f, ref _groundRollSmoothVel, 0.05f, Mathf.Infinity, dt);
                    _groundRollBlendSmoothed = Mathf.SmoothDamp(_groundRollBlendSmoothed, 0f, ref _groundRollBlendSmoothVel, 0.05f, Mathf.Infinity, dt);
                }
            }
            else
            {
                _runwayRollSmoothed = Mathf.SmoothDamp(_runwayRollSmoothed, 0f, ref _runwayRollSmoothVel, 0.05f, Mathf.Infinity, dt);
                _runwayRollBlendSmoothed = Mathf.SmoothDamp(_runwayRollBlendSmoothed, 0f, ref _runwayRollBlendSmoothVel, 0.05f, Mathf.Infinity, dt);
                _groundRollSmoothed = Mathf.SmoothDamp(_groundRollSmoothed, 0f, ref _groundRollSmoothVel, 0.05f, Mathf.Infinity, dt);
                _groundRollBlendSmoothed = Mathf.SmoothDamp(_groundRollBlendSmoothed, 0f, ref _groundRollBlendSmoothVel, 0.05f, Mathf.Infinity, dt);
            }

            if (airborneNow)
                _touchdownArmReady = true;
            _wasAirborne = airborneNow;

            float touchdownForFrame = _touchdownBumpSmoothed;
            if (GlocCameraPlugin.ShakeTouchdownEnabled.Value)
                _touchdownBumpSmoothed = Mathf.SmoothDamp(_touchdownBumpSmoothed, 0f, ref _touchdownBumpSmoothVel, Mathf.Max(0.01f, GlocCameraPlugin.ShakeTouchdownDecayTimeSec.Value), Mathf.Infinity, dt);
            else
                _touchdownBumpSmoothed = Mathf.SmoothDamp(_touchdownBumpSmoothed, 0f, ref _touchdownBumpSmoothVel, 0.04f, Mathf.Infinity, dt);

            float rocketForFrame = _rocketHitSmoothed;
            // Incoming explosive / rocket hit shake
            if (GlocCameraPlugin.ShakeRocketHitEnabled.Value)
                _rocketHitSmoothed = Mathf.SmoothDamp(_rocketHitSmoothed, 0f, ref _rocketHitSmoothVel, Mathf.Max(0.01f, GlocCameraPlugin.ShakeRocketHitDecayTimeSec.Value), Mathf.Infinity, dt);
            else
                _rocketHitSmoothed = Mathf.SmoothDamp(_rocketHitSmoothed, 0f, ref _rocketHitSmoothVel, 0.04f, Mathf.Infinity, dt);

            AccumulateWeakPhaseShake(ref lowRaw, ref highRaw, touchdownForFrame, GlocCameraPlugin.ShakeTouchdownLowMaxAdd.Value, GlocCameraPlugin.ShakeTouchdownHighMaxAdd.Value, trackMul);

            float rwLow = Mathf.Lerp(
                GlocCameraPlugin.ShakeRunwayVibrationLowMaxAdd.Value,
                GlocCameraPlugin.ShakeRunwayShakeLowMaxAdd.Value,
                _runwayRollBlendSmoothed);
            float rwHigh = Mathf.Lerp(
                GlocCameraPlugin.ShakeRunwayVibrationHighMaxAdd.Value,
                GlocCameraPlugin.ShakeRunwayShakeHighMaxAdd.Value,
                _runwayRollBlendSmoothed);
            lowRaw += _runwayRollSmoothed * rwLow * trackMul;
            highRaw += _runwayRollSmoothed * rwHigh * trackMul;

            float grLow = Mathf.Lerp(
                GlocCameraPlugin.ShakeGroundVibrationLowMaxAdd.Value,
                GlocCameraPlugin.ShakeGroundShakeLowMaxAdd.Value,
                _groundRollBlendSmoothed);
            float grHigh = Mathf.Lerp(
                GlocCameraPlugin.ShakeGroundVibrationHighMaxAdd.Value,
                GlocCameraPlugin.ShakeGroundShakeHighMaxAdd.Value,
                _groundRollBlendSmoothed);
            lowRaw += _groundRollSmoothed * grLow * trackMul;
            highRaw += _groundRollSmoothed * grHigh * trackMul;

            AccumulateWeakPhaseShake(ref lowRaw, ref highRaw, rocketForFrame, GlocCameraPlugin.ShakeRocketHitLowMaxAdd.Value, GlocCameraPlugin.ShakeRocketHitHighMaxAdd.Value, trackMul);

            AccumulateWeakPhaseShake(ref lowRaw, ref highRaw, _stallSmoothed, GlocCameraPlugin.ShakeStallLowMaxAdd.Value, GlocCameraPlugin.ShakeStallHighMaxAdd.Value, trackMul);

            AccumulateWeakPhaseShake(ref lowRaw, ref highRaw, _overspeedSmoothed, GlocCameraPlugin.ShakeOverspeedLowMaxAdd.Value, GlocCameraPlugin.ShakeOverspeedHighMaxAdd.Value, trackMul);

            AccumulateWeakPhaseShake(ref lowRaw, ref highRaw, _verticalSmoothed, GlocCameraPlugin.ShakeVerticalLowMaxAdd.Value, GlocCameraPlugin.ShakeVerticalHighMaxAdd.Value, trackMul);

            AccumulateWeakPhaseShake(ref lowRaw, ref highRaw, _vrsSmoothed, GlocCameraPlugin.ShakeVrsLowMaxAdd.Value, GlocCameraPlugin.ShakeVrsHighMaxAdd.Value, trackMul);

            float lowAdd = Mathf.Min(GlocCameraPlugin.ShakeMaxLowShakeAdd.Value, Mathf.Max(0f, lowRaw));
            float highAdd = Mathf.Min(GlocCameraPlugin.ShakeMaxHighShakeAdd.Value, Mathf.Max(0f, highRaw));

            if (lowAdd < 1e-6f && highAdd < 1e-6f)
                return;

            cockpitState.AddShake(lowAdd, highAdd);
        }

        internal static void ReportRocketHit(Aircraft owner, DamageInfo damageInfo)
        {
            if (owner == null || !GlocCameraPlugin.ShakeRocketHitEnabled.Value || !damageInfo.IsValid())
                return;
            Aircraft local;
            if (!GameManager.GetLocalAircraft(out local) || local != owner)
                return;

            float blast = damageInfo.blastDamage.Decompress();
            float impact = damageInfo.impactDamage.Decompress();

            float minBlast = GlocCameraPlugin.ShakeRocketHitMinBlast.Value;
            float scale = Mathf.Max(1e-4f, GlocCameraPlugin.ShakeRocketHitDamageScale.Value);
            float impactScale = GlocCameraPlugin.ShakeRocketHitImpactScale.Value;

            float raw = Mathf.Max(0f, blast - minBlast) + impact * impactScale;
            float severity = Mathf.Clamp01(raw / scale);
            _rocketHitSmoothed = Mathf.Max(_rocketHitSmoothed, severity);
        }

        private static float Smooth01(float current, float target, ref float smoothVel, float smoothTimeSec, float dt)
        {
            if (smoothTimeSec <= 1e-4f)
                return target;
            return Mathf.SmoothDamp(current, target, ref smoothVel, smoothTimeSec, Mathf.Infinity, dt);
        }

        /// <summary>
        /// Low severity → vibration-like (small low-freq, boosted high-freq); at severity 1 matches configured caps.
        /// </summary>
        private static void AccumulateWeakPhaseShake(ref float lowRaw, ref float highRaw, float severity01, float capLow, float capHigh, float trackMul)
        {
            if (severity01 < 1e-7f)
                return;

            float exp = Mathf.Max(0.2f, GlocCameraPlugin.ShakeWeakPhaseSeverityExponent.Value);
            float blend = Mathf.Pow(Mathf.Clamp01(severity01), exp);
            const float VibLowFactor = 0.2f;
            const float VibHighFactor = 2.0f;
            float vLow = capLow * VibLowFactor;
            float vHigh = capHigh * VibHighFactor;
            float lo = Mathf.Lerp(vLow, capLow, blend) * severity01;
            float hi = Mathf.Lerp(vHigh, capHigh, blend) * severity01;
            lowRaw += lo * trackMul;
            highRaw += hi * trackMul;
        }

        /// <summary>Matches <see cref="Aircraft.CheckRadarAlt"/> airborne gate (field is private on <see cref="Aircraft"/>).</summary>
        private static bool IsAirborneByRadarAlt(Aircraft ac)
        {
            return ac != null && ac.radarAlt > 0.2f;
        }

        private static Vector3 GetCockpitVelocityWorld(Aircraft ac)
        {
            if (ac?.cockpit != null && ac.cockpit.rb != null)
                return ac.cockpit.rb.velocity;
            if (ac?.rb != null)
                return ac.rb.velocity;
            return Vector3.zero;
        }

        private static bool TryIsOnRunwayStrip(Aircraft ac)
        {
            if (ac == null || ac.NetworkHQ == null)
                return false;
            if (!ac.NetworkHQ.AnyNearAirbase(ac.transform.position, out Airbase airbase))
                return false;
            return airbase.AircraftIsOnRunway(ac, false, out _);
        }

        private static float Severity01FromRatio(float ratio, float startRatio, float fullRatio)
        {
            if (fullRatio <= startRatio)
                return ratio >= fullRatio ? 1f : 0f;
            if (ratio <= startRatio)
                return 0f;
            if (ratio >= fullRatio)
                return 1f;
            return Mathf.Clamp01((ratio - startRatio) / (fullRatio - startRatio));
        }

        private static float GetMaxAirbrakeOpen(Aircraft ac)
        {
            if (ac == null || ac.partLookup == null || AirbrakeOpenAmountField == null)
                return 0f;

            float max = 0f;
            for (int i = 0; i < ac.partLookup.Count; i++)
            {
                var part = ac.partLookup[i];
                if (part == null)
                    continue;
                var ab = part.GetComponent<Airbrake>();
                if (ab == null)
                    continue;
                object boxed = AirbrakeOpenAmountField.GetValue(ab);
                if (boxed is float o)
                    max = Mathf.Max(max, o);
            }

            return max;
        }

        private static float ComputeVrsFactor(Aircraft ac)
        {
            if (ac == null)
                return 0f;

            if (_vrsAircraft != ac || _vrsSources == null)
            {
                _vrsAircraft = ac;
                _vrsSources = new List<RotorShaft>(8);
                foreach (var part in ac.partLookup)
                {
                    if (part == null)
                        continue;
                    var rs = part.GetComponent<RotorShaft>();
                    if (rs != null)
                        _vrsSources.Add(rs);
                }
            }

            if (_vrsSources == null || _vrsSources.Count == 0)
                return 0f;

            float sum = 0f;
            int count = 0;
            for (int i = 0; i < _vrsSources.Count; i++)
            {
                var rs = _vrsSources[i];
                if (rs == null)
                    continue;
                sum += rs.GetVRSFactor();
                count++;
            }

            if (count <= 0)
                return 0f;
            return sum / count;
        }
    }
}

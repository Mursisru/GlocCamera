**Developer:** Mursisru

# G-LOC Camera (GlocCamera_Engine)

[![Nuclear Option](https://img.shields.io/badge/Game-Nuclear%20Option-blue)](https://store.steampowered.com/app/2168680/Nuclear_Option/)
[![BepInEx 5](https://img.shields.io/badge/Loader-BepInEx%205-orange)](https://docs.bepinex.dev/)
[![Version](https://img.shields.io/badge/Version-2.2.2-green)](https://github.com/Mursisru/GlocCamera/releases/tag/v2.2.2)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow)](https://github.com/Mursisru/GlocCamera/blob/main/LICENSE)


BepInEx 5 plugin for **Nuclear Option**: in **cockpit**, smooth **FOV** and optional **camera dolly** from **throttle-style acceleration** only — the projection `Vector3.Dot(aircraft.accel, aircraft.transform.forward)` (G along the nose). **Turn/pull G is not used** for FOV so it does not stack with the game’s own G effects.

Optional **extra cockpit shake** uses the same longitudinal signal plus **lateral maneuver G**, fed into vanilla `CameraCockpitState.AddShake` (low/high frequency) so the stock shake pipeline and rattle stay coherent.

Optional **atmosphere** tweaks **URP Bloom** and **post-exposure** on the game’s main post `Volume`, blended stronger toward **night** (sun direction heuristic), with values **restored** when you leave the cockpit or unload the plugin.

The mod adds an offset on top of the game’s desired FOV (settings + zoom axis). Final easing uses a configurable per-frame lerp (default **0.11**, softer than vanilla **~0.2**).

**Current version:** **2.2.2** (see `GlocCameraPlugin.PluginVersion`, [CHANGELOG.md](CHANGELOG.md)).

---

## Critical warnings

> [!IMPORTANT]
> **BepInEx 5 (x64) required** - install [BepInEx](https://docs.bepinex.dev/articles/user_guide/installation/index.html) before this mod.

> [!NOTE]
> **Cockpit-only effect** - FOV/dolly/shake apply in cockpit view; atmosphere tweaks restore on exit/unload.

> [!NOTE]
> **Uses longitudinal G only for FOV** - turn/pull G is intentionally excluded to avoid stacking with vanilla G effects.

> [!WARNING]
> **URP assembly paths** - project references `Unity.RenderPipelines.*` from game `Managed\`; update `.csproj` HintPaths if your game build renames assemblies.

## Download

- **Releases:** prebuilt `GlocCamera_Engine_v*.zip` on the repo’s **Releases** page (after you publish).  
- Or build from source (below) and copy `GlocCamera_Engine.dll` into `BepInEx\plugins\`.

## Requirements

- **Nuclear Option** (Steam)
- **BepInEx 5** x64

## Install

> [!IMPORTANT]
> **BepInEx 5 (x64) required** - install [BepInEx](https://docs.bepinex.dev/) before this mod.

1. From a release zip: extract `GlocCamera_Engine.dll` into `Nuclear Option\BepInEx\plugins\` (see `release\INSTALL.txt`).
2. From source: build **Release** and copy `GlocCamera_Engine\bin\Release\GlocCamera_Engine.dll` to `BepInEx\plugins\`.

## Build (Visual Studio / MSBuild)

1. If **Nuclear Option** is not under the default Steam path, copy `Directory.Build.user.props.example` to **`Directory.Build.user.props`** in the repo root and set `<NuclearOptionRoot>` to your install folder (same pattern as `TacticalMapLayers` / other mods). Otherwise MSBuild uses the default in `Directory.Build.props`.
2. Open `GlocCamera_Engine.slnx` or `GlocCamera_Engine\GlocCamera_Engine.csproj`, build **Release**.
3. Output: `GlocCamera_Engine\bin\Release\GlocCamera_Engine.dll`.

The project references **`Unity.RenderPipelines.Core.Runtime`** and **`Unity.RenderPipelines.Universal.Runtime`** from `$(NuclearOptionRoot)\NuclearOption_Data\Managed\` for `Volume` / Bloom types. If your game build renames those assemblies, update the `HintPath` entries in `GlocCamera_Engine.csproj`.

### Maintainer: release zip

From repo root (after a Release build):

```powershell
.\scripts\package-release.ps1
```

Produces `release\GlocCamera_Engine_v<version>.zip`. Full publish checklist: [GITHUB_PUBLISH.md](GITHUB_PUBLISH.md).

## Configuration

Generated at `BepInEx\config\com.at747.gloccamera.cfg` after first run.

| Section | Key | Notes |
|--------|-----|--------|
| General | `Enabled` | Master toggle. |
| FOV | `DegreesPerLongitudinalG` | **Positive** (default **5**): wider FOV when accelerating along the nose; braking along the nose narrows. Tuned ~**2×** moderate (not the old “×10” preset). |
| FOV | `DeadZoneLongitudinalG` | Ignores tiny longitudinal noise. |
| FOV | `MaxDeltaDegrees` | Cap on smoothed FOV offset. |
| FOV | `SmoothTimeSeconds` / `SmoothMaxDegreesPerSec` | **SmoothDamp** time and optional °/s cap (0 = no cap). |
| FOV | `CockpitLerpBlend` | Per-frame lerp toward target (vanilla **~0.2**). Default **0.11** = smoother overall FOV / zoom axis feel. |
| Dolly | `MetersPerLongitudinalG` / `MaxMeters` | Local **Z** as **−(longG × scale)**. **Disabled** when TrackIR is on. |
| Dolly | `SmoothTimeSeconds` / `SmoothMaxMetersPerSec` | **SmoothDamp** for dolly; 0 = no speed cap. |
| Shake | `Enabled` | Extra shake via vanilla `AddShake` (cockpit, local aircraft, controls on, pilot alive). |
| Shake | `ManeuverScale`, `LongJerkScale`, `MaxLowShakeAdd`, `MaxHighShakeAdd` | Caps and scales for maneuver vs longitudinal jerk channels. |
| Shake | `WeakPhaseSeverityExponent` | Stall / overspeed / vertical G / VRS / rocket / touchdown: **low severity** → more **vibration** (high-frequency bias); **severity 1** → your normal low/high caps. |
| Shake | `DeadZoneManeuverG`, `DeadZoneLongJerk` | Ignores small lateral G and small \|Δ(longG)\|/s. |
| Shake | `TrackIRScale` | Multiplier when TrackIR is on (0 = disable extra shake). |
| Shake | `ManeuverSmoothTimeSeconds` | **SmoothDamp** on lateral maneuver magnitude (0 = off). |
| Shake | `StallEnabled` / `StallStartRatio` / `StallFullRatio` | Stall severity uses the same AoA gating as the game’s `AoAFeedback` (OnsetSpeed + OnsetAlpha). `StallStartRatio/StallFullRatio` are kept for backward compatibility but currently not used. |
| Shake | `StallLowMaxAdd` / `StallHighMaxAdd` | Max extra low/high shake at full stall severity. |
| Shake | `StallSmoothTimeSeconds` | **SmoothDamp** for stall severity (0 = no smoothing). |
| Shake | `OverspeedEnabled` / `OverspeedStartRatio` / `OverspeedFullRatio` | Overspeed severity uses `speed/maxSpeed` with **`Max(aircraftInfo.maxSpeed, aircraftParameters.maxSpeed)`** (km/h→m/s). Defaults **1.06 / 1.20**; older cfg keeps your numbers. |
| Shake | `SpeedReadingScale` | Multiplier on **game speed** for km/h-based shake (gear, airbrake, touchdown, runway, ground) and the **overspeed ratio numerator** (1 = unchanged). |
| Shake | `OverspeedUseApproximatedIas` / `OverspeedIasReferenceDensity` | If enabled, overspeed numerator uses **speed × √(ρ/ρ₀)** (`GetAirDensity()` vs reference kg/m³, default **1.225**). Rough dynamic-pressure hint, not full IAS. |
| Shake | `OverspeedLowMaxAdd` / `OverspeedHighMaxAdd` | Max extra low/high shake at full overspeed severity. |
| Shake | `OverspeedSmoothTimeSeconds` | **SmoothDamp** for overspeed severity (0 = no smoothing). |
| Shake | `VerticalGEnabled` / `VerticalGStart` / `VerticalGFull` | Vertical shake uses `|Dot(accel, up)| / 9.81` (vertical G). |
| Shake | `VerticalLowMaxAdd` / `VerticalHighMaxAdd` | Max extra low/high shake at full vertical-G severity. |
| Shake | `VerticalSmoothTimeSeconds` | **SmoothDamp** for vertical-G severity (0 = no smoothing). |
| Shake | `VrsEnabled` / `VrsStartFactor` / `VrsFullFactor` | Helicopter vortex ring uses averaged `RotorShaft.GetVRSFactor()`; gated above ground (radarAlt). |
| Shake | `VrsLowMaxAdd` / `VrsHighMaxAdd` | Max extra low/high shake at full VRS severity. |
| Shake | `VrsSmoothTimeSeconds` | **SmoothDamp** for VRS severity (0 = no smoothing). |
| Shake | `GearShakeEnabled` | Gear-driven cockpit effect only when **gear is down** and **indicated speed ≥ GearMinSpeedKmh** (default **500**), so runway/spawn stays calm. |
| Shake | `GearMinSpeedKmh` / `GearIntensityFullKmh` | Magnitude ramps from 0 at min speed to full by `GearIntensityFullKmh` (km/h; game speed is converted from m/s). |
| Shake | `GearBlendVibrationEndKmh` / `GearBlendShakeEndKmh` | Between these speeds the effect **lerps** from mostly high-frequency vibration to mostly low-frequency rumble (`SmoothDamp` on the blend). |
| Shake | `GearVibrationLowMaxAdd` / `GearVibrationHighMaxAdd` / `GearShakeLowMaxAdd` / `GearShakeHighMaxAdd` | Caps on vanilla `AddShake` at the two ends of that blend. |
| Shake | `GearSmoothTimeSeconds` | Smoothing for gear **magnitude** and **vibration↔shake** blend. |
| Shake | `AirbrakeShakeEnabled` | Same idea for **vanilla `Airbrake`** (spoilers / speedbrake when throttle at 0), scaled by **openAmount** and speed. |
| Shake | `AirbrakeMinSpeedKmh` / `AirbrakeOpenThreshold` / `AirbrakeIntensityFullKmh` | Gated so parked **idle throttle** (brakes open) does not shake; `openAmount` above threshold contributes. |
| Shake | `AirbrakeBlendVibrationEndKmh` / `AirbrakeBlendShakeEndKmh` | Vibration→shake blend by indicated speed (km/h). |
| Shake | `AirbrakeVibrationLowMaxAdd` / `AirbrakeVibrationHighMaxAdd` / `AirbrakeShakeLowMaxAdd` / `AirbrakeShakeHighMaxAdd` | `AddShake` caps for airbrake blend ends. |
| Shake | `AirbrakeSmoothTimeSeconds` | Smoothing for airbrake magnitude and blend. |
| Shake | `TouchdownShakeEnabled` / `TouchdownMinVsMps` / `TouchdownVsFullMps` / `TouchdownSpeedFullKmh` | One-shot jolt when **gear** meets the ground after you were **airborne** (`radarAlt > 0.2 m`); strength from **sink rate** (world up) and **ground speed** (`aircraft.speed`×3.6 as km/h). |
| Shake | `TouchdownLowMaxAdd` / `TouchdownHighMaxAdd` / `TouchdownDecayTimeSeconds` | `AddShake` caps and decay for touchdown. |
| Shake | `RunwayRollShakeEnabled` / `RunwayRollMinSpeedKmh` / `RunwayRollFullSpeedKmh` / `RunwayRollVibrationFocusStartKmh` | Runway rumble magnitude (gear down, **`AircraftIsOnRunway`**). Below **VibrationFocusStartKmh** (default **100**) magnitude scales with speed so slow taxi is light. |
| Shake | `RunwayRollVibrationLowMaxAdd` / `RunwayRollVibrationHighMaxAdd` / `RunwayRollShakeLowMaxAdd` / `RunwayRollShakeHighMaxAdd` | **Vibration** caps (mostly high freq) vs **shake** caps (mostly low freq). |
| Shake | `RunwayRollBlendVibrationEndKmh` / `RunwayRollBlendShakeEndKmh` / `RunwayRollSmoothTimeSeconds` | Up to **350** km/h (default) blend stays on vibration; above, **SmoothDamp** blend lerps toward shake by **BlendShakeEndKmh** (default **520**). |
| Shake | `GroundRoll*` | Same pattern off-runway: **`GroundRollVibrationFocusStartKmh`**, vibration/shake quad caps, **`GroundRollBlendVibrationEndKmh`** / **`GroundRollBlendShakeEndKmh`**, smoothing. |
| Shake | `RocketHitShakeEnabled` / `RocketHitLowMaxAdd` / `RocketHitHighMaxAdd` | Extra strong jolt on incoming explosive/rocket hits (local aircraft). |
| Shake | `RocketHitMinBlast` / `RocketHitDamageScale` / `RocketHitImpactScale` | How damageInfo blast/impact maps into severity. |
| Shake | `RocketHitDecayTimeSeconds` | Decay time for rocket-hit shake. |
| Atmosphere | `Enabled` | Cockpit-only bloom / post-exposure night blend; restores on exit. |
| Atmosphere | `LerpSpeed` | How fast driven values approach day/night targets. |
| Atmosphere | `BloomIntensityAddDay` / `BloomIntensityAddNight` | Added on top of values **snapshotted** when entering cockpit. |
| Atmosphere | `BloomThresholdAddDay` / `BloomThresholdAddNight` | Adds to `Bloom.threshold` (positive = less bloom, negative = more bloom; helps small instrument lights at night). |
| Atmosphere | `PostExposureAddDay` / `PostExposureAddNight` | Same for color grading post-exposure (EV-style). |
| Atmosphere | `SunDotDay`, `SunDotNight` | `Dot(-sun.forward, up)` range for day vs night (see in-game sun). |
| Atmosphere | `NightBlendNoSun` | Night blend when `RenderSettings.sun` is missing. |
| Lighting | `Enabled` | Cockpit-only fill spot + external light multipliers; restored on exit (same lifecycle as atmosphere). |
| Lighting | `FillToggleHotkey` / `FillWideModeHotkey` | Defaults **`J`** = toggle fill **armed** (still needs night + `FillNightMin01`), **`K`** = **wide** vs normal cone (`FillSpotAngle*` vs `FillSpotAngleWide*`). Edit in cfg for other keys / modifiers. |
| Lighting | `FillSpotAngleWide` / `FillInnerSpotAngleWide` | Wide flashlight outer/inner cone (degrees) when **K** has toggled wide mode on. |
| Lighting | `FillNightMin01` / `FillEnabled` / `FillLocalPosition` / `FillPitchDegrees` / `FillYawDegrees` / `FillRollDegrees` / `FillIntensity` / … | Fill **spawns** on the **cockpit camera** using **camera-local** pos/euler, then **one-shot** reparent to **`cockpit.transform`** with **world pose preserved** — frozen on the panel; changing cfg does **not** move it until the fill is recreated (leave cockpit / swap aircraft / toggle Fill). |
| Lighting | `ExternalIntensityMul` / `ExternalRangeMul` / `ExternalNameSubstrings` | Boost lights whose hierarchy path contains a substring (default: **nav**, **beacon**, **strobe**, **wing**, **taxi**, **spot** — not gear/nose). Paths under **cockpit** are ignored **unless** they also match. |
| CockpitView | `OffsetLocalX` / `OffsetLocalY` / `OffsetLocalZ` | Additive **local** camera shift after vanilla cockpit placement. **Z** stacks with G-LOC dolly when TrackIR is off (`finalZ = dolly + OffsetLocalZ`). |
| CockpitView | `FovBiasDegrees` | Additive FOV after vanilla clamp + G-LOC longitudinal offset; clamped again to cockpit min/max. |
| CockpitView | `ApplyFramingWithTrackIR` | Default **false** (vanilla TrackIR pose unchanged). If **true**, offsets and FOV bias apply; **XYZ** are clamped like vanilla TrackIR limits. |
| ApexView | `Enabled` | Smooth cockpit **local X/Y/Z** from **pitch/roll/yaw** `ControlInputs` (TrackIR off). |
| ApexView | `MaxLateralMeters` / `MaxVerticalMeters` / `MaxDepthMeters` | Offset caps (defaults **0.085** / **0.045** / **0.03** m). |
| ApexView | `Lateral*Scale` / `Vertical*Scale` / `Depth*Scale` | Per-axis blend from stick inputs. |
| ApexView | `SmoothTimeSeconds` / `SmoothMaxMetersPerSec` | Defaults **0.42** / **0.12**. |
| ApexView | `AlsoApplyWithTrackIR` | Apply apex with TrackIR when CockpitView framing-with-TrackIR is on. |

Set `Dolly.MaxMeters` to `0` to disable dolly and keep FOV-only (offsets and FOV bias still apply unless zero).

Atmosphere does **not** replace the volume’s `VolumeProfile` asset instance (so other cockpit effects that hold references to grading components keep working). It snapshots Bloom / Color Adjustments at **cockpit enter** and restores those numbers when you leave.

After upgrading, delete stale cfg keys or regenerate **`com.at747.gloccamera.cfg`**. From **v1.2.0**, old **`SmoothTowardDegPerSec`** / **`SmoothAwayDegPerSec`** / dolly **`*MPerSec`** keys are unused (replaced by **`SmoothTimeSeconds`** + max-speed caps).

## Manual test checklist

1. **Cockpit:** Add throttle / accelerate along flight path — FOV should **open up** (then relax when accel drops). Hard turns without much along-nose accel should stay **subtle** vs. throttle.
2. **Shake (day):** Hard turns + rapid throttle/brake, plus event shakes:
   stall (AoA gating), overspeed, large vertical G, helicopter VRS, **gear only at high speed** (vibration→rumble), **extended airbrakes/spoilers**, **gear touchdown bump**, **runway taxi rumble**, **off-runway ground rumble**, and explosive/rocket hits (strong) — tune via `Shake.*`. Toggle `Shake.Enabled` off to compare.
3. **Atmosphere day:** With `Atmosphere.Enabled` on, bloom/exposure should match baseline at midday (adds default to **0** at full day).
4. **Atmosphere night:** Same mission at night — cockpit emissive lighting (instruments) should read more **“glow / lift”**; tweak `BloomIntensityAddNight`, `BloomThresholdAddNight` and `PostExposureAddNight` if needed.
5. **Orbit / external:** Leaving cockpit should **restore** post values, **cockpit lights**, and **decay** FOV/dolly; shake state resets.
6. **Settings FOV:** Change default FOV in game; the mod still adds its offset on top.
7. **Zoom axis:** Cockpit zoom (`Zoom View`) still works.
8. **Pause:** When `flightControlsEnabled` is false, FOV/dolly postfix and shake do not apply.
9. **TrackIR:** Dolly skipped; FOV offset still applies; extra shake scaled by `Shake.TrackIRScale`.
10. **Mod off:** `General.Enabled` false — no FOV/dolly/shake/atmosphere changes; atmosphere restores if it was active.
11. **Unload:** Remove plugin DLL or exit game — atmosphere restores in `OnDestroy`.
12. **ApexView:** Steep turns (60°+ bank) — head should **look into the turn** (HUD shifts opposite side); level flight returns to center. Compare with DCS reference if needed.

## Technical notes

- Patched methods: `CameraCockpitState.UpdateState` (FOV/dolly postfix), `CameraCockpitState.FixedUpdateState` (shake postfix), `CameraCockpitState.LeaveState` (atmosphere + shake reset).
- Postfix recomputes vanilla `Clamp(desiredFOV + FOVAdjustment, min, max)` via reflection, adds smoothed delta, then `Lerp(prefixFov, target, CockpitLerpBlend)`.
- FOV signal: `CombatHUD.aircraft` and `GameManager.GetLocalAircraft`; **only** `Dot(accel, forward)` — **not** total `gForce`.
- Shake: same gating; lateral `|accel − (accel·f)f|` and longitudinal jerk on `Dot(accel, f)`; plus severity-based events (stall, overspeed, large vertical G, helicopter VRS) with **weak-phase vibration bias** (`WeakPhaseSeverityExponent`), high-speed gear/airbrake blends, **touchdown** (same weak-phase split) and **runway vs ground taxi** rumble, and (Harmony) **rocket/explosive hit** (split + pre-decay sample) — all summed into vanilla `AddShake(low, high)`.
- Atmosphere: `CameraStateManager.GetPostProcessVolume()`; snapshot/restore on the **same** profile components the game already uses.
- Drivers: FOV/dolly + atmosphere in plugin `LateUpdate` (execution order **1**, before `CameraStateManager` at **2**).

## Disclaimer

Independent fan mod — **not** affiliated with or endorsed by the developers of **Nuclear Option**.

## License

[MIT](LICENSE). Attribution appreciated when redistributing or forking.

---

## Keywords

nuclear-option, bepinex, harmony, mod, gloccamera, csharp, unity

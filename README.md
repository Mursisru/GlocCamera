# G-LOC Camera (GlocCamera_Engine)

BepInEx 5 plugin for **Nuclear Option**: in **cockpit**, smooth **FOV** and optional **camera dolly** from **throttle-style acceleration** only — the projection `Vector3.Dot(aircraft.accel, aircraft.transform.forward)` (G along the nose). **Turn/pull G is not used** so it does not stack with the game’s own G effects.

The mod adds an offset on top of the game’s desired FOV (settings + zoom axis). Final easing uses a configurable per-frame lerp (default **0.11**, softer than vanilla **~0.2**).

**Current version:** **1.2.0** (see `GlocCameraPlugin.PluginVersion` and [CHANGELOG.md](CHANGELOG.md)).

## Download

- **Releases:** prebuilt `GlocCamera_Engine_v*.zip` on the repo’s **Releases** page (after you publish).  
- Or build from source (below) and copy `GlocCamera_Engine.dll` into `BepInEx\plugins\`.

## Requirements

- **Nuclear Option** (Steam)
- **BepInEx 5** x64

## Install

1. From a release zip: extract `GlocCamera_Engine.dll` into `Nuclear Option\BepInEx\plugins\` (see `release\INSTALL.txt`).
2. From source: build **Release** and copy `GlocCamera_Engine\bin\Release\GlocCamera_Engine.dll` to `BepInEx\plugins\`.

## Build (Visual Studio / MSBuild)

1. Adjust `HintPath` entries in `GlocCamera_Engine/GlocCamera_Engine.csproj` if your game is not under the default Steam path.
2. Open `GlocCamera_Engine.slnx` or the `.csproj`, build **Release**.
3. Output: `GlocCamera_Engine\bin\Release\GlocCamera_Engine.dll`.

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

Set `Dolly.MaxMeters` to `0` to disable dolly and keep FOV-only.

After upgrading, delete stale cfg keys or regenerate **`com.at747.gloccamera.cfg`**. From **v1.2.0**, old **`SmoothTowardDegPerSec`** / **`SmoothAwayDegPerSec`** / dolly **`*MPerSec`** keys are unused (replaced by **`SmoothTimeSeconds`** + max-speed caps).

## Manual test checklist

1. **Cockpit:** Add throttle / accelerate along flight path — FOV should **open up** (then relax when accel drops). Hard turns without much along-nose accel should stay **subtle** vs. throttle.
2. **Settings FOV:** Change default FOV in game; the mod still adds its offset on top.
3. **Zoom axis:** Cockpit zoom (`Zoom View`) still works.
4. **External / orbit:** Effect decays when leaving cockpit.
5. **Pause:** When `flightControlsEnabled` is false, postfix does not apply the offset.
6. **TrackIR:** Dolly skipped; FOV offset still applies.
7. **Unload:** Remove plugin or disable in config.

## Technical notes

- Patched method: `CameraCockpitState.UpdateState(CameraStateManager cam)`.
- Postfix recomputes vanilla `Clamp(desiredFOV + FOVAdjustment, min, max)` via reflection, adds smoothed delta, then `Lerp(prefixFov, target, CockpitLerpBlend)`.
- Signal: `CombatHUD.aircraft` and `GameManager.GetLocalAircraft`; **only** `Dot(accel, forward)` — **not** `gForce`.
- Driver runs in the plugin `LateUpdate` (execution order **1**, before `CameraStateManager` at **2**).

## Disclaimer

Independent fan mod — **not** affiliated with or endorsed by the developers of **Nuclear Option**.

## License

[MIT](LICENSE). Attribution appreciated when redistributing or forking.

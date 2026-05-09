# Changelog

## 1.2.0 — 2026-05-09

- **Strength:** Defaults tuned to about **2×** a moderate baseline (was ~10×): `DegreesPerLongitudinalG` **5**, `MaxDeltaDegrees` **40**, dolly scale/max scaled down accordingly.
- **Smoothing:** FOV and dolly now use **`SmoothDamp`** (`SmoothTimeSeconds` + optional max speed caps) instead of fixed °/s and m/s slew.
- **Cockpit FOV blend:** New **`CockpitLerpBlend`** (default **0.11**, vanilla **~0.2**) for slower per-frame easing toward the final FOV target.
- Removed config keys **`SmoothTowardDegPerSec`**, **`SmoothAwayDegPerSec`**, **`SmoothTowardMPerSec`**, **`SmoothAwayMPerSec`** — delete stale lines from `com.at747.gloccamera.cfg` or regenerate the file.

## 1.1.0 — 2026-05-09

- **Behavior:** Only **longitudinal** acceleration (`accel · forward`) drives the effect — **maneuver / total G removed** (game already handles that).
- **FOV:** Positive longitudinal G (throttle-on acceleration) → **wider FOV** (zoom out); braking along the nose → narrower. Default `DegreesPerLongitudinalG` is now **+24**.
- Config keys `DegreesPerExcessTotalG` and `DeadZoneExcessTotalG` removed; delete or trim them from old `com.at747.gloccamera.cfg` if present.

## 1.0.2 — 2026-05-09

- Defaults scaled **~10×** for FOV/dolly sensitivity; **`MaxDeltaDegrees` raised to 200** so the driver no longer clips the effect to almost nothing.
- Tighter dead zones and faster smoothing so the change reads immediately in the cockpit.

## 1.0.1 — 2026-05-09

- **Stronger defaults** so the effect is easier to see in flight: higher |°/G|, larger max FOV delta, faster attack, slightly smaller dead zones, more dolly range.
- Existing `com.at747.gloccamera.cfg` keeps your saved numbers; delete the file (or edit keys) if you want these new defaults.

## 1.0.0 — 2026-05-09

- Initial release for **Nuclear Option** (BepInEx 5).
- Cockpit-only Harmony patch on `CameraCockpitState.UpdateState`: adds smoothed FOV delta from `Aircraft.accel` / `gForce` without replacing menu baseline FOV.
- Optional forward dolly on `CameraStateManager.transform.localPosition.z` when TrackIR is off.
- Config under `BepInEx/config/com.at747.gloccamera.cfg`.

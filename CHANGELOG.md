# Changelog

## 2.2.2 — 2026-05-30

### Performance
- **GlocFrameContext**: one GetLocalAircraft / CombatHUD lookup per frame for all drivers (no behavior change).

Version numbers: **MAJOR.MINOR.PATCH**, each segment **0вЂ“9** (carry on overflow).

## 2.2.1 вЂ” 2026-05-30

### Changed
- **ApexView restored** to classic **ControlInputs** local X/Y/Z offset (same as GitHub **2.0.9**). Removed DCS/neck-inertia experiments (2.1.xвЂ“2.2.0). Delete `[ApexView]` in cfg to get default scales back.

## 2.2.0 вЂ” 2026-05-30

### Changed
- **ApexView rewritten** (custom neck inertia, DCS reference video вЂ” not View.lua port): bank from `SignedAngle` (no euler flip at inverted), **pan/tilt add only**, local roll **always 0** (fixes 180В° spins).
- Into-turn pan = `-IntoTurnGain Г— bank`; look-up tilt ramps with bank + optional pitch/G assist.

### Removed
- DCS formula port keys (`NaturalHead*`, `HeadRollShaking*`, `NyMove*`, `VerticalAngleSign`, `OmzMaxRadPerSec`, offset clamps). Delete `[ApexView]` in cfg or regenerate.

## 2.1.1 вЂ” 2026-05-30

### Fixed
- **ApexView pitch inverted:** DCS positive vertical angle now maps to **look up** in NO (`VerticalAngleSign` default **-1**), fixing вЂњstare at floor/instrumentsвЂќ in hard turns.
- **Rotation application:** apex angles are **added** to vanilla local euler (tilt/pan/roll), not multiplied as a child quaternion.
- **NyMove:** signed **Dot(accel, up)/9.81** (not `Abs`); **disabled by default**; only the apex offset is clamped, not the whole cockpit position.
- **head_roll_shaking:** counter-roll sign aligned with DCS (+foldedГ—gain).
- **omz:** clamped (`OmzMaxRadPerSec` default **0.35**) before the Г—45 term to limit vertical spikes.

## 2.1.0 вЂ” 2026-05-30

### Changed
- **ApexView v2 (DCS View.lua):** replaced stick-driven local offset with **NaturalHeadMoving** head inertia вЂ” horizontal from bank (`-0.25Г—roll` with >90В° fold), vertical from pitch + body yaw rate (`0.4Г—tang + 45Г—omz`, clamp 90В°), applied as **local rotation** after vanilla pan/tilt.
- **head_roll_shaking:** optional counter-roll compensation (gain **0.3**, max **30В°**).
- **CockpitNyMove:** optional local Y shift from normal G (`|Dot(accel, up)|/9.81`).
- **CockpitOffsetsMin/Max** clamp on final cockpit local position (DCS defaults В±0.2 / В±0.25 / В±0.5 m).
- Angle/position smoothing defaults **0.15 s** (DCS H/V accelerate time). Free-look attenuation when `|panView|+|tiltView|` is large.

### Removed
- Legacy ApexView **ControlInputs** offset scales (`Lateral*Scale`, `Depth*`, `*InputSign`, old `Max*Meters`). Regenerate or edit `[ApexView]` in `com.at747.gloccamera.cfg`.

## 2.0.9 вЂ” 2026-05-16

### Fixed
- **Nose gear lighting leftovers:** remove all legacy `GlocCamera_*` objects on the airframe (nose strut landing light, forward ground spot, old pilot fill, etc.) on bind, restore, and plugin load.
- **External light boost:** default `ExternalNameSubstrings` no longer includes **gear** / **nose** / **landing** (stops amplifying nose-strut lamps).

## 2.0.8 вЂ” 2026-05-16

### Changed
- **NVG darker / no brighten-in:** vanilla gain Г— **`PostExposureScale` (0.94)**, zero EV add by default, removed high `PostExposureMin` floor; ambient sampled on arm (fixes fade-in brighter).

## 2.0.7 вЂ” 2026-05-16

### Fixed
- **NVG 1 Hz pulse:** skip vanilla `UpdateGain` (1 s throttle); recompute the same gain curve **every frame** with smoothed ambient.

## 2.0.6 вЂ” 2026-05-16

### Fixed
- **NVG pulsing / flashes:** smooth exposure & bloom after vanilla 1 Hz auto-gain; apply overrides same frame as `UpdateGain`; smoother ambient; lower film grain default.

## 2.0.5 вЂ” 2026-05-16

### Changed
- **NVG realistic preset:** P43 **green phosphor** (`#88FFAA`), near-monochrome (`Saturation = -86`), tube **vignette**, light **halation** on bright sources, optional **film grain** scintillation. `AbsoluteGrade = true` by default.

## 2.0.4 вЂ” 2026-05-16

### Changed
- **NVG:** more **natural** look вЂ” near-neutral color filter, **no green bloom tint**, lower brightness (`PostExposureMax` 1.75, slight negative offset by day), gentler saturation/contrast.

## 2.0.3 вЂ” 2026-05-16

### Changed
- **NVG:** much less green вЂ” lower `ColorFilterStrength`, softer default tint (`#E2F2EA`), minimal `BloomTintBlend`.

## 2.0.2 вЂ” 2026-05-16

### Fixed
- **NVG white-out:** removed stacked post-exposure on every auto-gain tick; added **`PostExposureMax`** cap; much lower bloom/light defaults; scene light boost **off** by default (`BoostSceneLights = false`).

## 2.0.1 вЂ” 2026-05-16

### Added
- **`[NightVision]`**: СѓР»СѓС‡С€РµРЅРЅС‹Р№ С€С‚Р°С‚РЅС‹Р№ NVG РІ РєРѕРєРїРёС‚Рµ вЂ” С„РѕСЃС„РѕСЂРЅС‹Р№ **color filter**, РєРѕРЅС‚СЂР°СЃС‚/РЅР°СЃС‹С‰РµРЅРёРµ, **bloom** РЅР° РѕРіРЅСЏС…, РґРѕРї. **post-exposure** Рё РїРѕСЂРѕРі bloom РїРѕРІРµСЂС… vanilla auto-gain, СѓСЃРёР»РµРЅРёРµ `nightVisLight` Рё `nightVisionIlluminator`. РџРѕРєР° NVG РІРєР»СЋС‡С‘РЅ, РєРёРЅРѕРїРѕСЃС‚ **Atmosphere** РЅР° РѕСЃРЅРѕРІРЅРѕР№ volume РЅРµ РїСЂРёРјРµРЅСЏРµС‚СЃСЏ.

### Archived
- **2.0.0** (Р±РµР· NVG): `release/GlocCamera_Engine_v2.0.0.dll`

## 2.0.0 вЂ” 2026-05-16

### Changed
- **Version scheme:** `MAJOR.MINOR.PATCH`, each digit **0вЂ“9** (carry to parent on overflow). Current release **2.0.0** (= legacy dev **1.11.0**).
- **Atmosphere defaults:** softer **bloom** (lower intensity/scatter/tint blend) and lighter **vignette** (less edge darkening).

### Removed
- Experimental **gear / nose landing light** code (legacy **1.10.0** dev; not shipped).

### Added
- **Cinematic cockpit post** (URP volume, **`[Atmosphere]`**): **bloom** (intensity, threshold, scatter, warm tint), **post-exposure**, **saturation/contrast**, **color filter**, **vignette**, **chromatic aberration**; day/night lerp (sun heuristic).
- **`DeferSaturationVignetteDuringVanillaGloc`**: during vanilla G-LOC greyout/blackout, saturation/vignette/filter defer to the game.

## 1.10.0 вЂ” 2026-05-16 *(dev, not released)*

### Changed
- Gear light uses **geometry**: forward-most point on airframe renderer bounds, then **`NoseTipDownOffsetMeters`** below and **`NoseTipBackInsetMeters`** back along the nose. No NavLights / gearlight path lookup.

## 1.9.4 вЂ” 2026-05-16

### Fixed
- Gear lamp stuck on **waiting**: better transform search (path suffix + `gearlight_F` leaf), **NavLights.Toggle** via reflection, parent hierarchy forced active on gear deploy.
- **Vanilla emissive** landing lamp works even before Unity `Light` spawns; `onSetGear` retriggers bind/visuals.

## 1.9.2 вЂ” 2026-05-16

### Added
- **`GearDebugDumpHierarchy`**: on first cockpit bind per airframe, writes **every GameObject** under the local aircraft to `BepInEx\<aircraft>_hierarchy.txt` (plus sections for `LandingGear`, `Light`, `NavLights`). Use it to pick the nose lamp path for a future cfg key.

## 1.9.1 вЂ” 2026-05-16

### Fixed
- Many airframes have **no `Light` on the nose strut** (only `NavLights` emissive meshes). The mod now finds the **NavLights landing lamp anchor** and **spawns a child spot** on that transform; also forces nav lamp meshes/materials on when gear is down.
- Clearer bind failure logs (gear path, anchor path, light count).

## 1.9.0 вЂ” 2026-05-16

### Changed
- Gear lighting **reverted from ground decals** to Unity lights: the mod **finds the vanilla nose-strut landing lamp** (under nose gear / `NavLights` gear link) and **boosts it in place** вЂ” no spawned world rig, no layer changes.
- `ForwardSpot*` and world offset keys are **unused**; only `NoseGearLight*` + `ExcludeOwnAircraftLayers` apply.

## 1.8.0 вЂ” 2026-05-16

### Changed
- Gear / landing lighting is now **ground decals** (soft mesh quads raycast onto terrain/runway), **not Unity `Light` spots** вЂ” no cockpit or instrument wash.
- `NoseGearLightIntensity` / `ForwardSpotIntensity` = decal **brightness**; `*Range` = **diameter** (meters).
- **`GearBoostVanilla` default `false`** (unused; vanilla light multiply removed with decals).

## 1.7.5 вЂ” 2026-05-16

### Fixed
- **Reverted cockpit layer reassignment** from 1.7.4 (moving meshes to Ignore Collisions broke rendering). Gear lights again use **culling mask only** вЂ” game object layers are never modified.

## 1.7.4 вЂ” 2026-05-16

### Fixed
- Gear lights **washed out cockpit instruments**: **outdoor-only** layer mask (terrain/runway/world) plus temporary isolation of `cockpitRenderers` / interior meshes so beams aim **outward** only.

### Changed
- Nose/forward spots placed **farther ahead** and **steeper down** with **narrower cones** (defaults).

## 1.7.3 вЂ” 2026-05-16

### Fixed
- Gear lights **washed out the whole screen** after tuning: sane defaults, narrower cones, and **hard caps** on intensity/range/vanilla boost (cfg values above max are clamped).

### Changed
- Defaults: nose **5.5** / forward **16** intensity; forward range **120 m**; slightly tighter spot cones.

## 1.7.2 вЂ” 2026-05-16

### Fixed
- Gear/ground spots did not appear on many airframes: lights are now in **world space** (in front of the nose), with **URP `UniversalAdditionalLightData`**.
- **`ExcludeOwnAircraftLayers`**: mod spots skip the local aircraft's renderer layers (e.g. layer 15) so the beam is not wasted on fuselage textures.

### Changed
- World offsets: `NoseGearLightForwardOffsetMeters` / `ForwardSpotForwardOffsetMeters` (defaults **1.4 m** / **7 m** ahead of nose gear).
- Higher default URP intensities (**12** / **48**).

## 1.7.1 вЂ” 2026-05-16

### Fixed
- **Forward ground spot** now parents to the **aircraft root** (not only the nose strut), with offset from the nose gear point in aircraft space вЂ” fixes missing/invisible spotlight on many airframes.

### Changed
- **+50%** default brightness for nose strut light and forward ground spot (intensity/range).

## 1.7.0 вЂ” 2026-05-16

### Added
- **Nose gear landing light:** wide warm spot parented to the nose strut **unsprung** (`GlocCamera_NoseGearLandingLight`) вЂ” reliable runway/taxi wash when gear is down.
- **Forward ground spot:** bright long-range spot ahead of the aircraft (`GlocCamera_ForwardGroundSpot`).
- **Vanilla gear boost:** optional force-enable + intensity multiply on existing `Light` components on nose/landing gear paths (`GearBoostVanilla`, `GearVanillaBoostMul`).
- Config under **`[Lighting]`**: `GearEnabled`, `GearNightMin01` (default **0** = day+night), `NoseGearLight*`, `ForwardSpot*`.

## 1.6.1 вЂ” 2026-05-12

- **ApexView:** Uses **pitch, roll, and yaw** from `ControlInputs` вЂ” local **X, Y, and Z** ( **`MaxDepthMeters`** + **`Depth*Scale`** ). New cfg: lateral/vertical/**depth** pitch scales, **`PitchInputSign`**. Default **yaw lateral** weight raised (**`LateralYawScale` = 1**) so rudder/coordinated yaw is easier to feel.

## 1.6.0 вЂ” 2026-05-12

- **ApexView (no TrackIR):** Cockpit camera **local X/Y** follows smoothed **roll + yaw** control inputs (same `ControlInputs` as the flight model), so banking/yawing gently **peeks** toward the turn / outside horizon. Config section **`ApexView`**: enable, lateral/vertical caps, per-axis blend scales, dead zone, SmoothDamp time/speed, optional **input sign** flips, and **`AlsoApplyWithTrackIR`** when using CockpitView framing with TrackIR.

## 1.5.2 вЂ” 2026-05-12

- **Lighting:** Standard spawn pitch **`FillPitchDegrees = +10В°`**. **Normal vs wide** cone: **`FillSpotAngle` / `FillInnerSpotAngle`** vs **`FillSpotAngleWide` / `FillInnerSpotAngleWide`**. Hotkeys (BepInEx `KeyboardShortcut`, rebindable in cfg): **`J`** toggles fill **armed** (night gate still applies), **`K`** toggles **wide** mode. Hotkeys only when **controls on**, **cursor hidden**, map not maximized, radial menu idle.

## 1.5.1 вЂ” 2026-05-12

- **Lighting вЂ” spawn-then-freeze:** The fill is **created** as a child of the **cockpit camera** using **`FillLocalPosition`** + **`FillPitch/Yaw/RollDegrees`** in **camera local space** (so you can line it up with the view), then **immediately** reparented to **`cockpit.transform`** with **`worldPositionStays: true`**. After that, **transform is never updated again** until aircraft change / leave / disable вЂ” no more вЂњcamera into the floorвЂќ drift from overwriting pose each frame. If spawn didnвЂ™t freeze in one tick, the next tick completes the snap from camera в†’ cockpit.

## 1.5.0 вЂ” 2026-05-12

- **Lighting вЂ” panel-fixed beam:** Fill spot is parented to **`cockpit.transform`** again (not the cockpit camera). **Position + euler are cockpit-local**, so the light **does not pan** with free-look / head motion вЂ” full fixation on the instrument volume. Default pitch **`FillPitchDegrees = +35`** (per user tuning; flip sign in cfg if your mesh reads inverted vs Unity). Stray copies (e.g. old camera-parented lights) are removed from anywhere under the aircraft when the fill is recreated.

## 1.4.9 вЂ” 2026-05-12

- **Lighting:** Fill rotation is **fixed in camera local space** again: default pitch **`FillPitchDegrees = в€’45`** (toward the instrument stack), with optional **`FillYawDegrees` / `FillRollDegrees`**. Removed **`FillAimLocal`** / per-frame look-at so the beam no longer вЂњtracksвЂќ free-look; the light **child still follows** the cockpit camera transform for position only.

## 1.4.8 вЂ” 2026-05-12

- **Lighting:** Fill spot **aims at** configurable **`FillAimLocal`** (camera space): rotation is **`LookRotation`** from **`FillLocalPosition` в†’ `FillAimLocal`** each frame so the beam tracks the **instrument stack** while you pan. Removed fixed **`FillLocalEuler`** (use aim point instead). Defaults: **narrower cone**, **lower intensity**, slightly shorter range.

## 1.4.6 вЂ” 2026-05-12

- **Lighting вЂ” collar / body flashlight:** Instrument fill is now parented to the **cockpit camera** (`CameraStateManager.transform`), not the cockpit root, so the source sits like a **shirt-pocket / upper-chest** light relative to your view (moves with pan/head). Defaults: slightly **below** the eyepoint and **back** along the view axis, gentler pitch so the beam favors **MFDs** over the footwell. Stray old fill objects under the cockpit mesh are still cleaned up on create.

## 1.4.5 вЂ” 2026-05-12

- **Lighting вЂ” instrument flashlight:** Fill is now a **wide handheld-style spot** from **behind** the pilot: shorter **range** (default ~**1.3 m**), wider **outer/inner cone**, steeper aim at the **panel**. Game object renamed to **`GlocCamera_InstrumentFlashlight`**; legacy **`GlocCamera_PilotFill`** is removed when the new light is created. New config **`FillInnerSpotAngle`**.

## 1.4.4 вЂ” 2026-05-12

- **Shake вЂ” overspeed:** Denominator uses **`Max(aircraftInfo.maxSpeed, aircraftParameters.maxSpeed)`** (km/h в†’ m/s). Optional **`OverspeedUseApproximatedIas`**: numerator Г— **в€љ(ПЃ/ПЃв‚Ђ)** using **`Aircraft.GetAirDensity()`** and **`OverspeedIasReferenceDensity`**. New **`SpeedReadingScale`** scales game speed for **gear / airbrake / touchdown / runway / ground** km/h logic and the overspeed numerator. Defaults **`OverspeedStartRatio` / `OverspeedFullRatio`** raised to **1.06 / 1.20** (existing cfg keeps saved values).
- **Lighting:** **`Lighting.*`** вЂ” night **pilot fill** spot (child under cockpit) when sun heuristic в‰Ґ **`FillNightMin01`**; multiply **`Light`** intensity/range on paths matching **`ExternalNameSubstrings`** (skips cockpit-only paths unless they also match). Restores on cockpit leave / plugin unload (with atmosphere).
- **CockpitView:** **`OffsetLocalX/Y/Z`**, **`FovBiasDegrees`**, optional **`ApplyFramingWithTrackIR`** (position clamps aligned with vanilla TrackIR box).
- **Refactor:** Shared **`GlocNightFactor.ComputeNight01()`** for atmosphere + lighting night gate.

## 1.4.3 вЂ” 2026-05-12

- **Shake:** Stall, overspeed, vertical G, VRS, rocket-hit, and touchdown now use a **weak-phase в†’ vibration** split: at low severity the mix is **low-freqвЂ‘light / high-freqвЂ‘heavy**; at severity **1** it matches your existing `*LowMaxAdd` / `*HighMaxAdd` caps. Tuning: **`Shake.WeakPhaseSeverityExponent`** (higher = weak phases stay buzzier longer). Rocket hit uses pre-decay severity for this frame.

## 1.4.2 вЂ” 2026-05-12

- **Shake вЂ” runway / ground roll:** Replaced single low/high caps with **vibration vs shake** pairs (like gear/airbrake). Default **100вЂ“350 km/h** stays **vibration-heavy**; above **350**, blend **SmoothDamp**-lerps toward **shake** by **`RunwayRollBlendShakeEndKmh`** / **`GroundRollBlendShakeEndKmh`**. Below **100 km/h**, magnitude scales with **speed / VibrationFocusStartKmh** so parking stays quiet. Removed old keys **`RunwayRollLowMaxAdd`**, **`RunwayRollHighMaxAdd`**, **`GroundRollLowMaxAdd`**, **`GroundRollHighMaxAdd`** вЂ” delete from cfg or regenerate.

## 1.4.1 вЂ” 2026-05-12

- **Shake:** Softer **runway / ground taxi** rumble: lower `RunwayRoll*` / `GroundRoll*` default caps, higher default вЂњfull speedвЂќ km/h, and **sublinear** speedв†’severity (`Pow` on normalized speed). **Touchdown** slightly reduced (lower caps, higher default VS/speed full thresholds) with **sqrt** blend of vertical + horizontal so heavy sink still reads but overall jolt is gentler.

## 1.4.0 вЂ” 2026-05-12

- **Shake:** Removed **gun** and **missile** launch hooks (Harmony + config); they were dropped as a feature direction.
- **Shake вЂ” touchdown:** One-shot cockpit jolt when **landing gear** meets the ground after a real airborne segment (`radarAlt` crossing the same **0.2 m** threshold as vanilla); strength scales with **downward vertical speed** (cockpit rigidbody vs world up) and **ground speed** (`speed`Г—3.6 as km/h).
- **Shake вЂ” taxi:** Continuous **`AddShake`** on **runway strip** vs **off-runway** surfaces: uses **`FactionHQ.AnyNearAirbase`** + **`Airbase.AircraftIsOnRunway(..., landingRunwaysOnly: false)`** for pavement; otherwise **ground roll** (grass, dirt, off-strip, etc.). Both scale with ground speed when gear is down and not airborne.

## 1.3.5 вЂ” 2026-05-12

- **Shake:** Missile-launch hook now also patches **`MountedMissile.Fire`** (single-shot / rail missiles never called **`MissileLauncher.Fire`**). Gun hook uses explicit **`Gun.SpawnBullet(float)`** signature for Harmony. **`ReportMissileLaunch` / `ReportGunFired`** use **`GameManager.IsLocalAircraft`**. Missile/gun severity is applied **before** decay in **`Apply`** so same-frame impulses are visible. Stronger default **`MissileFire*`** / **`GunFire*`** magnitudes and slightly longer decay.

## 1.3.4 вЂ” 2026-05-12

- **Shake / gear:** Removed the always-on вЂњgear downвЂќ shake that caused **runway/spawn jitter**. Gear-driven `AddShake` now starts only at **`GearMinSpeedKmh`** (default **500**), ramps to full by **`GearIntensityFullKmh`**, and **smoothly blends** from high-frequency **vibration** to low-frequency **rumble** between **`GearBlendVibrationEndKmh`** and **`GearBlendShakeEndKmh`**. Old config keys `GearLowMaxAdd`, `GearHighMaxAdd`, `GearSpeedStartKmh`, `GearSpeedFullKmh`, `GearSpeedExtra*` are **removed** вЂ” delete those lines from `com.at747.gloccamera.cfg` or regenerate the file.
- **Shake / airbrakes:** Adds the same **vibrationв†”shake** pattern when vanilla **`Airbrake`** surfaces are extended (`openAmount`, read via reflection), gated by **`AirbrakeMinSpeedKmh`** and deployment threshold so **idle throttle on the ground** stays quiet.

## 1.3.0 вЂ” 2026-05-11

- **Shake:** Harmony postfix on `CameraCockpitState.FixedUpdateState` calls vanilla `AddShake` from lateral maneuver G and sharp longitudinal jerk (`Dot(accel, forward)`), with cockpit/local-aircraft/controls/pilot gates and a **TrackIR** strength scale.
- **Atmosphere:** Cockpit-only blend toward stronger **Bloom** and **post-exposure** at night (sun `Dot(-forward, up)` heuristic), snapshotting baseline values on enter and **restoring** them on cockpit leave, when the plugin is destroyed, or when sections are turned off.
- **References:** `GlocCamera_Engine.csproj` now references **`Unity.RenderPipelines.Core.Runtime`** and **`Unity.RenderPipelines.Universal.Runtime`** from the game `Managed` folder for URP volume types.

## 1.3.1 вЂ” 2026-05-11

- **Glow boost:** lowered URP `Bloom.threshold` at night and increased night `Bloom` + `post-exposure` to make cockpit instrument emissive lights bloom more strongly.

## 1.3.2 вЂ” 2026-05-11

- **Shake events:** add severity-based cockpit shake for stall (speed below stallSpeed), overspeed (speed above maxSpeed), large vertical acceleration (Dot(accel, up)), and helicopter vortex ring (VRSFactor from RotorShaft). All respect cockpit/local/pilot/control gates and `Shake.TrackIRScale`.

## 1.3.3 вЂ” 2026-05-11

- **Shake extras:** add shake when landing gear is deployed (light + ramps with speed), small jolt on missile launch, small shake during gun fire, and strong jolt on incoming explosive/rocket hits (based on DamageInfo blast/impact).
- **Fix:** stall shake now uses the game's own AoA gating (`AoAFeedback` OnsetSpeed + OnsetAlpha) so it does not trigger on the runway at near-zero speed.

## 1.2.0 вЂ” 2026-05-09

- **Strength:** Defaults tuned to about **2Г—** a moderate baseline (was ~10Г—): `DegreesPerLongitudinalG` **5**, `MaxDeltaDegrees` **40**, dolly scale/max scaled down accordingly.
- **Smoothing:** FOV and dolly now use **`SmoothDamp`** (`SmoothTimeSeconds` + optional max speed caps) instead of fixed В°/s and m/s slew.
- **Cockpit FOV blend:** New **`CockpitLerpBlend`** (default **0.11**, vanilla **~0.2**) for slower per-frame easing toward the final FOV target.
- Removed config keys **`SmoothTowardDegPerSec`**, **`SmoothAwayDegPerSec`**, **`SmoothTowardMPerSec`**, **`SmoothAwayMPerSec`** вЂ” delete stale lines from `com.at747.gloccamera.cfg` or regenerate the file.

## 1.1.0 вЂ” 2026-05-09

- **Behavior:** Only **longitudinal** acceleration (`accel В· forward`) drives the effect вЂ” **maneuver / total G removed** (game already handles that).
- **FOV:** Positive longitudinal G (throttle-on acceleration) в†’ **wider FOV** (zoom out); braking along the nose в†’ narrower. Default `DegreesPerLongitudinalG` is now **+24**.
- Config keys `DegreesPerExcessTotalG` and `DeadZoneExcessTotalG` removed; delete or trim them from old `com.at747.gloccamera.cfg` if present.

## 1.0.2 вЂ” 2026-05-09

- Defaults scaled **~10Г—** for FOV/dolly sensitivity; **`MaxDeltaDegrees` raised to 200** so the driver no longer clips the effect to almost nothing.
- Tighter dead zones and faster smoothing so the change reads immediately in the cockpit.

## 1.0.1 вЂ” 2026-05-09

- **Stronger defaults** so the effect is easier to see in flight: higher |В°/G|, larger max FOV delta, faster attack, slightly smaller dead zones, more dolly range.
- Existing `com.at747.gloccamera.cfg` keeps your saved numbers; delete the file (or edit keys) if you want these new defaults.

## 1.0.0 вЂ” 2026-05-09

- Initial release for **Nuclear Option** (BepInEx 5).
- Cockpit-only Harmony patch on `CameraCockpitState.UpdateState`: adds smoothed FOV delta from `Aircraft.accel` / `gForce` without replacing menu baseline FOV.
- Optional forward dolly on `CameraStateManager.transform.localPosition.z` when TrackIR is off.
- Config under `BepInEx/config/com.at747.gloccamera.cfg`.

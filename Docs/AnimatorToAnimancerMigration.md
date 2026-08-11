# Animator to Animancer Migration

## Current Status

- Unity: `6000.2.9f1`.
- Animancer: `8.4.0`, package `Packages/com.kybernetik.animancer`.
- Odin Inspector: installed under `Assets/Plugins/Sirenix`.
- Target scene: `Assets/_Recovery/0 (58).unity`.
- Target object: scene instance `Player` from prefab `Chr_Fantasy_MalePeasant_01`.
- Runtime config: `Assets/Animation/Animancer/MobilityProPlayerAnimationConfig.asset`.
- Animation source: the project's original locomotion clips for every role that previously existed, with Unity MOBILITY PRO Mocap Animation Pack 2.7B1 IPC Humanoid clips filling missing roles.
- Generated configuration contains 628 serialized animation references, including runtime mappings, intentional directional fallbacks, and a 298-clip tuning/preview catalog (12 project-original clips plus 286 MOBILITY PRO clips).
- The scene's original `Player.controller` remains assigned in edit mode for rollback. Animancer clears it only after a valid config initializes at runtime.
- No Animancer, Odin, MOBILITY PRO, or other third-party source file was modified.

## Scene Wiring

The target scene player now has:

- `AnimancerComponent`, bound to the existing Animator.
- `CharacterAnimancerController`, bound to Animancer, Animator, and the generated config.
- `PlayerController.animancerController` assigned with legacy Animator fallback retained.
- `InventoryHeldItemController.animancerController` assigned for the existing masked holding layer.

The scene change is additive. Existing camera, CharacterController, interaction, inventory item roots, prefab overrides, movement values, and Animator Controller reference are preserved. Scene movement remains `walk = 2`, `run = 4`, and `crouch = 1`; animation thresholds were matched to those values instead of changing gameplay speed.

## MOBILITY PRO Mapping

| Feature | Assigned content | Runtime behavior |
| --- | --- | --- |
| Idle | Relaxed idle and idle variation | Timed variation with configurable fade and interval |
| Walk | Forward, backward, left, right, and four diagonals | Actual-speed 2D Cartesian mixer |
| Jog | Forward, backward, left, right, and four diagonals | Intermediate mixer ring for smoother walk/run blending |
| Run | Forward, left, right, and forward diagonals | Run mixer ring; gameplay still permits running only with forward input |
| Crouch | Crouch idle and eight-direction crouch walk | Separate actual-speed 2D mixer |
| Starts | Strafe starts plus 45/90/135/180-degree forward-turning variants for walk, jog, run, and crouch | Code-driven one-shot transitions; start family is selectable |
| Stops | Walk, jog, run, and crouch neutral/LU/RU directional stops | Selects the stop matching the current locomotion foot phase |
| Crouch transition | Stand-to-crouch and crouch-to-stand | Used while stationary; moving changes return to the crouch mixer |
| Turns and pivots | Standing/crouch 45/90/135/180 turns, continuous turn loops, walk/jog/run 90/180 pivots | Accumulates small yaw deltas, uses a settle timer for discrete turns, and supports continuous idle turning |
| Movement curves | Walk/jog/run/crouch CIR loops and walk/jog/crouch backpedal turn loops | Plays while movement direction is maintained and character yaw changes |
| Jump | Standing plus walk/jog/run, directional, left/right takeoff-foot start clips | Selected from actual horizontal movement |
| Air | Matching split jump air clips | Starts after takeoff transition and overrides locomotion |
| Landing | Matching split landing clips and fallbacks | Uses cached impact velocity and previous movement state |

Ordinary locomotion uses in-place clips. `CharacterController` remains the owner of position, gravity, collision, and gameplay movement.

## Smoothing Model

- Mixer parameters are local velocity in real metres per second, not raw input.
- Walk, jog, and run clips occupy separate radial speed rings.
- Diagonal thresholds are normalized so diagonal input does not inflate visual speed.
- Acceleration, deceleration, and direction changes have separate smoothing times.
- A visual acceleration limit prevents abrupt mixer jumps.
- Start and stop input thresholds suppress low-speed flicker.
- Stop clips can match left-foot-up/right-foot-up locomotion phase.
- Idle turn input accumulates across frames, so normal mouse movement can trigger 45/90/135/180 turns instead of requiring a single-frame angle spike.
- Sustained yaw uses turn-in-place or movement-curve loops and fades back to the velocity mixer after input settles.
- Locomotion cycles can be synchronized across walk, jog, and run clips.
- Fade In, Fade Out, Fade Out Start, normalized End Time, playback speed, normalized start time, Foot IK, and runtime override can be configured per clip.
- Every clip can also override the final CameraRoot local position and camera blend speed. Crouch-down and stand-up fallback speeds are independently adjustable.
- Mixers and transitions are created once and reused; no per-frame LINQ, reflection, string search, or state construction is used.

## Inspector Tuning

Select `MobilityProPlayerAnimationConfig` to tune all shared animation values through Odin tabs:

- `Setup`: source pack, target scene, quality, and runtime Animator Controller policy.
- `Camera`: per-animation camera positions, fallback crouch/stand speeds, wall-collision radius/mask/padding, absolute and near-pose minimum distance, pull-in/return speeds, anchor, and near clip.
- `Locomotion`: every clip reference, directional set, speed threshold, cross-fade, start/stop rule, smoothing value, playback range, turn, and pivot option.
- `Clip Tuning`: searchable, paged catalog of all 298 project/MOBILITY clips. Each entry exposes Fade Duration (Fade In), Speed, Start Time, Fade Out, Fade Out Start, End Time, Foot IK, source, and per-animation camera position/speed.
- `Open Animation Preview`: opens a searchable browser with a visible eye button for every clip. It is also available at `Tools > Light Tower > Animation > Open Animation Preview`.
- `Airborne`: jump start/air/landing clips, foot selection, fade timing, fall timing, and landing thresholds.
- `Holding`: current holding clip, right-arm AvatarMask, layer target, and fade values.
- `Actions`, `Parkour`, `Layers`, `Root Motion`, and `Events`: extension settings retained for later migration phases.
- `Validation`: required clip and threshold checks.

Select the scene Player's `CharacterAnimancerController` during Play Mode for read-only runtime data: logical state, gait, transient, clip, normalized time, target and smoothed speed, acceleration, local X/Z velocity, mixer parameter, vertical speed, airborne time, grounded/crouched state, layer weights, holding state, root motion, last event, and last state-change reason.

## Firewatch-Style Visual Layer

- The embedded Buto package is version `7.11.6` and is active through a `ButoRenderFeature` on `PC_Renderer`; the existing SSAO feature is preserved.
- Recover scene 58 has one global `Firewatch Visual Style` Volume. It carries Buto fog, Color Adjustments, White Balance, warm/cool Split Toning, Neutral Tonemapping, Bloom, and Vignette.
- The player camera has post-processing and depth enabled, plus a `0.05` near clip to reduce close-surface clipping.
- The active player body uses a generated `MK/Toon/URP/Standard/Simple` material. Other scene materials and shaders are left untouched.
- Select `Assets/Settings/FirewatchVisualStyleConfig.asset` to tune fog density/height/color/direction/noise, color grade, Bloom/Vignette, and MK Toon light bands. Apply changes with its `Apply Visual Style` button or `Tools > Light Tower > Visual > Apply Firewatch Visual Style`.

## Compatibility

- If Animancer or its config is unavailable, `PlayerController` still uses the original Animator state-name path.
- Existing animation-camera pose keys remain driven by equivalent logical movement states even though the active clips now have MOBILITY PRO names.
- Camera wall collision uses a sphere cast from a player-local anchor and filters the player's own colliders. Its absolute minimum distance and close-pose ratio are both adjustable; the camera pulls in quickly before geometry and returns smoothly afterward.
- Existing `InventoryHeldItemController` item selection remains unchanged. Its holding state now fades the Animancer upper-body layer through the existing right-arm mask; the old Animator parameter/layer implementation remains the fallback.
- The original Animator Controller asset is not deleted or rewritten by this migration.
- Runtime initialization temporarily clears the Animator Controller for Animancer, then restores the original reference on disable or Play Mode reload so the scene cannot save a disconnected controller.
- The Day 1 wake-up sequence always restores player controls in `finally` and has a 45-second realtime recovery timeout if the cutscene stalls.
- The setup can be re-applied from `Tools > Light Tower > Animation > Configure MOBILITY PRO Player (recover 58)`.

## Files

| File | Purpose |
| --- | --- |
| `Assets/Scripts/Animation/CharacterAnimationTypes.cs` | Shared state, gait, transition, action, parkour, and validation types |
| `Assets/Scripts/Animation/CharacterAnimationConfig.cs` | Unity-serialized and Odin-organized animation configuration |
| `Assets/Scripts/Animation/CharacterAnimancerController.cs` | Runtime mixers, transitions, layers, action APIs, and debug data |
| `Assets/Scripts/Animation/Editor/MobilityProLocomotionConfigurator.cs` | Repeatable clip assignment and scene-instance wiring |
| `Assets/Scripts/Animation/Editor/CharacterAnimationPreviewWindow.cs` | Searchable eye-button preview browser for every configured clip |
| `Assets/Scripts/Animation/Editor/LightTowerRuntimeSmokeTest.cs` | Repeatable recover-58 Play Mode control, camera, animation, and visual validation |
| `Assets/Scripts/ControllerView/Player/PlayerController.cs` | Feeds motor velocity, grounded/crouch/jump/landing state to Animancer |
| `Assets/Scripts/ControllerView/Player/InventoryHeldItemController.cs` | Bridges existing held-item state to the masked Animancer layer |
| `Assets/Animation/Animancer/MobilityProPlayerAnimationConfig.asset` | Generated MOBILITY PRO clip and tuning data |
| `Assets/Scripts/Visual/FirewatchVisualStyleConfig.cs` | Adjustable Buto fog, URP color grade, and MK Toon settings |
| `Assets/Scripts/Visual/Editor/FirewatchVisualStyleConfigurator.cs` | Repeatable recover-scene visual-style wiring |
| `Assets/Settings/FirewatchVisualStyleConfig.asset` | Firewatch-style visual tuning asset |
| `Assets/Settings/FirewatchVisualStyleProfile.asset` | Global Buto and URP post-processing profile |

## Verification

- Unity's script pipeline completed successfully after configuration.
- `dotnet build Assembly-CSharp.csproj -nologo`: 0 warnings, 0 errors.
- `dotnet build Assembly-CSharp-Editor.csproj -nologo`: 0 warnings, 0 errors.
- Configuration result: success, 628 references, 298 unique preview/tuning entries, original Animator Controller preserved, config validation usable.
- Final recover-58 Play Mode smoke test passed every check: controls released, CharacterController moved, Animancer reached target speed 2, original clips remained assigned, MOBILITY PRO filled missing roles, all 298 clip tunings loaded, and runtime Animator Controller clearing/restoration completed cleanly.
- Camera validation passed: crouch CameraRoot moved from `1.30` to `1.05`, an obstruction pulled the camera from `0.684` to `0.360`, and it returned to `0.684` after the obstruction was removed.
- Visual validation passed: the seven-component global profile, Buto renderer feature, SSAO preservation, post-processing/depth, warm/cool Split Toning, and the player's MK Toon shader were all active in Play Mode. A 1280x720 runtime render was captured successfully.
- Automated turn smoke test: sustained right yaw triggered `MOB1_Stand_Rlx_Turn_In_Place_R_Loop_IPC`, a 90-degree request triggered `MOB1_Stand_Relaxed_R_90_IPC`, a moving forward-to-right change triggered `MOB1_Walk_R_90_IPC`, and Play Mode exited cleanly.
- Runtime control probe: PlayerController, CharacterController, all five control flags, and both Animancer components stayed enabled; the player position changed under input; runtime Animator Controller was temporarily clear and the original `Player.controller` reference remained serialized after Play Mode exited.

## Remaining Review

- Run a hands-on Play Mode pass for foot sliding and adjust mixer thresholds/playback speed in the config, not the player's gameplay speeds.
- Review turn-loop and curve thresholds by feel with the final camera sensitivity; the defaults are enabled and verified, but these values are intentionally exposed for character-specific tuning.
- Attack, hit, death, dodge, bespoke interaction, and Parkour Set actions are outside this locomotion configuration and remain future migration phases.
- Parkour root motion must be forwarded through the existing movement motor before any parkour clip enables it.

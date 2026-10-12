# Light stops moving platforms

Open `Assets/Scenes/Johnny/LightPlatformLab.unity` and press Play, or choose **Light Platform Lab** in level selection.

- The platform moves between two endpoints while dark. Any enabled collider tagged `Light` touching its slab holds it at its current position. Removing the last light resumes the same route and direction.
- **Click the amber lamp**, then press **F** to rotate it clockwise. Two turns from its initial upward direction aim down and hold the platform; two more release it. Click the lamp again if clicking elsewhere deselects it. The emitter turns mint and shows SELECTED / PRESS F when selected. These use the existing `ControllableLight` script. The platform changes from mint `MOVING` to amber `HELD BY LIGHT`.
- Jump onto the platform, ride across the gap, and jump onto the exit ledge. A permanent lower path and a step allow recovery after a missed jump.
- Overhead and recovery training light volumes keep the player solid while leaving the platform slab unlit. This isolates the brake lesson from the player's darkness mechanic. The controllable cone's visible shape matches its light trigger.

## Reuse in the team's levels

Drag `Assets/Johnny/Obstacles/Prefabs/LightStoppingPlatform.prefab` into your own scene. Its root is the top surface. Set **Travel Offset** to the desired world-space displacement from its starting position and **Speed** in units per second. Select the platform to see both endpoints. Keep the supplied platform horizontal, unrotated, and stationary relative to its parent. The demonstration and rider tests use horizontal travel; test other trajectories in your own scene with enough clearance. Light detection uses the actual slab collider, including its offset and scale.

The light collider itself must carry the `Light` tag, be enabled, and overlap the slab. This follows the game's existing collider-based light system; a `Light2D` renderer alone does not supply gameplay coverage, and visual shadows do not automatically remove coverage. Existing blink lights disable their collider when off. Multiple lights are supported: all must leave or turn off before movement resumes. Disabling or destroying a light also releases it without relying on an exit callback.

The component uses a kinematic rigidbody and carries standing, solid players using the actual player box, rather than the larger light sensor. Ascending jumps and side-only sensor contacts are not carried. It does not parent the player or replace the shared player controller. The menu's pause freezes travel; restarting reloads the original position, direction, and light state. A zero speed leaves the platform stationary.

**Tools > Johnny UI > Create light platform demonstration level** generates this prefab and scene. Close LightPlatformLab and stop Play mode first. Preserve manual changes before regenerating; the helper replaces those two generated assets and keeps other saved open scenes intact.

## Verification

Original prototype verification before migration, Unity 6000.3.22f1 Play Mode suite: **30 passed, 0 failed** on October 10, 2026. Nine light-platform tests cover bounded round trips, exact light holds and resumed travel, multiple/disabled/destroyed/blinking light colliders, unrelated triggers, the existing controllable light's direction handler, standing rider carry/hold and jump detachment, sensor-only side contact, pause/restart, and completing the real demo with the original 5-unit walk speed, 20-unit jump impulse, and gravity scale 5. Twenty-one existing UI and obstacle tests also pass, including empty level selection and final-level menu behavior. The route tests drive real player physics deterministically rather than synthesizing OS keys. Reports: `tmp/LightPlatformReview/tests.xml` and `tests.log`.

The original prototype Windows development preview built successfully. Desktop checks confirmed all three level cards, selecting/launching this demo, readable instructions, and actual Down/Up key input producing a held/resumed platform with matching text and colors. The tutorial scene and shared player prefab hashes remain unchanged. WebGL remains unverified because its Unity module is not installed locally.

## Files to share

Include `LightStoppingPlatform.cs`, `Prefabs/LightStoppingPlatform.prefab`, `LightPlatform_README.md`, and their `.meta` files from this folder; `Assets/Johnny/Editor/LightPlatformLabSetup.cs` and its meta; and `Assets/Scenes/Johnny/LightPlatformLab.unity` and its meta. They depend on Johnny's existing obstacle assembly, `HazardContact`, obstacle art/material, UI assembly, and the team's existing player, camera, light controller, and victory scripts. Sharing the whole `Assets/Johnny` and `Assets/Scenes/Johnny` folders with their metas is easier if teammates do not already have those dependencies.

Also share the updated `LevelCatalog.cs`, `LightPlatformTests.cs`, and metas. Merge the new scene into **Build Profiles > Scene List**, enabled after ObstacleLab, preserving teammates' scene entries; retain the `Light` tag and `Ground` layer. Do not send Library, Temp, Logs, or the `tmp` preview/test output.

Unity API references checked October 10, 2026: [collider overlap](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Collider2D.Overlap.html) and [kinematic movement](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rigidbody2D.MovePosition.html). This feature uses the project's existing light rule; no external assets or packages were added. Record Codex implementation and verification assistance in the GDD External References.

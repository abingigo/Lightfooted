# Johnny's Alpha features in Lightfooted

This project is a fresh checkout of `https://github.com/abingigo/Lightfooted`, based on team commit `5fa5e9d51beb798bde4c15c62474809dd9f187bb`. Work is on `feature/johnny-alpha-ui-obstacles`. The earlier paired prototype remains in the parent folder as a backup; its Git history has not been merged into this repository.

## Open and play

Add **this folder** (`Light_Platformer/Lightfooted`) to Unity Hub and open it with Unity **6000.3.22f1**. Open `Assets/Scenes/Johnny/LevelSelect.unity`, then press Play. A blank Untitled scene is not the game.

Enabled scenes are Level Select, the existing Tutorial, Obstacle Lab, and Light Platform Lab. The team's existing unfinished scenes remain outside the build list. New enabled gameplay scenes appear in level selection automatically.

- A / D: move; Space: jump; Left Shift: dash.
- Click a controllable light, then F: rotate it clockwise.
- E / Q: pick up / throw a light; R: interact with batteries.
- Esc / P or the Pause button: pause/resume. Menus offer restart, level select, and controls. Death and victory open the appropriate result screens.

The light-platform demo starts with its lamp pointing upward. Click the amber emitter to select it; its mint color and SELECTED / PRESS F text confirm selection. Press F twice to point down and stop the platform as it enters the cone. Two more turns release it. Both demo levels have recovery paths.

Keyboard focus belongs to menus while they are open. During gameplay the HUD does not select its Pause button, so Space remains a jump input rather than submitting Pause. Esc/P still pauses with the keyboard, and the mouse Pause button remains available.

## Integration

Transferred Johnny's level menus, spike hazards, pulse beams, crumble platforms, light-stopping platform, reusable prefabs, and two separate demonstration levels with their existing metadata. The menus use the team's Lightfooted name and current controls.

Preserved the team's force-based movement and dash implementation, player prefab, light tracker, battery and throwable-light behavior, camera follow, service groundwork, and tutorial scene. Demo player instances use the current team prefab without changing its asset or movement settings.

Shared changes are limited to:

- `PlayerController`: block input in Update and FixedUpdate while menus are active; discard queued input so a menu submit does not become a jump/dash on resume.
- `ControlLightSource`: block gameplay while menus are active, ignore UI clicks, handle a missing main camera safely, and expose selection state for demo feedback. Click/F controls remain.
- `PlayerBatteryInteraction` and `PlayerThrowableLight`: block interactions while menus are active.
- `GameEndText` and `Victory`: send one-time menu notifications and tolerate demos without legacy text objects.
- Build settings: add the menu and Johnny's demo scenes while retaining existing scene entries. Tag settings: retain the Light tag for collider-based light detection.

Johnny's demo cameras are tagged MainCamera. The brake emitter has a separate small clickable collider and selection feedback; its cone continues to supply gameplay light coverage. The starting camera keeps the lamp visible.

## Validation

Verified on October 11, 2026 with Unity 6000.3.22f1: **36 Play Mode tests passed, 0 failed** (13 menu, 10 obstacle, 9 light-platform, 4 team-integration tests). The Windows development build succeeded. Desktop checks confirmed updated controls, selecting/launching the light demo, visible spawn and lamp, real Space-key jumping without pausing, click/F rotation and platform hold/release feedback, Escape pause, restart, and return to level selection. Full demo completion is covered by the force-based route tests, rather than a complete manual playthrough.

Play Mode reports and build logs are in `tmp/MigrationReview`, and the Windows preview is `tmp/UIBuild/Lightfooted.exe`. These local outputs are ignored by Git. WebGL has not been validated because the module is not installed locally.

The suite exercises menu buttons, empty/single-level catalogs, unavailable scenes, keyboard focus, tutorial death/victory, all obstacle behavior, platform carry and restart, queued-input clearing, visible spawn/lamp, selection feedback, and compatibility with battery/throwable light ranges. The two route tests now invoke the team's force-based physics handler on the real player rigidbody. Automated tests inject movement commands rather than synthesizing operating-system keys; the Windows preview provides the separate real-input check.

The feature guides under `Assets/Johnny` retain the original prototype test results explicitly labeled as historical. Current migration results belong here and in the migration report.

## Share and extend

Use this branch's commit when sharing with the team; it includes the required shared-script integrations. Sending only scenes or scripts without their `.meta` files will lose references. Keep Library, Temp, Logs, test output, and preview builds out of Git.

Use the editor helpers under **Tools > Johnny UI** only to regenerate Johnny's scenes. Close those scenes first and preserve manual edits; generation replaces their contents. It does not save or replace the tutorial.

No external assets or packages were added. Design sources remain in the obstacle guide. Record implementation and verification assistance in the GDD External References as described in the feature guides.

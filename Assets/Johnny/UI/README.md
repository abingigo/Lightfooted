# Game menus

Open `Assets/Scenes/Johnny/LevelSelect.unity` and press Play. Builds start with that scene. Opening the tutorial directly in the editor also gets the gameplay UI automatically. The tutorial scene is unchanged.

- Select a level, then Play. Only enabled playable build scenes are listed; SampleScene and NewScene remain unavailable.
- Esc or P pauses/resumes. The Pause button works with the mouse.
- Pause offers Resume, Restart level, Level select and Controls.
- Death offers Restart and Level select. Victory offers Play again and Level select; Next level appears only if another playable level follows the current one.
- Tab / Shift+Tab cycles buttons; arrows navigate, Enter submits, and Esc closes Controls.
- With no playable levels, the menu explains that no levels are available, disables Play, and keeps Controls accessible.

Add a finished level to Unity's enabled build scenes to list it automatically. Keep the menu first. Do not enable unfinished test scenes. Stable scene paths are used for loading, so duplicate display names do not choose the wrong scene. Test scenes and Unity's temporary test-runner scenes are excluded.

The playable **Obstacle Lab** demonstration is listed after Tutorial, followed by **Light Platform Lab**. Tutorial and Obstacle Lab victories offer Next level; the last available level offers Play again. The original empty-catalog checks remain in the test suite.

The runtime creates a persistent uGUI/TMP Canvas with a high sorting order and a scene-owned EventSystem where needed. It uses the project's existing font and simple UI geometry. Gameplay input is blocked while menus are open and for the frame in which gameplay resumes, preventing a UI submit from becoming a jump. Restart and level changes reset time scale and guard against repeated requests. Esc cannot resume a completed or failed attempt.

Integration preserves the team force-based PlayerController and click/F ControllableLight. Input guards also cover PlayerBatteryInteraction and PlayerThrowableLight, with queued jump/dash input cleared while menus block gameplay. Death/victory components notify the menus once. Their existing gameplay behavior and text remain. The menu owns its separate assembly, scene, tests and editor setup tool under Johnny's folders.

The editor helper `Tools > Johnny UI > Create or update menu scene` recreates the menu scene and puts it first in build settings. It does not save the tutorial. The UI is generated at runtime, so its scene contains only a camera.

Play Mode tests are in `Assets/Johnny/Tests/PlayMode/GameMenuTests.cs`. They load the actual tutorial and exercise Play/Pause/Resume/Restart/Menu buttons, empty catalog, invalid scene handling, keyboard focus, final-level behavior, and the tutorial's death/victory components. Migration reports are saved under `tmp/MigrationReview`. Windows preview builds are local only, under `tmp/UIBuild`; the project's WebGL module is not installed, so a new WebGL build has not been validated.

Original prototype verification on October 10, 2026 (before migration to Lightfooted): all 11 Play Mode tests passed and the Windows development build succeeded. Desktop checks confirmed initial menu rendering, Controls/Back/Esc, tutorial loading, the actual game-over screen, restart, and returning to level selection. The tutorial scene's SHA-256 is unchanged. The tests cover pause/resume and victory directly; the desktop check did not play through the complete tutorial.

For the GDD External References, record Codex assistance with UI implementation and verification, and Unity's uGUI, TMP, scene-loading and Test Framework documentation. No new external art or assets were added.

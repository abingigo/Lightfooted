# WebGL analytics

Tracks level time, progress, and outcomes for issue #17. The collector stores events in SQLite. Open the dashboard to see graphs and export a CSV.

## Run it locally

1. Open the project in Unity **6000.3.22f1** with **Web Build Support** installed.
2. Select **Tools > Johnny Analytics > Build Alpha WebGL**. The build goes to `tmp/AnalyticsWebReview/alpha/play/`.
3. From the project folder, run this with Python 3.10 or newer:

```powershell
python -B Tools/Analytics/collector.py serve --database tmp/AnalyticsWebReview/alpha.sqlite3 --web-root tmp/AnalyticsWebReview/alpha/play
```

4. Open the game at `http://127.0.0.1:8765/play/` and the graphs at `http://127.0.0.1:8765/analytics/`.
5. Play, pause, restart, or finish a level. The graphs refresh every five seconds. Use the build and level filters to separate test runs.

Players only need a browser once the collector is hosted.

## What it records

- Starts and outcomes: completed, died, restarted, left the level, or quit.
- Active seconds exclude pauses, loading, menus, and time when the game has no focus. Elapsed seconds include that time.
- Progress markers record the first player body contact per attempt. They do not change respawn. Only AnalyticsLab has new markers so far.
- Events use random IDs, a level path, build version, platform, and UTC time. No names, emails, or device IDs.

The browser saves pending events in local storage and retries delivery. The collector checks event IDs, so retries do not add rows. If storage is full or blocked, events stay in memory and the game continues. A closed browser can lose those memory-only events. A page-exit beacon tries to send the last batch; an interrupted run can remain unfinished.

The graphs show completion rate, median active completion time, checkpoint reach rate, and death/restart rates. All started attempts count in the rate denominators, including unfinished runs. Time uses completed attempts only. Missing checkpoint observations stay absent. Attempts are not unique players.

## Check it

```powershell
python -B -m unittest discover -s Tools/Analytics -v
```

Run the Unity PlayMode tests too. The existing obstacle tests need ObstacleLab and LightPlatformLab enabled temporarily in Build Profiles; restore their original states afterward.

For browser QA, use `AnalyticsWebBuild.BuildQa` as the Unity batch entry point. Serve `tmp/AnalyticsWebReview/qa/play/`, then open `/play/?analytics-qa=route`. Focus the canvas to start. This development-only route crosses two markers, pauses, jumps a hazard, and finishes. The Alpha build excludes this scene and runner.

Checked on 2026-10-19: 18 Python tests passed; 59 Unity tests passed, with one opt-in offline test skipped. The release WebGL build succeeded. A development WebGL run in Chrome sent checkpoint, timing, and completion events to SQLite and the graphs.

Still needed before publishing: release-build browser review, browser offline/reload recovery, and delivery from the real game host to the HTTPS collector. The queue and cross-origin request rules have automated tests, but those do not replace the browser checks.

## Team handoff

This branch builds on the assembly fix in PR #24. Merge that fix first, then change this PR's base to `master` before merging analytics.

The local default sends to `/api/events` on the game host. GitHub Pages cannot run the collector. Set up an HTTPS collector and configure its URL before publishing; see [hosting steps](Tools/Analytics/deploy/README.md). Keep databases, logs, and generated builds out of Git.

[The GDD draft](Analytics_Alpha_GDD_Section.md) has four metric plans and labeled mockups. Add the approved text, public playable/dashboard links, and a real WebGL data screenshot to the shared GDD. The [Alpha rubric](https://docs.google.com/document/d/1JlAzUVhyM431zfTUHfOTiCGPoa32RnDv/edit) requires one metric collected directly from WebGL and a way to graph it. Local testing alone does not finish the online submission.

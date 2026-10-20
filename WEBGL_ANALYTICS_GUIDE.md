# Analytics

We're using the team's Firebase / Google Analytics setup. No Docker setup or player file uploads needed.

## What's added

- **Completion time:** `active_seconds` on `level_completed`. It counts active play during the successful attempt, without pauses, loading, menus, or background time. Earlier failed attempts aren't included.
- **Manual restarts:** `level_restarted` when someone restarts an unfinished run. Trying again after Game Over or victory doesn't count as abandoning a run.
- `level_started` gives us the number of attempts. All four events include `level` and `build_version`.

The coin and falling-death events are separate teammate work. The old download code stays as a backup, but it doesn't start or show buttons in normal builds.

## See the graphs

The teammate with Analytics access needs to set this up once:

1. In **Admin > Custom definitions**, add event dimensions for `level` and `build_version`.
2. Add a custom metric for `active_seconds`, using seconds.
3. In **Explore**, group by level and filter to the same build.
4. For completion time, filter to `level_completed`. Use average active seconds, or total active seconds / completion count. The total alone isn't an average.
5. For restart rate, use `level_restarted` count / `level_started` count x 100. No starts means no measured rate.

Use Realtime or DebugView to check incoming events. Normal reports can take 24-48 hours. Older events won't have the new fields. We still need the property owner to confirm the live graphs.

## Build it

Use Unity **6000.3.22f1** with Web Build Support. **Tools > Johnny Analytics > Build Alpha WebGL** builds to `tmp/FirebaseAnalyticsReview/alpha/play/`. Rebuild and publish after review.

For a local preview, run this from the project folder:

```powershell
python -m http.server 8878 --bind 127.0.0.1 --directory tmp/FirebaseAnalyticsReview
```

Open <http://127.0.0.1:8878/alpha/play/>. Testers using the published game don't need Python.

Developer check: `AnalyticsWebBuild.BuildQa` builds the QA version. Open `/qa/play/?analytics-qa=route&analytics-capture=1` to see actual game calls locally without sending QA data to Google. The capture page and route runner aren't included in release builds. Don't commit local test files or generated builds.

[The GDD draft](Analytics_Alpha_GDD_Section.md) has the four metric plans and mockups. [Google's parameter setup guide](https://firebase.google.com/docs/analytics/web/events) explains the custom definitions.

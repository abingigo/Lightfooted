# Playtest graphs

No server or Docker setup needed for this version. The game saves data in the browser, and we collect the downloaded files from testers.

## How to use it

1. Play a level, then finish, die, restart, or go back to level select.
2. Click **Download playtest data** below the game. Save the JSON file and send it to the team.
3. Click **Open playtest graphs** and choose that file. You can import several testers' files together.
4. Use the build and level filters to check the results. You can also download the tables as CSV files.

The graphs show completion rate, active completion time, checkpoint reach rate, and deaths/restarts. Importing the same file again won't double-count it. Bad files won't replace results you've already imported.

You can save the graph viewer as one HTML file and use it offline. Reimport the JSON files when you reopen it.

## Build it

Use Unity **6000.3.22f1** with Web Build Support installed.

**Tools > Johnny Analytics > Build Alpha WebGL** puts the build in `tmp/DownloadAnalyticsReview/alpha/play/`. Normal Unity WebGL builds include the download buttons and viewer too.

We need to rebuild and publish the Alpha for testers to get these buttons. Changing the old build's config alone won't add them. Keep the earlier assignment builds.

For a local check, run this from the project folder:

```powershell
python -m http.server 8877 --bind 127.0.0.1 --directory tmp/DownloadAnalyticsReview
```

Then open <http://127.0.0.1:8877/alpha/play/>. This just serves the build files. Testers using the published game don't need Python.

## A few things to know

- Download before closing the game. Private browsing or blocked storage can lose saved data.
- Downloading keeps the saved events. During play, it also pauses the game without ending the run.
- Unfinished attempts count in completion rates. Completion time only uses successful runs and excludes paused/background time.
- Analytics markers are currently in AnalyticsLab. Team respawn checkpoints don't automatically record analytics. Deaths here mean Game Over, not every respawn.
- Keep test runs separate from peer playtests. Don't commit player files or generated builds.
- The four metric plans and mockups for the GDD are in [Analytics_Alpha_GDD_Section.md](Analytics_Alpha_GDD_Section.md).

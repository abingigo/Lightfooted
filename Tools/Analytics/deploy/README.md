# Host the analytics collector

Use [the local guide](../../../WEBGL_ANALYTICS_GUIDE.md) first. Public playtests need an HTTPS collector with persistent storage. GitHub Pages serves the game only.

## Set up the server

The Docker option below includes the release build. Build it in Unity first, then run these from the project folder:

```text
docker build -f Tools/Analytics/deploy/Dockerfile -t lightfooted-analytics .
docker run --name lightfooted-analytics -p 127.0.0.1:8765:8765 -v lightfooted-playtests:/data lightfooted-analytics
```

Put an HTTPS reverse proxy in front of port 8765. Forward `/api/` and `/analytics/`, preserve the Host header, and allow request bodies up to 256 KiB. Keep `/data` on persistent storage. Back up `playtest.sqlite3` with the collector stopped. The Docker setup still needs testing on the chosen host.

The image allows browser requests from `https://abingigo.github.io`. Change `--allow-origin` in the Dockerfile if the game uses another origin. Keep it exact, without a URL path. These are anonymous classroom events; use the host's normal request limits.

## Connect the game

Replace `YOUR-ANALYTICS-HOST` below with the real HTTPS host:

```text
python -B Tools/Analytics/configure_web.py --build tmp/AnalyticsWebReview/alpha/play --endpoint https://YOUR-ANALYTICS-HOST/api/events --dataset alpha-2026-10-20
```

This updates the built game's public config and dashboard link. No keys belong in this config. Use a separate database and dataset for QA. Clear or export old QA queues before switching studies.

Publish the game to the team's Alpha path on GitHub Pages. Keep the earlier progress-check build available.

## Before the team submits

1. Open the public game from another browser or computer. Its analytics requests must use the real host, not localhost.
2. Finish or restart a level. Check for `WebGLPlayer` events and updated graphs with the right build filter.
3. Pause and switch tabs. Active time must stop. Restart must start a new attempt.
4. Stop the collector, play, reload, then start it again. Check that saved events arrive without duplicate rows.
5. Add the playable link, dashboard link, metric plans, method, and a real data screenshot to the GDD. Keep QA runs separate from peer playtests.

No public collector is configured yet. These checks remain part of the hosting handoff.

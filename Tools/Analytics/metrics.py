"""Aggregate design metrics. Incomplete attempts stay in rate denominators."""
from collections import defaultdict
from statistics import median
from collector import connect


def calculate(database, level=None, build=None):
    filters, values = [], []
    for key, value in (("level_id", level), ("build_version", build)):
        if value:
            filters.append(key + "=?")
            values.append(value)
    with connect(database) as db:
        available = db.execute("SELECT DISTINCT level_id, build_version FROM events ORDER BY level_id, build_version").fetchall()
        rows = db.execute("SELECT * FROM events" + (" WHERE " + " AND ".join(filters) if filters else "") + " ORDER BY attempt_id, sequence", values).fetchall()
    attempts = defaultdict(list)
    for row in rows:
        attempts[row["attempt_id"]].append(dict(row))
    levels = defaultdict(list)
    checkpoints = defaultdict(list)
    for events in attempts.values():
        # Retries can arrive out of order. Wait for the start before counting an attempt.
        if not any(item["event_type"] == "level_started" for item in events):
            continue
        levels[events[0]["level_id"]].append(events)
        for item in events:
            if item["event_type"] == "checkpoint_reached":
                checkpoints[(item["level_id"], item["checkpoint_id"])].append(item)
    level_metrics = []
    for name, entries in sorted(levels.items()):
        ends = [next((item for item in events if item["event_type"] == "level_ended"), None) for events in entries]
        complete = [item for item in ends if item and item["outcome"] == "completed"]
        deaths = sum(bool(item and item["outcome"] == "died") for item in ends)
        restarts = sum(bool(item and item["outcome"] == "restarted") for item in ends)
        starts = len(entries)
        level_metrics.append(dict(level_id=name, starts=starts, completions=len(complete),
                                  completion_rate=100 * len(complete) / starts,
                                  median_active_completion_seconds=median(item["active_seconds"] for item in complete) if complete else None,
                                  deaths=deaths, restarts=restarts, death_rate=100 * deaths / starts,
                                  restart_rate=100 * restarts / starts, unfinished=sum(item is None for item in ends)))
    checkpoint_metrics = []
    for (name, checkpoint), events in sorted(checkpoints.items()):
        checkpoint_metrics.append(dict(level_id=name, checkpoint_id=checkpoint, reaches=len(events),
                                       reach_rate=100 * len(events) / len(levels[name]),
                                       median_split_seconds=median(item["split_seconds"] for item in events),
                                       median_arrival_seconds=median(item["active_seconds"] for item in events),
                                       route_order=median(item["sequence"] for item in events)))
    checkpoint_metrics.sort(key=lambda item: (item["level_id"], item["route_order"], item["checkpoint_id"]))
    return dict(schema_version=1, event_count=len(rows), attempt_count=sum(len(items) for items in levels.values()),
                builds=sorted({item["build_version"] for item in available}),
                levels=sorted({item["level_id"] for item in available}), level_metrics=level_metrics,
                checkpoint_metrics=checkpoint_metrics,
                definitions={"rates": "Percent of started attempts; unfinished attempts remain in the denominator.",
                             "time": "Median active seconds among completed attempts only. Pause/background time is excluded.",
                             "checkpoint": "First arrival per checkpoint per attempt. Only observed checkpoints are shown; an absent marker is not a measured zero.",
                             "failure": "Death and manual restart are separate terminal outcomes. Quits/leaving are not deaths."})

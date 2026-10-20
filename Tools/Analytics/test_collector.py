import copy
import json
import sqlite3
import tempfile
import threading
import unittest
import urllib.error
import urllib.request
from pathlib import Path
from collector import ingest, make_server, summary


def event(kind="level_started", sequence=0, seconds=0):
    return dict(event_id=f"event-{sequence}", session_id="session", attempt_id="attempt",
                sequence=sequence, schema_version=1, event_type=kind, level_id="AnalyticsLab",
                checkpoint_id="gate-a" if kind == "checkpoint_reached" else "",
                outcome="completed" if kind == "level_ended" else "", active_seconds=seconds,
                elapsed_seconds=seconds + (2 if seconds else 0), split_seconds=seconds,
                recorded_utc="2026-10-18T12:00:00.0000000Z", build_version="test", platform="test")


class CollectorTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.database = str(Path(self.temp.name) / "playtest.sqlite3")

    def tearDown(self):
        self.temp.cleanup()

    def test_persistence_retry_and_summary(self):
        batch = [event(), event("checkpoint_reached", 1, 3), event("level_ended", 2, 7)]
        self.assertEqual(ingest(self.database, batch), [e["event_id"] for e in batch])
        ingest(self.database, batch)
        report = summary(self.database)
        self.assertEqual(report["events"], 3)
        self.assertEqual(report["attempts"][0]["outcome"], "completed")
        self.assertEqual(report["attempts"][0]["active_seconds"], 7)
        self.assertEqual(report["checkpoints"][0]["reached_attempts"], 1)

    def test_invalid_batch_is_atomic(self):
        bad = event("level_ended", 1, 1)
        bad["active_seconds"] = float("nan")
        with self.assertRaises(ValueError):
            ingest(self.database, [event(), bad])
        self.assertEqual(summary(self.database)["events"], 0)

    def test_conflicting_id_does_not_overwrite(self):
        ingest(self.database, [event()])
        bad = event()
        bad["level_id"] = "changed"
        with self.assertRaises(ValueError):
            ingest(self.database, [bad])
        self.assertEqual(summary(self.database)["attempts"][0]["level_id"], "AnalyticsLab")

    def test_sequence_conflict_rolls_back_whole_batch(self):
        ingest(self.database, [event()])
        good = event("checkpoint_reached", 1, 1)
        bad = copy.deepcopy(good)
        bad["event_id"] = "different-id-same-sequence"
        with self.assertRaises(sqlite3.IntegrityError):
            ingest(self.database, [good, bad])
        self.assertEqual(summary(self.database)["events"], 1)

    def test_text_is_parameterized(self):
        data = event()
        data["level_id"] = "'); DROP TABLE events; --"
        ingest(self.database, [data])
        self.assertEqual(summary(self.database)["events"], 1)

    def test_schema_rejects_wrong_types_and_inconsistent_fields(self):
        for key, value in [("schema_version", 2), ("sequence", True), ("event_type", "unknown"),
                           ("elapsed_seconds", -1), ("recorded_utc", "2026-10-18T12:00:00"),
                           ("checkpoint_id", "unexpected"), ("outcome", "died")]:
            with self.subTest(key=key):
                bad = event()
                bad[key] = value
                with self.assertRaises(ValueError):
                    ingest(self.database, [bad])

    def test_http_ingest_and_invalid_retry(self):
        server = make_server(self.database, 0)
        thread = threading.Thread(target=server.serve_forever, daemon=True)
        thread.start()
        address = f"http://127.0.0.1:{server.server_port}"
        try:
            self.assertEqual(json.load(urllib.request.urlopen(address + "/health"))["status"], "ready")
            body = json.dumps({"events": [event()]}).encode()
            request = urllib.request.Request(address + "/events", body, {"Content-Type": "application/json"})
            for _ in range(2):
                self.assertEqual(json.load(urllib.request.urlopen(request))["accepted_ids"], ["event-0"])
            self.assertEqual(json.load(urllib.request.urlopen(address + "/summary"))["events"], 1)
            bad = urllib.request.Request(address + "/events", b'{}', {"Content-Type": "application/json"})
            with self.assertRaises(urllib.error.HTTPError) as error:
                urllib.request.urlopen(bad)
            self.assertEqual(error.exception.code, 400)
        finally:
            server.shutdown()
            server.server_close()
            thread.join()


if __name__ == "__main__":
    unittest.main(verbosity=2)

import json
import tempfile
import threading
import unittest
import urllib.error
import urllib.request
from pathlib import Path
from collector import ingest, make_server
from metrics import calculate
from test_collector import event


def attempt(name, outcome=None, seconds=10, level="Tutorial", build="alpha", checkpoint=False):
    events = [event()]
    if checkpoint:
        events.append(event("checkpoint_reached", 1, seconds / 2))
    if outcome:
        end = event("level_ended", len(events), seconds)
        end["outcome"] = outcome
        events.append(end)
    for item in events:
        item.update(attempt_id=name, event_id=name + "-" + str(item["sequence"]), level_id=level, build_version=build)
    return events


class MetricTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.database = str(Path(self.temp.name) / "metrics.sqlite3")

    def tearDown(self):
        self.temp.cleanup()

    def test_rates_keep_unfinished_attempts_and_time_uses_completions_only(self):
        for data in (attempt("a", "completed", 10, checkpoint=True), attempt("b", "died", 50, checkpoint=True),
                     attempt("c", "restarted", 20), attempt("d")):
            ingest(self.database, data)
        report = calculate(self.database)
        level = report["level_metrics"][0]
        self.assertEqual(level["starts"], 4)
        self.assertEqual(level["completion_rate"], 25)
        self.assertEqual(level["death_rate"], 25)
        self.assertEqual(level["restart_rate"], 25)
        self.assertEqual(level["median_active_completion_seconds"], 10)
        self.assertEqual(level["unfinished"], 1)
        self.assertEqual(report["checkpoint_metrics"][0]["reach_rate"], 50)

    def test_median_is_not_mean(self):
        for name, seconds in (("a", 10), ("b", 20), ("c", 300)):
            ingest(self.database, attempt(name, "completed", seconds))
        self.assertEqual(calculate(self.database)["level_metrics"][0]["median_active_completion_seconds"], 20)

    def test_missing_completion_is_null_and_unobserved_markers_are_not_invented(self):
        ingest(self.database, attempt("a", "died"))
        report = calculate(self.database)
        self.assertIsNone(report["level_metrics"][0]["median_active_completion_seconds"])
        self.assertEqual(report["checkpoint_metrics"], [])

    def test_level_and_build_filters_do_not_mix_versions(self):
        ingest(self.database, attempt("a", "completed", 10))
        ingest(self.database, attempt("b", "died", 100, level="Level 1", build="old"))
        report = calculate(self.database, "Tutorial", "alpha")
        self.assertEqual(report["attempt_count"], 1)
        self.assertEqual(report["level_metrics"][0]["completion_rate"], 100)
        self.assertEqual(report["builds"], ["alpha", "old"])

    def test_out_of_order_end_waits_for_start_before_counting(self):
        data = attempt("a", "completed", 10)
        ingest(self.database, [data[1]])
        self.assertEqual(calculate(self.database)["attempt_count"], 0)
        ingest(self.database, [data[0]])
        self.assertEqual(calculate(self.database)["level_metrics"][0]["completion_rate"], 100)

    def test_duplicate_delivery_does_not_inflate_rates(self):
        data = attempt("a", "completed", 10, checkpoint=True)
        ingest(self.database, data); ingest(self.database, data)
        report = calculate(self.database)
        self.assertEqual(report["attempt_count"], 1)
        self.assertEqual(report["checkpoint_metrics"][0]["reaches"], 1)

    def test_http_cors_dashboard_assets_metrics_and_traversal(self):
        root = Path(self.temp.name) / "web"
        root.mkdir(); (root / "index.html").write_text("<h1>Test build root</h1>")
        (root / "test.wasm").write_bytes(b"wasm-test")
        server = make_server(self.database, 0, web_root=root, allowed_origins=("https://abingigo.github.io",))
        thread = threading.Thread(target=server.serve_forever, daemon=True); thread.start()
        address = f"http://127.0.0.1:{server.server_port}"
        try:
            request = urllib.request.Request(address + "/api/events", json.dumps({"events": attempt("a", "completed")}).encode(), {"Origin": "https://abingigo.github.io", "Content-Type": "application/json"})
            with urllib.request.urlopen(request) as response:
                self.assertEqual(response.headers["Access-Control-Allow-Origin"], "https://abingigo.github.io")
            preflight = urllib.request.Request(address + "/api/events", method="OPTIONS", headers={"Origin": "https://abingigo.github.io"})
            self.assertEqual(urllib.request.urlopen(preflight).status, 204)
            blocked = urllib.request.Request(address + "/api/events", b'{}', {"Origin": "https://unlisted.example"})
            with self.assertRaises(urllib.error.HTTPError) as error:
                urllib.request.urlopen(blocked)
            self.assertEqual(error.exception.code, 403)
            self.assertEqual(json.load(urllib.request.urlopen(address + "/api/metrics"))["attempt_count"], 1)
            self.assertIn(b"completion_rate", urllib.request.urlopen(address + "/api/metrics.csv").read())
            self.assertIn(b"No gameplay data yet", urllib.request.urlopen(address + "/analytics/").read())
            self.assertIn(b"Test build root", urllib.request.urlopen(address + "/play/").read())
            self.assertEqual(urllib.request.urlopen(address + "/play/test.wasm").headers["Content-Type"], "application/wasm")
            with self.assertRaises(urllib.error.HTTPError):
                urllib.request.urlopen(address + "/play/%2e%2e/metrics.sqlite3")
        finally:
            server.shutdown(); server.server_close(); thread.join()


if __name__ == "__main__":
    unittest.main(verbosity=2)

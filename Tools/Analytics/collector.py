"""Local Lightfooted event collector. Python 3.10+, standard library only."""
import argparse
import csv
import json
import math
import re
import sqlite3
import mimetypes
import io
from datetime import datetime
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from contextlib import contextmanager
from urllib.parse import urlsplit, parse_qs, unquote

FIELDS = ("event_id", "session_id", "attempt_id", "sequence", "schema_version",
          "event_type", "level_id", "checkpoint_id", "outcome", "active_seconds",
          "elapsed_seconds", "split_seconds", "recorded_utc", "build_version", "platform")
OUTCOMES = {"completed", "died", "restarted", "left_level", "quit"}
SCHEMA = """
CREATE TABLE IF NOT EXISTS events (
 event_id TEXT PRIMARY KEY, session_id TEXT NOT NULL, attempt_id TEXT NOT NULL,
 sequence INTEGER NOT NULL, schema_version INTEGER NOT NULL,
 event_type TEXT NOT NULL, level_id TEXT NOT NULL, checkpoint_id TEXT NOT NULL,
 outcome TEXT NOT NULL, active_seconds REAL NOT NULL, elapsed_seconds REAL NOT NULL,
 split_seconds REAL NOT NULL, recorded_utc TEXT NOT NULL,
 build_version TEXT NOT NULL, platform TEXT NOT NULL,
 UNIQUE(attempt_id, sequence)
);
CREATE INDEX IF NOT EXISTS events_level ON events(level_id, event_type);
CREATE UNIQUE INDEX IF NOT EXISTS one_start ON events(attempt_id) WHERE event_type='level_started';
CREATE UNIQUE INDEX IF NOT EXISTS one_end ON events(attempt_id) WHERE event_type='level_ended';
CREATE UNIQUE INDEX IF NOT EXISTS one_checkpoint ON events(attempt_id, checkpoint_id) WHERE event_type='checkpoint_reached';
CREATE VIEW IF NOT EXISTS attempt_summary AS
 SELECT attempt_id, session_id, level_id,
 MAX(CASE WHEN event_type='level_ended' THEN outcome END) AS outcome,
 MAX(active_seconds) AS active_seconds, MAX(elapsed_seconds) AS elapsed_seconds,
 SUM(CASE WHEN event_type='checkpoint_reached' THEN 1 ELSE 0 END) AS checkpoints
 FROM events GROUP BY attempt_id, session_id, level_id;
CREATE VIEW IF NOT EXISTS checkpoint_summary AS
 SELECT level_id, checkpoint_id, COUNT(*) AS reached_attempts,
 AVG(active_seconds) AS mean_arrival_seconds, AVG(split_seconds) AS mean_split_seconds
 FROM events WHERE event_type='checkpoint_reached' GROUP BY level_id, checkpoint_id;
"""


@contextmanager
def connect(database):
    Path(database).parent.mkdir(parents=True, exist_ok=True)
    db = sqlite3.connect(database, timeout=5)
    db.row_factory = sqlite3.Row
    try:
        db.executescript(SCHEMA)
        with db:
            yield db
    finally:
        db.close()


def validate(event):
    if not isinstance(event, dict) or set(event) != set(FIELDS):
        raise ValueError("Event fields do not match schema version 1")
    for key in FIELDS:
        if key in {"sequence", "schema_version", "active_seconds", "elapsed_seconds", "split_seconds"}:
            continue
        value = event[key]
        if not isinstance(value, str) or len(value) > 512:
            raise ValueError("Invalid text field: " + key)
    for key in ("event_id", "session_id", "attempt_id", "level_id", "recorded_utc"):
        if not event[key].strip():
            raise ValueError("Missing " + key)
    if type(event["sequence"]) is not int or event["sequence"] < 0:
        raise ValueError("Invalid sequence")
    if type(event["schema_version"]) is not int or event["schema_version"] != 1:
        raise ValueError("Unsupported schema version")
    for key in ("active_seconds", "elapsed_seconds", "split_seconds"):
        value = event[key]
        if type(value) not in (int, float) or not math.isfinite(value) or value < 0:
            raise ValueError("Invalid time: " + key)
    if event["split_seconds"] > event["active_seconds"] + 0.001 or event["active_seconds"] > event["elapsed_seconds"] + 0.001:
        raise ValueError("Inconsistent times")
    # .NET round-trip timestamps have seven fractional digits; Python 3.10 accepts six.
    timestamp_text = re.sub(r"(\.\d{6})\d+(?=(?:Z|[+-]\d{2}:\d{2})$)", r"\1", event["recorded_utc"])
    timestamp = datetime.fromisoformat(timestamp_text.replace("Z", "+00:00"))
    if timestamp.utcoffset() is None or timestamp.utcoffset().total_seconds() != 0:
        raise ValueError("Timestamp must use UTC")
    kind = event["event_type"]
    if kind not in {"level_started", "checkpoint_reached", "level_ended"}:
        raise ValueError("Unknown event type")
    if (kind == "checkpoint_reached") != bool(event["checkpoint_id"].strip()):
        raise ValueError("Checkpoint ID does not match event type")
    if kind == "level_ended":
        if event["outcome"] not in OUTCOMES:
            raise ValueError("Invalid outcome")
    elif event["outcome"]:
        raise ValueError("Outcome only belongs to level_ended")
    if kind == "level_started" and (event["sequence"] != 0 or event["elapsed_seconds"] != 0):
        raise ValueError("Level start must have sequence and time zero")


def ingest(database, events):
    if not isinstance(events, list) or not 1 <= len(events) <= 32:
        raise ValueError("Expected 1 to 32 events")
    for event in events:
        validate(event)
    with connect(database) as db:
        for event in events:
            previous = db.execute("SELECT * FROM events WHERE event_id=?", (event["event_id"],)).fetchone()
            if previous is not None:
                if dict(previous) != event:
                    raise ValueError("Event ID reused with different data")
                continue
            context = db.execute("SELECT session_id, level_id, build_version, platform FROM events WHERE attempt_id=? LIMIT 1", (event["attempt_id"],)).fetchone()
            if context and any(context[key] != event[key] for key in context.keys()):
                raise ValueError("Attempt metadata cannot change")
            db.execute("INSERT INTO events (" + ",".join(FIELDS) + ") VALUES (" + ",".join("?" for _ in FIELDS) + ")",
                       tuple(event[key] for key in FIELDS))
    return [event["event_id"] for event in events]


def summary(database):
    with connect(database) as db:
        return {
            "events": db.execute("SELECT COUNT(*) FROM events").fetchone()[0],
            "attempts": [dict(row) for row in db.execute("SELECT * FROM attempt_summary ORDER BY level_id, attempt_id")],
            "checkpoints": [dict(row) for row in db.execute("SELECT * FROM checkpoint_summary ORDER BY level_id, checkpoint_id")],
        }


def make_server(database, port=8765, host="127.0.0.1", web_root=None, allowed_origins=()):
    with connect(database):
        pass

    class Handler(BaseHTTPRequestHandler):
        def setup(self):
            super().setup()
            self.connection.settimeout(15)

        def allowed_origin(self):
            origin = self.headers.get("Origin")
            if not origin:
                return True
            return origin in allowed_origins or (urlsplit(origin).scheme in {"http", "https"} and urlsplit(origin).netloc == self.headers.get("Host"))

        def send_headers(self, status, kind, length):
            self.send_response(status)
            self.send_header("Content-Type", kind)
            self.send_header("Content-Length", str(length))
            self.send_header("X-Content-Type-Options", "nosniff")
            self.send_header("Cache-Control", "no-store")
            origin = self.headers.get("Origin")
            if origin and self.allowed_origin():
                self.send_header("Access-Control-Allow-Origin", origin)
                self.send_header("Vary", "Origin")
            self.end_headers()

        def reply(self, status, payload):
            body = json.dumps(payload, allow_nan=False).encode("utf-8")
            self.send_headers(status, "application/json", len(body))
            self.wfile.write(body)

        def do_OPTIONS(self):
            if not self.allowed_origin():
                self.reply(403, {"error": "Origin is not allowed"})
                return
            self.send_response(204)
            self.send_header("Access-Control-Allow-Origin", self.headers.get("Origin", ""))
            self.send_header("Access-Control-Allow-Methods", "GET, POST, OPTIONS")
            self.send_header("Access-Control-Allow-Headers", "Content-Type")
            self.send_header("Vary", "Origin")
            self.send_header("Content-Length", "0")
            self.end_headers()

        def serve_file(self, root, relative):
            root = Path(root).resolve()
            path = (root / unquote(relative)).resolve()
            if not path.is_relative_to(root) or not path.is_file():
                self.reply(404, {"error": "File unavailable"})
                return
            kind = {".wasm": "application/wasm", ".js": "text/javascript", ".json": "application/json", ".unityweb": "application/octet-stream"}.get(path.suffix, mimetypes.guess_type(str(path))[0] or "application/octet-stream")
            self.send_headers(200, kind, path.stat().st_size)
            with path.open("rb") as stream:
                while chunk := stream.read(1024 * 1024):
                    self.wfile.write(chunk)

        def do_GET(self):
            path = urlsplit(self.path).path
            if not self.allowed_origin():
                self.reply(403, {"error": "Origin is not allowed"})
                return
            if path in {"/health", "/api/health"}:
                self.reply(200, {"status": "ready", "schema_version": 1})
            elif path == "/summary":
                self.reply(200, summary(database))
            elif path in {"/api/metrics", "/api/metrics.csv"}:
                from metrics import calculate
                query = parse_qs(urlsplit(self.path).query)
                report = calculate(database, query.get("level", [None])[0], query.get("build", [None])[0])
                if path.endswith(".csv"):
                    stream = io.StringIO(newline="")
                    fields = ["level_id", "starts", "completions", "completion_rate", "median_active_completion_seconds", "deaths", "restarts", "death_rate", "restart_rate", "unfinished"]
                    writer = csv.DictWriter(stream, fieldnames=fields); writer.writeheader(); writer.writerows(report["level_metrics"])
                    body = stream.getvalue().encode("utf-8")
                    self.send_headers(200, "text/csv; charset=utf-8", len(body)); self.wfile.write(body)
                else:
                    self.reply(200, report)
            elif path == "/" or path.startswith("/analytics/"):
                self.serve_file(Path(__file__).parent / "dashboard", "index.html" if path in {"/", "/analytics/"} else path[len("/analytics/"):])
            elif path.startswith("/play/") and web_root:
                self.serve_file(web_root, "index.html" if path == "/play/" else path[len("/play/"):])
            else:
                self.reply(404, {"error": "Unknown route"})

        def do_POST(self):
            if not self.allowed_origin():
                self.reply(403, {"error": "Origin is not allowed"}); return
            if self.path not in {"/events", "/api/events"}:
                self.reply(404, {"error": "Unknown route"})
                return
            try:
                length = int(self.headers.get("Content-Length", "0"))
                if not 0 < length <= 262144:
                    self.reply(413, {"error": "Invalid body size"})
                    return
                payload = json.loads(self.rfile.read(length))
                if not isinstance(payload, dict) or set(payload) != {"events"}:
                    raise ValueError("Expected events object")
                self.reply(200, {"accepted_ids": ingest(database, payload["events"])})
            except (ValueError, UnicodeError, sqlite3.IntegrityError) as error:
                self.reply(400, {"error": str(error)})
            except sqlite3.Error:
                self.reply(503, {"error": "Database unavailable; retain queued events"})

        def log_message(self, *_):
            pass

    return ThreadingHTTPServer((host, port), Handler)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("command", choices=("serve", "summary", "export"))
    parser.add_argument("--database", default="tmp/Analytics/playtest.sqlite3")
    parser.add_argument("--port", type=int, default=8765)
    parser.add_argument("--out", default="tmp/Analytics/events.csv")
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--web-root", help="Directory containing a Unity Web build's index.html")
    parser.add_argument("--allow-origin", action="append", default=[], help="Exact additional browser origin, e.g. https://abingigo.github.io")
    args = parser.parse_args()
    if args.command == "serve":
        server = make_server(args.database, args.port, args.host, args.web_root, tuple(args.allow_origin))
        print(f"Analytics: http://{args.host}:{server.server_port}/analytics/ | {Path(args.database).resolve()}", flush=True)
        try:
            server.serve_forever()
        except KeyboardInterrupt:
            pass
        finally:
            server.server_close()
    elif args.command == "summary":
        print(json.dumps(summary(args.database), indent=2))
    else:
        Path(args.out).parent.mkdir(parents=True, exist_ok=True)
        with connect(args.database) as db, open(args.out, "w", newline="", encoding="utf-8") as stream:
            writer = csv.writer(stream)
            writer.writerow(FIELDS)
            writer.writerows(db.execute("SELECT " + ",".join(FIELDS) + " FROM events ORDER BY recorded_utc, attempt_id, sequence"))
        print(Path(args.out).resolve())


if __name__ == "__main__":
    main()

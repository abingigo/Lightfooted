import json
import tempfile
import unittest
from pathlib import Path
from configure_web import configure


class WebConfigurationTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        (self.root / "index.html").write_text('<a href="/analytics/">Graph</a>')
        (self.root / "StreamingAssets").mkdir()
        self.config = self.root / "StreamingAssets" / "analytics-config.json"
        self.config.write_text('{}')

    def tearDown(self):
        self.temp.cleanup()

    def test_https_configuration_is_public_data_without_secrets(self):
        configure(self.root, "https://analytics.example/api/events", "alpha-2026")
        self.assertEqual(json.loads(self.config.read_text()), dict(mode="server", endpoint="https://analytics.example/api/events", dataset="alpha-2026"))
        self.assertIn('href="https://analytics.example/analytics/"', (self.root / "index.html").read_text())

    def test_loopback_http_is_allowed_for_review(self):
        configure(self.root, "http://127.0.0.1:8765/api/events", "qa")
        self.assertIn("127.0.0.1", self.config.read_text())

    def test_remote_http_credentials_tokens_and_invalid_datasets_are_rejected(self):
        for address in ("http://analytics.example/api/events", "https://user:secret@analytics.example/api/events", "https://analytics.example/api/events?token=secret", "javascript:alert(1)"):
            with self.subTest(address=address), self.assertRaises(ValueError):
                configure(self.root, address, "qa")
        with self.assertRaises(ValueError):
            configure(self.root, "https://analytics.example/api/events", "../bad")
        self.assertEqual(self.config.read_text(), '{}')

    def test_missing_build_is_rejected(self):
        with self.assertRaises(ValueError):
            configure(self.root / "missing", "https://analytics.example/api/events", "qa")

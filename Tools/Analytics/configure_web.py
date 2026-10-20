"""Set the public collector URL in an already built WebGL package."""
import argparse
import json
import re
from html import escape
from pathlib import Path
from urllib.parse import urlsplit


def configure(build, endpoint, dataset):
    address = urlsplit(endpoint)
    if not (address.scheme == "https" and address.netloc) and not (address.scheme == "http" and address.hostname in {"localhost", "127.0.0.1", "::1"}):
        raise ValueError("Use an HTTPS collector URL, or HTTP loopback for local testing")
    if address.username or address.password or address.query or address.fragment:
        raise ValueError("The public endpoint must not contain credentials, query tokens, or fragments")
    if not re.fullmatch(r"[a-zA-Z0-9_-]{1,64}", dataset):
        raise ValueError("Dataset must contain 1 to 64 letters, numbers, underscores or hyphens")
    root = Path(build).resolve()
    path = root / "StreamingAssets" / "analytics-config.json"
    if not (root / "index.html").is_file() or not path.is_file():
        raise ValueError("Expected the directory containing the completed WebGL index.html and StreamingAssets")
    path.write_text(json.dumps(dict(mode="server", endpoint=endpoint, dataset=dataset), indent=2) + "\n", encoding="utf-8")
    page = root / "index.html"
    dashboard = address.scheme + "://" + address.netloc + "/analytics/"
    page.write_text(page.read_text(encoding="utf-8").replace('href="/analytics/"', 'href="' + escape(dashboard, quote=True) + '"'), encoding="utf-8")
    return path


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--build", required=True)
    parser.add_argument("--endpoint", required=True)
    parser.add_argument("--dataset", default="alpha-2026-10-20")
    args = parser.parse_args()
    print(configure(args.build, args.endpoint, args.dataset))

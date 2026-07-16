from __future__ import annotations

import argparse
import http.server
import os
from pathlib import Path


class WebGLRequestHandler(http.server.SimpleHTTPRequestHandler):
    extensions_map = {
        **http.server.SimpleHTTPRequestHandler.extensions_map,
        ".wasm": "application/wasm",
        ".data": "application/octet-stream",
        ".js": "application/javascript",
    }

    def guess_type(self, path: str) -> str:
        if path.endswith(".br"):
            path = path[:-3]
        return super().guess_type(path)

    def end_headers(self) -> None:
        request_path = self.path.split("?", 1)[0]
        if request_path.endswith(".br"):
            self.send_header("Content-Encoding", "br")
        if request_path.endswith("index.html") or request_path == "/":
            self.send_header("Cache-Control", "no-cache")
        else:
            self.send_header("Cache-Control", "public, max-age=31536000, immutable")
        super().end_headers()


def main() -> None:
    parser = argparse.ArgumentParser(description="Serve a Unity WebGL build with Brotli and MIME headers.")
    parser.add_argument("directory", type=Path)
    parser.add_argument("--port", type=int, default=8000)
    args = parser.parse_args()

    directory = args.directory.resolve()
    if not (directory / "index.html").is_file():
        raise SystemExit(f"index.html not found in {directory}")

    os.chdir(directory)
    server = http.server.ThreadingHTTPServer(("127.0.0.1", args.port), WebGLRequestHandler)
    print(f"Serving {directory} at http://127.0.0.1:{args.port}", flush=True)
    server.serve_forever()


if __name__ == "__main__":
    main()

#!/usr/bin/env python3
import argparse
import os
from http.server import HTTPServer, SimpleHTTPRequestHandler

BUILD_DIR = os.path.join(
    os.path.dirname(os.path.abspath(__file__)),
    "UberStrike.Unity", "Latest", "WebPlayer",
)


class UnityWebGLHandler(SimpleHTTPRequestHandler):
    extensions_map = {
        **SimpleHTTPRequestHandler.extensions_map,
        ".wasm": "application/wasm",
        ".js": "application/javascript",
        ".data": "application/octet-stream",
        ".unity3d": "application/octet-stream",
        ".mem": "application/octet-stream",
        ".br": "application/octet-stream",
        ".gz": "application/octet-stream",
        ".json": "application/json",
        ".xml": "application/xml",
        ".ico": "image/x-icon",
        ".svg": "image/svg+xml",
    }

    def end_headers(self):
        self.send_header("Cross-Origin-Opener-Policy", "same-origin")
        self.send_header("Cross-Origin-Embedder-Policy", "require-corp")
        self.send_header("Cache-Control", "no-cache")
        super().end_headers()

    def log_message(self, format, *args):
        print(f"[{self.log_date_time_string()}] {format % args}")


def main():
    parser = argparse.ArgumentParser(description="Serve the UberStrike Unity WebGL build.")
    parser.add_argument("--dir", default=BUILD_DIR, help="WebGL build directory")
    parser.add_argument("--host", default="0.0.0.0")
    parser.add_argument("--port", type=int, default=8000)
    args = parser.parse_args()

    os.chdir(args.dir)
    server = HTTPServer((args.host, args.port), UnityWebGLHandler)
    print(f"Serving {os.path.abspath(args.dir)} at http://localhost:{args.port}")
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\nStopped.")


if __name__ == "__main__":
    main()

#!/usr/bin/env python3
import argparse
import json
import os
import time
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer

BUILD_DIR = os.path.join(
    os.path.dirname(os.path.abspath(__file__)),
    "UberStrike.Unity", "Latest", "WebPlayer",
)

CLIENT_LOG = "/tmp/opencode/client-logs.log"


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

    protocol_version = "HTTP/1.1"

    def do_GET(self):
        import time
        start = time.time()
        self._start = start
        super().do_GET()

    def do_POST(self):
        if self.path != "/__log":
            self.send_error(404)
            return
        length = int(self.headers.get("Content-Length", 0))
        body = self.rfile.read(length)
        try:
            entries = json.loads(body)
        except Exception:
            entries = [{"raw": body.decode(errors="replace")}]
        with open(CLIENT_LOG, "a") as f:
            for e in entries:
                line = json.dumps(e, ensure_ascii=False)
                f.write(line + "\n")
                print(f"[client:{self.address_string()}] {line}", flush=True)
        self.send_response(200)
        self.send_header("Content-Type", "application/json")
        self.end_headers()
        self.wfile.write(b'{"ok":true}')

    def log_request(self, code="-", size="-"):
        import time
        start = getattr(self, "_start", None)
        dur = f"{time.time() - start:6.1f}s" if start else "      "
        print(f"[{self.log_date_time_string()}] {dur} {code} {size:>9} {self.address_string()} {self.requestline}", flush=True)

    def copyfile(self, source, outputfile):
        import shutil
        import time
        t0 = time.time()
        shutil.copyfileobj(source, outputfile)
        n = getattr(outputfile, "_written", None)
        print(f"    body done in {time.time() - t0:.1f}s", flush=True)

    def send_response(self, code, message=None):
        self._bytes = 0
        super().send_response(code, message)

    def log_error(self, format, *args):
        print(f"[{self.log_date_time_string()}] ERROR {self.address_string()} {format % args}", flush=True)


def main():
    parser = argparse.ArgumentParser(description="Serve the UberStrike Unity WebGL build.")
    parser.add_argument("--dir", default=BUILD_DIR, help="WebGL build directory")
    parser.add_argument("--host", default="0.0.0.0")
    parser.add_argument("--port", type=int, default=8000)
    args = parser.parse_args()

    class Server(ThreadingHTTPServer):
        daemon_threads = True
        allow_reuse_address = True

    os.chdir(args.dir)
    server = Server((args.host, args.port), UnityWebGLHandler)
    print(f"Serving {os.path.abspath(args.dir)} at http://localhost:{args.port}")
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\nStopped.")


if __name__ == "__main__":
    main()

#!/usr/bin/env python3
import argparse
import json
import os
import re
import time
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer

BUILD_DIR = os.path.join(
    os.path.dirname(os.path.abspath(__file__)),
    "UberStrike.Unity", "Latest", "WebPlayer",
)

CLIENT_LOG = "/tmp/opencode/client-logs.log"
MAX_LOG_BODY = 64 * 1024
MAX_LOG_LINES = 50
MAX_LINE = 500

SCRUB = [
    (re.compile(r"eyJ[\w-]+\.[\w-]+\.[\w-]*"), "<jwt>"),
    (re.compile(r"(?i)\bbearer\s+\S+"), "Bearer <redacted>"),
    (re.compile(r'(?i)("?\b\w*(?:token|passw\w*|pwd|secret|key|(?<!dev)auth\w*|ticket|session\w*|cookie|credential\w*)"?\s*[:=]\s*)("[^"]*"|[^\s,;&}]+)'), r"\1<redacted>"),
    (re.compile(r'dev:\d+:\d+:[^"\r\n,)]*'), "<devtoken>"),
    (re.compile(r"[\w.+-]+@[\w-]+(?:\.[\w-]+)+"), "<email>"),
    (re.compile(r'(https?://[^\s?"#]+)\?[^\s"#]*'), r"\1?<redacted>"),
    (re.compile(r"\b7656119\d{10}\b"), "<steamid>"),
    (re.compile(r"(?i)(\bcmid\s*[:=#]?\s*)\d+"), r"\1<n>"),
    (re.compile(r"(?i)(identity changed )\d+ -> \d+"), r"\1<n> -> <n>"),
    (re.compile(r"[A-Za-z0-9+/_-]{32,}={0,2}"), "<redacted>"),
]
IPV4 = re.compile(r"\b(\d{1,3})\.(\d{1,3})\.(\d{1,3})\.(\d{1,3})\b")


def scrub(msg, page_host):
    s = msg[:MAX_LINE * 2]
    for rx, sub in SCRUB:
        s = rx.sub(sub, s)

    def ip(m):
        a, b = int(m.group(1)), int(m.group(2))
        private = a in (10, 127) or (a == 172 and 16 <= b <= 31) or (a == 192 and b == 168) or (a == 169 and b == 254)
        return m.group(0) if private or m.group(0) in (page_host, "0.0.0.0") else "<ip>"

    s = IPV4.sub(ip, s)
    return s[:MAX_LINE] + "..." if len(s) > MAX_LINE else s


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

    # client ?devlog=1 lines: no sender ip stored, size capped, scrubbed again (see ClientLogFilter.cs)
    def do_POST(self):
        if self.path != "/__log":
            self.send_error(404)
            return
        length = int(self.headers.get("Content-Length", 0) or 0)
        if length <= 0 or length > MAX_LOG_BODY:
            self.send_error(413)
            return
        try:
            entries = json.loads(self.rfile.read(length))
        except Exception:
            self.send_error(400)
            return
        if not isinstance(entries, list):
            self.send_error(400)
            return
        host = (self.headers.get("Host") or "").rsplit(":", 1)[0]
        os.makedirs(os.path.dirname(CLIENT_LOG) or ".", exist_ok=True)
        with open(CLIENT_LOG, "a", encoding="utf-8") as f:
            for e in entries[:MAX_LOG_LINES]:
                if not isinstance(e, dict):
                    continue
                line = json.dumps({"t": e.get("t"), "type": str(e.get("type", ""))[:16],
                                   "msg": scrub(str(e.get("msg", "")), host)}, ensure_ascii=False)
                f.write(line + "\n")
                print(f"[client] {line}", flush=True)
        body = b'{"ok":true}'
        self.send_response(200)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

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
    global CLIENT_LOG
    parser = argparse.ArgumentParser(description="Serve the UberStrike Unity WebGL build.")
    parser.add_argument("--dir", default=BUILD_DIR, help="WebGL build directory")
    parser.add_argument("--host", default="0.0.0.0")
    parser.add_argument("--port", type=int, default=8000)
    parser.add_argument("--client-log", default=CLIENT_LOG, help="where ?devlog=1 client lines go")
    args = parser.parse_args()
    CLIENT_LOG = os.path.abspath(args.client_log)

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

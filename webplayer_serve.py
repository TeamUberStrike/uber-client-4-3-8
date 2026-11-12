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

LOG_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "_log")
CLIENT_LOG = os.path.join(LOG_DIR, "client-logs.log")

MARKER = "<!-- webplayer-log-forwarder -->"

FORWARDER_JS = f"""<script>
{MARKER}
(function(){{
  var queue = [];
  var timer = null;
  function push(level, args){{
    var text = Array.prototype.map.call(args, function(a){{
      try {{ return (typeof a === 'object') ? JSON.stringify(a) : String(a); }} catch(e){{ return String(a); }}
    }}).join(' ');
    queue.push({{ t: Date.now(), ua: navigator.userAgent, level: level, text: text }});
    if (queue.length > 500) queue = queue.slice(-500);
    if (!timer) timer = setInterval(flush, 1000);
  }}
  function flush(){{
    if (!queue.length) return;
    var batch = queue; queue = [];
    try {{
      fetch('/__log', {{ method: 'POST', headers: {{'Content-Type':'application/json'}}, body: JSON.stringify(batch) }}).catch(function(){{}});
    }} catch(e){{}}
  }}
  window.addEventListener('beforeunload', flush);
  var origLog = console.log.bind(console), origErr = console.error.bind(console), origWarn = console.warn.bind(console);
  console.log = function(){{ push('log', arguments); origLog.apply(null, arguments); }};
  console.warn = function(){{ push('warn', arguments); origWarn.apply(null, arguments); }};
  console.error = function(){{ push('err', arguments); origErr.apply(null, arguments); }};
  window.addEventListener('error', function(e){{ push('jserr', [e.message + ' @ ' + e.filename + ':' + e.lineno]); }});
  push('boot', ['page loaded']);
}})();
</script>"""


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

    inject_logs = True

    def end_headers(self):
        self.send_header("Cross-Origin-Opener-Policy", "same-origin")
        self.send_header("Cross-Origin-Embedder-Policy", "require-corp")
        self.send_header("Cache-Control", "no-cache")
        super().end_headers()

    protocol_version = "HTTP/1.1"

    def do_GET(self):
        start = time.time()
        self._start = start
        if self.inject_logs and self.path.split("?", 1)[0].rstrip("/") in ("", "/index.html"):
            self.serve_injected_index()
            return
        super().do_GET()

    def serve_injected_index(self):
        path = self.translate_path("/index.html")
        try:
            with open(path, "rb") as f:
                content = f.read()
        except OSError:
            self.send_error(404)
            return
        if MARKER.encode() not in content:
            idx = content.lower().find(b"<body>")
            if idx >= 0:
                idx += len(b"<body>")
                content = content[:idx] + b"\n" + FORWARDER_JS.encode() + content[idx:]
        self.send_response(200)
        self.send_header("Content-Type", "text/html; charset=utf-8")
        self.send_header("Content-Length", str(len(content)))
        self.end_headers()
        self.wfile.write(content)

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
        os.makedirs(LOG_DIR, exist_ok=True)
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
        start = getattr(self, "_start", None)
        dur = f"{time.time() - start:6.1f}s" if start else "      "
        print(f"[{self.log_date_time_string()}] {dur} {code} {size:>9} {self.address_string()} {self.requestline}", flush=True)

    def copyfile(self, source, outputfile):
        import shutil
        t0 = time.time()
        shutil.copyfileobj(source, outputfile)
        print(f"    body done in {time.time() - t0:.1f}s", flush=True)

    def log_error(self, format, *args):
        print(f"[{self.log_date_time_string()}] ERROR {self.address_string()} {format % args}", flush=True)


def main():
    parser = argparse.ArgumentParser(description="Serve the UberStrike Unity WebGL build.")
    parser.add_argument("--dir", default=BUILD_DIR, help="WebGL build directory")
    parser.add_argument("--host", default="0.0.0.0")
    parser.add_argument("--port", type=int, default=8000)
    parser.add_argument("--logs", action=argparse.BooleanOptionalAction, default=True,
                        help="inject the client-log forwarder into index.html (default: on); "
                             "client logs land in %s via POST /__log" % CLIENT_LOG)
    args = parser.parse_args()

    UnityWebGLHandler.inject_logs = args.logs

    class Server(ThreadingHTTPServer):
        daemon_threads = True
        allow_reuse_address = True

    os.chdir(args.dir)
    server = Server((args.host, args.port), UnityWebGLHandler)
    print(f"Serving {os.path.abspath(args.dir)} at http://localhost:{args.port} "
          f"(log forwarding {'ON' if args.logs else 'OFF'} -> {CLIENT_LOG})")
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\nStopped.")


if __name__ == "__main__":
    main()

#!/usr/bin/env python3
import argparse
import json
import os
import signal
import socket
import subprocess
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


def find_pids_on_port(port):
    pids = []

    try:
        out = subprocess.run(["ss", "-tlnp"], capture_output=True, text=True, timeout=5).stdout
        for line in out.splitlines():
            if f":{port} " in line or f":{port}\t" in line:
                for tok in line.split():
                    if tok.startswith("pid="):
                        pid = tok.split("=", 1)[1].split(",", 1)[0]
                        if pid.isdigit() and int(pid) not in pids:
                            pids.append(int(pid))
    except Exception:
        pass

    try:
        out = subprocess.run(["lsof", "-ti", f"tcp:{port}"], capture_output=True, text=True, timeout=5).stdout
        for p in out.split():
            if p.isdigit() and int(p) not in pids:
                pids.append(int(p))
    except Exception:
        pass

    if pids:
        return pids

    # Fallback: map the LISTEN socket inode for this port to processes via /proc.
    try:
        port_hex = f"{port:04X}"
        inodes = set()
        for proto in ("/proc/net/tcp", "/proc/net/tcp6"):
            with open(proto) as f:
                next(f)
                for line in f:
                    parts = line.split()
                    if len(parts) >= 10 and parts[3] == "0A" and parts[1].split(":")[1] == port_hex:
                        inodes.add(parts[9])
        for proc_dir in os.listdir("/proc"):
            if not proc_dir.isdigit():
                continue
            fd_dir = f"/proc/{proc_dir}/fd"
            try:
                for fd in os.listdir(fd_dir):
                    target = os.readlink(f"{fd_dir}/{fd}")
                    for inode in inodes:
                        if target == f"socket:[{inode}]":
                            pid = int(proc_dir)
                            if pid not in pids:
                                pids.append(pid)
            except OSError:
                continue
    except Exception:
        pass

    return pids


def bind_server(handler_cls, host, port):
    class Server(ThreadingHTTPServer):
        daemon_threads = True
        allow_reuse_address = True

    return Server((host, port), handler_cls)


def main():
    parser = argparse.ArgumentParser(description="Serve the UberStrike Unity WebGL build.")
    parser.add_argument("--dir", default=BUILD_DIR, help="WebGL build directory")
    parser.add_argument("--host", default="0.0.0.0")
    parser.add_argument("--port", type=int, default=8000)
    parser.add_argument("--logs", action=argparse.BooleanOptionalAction, default=True,
                        help="inject the client-log forwarder into index.html (default: on); "
                             "client logs land in %s via POST /__log" % CLIENT_LOG)
    parser.add_argument("--kill", action="store_true",
                        help="kill every process holding --port, then start serving")
    args = parser.parse_args()

    server = None

    if args.kill:
        pids = find_pids_on_port(args.port)
        if pids:
            for pid in pids:
                try:
                    os.kill(pid, signal.SIGKILL)
                    print(f"Killed pid {pid}.", flush=True)
                except OSError as e:
                    print(f"Could not kill pid {pid}: {e}", flush=True)
        else:
            print(f"Nothing is holding port {args.port}.", flush=True)
        # Give the killed sockets a moment to release, then grab the port without prompting.
        for _ in range(10):
            time.sleep(0.3)
            try:
                server = bind_server(UnityWebGLHandler, args.host, args.port)
                break
            except OSError:
                continue

    UnityWebGLHandler.inject_logs = args.logs

    if server is None:
        while server is None:
            try:
                server = bind_server(UnityWebGLHandler, args.host, args.port)
            except OSError:
                pids = find_pids_on_port(args.port)
                holder = f" (pid {' '.join(map(str, pids))})" if pids else ""
                prompt = f"Port {args.port} is already in use{holder}. [k] kill all and retry, [n] use port {args.port + 1}: "
                try:
                    answer = input(prompt).strip().lower()
                except (EOFError, KeyboardInterrupt):
                    answer = "n"
                if answer.startswith("k"):
                    if pids:
                        killed = []
                        for pid in pids:
                            try:
                                os.kill(pid, signal.SIGKILL)
                                killed.append(pid)
                            except OSError:
                                pass
                        print(f"Killed pid(s) {killed}.", flush=True)
                    else:
                        print("Could not identify the process holding the port.", flush=True)
                    # Sockets take a moment to release after SIGKILL; retry the bind a
                    # few times before asking again.
                    for _ in range(10):
                        time.sleep(0.3)
                        try:
                            server = bind_server(UnityWebGLHandler, args.host, args.port)
                            break
                        except OSError:
                            continue
                else:
                    args.port += 1

    os.chdir(args.dir)
    print(f"Serving {os.path.abspath(args.dir)} at http://localhost:{args.port} "
          f"(log forwarding {'ON' if args.logs else 'OFF'} -> {CLIENT_LOG})", flush=True)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\nStopped.")


if __name__ == "__main__":
    main()

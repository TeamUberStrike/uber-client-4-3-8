#!/usr/bin/env python3
import argparse
import os
import re
import urllib.error
import urllib.request
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer

WS_BASE = "https://ws-dev.uberforever.eu/"
CONTENT_BASE = "http://client-dev.uberforever.eu/"
ROOT = "UberStrike.Unity/Build/WebGL-Spike"

HOP_HEADERS = {
    "connection",
    "keep-alive",
    "proxy-authenticate",
    "proxy-authorization",
    "te",
    "trailers",
    "transfer-encoding",
    "upgrade",
    "host",
    "accept-encoding",
    "origin",
    "referer",
    "content-length",
}


class Handler(SimpleHTTPRequestHandler):
    server_version = "UberStrikeDev/1.0"

    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=ROOT, **kwargs)

    def log_message(self, fmt, *args):
        print("%s - %s" % (self.address_string(), fmt % args), flush=True)

    def do_HEAD(self):
        if self._is_proxy():
            self._proxy()
        elif self._is_config():
            self._serve_config(head=True)
        else:
            super().do_HEAD()

    def do_GET(self):
        if self._is_proxy():
            self._proxy()
        elif self._is_config():
            self._serve_config(head=False)
        else:
            super().do_GET()

    def do_POST(self):
        if self._is_proxy():
            self._proxy()
        else:
            self.send_error(404)

    def do_PUT(self):
        if self._is_proxy():
            self._proxy()
        else:
            self.send_error(404)

    def do_DELETE(self):
        if self._is_proxy():
            self._proxy()
        else:
            self.send_error(404)

    def do_OPTIONS(self):
        self.send_response(204)
        self.send_header("Allow", "GET,HEAD,POST,PUT,DELETE,OPTIONS")
        self.send_header("Content-Length", "0")
        self.end_headers()

    def _path_only(self):
        return self.path.split("?", 1)[0]

    def _is_config(self):
        return self._path_only() == "/UberStrike.xml"

    def _is_proxy(self):
        p = self._path_only()
        return p.startswith("/ws/") or p.startswith("/content/")

    def _serve_config(self, head):
        path = os.path.join(ROOT, "UberStrike.xml")
        try:
            with open(path, "r", encoding="utf-8") as f:
                xml = f.read()
        except OSError as e:
            self.send_error(500, "cannot read UberStrike.xml: %s" % e)
            return

        host = self.headers.get("Host", "127.0.0.1:%d" % self.server.server_port)
        xml = re.sub(r'WebServiceBaseUrl="[^"]*"',
                     'WebServiceBaseUrl="http://%s/ws/"' % host, xml)
        xml = re.sub(r'ContentBaseUrl="[^"]*"',
                     'ContentBaseUrl="http://%s/content/"' % host, xml)
        body = xml.encode("utf-8")

        self.send_response(200)
        self.send_header("Content-Type", "application/xml; charset=utf-8")
        self.send_header("Cache-Control", "no-store")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        if not head:
            self.wfile.write(body)

    def _proxy(self):
        path = self.path
        if path.startswith("/ws/"):
            target = WS_BASE + path[len("/ws/"):]
        else:
            target = CONTENT_BASE + path[len("/content/"):]

        length = int(self.headers.get("Content-Length") or 0)
        body = self.rfile.read(length) if length else None

        headers = {}
        for key, value in self.headers.items():
            if key.lower() not in HOP_HEADERS:
                headers[key] = value

        req = urllib.request.Request(target, data=body, headers=headers,
                                     method=self.command)
        try:
            resp = urllib.request.urlopen(req, timeout=60)
        except urllib.error.HTTPError as e:
            resp = e
        except Exception as e:
            self.send_error(502, "upstream failed: %s" % e)
            return

        payload = resp.read()
        self.send_response(resp.getcode())
        for key, value in resp.headers.items():
            if key.lower() not in HOP_HEADERS and key.lower() != "content-length":
                self.send_header(key, value)
        self.send_header("Content-Length", str(len(payload)))
        self.end_headers()
        if self.command != "HEAD":
            self.wfile.write(payload)


def main():
    global ROOT, WS_BASE, CONTENT_BASE
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", default=ROOT)
    parser.add_argument("--port", type=int, default=8125)
    parser.add_argument("--ws", default=WS_BASE)
    parser.add_argument("--content", default=CONTENT_BASE)
    args = parser.parse_args()

    ROOT = args.root
    WS_BASE = args.ws
    CONTENT_BASE = args.content

    print("serving %s on http://127.0.0.1:%d/" % (ROOT, args.port), flush=True)
    print("proxy /ws/ -> %s" % WS_BASE, flush=True)
    print("proxy /content/ -> %s" % CONTENT_BASE, flush=True)
    ThreadingHTTPServer(("0.0.0.0", args.port), Handler).serve_forever()


if __name__ == "__main__":
    main()

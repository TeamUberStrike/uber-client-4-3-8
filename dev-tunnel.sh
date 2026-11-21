#!/usr/bin/env bash
set -euo pipefail

log() { printf '\033[1;36mdev-tunnel:\033[0m %s\n' "$*"; }

WEBPLAYER_DIR="UberStrike.Unity/Latest/WebPlayer"
CONFIG_XML="${WEBPLAYER_DIR}/UberStrike.xml"
WEBSERVICE_HEALTH_URL='http://localhost:9000/UberStrike.DataCenter.WebService.CWS.AuthenticationWebServiceContract.svc'

usage() {
    cat <<EOF
Usage: ./dev-tunnel.sh [webplayer|webservice]

  webplayer   Cloudflare quick tunnel for the WebGL client (localhost:8000)
  webservice  Cloudflare quick tunnel for the IIS webservice (localhost:9000),
              updates WebServiceBaseUrl in ${CONFIG_XML}

If no option is given, you are prompted to choose.
EOF
}

start_webplayer() {
    # make sure the local WebGL server is running on http://localhost:8000
    if ! curl -fsS -o /dev/null http://localhost:8000/ 2>/dev/null; then
        log 'webplayer server not running, starting webplayer_serve.py'
        nohup python3 webplayer_serve.py >/tmp/webplayer_serve.log 2>&1 &
        for _ in $(seq 1 30); do
            curl -fsS -o /dev/null http://localhost:8000/ 2>/dev/null && break
            sleep 1
        done
    fi
    if ! curl -fsS -o /dev/null http://localhost:8000/ 2>/dev/null; then
        log 'ERROR: webplayer not reachable on http://localhost:8000' >&2
        exit 1
    fi
    log 'local webplayer running on http://localhost:8000'

    log 'starting cloudflared tunnel for webplayer (localhost:8000)'
    docker rm -f cloudflared-webplayer >/dev/null 2>&1 || true
    docker run -d --name cloudflared-webplayer --network host \
        cloudflare/cloudflared:latest tunnel --no-autoupdate --url http://localhost:8000 \
        >/dev/null

    URL=""
    for _ in $(seq 1 60); do
        URL="$(docker logs cloudflared-webplayer 2>&1 | grep -oE 'https://[a-z0-9-]+\.trycloudflare\.com' | head -1 || true)"
        [ -n "$URL" ] && break
        sleep 2
    done
    if [ -z "$URL" ]; then
        log 'ERROR: no tunnel URL, check "docker logs cloudflared-webplayer"' >&2
        exit 1
    fi

    cat <<EOF

Tunnel ready:
  WebGL client:  ${URL}

Open this URL on the iPhone — it is HTTPS (Cloudflare edge) and forwards
to the local http://localhost:8000 webplayer.

Stop the tunnel: docker rm -f cloudflared-webplayer
Tunnel logs:     docker logs -f cloudflared-webplayer
EOF
}

start_webservice() {
    # the site root returns 403 without a default document, so probe the .svc endpoint
    if ! curl -fsS -o /dev/null "$WEBSERVICE_HEALTH_URL" 2>/dev/null; then
        log 'ERROR: webservice not reachable on http://localhost:9000 (Windows VM IIS).' >&2
        log '       Start the Windows container and make sure IIS listens on port 9000.' >&2
        exit 1
    fi
    log 'local webservice running on http://localhost:9000'

    log 'starting cloudflared tunnel for webservice (localhost:9000)'
    docker rm -f cloudflared-ws >/dev/null 2>&1 || true
    docker run -d --name cloudflared-ws --network host \
        cloudflare/cloudflared:latest tunnel --no-autoupdate --url http://localhost:9000 \
        >/dev/null

    WS_URL=""
    for _ in $(seq 1 60); do
        WS_URL="$(docker logs cloudflared-ws 2>&1 | grep -oE 'https://[a-z0-9-]+\.trycloudflare\.com' | head -1 || true)"
        [ -n "$WS_URL" ] && break
        sleep 2
    done
    if [ -z "$WS_URL" ]; then
        log 'ERROR: no webservice tunnel URL, check "docker logs cloudflared-ws"' >&2
        exit 1
    fi

    # The WebGL build config URL is NOT touched here: the Dev WebGL build
    # hardcodes the webservice URL in SceneExporter
    # (GetWebserviceBaseUrl -> WebserviceLocation.Localhost).

    cat <<EOF

Tunnel ready:
  Webservice:    ${WS_URL}/

Update the hardcoded URL in SceneExporter.cs
(WebserviceLocation.Localhost) and the build config
(${CONFIG_XML}) if this URL changed, then rebuild.

Requirements for login to work from the phone:
  - IIS must serve CORS headers (payment branch Global.asax, see
    windows-docker/shared/handover-cors-deploy.md)
  - the test account must exist in the DB the IIS webservice uses

Stop the tunnel: docker rm -f cloudflared-ws
Tunnel logs:     docker logs -f cloudflared-ws
EOF
}

ACTION="${1:-}"

if [ -z "$ACTION" ]; then
    printf 'Select tunnel:\n  (1) webplayer\n  (2) webservice\nChoice: '
    read -r CHOICE
    case "$CHOICE" in
        1|webplayer) ACTION="webplayer" ;;
        2|webservice) ACTION="webservice" ;;
        *) log "invalid choice: $CHOICE" >&2; exit 1 ;;
    esac
fi

case "$ACTION" in
    webplayer) start_webplayer ;;
    webservice) start_webservice ;;
    -h|--help|help) usage ;;
    *) usage >&2; exit 1 ;;
esac

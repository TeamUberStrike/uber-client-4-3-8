#!/bin/bash
#
# build-ios.sh — export the iOS player with Unity, compile it with xcodebuild
# (automatic signing) and install + launch uberstrike on the connected device.
#
# Usage:
#   ./build-ios.sh
#
# Environment overrides:
#   UNITY            Path to the Unity editor binary
#   DEVICE           Device name / UDID to deploy to (default: Xaver)
#   BUNDLE_ID        Bundle identifier to launch (default: com.yoxa.uberstrike)
#   DEVELOPMENT_TEAM Apple team ID for automatic signing (default: S4CNK69FK9)
#   CONFIGURATION    Xcode build configuration (default: Debug)
#   SCHEME           Xcode scheme (default: Unity-iPhone)
#   BUILD_METHOD     Unity -executeMethod entry point (default: SceneExporter.BuildiOS)
#
# Requirements:
#   - Unity Editor (2022.3.62f3 or override via UNITY=...) with iOS Build Support
#   - Xcode with the signing account added (Signing & Capabilities team selector)
#   - the target device connected, unlocked and trusted (xcrun devicectl list devices)
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT="$ROOT/UberStrike.Unity"
XCODE_PROJECT="$PROJECT/BuildIos/Unity-iPhone.xcodeproj"
DERIVED_DATA="$PROJECT/BuildIos/DerivedData"

UNITY="${UNITY:-/Applications/Unity/Hub/Editor/2022.3.62f3/Unity.app/Contents/MacOS/Unity}"
BUNDLE_ID="${BUNDLE_ID:-com.yoxa.uberstrike}"
DEVELOPMENT_TEAM="${DEVELOPMENT_TEAM:-S4CNK69FK9}"
CONFIGURATION="${CONFIGURATION:-Debug}"
SCHEME="${SCHEME:-Unity-iPhone}"
BUILD_METHOD="${BUILD_METHOD:-SceneExporter.BuildiOS}"

# Auto-detect the first connected iPhone/iPad unless DEVICE is set explicitly.
if [ -z "${DEVICE:-}" ]; then
    DEVICE="$(xcrun devicectl list devices 2>/dev/null | awk '/connected/ && /iPhone|iPad|iPod/ {print $1; exit}')"
    if [ -z "$DEVICE" ]; then
        echo "Error: no connected iPhone/iPad found (set DEVICE=<name|udid> to override)" >&2
        exit 1
    fi
    echo "Auto-detected device: $DEVICE"
fi

UNITY_LOG="$PROJECT/ios-unity-build.log"

if [ ! -x "$UNITY" ]; then
    echo "Error: Unity not found at $UNITY" >&2
    echo "       Install Unity 2022.3.62f3 (with iOS Build Support) or set UNITY=..." >&2
    exit 1
fi

echo "== [1/3] Unity iOS export =="
if pgrep -f "Unity.app/Contents/MacOS/Unity" >/dev/null 2>&1; then
    echo "Error: Unity Editor is running. Close it first — batchmode needs the project lock." >&2
    exit 1
fi

"$UNITY" -batchmode -nographics -quit \
    -projectPath "$PROJECT" \
    -buildTarget iOS \
    -executeMethod "$BUILD_METHOD" \
    -logFile "$UNITY_LOG"

if grep -q "BuildPlayer failed" "$UNITY_LOG"; then
    echo "Error: Unity export failed. Tail of $UNITY_LOG:" >&2
    tail -40 "$UNITY_LOG" >&2 || true
    exit 1
fi
if [ ! -d "$XCODE_PROJECT" ]; then
    echo "Error: Unity export did not produce $XCODE_PROJECT" >&2
    exit 1
fi
echo "Unity export OK -> $XCODE_PROJECT"

echo "== [2/3] xcodebuild (automatic signing, team $DEVELOPMENT_TEAM) =="
xcodebuild \
    -project "$XCODE_PROJECT" \
    -scheme "$SCHEME" \
    -configuration "$CONFIGURATION" \
    -destination "generic/platform=iOS" \
    -derivedDataPath "$DERIVED_DATA" \
    DEVELOPMENT_TEAM="$DEVELOPMENT_TEAM" \
    CODE_SIGN_STYLE=Automatic \
    -allowProvisioningUpdates \
    build

APP="$DERIVED_DATA/Build/Products/$CONFIGURATION-iphoneos/uberstrike.app"
if [ ! -d "$APP" ]; then
    echo "Error: build product not found at $APP" >&2
    exit 1
fi

echo "== [3/3] Install + launch on '$DEVICE' =="
xcrun devicectl device install app --device "$DEVICE" "$APP"
xcrun devicectl device process launch --device "$DEVICE" --terminate-existing "$BUNDLE_ID"

echo "Done. uberstrike is running on $DEVICE."

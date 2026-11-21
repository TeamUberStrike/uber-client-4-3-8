# UberStrike Client 4.3.8

## License
  This project is licensed under the GNU General Public License v3.0 (GPL-3.0). See the LICENSE file for details.

## Setup
- Install Unity Hub -> https://unity.com/download
- Add the repository in Unity Hub
- Unity Hub will automatically select the required Unity version

- Inside the Unity Editor Latest.unity is used. Outside the Editor, running the application Spaceship.unity is the entry point.

### Local Photon Server
  You can choose to use local Photon Game/Comm servers instead of configuring this in the database.
  Turn isEnabled on:
```
./UberStrike.Unity/Assets/Scenes/Latest.unity:8127:  _localGameServer:
./UberStrike.Unity/Assets/Scenes/Latest.unity:8131:  _localCommServer:
```

## Unity MCP (AI editor integration)

The Unity project pins [MCP for Unity](https://github.com/CoplayDev/unity-mcp) (`com.coplaydev.unity-mcp`, v10.3.0) in `UberStrike.Unity/Packages/manifest.json`; Unity installs it automatically.

Prerequisites: `uv` (the MCP server runs on Python 3.10+):

```
curl -LsSf https://astral.sh/uv/install.sh | sh
```

1. In the Unity Editor: **Window → MCP for Unity**.
2. Set the port to **8090** (8080 is used by Adminer on this machine) and click **Start Server**.
3. Add the server to `~/.config/opencode/opencode.json`:

```json
"mcp": {
  "unity": {
    "type": "remote",
    "url": "http://127.0.0.1:8090/mcp",
    "enabled": true
  }
}
```

4. Restart opencode and run `/mcp` to confirm `unity` is connected.

Notes:
- Use `127.0.0.1`, not `localhost` (the server binds IPv4; `localhost` may resolve to `::1`).
- The connection drops after script recompiles or leaving Play mode — reconnect via `/mcp` or the MCP for Unity window.
- If HTTP transport is unavailable, use stdio instead (use `~/.local/bin/uvx` if opencode cannot find `uvx`):

```json
"unity": {
  "type": "local",
  "command": ["uvx", "--from", "mcpforunityserver", "mcp-for-unity", "--transport", "stdio"],
  "enabled": true
}
```

## Build Versioning

GitVersion is required for local builds:

- macOS: `brew install gitversion`
- Windows/Linux: download `gitversion-win-x64-*.zip` / `gitversion-linux-x64-*.tar.gz` from [GitVersion releases](https://github.com/GitTools/GitVersion/releases) and add it to `PATH`

`GitVersion.yml` is in the v7 format — GitVersion 7.x (or later) is required; older 5.x/6.x releases will reject it.

## iOS Build Notes

These settings/fixes are required for the iOS (iPhone/iPad) IL2CPP build to work. They are already committed — this section documents why they exist, in case a Unity upgrade or re-export regresses them.

### Signing & build target (ProjectSettings.asset)
- `appleEnableAutomaticSigning: 1` — exports the Xcode project with `CODE_SIGN_STYLE = Automatic`. With this off, Unity exported a manual-signing project with empty provisioning references and device builds failed to sign.
- `appleDeveloperTeamID: S4CNK69FK9` — the Xaver Gruber team that owns provisioning profiles for `eu.yoxa.uberstrike`. If you use another Apple account/team, change this in Unity: Player Settings → iOS → Signing → Team ID.
- `targetDevice: 2` — iPhone + iPad (`1` is iPad-only; the export then generates `TARGETED_DEVICE_FAMILY = 2`).

### IL2CPP managed-stripping preservation (Assets/link.xml)
`managedStrippingLevel` is 1 (Low) for all platforms. Without `link.xml`:
- `SecureMemory<T>` fails at login: `ExtendableEnum` const tables (`CmuneDataType`, `UberStrikeDataType`) are only read via reflection, so the linker strips them → `"Decode Object failed because found unrecognized datatype 6"` / `"CmuneSecureVariable failed decrypting Data"`.
- `Singleton<T>` constructs instances via reflection on private constructors.
- Prime31 StoreKit callbacks arrive via `UnitySendMessage` (string dispatch).

`link.xml` therefore preserves `Assembly-CSharp`, `UberStrike.UnitySdk`, `P31RestKit` and `System.Linq.Enumerable`. (No Photon AOT hints needed — Photon3Unity3D was removed and the rebuilt SDK has no `MakeGenericType` deserialization.)

### Plain-HTTP content downloads
The content CDN (`ContentBaseUrl = http://client-dev.uberstrike.com/` for maps/images/audio) is plain HTTP only; the host's 443 listener presents a certificate that does not match the domain, so upgrading the URLs to `https://` is not an option until a real cert is installed. Therefore:
- `insecureHttpOption: 2` (AlwaysAllowed) in ProjectSettings.asset — release builds otherwise throw `InvalidOperationException: Insecure connection not allowed`.
- `Assets/Editor/IosBuildPostProcessor.cs` re-applies the `NSAppTransportSecurity`/`NSAllowsArbitraryLoads` exception to Info.plist after every export (Unity regenerates Info.plist each build).
- `TextureLoader` catches synchronous `WWW`/`UnityWebRequest` construction exceptions and falls back to the placeholder texture instead of an unhandled OnGUI exception.

### Maps are baked into the build on mobile
All `Level*` scenes are in Build Settings, and on mobile `LevelManager.MapLoader` loads them directly via `Application.LoadLevelAdditiveAsync(SceneName)` — it must not fall through to the desktop CDN asset-bundle path (`WWW.LoadFromCacheOrDownload`), which hangs forever ("StartLoadingMap ... timeout: True" → infinite loading bar when joining training or any game).

- Mobile channels request `MapType.HighDefinition` map data (`AuthenticationManager`); the server's 4.3.8 map rows are all `MapType=2` (HD).
- The embedded mobile config (`ApplicationDataManager.mobileConfigXml`) must keep `ChannelType="IPad"` on iOS — `IsMobile` gates the baked-scene path and the mobile UI. The dev server whitelists the iOS channel for 4.3.8.

### Dev config
`EditorConfiguration.xml` (editor) / `mobileConfigXml` (mobile builds):
- `WebServiceBaseUrl = https://ws-dev.uberstrike.com/`
- `ContentBaseUrl = http://client-dev.uberstrike.com/`

### Build & deploy script (`build-ios.sh`)
One-command device deployment:

```
./build-ios.sh
```

1. Exports the iOS player from Unity in batchmode via the existing `SceneExporter.BuildiOS` (also available as **File → Build UberStrike → Internal Dev → iOS**; builds the Editor Build Settings scene list) into `UberStrike.Unity/BuildIos`.
2. Compiles with `xcodebuild` (scheme `Unity-iPhone`, automatic signing, `-allowProvisioningUpdates`).
3. Installs the `.app` and launches `eu.yoxa.uberstrike` on the connected device via `devicectl`.

Overridable via env vars: `UNITY`, `DEVICE` (auto-detects the first connected iPhone/iPad when unset), `BUNDLE_ID`, `DEVELOPMENT_TEAM` (default `S4CNK69FK9`), `CONFIGURATION`, `SCHEME`, `BUILD_METHOD`. The Unity Editor must be closed (batchmode needs the project lock), and Xcode must have the signing account/team set up once.

## WebPlayer (WebGL) publish

1. Build in Unity: **File → Build UberStrike → Production → Web Player (Main Only)** → output in `UberStrike.Unity/Latest/WebPlayer/` (`index.html`, `Build/`, `TemplateData/`, `StreamingAssets/`, `UberStrike.xml`).
2. `./deploy_webplayer.py` SFTPs the folder to All-Inkl (credentials in gitignored `.env`):
   - `REMOTE_WEBPLAYER_PATH=/www/htdocs/w01a5092/UberStrikeWebgl438` (All-Inkl web root of the domain)
   - skips `Map-*.unity3d` (maps come from the CDN via `deploy_maps.py` → `REMOTE_MAPS_PATH`)
   - also deploys `content-server.htaccess` as `.htaccess` (CORS headers)
3. Maps: `./deploy_maps.py` stages `Map-XX-*.unity3d` → `LevelName.unity3d` and uploads them to the maps path.

### Local dev tunnels (mobile testing)

`./dev-tunnel.sh` exposes the local WebGL build and the local webservice
over Cloudflare quick tunnels so an iPhone can test them (HTTPS via the
Cloudflare edge, no insecure-connection warnings):

- **Webplayer tunnel** — docker container `cloudflared-webplayer`,
  Cloudflare quick tunnel `--url http://localhost:8000` → forwards to
  `webplayer_serve.py`.
- **Webservice tunnel** — docker container `cloudflared-ws`,
  Cloudflare quick tunnel `--url http://localhost:9000` → forwards to the
  IIS webservice in the Windows VM (docker port map `9000:9000`).

Both quick-tunnel URLs are random and change on every restart. The script
just starts the requested tunnel and prints its URL; it does NOT rewrite
the build config. The webservice URL is hardcoded in `SceneExporter.cs`
(`GetWebserviceBaseUrl` → `WebserviceLocation.Localhost`, used by the
Internal Dev WebGL build menu) — update that constant and the generated
`UberStrike.xml` when the tunnel URL changes. Because the page is HTTPS,
the webservice must also be HTTPS — hence the second tunnel (plain
`http://192.168.1.51:9000/` from an HTTPS page is blocked as mixed
content, and `localhost:9000` on the phone is the phone itself).

Prerequisites for login to work from the phone:

- The Windows VM IIS must serve CORS headers (payment-branch
  `Global.asax`, commit `1cfa004`); without it every SOAP call is blocked
  by the browser. See `windows-docker/shared/handover-cors-deploy.md`.
- The test account must exist in the database the IIS webservice uses
  (`Cmune.dbo.Members.Login` lookup), with `MemberAccess` for the app and
  `ApplicationVersions.IsBeta = 1` for `(Version='4.3.8', Channel=4)`.

Stop the tunnels with `docker rm -f cloudflared-webplayer cloudflared-ws`;
logs via `docker logs -f <container>`.

### Domain + HTTPS (uberstrike.app)

- Cloudflare DNS: `uberstrike.app` → CNAME `w01a5092.kasserver.com`, **Proxied** (orange cloud).
- Cloudflare SSL/TLS mode: **Flexible**. All-Inkl has `SSL-Schutz` deaktiviert and redirects HTTPS → HTTP on the origin, so Full mode 301-loops; Flexible terminates TLS at Cloudflare and talks plain HTTP to All-Inkl.
- All-Inkl domain config: root directory `/www/htdocs/w01a5092/UberStrikeWebgl438/`.
- `UberStrike.xml` `ContentBaseUrl` must be HTTPS or same-origin, otherwise the browser blocks map/image/audio downloads as mixed content from the HTTPS page.

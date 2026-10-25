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

## iOS Build Notes

These settings/fixes are required for the iOS (iPhone/iPad) IL2CPP build to work. They are already committed — this section documents why they exist, in case a Unity upgrade or re-export regresses them.

### Signing & build target (ProjectSettings.asset)
- `appleEnableAutomaticSigning: 1` — exports the Xcode project with `CODE_SIGN_STYLE = Automatic`. With this off, Unity exported a manual-signing project with empty provisioning references and device builds failed to sign.
- `appleDeveloperTeamID: S4CNK69FK9` — the Xaver Gruber team that owns provisioning profiles for `com.yoxa.uberstrike`. If you use another Apple account/team, change this in Unity: Player Settings → iOS → Signing → Team ID.
- `targetDevice: 2` — iPhone + iPad (`1` is iPad-only; the export then generates `TARGETED_DEVICE_FAMILY = 2`).

### IL2CPP managed-stripping preservation (Assets/link.xml)
`managedStrippingLevel` is 1 (Low) for all platforms. Without `link.xml`:
- `SecureMemory<T>` fails at login: `ExtendableEnum` const tables (`CmuneDataType`, `UberStrikeDataType`) are only read via reflection, so the linker strips them → `"Decode Object failed because found unrecognized datatype 6"` / `"CmuneSecureVariable failed decrypting Data"`.
- `Singleton<T>` constructs instances via reflection on private constructors.
- Prime31 StoreKit callbacks arrive via `UnitySendMessage` (string dispatch).

`link.xml` therefore preserves `Assembly-CSharp`, `UberStrike.UnitySdk`, `P31RestKit` and `System.Linq.Enumerable`. (No Photon AOT hints needed — Photon3Unity3D was removed and the rebuilt SDK has no `MakeGenericType` deserialization.)

### Plain-HTTP content downloads
The content CDN (`ContentBaseUrl = http://client-dev.uberforever.eu/` for maps/images/audio) is plain HTTP only; the host's 443 listener presents a certificate that does not match the domain, so upgrading the URLs to `https://` is not an option until a real cert is installed. Therefore:
- `insecureHttpOption: 2` (AlwaysAllowed) in ProjectSettings.asset — release builds otherwise throw `InvalidOperationException: Insecure connection not allowed`.
- `Assets/Editor/IosBuildPostProcessor.cs` re-applies the `NSAppTransportSecurity`/`NSAllowsArbitraryLoads` exception to Info.plist after every export (Unity regenerates Info.plist each build).
- `TextureLoader` catches synchronous `WWW`/`UnityWebRequest` construction exceptions and falls back to the placeholder texture instead of an unhandled OnGUI exception.

### Maps are baked into the build on mobile
All `Level*` scenes are in Build Settings, and on mobile `LevelManager.MapLoader` loads them directly via `Application.LoadLevelAdditiveAsync(SceneName)` — it must not fall through to the desktop CDN asset-bundle path (`WWW.LoadFromCacheOrDownload`), which hangs forever ("StartLoadingMap ... timeout: True" → infinite loading bar when joining training or any game).

- Mobile channels request `MapType.HighDefinition` map data (`AuthenticationManager`); the server's 4.3.8 map rows are all `MapType=2` (HD).
- The embedded mobile config (`ApplicationDataManager.mobileConfigXml`) must keep `ChannelType="IPad"` on iOS — `IsMobile` gates the baked-scene path and the mobile UI. The dev server whitelists the iOS channel for 4.3.8.

### Dev config
`EditorConfiguration.xml` (editor) / `mobileConfigXml` (mobile builds):
- `WebServiceBaseUrl = https://ws-dev.uberforever.eu/`
- `ContentBaseUrl = http://client-dev.uberforever.eu/`

### Build & deploy script (`build-ios.sh`)
One-command device deployment:

```
./build-ios.sh
```

1. Exports the iOS player from Unity in batchmode (`IosCommandLineBuilder.Build` in `Assets/Editor/`, same scene list as Build Settings) into `UberStrike.Unity/BuildIos`.
2. Compiles with `xcodebuild` (scheme `Unity-iPhone`, automatic signing, `-allowProvisioningUpdates`).
3. Installs the `.app` and launches `com.yoxa.uberstrike` on the connected device via `devicectl`.

Overridable via env vars: `UNITY`, `DEVICE` (default `Xaver`), `BUNDLE_ID`, `DEVELOPMENT_TEAM` (default `S4CNK69FK9`), `CONFIGURATION`, `SCHEME`, `BUILD_METHOD`. The Unity Editor must be closed (batchmode needs the project lock), and Xcode must have the signing account/team set up once.

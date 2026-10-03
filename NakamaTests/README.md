# NakamaTests

Plain .NET tests for `UberStrike.Unity/Assets/Scripts/Network/Nakama` (outside the Unity project).
Compiles the BCL-only adapter files (`NakamaFraming`, `NakamaClock`, `INakamaLink`, `NakamaPeer`, `NakamaConfig`)
against `Assets/Plugins/UberStrike.UnitySdk.dll`.

```
dotnet build NakamaTests\NakamaTests.csproj -c Release
NakamaTests\bin\Release\net48\NakamaTests.exe        framing, clock, config, peer contract
dotnet NakamaTests\bin\Release\net8.0\NakamaTests.dll + real SDK PhotonPeerListener over NakamaPeer
```

net8.0 adds `SdkIntegrationTests` when Unity 2022.3.62f3 is installed (`UnityEngine.dll` is netstandard 2.1;
override with `-p:UnityManaged=<Editor\Data\Managed>`). No NuGet packages.

Byte vectors come from the Go module (`photon-migration@nakama`: `rmi_test.go`, `core_test.go`, `wire/testdata/golden.json`).

net8.0 also runs `LinkTests` (the shipped `NakamaLink` over proxied `IClient`/`ISocket`) when nakama-unity's
`Nakama.dll` is found (`Library\PackageCache` after a Unity import, or `-p:NakamaDll=<path>`).

## Live node (e2e/)

`ClientE2E` = `NakamaPeer` + `NakamaLink` + nakama-dotnet stdlib socket against Nakama 3.41.0 + the uber Go module
(`UBER_DEV_AUTH=true`), single-threaded like Unity's main thread. Runs in the server repo CI (`client-e2e.yml`):
`dotnet run --project NakamaTests/e2e/ClientE2E.csproj -c Release -p:NakamaDll=<Nakama.dll> -- --url http://127.0.0.1:7350 --report out.txt`

## Contract with the Go module

`contract\run-contract.bat -Nakama <photon-migration>\nakama -Work <short dir>`:
C# emits envelopes / op 66 / room join / time / ack layout -> Go `rmi`, `rpc`, `roomcore` parse them and emit
replies, acks, events, reject reasons -> C# parses those. Uses `nakama\tools\win-typecheck.ps1` (GitHub only).

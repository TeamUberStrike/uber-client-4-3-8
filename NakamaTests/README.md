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

## Contract with the Go module

`contract\run-contract.bat -Nakama <photon-migration>\nakama -Work <short dir>`:
C# emits envelopes / op 66 / room join / time / ack layout -> Go `rmi`, `rpc`, `roomcore` parse them and emit
replies, acks, events, reject reasons -> C# parses those. Uses `nakama\tools\win-typecheck.ps1` (GitHub only).

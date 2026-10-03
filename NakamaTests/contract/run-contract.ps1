# Client <-> Go contract round trip (C# emit -> Go parse + emit -> C# verify).
# Windows PowerShell 5.1 compatible, ASCII only. -Nakama = photon-migration nakama folder (branch nakama).
param(
    [Parameter(Mandatory = $true)][string]$Nakama,
    [string]$Work = ""
)
$ErrorActionPreference = 'Continue'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$tests = Split-Path -Parent $here
if (-not $Work) { $Work = Join-Path ([System.IO.Path]::GetTempPath()) 'uberct' }

function Fail([string]$what, [int]$code) { Write-Host "FAILED: $what (exit $code)"; exit $code }

dotnet build (Join-Path $tests 'NakamaTests.csproj') -c Release -nologo -v q
if ($LASTEXITCODE -ne 0) { Fail 'dotnet build' $LASTEXITCODE }
$exe = Join-Path $tests 'bin\Release\net48\NakamaTests.exe'

New-Item -ItemType Directory -Force $Work | Out-Null
$cs = Join-Path $Work 'cs.json'
$go = Join-Path $Work 'go.json'

& $exe --emit $cs
if ($LASTEXITCODE -ne 0) { Fail 'emit' $LASTEXITCODE }

& (Join-Path $Nakama 'tools\win-typecheck.ps1') -Work $Work -Keep
if ($LASTEXITCODE -ne 0) { Fail 'win-typecheck' $LASTEXITCODE }

$srv = Join-Path $Work 'server'
$dst = Join-Path $srv 'contract'
New-Item -ItemType Directory -Force $dst | Out-Null
Copy-Item -Force (Join-Path $here 'go\client_contract_test.go') $dst

$env:GOPROXY = 'direct'
$env:GOFLAGS = '-mod=mod'
$env:GOTOOLCHAIN = 'local'
$env:GOSUMDB = 'off'
$env:CGO_ENABLED = '0'
$env:UBER_CS_VECTORS = $cs
$env:UBER_GO_VECTORS = $go
Push-Location $srv
try { go test -count=1 -v ./contract/; $code = $LASTEXITCODE } finally { Pop-Location }
if ($code -ne 0) { Fail 'go test ./contract/' $code }

& $exe --verify $go
if ($LASTEXITCODE -ne 0) { Fail 'verify' $LASTEXITCODE }
Write-Host 'contract: OK'
exit 0

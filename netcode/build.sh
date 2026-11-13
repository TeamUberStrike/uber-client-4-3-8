#!/usr/bin/env bash
# Requires the .NET 8 SDK: https://dotnet.microsoft.com/download
set -euo pipefail
cd "$(dirname "$0")"
dotnet build UberStrike.Netcode.sln -c Release
echo "--- running tests ---"
dotnet run --project tests/UberStrike.Tests -c Release
echo "--- running sandbox demo ---"
dotnet run --project src/UberStrike.Sandbox -c Release

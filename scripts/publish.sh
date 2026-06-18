#!/usr/bin/env bash
# Build a self-contained, single-file EchoFactory build for one OS.
# Usage: scripts/publish.sh [win-x64|linux-x64|osx-x64|osx-arm64]   (default: win-x64)
#
# Output: artifacts/<rid>/  — a folder you can zip and ship. It contains the
# executable, the native SDL/OpenAL libraries, and loose data/ + branding/
# folders the game reads at runtime. No .NET install needed on the target.
set -euo pipefail

RID="${1:-win-x64}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
OUT="$ROOT/artifacts/$RID"

echo ">> Publishing EchoFactory for $RID -> $OUT"
dotnet publish "$ROOT/src/EchoFactory.Game/EchoFactory.Game.csproj" \
  -c Release -r "$RID" --self-contained \
  -p:PublishSingleFile=true \
  -o "$OUT"

echo ">> Done. Ship the whole folder:"
echo "   $OUT"

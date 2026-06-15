#!/bin/bash
# SessionStart hook for Claude Code on the web.
# Installs the .NET 8 SDK (not preinstalled in the remote container) and warms the
# NuGet cache so `dotnet build` / `dotnet test` work out of the box.
set -euo pipefail

# Only run in Claude Code on the web (remote) environments.
if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

DOTNET_DIR="$HOME/.dotnet"
DOTNET_CHANNEL="8.0"

# Persist environment for the whole session (PATH + telemetry opt-out).
if [ -n "${CLAUDE_ENV_FILE:-}" ]; then
  {
    echo "export DOTNET_ROOT=\"$DOTNET_DIR\""
    echo "export PATH=\"$DOTNET_DIR:\$PATH\""
    echo "export DOTNET_CLI_TELEMETRY_OPTOUT=1"
    echo "export DOTNET_NOLOGO=1"
  } >> "$CLAUDE_ENV_FILE"
fi

export PATH="$DOTNET_DIR:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

# Install the SDK once (idempotent: skip if already present).
if ! "$DOTNET_DIR/dotnet" --version >/dev/null 2>&1; then
  echo "Installing .NET SDK ($DOTNET_CHANNEL) ..."
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
  chmod +x /tmp/dotnet-install.sh
  /tmp/dotnet-install.sh --channel "$DOTNET_CHANNEL" --install-dir "$DOTNET_DIR"
else
  echo ".NET SDK already installed."
fi

# Warm the NuGet cache (restore is cached in the container image).
if [ -f "${CLAUDE_PROJECT_DIR:-.}/EchoFactory.sln" ]; then
  "$DOTNET_DIR/dotnet" restore "${CLAUDE_PROJECT_DIR:-.}/EchoFactory.sln"
fi

echo ".NET ready: $("$DOTNET_DIR/dotnet" --version)"

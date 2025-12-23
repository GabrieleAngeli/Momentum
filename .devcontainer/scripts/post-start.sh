#!/usr/bin/env bash
set -euo pipefail

say() { printf '%s\n' "$*"; }

# Ensure the dev user owns the HTTPS cert folder (volume may be root-owned on first start)
if [ -d "/home/vscode/.aspnet/https" ]; then
  if command -v sudo >/dev/null 2>&1; then
    sudo chown -R vscode:vscode /home/vscode/.aspnet/https 2>/dev/null || true
  fi
  chmod -R u+rwX /home/vscode/.aspnet/https 2>/dev/null || true
fi

# Ensure certs exist after every start (non-blocking)
if [ -f ".devcontainer/scripts/setup-https-certs.sh" ]; then
  say "[post-start] setup-https-certs (non-blocking)"
  bash .devcontainer/scripts/setup-https-certs.sh || true
fi

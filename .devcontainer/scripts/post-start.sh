#!/usr/bin/env bash
set -euo pipefail

# Ensure the developer user owns the HTTPS development certificates folder if mounted
if [ -d /home/vscode/.aspnet/https ]; then
  sudo chown -R vscode:vscode /home/vscode/.aspnet/https || true
fi

if [ -d /home/vscode/.aspnet/https-windows ]; then
  sudo chown -R vscode:vscode /home/vscode/.aspnet/https-windows || true
fi

# Guarantee HTTPS dev certificates exist after every start
bash "$(dirname "$0")/setup-https-certs.sh"

# Display quick usage summary
cat <<'MSG'
Momentum devcontainer ready.
- make build       → build backend and frontend
- make test        → run unit tests
- npm audit        → audit frontend packages
- dotnet list ...  → audit backend packages
MSG

#!/usr/bin/env bash
set -euo pipefail

say() { printf '%s\n' "$*"; }

# Always run from workspace root if possible
if command -v git >/dev/null 2>&1 && git rev-parse --show-toplevel >/dev/null 2>&1; then
  cd "$(git rev-parse --show-toplevel)"
fi

# Restore backend dependencies
if [ -f "Momentum.sln" ]; then
  say "[post-create] dotnet restore Momentum.sln"
  dotnet restore Momentum.sln
fi

# Restore frontend dependencies
if [ -f "src/web-core/package.json" ]; then
  say "[post-create] npm install (src/web-core)"
  if [ -f "src/web-core/package-lock.json" ]; then
    npm ci --prefix src/web-core
  else
    npm install --prefix src/web-core
  fi
fi

# Playwright (optional): only if dependency is present
if [ -f "src/web-core/package.json" ] && grep -q "\"@playwright/test\"" "src/web-core/package.json" 2>/dev/null; then
  say "[post-create] Playwright detected -> installing browsers"
  npx --yes --prefix src/web-core playwright install --with-deps || true
fi

# HTTPS dev certs (do not block container creation if something goes wrong)
if [ -f ".devcontainer/scripts/setup-https-certs.sh" ]; then
  say "[post-create] setup-https-certs (non-blocking)"
  bash .devcontainer/scripts/setup-https-certs.sh || true
fi

say "[post-create] done."

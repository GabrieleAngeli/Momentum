#!/usr/bin/env bash
set -euo pipefail

# Restore backend dependencies
if [ -f Momentum.sln ]; then
  dotnet restore Momentum.sln
fi

# Restore frontend dependencies
if [ -f src/web-core/package.json ]; then
  npm install --prefix src/web-core
fi

# Prepare Playwright browsers if end-to-end tests are present
if [ -d tests ]; then
  npx --yes playwright install --with-deps || true
fi

# Ensure development HTTPS certificates are available for ASP.NET services
bash "$(dirname "$0")/setup-https-certs.sh"

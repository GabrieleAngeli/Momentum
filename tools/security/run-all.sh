#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)
REPORT_DIR="${ROOT_DIR}/reports"
mkdir -p "${REPORT_DIR}"

export PREVIEW_BASE_URL="${PREVIEW_BASE_URL:-}"
export PATH="$HOME/.local/bin:$PATH"

log() {
  echo "[security] $*"
}

log "Refreshing vulnerability databases"
trivy --download-db-only
semgrep --update
nuclei -update-templates
if command -v dependency-check.sh >/dev/null 2>&1; then
  dependency-check.sh --updateonly
fi
if command -v osv-scanner >/dev/null 2>&1; then
  osv-scanner --lockfile --recursive "${ROOT_DIR}" || true
fi

log "Running dotnet build/test + audit"
if compgen -G "${ROOT_DIR}/*.sln" > /dev/null; then
  dotnet restore "${ROOT_DIR}/Momentum.sln"
  dotnet build "${ROOT_DIR}/Momentum.sln" --configuration Release --no-restore
  dotnet test "${ROOT_DIR}/Momentum.sln" --configuration Release --no-build --logger trx --results-directory "${REPORT_DIR}/dotnet-tests"
  dotnet list "${ROOT_DIR}/Momentum.sln" package --vulnerable --include-transitive --format json > "${REPORT_DIR}/dotnet-packages.json" || true
fi

log "Running Java dependency analysis"
if [ -f "${ROOT_DIR}/pom.xml" ]; then
  (cd "${ROOT_DIR}" && mvn -B -ntp verify)
fi
if compgen -G "${ROOT_DIR}/build.gradle*" > /dev/null; then
  (cd "${ROOT_DIR}" && ./gradlew build)
fi

log "Running npm audit"
if [ -f "${ROOT_DIR}/package.json" ]; then
  (cd "${ROOT_DIR}" && npm install --ignore-scripts)
  (cd "${ROOT_DIR}" && npm audit --json > "${REPORT_DIR}/npm-audit.json")
  (cd "${ROOT_DIR}" && npx retire --outputformat json --outputpath "${REPORT_DIR}/retire.json" || true)
fi

log "Running Semgrep"
semgrep --config p/owasp-top-ten --sarif --output "${REPORT_DIR}/sast-semgrep.sarif" || true

log "Running Gitleaks"
gitleaks detect --source "${ROOT_DIR}" --redact --report-format sarif --report-path "${REPORT_DIR}/secrets-gitleaks.sarif" --config "${ROOT_DIR}/.gitleaks.toml"

log "Running Trivy filesystem scan"
trivy fs --security-checks vuln,config,secret --format sarif --output "${REPORT_DIR}/sast-trivy-fs.sarif" "${ROOT_DIR}" || true

log "Generating SBOM with Syft"
syft dir:"${ROOT_DIR}" --output cyclonedx-json > "${REPORT_DIR}/sbom-cyclonedx.json"

log "Scanning SBOM with Grype"
grype sbom:"${REPORT_DIR}/sbom-cyclonedx.json" -o sarif > "${REPORT_DIR}/deps-grype.sarif" || true

if [ -n "${PREVIEW_BASE_URL}" ]; then
  log "Running Nuclei against ${PREVIEW_BASE_URL}"
  nuclei -target "${PREVIEW_BASE_URL}" -severity medium,high,critical -json -o "${REPORT_DIR}/dast-nuclei.json"
fi

log "Security run complete. Reports available in ${REPORT_DIR}"

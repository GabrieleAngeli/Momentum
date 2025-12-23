#!/usr/bin/env bash
set -euo pipefail

CERT_FOLDER="/home/vscode/.aspnet/https"
CERT_NAME="${CERT_NAME:-momentum-dev-cert}"
PASSWORD="${CERT_PASSWORD:-momentum-dev}"

PFX_PATH="${CERT_FOLDER}/${CERT_NAME}.pfx"
CRT_PATH="${CERT_FOLDER}/${CERT_NAME}.crt"
KEY_PATH="${CERT_FOLDER}/${CERT_NAME}.key"

say() { printf '%s\n' "$*"; }

mkdir -p "${CERT_FOLDER}"

# Ensure writable (volume/bind can be root-owned)
if command -v sudo >/dev/null 2>&1; then
  sudo chown -R vscode:vscode "${CERT_FOLDER}" 2>/dev/null || true
fi
chmod -R u+rwX "${CERT_FOLDER}" 2>/dev/null || true

# Ensure openssl exists (dotnet images usually have it, but be safe)
if ! command -v openssl >/dev/null 2>&1; then
  say "[https-certs] openssl missing. Install it in Dockerfile (apt-get install -y openssl) or add a feature."
  exit 0
fi

# 1) PFX for Kestrel (stable + easy)
if [ ! -f "${PFX_PATH}" ] || [ ! -s "${PFX_PATH}" ]; then
  say "[https-certs] Creating PFX: ${PFX_PATH}"
  dotnet dev-certs https --export-path "${PFX_PATH}" --password "${PASSWORD}"
fi

# 2) Extract CRT + KEY for Angular/Node dev server
# (Regenerate if missing or empty)
if [ ! -f "${KEY_PATH}" ] || [ ! -s "${KEY_PATH}" ] || [ ! -f "${CRT_PATH}" ] || [ ! -s "${CRT_PATH}" ]; then
  say "[https-certs] Extracting CRT/KEY for Angular:"
  say "  - ${CRT_PATH}"
  say "  - ${KEY_PATH}"

  # Private key (unencrypted, because dev server expects a plain key)
  openssl pkcs12 -in "${PFX_PATH}" \
    -nocerts -nodes \
    -passin pass:"${PASSWORD}" \
    -out "${KEY_PATH}" >/dev/null 2>&1

  # Certificate
  openssl pkcs12 -in "${PFX_PATH}" \
    -clcerts -nokeys \
    -passin pass:"${PASSWORD}" \
    -out "${CRT_PATH}" >/dev/null 2>&1

  chmod 600 "${KEY_PATH}" "${CRT_PATH}" 2>/dev/null || true
fi

say "[https-certs] Done."
say "[https-certs] PFX: ${PFX_PATH}"
say "[https-certs] CRT: ${CRT_PATH}"
say "[https-certs] KEY: ${KEY_PATH}"

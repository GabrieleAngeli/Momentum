#!/usr/bin/env bash
set -euo pipefail

PRIMARY_FOLDER="/home/vscode/.aspnet/https"
WINDOWS_MOUNT="/home/vscode/.aspnet/https-windows"
CERT_NAME="momentum-dev-cert"
PASSWORD="momentum-dev"

if [ -d "${WINDOWS_MOUNT}" ]; then
  mkdir -p "${WINDOWS_MOUNT}"
  if ! mountpoint -q "${PRIMARY_FOLDER}" 2>/dev/null; then
    rm -rf "${PRIMARY_FOLDER}" 2>/dev/null || true
    ln -s "${WINDOWS_MOUNT}" "${PRIMARY_FOLDER}" 2>/dev/null || true
  fi
fi

CERT_FOLDER="$(readlink -f "${PRIMARY_FOLDER}" 2>/dev/null || echo "${PRIMARY_FOLDER}")"
mkdir -p "${CERT_FOLDER}"

PFX_PATH="${CERT_FOLDER}/${CERT_NAME}.pfx"
CRT_PATH="${CERT_FOLDER}/${CERT_NAME}.crt"

if [ ! -f "${PFX_PATH}" ] || [ ! -f "${CRT_PATH}" ]; then
  echo "[devcontainer] Preparing development HTTPS certificates in ${CERT_FOLDER}"
fi

if [ ! -f "${PFX_PATH}" ]; then
  dotnet dev-certs https --trust >/dev/null 2>&1 || true
  dotnet dev-certs https --export-path "${PFX_PATH}" --password "${PASSWORD}"
fi

if [ ! -f "${CRT_PATH}" ]; then
  dotnet dev-certs https --export-path "${CRT_PATH}" --format Pem --no-password
fi

if [ -f "${PFX_PATH}" ]; then
  chmod 600 "${PFX_PATH}"
fi

if [ -f "${CRT_PATH}" ]; then
  chmod 600 "${CRT_PATH}"
fi

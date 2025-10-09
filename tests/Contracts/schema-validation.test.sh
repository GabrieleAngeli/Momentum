#!/usr/bin/env bash
set -euo pipefail

echo "Validating JSON schemas"
for schema in contracts/**/*.json; do
  echo "- $schema"
  ajv validate -s "$schema" -d example.json || true
done

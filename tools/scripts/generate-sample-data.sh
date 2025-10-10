#!/usr/bin/env bash
set -euo pipefail

BROKER_URL=${BROKER_URL:-"localhost:9092"}
TOPIC=${TOPIC:-"telemetry.input"}

for i in {1..10}; do
  payload=$(jq -n --arg id "$(uuidgen)" --arg source "demo-sensor" --arg type "temperature" --argjson value "$(shuf -i 1-100 -n 1)" --arg timestamp "$(date -u +%Y-%m-%dT%H:%M:%SZ)" '{id:$id, source:$source, type:$type, timestamp:$timestamp, value:$value, metadata:{unit:"C"}}')
  echo "Sending $payload"
  kafka-console-producer --broker-list "$BROKER_URL" --topic "$TOPIC" <<< "$payload"
done

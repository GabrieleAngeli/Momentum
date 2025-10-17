# Observability Guidelines

## Telemetry baseline
- OpenTelemetry SDK is configured in all services for metrics and traces.
- Default export path: OTLP → Tempo, Prometheus scrape, Loki for logs.
- Every service exposes `/healthz` and `/metrics` endpoints.
- Grafana dashboards are versioned under `observability/dashboards` (TODO).
- Observability stack relies exclusively on OSS components (Prometheus, Loki, Tempo, Grafana OSS).
- Modular monolith deployments emit the same telemetry envelopes to ensure consistent dashboards regardless of runtime topology.

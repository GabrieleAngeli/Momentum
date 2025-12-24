# Observability Guidelines

## Telemetry baseline
- OpenTelemetry SDK is configured in the .NET services for traces; Prometheus metrics exporters are enabled where configured.
- There is no OTLP exporter configured in code yet, so Tempo/Loki ingestion requires additional setup.
- Health endpoints:
  - `/healthz` for `identifier`, `streamer`, `notifier`, `web-backend-core`, `modular-monolith`.
  - `/health` for `core-web`.
- Metrics endpoints:
  - `/metrics` is wired with Prometheus exporter in `identifier`.
  - Other services expose `/metrics` as a placeholder string response today.
- Grafana dashboards will be versioned under `observability/dashboards/` once the first dashboards are published (TODO).
- Observability stack relies exclusively on OSS components (Prometheus, Loki, Tempo, Grafana OSS).
- Modular monolith deployments emit the same telemetry envelopes to ensure consistent dashboards regardless of runtime topology.

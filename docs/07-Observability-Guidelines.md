# Observability Guidelines / Linee Guida per l'Osservabilità

## Telemetry baseline / Baseline di telemetria
**English:**
- OpenTelemetry SDK is configured in all services for metrics and traces.
- Default export path: OTLP → Tempo, Prometheus scrape, Loki for logs.
- Every service exposes `/healthz` and `/metrics` endpoints.
- Grafana dashboards are versioned under `observability/dashboards` (TODO).
- Observability stack relies exclusively on OSS components (Prometheus, Loki, Tempo, Grafana OSS).

**Italiano:**
- L'SDK OpenTelemetry è configurato in tutti i servizi per metriche e trace.
- Flusso di esportazione predefinito: OTLP → Tempo, Prometheus per lo scrape, Loki per i log.
- Ogni servizio espone gli endpoint `/healthz` e `/metrics`.
- Le dashboard Grafana sono versionate in `observability/dashboards` (TODO).
- Lo stack di osservabilità si basa esclusivamente su componenti OSS (Prometheus, Loki, Tempo, Grafana OSS).

# Observability Guidelines

- OpenTelemetry SDK configurato in tutti i servizi (metrics + traces).
- Esportazione default: OTLP → Tempo, Prometheus scrape, Loki per logs.
- Ogni servizio espone `/healthz` e `/metrics`.
- Dashboards Grafana versionate nella cartella `observability/dashboards` (TODO).

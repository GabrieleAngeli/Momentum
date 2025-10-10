# Architecture Overview (C4)

## C1 – System Context

- **Momentum Platform** for ingesting telemetry, managing identities and broadcasting notifications.
- External Actors: Operators (web UI), Sensor producers (Kafka), Email/SMS providers, Observability stack.

## C2 – Container Diagram

- **web-core (Angular)**: shell, consumes SignalR, federated modules
- **web-backend-core (.NET)**: API gateway + SignalR hub + OpenFeature integration
- **identifier service**: gRPC auth, JWT minting
- **streamer service**: Kafka consumer, TimescaleDB persistence, Ignite cache
- **notifier service**: Subscribes to `telemetry.ingested`, dispatches email/SignalR
- **Infrastructure**: Kafka, TimescaleDB, Ignite, Prometheus, Loki, Tempo, Grafana, Dapr sidecars

## C3 – Component Diagram (web-backend-core)

- **Controllers** orchestrate requests
- **NotificationOrchestrator** coordinates broadcast
- **SignalRNotificationBroadcaster** publishes to hub
- **NotificationHub** exposes realtime channel
- **Dapr Client** handles module invocations

## C4 – Code/Module View

- Clean architecture layers per servizio (`Domain` → `Application` → `Infrastructure` → `Api`)
- Contracts versionati in `/contracts`
- Tests in `/tests`
- Module federation per front-end modulare

## Quality Attributes

- **Resilienza**: Dapr retries, Kafka buffering, Ignite caching
- **Osservabilità**: OpenTelemetry/Prometheus, logs su Loki, traces su Tempo
- **Sicurezza**: JWT, ruoli, feature flag via OpenFeature
- **Evolvibilità**: Contratti stabili, bounded context per modulo, moduli plug-in

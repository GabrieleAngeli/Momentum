# Architecture Overview (C4)

## C1 – System Context
- **Momentum Platform** ingests telemetry, manages identities, and broadcasts notifications.
- External actors: Operators (web UI), sensor producers (Kafka), email/SMS providers, observability stack.

## C2 – Container Diagram
- **web-core (Angular):** shell consuming SignalR and federated modules.
- **web-backend-core (.NET):** API gateway + SignalR hub + OpenFeature integration.
- **modular-monolith (.NET):** Unified façade invoking all services via Dapr and acting as the reference architecture for modular monolith deployments.
- **identifier service:** gRPC auth, JWT minting.
- **streamer service:** Kafka consumer, TimescaleDB persistence, Ignite cache.
- **notifier service:** Subscribes to `telemetry.ingested`, dispatches email/SignalR.
- **Infrastructure:** Kafka, TimescaleDB, Ignite, Prometheus, Loki, Tempo, Grafana, Dapr sidecars.

## C3 – Component Diagram (web-backend-core)
- Controllers orchestrate requests.
- `NotificationOrchestrator` coordinates broadcasts.
- `SignalRNotificationBroadcaster` publishes to the hub.
- `NotificationHub` exposes the realtime channel.
- `DaprClient` handles module invocations.

## C4 – Code/Module View
- Clean architecture layers per service (`Domain` → `Application` → `Infrastructure` → `Api`).
- Versioned contracts in `/contracts`.
- Tests in `/tests`.
- Module federation powering the modular frontend.

## Quality Attributes
- **Resilience:** Dapr retries, Kafka buffering, Ignite caching.
- **Observability:** OpenTelemetry/Prometheus, logs in Loki, traces in Tempo.
- **Security:** JWT, roles, feature flags via OpenFeature.
- **Evolvability:** Stable contracts, bounded contexts per module, plug-in modules.

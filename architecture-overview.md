# Architecture Overview (C4) / Panoramica Architetturale (C4)

## C1 – System Context / Contesto di Sistema
**English:**
- **Momentum Platform** ingests telemetry, manages identities, and broadcasts notifications.
- External actors: Operators (web UI), sensor producers (Kafka), email/SMS providers, observability stack.

**Italiano:**
- **Momentum Platform** raccoglie telemetria, gestisce identità e pubblica notifiche.
- Attori esterni: Operatori (web UI), produttori di sensori (Kafka), provider email/SMS, stack di osservabilità.

## C2 – Container Diagram / Diagramma dei Container
**English:**
- **web-core (Angular):** shell consuming SignalR and federated modules.
- **web-backend-core (.NET):** API gateway + SignalR hub + OpenFeature integration.
- **modular-monolith (.NET):** Unified façade invoking all services via Dapr.
- **identifier service:** gRPC auth, JWT minting.
- **streamer service:** Kafka consumer, TimescaleDB persistence, Ignite cache.
- **notifier service:** Subscribes to `telemetry.ingested`, dispatches email/SignalR.
- **Infrastructure:** Kafka, TimescaleDB, Ignite, Prometheus, Loki, Tempo, Grafana, Dapr sidecars.

**Italiano:**
- **web-core (Angular):** shell che consuma SignalR e moduli federati.
- **web-backend-core (.NET):** API gateway + hub SignalR + integrazione OpenFeature.
- **modular-monolith (.NET):** Facciata unificata che invoca tutti i servizi tramite Dapr.
- **identifier service:** autenticazione gRPC, emissione JWT.
- **streamer service:** consumer Kafka, persistenza TimescaleDB, cache Ignite.
- **notifier service:** si sottoscrive a `telemetry.ingested`, inoltra email/SignalR.
- **Infrastruttura:** Kafka, TimescaleDB, Ignite, Prometheus, Loki, Tempo, Grafana, sidecar Dapr.

## C3 – Component Diagram (web-backend-core) / Diagramma dei Componenti (web-backend-core)
**English:**
- Controllers orchestrate requests.
- `NotificationOrchestrator` coordinates broadcasts.
- `SignalRNotificationBroadcaster` publishes to the hub.
- `NotificationHub` exposes the realtime channel.
- `DaprClient` handles module invocations.

**Italiano:**
- I controller orchestrano le richieste.
- `NotificationOrchestrator` coordina i broadcast.
- `SignalRNotificationBroadcaster` pubblica sull'hub.
- `NotificationHub` espone il canale realtime.
- `DaprClient` gestisce le invocazioni ai moduli.

## C4 – Code/Module View / Vista Code/Modulo
**English:**
- Clean architecture layers per service (`Domain` → `Application` → `Infrastructure` → `Api`).
- Versioned contracts in `/contracts`.
- Tests in `/tests`.
- Module federation powering the modular frontend.

**Italiano:**
- Strati clean architecture per servizio (`Domain` → `Application` → `Infrastructure` → `Api`).
- Contratti versionati in `/contracts`.
- Test in `/tests`.
- Module federation a supporto del frontend modulare.

## Quality Attributes / Attributi di Qualità
**English:**
- **Resilience:** Dapr retries, Kafka buffering, Ignite caching.
- **Observability:** OpenTelemetry/Prometheus, logs in Loki, traces in Tempo.
- **Security:** JWT, roles, feature flags via OpenFeature.
- **Evolvability:** Stable contracts, bounded contexts per module, plug-in modules.

**Italiano:**
- **Resilienza:** retry Dapr, buffering Kafka, caching Ignite.
- **Osservabilità:** OpenTelemetry/Prometheus, log su Loki, trace su Tempo.
- **Sicurezza:** JWT, ruoli, feature flag con OpenFeature.
- **Evolvibilità:** Contratti stabili, bounded context per modulo, moduli plug-in.

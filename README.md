# Momentum Platform / Piattaforma Momentum

## Overview / Panoramica
**English:** Momentum is a modular and resilient platform that can run as a modular monolith or evolve into a microservice topology without changing public contracts.

**Italiano:** Momentum è una piattaforma modulare e resiliente che può funzionare come monolite modulare oppure evolvere in una topologia a microservizi senza modificare i contratti pubblici.

## Stack
**English:**
- **Backend:** .NET 8 Aspire with Dapr over gRPC and Kafka pub/sub.
- **Frontend:** Angular 19 (standalone, i18n, module federation ready).
- **Data:** TimescaleDB for historical storage, Apache Ignite for caching.
- **Messaging:** Kafka for telemetry events.
- **Realtime:** SignalR and notification channels.
- **Observability:** OpenTelemetry exported to Prometheus, Loki, Tempo, Grafana.
- **Security:** JWT with OpenFeature-ready hooks.
- **Licensing:** OSS-only dependencies (Docker images `-oss`/Debian and community libraries).

**Italiano:**
- **Backend:** .NET 8 Aspire con Dapr su gRPC e pub/sub Kafka.
- **Frontend:** Angular 19 (standalone, i18n, predisposto per module federation).
- **Dati:** TimescaleDB per lo storico, Apache Ignite per la cache.
- **Messaging:** Kafka per gli eventi telemetrici.
- **Realtime:** SignalR e canali di notifica.
- **Osservabilità:** OpenTelemetry esportato verso Prometheus, Loki, Tempo, Grafana.
- **Sicurezza:** JWT con integrazione OpenFeature-ready.
- **Licensing:** Dipendenze solo OSS (immagini Docker `-oss`/Debian e librerie community).

## Quick start / Avvio rapido
**English:**
```bash
make build
make dev
```
Orchestrate the full topology with Aspire:
```bash
dotnet run --project src/AppHost/Momentum.AppHost.csproj
```

**Italiano:**
```bash
make build
make dev
```
Per orchestrare l'intera topologia con Aspire:
```bash
dotnet run --project src/AppHost/Momentum.AppHost.csproj
```

## Devcontainer
**English:** The repository ships a ready-to-use [Dev Container](https://containers.dev/) configuration in `.devcontainer/`. It ensures a consistent environment for builds, tests, and security checks. Follow the detailed instructions in [`docs/devcontainer.md`](docs/devcontainer.md).

**Italiano:** Il repository include una configurazione [Dev Container](https://containers.dev/) pronta all'uso in `.devcontainer/`. Garantisce un ambiente coerente per build, test e controlli di sicurezza. Le istruzioni dettagliate sono in [`docs/devcontainer.md`](docs/devcontainer.md).

## Services / Servizi
**English:**
| Service | Description |
| --- | --- |
| identifier | Authentication and license management |
| streamer | Telemetry ingestion from Kafka → Timescale/Ignite |
| notifier | Multi-channel notification dispatch |
| web-backend-core | API gateway + SignalR hub |
| web-core | Modular Angular frontend |

**Italiano:**
| Servizio | Descrizione |
| --- | --- |
| identifier | Autenticazione e gestione licenze |
| streamer | Ingest telemetria da Kafka → Timescale/Ignite |
| notifier | Invio notifiche multi-canale |
| web-backend-core | Gateway API + hub SignalR |
| web-core | Frontend modulare Angular |

## End-to-end flow / Flusso end-to-end
**English:**
1. A raw event arrives on `telemetry.input` (Kafka).
2. The streamer transforms it, persists to Timescale, invalidates Ignite cache, and publishes `telemetry.ingested`.
3. The notifier subscribes to the event, delivers mail/SignalR via web-backend-core.
4. Web-backend-core broadcasts on the SignalR hub.
5. Web-core receives the realtime notification and updates the dashboard.

**Italiano:**
1. Un evento grezzo arriva su `telemetry.input` (Kafka).
2. Lo streamer lo trasforma, lo persiste su Timescale, invalida la cache Ignite e pubblica `telemetry.ingested`.
3. Il notifier sottoscrive l'evento, invia mail/SignalR tramite web-backend-core.
4. Web-backend-core trasmette sull'hub SignalR.
5. Web-core riceve la notifica realtime e aggiorna la dashboard.

## Useful scripts / Script utili
**English:**
- `tools/scripts/generate-sample-data.sh` seeds demo events.
- `make test` runs .NET and Angular tests (best-effort in CI).
- `make contracts` produces the contract bundle.

**Italiano:**
- `tools/scripts/generate-sample-data.sh` genera eventi demo.
- `make test` esegue test .NET e Angular (best-effort in CI).
- `make contracts` produce l'archivio dei contratti.

## Deployment / Deploy
**English:**
- `docker compose up` spins up the full environment (infrastructure + services).
- `src/AppHost` hosts local orchestration with .NET Aspire.

**Italiano:**
- `docker compose up` avvia l'ambiente completo (infra + servizi).
- `src/AppHost` gestisce l'orchestrazione locale con .NET Aspire.

## Repository configuration / Configurazione del repository
**English:**
- `Directory.Build.props` centralises common .NET build settings (nullable, analyzers, warnings-as-errors).
- `Directory.Packages.props` manages package versions and floating constraints shared across projects.
- `NuGet.config` pins internal feeds and enables repeatable restore.
- `global.json` locks the .NET SDK version used by CI and devcontainer.
- `Makefile` orchestrates build, test, lint, contracts, and release automation.
- `docker-compose*.yml` files define local/CI infrastructure for databases, Kafka, and observability.
- `ReleaseNotes/` collects generated release artefacts per version.

**Italiano:**
- `Directory.Build.props` centralizza le impostazioni comuni di build .NET (nullable, analyzer, warnings-as-errors).
- `Directory.Packages.props` gestisce le versioni dei pacchetti e i vincoli condivisi fra i progetti.
- `NuGet.config` blocca i feed interni e abilita restore ripetibili.
- `global.json` vincola la versione di .NET SDK usata da CI e devcontainer.
- `Makefile` coordina build, test, lint, contratti e automazioni di release.
- I file `docker-compose*.yml` definiscono l'infrastruttura locale/CI per database, Kafka e osservabilità.
- `ReleaseNotes/` raccoglie gli artefatti di release generati per versione.

## Documentation / Documentazione
**English:**
- [`architecture-overview.md`](architecture-overview.md) describes the system using the C4 model.
- [`docs/`](docs/) contains policies, guidelines, ADRs, and operational manuals.
- [`contracts/`](contracts/) holds proto/OpenAPI/event schemas.

**Italiano:**
- [`architecture-overview.md`](architecture-overview.md) descrive il sistema con il modello C4.
- [`docs/`](docs/) ospita policy, linee guida, ADR e manuali operativi.
- [`contracts/`](contracts/) contiene i contratti proto/OpenAPI/eventi.

## Process automation / Automazioni di processo
**English:**
- **Ensure issue linking:** workflow that enforces issue references in commits. When missing, it opens a summary ticket and updates the pull request description.
- **Generate release notes:** manual workflow that creates release note files for the repository and modules, stored under `ReleaseNotes/` with the provided version number.

**Italiano:**
- **Ensure issue linking:** workflow che impone la presenza del riferimento alle issue nei commit. In mancanza, apre un ticket riassuntivo e aggiorna la descrizione della pull request.
- **Generate release notes:** workflow manuale che crea i file di release note per repository e moduli, salvati in `ReleaseNotes/` con il numero di versione fornito.

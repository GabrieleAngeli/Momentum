# Momentum Platform

## Overview
Momentum is a modular and resilient platform that can operate as a modular monolith or evolve into a microservice topology without changing public contracts. The `modular-monolith` service provides the reference implementation for the aggregated runtime, and developers should follow the dedicated [Modular Architecture Guidelines](docs/06-Modular-Architecture-Guidelines.md) when extending it. Additional architectural context is available in the [architecture overview](architecture-overview.md).

## Stack
- **Backend:** .NET 8 Aspire with Dapr over gRPC and Kafka pub/sub.
- **Frontend:** Angular 19 (standalone, i18n, module federation ready).
- **Data:** TimescaleDB for historical storage, Apache Ignite for caching.
- **Messaging:** Kafka for telemetry events.
- **Realtime:** SignalR and notification channels.
- **Observability:** OpenTelemetry exported to Prometheus, Loki, Tempo, Grafana.
- **Security:** JWT with OpenFeature-ready hooks.
- **Licensing:** OSS-only dependencies (Docker images `-oss`/Debian and community libraries).

## Quick start
```bash
make build
make dev
```
Orchestrate the full topology with Aspire:
```bash
dotnet run --project src/AppHost/Momentum.AppHost.csproj
```

## Devcontainer
The repository ships a ready-to-use [Dev Container](https://containers.dev/) configuration in `.devcontainer/`. It ensures a consistent environment for builds, tests, and security checks. Follow the detailed instructions in [`docs/devcontainer.md`](docs/devcontainer.md).

## Services
| Service | Description |
| --- | --- |
| identifier | Authentication and license management |
| streamer | Telemetry ingestion from Kafka → Timescale/Ignite |
| notifier | Multi-channel notification dispatch |
| web-backend-core | API gateway + SignalR hub |
| modular-monolith | Aggregates all backend capabilities via Dapr |
| web-core | Modular Angular frontend |

## End-to-end flow
1. A raw event arrives on `telemetry.input` (Kafka).
2. The streamer transforms it, persists to Timescale, invalidates Ignite cache, and publishes `telemetry.ingested`.
3. The notifier subscribes to the event, delivers mail/SignalR via web-backend-core.
4. Web-backend-core broadcasts on the SignalR hub.
5. Web-core receives the realtime notification and updates the dashboard.

## Useful scripts
- `tools/scripts/generate-sample-data.sh` seeds demo events.
- `make test` runs .NET and Angular tests (best-effort in CI).
- `make contracts` produces the contract bundle.

## Deployment
- `docker compose up` spins up the full environment (infrastructure + services).
- `src/AppHost` hosts local orchestration with .NET Aspire.

## Repository configuration
- `Directory.Build.props` centralises common .NET build settings (nullable, analyzers, warnings-as-errors).
- `Directory.Packages.props` manages package versions and floating constraints shared across projects.
- `NuGet.config` pins internal feeds and enables repeatable restore.
- `global.json` locks the .NET SDK version used by CI and devcontainer.
- `Makefile` orchestrates build, test, lint, contracts, and release automation.
- `docker-compose*.yml` files define local/CI infrastructure for databases, Kafka, and observability.
- `ReleaseNotes/` collects generated release artefacts per version.

## Documentation
- [`architecture-overview.md`](architecture-overview.md) describes the system using the C4 model and highlights how the modular monolith encapsulates domain capabilities behind Dapr endpoints.
- [`docs/06-Modular-Architecture-Guidelines.md`](docs/06-Modular-Architecture-Guidelines.md) acts as the developer playbook for modular monolith extensions and module onboarding.
- [`docs/02-Coding-Guidelines.md`](docs/02-Coding-Guidelines.md) and the remaining policies under `docs/` cover day-to-day engineering practices.
- [`contracts/`](contracts/) holds proto/OpenAPI/event schemas.

## Process automation
- **Ensure issue linking:** workflow that enforces issue references in commits. When missing, it opens a summary ticket and updates the pull request description.
- **Generate release notes:** manual workflow that creates release note files for the repository and modules, stored under `ReleaseNotes/` with the provided version number.

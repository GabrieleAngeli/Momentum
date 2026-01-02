# Momentum Platform

[![Security Checks](../../actions/workflows/security-pr.yml/badge.svg?branch=main)](../../actions/workflows/security-pr.yml)

## Overview
Momentum is a modular and resilient IoT platform that can operate as a modular monolith or evolve into a microservice topology without changing public contracts. The `modular-monolith` service provides the reference implementation for the aggregated runtime, and developers should follow the dedicated [Modular Architecture Guidelines](docs/06-Modular-Architecture-Guidelines.md) when extending it. Additional architectural context is available in the [architecture overview](architecture-overview.md).

## Stack
- **Backend:** .NET 8 with Aspire AppHost for local orchestration and Dapr sidecars (service invocation + pub/sub).
- **Frontend:** Angular 19 shell with runtime module federation, shared providers (auth, flags, i18n) and a neutral UI kit.
- **Data:** TimescaleDB for telemetry persistence; Apache Ignite is provisioned for caching (no cache usage is wired in code yet).
- **Messaging:** Kafka (Redpanda in `docker-compose.yml`) for telemetry events.
- **Realtime:** SignalR hubs in `web-backend-core` (notifications) and `core-web` (UI shell).
- **Observability:** OpenTelemetry instrumentation with Prometheus metrics exporters enabled in the .NET services (traces require an OTLP exporter configuration).
- **Security:** JWT authentication; OpenFeature is configured in `web-backend-core` with an in-memory provider.
- **Licensing:** OSS-only dependencies (Grafana OSS, Timescale OSS images, community libraries).

## Quick start
```bash
make build
make dev
```
`make dev` uses `docker compose` to run infrastructure plus the distributed services (`identifier`, `streamer`, `notifier`, `web-backend-core`) and the `web-core` UI. Dapr sidecars are **not** started in this path.

Orchestrate the full topology with Aspire (includes Dapr sidecars, `core-web`, and the modular monolith):
```bash
dotnet run --project src/AppHost/Momentum.AppHost.csproj
```

## Development patterns
- **Domain-driven design + Clean Architecture:** Each bounded context follows the `Domain → Application → Infrastructure → Api` layering enforced by solution folder structure and shared build props.
- **Modular monolith first:** New capabilities should be implemented inside the `modular-monolith` service (or as modules under [`modules/`](modules/)). When extraction to independent services is required, keep the public contracts in sync with the modular façade.
- **Event-driven integration:** Services communicate through Dapr bindings and Kafka topics; long-running workflows are composed via pub/sub events instead of synchronous chains.
- **Contract-first evolution:** gRPC/OpenAPI/schema artefacts in [`contracts`](contracts/) are versioned; JSON schemas are validated in CI (`tests/Contracts/schema-validation.test.sh`), while other contract checks are still to be automated.
- **Infrastructure as code:** Local environments rely on Aspire or `docker compose`. Production IaC is not yet tracked in this repository (no `infra/` directory).

### How to extend the platform
1. Shape the domain model and contracts inside the appropriate module under [`modules/`](modules/) and [`contracts/`](contracts/).
2. Implement use cases using CQRS handlers in `Application`, adapters in `Infrastructure`, and façade endpoints in `Api`.
3. Register Dapr components (bindings, pub/sub) and module metadata (`contracts/web-core/module-manifest.json`).
4. Cover the feature with unit, integration, and contract tests via `make test` and module-specific pipelines.
5. Update documentation (`docs/`) and release notes to reflect the new capability.

### Documentation map
- **Architecture policies:** [`docs/01-Architecture-Policy.md`](docs/01-Architecture-Policy.md) and [`architecture-overview.md`](architecture-overview.md) capture the C4 views and architectural guardrails.
- **Engineering handbook:** [`docs/02-Coding-Guidelines.md`](docs/02-Coding-Guidelines.md) through [`docs/08-Data-Guidelines.md`](docs/08-Data-Guidelines.md) describe coding, testing, security, observability, and data expectations.
- **Module playbook:** [`docs/06-Modular-Architecture-Guidelines.md`](docs/06-Modular-Architecture-Guidelines.md) explains how to build and ship modules consistently with the modular monolith.
- **Process automation:** [`docs/release-process.md`](docs/release-process.md) and CI workflows (`.github/workflows/`) document the release and compliance pipelines.

## Devcontainer
The repository ships a ready-to-use [Dev Container](https://containers.dev/) configuration in `.devcontainer/`. It ensures a consistent environment for builds, tests, and security checks. Follow the detailed instructions in [`docs/devcontainer.md`](docs/devcontainer.md).

## Services
| Service | Description |
| --- | --- |
| core-web | UI gateway exposing manifest, feature flags, menu, i18n, and auth for micro frontends |
| identifier | Authentication and license management |
| streamer | Telemetry ingestion from Kafka → Timescale |
| notifier | Multi-channel notification dispatch |
| web-backend-core | API gateway + SignalR hub |
| modular-monolith | Aggregates all backend capabilities via Dapr |
| web-core | Modular Angular frontend |

> **Note:** `docker-compose.yml` runs `identifier`, `streamer`, `notifier`, `web-backend-core`, and `web-core`. `core-web` and `modular-monolith` are currently wired only in the Aspire AppHost (`src/AppHost`).

### Modules

- [`ticketing`](modules/ticketing/README.md): sample plug-in published via Dapr and exposed to the frontend through [`contracts/web-core/module-manifest.json`](contracts/web-core/module-manifest.json).

## End-to-end flow
1. A raw event arrives on `telemetry.input` (Kafka).
2. The streamer transforms it, persists to Timescale, and publishes `telemetry.ingested` via Dapr/Kafka.
3. The notifier subscribes to the event and dispatches notifications (SignalR via `web-backend-core`, plus email if configured).
4. Web-backend-core broadcasts on the SignalR hub.
5. Any clients connected to the `web-backend-core` SignalR hub receive the realtime notification (the `web-core` UI currently connects to the `core-web` hub for shell updates).

## Useful scripts
- `tools/scripts/generate-sample-data.sh` seeds demo events.
- `make test` runs .NET and Angular tests (best-effort in CI).
- `make security` refreshes vulnerability databases and runs the multilayer security suite described in [`docs/security/README.md`](docs/security/README.md).
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
- [`architecture-overview.md`](architecture-overview.md) captures the C4 model and operational views of the platform.
- [`docs/`](docs/) hosts policies for architecture, coding, testing, security, observability, and data.
- [`docs/adr/`](docs/adr/) stores Architecture Decision Records.
- [`contracts/`](contracts/) holds proto/OpenAPI/event schemas and module manifests consumed by clients.

## Process automation
- **Ensure issue linking:** workflow that enforces issue references in commits. When missing, it opens a summary ticket and updates the pull request description.
- **Generate release notes:** manual workflow that creates release note files for the repository and modules, stored under `ReleaseNotes/` with the provided version number.


## Backlog & Outstanding Work
- **Internationalisation**
  - [ ] Gestione delle traduzioni lato backoffice
  - [ ] Astrazione per l'utilizzo lato frontend
  - [ ] Astrazione per l'utilizzo lato backend
- **Feature flags**
  - [ ] Area di configurazione
  - [ ] SDK/adapter per il frontend
  - [ ] SDK/adapter per il backend
- **Licensing**
  - [ ] Portale di gestione licenze
  - [ ] Integrazione lato frontend
  - [ ] Integrazione lato backend
- **Platform capabilities**
  - [ ] Federation runtime hardening
  - [ ] Gestione asset
  - [ ] Gestione clienti
  - [ ] Gestione spedizioni
  - [ ] Reporting
  - [ ] Flow manager
  - [ ] Notifier enhancements
  - [ ] Realtime hardening
  - [ ] Stato del sistema
  - [ ] Integrazione Sentry

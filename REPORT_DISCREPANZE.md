# Documentation Discrepancy Report (Momentum)

## Phase 0 — Inventory

### Repository structure (top-level)
- `src/` (services, shared libraries, web-core)
- `contracts/` (proto/OpenAPI schemas and module manifest)
- `modules/` (sample module docs/contracts)
- `docs/` (architecture, policies, security, ADRs)
- `tests/` (backend + frontend tests)
- `tools/` (release automation, security, CI helpers)
- `docker-compose*.yml`, `Makefile`, `Momentum.sln`

### Documentation files discovered
- `README.md`, `architecture-overview.md`
- `docs/**/*.md` (architecture, guidelines, policies, ADRs, devcontainer)
- `modules/ticketing/README.md`
- `tools/**/README.md`
- `ReleaseNotes/*.md`, `SECURITY.md`

### Stack/toolchain (evidence)
- .NET 8 SDK (`global.json`)
- Aspire AppHost + Dapr sidecars (`src/AppHost/AppHost.cs`)
- Angular 19 (`src/web-core/package.json`)
- Docker Compose runtime (`docker-compose.yml`)
- CI workflows (`.github/workflows/*.yml`)

## Phase 1 — Reality map

### Entrypoints & runtime topologies
- **Aspire AppHost**: `dotnet run --project src/AppHost/Momentum.AppHost.csproj` provisions infra containers + Dapr sidecars and starts `core-web`, `identifier`, `streamer`, `notifier`, `web-backend-core`, `modular-monolith`, `web-core` container (`src/AppHost/AppHost.cs`).
- **Docker Compose**: `docker compose up` provisions Kafka (Redpanda), Timescale, Ignite, Prometheus/Loki/Tempo/Grafana and starts `identifier`, `streamer`, `notifier`, `web-backend-core`, `web-core` (`docker-compose.yml`). Dapr sidecars are not part of this topology.

### Core services & responsibilities
- `core-web` API: manifest/menu/i18n/auth endpoints + SignalR hub (`src/services/core-web/CoreWeb.Api/Program.cs`).
- `web-backend-core`: API gateway + SignalR hub, OpenFeature in-memory provider (`src/services/web-backend-core/WebBackendCore.Api/Program.cs`).
- `identifier`: RBAC/licensing/flags, HTTP + gRPC, PostgreSQL/Timescale persistence (`src/services/identifier/Identifier.Api/Program.cs`).
- `streamer`: Kafka consumer → Timescale persistence → Dapr pub/sub (`src/services/streamer/Streamer.Infrastructure/*`).
- `notifier`: Dapr subscriber to `telemetry.ingested`, dispatches SignalR + email (`src/services/notifier/Notifier.Api/Controllers/SubscriptionsController.cs`, `Notifier.Infrastructure/Channels/*`).

### Contracts & APIs
- gRPC/OpenAPI contracts in `contracts/*` (e.g. `contracts/identifier/identifier.proto`, `contracts/web-backend-core/orchestrator.yaml`).
- Module manifest in `contracts/web-core/module-manifest.json`.

### Configuration & dependencies
- AppHost uses Timescale, Kafka (Redpanda), Ignite, Prometheus/Loki/Tempo/Grafana, Redis container (provisioned only), and Dapr components under `src/AppHost/mounts/dapr/components` (currently placeholder files).
- Compose uses the same infra containers but does not mount or run Dapr sidecars.

### CI/CD
- `pr-gate.yml` runs .NET build/test, frontend lint/tests, and JSON schema validation (`tests/Contracts/schema-validation.test.sh`).
- `release.yml` drives release automation (`tools/release_notes/ci_release.py`).
- `security-pr.yml` runs the security suite (`tools/security/run-all.sh`).

## Phase 2 — Documentation vs reality (mismatch table)

| File/Section | Type | Evidence | Proposed action |
| --- | --- | --- | --- |
| `README.md` stack/quick start/deployment | O/E | `docker-compose.yml`, `src/AppHost/AppHost.cs`, `Makefile` | Clarify compose vs Aspire topology, Dapr availability, and service coverage. |
| `README.md` end-to-end flow | E | `Streamer.Infrastructure/Ingestion/*`, `Notifier.Api/*` | Remove Ignite cache invalidation and describe actual Dapr/Kafka + SignalR flow. |
| `architecture-overview.md` C3 components | E | `src/services/web-backend-core/*` (no Outbox/FeatureFlagProvider) | Update diagram and component list to match code. |
| `architecture-overview.md` data & Dapr sections | E/O | `src/AppHost/mounts/dapr/components/*`, `src/services/*/Migrations` | Note placeholder Dapr components and actual migrations usage. |
| `docs/01-Architecture-Policy.md` ADR naming | E | `docs/adr/0001.md` | Align ADR naming guidance with current numbering. |
| `docs/04-Security-Policy.md` controls | E | `src/services/core-web/CoreWeb.Api/Program.cs`, `src/services/web-backend-core/WebBackendCore.Api/Program.cs` | Remove Key Vault/OpenFeature binding claims; document current config. |
| `docs/05-Testing-Policy.md` test flow | E/O | `Makefile`, `.github/workflows/pr-gate.yml`, `tests/Identifier/*` | Replace buf/spectral/Testcontainers claims with actual CI and tests. |
| `docs/06-Modular-Architecture-Guidelines.md` contracts/modules layout | E | `contracts/`, `modules/ticketing/README.md` | Clarify service-centric contracts and current module state. |
| `docs/07-Observability-Guidelines.md` endpoints/export | E | `src/services/*/Program.cs` | Update health/metrics endpoints and exporter status. |
| `docs/identifier/README.md` database/ports/tests | E | `Identifier.Api/appsettings.json`, `IdentifierDbContextFactory.cs`, `Identifier.Api/Program.cs`, `tests/Identifier/*` | Update to Postgres, correct ports, Testcontainers usage, and Dapr seed topic. |

Legend: (M) Missing, (O) Obsolete, (E) Erroneous, (D) Duplicate.

## Phase 3 — Plan of modification (file-by-file)

- `README.md`
  - Clarify Docker Compose vs Aspire behavior and Dapr availability.
  - Fix end-to-end flow to match streamer/notifier behavior.
  - Note actual service list coverage per topology.
- `architecture-overview.md`
  - Update container/component diagrams, remove nonexistent components.
  - Align Dapr, data, and event flow sections with current code.
- `docs/01-Architecture-Policy.md`
  - Update ADR naming guidance and IaC statement.
- `docs/04-Security-Policy.md`
  - Replace Key Vault/OpenFeature binding claims with current JWT + OpenFeature setup.
- `docs/05-Testing-Policy.md`
  - Document actual tests, CI workflow, and schema validation approach.
- `docs/06-Modular-Architecture-Guidelines.md`
  - Align contract locations and module scaffolding guidance.
- `docs/07-Observability-Guidelines.md`
  - Document real health/metrics endpoints and exporter status.
- `docs/identifier/README.md`
  - Correct DB engine/connection strings, ports, and integration test description.
  - Add Dapr seed topic note and gRPC surface reference.
- Add `REPORT_DISCREPANZE.md` and `CHANGELOG_DOCS.md`.

## Phase 4 — Applied changes
All modifications listed above have been applied to the respective documentation files.

## Files to remove
- None. (No obsolete documentation files were removed in this pass.)

## Files added
- `REPORT_DISCREPANZE.md` (discrepancy report and update plan)
- `CHANGELOG_DOCS.md` (documentation changelog)

## Phase 5 — Final validation checks
- Commands cited cross-checked against `Makefile`, `docker-compose.yml`, and `src/*/Program.cs`.
- Environment variables aligned with configuration files (notably `ConnectionStrings__Identifier`).
- API routes verified against `src/services/*/Program.cs` and `contracts/*`.
- Internal links preserved.

## Open items / to verify
- Dapr component YAML files under `src/AppHost/mounts/dapr/components` are empty; Dapr pub/sub configuration must be filled before local Dapr flows will work.
- `docker-compose.yml` does not currently set `ConnectionStrings__Identifier`; identifier containers need an override to connect to Timescale.

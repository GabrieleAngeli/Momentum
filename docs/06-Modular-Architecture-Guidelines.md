# Modular Architecture Guidelines

## Contracts & boundaries
- Service contracts live under `/contracts/<service>` and frontend remotes are registered in [`contracts/web-core/module-manifest.json`](../contracts/web-core/module-manifest.json).
- Shared use cases flow through the web-backend-core orchestrator via Dapr gRPC.
- Module federation lets every micro-frontend expose its entry point as a remote.
- Backend plug-ins register through Dapr service discovery.
- The modular monolith hosts modules behind a unified façade; when extracting services, keep the same contracts to maintain backwards compatibility. Use existing examples such as [`modules/ticketing`](../modules/ticketing/README.md) as a baseline for additional plug-ins (currently a contract-only sample).

## Module lifecycle checklist
1. **Design:** Capture the bounded context, commands, queries, and events. Document assumptions in an ADR when required.
2. **Contracts:** Define gRPC/OpenAPI schemas under `/contracts/<service>` (or a module folder when introduced) and publish the manifest entry.
3. **Backend implementation:** Implement Clean Architecture layers under `modules/<module>/src/*` or the dedicated service project. Register Dapr components and health checks.
4. **Frontend surface:** Provide a federated Angular remote packaged under `modules/<module>/web` and register routes/components in the manifest.
5. **Testing:** Add unit/integration/contract tests. Ensure Playwright smoke tests cover the module entry points.
6. **Documentation:** Update module README, architecture overview, and release notes.

## Dapr registration
- Register invocation endpoints in the Aspire AppHost and add Dapr components under `src/AppHost/mounts/dapr/components/`.
- Configure pub/sub topics via component files in that directory and apply dead-letter queues where necessary.
- For background processors, define Cron bindings or input bindings with clear naming and TTL policies.

## Observability expectations
- Emit structured logs with the `module` property to simplify filtering.
- Export metrics via OpenTelemetry meters and register dashboards per module.
- Trace external calls (databases, HTTP, Dapr) using the shared instrumentation libraries referenced in [`docs/07-Observability-Guidelines.md`](07-Observability-Guidelines.md).

## Deployment considerations
- Modules packaged within the modular monolith should include feature toggles to gate unfinished capabilities.
- When deploying as independent services, document the infrastructure requirements (Kafka topics, Timescale schemas, Ignite caches) since no Helm/Bicep manifests are tracked here yet.
- Document rollout steps (migrations, seed data, toggle activation) inside the module README and release notes.

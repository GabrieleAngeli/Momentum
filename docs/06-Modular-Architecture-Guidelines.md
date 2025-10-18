# Modular Architecture Guidelines

## Contracts & boundaries
- Each module exposes independent contracts in `/contracts/<module>` and registers its frontend entry point inside [`contracts/web-core/module-manifest.json`](../contracts/web-core/module-manifest.json).
- Shared use cases flow through the web-backend-core orchestrator via Dapr gRPC.
- Module federation lets every micro-frontend expose its entry point as a remote.
- Backend plug-ins register through Dapr service discovery.
- The modular monolith hosts modules behind a unified façade; when extracting services, keep the same contracts to maintain backwards compatibility. Use existing examples such as [`modules/ticketing`](../modules/ticketing/README.md) as a baseline for additional plug-ins.

## Module lifecycle checklist
1. **Design:** Capture the bounded context, commands, queries, and events. Document assumptions in an ADR when required.
2. **Contracts:** Define gRPC/OpenAPI schemas under `/contracts/<module>` and publish the manifest entry.
3. **Backend implementation:** Implement Clean Architecture layers under `modules/<module>/src/*` or equivalent service project. Register Dapr components and health checks.
4. **Frontend surface:** Provide a federated Angular remote packaged under `modules/<module>/web`. Register routes/components in the manifest.
5. **Testing:** Add unit/integration/contract tests. Ensure Playwright smoke tests cover the module entry points.
6. **Documentation:** Update module README, architecture overview, and release notes.

## Dapr registration
- Register invocation endpoints in `Components/dapr.yaml` with consistent IDs (`module-<name>`).
- Configure pub/sub topics via component files stored alongside the module. Apply dead-letter queues where necessary.
- For background processors, define Cron bindings or input bindings with clear naming and TTL policies.

## Observability expectations
- Emit structured logs with the `module` property to simplify filtering.
- Export metrics via OpenTelemetry meters and register dashboards per module.
- Trace external calls (databases, HTTP, Dapr) using the shared instrumentation libraries referenced in [`docs/07-Observability-Guidelines.md`](07-Observability-Guidelines.md).

## Deployment considerations
- Modules packaged within the modular monolith should include feature toggles to gate unfinished capabilities.
- When deploying as independent services, ship Helm/Bicep manifests referencing the shared infrastructure (Kafka topics, Timescale schemas, Ignite caches).
- Document rollout steps (migrations, seed data, toggle activation) inside the module README and release notes.

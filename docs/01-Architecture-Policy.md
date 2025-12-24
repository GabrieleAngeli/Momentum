# Architecture Policy

## Principles
- Each bounded context follows DDD with Clean Architecture layering.
- External contracts (gRPC, OpenAPI, event schema) live in `/contracts` and are published as CI artefacts.
- Aspire and Dapr are the default for local orchestration and service invocation.
- Infrastructure dependencies (Kafka, Timescale, Ignite) are managed as code (`docker-compose.yml` and Aspire AppHost). No Bicep/Terraform manifests are tracked yet.
- The modular monolith provides the canonical integration surface; distributed services must preserve the same contracts and follow the [Modular Architecture Guidelines](06-Modular-Architecture-Guidelines.md).

## Layering & boundaries
- `Domain` projects model aggregates, value objects, and domain events. Business invariants must live here with no external dependencies besides shared primitives.
- `Application` projects expose CQRS handlers, orchestrators, and ports for infrastructure concerns. Only depend on domain abstractions and `Contracts` packages.
- `Infrastructure` projects implement ports (repositories, Dapr clients, messaging adapters) and encapsulate third-party SDKs.
- `Api` projects compose controllers/endpoints, request/response DTOs, and DI wiring. Cross-service communication must go through Dapr clients defined in `Infrastructure`.
- Shared kernel libraries are allowed only for cross-cutting concerns (OpenTelemetry, security primitives) and must remain dependency-free from feature code.

## Service topologies
- **Modular monolith:** Default topology for development. Modules reside under `modules/` and are hosted inside the `modular-monolith` service with Dapr components declared locally.
- **Distributed services:** When extracting a module, copy its contracts to a dedicated service project preserving the namespace and Dapr invocation IDs. Keep synchronous communication minimal and prefer event-driven interactions.
- **Edge integrations:** The `web-backend-core` service remains the ingress even when modules are extracted. Use its façade to expose HTTP/gRPC APIs to clients.

## Integration patterns
- **Messaging:** Publish domain events through Dapr/Kafka with idempotent handlers. Adopt the `bounded-context.event` naming convention.
- **Outbox:** Use transactional outbox tables processed by background services to guarantee reliable event delivery and eventual consistency across modules.
- **API Composition:** Backend-for-frontend (`web-backend-core`) composes responses from modules and applies caching policies with Ignite.
- **Observability:** Emit traces/spans using OpenTelemetry instrumentation defined in shared packages. Every external call must carry correlation IDs.

## Exception handling & resilience
- Wrap infrastructure interactions with policies (Polly or Dapr retry metadata) and expose domain-specific errors to callers.
- Prefer compensating actions published as events over distributed transactions.
- Health checks must validate downstream dependencies (Kafka, Timescale, Ignite) and align with Kubernetes/Aspire readiness probes.

## Documentation & decision records
- Any new architecture significant change requires an ADR stored under `docs/adr/` following the existing `0001.md`, `0002.md` numbering pattern.
- Update [architecture-overview](../architecture-overview.md) diagrams and README sections when the container or component topology changes.
- Record contract-breaking changes in `ReleaseNotes/` and link them from module READMEs.

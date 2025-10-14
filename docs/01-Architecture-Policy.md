# Architecture Policy

## Principles
- Each bounded context follows DDD with Clean Architecture layering.
- External contracts (gRPC, OpenAPI, event schema) live in `/contracts` and are published as CI artefacts.
- Aspire and Dapr are the default for local orchestration and service invocation.
- Infrastructure dependencies (Kafka, Timescale, Ignite) are managed as code (docker compose / Bicep TBD).
- The modular monolith provides the canonical integration surface; distributed services must preserve the same contracts and follow the [Modular Architecture Guidelines](06-Modular-Architecture-Guidelines.md).

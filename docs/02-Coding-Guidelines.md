# Coding Guidelines

## Core practices
- Target C# 12 on `net8.0` with nullable enabled.
- Layering: `Api` depends on `Application` → `Domain`; `Infrastructure` implements ports.
- Input validation relies on records and light CQRS handlers.
- Frontend uses Angular standalone components, typed SignalR wrappers, and RxJS streams.
- Every feature requires unit tests plus minimal integration coverage.
- When building modules for the modular monolith, align abstractions with the domain boundaries defined in the [Modular Architecture Guidelines](06-Modular-Architecture-Guidelines.md) and expose contracts through `/contracts`.

## Backend conventions
- Use minimal APIs or controllers with explicit DTOs; never expose domain entities directly.
- Prefer MediatR-style command/query handlers stored under `Application/UseCases/*` with clear naming (`CreateTicketHandler`).
- Repository interfaces live in `Domain` (or `Application.Abstractions`); implementations use EF Core, Dapr clients, or HTTP clients under `Infrastructure`.
- All outgoing HTTP/gRPC calls must include resiliency policies (retry, timeout) configured via `IHttpClientFactory` or Dapr metadata.
- Background jobs leverage `IHostedService` or Aspire jobs and must emit structured logs + metrics.

## Frontend conventions
- Adopt Angular standalone components with `Signals`/RxJS for state management; avoid NgModules unless interacting with legacy libraries.
- Shared UI primitives belong to `src/app/shared/` with Storybook examples (TODO) to guarantee reusability.
- Communicate with the backend through generated clients (OpenAPI/gRPC-web) stored in `src/app/core/api/`.
- Internationalisation relies on Angular i18n; new strings must be added to the locale extraction workflow.
- Module federation remotes expose `bootstrap.ts` entries; register them in `contracts/web-core/module-manifest.json`.

## Testing & quality gates
- Unit tests live alongside projects (`*.Tests`) and must be runnable via `make test`.
- Integration tests rely on Testcontainers to boot Kafka/Timescale/Ignite locally; seed data through fixtures under `tests/fixtures`.
- Frontend tests include Jasmine/Karma unit specs and Playwright e2e tests executed in CI.
- Static analysis: enable `dotnet format`, `eslint`, and `stylelint` before pushing. The pipeline enforces analyzers and warnings-as-errors.
- Coverage reports are uploaded as build artefacts; breaking the minimum 70% threshold blocks merges.

## Commit & issue hygiene
- Each commit subject must reference the primary issue (`#123`).
- Pull requests must keep the _Fixes_ section updated with covered issues. The _Ensure issue linking_ workflow updates the description when it detects unlinked commits and creates missing issues via LLM.
- Before merging, tidy commits and squash only when necessary to preserve traceability with generated issues.

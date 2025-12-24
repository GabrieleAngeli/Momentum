# Testing Policy

## Coverage pillars
- **Unit:** xUnit for .NET, Jasmine for Angular.
- **Integration:** Identifier API tests use Testcontainers (Timescale/Postgres). Other services currently rely on unit tests only.
- **Contract:** JSON schema validation uses `tests/Contracts/schema-validation.test.sh` (AJV in CI). `make contracts` only bundles the contracts directory.
- **E2E:** Playwright spec under `tests/WebCore/e2e.spec.ts` targets a running `web-core` instance.
- **Coverage goal:** minimum 70% for critical domains, including modules delivered through the modular monolith façade.

## Test environments
- **Local:** `make test` runs `dotnet test` and `npm test` (it does not start containers). Integration tests that need databases spin up Testcontainers.
- **CI (pull requests):** `pr-gate.yml` runs .NET build/test, frontend lint/tests, and JSON schema validation; coverage artefacts are uploaded.
- **Nightly:** No nightly workflow is defined in `.github/workflows/` at the moment.

## Quality gates
- Block merges when tests fail or coverage decreases beyond threshold.
- Contracts should be re-bundled (`make contracts`) and committed when proto/OpenAPI definitions change.
- New modules should add smoke tests proving integration with the modular monolith façade and web-core module manifest.

## Tooling matrix
| Layer | Backend | Frontend |
| --- | --- | --- |
| Unit | xUnit + FluentAssertions | Jasmine/Karma |
| Integration | Testcontainers (Timescale/Postgres) | - |
| Contract | AJV schema validation | - |
| E2E | Playwright (manual orchestration) | Playwright end-user journeys |

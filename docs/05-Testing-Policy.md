# Testing Policy

## Coverage pillars
- **Unit:** xUnit for .NET, Jasmine for Angular.
- **Integration:** gRPC/HTTP tests with Testcontainers (Kafka, Timescale, Ignite) plus Playwright e2e suites executed in CI.
- **Contract:** schema verification via `buf`/`spectral` (pipeline step TBD) triggered by `make contracts` and release workflows.
- **E2E:** Playwright orchestrated against the docker compose profile and Aspire AppHost.
- **Coverage goal:** minimum 70% for critical domains, including modules delivered through the modular monolith façade.

## Test environments
- **Local:** `make test` spins up required containers and runs .NET/Angular unit tests. Integration tests rely on Testcontainers to remain hermetic.
- **CI (pull requests):** Executes unit + integration tests, static analysis, contract validation, and publishes coverage reports.
- **Nightly:** Full E2E suite against the docker compose topology with seeded sample data (`tools/scripts/generate-sample-data.sh`).

## Quality gates
- Block merges when tests fail or coverage decreases beyond threshold.
- Contracts must be regenerated (`make contracts`) and committed when proto/OpenAPI definitions change.
- New modules require dedicated smoke tests proving integration with the modular monolith façade and web-core module manifest.

## Tooling matrix
| Layer | Backend | Frontend |
| --- | --- | --- |
| Unit | xUnit + FluentAssertions | Jasmine/Karma |
| Integration | Testcontainers + Aspire orchestrations | Playwright component tests |
| Contract | `buf`, `spectral`, JSON schema diff | API client generation smoke tests |
| E2E | Playwright headless via docker compose | Playwright end-user journeys |

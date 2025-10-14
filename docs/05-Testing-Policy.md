# Testing Policy

## Coverage pillars
- **Unit:** xUnit for .NET, Jasmine for Angular.
- **Integration:** gRPC/HTTP tests with Testcontainers (TODO) plus Playwright e2e.
- **Contract:** schema verification via `buf`/`spectral` (pipeline step TBD).
- **E2E:** Playwright orchestrated against the docker compose profile.
- **Coverage goal:** minimum 70% for critical domains, including modules delivered through the modular monolith façade.

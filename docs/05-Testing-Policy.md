# Testing Policy

- **Unit**: xUnit per .NET, Jasmine per Angular.
- **Integration**: test gRPC/HTTP con Testcontainers (TODO) e Playwright e2e.
- **Contract**: verfica schema via `buf`/`spectral` (pipeline step TBD).
- **E2E**: Playwright orchestrato contro docker compose profile.
- Copertura minima 70% per domini critici.

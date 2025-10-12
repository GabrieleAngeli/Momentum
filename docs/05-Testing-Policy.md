# Testing Policy / Politica di Test

## Coverage pillars / Pilastri di copertura
**English:**
- **Unit:** xUnit for .NET, Jasmine for Angular.
- **Integration:** gRPC/HTTP tests with Testcontainers (TODO) plus Playwright e2e.
- **Contract:** schema verification via `buf`/`spectral` (pipeline step TBD).
- **E2E:** Playwright orchestrated against the docker compose profile.
- **Coverage goal:** minimum 70% for critical domains.

**Italiano:**
- **Unit:** xUnit per .NET, Jasmine per Angular.
- **Integration:** test gRPC/HTTP con Testcontainers (TODO) e Playwright e2e.
- **Contract:** verifica schema tramite `buf`/`spectral` (step di pipeline da definire).
- **E2E:** Playwright orchestrato contro il profilo docker compose.
- **Obiettivo copertura:** minimo 70% per i domini critici.

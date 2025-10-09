# Security Policy

- JWT signed con chiave rotabile, memorizzata in Key Vault (placeholder).
- OpenFeature usato per feature flag/licenze (provider Dapr binding).
- Minimo privilegio per accesso Timescale/Ignite.
- gRPC + HTTPS obbligatori in ambienti produttivi.
- Secret in repo vietati; usare variabili CI/CD.

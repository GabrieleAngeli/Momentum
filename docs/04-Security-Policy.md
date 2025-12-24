# Security Policy

## Controls
- JWT tokens are signed with a configurable key (`Auth:Jwt:SigningKey` in `core-web`), with a development default if no secret is provided.
- OpenFeature is wired in `web-backend-core` with an in-memory provider; no external flag provider or Dapr binding is configured yet.
- TimescaleDB credentials are injected via connection strings (environment overrides are supported).
- gRPC endpoints (Identifier and Streamer) run alongside HTTP; HTTPS is a deployment concern and is not enforced by default in the repo.
- Secrets must never live in the repository; use environment variables or secret stores in CI/CD.
- Modular monolith deployments must keep the same auth/flag semantics as the distributed topology to preserve contract parity.

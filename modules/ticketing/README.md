# Ticketing Module

Example plug-in module exposing an OpenAPI contract via Dapr invocation. It can run inside the modular monolith or independently.

## Structure
- `src/` – Backend Clean Architecture layers (`Domain`, `Application`, `Infrastructure`, `Api`).
- `contracts/` – OpenAPI definitions and events published to the platform catalogue.
- `web/` – Federated Angular remote providing UI components registered in `contracts/web-core/module-manifest.json`.
- `tests/` – Unit/integration/contract test suites executed via `make test`.

## Development checklist
1. Update domain models/commands in `src/Domain` and `src/Application`.
2. Expose Dapr-invokable endpoints via `src/Api` and register pub/sub topics in `Components/`.
3. Regenerate contracts (`make contracts`) and ensure compatibility tests pass.
4. Implement frontend widgets in `web/` and wire them to shared design tokens.
5. Add smoke tests (Playwright) validating the module entry point.
6. Document changes in this README and `ReleaseNotes/` when publishing the module.

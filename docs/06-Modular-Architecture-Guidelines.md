# Modular Architecture Guidelines

## Contracts & boundaries
- Each module exposes independent contracts in `/contracts/<module>` and registers its frontend entry point inside [`contracts/web-core/module-manifest.json`](../contracts/web-core/module-manifest.json).
- Shared use cases flow through the web-backend-core orchestrator via Dapr gRPC.
- Module federation lets every micro-frontend expose its entry point as a remote.
- Backend plug-ins register through Dapr service discovery.
- The modular monolith hosts modules behind a unified façade; when extracting services, keep the same contracts to maintain backwards compatibility. Use existing examples such as [`modules/ticketing`](../modules/ticketing/README.md) as a baseline for additional plug-ins.

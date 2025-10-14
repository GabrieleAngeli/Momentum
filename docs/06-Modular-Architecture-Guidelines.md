# Modular Architecture Guidelines

## Contracts & boundaries
- Each module exposes independent contracts in `/contracts/<module>`.
- Shared use cases flow through the web-backend-core orchestrator via Dapr gRPC.
- Module federation lets every micro-frontend expose its entry point as a remote.
- Backend plug-ins register through Dapr service discovery.
- The modular monolith hosts modules behind a unified façade; when extracting services, keep the same contracts to maintain backwards compatibility.

# Modular Architecture Guidelines

- Ogni modulo espone contratti indipendenti (`/contracts/<module>`).
- Use-case condivisi passano tramite orchestratore web-backend-core via Dapr gRPC.
- Module federation: ogni micro-frontend espone entry come remote.
- Plug-in backend registrati via Dapr service discovery.

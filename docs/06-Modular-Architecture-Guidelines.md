# Modular Architecture Guidelines / Linee Guida per l'Architettura Modulare

## Contracts & boundaries / Contratti e confini
**English:**
- Each module exposes independent contracts in `/contracts/<module>`.
- Shared use cases flow through the web-backend-core orchestrator via Dapr gRPC.
- Module federation lets every micro-frontend expose its entry point as a remote.
- Backend plug-ins register through Dapr service discovery.

**Italiano:**
- Ogni modulo espone contratti indipendenti in `/contracts/<module>`.
- I casi d'uso condivisi passano dall'orchestratore web-backend-core tramite gRPC Dapr.
- La module federation consente a ogni micro-frontend di esporre il proprio entry point come remote.
- I plug-in backend si registrano tramite il service discovery di Dapr.

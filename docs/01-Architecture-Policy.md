# Architecture Policy

- Ogni bounded context rispetta DDD + Clean Architecture.
- I contratti esterni (gRPC, OpenAPI, event schema) sono versionati in `/contracts` e pubblicati come artefatti CI.
- Aspire e Dapr sono il default per orchestrazione locale e service invocation.
- Le dipendenze infra (Kafka, Timescale, Ignite) sono gestite tramite IaC (docker compose / Bicep TBD).

# Architecture Policy / Politica Architetturale

## Principles / Principi
**English:**
- Each bounded context follows DDD with Clean Architecture layering.
- External contracts (gRPC, OpenAPI, event schema) live in `/contracts` and are published as CI artefacts.
- Aspire and Dapr are the default for local orchestration and service invocation.
- Infrastructure dependencies (Kafka, Timescale, Ignite) are managed as code (docker compose / Bicep TBD).

**Italiano:**
- Ogni bounded context segue DDD con layering Clean Architecture.
- I contratti esterni (gRPC, OpenAPI, event schema) risiedono in `/contracts` e vengono pubblicati come artefatti CI.
- Aspire e Dapr sono il default per l'orchestrazione locale e le chiamate fra servizi.
- Le dipendenze infrastrutturali (Kafka, Timescale, Ignite) sono gestite come codice (docker compose / Bicep da definire).

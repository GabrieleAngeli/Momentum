# Data Guidelines / Linee Guida sui Dati

## Storage strategy / Strategia di storage
**English:**
- Timescale schema `telemetry_events` managed through migrations (TODO EF Core migrations).
- Ignite powers realtime cache and in-memory analytics.
- Timescale retention set to 365 days with compression enabled.
- Sensitive data encrypted at rest and in transit.
- Only OSS database/cache components (TimescaleDB OSS, Apache Ignite OSS) to honour licensing requirements.

**Italiano:**
- Lo schema Timescale `telemetry_events` è gestito tramite migrazioni (TODO EF Core migrations).
- Ignite alimenta la cache realtime e le analisi in-memory.
- La retention di Timescale è impostata a 365 giorni con compressione attiva.
- I dati sensibili sono cifrati at-rest e in-transit.
- Sono utilizzati solo componenti database/cache OSS (TimescaleDB OSS, Apache Ignite OSS) per rispettare i requisiti di licenza.

# Data Guidelines

- Timescale schema `telemetry_events` gestito via migrazioni (TODO EF Core migrations).
- Ignite usato per cache realtime e analytics in-memory.
- Retention Timescale 365 giorni con compressione.
- Dati sensibili cifrati at-rest e in-transit.
- Solo componenti database/cache open source (TimescaleDB OSS, Apache Ignite OSS) per conformità licenze.

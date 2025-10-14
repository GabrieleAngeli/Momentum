# Data Guidelines

## Storage strategy
- Timescale schema `telemetry_events` managed through migrations (TODO EF Core migrations).
- Ignite powers realtime cache and in-memory analytics.
- Timescale retention set to 365 days with compression enabled.
- Sensitive data encrypted at rest and in transit.
- Only OSS database/cache components (TimescaleDB OSS, Apache Ignite OSS) to honour licensing requirements.
- Modular monolith modules must treat the shared data stores as authoritative sources and avoid duplicating persistence when extracted into standalone services.

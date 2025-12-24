# Documentation Changelog

## 2025-12-24

### Updated
- `README.md`
  - Clarified Aspire vs Docker Compose runtime topologies, Dapr availability, and service coverage.
  - Corrected telemetry flow description to match streamer/notifier behavior.
  - Updated stack notes for OpenTelemetry and caching status.

- `architecture-overview.md`
  - Aligned container/component diagrams and event flow with the actual services and code.
  - Corrected Dapr component references and data storage notes.

- `docs/01-Architecture-Policy.md`
  - Updated ADR naming guidance and clarified IaC status.

- `docs/04-Security-Policy.md`
  - Replaced Key Vault/OpenFeature binding statements with the current JWT/OpenFeature configuration.

- `docs/05-Testing-Policy.md`
  - Documented real test tooling, CI checks, and contract validation flow.

- `docs/06-Modular-Architecture-Guidelines.md`
  - Updated contract locations and module scaffolding guidance based on the repository structure.

- `docs/07-Observability-Guidelines.md`
  - Documented actual health/metrics endpoints and exporter status.

- `docs/identifier/README.md`
  - Corrected database engine, connection strings, ports, integration test setup, and Dapr seed topic.

### Added
- `REPORT_DISCREPANZE.md`
  - Added discrepancy report, reality map, and documentation update plan.

- `CHANGELOG_DOCS.md`
  - Added documentation changelog.

- `REPORT_DISCREPANZE.md`
  - Added explicit “Files to remove” and “Files added” sections for traceability.

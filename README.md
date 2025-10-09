# Momentum Platform

Momentum è una piattaforma modulare e resiliente progettata per essere eseguita come monolite modulare oppure evolvere in una topologia a microservizi senza cambiare i contratti pubblici.

## Stack

- **Backend**: .NET 8 Aspire + Dapr gRPC e pub/sub Kafka
- **Frontend**: Angular 19 standalone, i18n, module federation
- **Dati**: TimescaleDB per storico, Apache Ignite per cache
- **Messaging**: Kafka per eventi telemetrici
- **Realtime**: SignalR e canali notifiche
- **Observability**: OpenTelemetry → Prometheus, Loki, Tempo, Grafana
- **Sicurezza**: JWT, OpenFeature-ready

## Quick start

```bash
make build
make dev
```

Oppure con Aspire:

```bash
dotnet run --project src/AppHost/Momentum.AppHost.csproj
```

## Servizi

| Servizio | Descrizione |
| --- | --- |
| identifier | Autenticazione e gestione licenze |
| streamer | Ingest telemetria da Kafka → Timescale/ Ignite |
| notifier | Dispatch notifiche multi-canale |
| web-backend-core | Gateway API + SignalR hub |
| web-core | Frontend modulare Angular |

## Flusso end-to-end

1. Evento grezzo su `telemetry.input` (Kafka)
2. Streamer lo trasforma e persiste su Timescale, invalida cache Ignite e pubblica `telemetry.ingested`
3. Notifier sottoscrive l'evento, invia mail/SignalR verso web-backend-core
4. Web-backend-core broadcast su hub SignalR
5. Web-core riceve notifica realtime e la mostra nella dashboard

## Scripts utili

- `tools/scripts/generate-sample-data.sh` genera eventi demo
- `make test` esegue test .NET e Angular (best-effort in CI
)
- `make contracts` produce archivio dei contratti

## Deploy

- `docker compose up` per ambiente completo (infra + servizi)
- `src/AppHost` per orchestrazione locale con .NET Aspire

## Documentazione

- [Architecture Overview](architecture-overview.md)
- Cartella `docs/` per policy e ADR
- Cartella `contracts/` per proto/OpenAPI/event schema

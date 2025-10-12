# Coding Guidelines / Linee Guida di Sviluppo

## Core practices / Pratiche di base
**English:**
- Target C# 12 on `net8.0` with nullable enabled.
- Layering: `Api` depends on `Application` → `Domain`; `Infrastructure` implements ports.
- Input validation relies on records and light CQRS handlers.
- Frontend uses Angular standalone components, typed SignalR wrappers, and RxJS streams.
- Every feature requires unit tests plus minimal integration coverage.

**Italiano:**
- Target C# 12 su `net8.0` con nullable abilitato.
- Layering: `Api` dipende da `Application` → `Domain`; `Infrastructure` implementa le porte.
- La validazione input utilizza record e handler CQRS leggeri.
- Il frontend adotta componenti standalone Angular, wrapper SignalR tipizzati e stream RxJS.
- Ogni feature richiede test unitari più una copertura di integrazione minima.

## Commit & issue hygiene / Buone pratiche per commit e issue
**English:**
- Each commit subject must reference the primary issue (`#123`).
- Pull requests must keep the _Fixes_ section updated with covered issues. The _Ensure issue linking_ workflow updates the description when it detects unlinked commits and creates missing issues via LLM.
- Before merging, tidy commits and squash only when necessary to preserve traceability with generated issues.

**Italiano:**
- Ogni commit deve riportare nel subject il riferimento alla issue principale (`#123`).
- Le pull request devono mantenere aggiornata la sezione _Fixes_ con le issue coperte. Il workflow _Ensure issue linking_ aggiorna la descrizione quando rileva commit non collegati e crea le issue mancanti tramite LLM.
- Prima del merge ripulisci i commit e fai squash solo se necessario, preservando la tracciabilità con le issue generate.

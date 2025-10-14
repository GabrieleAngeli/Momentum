# Coding Guidelines

## Core practices
- Target C# 12 on `net8.0` with nullable enabled.
- Layering: `Api` depends on `Application` → `Domain`; `Infrastructure` implements ports.
- Input validation relies on records and light CQRS handlers.
- Frontend uses Angular standalone components, typed SignalR wrappers, and RxJS streams.
- Every feature requires unit tests plus minimal integration coverage.
- When building modules for the modular monolith, align abstractions with the domain boundaries defined in the [Modular Architecture Guidelines](06-Modular-Architecture-Guidelines.md) and expose contracts through `/contracts`.

## Commit & issue hygiene
- Each commit subject must reference the primary issue (`#123`).
- Pull requests must keep the _Fixes_ section updated with covered issues. The _Ensure issue linking_ workflow updates the description when it detects unlinked commits and creates missing issues via LLM.
- Before merging, tidy commits and squash only when necessary to preserve traceability with generated issues.

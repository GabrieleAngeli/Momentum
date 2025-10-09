# Coding Guidelines

- C# 12, `net8.0`, nullable abilitato.
- Layering: `Api` dipende da `Application` → `Domain`, `Infrastructure` implementa porte.
- Validazione input tramite record e handler CQRS light.
- Frontend Angular standalone components, SignalR typed wrappers, RxJS per stream.
- Ogni feature richiede test unitari + integrazione minimi.

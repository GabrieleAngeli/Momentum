# Coding Guidelines

- C# 12, `net8.0`, nullable abilitato.
- Layering: `Api` dipende da `Application` → `Domain`, `Infrastructure` implementa porte.
- Validazione input tramite record e handler CQRS light.
- Frontend Angular standalone components, SignalR typed wrappers, RxJS per stream.
- Ogni feature richiede test unitari + integrazione minimi.

## Commit & Issue hygiene

- Ogni commit deve riportare nel subject il riferimento alla issue principale (`#123`) che lo ha
  originato.
- Le pull request devono mantenere la sezione _Fixes_ aggiornata con tutte le issue coperte. Il
  workflow _Ensure issue linking_ aggiorna automaticamente la descrizione quando rileva commit non
  collegati e crea eventuali issue mancanti tramite LLM.
- Prima del merge assicurarsi che i commit siano ripuliti e squashed solo se strettamente
  necessario, mantenendo la tracciabilità con le issue generate.

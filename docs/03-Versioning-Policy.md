# Versioning Policy

- Semantic Versioning per servizi e front-end.
- Contratti gRPC/OpenAPI versionati via namespace/route `vX`.
- Event schema versionati tramite `$id` incrementale.
- Tag Docker `major.minor.patch` + `latest` per ambienti demo.

## Flusso di merge verso `main`

1. Ogni attività viene sviluppata su un branch feature derivato da `main` e collegato ad una
   issue di GitHub.
2. I branch feature vengono integrati tramite pull request. Il merge verso `main` avviene
   esclusivamente con merge commit generati dall'interfaccia GitHub dopo che la pull request è
   stata approvata e che tutti i controlli automatizzati sono passati.
3. Prima del merge viene eseguito il workflow _Ensure issue linking_ che verifica che tutti i
   commit referenzino almeno una issue (`#<numero>`). In assenza del riferimento, lo script
   genera automaticamente una issue con il riassunto delle modifiche e aggiorna la descrizione
   della pull request.
4. Una volta completato il merge, `main` rappresenta sempre lo stato pronto alla release. Il
   numero di versione viene incrementato e taggato contestualmente alla generazione delle release
   notes.

## Gestione automatica delle issue e dei commit

- Tutti i commit devono includere nel messaggio il riferimento a una issue (`#123`).
- Lo script `tools/issue_management/ensure_issue_links.py` è invocato nella pipeline di pull
  request per controllare la presenza del riferimento. Se non è presente, e sono configurate le
  chiavi `GITHUB_TOKEN` e `OPENAI_API_KEY`, lo script crea automaticamente una nuova issue
  sintetizzando il contenuto del commit tramite LLM e aggiorna il corpo della pull request con il
  riferimento `Fixes #<numero>`.
- L'output dello script indica sempre le azioni svolte e rende il controllo bloccante finché il
  commit non è correttamente collegato.

## Release notes

- Il workflow manuale _Generate release notes_ crea i file di release note a partire dai commit
  tra un riferimento di base (`base_ref`) e la versione da rilasciare.
- I file sono posizionati nella cartella `ReleaseNotes/` del repository e nella cartella
  `modules/<nome-modulo>/ReleaseNotes/` per ciascun modulo toccato dai commit.
- Ogni file include l'elenco delle issue collegate e dei commit inclusi nella release, così da
  fornire visibilità completa sulle modifiche.
- Durante l'esecuzione del workflow viene creato automaticamente un commit su `main` con i file di
  release note generati e il messaggio `chore: add release notes for <version>`.

# Versioning Policy / Politica di Versionamento

## SemVer rules / Regole SemVer
**English:**
- Services and frontend follow Semantic Versioning.
- gRPC/OpenAPI contracts versioned through `vX` namespaces/routes.
- Event schemas carry incremental `$id` identifiers.
- Docker tags use `major.minor.patch` plus `latest` for demo environments.

**Italiano:**
- Servizi e frontend adottano il Semantic Versioning.
- I contratti gRPC/OpenAPI sono versionati tramite namespace/route `vX`.
- Gli event schema includono identificativi `$id` incrementali.
- I tag Docker usano `major.minor.patch` e `latest` per gli ambienti demo.

## Merge flow to `main` / Flusso di merge verso `main`
**English:**
1. Each task lives on a feature branch derived from `main` and linked to a GitHub issue.
2. Feature branches merge through pull requests; merging into `main` happens via GitHub merge commits once approvals and checks pass.
3. Before merging, the _Ensure issue linking_ workflow verifies that every commit references an issue (`#<number>`). If missing, it creates an issue summarising the changes and updates the PR description.
4. After merge, `main` always reflects a release-ready state. Version numbers are bumped and tagged alongside release note generation.

**Italiano:**
1. Ogni attività vive su un branch feature derivato da `main` e collegato a una issue GitHub.
2. I branch feature vengono integrati tramite pull request; il merge su `main` avviene con merge commit di GitHub dopo approvazioni e controlli positivi.
3. Prima del merge il workflow _Ensure issue linking_ verifica che ogni commit referenzi una issue (`#<numero>`). Se manca, crea una issue con il riassunto delle modifiche e aggiorna la descrizione della PR.
4. Dopo il merge `main` rappresenta sempre uno stato pronto alla release. Il numero di versione viene incrementato e taggato insieme alla generazione delle release note.

## Automated issue & commit management / Gestione automatica di issue e commit
**English:**
- Every commit message must include an issue reference (`#123`).
- The script `tools/issue_management/ensure_issue_links.py` runs in PR pipelines to enforce references. With `GITHUB_TOKEN` and `OPENAI_API_KEY` configured, it can create missing issues via LLM and update the PR body with `Fixes #<number>`.
- The script output reports the performed actions and blocks the check until every commit is linked.

**Italiano:**
- Ogni commit deve includere nel messaggio il riferimento a una issue (`#123`).
- Lo script `tools/issue_management/ensure_issue_links.py` gira nelle pipeline delle PR per garantire la presenza dei riferimenti. Con `GITHUB_TOKEN` e `OPENAI_API_KEY` configurati crea le issue mancanti via LLM e aggiorna il corpo della PR con `Fixes #<numero>`.
- L'output dello script riporta le azioni svolte e rende il controllo bloccante finché tutti i commit non sono collegati.

## Release notes
**English:**
- The manual _Generate release notes_ workflow builds release files from commits between a base ref and the target version.
- Files land in `ReleaseNotes/` and `modules/<module-name>/ReleaseNotes/` for each affected module.
- Each file lists linked issues and included commits to provide full visibility.
- During the workflow execution, a commit on `main` is created with the generated notes using the message `chore: add release notes for <version>`.

**Italiano:**
- Il workflow manuale _Generate release notes_ crea i file a partire dai commit tra un riferimento di base e la versione da rilasciare.
- I file vengono salvati in `ReleaseNotes/` e in `modules/<nome-modulo>/ReleaseNotes/` per ogni modulo interessato.
- Ogni file elenca le issue collegate e i commit inclusi per offrire piena visibilità.
- Durante l'esecuzione del workflow viene creato un commit su `main` con le note generate e il messaggio `chore: add release notes for <version>`.

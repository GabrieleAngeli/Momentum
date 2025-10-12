# Issue management automation / Automazione della gestione issue

## Purpose / Scopo
**English:** `ensure_issue_links.py` runs as part of the pull request validation pipeline to ensure every commit links to at least one issue. With `--auto-create` and the `GITHUB_TOKEN`/`OPENAI_API_KEY` variables available, the tool creates a new issue summarising the commit and updates the PR body with `Fixes #<number>`.

**Italiano:** `ensure_issue_links.py` viene eseguito nella pipeline di verifica delle pull request per garantire che ogni commit sia collegato ad almeno una issue. Con `--auto-create` e le variabili `GITHUB_TOKEN`/`OPENAI_API_KEY` disponibili, lo strumento crea una nuova issue con il riassunto del commit e aggiorna il corpo della PR con `Fixes #<numero>`.

## Manual execution / Esecuzione manuale
```bash
python tools/issue_management/ensure_issue_links.py \
  --repo <owner>/<repository> \
  --pr-number 123 \
  --auto-create
```

**English:** The command returns an error status when commits lack issue references. In auto mode it opens a ticket with the summary.

**Italiano:** Il comando restituisce uno stato di errore se sono presenti commit senza riferimento a una issue. In modalità automatica apre un ticket con il riassunto.

**English:** Install Python dependencies with:

**Italiano:** Installa le dipendenze Python con:

```bash
pip install -r tools/issue_management/requirements.txt
```

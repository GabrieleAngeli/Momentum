# Issue management automation

Lo script `ensure_issue_links.py` viene eseguito come parte della pipeline di verifica delle
pull request per controllare che ogni commit sia collegato ad almeno una issue. Quando il
parametro `--auto-create` è abilitato e sono disponibili le variabili `GITHUB_TOKEN` e
`OPENAI_API_KEY`, il tool genera automaticamente una nuova issue riassumendo il commit e
aggiorna il corpo della pull request aggiungendo il riferimento `Fixes #<numero>`.

## Esecuzione manuale

```bash
python tools/issue_management/ensure_issue_links.py \
  --repo <owner>/<repository> \
  --pr-number 123 \
  --auto-create
```

Il comando restituisce uno stato di errore se sono presenti commit senza riferimento ad una
issue. In modalità automatica viene creato un ticket con il riassunto delle modifiche.

Dipendenze Python:

```bash
pip install -r tools/issue_management/requirements.txt
```


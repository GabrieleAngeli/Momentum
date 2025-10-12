# Release notes automation / Automazione delle release notes

## Purpose / Scopo
**English:** This module contains `generate_release_notes.py`, used locally and in GitHub automation to produce release note files for the project and its modules.

**Italiano:** Questo modulo contiene `generate_release_notes.py`, usato localmente e nell'automazione GitHub per produrre i file di release note per il progetto e i moduli.

## Local usage / Utilizzo locale
```bash
python tools/release_notes/generate_release_notes.py \
  --release-version 1.0.0 \
  --base-ref origin/main \
  --head-ref HEAD \
  --repo <owner>/<repository> \
  --github-token $GITHUB_TOKEN
```

**English:** Files are generated under the repository-level `ReleaseNotes` folder and inside each module at `modules/<module-name>/ReleaseNotes/`.

**Italiano:** I file vengono creati nella cartella `ReleaseNotes` del repository e nella cartella `modules/<nome-modulo>/ReleaseNotes/` di ciascun modulo.

**English:** Install Python dependencies with:

**Italiano:** Installa le dipendenze Python con:

```bash
pip install -r tools/release_notes/requirements.txt
```

## Automated release creation / Creazione automatica della release
**English:** `tools/release_notes/create_release.py` automates the whole flow:
1. Determines the next version using semantic rules by analysing commits since the latest tag.
2. Generates release notes enriched with GitHub issue titles.
3. Creates a `chore: release <version>` commit containing the generated files.
4. Tags `v<version>` on `main` and creates the `release/v<version>` branch.

**Italiano:** `tools/release_notes/create_release.py` automatizza l'intero flusso:
1. Determina il prossimo numero di versione secondo le regole semantiche analizzando i commit dall'ultimo tag.
2. Genera le release notes arricchite con i titoli delle issue GitHub.
3. Crea un commit `chore: release <version>` con i file prodotti.
4. Applica il tag `v<version>` su `main` e crea il branch `release/v<version>`.

**English:** Example run:

**Italiano:** Esempio di esecuzione:

```bash
python -m tools.release_notes.create_release \
  --repo <owner>/<repository> \
  --github-token $GITHUB_TOKEN
```

**English:** Ensure the working tree is clean and execute the command from the `main` branch.

**Italiano:** Assicurati che il working tree sia pulito ed esegui il comando dal branch `main`.

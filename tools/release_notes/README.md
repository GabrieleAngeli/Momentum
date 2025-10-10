# Release notes automation

Questo modulo contiene lo script `generate_release_notes.py` utilizzato sia localmente sia
nell'automazione GitHub per produrre file di release note per progetto e moduli.

## Utilizzo locale

```bash
python tools/release_notes/generate_release_notes.py \
  --release-version 1.0.0 \
  --base-ref origin/main \
  --head-ref HEAD \
  --repo <owner>/<repository> \
  --github-token $GITHUB_TOKEN
```

I file vengono creati nella cartella `ReleaseNotes` a livello di repository e nella
cartella `ReleaseNotes` di ciascun modulo sotto `modules/<nome-modulo>/`.

Installare le dipendenze Python con:

```bash
pip install -r tools/release_notes/requirements.txt
```

## Creazione automatica della release

Lo script `tools/release_notes/create_release.py` automatizza l'intero flusso:

1. Determina il prossimo numero di versione seguendo la policy semantica analizzando i commit
   dall'ultimo tag.
2. Genera i file di release notes arricchiti con i titoli delle issue di GitHub.
3. Crea un commit `chore: release <version>` con i file prodotti.
4. Applica il tag `v<version>` su `main` e crea il branch `release/v<version>`.

Esempio di esecuzione:

```bash
python -m tools.release_notes.create_release \
  --repo <owner>/<repository> \
  --github-token $GITHUB_TOKEN
```

È necessario che il working tree sia pulito e che il comando venga lanciato dal branch `main`.
In ambiente CI (ad esempio GitHub Actions) il checkout avviene in detached HEAD: lo script
verifica automaticamente che la commit corrente coincida con `main` e prosegue senza errori,
creando il tag `v<version>` e il branch `release/v<version>` dopo aver committato le note.


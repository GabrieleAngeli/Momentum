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


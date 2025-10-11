# Flusso di rilascio automatizzato

Questo documento descrive il comportamento del workflow GitHub Actions
`Release automation` e come preparare correttamente una release di
Momentum.

## Convenzioni sui commit

Il repository utilizza le [Conventional Commits](https://www.conventionalcommits.org/).
Le tipologie rilevanti per il calcolo della versione sono:

- `feat:` → incremento **minor**
- `fix:`, `perf:`, `refactor:` → incremento **patch**
- commit con `BREAKING CHANGE` o `!` → incremento **major**

Il workflow esegue `commitlint` su ogni pull request verso `main`.
Assicurati che ogni commit rispetti il formato per evitare errori di
validazione.

## Calcolo della versione

Il comando `tools/release_notes/ci_release.py plan` analizza i commit
presenti tra l'ultimo tag `v*` e la testa della pull request. In base
alle tipologie individuate calcola la versione `nextVersion` e crea un
branch di servizio `release/v<nextVersion>` tramite PR automatica.

Il bump può essere modificato in due modi:

1. Applicando una label sulla PR: `release:major`, `release:minor` o
   `release:patch`.
2. Creando un file `.release-override.yml` nella root della PR con i
   seguenti campi opzionali:

   ```yaml
   bump: minor   # major | minor | patch
   version: 1.4.0
   allowDocsOnlyRelease: true
   excludeCategories:
     - chore
   ```

Se la PR contiene solo commit di documentazione o `chore`, la release
viene annullata automaticamente a meno che non sia impostato
`allowDocsOnlyRelease: true`.

## Creazione automatica delle issue

Per garantire la tracciabilità, il workflow esegue
`tools/issue_management/ensure_issue_links.py` su ogni PR interna.
Quando un commit non contiene riferimenti a issue, viene aperta (o
riutilizzata, se ne esiste una equivalente negli ultimi 90 giorni) una
nuova issue con label coerenti (`feature`, `bug` o `tech-debt`) e il
corpo della PR viene aggiornato con una voce `Closes #<id>` che mantiene
il collegamento fino al merge.

Questa funzionalità è disabilitata automaticamente per le PR provenienti
da fork, dove il workflow opera solo in modalità dry-run.

## Release notes

Durante la fase di pianificazione viene generato il file
`ReleaseNotes/<version>.md` utilizzando il template
`.github/release_notes.hbs`. Le categorie riportate sono: Breaking
Changes, Features, Fixes, Performance, Refactor, Docs e Chore. Ogni
voce include il titolo della issue (con link) oppure un titolo sintetico
derivato dal commit.

Un commento sulla PR mostra la preview delle release notes insieme alla
checklist pre-merge:

- Test
- Lint
- Build
- Security scan

## Gestione della PR di release

Se la PR non proviene da un fork, il workflow aggiorna o crea una PR
`release/v<version>` → `main` contenente il file di release notes. Questa
PR deve rispettare le stesse regole di merge del branch protetto.

## Workflow sui branch

- **Pull request verso main**: calcolo versione, generazione release
  notes, creazione/aggiornamento del branch di release, creazione issue
  mancanti, etichettatura della PR e pubblicazione della preview.
- **Push su release/v\***: validazione della sincronizzazione delle
  release notes rispetto al piano calcolato.
- **Push su main**: creazione del tag `v<version>` e della GitHub
  Release (bozza o definitiva a seconda del campo `githubRelease.draft`
in `.releaserc`).

## Ambienti e fork

Le PR provenienti da fork vengono eseguite in modalità dry-run: non
vengono create issue, label o commenti, né viene aperta la PR di release.
Questo evita scritture non autorizzate su repository esterni.

## Override manuali

Oltre a `.release-override.yml`, è possibile escludere categorie dalle
release notes modificando `.releaserc`. Le impostazioni correnti
escludono la categoria `chore` dalla pubblicazione.

## Risoluzione problemi

- Verifica i log del job `plan-release` per il dettaglio del piano.
- Controlla il file `.github/release-preview.md` generato localmente per
  la preview completa delle note.
- In caso di conflitti sul branch `release/v*`, risolvere manualmente e
  rilanciare il workflow (il comando `sync` garantisce la coerenza degli
  artefatti).

Per ulteriori personalizzazioni consulta
`tools/release_notes/ci_release.py` e il workflow
`.github/workflows/release.yml`.

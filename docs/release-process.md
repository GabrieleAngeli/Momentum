# Automated Release Flow / Flusso di Rilascio Automatizzato

## Overview / Panoramica
**English:** This document describes how the `Release automation` GitHub Actions workflow prepares a Momentum release.

**Italiano:** Questo documento descrive come il workflow GitHub Actions `Release automation` prepara una release di Momentum.

## Commit conventions / Convenzioni sui commit
**English:**
- The repository follows [Conventional Commits](https://www.conventionalcommits.org/).
- Relevant types for version calculation: `feat:` → **minor**, `fix:`/`perf:`/`refactor:` → **patch**, commits with `BREAKING CHANGE` or `!` → **major**.
- The workflow runs `commitlint` on every pull request targeting `main`; keep commits compliant to avoid validation failures.

**Italiano:**
- Il repository utilizza le [Conventional Commits](https://www.conventionalcommits.org/).
- Tipologie rilevanti per il calcolo versione: `feat:` → **minor**, `fix:`/`perf:`/`refactor:` → **patch**, commit con `BREAKING CHANGE` o `!` → **major**.
- Il workflow esegue `commitlint` su ogni pull request verso `main`; mantieni i commit conformi per evitare errori di validazione.

## Version calculation / Calcolo della versione
**English:**
- `tools/release_notes/ci_release.py plan` inspects commits between the latest `v*` tag and the PR head, computes `nextVersion`, and opens/updates a `release/v<nextVersion>` PR.
- Adjust the bump by: (1) applying a PR label (`release:major|minor|patch`); or (2) adding `.release-override.yml` with optional fields:
  ```yaml
  bump: minor   # major | minor | patch
  version: 1.4.0
  allowDocsOnlyRelease: true
  excludeCategories:
    - chore
  ```
- If a PR only contains documentation or `chore` commits the release is skipped unless `allowDocsOnlyRelease: true` is set.

**Italiano:**
- `tools/release_notes/ci_release.py plan` analizza i commit tra l'ultimo tag `v*` e la testa della PR, calcola `nextVersion` e apre/aggiorna una PR `release/v<nextVersion>`.
- Puoi modificare il bump: (1) applicando una label alla PR (`release:major|minor|patch`); oppure (2) aggiungendo `.release-override.yml` con i campi opzionali riportati sopra.
- Se la PR contiene solo commit di documentazione o `chore`, la release viene saltata a meno che `allowDocsOnlyRelease: true` non sia impostato.

## Automatic issue creation / Creazione automatica delle issue
**English:**
- To preserve traceability the workflow runs `tools/issue_management/ensure_issue_links.py` on every internal PR.
- When a commit lacks issue references, the script opens a new issue (`feature`, `bug`, or `tech-debt`) and updates the PR body with `Fixes #<id>`.
- For forks the script operates in dry-run mode and performs no write operations.

**Italiano:**
- Per mantenere la tracciabilità il workflow esegue `tools/issue_management/ensure_issue_links.py` su ogni PR interna.
- Quando un commit non contiene riferimenti a issue, lo script apre una nuova issue (`feature`, `bug` o `tech-debt`) e aggiorna il corpo della PR con `Fixes #<id>`.
- Sui fork lo script lavora in modalità dry-run senza effettuare scritture.

## Release notes
**English:**
- During planning the workflow generates `ReleaseNotes/<version>.md` from `.github/release_notes.hbs`, grouping entries under Breaking Changes, Features, Fixes, Performance, Refactor, Docs, and Chore.
- A PR comment shows the preview along with the pre-merge checklist (Tests, Lint, Build, Security scan).

**Italiano:**
- In fase di pianificazione viene generato `ReleaseNotes/<version>.md` usando `.github/release_notes.hbs`, con categorie Breaking Changes, Features, Fixes, Performance, Refactor, Docs e Chore.
- Un commento sulla PR mostra la preview insieme alla checklist pre-merge (Test, Lint, Build, Security scan).

## Release PR management / Gestione della PR di release
**English:** If the PR is not from a fork, the workflow creates or updates `release/v<version>` → `main` with the release notes file. It must follow the protected-branch merge rules.

**Italiano:** Se la PR non proviene da un fork, il workflow crea o aggiorna `release/v<version>` → `main` con il file di release notes. Deve rispettare le regole di merge del branch protetto.

## Branch workflows / Workflow sui branch
**English:**
- **PR to main:** version planning, release notes generation, release branch sync, missing issue creation, PR labelling, preview publication.
- **Push to `release/v*`:** validates that release notes stay aligned with the computed plan.
- **Push to `main`:** creates tag `v<version>` and the GitHub Release (draft or final according to `.releaserc` `githubRelease.draft`).

**Italiano:**
- **Pull request verso main:** pianificazione versione, generazione release notes, sincronizzazione branch di release, creazione issue mancanti, etichettatura PR, pubblicazione della preview.
- **Push su `release/v*`:** valida che le release notes restino allineate al piano calcolato.
- **Push su `main`:** crea il tag `v<version>` e la GitHub Release (bozza o definitiva in base a `githubRelease.draft` in `.releaserc`).

## Environments & forks / Ambienti e fork
**English:** Fork-originated PRs run in dry-run mode: no issues, labels, comments, or release PRs are created. This avoids unauthorised writes to external repositories.

**Italiano:** Le PR provenienti da fork girano in modalità dry-run: non vengono create issue, label, commenti o PR di release, evitando scritture non autorizzate verso repository esterni.

## Manual overrides / Override manuali
**English:** Besides `.release-override.yml`, adjust categories via `.releaserc`. Currently the `chore` category is excluded from publication.

**Italiano:** Oltre a `.release-override.yml`, puoi modificare le categorie tramite `.releaserc`. Al momento la categoria `chore` è esclusa dalla pubblicazione.

## Troubleshooting / Risoluzione problemi
**English:**
- Inspect the `plan-release` job logs for detailed plans.
- Review `.github/release-preview.md` generated locally for the full notes preview.
- Resolve conflicts on `release/v*`, then rerun the workflow (`sync` keeps artefacts consistent).

**Italiano:**
- Controlla i log del job `plan-release` per il dettaglio del piano.
- Consulta `.github/release-preview.md` generato localmente per la preview completa delle note.
- Risolvi manualmente i conflitti su `release/v*` e rilancia il workflow (`sync` mantiene coerenti gli artefatti).

**English:** For further customisation see `tools/release_notes/ci_release.py` and `.github/workflows/release.yml`.

**Italiano:** Per ulteriori personalizzazioni consulta `tools/release_notes/ci_release.py` e `.github/workflows/release.yml`.

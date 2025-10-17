# Automated Release Flow

## Overview
This document describes how the `Release automation` GitHub Actions workflow prepares a Momentum release.

## Commit conventions
- The repository follows [Conventional Commits](https://www.conventionalcommits.org/).
- Relevant types for version calculation: `feat:` → **minor**, `fix:`/`perf:`/`refactor:` → **patch**, commits with `BREAKING CHANGE` or `!` → **major**.
- The workflow runs `commitlint` on every pull request targeting `main`; keep commits compliant to avoid validation failures.

## Version calculation
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
- Modular monolith changes follow the same workflow as distributed services to guarantee aligned versioning.

## Automatic issue creation
- To preserve traceability the workflow runs `tools/issue_management/ensure_issue_links.py` on every internal PR.
- When a commit lacks issue references, the script opens a new issue (`feature`, `bug`, or `tech-debt`) and updates the PR body with `Fixes #<id>`.
- For forks the script operates in dry-run mode and performs no write operations.

## Release notes
- During planning the workflow generates `ReleaseNotes/<version>.md` from `.github/release_notes.hbs`, grouping entries under Breaking Changes, Features, Fixes, Performance, Refactor, Docs, and Chore.
- A PR comment shows the preview along with the pre-merge checklist (Tests, Lint, Build, Security scan).

## Release PR management
If the PR is not from a fork, the workflow creates or updates `release/v<version>` → `main` with the release notes file. It must follow the protected-branch merge rules.

## Branch workflows
- **PR to main:** version planning, release notes generation, release branch sync, missing issue creation, PR labelling, preview publication.
- **Push to `release/v*`:** validates that release notes stay aligned with the computed plan.
- **Push to `main`:** creates tag `v<version>` and the GitHub Release (draft or final according to `.releaserc` `githubRelease.draft`).

## Environments & forks
Fork-originated PRs run in dry-run mode: no issues, labels, comments, or release PRs are created. This avoids unauthorised writes to external repositories.

## Manual overrides
Besides `.release-override.yml`, adjust categories via `.releaserc`. Currently the `chore` category is excluded from publication.

## Troubleshooting
- Inspect the `plan-release` job logs for detailed plans.
- Review `.github/release-preview.md` generated locally for the full notes preview.
- Resolve conflicts on `release/v*`, then rerun the workflow (`sync` keeps artefacts consistent).

For further customisation see `tools/release_notes/ci_release.py` and `.github/workflows/release.yml`.

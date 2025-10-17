# Release notes automation

## Purpose
This module contains `generate_release_notes.py`, used locally and in GitHub automation to produce release note files for the project and its modules.

## Local usage
```bash
python tools/release_notes/generate_release_notes.py \
  --release-version 1.0.0 \
  --base-ref origin/main \
  --head-ref HEAD \
  --repo <owner>/<repository> \
  --github-token $GITHUB_TOKEN
```

Files are generated under the repository-level `ReleaseNotes` folder and inside each module at `modules/<module-name>/ReleaseNotes/`.

Install Python dependencies with:
```bash
pip install -r tools/release_notes/requirements.txt
```

## Automated release creation
`tools/release_notes/create_release.py` automates the whole flow:
1. Determines the next version using semantic rules by analysing commits since the latest tag.
2. Generates release notes enriched with GitHub issue titles.
3. Creates a `chore: release <version>` commit containing the generated files.
4. Tags `v<version>` on `main` and creates the `release/v<version>` branch.

Example run:
```bash
python -m tools.release_notes.create_release \
  --repo <owner>/<repository> \
  --github-token $GITHUB_TOKEN
```

Ensure the working tree is clean and execute the command from the `main` branch.

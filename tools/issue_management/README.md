# Issue management automation

## Purpose
`ensure_issue_links.py` runs as part of the pull request validation pipeline to ensure every commit links to at least one issue. With `--auto-create` and the `GITHUB_TOKEN`/`OPENAI_API_KEY` variables available, the tool creates a new issue summarising the commit and updates the PR body with `Fixes #<number>`.

## Manual execution
```bash
python tools/issue_management/ensure_issue_links.py \
  --repo <owner>/<repository> \
  --pr-number 123 \
  --auto-create
```

The command returns an error status when commits lack issue references. In auto mode it opens a ticket with the summary.

Install Python dependencies with:
```bash
pip install -r tools/issue_management/requirements.txt
```

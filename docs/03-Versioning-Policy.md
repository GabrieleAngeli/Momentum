# Versioning Policy

## SemVer rules
- Services and frontend follow Semantic Versioning.
- gRPC/OpenAPI contracts are versioned through `vX` namespaces/routes.
- Event schemas carry incremental `$id` identifiers.
- Docker tags use `major.minor.patch` plus `latest` for demo environments.
- Modular monolith packages track the same semantic versions as their distributed counterparts to guarantee compatibility.

## Merge flow to `main`
1. Each task lives on a feature branch derived from `main` and linked to a GitHub issue.
2. Feature branches merge through pull requests; merging into `main` happens via GitHub merge commits once approvals and checks pass.
3. Before merging, the _Ensure issue linking_ workflow verifies that every commit references an issue (`#<number>`). If missing, it creates an issue summarising the changes and updates the PR description.
4. After merge, `main` always reflects a release-ready state. Version numbers are bumped and tagged alongside release note generation.

## Automated issue & commit management
- Every commit message must include an issue reference (`#123`).
- The script `tools/issue_management/ensure_issue_links.py` runs in PR pipelines to enforce references. With `GITHUB_TOKEN` and `OPENAI_API_KEY` configured, it can create missing issues via LLM and update the PR body with `Fixes #<number>`.
- The script output reports the performed actions and blocks the check until every commit is linked.

## Release notes
- The manual _Generate release notes_ workflow builds release files from commits between a base ref and the target version.
- Files land in `ReleaseNotes/` and, when module-specific notes are produced, under `modules/<module-name>/ReleaseNotes/`.
- Each file lists linked issues and included commits to provide full visibility.
- During the workflow execution, a commit on `main` is created with the generated notes using the message `chore: add release notes for <version>`.

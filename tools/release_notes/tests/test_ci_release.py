from pathlib import Path

import pytest

from tools.release_notes import ci_release
from tools.release_notes.versioning import CommitMetadata, VersionBump


def test_determine_bump_override_from_labels():
    bump = ci_release.determine_bump_override(["release:minor", "something"])
    assert bump == VersionBump.MINOR


def test_apply_bump_override_with_explicit_version():
    commit = CommitMetadata(sha="abc", message="feat: add", files=())
    version, bump = ci_release.apply_bump_override(
        current_version="1.2.3",
        commits=[commit],
        default_bump=VersionBump.MINOR,
        override_bump=None,
        override_version="2.0.0",
    )
    assert version == "2.0.0"
    assert bump == VersionBump.MINOR


def test_collect_release_entries_merges_issue(monkeypatch):
    commit = CommitMetadata(sha="abc123", message="feat: add (#42)", files=("src/app.py",))

    def fake_fetch(repo: str, issues, token):
        return {42: ci_release.IssueInfo(number=42, title="Implement feature", url="https://example.com")}

    monkeypatch.setattr(ci_release, "fetch_issue_details", fake_fetch)

    entries = ci_release.collect_release_entries([commit], "owner/repo", None)
    assert entries
    entry = entries[0]
    assert entry.issue and entry.issue.number == 42
    assert entry.category == "feat"
    assert "abc123" in entry.commits


def test_compute_release_plan_skips_docs_without_override(tmp_path: Path, monkeypatch: pytest.MonkeyPatch):
    commit = CommitMetadata(sha="abc123", message="docs: update", files=("docs/readme.md",))

    monkeypatch.setattr(ci_release, "get_latest_tag", lambda prefix: None)
    monkeypatch.setattr(ci_release, "collect_commits", lambda base, head: [commit])

    entry = ci_release.ReleaseEntry(
        category="docs",
        title="Docs update",
        details="",
        issue=None,
        files=("docs/readme.md",),
        commits=("abc123",),
    )

    monkeypatch.setattr(ci_release, "collect_release_entries", lambda commits, repo, token: [entry])

    plan = ci_release.compute_release_plan(
        repo_root=tmp_path,
        repo="owner/repo",
        head_ref="HEAD",
        base_ref=None,
        labels=[],
        github_token=None,
    )

    assert not plan.should_release
    assert plan.reason and "documentazione" in plan.reason
    plan_file = tmp_path / ci_release.PLAN_FILE
    assert not plan_file.exists()

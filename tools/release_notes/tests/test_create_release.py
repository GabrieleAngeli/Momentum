from pathlib import Path
from subprocess import CalledProcessError

import pytest

from tools.release_notes import create_release


class GitStub:
    def __init__(self, responses):
        self._responses = responses
        self.calls = []

    def __call__(self, args, cwd=None):
        key = tuple(args)
        self.calls.append(key)
        handler = self._responses.get(key)
        if handler is None:
            raise AssertionError(f"Unexpected git command: {args}")
        if isinstance(handler, Exception):
            raise handler
        if callable(handler):
            return handler()
        return handler


def test_ensure_on_main_accepts_expected_branch(monkeypatch):
    monkeypatch.delenv("GITHUB_REF", raising=False)
    stub = GitStub({
        ("rev-parse", "--abbrev-ref", "HEAD"): "main",
    })
    monkeypatch.setattr(create_release, "run_git", stub)

    create_release.ensure_on_main(Path("."))


def test_ensure_on_main_accepts_detached_head_matching_remote(monkeypatch):
    monkeypatch.delenv("GITHUB_REF", raising=False)
    head_sha = "abc123"
    stub = GitStub({
        ("rev-parse", "--abbrev-ref", "HEAD"): "HEAD",
        ("rev-parse", "HEAD"): head_sha,
        ("rev-parse", "main"): CalledProcessError(returncode=128, cmd="git"),
        ("rev-parse", "origin/main"): head_sha,
    })
    monkeypatch.setattr(create_release, "run_git", stub)

    create_release.ensure_on_main(Path("."))


def test_ensure_on_main_raises_if_branch_is_wrong(monkeypatch):
    monkeypatch.delenv("GITHUB_REF", raising=False)
    head_sha = "def456"
    stub = GitStub({
        ("rev-parse", "--abbrev-ref", "HEAD"): "feature",
        ("rev-parse", "HEAD"): head_sha,
        ("rev-parse", "main"): CalledProcessError(returncode=128, cmd="git"),
        ("rev-parse", "origin/main"): "123000",
    })
    monkeypatch.setattr(create_release, "run_git", stub)

    with pytest.raises(RuntimeError):
        create_release.ensure_on_main(Path("."))

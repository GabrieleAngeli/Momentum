from types import SimpleNamespace

import pytest

from tools.issue_management import ensure_issue_links as module


@pytest.fixture(autouse=True)
def _set_token(monkeypatch: pytest.MonkeyPatch) -> None:
    monkeypatch.setenv("GITHUB_TOKEN", "token")


def make_commit(**overrides):
    defaults = {
        "sha": "abc1234567890",
        "message": "feat: add feature",
        "files": ("src/app.py",),
        "html_url": "https://example.com/commit/abc1234",
    }
    return module.Commit(**{**defaults, **overrides})


def test_ensure_issue_links_creates_issue_and_updates_body(monkeypatch: pytest.MonkeyPatch) -> None:
    commits = [make_commit()]
    client_state = SimpleNamespace(created=[], updated_body="", titles_checked=[])

    class FakeClient:
        def __init__(self, token: str, repo: str) -> None:
            self.repo = repo

        def list_pull_request_commits(self, pr_number: int):
            return commits

        def get_pull_request(self, pr_number: int):
            return {"body": ""}

        def find_recent_issue(self, title: str, window_days: int = 90):
            client_state.titles_checked.append(title)
            return None

        def create_issue(self, title: str, body: str, labels=None):
            client_state.created.append((title, body, tuple(labels or ())))
            return 101

        def update_pull_request_body(self, pr_number: int, body: str) -> None:
            client_state.updated_body = body

    monkeypatch.setattr(module, "GitHubClient", FakeClient)
    monkeypatch.setattr(module, "summarise_with_llm", lambda *args, **kwargs: "Sintesi")

    success, messages = module.ensure_issue_links(
        repo="owner/repo",
        pr_number=7,
        auto_create=True,
        openai_api_key=None,
        openai_model="gpt-4o-mini",
        labels=["feature"],
    )

    assert success is True
    assert any("Closes #101" in line for line in client_state.updated_body.splitlines())
    assert client_state.created, "una issue deve essere creata"
    assert client_state.titles_checked
    assert any("Creata issue #101" in msg for msg in messages)


def test_reuses_existing_issue(monkeypatch: pytest.MonkeyPatch) -> None:
    commits = [make_commit(message="fix: adjust bug")]
    client_state = SimpleNamespace(created=False, updated_body="")

    class FakeClient:
        def __init__(self, token: str, repo: str) -> None:
            self.repo = repo

        def list_pull_request_commits(self, pr_number: int):
            return commits

        def get_pull_request(self, pr_number: int):
            return {"body": ""}

        def find_recent_issue(self, title: str, window_days: int = 90):
            return 77

        def create_issue(self, *args, **kwargs):
            client_state.created = True
            return 77

        def update_pull_request_body(self, pr_number: int, body: str) -> None:
            client_state.updated_body = body

    monkeypatch.setattr(module, "GitHubClient", FakeClient)
    monkeypatch.setattr(module, "summarise_with_llm", lambda *args, **kwargs: "Sintesi")

    success, messages = module.ensure_issue_links(
        repo="owner/repo",
        pr_number=8,
        auto_create=True,
        openai_api_key=None,
        openai_model="gpt-4o-mini",
        labels=["bug"],
    )

    assert success is True
    assert not client_state.created
    assert any("Closes #77" in line for line in client_state.updated_body.splitlines())
    assert any("Riutilizzata issue #77" in msg for msg in messages)

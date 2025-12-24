#!/usr/bin/env python3
"""Utility to ensure every commit in a pull request is linked to at least one issue.

Optionally auto-creates an issue by summarizing the commit via an LLM when no explicit
reference is found (pattern `#123`).
"""
from __future__ import annotations

import argparse
import json
import os
import re
import sys
import textwrap
from dataclasses import dataclass
from datetime import datetime, timedelta
from typing import List, Optional, Sequence, Tuple

import requests

ISSUE_PATTERN = re.compile(r"#(\d+)")


@dataclass
class Commit:
    sha: str
    message: str
    files: Sequence[str]
    html_url: str

    @property
    def short_sha(self) -> str:
        return self.sha[:7]


class GitHubClient:
    def __init__(self, token: str, repo: str) -> None:
        self._token = token
        self._repo = repo
        self._base_url = f"https://api.github.com/repos/{repo}"
        self._session = requests.Session()
        self._session.headers.update(
            {
                "Authorization": f"Bearer {token}",
                "Accept": "application/vnd.github+json",
                "X-GitHub-Api-Version": "2022-11-28",
            }
        )

    def get_pull_request(self, pr_number: int) -> dict:
        response = self._session.get(f"{self._base_url}/pulls/{pr_number}", timeout=30)
        self._raise_for_status(response, "fetching pull request details")
        return response.json()

    def update_pull_request_body(self, pr_number: int, body: str) -> None:
        response = self._session.patch(
            f"{self._base_url}/pulls/{pr_number}",
            json={"body": body},
            timeout=30,
        )
        self._raise_for_status(response, "updating pull request description")

    def create_issue(self, title: str, body: str, labels: Optional[Sequence[str]] = None) -> int:
        payload = {"title": title, "body": body}
        if labels:
            payload["labels"] = list(labels)
        response = self._session.post(
            f"{self._base_url}/issues",
            json=payload,
            timeout=30,
        )
        self._raise_for_status(response, "creating issue")
        data = response.json()
        return data["number"]

    def find_recent_issue(self, title: str, window_days: int = 90) -> Optional[int]:
        cutoff = (datetime.utcnow() - timedelta(days=window_days)).date().isoformat()
        query = f'repo:{self._repo} type:issue in:title "{title}" created:>={cutoff}'
        response = self._session.get(
            "https://api.github.com/search/issues",
            params={"q": query, "per_page": 5},
            timeout=30,
        )
        self._raise_for_status(response, "searching for similar issues")
        for item in response.json().get("items", []):
            if item.get("title", "").strip().lower() == title.strip().lower():
                return int(item.get("number"))
        return None

    def list_pull_request_commits(self, pr_number: int) -> List[Commit]:
        commits: List[Commit] = []
        page = 1
        while True:
            response = self._session.get(
                f"{self._base_url}/pulls/{pr_number}/commits",
                params={"per_page": 100, "page": page},
                timeout=30,
            )
            self._raise_for_status(response, "fetching pull request commits")
            batch = response.json()
            if not batch:
                break
            for item in batch:
                commits.append(
                    Commit(
                        sha=item["sha"],
                        message=item["commit"]["message"],
                        files=self._fetch_commit_files(item["sha"]),
                        html_url=item["html_url"],
                    )
                )
            page += 1
        return commits

    def _fetch_commit_files(self, sha: str) -> Sequence[str]:
        response = self._session.get(f"{self._base_url}/commits/{sha}", timeout=30)
        self._raise_for_status(response, "fetching commit files")
        data = response.json()
        return [file_info["filename"] for file_info in data.get("files", [])]

    @staticmethod
    def _raise_for_status(response: requests.Response, context: str) -> None:
        try:
            response.raise_for_status()
        except requests.HTTPError as exc:  # pragma: no cover
            message = f"Error while {context}. Status {response.status_code}: {response.text}"
            raise RuntimeError(message) from exc


def extract_issue_numbers(text: str) -> List[int]:
    return [int(match.group(1)) for match in ISSUE_PATTERN.finditer(text)]


def _parse_llm_summary(text: str) -> Tuple[str, str]:
    """Best-effort parser: first non-empty line is title; the rest is the paragraph."""
    lines = [ln.strip() for ln in (text or "").splitlines()]
    lines = [ln for ln in lines if ln]  # remove empty
    if not lines:
        return ("Update changes", "Summarize commit changes and impacted files.")
    title = lines[0]
    paragraph = " ".join(lines[1:]).strip()
    if not paragraph:
        paragraph = "Summarize commit changes and impacted files."
    return title, paragraph


def summarise_with_llm(
    openai_api_key: Optional[str],
    message: str,
    files: Sequence[str],
    model: str,
) -> Tuple[str, str]:
    """Generate an English commit summary (title + paragraph). Falls back to heuristics."""
    context = {"message": message, "files": list(files)}
    heuristic_title, heuristic_paragraph = heuristic_commit_summary(context)
    if not openai_api_key:
        return heuristic_title, heuristic_paragraph

    payload = {
        "model": model,
        "messages": [
            {
                "role": "system",
                "content": (
                    "You are an assistant that summarizes software commits. "
                    "Respond in English. "
                    "Output exactly: a concise title (max 12 words) on the first line, "
                    "then one paragraph describing the main changes."
                ),
            },
            {
                "role": "user",
                "content": textwrap.dedent(
                    f"""
                    Summarize the following commit:

                    Message:
                    {message}

                    Files involved:
                    {json.dumps(context["files"], ensure_ascii=False, indent=2)}
                    """
                ).strip(),
            },
        ],
        "temperature": 0.2,
        "max_tokens": 250,
    }

    try:
        response = requests.post(
            "https://api.openai.com/v1/chat/completions",
            headers={
                "Authorization": f"Bearer {openai_api_key}",
                "Content-Type": "application/json",
            },
            json=payload,
            timeout=60,
        )
        response.raise_for_status()
        data = response.json()
        raw = (data["choices"][0]["message"]["content"] or "").strip()
        title, paragraph = _parse_llm_summary(raw)

        # Safety trims
        title = title[:120].strip()  # keep title reasonable, GitHub limit is large anyway
        paragraph = paragraph.strip() or heuristic_paragraph
        return title, paragraph
    except Exception:  # pragma: no cover
        return heuristic_title, heuristic_paragraph


def heuristic_commit_summary(context: dict) -> Tuple[str, str]:
    """English-ish fallback without a real translator."""
    first_line = (context.get("message") or "").splitlines()[0].strip()
    files = context.get("files") or []
    if files:
        limited_files = ", ".join(files[:5])
        if len(files) > 5:
            limited_files += ", …"
        title = (first_line[:60] or "Update changes").strip()
        paragraph = (
            f"Changes from commit message: {first_line}. "
            f"Main files touched: {limited_files}."
        )
        return title, paragraph
    title = (first_line[:60] or "Update changes").strip()
    paragraph = f"Changes from commit message: {first_line}."
    return title, paragraph


def ensure_issue_links(
    repo: str,
    pr_number: int,
    auto_create: bool,
    openai_api_key: Optional[str],
    openai_model: str,
    labels: Sequence[str],
) -> Tuple[bool, List[str]]:
    token = os.getenv("GITHUB_TOKEN")
    if not token:
        raise RuntimeError("GITHUB_TOKEN is not set in the environment.")

    client = GitHubClient(token, repo)
    commits = client.list_pull_request_commits(pr_number)
    missing_commits: List[Commit] = [c for c in commits if not extract_issue_numbers(c.message)]

    if not missing_commits:
        return True, ["All commits are linked to at least one issue."]

    pr_data = client.get_pull_request(pr_number)
    pr_body = pr_data.get("body") or ""

    if not auto_create:
        missing_list = ", ".join(commit.short_sha for commit in missing_commits)
        message = (
            "Commits without an issue reference detected: "
            f"{missing_list}. Please add a reference like #<number>."
        )
        return False, [message]

    log_messages: List[str] = []
    for commit in missing_commits:
        issue_title, summary_paragraph = summarise_with_llm(
            openai_api_key, commit.message, commit.files, openai_model
        )

        # Ensure the issue title is not empty and not too long
        issue_title = (issue_title or "Update changes").strip()[:120]

        body = textwrap.dedent(
            f"""
            Auto-generated issue for commit `{commit.sha}`.

            ## Summary
            {summary_paragraph}

            ## Links
            - Commit: {commit.html_url}
            """
        ).strip()

        existing_issue = client.find_recent_issue(issue_title)
        if existing_issue:
            issue_number = existing_issue
            log_messages.append(f"Reused issue #{issue_number} for commit {commit.short_sha}.")
        else:
            issue_number = client.create_issue(title=issue_title, body=body, labels=labels)
            log_messages.append(f"Created issue #{issue_number} for commit {commit.short_sha}.")

        bullet = f"- Closes #{issue_number} (commit {commit.short_sha})"
        if bullet not in pr_body:
            pr_body = (pr_body + "\n" + bullet).strip()

    client.update_pull_request_body(pr_number, pr_body)
    log_messages.append("Updated the pull request description with generated issues.")
    log_messages.append("Issue IDs are now linked in the pull request body.")
    return True, log_messages


def parse_args(argv: Optional[Sequence[str]] = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", required=True, help="Repository in the form <owner>/<name>.")
    parser.add_argument("--pr-number", required=True, type=int, help="Pull request number.")
    parser.add_argument(
        "--auto-create",
        action="store_true",
        help="Automatically create an issue when no reference is found.",
    )
    parser.add_argument(
        "--openai-model",
        default="gpt-4o-mini",
        help="Model used to generate the summary (default: gpt-4o-mini).",
    )
    parser.add_argument(
        "--labels",
        nargs="*",
        default=["triage"],
        help="Labels to assign to auto-generated issues.",
    )
    return parser.parse_args(argv)


def main(argv: Optional[Sequence[str]] = None) -> int:
    args = parse_args(argv)
    try:
        success, messages = ensure_issue_links(
            repo=args.repo,
            pr_number=args.pr_number,
            auto_create=args.auto_create,
            openai_api_key=os.getenv("OPENAI_API_KEY"),
            openai_model=args.openai_model,
            labels=args.labels,
        )
    except Exception as exc:  # pragma: no cover
        print(f"Error: {exc}", file=sys.stderr)
        return 1

    for message in messages:
        print(message)

    return 0 if success else 1


if __name__ == "__main__":
    sys.exit(main())

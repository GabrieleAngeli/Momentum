#!/usr/bin/env python3
"""Utility per garantire che ogni commit di una pull request sia collegato ad almeno una issue.

Il tool può opzionalmente creare automaticamente una issue sintetizzando le modifiche
tramite un LLM quando non viene trovato alcun riferimento esplicito (pattern `#123`).
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
        self._raise_for_status(response, "recupero dettagli della pull request")
        return response.json()

    def update_pull_request_body(self, pr_number: int, body: str) -> None:
        response = self._session.patch(
            f"{self._base_url}/pulls/{pr_number}",
            json={"body": body},
            timeout=30,
        )
        self._raise_for_status(response, "aggiornamento della descrizione della pull request")

    def create_issue(self, title: str, body: str, labels: Optional[Sequence[str]] = None) -> int:
        payload = {"title": title, "body": body}
        if labels:
            payload["labels"] = list(labels)
        response = self._session.post(
            f"{self._base_url}/issues",
            json=payload,
            timeout=30,
        )
        self._raise_for_status(response, "creazione della issue")
        data = response.json()
        return data["number"]

    def find_recent_issue(self, title: str, window_days: int = 90) -> Optional[int]:
        cutoff = (datetime.utcnow() - timedelta(days=window_days)).date().isoformat()
        query = (
            f"repo:{self._repo} type:issue in:title \"{title}\" created:>={cutoff}"
        )
        response = self._session.get(
            "https://api.github.com/search/issues",
            params={"q": query, "per_page": 5},
            timeout=30,
        )
        self._raise_for_status(response, "ricerca di issue simili")
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
            self._raise_for_status(response, "recupero dei commit della pull request")
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
        self._raise_for_status(response, "recupero dei file del commit")
        data = response.json()
        return [file_info["filename"] for file_info in data.get("files", [])]

    @staticmethod
    def _raise_for_status(response: requests.Response, context: str) -> None:
        try:
            response.raise_for_status()
        except requests.HTTPError as exc:  # pragma: no cover - logging helper
            message = (
                f"Errore durante il {context}. Stato {response.status_code}: "
                f"{response.text}"
            )
            raise RuntimeError(message) from exc


def extract_issue_numbers(text: str) -> List[int]:
    return [int(match.group(1)) for match in ISSUE_PATTERN.finditer(text)]


def summarise_with_llm(openai_api_key: Optional[str], message: str, files: Sequence[str], model: str) -> str:
    """Genera una sintesi del commit usando un LLM.

    Se non è disponibile alcuna chiave API, viene utilizzata una sintesi euristica.
    """
    context = {
        "message": message,
        "files": list(files),
    }
    heuristic_summary = heuristic_commit_summary(context)
    if not openai_api_key:
        return heuristic_summary

    payload = {
        "model": model,
        "messages": [
            {
                "role": "system",
                "content": (
                    "Sei un assistente che riassume commit software. "
                    "Produci un titolo conciso (max 12 parole) e un paragrafo "
                    "che descrive le modifiche principali."
                ),
            },
            {
                "role": "user",
                "content": textwrap.dedent(
                    f"""
                    Riassumi il seguente commit:

                    Messaggio:
                    {message}

                    File coinvolti:
                    {json.dumps(context['files'], ensure_ascii=False, indent=2)}
                    """
                ).strip(),
            },
        ],
        "temperature": 0.2,
        "max_tokens": 300,
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
        summary = data["choices"][0]["message"]["content"].strip()
        return summary or heuristic_summary
    except Exception:  # pragma: no cover - best effort fallback
        return heuristic_summary


def heuristic_commit_summary(context: dict) -> str:
    message = context["message"].splitlines()[0].strip()
    files = context.get("files") or []
    if files:
        limited_files = ", ".join(files[:5])
        if len(files) > 5:
            limited_files += ", …"
        return f"{message}. File principali: {limited_files}."
    return message


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
        raise RuntimeError("GITHUB_TOKEN non presente nell'ambiente.")

    client = GitHubClient(token, repo)
    commits = client.list_pull_request_commits(pr_number)
    missing_commits: List[Commit] = []
    for commit in commits:
        if not extract_issue_numbers(commit.message):
            missing_commits.append(commit)

    if not missing_commits:
        return True, ["Tutti i commit sono associati a issue."]

    pr_data = client.get_pull_request(pr_number)
    pr_body = pr_data.get("body") or ""

    log_messages: List[str] = []
    if not auto_create:
        missing_list = ", ".join(commit.short_sha for commit in missing_commits)
        message = (
            "Commit senza riferimento a issue rilevati: "
            f"{missing_list}. Aggiungere un riferimento del tipo #<numero>."
        )
        return False, [message]

    for commit in missing_commits:
        summary = summarise_with_llm(openai_api_key, commit.message, commit.files, openai_model)
        title = commit.message.splitlines()[0][:70]
        body = textwrap.dedent(
            f"""
            Issue generata automaticamente per il commit {commit.sha}.

            ## Sintesi
            {summary}

            ## Collegamenti
            - Commit: {commit.html_url}
            """
        ).strip()

        existing_issue = client.find_recent_issue(title)
        if existing_issue:
            issue_number = existing_issue
            log_messages.append(
                f"Riutilizzata issue #{issue_number} per il commit {commit.short_sha}."
            )
        else:
            issue_number = client.create_issue(title=title, body=body, labels=labels)
            log_messages.append(
                f"Creata issue #{issue_number} per il commit {commit.short_sha}."
            )

        bullet = f"- Closes #{issue_number} (commit {commit.short_sha})"
        if bullet not in pr_body:
            pr_body = (pr_body + "\n" + bullet).strip()

    client.update_pull_request_body(pr_number, pr_body)
    log_messages.append("Aggiornata la descrizione della pull request con le issue generate.")
    log_messages.append(
        "Gli ID delle issue sono ora collegati nel corpo della pull request."
    )
    return True, log_messages


def parse_args(argv: Optional[Sequence[str]] = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", required=True, help="Repository nel formato <owner>/<name>.")
    parser.add_argument("--pr-number", required=True, type=int, help="Numero della pull request.")
    parser.add_argument(
        "--auto-create",
        action="store_true",
        help="Crea automaticamente una issue quando non viene trovato alcun riferimento.",
    )
    parser.add_argument(
        "--openai-model",
        default="gpt-4o-mini",
        help="Modello da utilizzare per la generazione della sintesi (default: gpt-4o-mini).",
    )
    parser.add_argument(
        "--labels",
        nargs="*",
        default=["triage"],
        help="Etichette da assegnare alle issue generate automaticamente.",
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
    except Exception as exc:  # pragma: no cover - CLI failure handling
        print(f"Errore: {exc}", file=sys.stderr)
        return 1

    for message in messages:
        print(message)

    return 0 if success else 1


if __name__ == "__main__":
    sys.exit(main())

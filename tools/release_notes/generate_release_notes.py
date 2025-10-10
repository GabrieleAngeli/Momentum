#!/usr/bin/env python3
"""Genera file di release note per i moduli del progetto a partire da un intervallo di commit."""
from __future__ import annotations

import argparse
import re
import subprocess
import sys
from collections import defaultdict
from dataclasses import dataclass
from pathlib import Path
from typing import Dict, Iterable, List, Optional, Sequence, Set

import requests

ISSUE_PATTERN = re.compile(r"#(\d+)")


@dataclass
class Commit:
    sha: str
    subject: str
    body: str
    files: Sequence[str]

    @property
    def short_sha(self) -> str:
        return self.sha[:7]

    @property
    def message(self) -> str:
        if self.body:
            return f"{self.subject}\n{self.body}".strip()
        return self.subject


@dataclass
class ModuleReleaseData:
    issues: Set[int]
    commits: List[Commit]


def run_git(args: Sequence[str], cwd: Optional[Path] = None) -> str:
    result = subprocess.run(
        ["git", *args],
        cwd=cwd,
        check=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
    )
    return result.stdout.strip()


def determine_commits(base_ref: str, head_ref: str) -> List[str]:
    if base_ref:
        range_spec = f"{base_ref}..{head_ref}"
    else:
        range_spec = head_ref
    output = run_git(["rev-list", range_spec])
    commits = [line for line in output.splitlines() if line]
    commits.reverse()  # cronologico
    return commits


def load_commit(sha: str) -> Commit:
    pretty_format = "%H%x01%s%x01%b"
    output = run_git(["show", sha, f"--pretty=format:{pretty_format}", "--name-only"])
    header, *file_lines = output.splitlines()
    sha_value, subject, body = header.split("\x01")
    files = [line.strip() for line in file_lines if line.strip()]
    return Commit(sha=sha_value, subject=subject.strip(), body=body.strip(), files=files)


def extract_issue_numbers(text: str) -> Set[int]:
    return {int(match.group(1)) for match in ISSUE_PATTERN.finditer(text)}


def group_commits_by_module(commits: Sequence[Commit], modules_root: str) -> Dict[str, ModuleReleaseData]:
    grouped: Dict[str, ModuleReleaseData] = defaultdict(lambda: ModuleReleaseData(set(), []))
    modules_root_with_sep = modules_root.rstrip("/") + "/"

    for commit in commits:
        matched_modules: Set[str] = set()
        for file_path in commit.files:
            if file_path.startswith(modules_root_with_sep):
                parts = file_path.split("/")
                if len(parts) >= 2:
                    matched_modules.add(parts[1])
            else:
                matched_modules.add("__root__")
        if not matched_modules:
            matched_modules.add("__root__")

        issues = extract_issue_numbers(commit.message)
        for module in matched_modules:
            bucket = grouped[module]
            bucket.commits.append(commit)
            bucket.issues.update(issues)

    return grouped


def fetch_issue_titles(repo: str, issue_numbers: Iterable[int], token: Optional[str]) -> Dict[int, str]:
    if not issue_numbers:
        return {}
    headers = {"Accept": "application/vnd.github+json", "X-GitHub-Api-Version": "2022-11-28"}
    if token:
        headers["Authorization"] = f"Bearer {token}"
    titles: Dict[int, str] = {}
    session = requests.Session()
    session.headers.update(headers)
    for number in issue_numbers:
        response = session.get(f"https://api.github.com/repos/{repo}/issues/{number}", timeout=30)
        if response.status_code == 404:
            continue
        response.raise_for_status()
        data = response.json()
        titles[number] = data.get("title", "")
    return titles


def render_release_note(version: str, module: str, data: ModuleReleaseData, issue_titles: Dict[int, str]) -> str:
    lines = [f"# Release {version}", ""]
    if data.issues:
        lines.append("## Issues")
        for issue in sorted(data.issues):
            title = issue_titles.get(issue, "")
            if title:
                lines.append(f"- #{issue} — {title}")
            else:
                lines.append(f"- #{issue}")
        lines.append("")
    lines.append("## Commits")
    for commit in data.commits:
        lines.append(f"- {commit.short_sha} — {commit.subject}")
    lines.append("")
    lines.append("## Dettagli")
    lines.append(
        "Ogni commit elencato include nel messaggio il riferimento alla issue corrispondente."
    )
    return "\n".join(lines).strip() + "\n"


def write_release_notes(
    repo_root: Path,
    modules_root: str,
    version: str,
    grouped: Dict[str, ModuleReleaseData],
    issue_titles: Dict[int, str],
    release_dir_name: str,
) -> List[Path]:
    created_files: List[Path] = []
    for module, data in grouped.items():
        if not data.commits:
            continue
        if module == "__root__":
            base_dir = repo_root / release_dir_name
        else:
            base_dir = repo_root / modules_root / module / release_dir_name
        base_dir.mkdir(parents=True, exist_ok=True)
        file_path = base_dir / f"{version}.md"
        file_path.write_text(render_release_note(version, module, data, issue_titles), encoding="utf-8")
        created_files.append(file_path)
    return created_files


def parse_args(argv: Optional[Sequence[str]] = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--release-version", required=True, help="Versione di rilascio (es. 1.2.3).")
    parser.add_argument("--head-ref", required=True, help="Commit HEAD da includere nel rilascio.")
    parser.add_argument("--base-ref", help="Commit di partenza escluso dal rilascio.")
    parser.add_argument(
        "--modules-root",
        default="modules",
        help="Directory contenente i moduli (default: modules).",
    )
    parser.add_argument(
        "--release-dir",
        default="ReleaseNotes",
        help="Nome della cartella che conterrà i file di release (default: ReleaseNotes).",
    )
    parser.add_argument(
        "--repo",
        required=True,
        help="Repository nel formato <owner>/<name> per recuperare i titoli delle issue.",
    )
    parser.add_argument(
        "--github-token",
        help="Token GitHub per interrogare le issue e arricchire le release notes.",
    )
    parser.add_argument(
        "--repository-root",
        default=str(Path(__file__).resolve().parents[2]),
        help="Percorso della root del repository (default: due livelli sopra lo script).",
    )
    return parser.parse_args(argv)


def main(argv: Optional[Sequence[str]] = None) -> int:
    args = parse_args(argv)
    repo_root = Path(args.repository_root).resolve()

    commits_sha = determine_commits(args.base_ref, args.head_ref)
    if not commits_sha:
        print("Nessun commit trovato nell'intervallo specificato, nessuna release note generata.")
        return 0

    commits = [load_commit(sha) for sha in commits_sha]
    grouped = group_commits_by_module(commits, args.modules_root)

    all_issues: Set[int] = set()
    for data in grouped.values():
        all_issues.update(data.issues)

    issue_titles = fetch_issue_titles(args.repo, all_issues, args.github_token)

    created_files = write_release_notes(
        repo_root=repo_root,
        modules_root=args.modules_root,
        version=args.release_version,
        grouped=grouped,
        issue_titles=issue_titles,
        release_dir_name=args.release_dir,
    )

    if not created_files:
        print("Nessun file di release note generato.")
    else:
        print("Generati i seguenti file di release note:")
        for path in created_files:
            print(f" - {path.relative_to(repo_root)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())

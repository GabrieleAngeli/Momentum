#!/usr/bin/env python3
"""Automatizza il processo di creazione di una release."""

from __future__ import annotations

import argparse
import sys
from pathlib import Path
from typing import Iterable, Optional, Sequence

import os
from subprocess import CalledProcessError

from tools.release_notes.generate_release_notes import (
    determine_commits,
    fetch_issue_titles,
    group_commits_by_module,
    load_commit,
    write_release_notes,
)
from tools.release_notes.versioning import (
    collect_commits,
    get_latest_tag,
    plan_next_version,
    run_git,
)


def ensure_clean_worktree(repository_root: Path) -> None:
    status = run_git(["status", "--porcelain"], cwd=repository_root)
    if status.strip():
        raise RuntimeError(
            "Il working tree contiene modifiche non committate."
            " Completare o stornare le modifiche prima di generare una release."
        )


def ensure_on_main(repository_root: Path, expected_branch: str = "main") -> None:
    """Verifica che la release venga generata da ``expected_branch``.

    In CI (es. GitHub Actions) il checkout avviene spesso in modalità detached HEAD,
    quindi ``git rev-parse --abbrev-ref HEAD`` restituisce ``HEAD`` anziché il nome
    del branch. In questo caso confrontiamo l'hash di ``HEAD`` con quello del branch
    atteso (locale o remoto) prima di fallire.
    """

    branch = run_git(["rev-parse", "--abbrev-ref", "HEAD"], cwd=repository_root).strip()
    if branch == expected_branch:
        return

    head_sha = run_git(["rev-parse", "HEAD"], cwd=repository_root).strip()
    candidate_refs = [expected_branch, f"origin/{expected_branch}"]

    # Alcuni workflow (es. checkout@v3) esportano anche GITHUB_REF.
    github_ref = os.environ.get("GITHUB_REF")
    if github_ref and github_ref.startswith("refs/heads/"):
        candidate_refs.insert(0, github_ref.replace("refs/heads/", ""))

    for ref in candidate_refs:
        try:
            ref_sha = run_git(["rev-parse", ref], cwd=repository_root).strip()
        except CalledProcessError:
            continue
        if ref_sha == head_sha:
            return

    raise RuntimeError(
        "La release deve essere creata partendo da "
        f"{expected_branch}, branch corrente: {branch}."
    )


def generate_release_notes(
    *,
    repository_root: Path,
    version: str,
    base_ref: Optional[str],
    repo: str,
    github_token: Optional[str],
    modules_root: str,
    release_dir: str,
) -> Iterable[Path]:
    commits_sha = determine_commits(base_ref or "", "HEAD")
    if not commits_sha:
        return []
    commits = [load_commit(sha) for sha in commits_sha]
    grouped = group_commits_by_module(commits, modules_root)

    all_issues = set()
    for data in grouped.values():
        all_issues.update(data.issues)

    issue_titles = fetch_issue_titles(repo, all_issues, github_token)
    created_files = write_release_notes(
        repo_root=repository_root,
        modules_root=modules_root,
        version=version,
        grouped=grouped,
        issue_titles=issue_titles,
        release_dir_name=release_dir,
    )
    return created_files


def create_commit(repository_root: Path, version: str, files: Sequence[Path]) -> None:
    if not files:
        raise RuntimeError("Nessun file di release note generato, impossibile creare la release.")
    rel_paths = [str(path.relative_to(repository_root)) for path in files]
    run_git(["add", *rel_paths], cwd=repository_root)
    run_git(["commit", "-m", f"chore: release {version}"], cwd=repository_root)


def create_tag_and_branch(repository_root: Path, version: str, tag_prefix: str) -> None:
    tag_name = f"{tag_prefix}{version}" if tag_prefix else version
    run_git(["tag", tag_name], cwd=repository_root)
    run_git(["branch", f"release/{tag_name}", "HEAD"], cwd=repository_root)


def parse_args(argv: Optional[Sequence[str]] = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", required=True, help="Repository GitHub nel formato owner/name.")
    parser.add_argument("--github-token", help="Token GitHub per arricchire le release notes.")
    parser.add_argument(
        "--repository-root",
        default=Path(__file__).resolve().parents[2],
        type=Path,
        help="Percorso della root del repository.",
    )
    parser.add_argument(
        "--modules-root",
        default="modules",
        help="Directory contenente i moduli per cui generare release notes.",
    )
    parser.add_argument(
        "--release-dir",
        default="ReleaseNotes",
        help="Cartella in cui salvare le release notes generate.",
    )
    parser.add_argument(
        "--tag-prefix",
        default="v",
        help="Prefisso da anteporre al tag di release.",
    )
    parser.add_argument(
        "--branch",
        default="main",
        help="Branch da cui generare la release (default: main).",
    )
    return parser.parse_args(argv)


def main(argv: Optional[Sequence[str]] = None) -> int:
    args = parse_args(argv)
    repo_root: Path = args.repository_root.resolve()

    ensure_clean_worktree(repo_root)
    ensure_on_main(repo_root, args.branch)

    latest_tag = get_latest_tag(args.tag_prefix)
    current_version = latest_tag[len(args.tag_prefix) :] if latest_tag else None
    commits = collect_commits(latest_tag, "HEAD")
    if not commits:
        print("Nessun commit da rilasciare. Nessuna azione eseguita.")
        return 0

    next_version, bump = plan_next_version(current_version=current_version, commits=commits)
    print(f"Ultimo tag: {latest_tag or 'nessuno'}")
    print(f"Incremento richiesto: {bump.name.lower()}")
    print(f"Nuova versione: {next_version}")

    created_files = list(
        generate_release_notes(
        repository_root=repo_root,
        version=next_version,
        base_ref=latest_tag,
        repo=args.repo,
        github_token=args.github_token,
        modules_root=args.modules_root,
        release_dir=args.release_dir,
        )
    )

    create_commit(repo_root, next_version, created_files)
    create_tag_and_branch(repo_root, next_version, args.tag_prefix)

    print(f"Creato tag {args.tag_prefix}{next_version} e branch release/{args.tag_prefix}{next_version}")
    return 0


if __name__ == "__main__":
    sys.exit(main())


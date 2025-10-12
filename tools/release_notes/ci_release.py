"""Automation helpers used by the GitHub Actions release workflow.

The module exposes a CLI with three main responsibilities:

* plan – executed for pull requests. It analyses the commits since the
  latest tag, determines the next semantic version and generates the
  release notes preview together with the artefacts committed on the
  `release/v<version>` branch.
* sync – executed when the release branch receives new pushes. It
  validates that the generated artefacts are still up to date.
* finalize – executed on pushes to main after merging the release
  branch. It tags the repository and publishes the GitHub Release using
  the same notes generated during the plan phase.

The implementation favours readability over cleverness because the
workflow is fairly involved and needs to stay maintainable by the
on-call team.
"""

from __future__ import annotations

import argparse
import json
import os
import re
import subprocess
import sys
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from textwrap import dedent
from typing import Dict, Iterable, List, Mapping, MutableMapping, Optional, Sequence, Set, Tuple

import requests
import yaml

if __package__ is None or __package__ == "":
    REPO_ROOT = Path(__file__).resolve().parents[2]
    sys.path.insert(0, str(REPO_ROOT))

from tools.release_notes.generate_release_notes import extract_issue_numbers
from tools.release_notes.versioning import (
    CommitMetadata,
    VersionBump,
    collect_commits,
    format_version,
    get_latest_tag,
    parse_version,
    plan_next_version,
    run_git,
)


CATEGORY_ORDER: Sequence[Tuple[str, str]] = (
    ("breaking", "Breaking Changes"),
    ("feat", "Features"),
    ("fix", "Fixes"),
    ("perf", "Performance"),
    ("refactor", "Refactor"),
    ("docs", "Docs"),
    ("chore", "Chore"),
)

CATEGORY_PRIORITY: Mapping[str, int] = {
    "breaking": 6,
    "feat": 5,
    "fix": 4,
    "perf": 4,
    "refactor": 3,
    "docs": 2,
    "chore": 1,
}

CONVENTIONAL_RE = re.compile(r"^(?P<type>[a-zA-Z]+)(?P<scope>\([^)]*\))?(?P<breaking>!)?:")

PLAN_FILE = Path(".github") / "release-plan.json"
PREVIEW_FILE = Path(".github") / "release-preview.md"
PR_BODY_FILE = Path(".github") / "release-pr-body.md"


class ReleaseError(RuntimeError):
    """Custom error used to bubble failures to the CLI exit code."""


@dataclass
class IssueInfo:
    number: int
    title: str
    url: Optional[str]


@dataclass
class ReleaseEntry:
    category: str
    title: str
    details: str
    issue: Optional[IssueInfo]
    files: Sequence[str]
    commits: Sequence[str]

    def render(self, repo: str) -> str:
        files_hint = ", ".join(sorted(set(self.files)))
        files_line = f"  - Files: {files_hint}" if files_hint else ""
        commits_line = "  - Commits: " + ", ".join(
            f"[{sha[:7]}](https://github.com/{repo}/commit/{sha})" for sha in self.commits
        )
        lines = []
        if self.issue:
            issue_reference = f"#{self.issue.number}"
            if self.issue.url:
                issue_reference = f"[{issue_reference}]({self.issue.url})"
            header = f"- {issue_reference} — {self.issue.title.strip()}"
            lines.append(header)
        else:
            header = f"- {self.title.strip()}"
            lines.append(header)
        if self.details:
            lines.append(f"  - {self.details.strip()}")
        if files_line:
            lines.append(files_line)
        if commits_line.strip():
            lines.append(commits_line)
        return "\n".join(lines)


@dataclass
class ReleasePlan:
    version: str
    bump: VersionBump
    release_branch: str
    release_notes_path: Path
    comment_path: Path
    pr_body_path: Path
    should_release: bool
    reason: Optional[str] = None


def _log(message: str) -> None:
    print(message)


def cleanup_plan_artifacts(repo_root: Path) -> None:
    for relative in (PLAN_FILE, PREVIEW_FILE, PR_BODY_FILE):
        path = repo_root / relative
        if path.exists():
            path.unlink()


def _load_json(path: Path) -> MutableMapping[str, object]:
    if not path.exists():
        return {}
    return json.loads(path.read_text(encoding="utf-8"))


def _load_yaml(path: Path) -> MutableMapping[str, object]:
    if not path.exists():
        return {}
    return yaml.safe_load(path.read_text(encoding="utf-8")) or {}


def load_release_configuration(repo_root: Path) -> MutableMapping[str, object]:
    """Load repository wide configuration.

    We support both `.releaserc` (JSON) and `.release-override.yml` (YAML) for
    flexibility. Values from `.release-override.yml` take precedence.
    """

    base_config = _load_json(repo_root / ".releaserc")
    overrides = _load_yaml(repo_root / ".release-override.yml")
    if not isinstance(base_config, MutableMapping):
        base_config = {}
    if not isinstance(overrides, MutableMapping):
        overrides = {}

    config: MutableMapping[str, object] = dict(base_config)
    config.update(overrides)
    return config


def load_pr_labels(event_path: Optional[Path]) -> List[str]:
    if not event_path or not event_path.exists():
        return []
    event = json.loads(event_path.read_text(encoding="utf-8"))
    labels = event.get("pull_request", {}).get("labels", [])
    return [label.get("name", "") for label in labels]


def detect_category(message: str, files: Sequence[str]) -> str:
    normalized = message.strip()
    if "BREAKING CHANGE" in normalized or "BREAKING-CHANGE" in normalized:
        return "breaking"

    match = CONVENTIONAL_RE.match(normalized)
    commit_type = match.group("type").lower() if match else ""
    if match and match.group("breaking"):
        return "breaking"

    if commit_type:
        lowered = commit_type.lower()
        if lowered in {key for key, _ in CATEGORY_ORDER}:
            return lowered

    # Heuristic fallback based on touched files
    lowered_files = [file_path.lower() for file_path in files]
    if any(path.endswith(".proto") or path.startswith("contracts/") for path in lowered_files):
        return "breaking"
    if any(path.startswith("src/") or path.startswith("modules/") for path in lowered_files):
        return "feat"
    if any(path.startswith("docs/") for path in lowered_files):
        return "docs"
    return "chore"


def fetch_issue_details(repo: str, issues: Iterable[int], token: Optional[str]) -> Dict[int, IssueInfo]:
    issue_numbers = sorted(set(issues))
    if not issue_numbers:
        return {}

    session = requests.Session()
    headers = {
        "Accept": "application/vnd.github+json",
        "X-GitHub-Api-Version": "2022-11-28",
    }
    if token:
        headers["Authorization"] = f"Bearer {token}"
    session.headers.update(headers)

    details: Dict[int, IssueInfo] = {}
    for issue_number in issue_numbers:
        response = session.get(
            f"https://api.github.com/repos/{repo}/issues/{issue_number}", timeout=30
        )
        if response.status_code == 404:
            continue
        response.raise_for_status()
        data = response.json()
        details[issue_number] = IssueInfo(
            number=issue_number,
            title=data.get("title", f"Issue #{issue_number}"),
            url=data.get("html_url"),
        )
    return details


def collect_release_entries(
    commits: Sequence[CommitMetadata],
    repo: str,
    token: Optional[str],
) -> List[ReleaseEntry]:
    issues: List[int] = []
    for commit in commits:
        issues.extend(extract_issue_numbers(commit.message))

    issue_details = fetch_issue_details(repo, issues, token)

    entries: List[ReleaseEntry] = []
    entries_by_issue: Dict[int, ReleaseEntry] = {}

    for commit in commits:
        category = detect_category(commit.message, commit.files)
        issue_numbers = extract_issue_numbers(commit.message)
        if issue_numbers:
            for number in issue_numbers:
                info = issue_details.get(number) or IssueInfo(
                    number=number,
                    title=f"Issue #{number}",
                    url=None,
                )
                existing = entries_by_issue.get(number)
                if existing:
                    existing.files = tuple(sorted(set(existing.files + tuple(commit.files))))
                    existing.commits = tuple(sorted(set(existing.commits + (commit.sha,))))
                    # Promote category if the new commit is more impactful
                    if CATEGORY_PRIORITY.get(category, 0) > CATEGORY_PRIORITY.get(
                        existing.category, 0
                    ):
                        existing.category = category
                    continue
                entry = ReleaseEntry(
                    category=category,
                    title=info.title,
                    details=commit.subject,
                    issue=info,
                    files=tuple(sorted(set(commit.files))),
                    commits=(commit.sha,),
                )
                entries.append(entry)
                entries_by_issue[number] = entry
        else:
            description = commit.subject
            file_hint = ", ".join(commit.files[:6])
            if len(commit.files) > 6:
                file_hint += ", …"
            details = f"Impacted files: {file_hint}" if file_hint else ""
            entries.append(
                ReleaseEntry(
                    category=category,
                    title=description,
                    details=details,
                    issue=None,
                    files=tuple(sorted(set(commit.files))),
                    commits=(commit.sha,),
                )
            )

    return entries


def render_release_notes(
    version: str,
    repo: str,
    entries: Sequence[ReleaseEntry],
    *,
    excluded_categories: Sequence[str],
    template_path: Optional[Path],
) -> str:
    excluded = {name.lower() for name in excluded_categories}
    section_blocks: List[str] = []
    for key, title in CATEGORY_ORDER:
        if key in excluded:
            continue
        section_entries = [entry for entry in entries if entry.category == key]
        if not section_entries:
            continue
        section_lines = [f"## {title}"]
        for entry in section_entries:
            section_lines.append(entry.render(repo))
        section_blocks.append("\n".join(section_lines))

    if section_blocks:
        sections_text = "\n\n".join(section_blocks)
    else:
        sections_text = "Nessuna modifica rilevante per questa release."

    default_body = dedent(
        f"""
        # Momentum v{version}

        {sections_text}

        _Generated on {datetime.now(timezone.utc).strftime('%Y-%m-%d %H:%M UTC')}._
        """
    ).strip() + "\n"

    if template_path and template_path.exists():
        template = template_path.read_text(encoding="utf-8")
        rendered = (
            template.replace("{{version}}", version)
            .replace("{{sections}}", sections_text)
            .replace(
                "{{generated_at}}",
                datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M UTC"),
            )
        )
        return rendered.strip() + "\n"

    return default_body


def determine_bump_override(labels: Sequence[str]) -> Optional[VersionBump]:
    normalized = {label.lower() for label in labels}
    if "release:major" in normalized:
        return VersionBump.MAJOR
    if "release:minor" in normalized:
        return VersionBump.MINOR
    if "release:patch" in normalized:
        return VersionBump.PATCH
    return None


def apply_bump_override(
    *,
    current_version: Optional[str],
    commits: Sequence[CommitMetadata],
    default_bump: VersionBump,
    override_bump: Optional[VersionBump],
    override_version: Optional[str],
) -> Tuple[str, VersionBump]:
    if override_version:
        parse_version(override_version)  # validates format
        return override_version, override_bump or default_bump

    if override_bump:
        if current_version:
            base = parse_version(current_version)
        else:
            base = (0, 0, 0)
        next_version_tuple = override_bump.apply(base)
        return format_version(next_version_tuple), override_bump

    return plan_next_version(current_version=current_version, commits=commits)


def compute_release_plan(
    *,
    repo_root: Path,
    repo: str,
    head_ref: str,
    base_ref: Optional[str],
    labels: Sequence[str],
    github_token: Optional[str],
) -> ReleasePlan:
    config = load_release_configuration(repo_root)
    tag_prefix = str(config.get("tagPrefix", "v"))

    latest_tag = get_latest_tag(tag_prefix)
    current_version = (
        latest_tag[len(tag_prefix) :] if latest_tag and latest_tag.startswith(tag_prefix) else None
    )

    commits = collect_commits(latest_tag, head_ref)
    if not commits:
        cleanup_plan_artifacts(repo_root)
        return ReleasePlan(
            version=current_version or "0.0.0",
            bump=VersionBump.NONE,
            release_branch=f"release/{tag_prefix}{current_version}" if current_version else "release/",
            release_notes_path=repo_root / "ReleaseNotes" / "noop.md",
            comment_path=repo_root / PREVIEW_FILE,
            pr_body_path=repo_root / PR_BODY_FILE,
            should_release=False,
            reason="Nessun commit da rilasciare",
        )

    override_file = repo_root / ".release-override.yml"
    override_data = _load_yaml(override_file)
    override_version = override_data.get("version") if isinstance(override_data, dict) else None
    override_bump = override_data.get("bump") if isinstance(override_data, dict) else None

    bump_override = determine_bump_override(labels)
    if isinstance(override_bump, str):
        override_bump = override_bump.lower()
        mapping = {
            "major": VersionBump.MAJOR,
            "minor": VersionBump.MINOR,
            "patch": VersionBump.PATCH,
        }
        override_bump = mapping.get(override_bump)
    else:
        override_bump = None

    next_version, bump = apply_bump_override(
        current_version=current_version,
        commits=commits,
        default_bump=plan_next_version(current_version=current_version, commits=commits)[1],
        override_bump=bump_override or override_bump,
        override_version=override_version,
    )

    entries = collect_release_entries(commits, repo, github_token)

    impactful_categories = {
        entry.category for entry in entries if entry.category not in {"docs", "chore"}
    }
    allow_docs_release = bool(config.get("allowDocsOnlyRelease")) or bool(
        override_data.get("allowDocsOnlyRelease") if isinstance(override_data, dict) else False
    )
    if not impactful_categories and not allow_docs_release:
        cleanup_plan_artifacts(repo_root)
        return ReleasePlan(
            version=next_version,
            bump=VersionBump.NONE,
            release_branch=f"release/{tag_prefix}{next_version}",
            release_notes_path=repo_root / "ReleaseNotes" / f"{next_version}.md",
            comment_path=repo_root / PREVIEW_FILE,
            pr_body_path=repo_root / PR_BODY_FILE,
            should_release=False,
            reason="Solo modifiche di documentazione/chore, rilascio annullato",
        )

    template_path = repo_root / ".github" / "release_notes.hbs"
    excluded_config = config.get("excludeCategories", [])
    if isinstance(excluded_config, str):
        excluded_categories = [excluded_config]
    elif isinstance(excluded_config, Sequence):
        excluded_categories = list(excluded_config)
    else:
        excluded_categories = []
    release_notes = render_release_notes(
        next_version,
        repo,
        entries,
        excluded_categories=excluded_categories,
        template_path=template_path,
    )

    release_dir = repo_root / "ReleaseNotes"
    release_dir.mkdir(parents=True, exist_ok=True)
    release_notes_path = release_dir / f"{next_version}.md"
    release_notes_path.write_text(release_notes, encoding="utf-8")

    preview_content = build_pr_comment(next_version, bump, release_notes)
    preview_path = repo_root / PREVIEW_FILE
    preview_path.parent.mkdir(parents=True, exist_ok=True)
    preview_path.write_text(preview_content, encoding="utf-8")

    pr_body = build_release_pr_body(next_version, bump, entries, repo)
    pr_body_path = repo_root / PR_BODY_FILE
    pr_body_path.write_text(pr_body, encoding="utf-8")

    plan_data = {
        "version": next_version,
        "bump": bump.name.lower(),
        "releaseBranch": f"release/{tag_prefix}{next_version}",
        "releaseNotes": str(release_notes_path.relative_to(repo_root)),
        "generatedAt": datetime.now(timezone.utc).isoformat(),
        "issues": [entry.issue.number for entry in entries if entry.issue],
        "commits": [commit.sha for commit in commits],
        "shouldRelease": True,
    }

    plan_path = repo_root / PLAN_FILE
    plan_path.parent.mkdir(parents=True, exist_ok=True)
    plan_path.write_text(json.dumps(plan_data, indent=2), encoding="utf-8")

    return ReleasePlan(
        version=next_version,
        bump=bump,
        release_branch=f"release/{tag_prefix}{next_version}",
        release_notes_path=release_notes_path,
        comment_path=preview_path,
        pr_body_path=pr_body_path,
        should_release=True,
        reason=None,
    )


def build_pr_comment(version: str, bump: VersionBump, release_notes: str) -> str:
    preview_lines = release_notes.strip().splitlines()
    trimmed_preview = "\n".join(preview_lines[:80])
    checklist = "\n".join(
        [
            "- [ ] Test",
            "- [ ] Lint",
            "- [ ] Build",
            "- [ ] Security scan",
        ]
    )
    comment = dedent(
        f"""
        ## Release plan for v{version}

        Tipo di rilascio previsto: **{bump.name.lower()}**

        ### Anteprima Release Notes
        {trimmed_preview}

        <details>
        <summary>Checklist pre-merge</summary>

        {checklist}
        </details>
        """
    ).strip()
    return comment + "\n"


def build_release_pr_body(
    version: str, bump: VersionBump, entries: Sequence[ReleaseEntry], repo: str
) -> str:
    lines = [f"## Release v{version}", "", f"Tipo di rilascio: **{bump.name.lower()}**", ""]
    lines.append("## Changelog")
    categorized: Dict[str, List[ReleaseEntry]] = {}
    for category, _ in CATEGORY_ORDER:
        categorized[category] = []
    for entry in entries:
        categorized.setdefault(entry.category, []).append(entry)
    for key, title in CATEGORY_ORDER:
        bucket = categorized.get(key) or []
        if not bucket:
            continue
        lines.append(f"### {title}")
        for item in bucket:
            lines.append(item.render(repo))
        lines.append("")
    lines.append("## Checklist")
    lines.extend(["- [ ] Test", "- [ ] Lint", "- [ ] Build", "- [ ] Security scan"])
    return "\n".join(lines).strip() + "\n"


def write_plan_outputs(plan: ReleasePlan, output_path: Path, repo_root: Path) -> None:
    def _relative(path: Path) -> str:
        try:
            return str(path.relative_to(repo_root))
        except ValueError:
            return str(path)

    data = {
        "version": plan.version,
        "bump": plan.bump.name.lower(),
        "release_branch": plan.release_branch,
        "release_notes_path": _relative(plan.release_notes_path),
        "preview_path": _relative(plan.comment_path),
        "pr_body_path": _relative(plan.pr_body_path),
        "should_release": plan.should_release,
        "reason": plan.reason,
    }
    output_path.write_text(json.dumps(data, indent=2), encoding="utf-8")


def cmd_plan(args: argparse.Namespace) -> int:
    repo_root = Path(args.repository_root).resolve()
    labels = load_pr_labels(Path(args.event_path) if args.event_path else None)
    plan = compute_release_plan(
        repo_root=repo_root,
        repo=args.repo,
        head_ref=args.head_ref,
        base_ref=args.base_ref,
        labels=labels,
        github_token=os.getenv("GITHUB_TOKEN"),
    )

    if not plan.should_release:
        _log(f"Nessuna release necessaria: {plan.reason}")

    write_plan_outputs(plan, Path(args.output_json), repo_root)
    return 0


def load_plan(repo_root: Path) -> MutableMapping[str, object]:
    plan_path = repo_root / PLAN_FILE
    if not plan_path.exists():
        raise ReleaseError("Nessun piano di rilascio trovato; eseguire il comando plan prima.")
    return json.loads(plan_path.read_text(encoding="utf-8"))


def cmd_sync(args: argparse.Namespace) -> int:
    repo_root = Path(args.repository_root).resolve()
    plan_path = repo_root / PLAN_FILE
    if not plan_path.exists():
        _log("Nessun piano di rilascio trovato, sincronizzazione non necessaria.")
        return 0

    plan_data = load_plan(repo_root)
    version = str(plan_data.get("version"))
    release_notes_rel = plan_data.get("releaseNotes")
    release_notes_path = repo_root / release_notes_rel

    generated_plan = compute_release_plan(
        repo_root=repo_root,
        repo=args.repo,
        head_ref=args.head_ref,
        base_ref=args.base_ref,
        labels=[],
        github_token=os.getenv("GITHUB_TOKEN"),
    )

    if generated_plan.version != version:
        raise ReleaseError(
            f"La versione calcolata ({generated_plan.version}) non corrisponde a quella prevista ({version})."
        )

    current_content = release_notes_path.read_text(encoding="utf-8") if release_notes_path.exists() else ""
    new_content = generated_plan.release_notes_path.read_text(encoding="utf-8")
    if current_content.strip() != new_content.strip():
        raise ReleaseError("Le release notes sul branch non sono aggiornate rispetto al piano.")

    _log("Il branch di release è sincronizzato con il piano.")
    return 0


def create_tag_and_release(
    *,
    repo_root: Path,
    repo: str,
    version: str,
    release_notes_path: Path,
    draft: bool,
) -> None:
    tag_name = f"v{version}"
    tag_message = f"Momentum {version}"
    existing_tags = run_git(["tag", "--list", tag_name], cwd=repo_root)
    if existing_tags.strip():
        _log(f"Il tag {tag_name} esiste già, salto la creazione del tag.")
    else:
        run_git(["tag", "-a", tag_name, "-m", tag_message], cwd=repo_root)
        run_git(["push", "origin", tag_name], cwd=repo_root)
        _log(f"Creato tag {tag_name}.")

    notes = release_notes_path.read_text(encoding="utf-8")
    token = os.getenv("GITHUB_TOKEN")
    if not token:
        raise ReleaseError("Variabile GITHUB_TOKEN non impostata, impossibile creare la release.")
    session = requests.Session()
    session.headers.update(
        {
            "Accept": "application/vnd.github+json",
            "Authorization": f"Bearer {token}",
            "X-GitHub-Api-Version": "2022-11-28",
        }
    )

    response = session.post(
        f"https://api.github.com/repos/{repo}/releases",
        json={
            "tag_name": tag_name,
            "name": f"Momentum v{version}",
            "body": notes,
            "draft": draft,
        },
        timeout=30,
    )

    if response.status_code == 422 and "already_exists" in response.text:
        _log("La GitHub Release esiste già, non viene ricreata.")
        return
    response.raise_for_status()
    _log("GitHub Release creata con successo.")


def cmd_finalize(args: argparse.Namespace) -> int:
    repo_root = Path(args.repository_root).resolve()
    plan_path = repo_root / PLAN_FILE
    if plan_path.exists():
        plan_data = load_plan(repo_root)
        if plan_data.get("shouldRelease") is False:
            _log("Il piano segnala che non è richiesta alcuna release.")
            cleanup_plan_artifacts(repo_root)
            return 0

        commits = plan_data.get("commits") or []
        if not commits:
            _log("Il piano di rilascio non contiene commit, salto la pubblicazione.")
            cleanup_plan_artifacts(repo_root)
            return 0

        version = str(plan_data.get("version"))
        release_notes_rel = plan_data.get("releaseNotes")
        if not isinstance(release_notes_rel, str):
            raise ReleaseError("Il piano di rilascio non specifica il percorso delle release notes.")

        release_notes_path = repo_root / release_notes_rel
        if not release_notes_path.exists():
            raise ReleaseError(
                f"Il file delle release notes {release_notes_rel} non esiste nella repository."
            )
        cleanup_plan_artifacts(repo_root)
    else:
        version, release_notes_path = resolve_release_from_notes(
            repo_root, args.previous_ref
        )
        if not version or not release_notes_path:
            _log("Nessuna modifica di release rilevata, salto la pubblicazione.")
            return 0

    config = load_release_configuration(repo_root)
    draft = bool(config.get("githubRelease", {}).get("draft", False))
    create_tag_and_release(
        repo_root=repo_root,
        repo=args.repo,
        version=version,
        release_notes_path=release_notes_path,
        draft=draft,
    )
    return 0


def resolve_release_from_notes(repo_root: Path, previous_ref: Optional[str]) -> Tuple[str, Optional[Path]]:
    release_dir = repo_root / "ReleaseNotes"
    if not release_dir.exists():
        return "", None

    candidates: List[Tuple[Tuple[int, int, int], Path]] = []
    for file_path in release_dir.glob("*.md"):
        try:
            version_tuple = parse_version(file_path.stem)
        except ValueError:
            continue
        candidates.append((version_tuple, file_path))

    if not candidates:
        return "", None

    candidates.sort()
    selected_version, selected_path = candidates[-1]

    if previous_ref:
        diff_output = run_git(
            ["diff", "--name-only", f"{previous_ref}..HEAD"], cwd=repo_root
        )
        changed: Set[Path] = {
            Path(line.strip())
            for line in diff_output.splitlines()
            if line.strip()
        }
        if selected_path.relative_to(repo_root) not in changed:
            return "", None

    return format_version(selected_version), selected_path


def parse_arguments(argv: Optional[Sequence[str]] = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    subparsers = parser.add_subparsers(dest="command", required=True)

    plan_parser = subparsers.add_parser("plan", help="Calcola il piano di rilascio per una PR.")
    plan_parser.add_argument("--repo", required=True, help="Repository owner/name.")
    plan_parser.add_argument("--head-ref", required=True, help="SHA del commit HEAD della PR.")
    plan_parser.add_argument("--base-ref", help="SHA del commit base della PR.")
    plan_parser.add_argument("--event-path", help="Percorso al file JSON dell'evento GitHub.")
    plan_parser.add_argument(
        "--repository-root",
        default=Path(__file__).resolve().parents[2],
        type=Path,
        help="Root del repository",
    )
    plan_parser.add_argument(
        "--output-json",
        default="release-plan-output.json",
        help="File in cui salvare gli output del piano.",
    )
    plan_parser.set_defaults(func=cmd_plan)

    sync_parser = subparsers.add_parser(
        "sync", help="Verifica che il branch di release sia allineato con il piano."
    )
    sync_parser.add_argument("--repo", required=True, help="Repository owner/name.")
    sync_parser.add_argument("--head-ref", required=True, help="SHA del branch di release.")
    sync_parser.add_argument("--base-ref", help="SHA di confronto per il calcolo delle note.")
    sync_parser.add_argument(
        "--repository-root",
        default=Path(__file__).resolve().parents[2],
        type=Path,
        help="Root del repository",
    )
    sync_parser.set_defaults(func=cmd_sync)

    finalize_parser = subparsers.add_parser(
        "finalize", help="Crea il tag e la GitHub Release dopo il merge su main."
    )
    finalize_parser.add_argument("--repo", required=True, help="Repository owner/name.")
    finalize_parser.add_argument(
        "--repository-root",
        default=Path(__file__).resolve().parents[2],
        type=Path,
        help="Root del repository",
    )
    finalize_parser.add_argument(
        "--previous-ref",
        help="Commit SHA precedente alla merge commit per identificare i file modificati.",
    )
    finalize_parser.set_defaults(func=cmd_finalize)

    return parser.parse_args(argv)


def main(argv: Optional[Sequence[str]] = None) -> int:
    args = parse_arguments(argv)
    try:
        return args.func(args)
    except ReleaseError as exc:
        print(f"Errore: {exc}")
        return 1


if __name__ == "__main__":
    raise SystemExit(main())


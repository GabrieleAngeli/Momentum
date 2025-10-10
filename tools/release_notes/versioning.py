"""Utility per determinare la prossima versione di release secondo la policy semantica."""

from __future__ import annotations

import enum
import re
import subprocess
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable, List, Optional, Sequence, Tuple


SEMVER_RE = re.compile(r"^(?P<major>0|[1-9]\d*)\.(?P<minor>0|[1-9]\d*)\.(?P<patch>0|[1-9]\d*)$")


class VersionBump(enum.IntEnum):
    """Livelli di incremento supportati per le versioni semantiche."""

    NONE = 0
    PATCH = 1
    MINOR = 2
    MAJOR = 3

    def apply(self, version: Tuple[int, int, int]) -> Tuple[int, int, int]:
        major, minor, patch = version
        if self == VersionBump.MAJOR:
            return major + 1, 0, 0
        if self == VersionBump.MINOR:
            return major, minor + 1, 0
        if self == VersionBump.PATCH:
            return major, minor, patch + 1
        return version

    @classmethod
    def max(cls, first: "VersionBump", second: "VersionBump") -> "VersionBump":
        return first if first >= second else second


@dataclass
class CommitMetadata:
    """Informazioni minime utili per dedurre l'impatto delle modifiche."""

    sha: str
    message: str
    files: Sequence[str]

    @property
    def subject(self) -> str:
        return self.message.splitlines()[0] if self.message else ""


CONVENTIONAL_RE = re.compile(
    r"^(?P<type>[a-zA-Z]+)(?P<scope>\([^)]*\))?(?P<breaking>!)?:"
)


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


def parse_version(raw_version: str) -> Tuple[int, int, int]:
    match = SEMVER_RE.match(raw_version)
    if not match:
        raise ValueError(f"Versione non valida: {raw_version!r}")
    return (
        int(match.group("major")),
        int(match.group("minor")),
        int(match.group("patch")),
    )


def format_version(version: Tuple[int, int, int]) -> str:
    return f"{version[0]}.{version[1]}.{version[2]}"


def detect_bump_from_commit(message: str, files: Sequence[str]) -> VersionBump:
    """Inferisce l'impatto del commit utilizzando convenzioni e file toccati."""

    normalized_message = message.strip()
    if not normalized_message:
        return VersionBump.NONE

    if "BREAKING CHANGE" in normalized_message or "BREAKING-CHANGE" in normalized_message:
        return VersionBump.MAJOR

    match = CONVENTIONAL_RE.match(normalized_message)
    if match:
        if match.group("breaking"):
            return VersionBump.MAJOR
        commit_type = match.group("type").lower()
        if commit_type == "feat":
            return VersionBump.MINOR
        if commit_type in {"fix", "perf", "refactor"}:
            return VersionBump.PATCH

    # In assenza di convenzioni esplicite, si utilizza una stima sui file toccati.
    for file_path in files:
        normalized = file_path.lower()
        if ".proto" in normalized or normalized.startswith("contracts/"):
            return VersionBump.MAJOR
        if normalized.startswith("src/") or normalized.startswith("modules/"):
            # Le modifiche funzionali al codice sono almeno una release minor.
            return VersionBump.MINOR

    if any(file_path.lower().startswith("docs/") for file_path in files):
        return VersionBump.PATCH

    return VersionBump.NONE


def determine_required_bump(commits: Iterable[CommitMetadata]) -> VersionBump:
    bump = VersionBump.NONE
    for commit in commits:
        bump = VersionBump.max(bump, detect_bump_from_commit(commit.message, commit.files))
        if bump == VersionBump.MAJOR:
            break
    if bump == VersionBump.NONE:
        return VersionBump.PATCH
    return bump


def get_latest_tag(prefix: str = "v") -> Optional[str]:
    pattern = f"{prefix}*" if prefix else "*"
    output = run_git(["tag", "--list", pattern, "--sort=-v:refname"])
    tags = [line for line in output.splitlines() if line]
    return tags[0] if tags else None


def collect_commits(base_ref: Optional[str], head_ref: str = "HEAD") -> List[CommitMetadata]:
    if base_ref:
        range_spec = f"{base_ref}..{head_ref}"
    else:
        range_spec = head_ref
    raw_log = run_git(
        [
            "log",
            range_spec,
            "--pretty=format:%H%x01%B%x02",
            "--name-only",
        ]
    )
    commits: List[CommitMetadata] = []
    for chunk in raw_log.split("\x02\n"):
        if not chunk.strip():
            continue
        header, *file_lines = chunk.splitlines()
        sha, message = header.split("\x01", 1)
        files = [line.strip() for line in file_lines if line.strip()]
        commits.append(CommitMetadata(sha=sha, message=message.strip(), files=files))
    commits.reverse()  # ordine cronologico
    return commits


def plan_next_version(
    *,
    current_version: Optional[str],
    commits: Sequence[CommitMetadata],
) -> Tuple[str, VersionBump]:
    if current_version:
        base = parse_version(current_version)
    else:
        base = (0, 0, 0)
    bump = determine_required_bump(commits)
    next_version_tuple = bump.apply(base)
    next_version = format_version(next_version_tuple)
    return next_version, bump


#!/usr/bin/env python3
"""Momentum Codex PR documentation autopilot runner."""
from __future__ import annotations

import argparse
import json
import os
import subprocess
import sys
from datetime import datetime
from pathlib import Path
from typing import Any, Dict, Iterable, List, Tuple

from zoneinfo import ZoneInfo

import requests

REPO_ROOT = Path(__file__).resolve().parents[2]
SYSTEM_PROMPT_PATH = REPO_ROOT / "tools" / "ci" / "prompts" / "momentum_codex_autopilot.system.md"
USER_PROMPT_PATH = REPO_ROOT / "tools" / "ci" / "prompts" / "momentum_codex_autopilot.user.md"
COMMENT_MARKER = "<!-- momentum-doc-autopilot -->"
SUPPORTED_DOC_FILES = [
    Path("README.md"),
    Path("docs/ARCHITECTURE.md"),
    Path("docs/SECURITY.md"),
    Path("docs/TESTING.md"),
    Path("docs/OBSERVABILITY.md"),
    Path("docs/RELEASE_NOTES_TEMPLATE.md"),
    Path("docs/CONTRIBUTING.md"),
    Path(".github/CODEOWNERS"),
    Path(".github/labeler.yml"),
]
DOC_ADR_DIR = Path("docs/ADR")
MAX_CONTEXT_CHARS = 16000
MAX_DIFF_CHARS = 120000
MAX_BODY_CHARS = 6000


def load_event() -> Dict:
    event_path = os.environ.get("GITHUB_EVENT_PATH")
    if not event_path:
        raise RuntimeError("GITHUB_EVENT_PATH is not defined")
    with open(event_path, "r", encoding="utf-8") as handle:
        return json.load(handle)


def run_command(args: List[str], *, cwd: Path | None = None, check: bool = True, capture_output: bool = True) -> Tuple[int, str, str]:
    result = subprocess.run(
        args,
        cwd=str(cwd) if cwd else None,
        check=False,
        capture_output=capture_output,
        text=True,
    )
    if check and result.returncode != 0:
        raise subprocess.CalledProcessError(result.returncode, args, result.stdout, result.stderr)
    return result.returncode, result.stdout, result.stderr


def truncate(text: str, limit: int) -> str:
    if len(text) <= limit:
        return text
    return text[: limit - 3] + "..."


def collect_repository_context(touched_modules: Iterable[str]) -> str:
    sections: List[str] = []
    for relative in SUPPORTED_DOC_FILES:
        path = REPO_ROOT / relative
        if path.exists():
            try:
                content = path.read_text(encoding="utf-8")
            except UnicodeDecodeError:
                continue
            sections.append(f"### {relative}\n" + truncate(content, MAX_CONTEXT_CHARS // 6))
    adr_dir = REPO_ROOT / DOC_ADR_DIR
    if adr_dir.exists():
        adr_files = sorted([p for p in adr_dir.glob("ADR-*.md") if p.is_file()])
        listings: List[str] = []
        for adr_file in adr_files:
            try:
                head = adr_file.read_text(encoding="utf-8").splitlines()[:40]
                listings.append(f"#### {DOC_ADR_DIR / adr_file.name}\n" + "\n".join(head))
            except UnicodeDecodeError:
                continue
        if listings:
            sections.append(f"### {DOC_ADR_DIR}/\n" + "\n\n".join(listings))
    for module in sorted(set(touched_modules)):
        module_readme = REPO_ROOT / "modules" / module / "README.md"
        if module_readme.exists():
            try:
                content = module_readme.read_text(encoding="utf-8")
            except UnicodeDecodeError:
                continue
            sections.append(f"### modules/{module}/README.md\n" + truncate(content, MAX_CONTEXT_CHARS // 6))
    return "\n\n".join(sections)


def get_touched_modules_from_diff(diff_text: str) -> List[str]:
    modules: set[str] = set()
    for line in diff_text.splitlines():
        if line.startswith("+++ b/") or line.startswith("--- a/"):
            path = line[6:]
            if path.startswith("modules/"):
                parts = Path(path).parts
                if len(parts) >= 2:
                    modules.add(parts[1])
    return sorted(modules)


def export_metadata_to_env(pr: Dict) -> None:
    github_env = os.environ.get("GITHUB_ENV")
    if not github_env:
        return
    with open(github_env, "a", encoding="utf-8") as handle:
        handle.write(f"PR_NUMBER={pr['number']}\n")
        handle.write(f"PR_TITLE<<EOF\n{pr['title']}\nEOF\n")
        body = pr.get("body") or ""
        handle.write(f"PR_BODY<<EOF\n{body}\nEOF\n")
        handle.write(f"PR_AUTHOR={pr['user']['login']}\n")
        handle.write(f"PR_HEAD={pr['head']['ref']}\n")
        handle.write(f"PR_BASE={pr['base']['ref']}\n")


def fetch_branches(base_ref: str) -> None:
    run_command(["git", "fetch", "origin", base_ref], cwd=REPO_ROOT)


def build_diff(base_ref: str) -> str:
    _, diff_output, _ = run_command([
        "git",
        "diff",
        f"origin/{base_ref}...HEAD",
    ], cwd=REPO_ROOT)
    return truncate(diff_output, MAX_DIFF_CHARS)


def collect_commit_subjects(max_count: int = 20) -> str:
    _, output, _ = run_command([
        "git",
        "log",
        "--pretty=format:%s",
        "HEAD",
        f"-n{max_count}",
    ], cwd=REPO_ROOT)
    return output.strip()


def load_prompt_template(path: Path) -> str:
    if not path.exists():
        raise FileNotFoundError(f"Prompt template missing: {path}")
    return path.read_text(encoding="utf-8")


def render_user_prompt(template: str, values: Dict[str, str]) -> str:
    prompt = template
    for key, value in values.items():
        prompt = prompt.replace(f"{{{{{key}}}}}", value)
    return prompt


def call_model(system_prompt: str, user_prompt: str) -> Dict:
    provider = os.environ.get("CODEX_PROVIDER", "openai").lower()
    model = os.environ.get("MODEL", "gpt-4.1")
    if provider == "openai":
        api_key = os.environ.get("OPENAI_API_KEY")
        if not api_key:
            raise RuntimeError("OPENAI_API_KEY is required when CODEX_PROVIDER=openai")
        base_url = os.environ.get("OPENAI_BASE_URL")
        from openai import OpenAI

        client_kwargs: Dict[str, Any] = {"api_key": api_key}
        if base_url:
            client_kwargs["base_url"] = base_url
        client = OpenAI(**client_kwargs)
        response = client.chat.completions.create(
            model=model,
            temperature=0.2,
            messages=[
                {"role": "system", "content": system_prompt},
                {"role": "user", "content": user_prompt},
            ],
        )
        content = response.choices[0].message.content
        return parse_json_response(content)
    if provider in {"http", "self_hosted", "compat"}:
        return call_http_compatible_model(model, system_prompt, user_prompt)
    raise RuntimeError(f"Unsupported CODEX_PROVIDER '{provider}'")


def call_http_compatible_model(model: str, system_prompt: str, user_prompt: str) -> Dict:
    base = os.environ.get("CODEX_API_BASE")
    if not base:
        raise RuntimeError(
            "CODEX_API_BASE is required when CODEX_PROVIDER is set to an OpenAI-compatible HTTP mode"
        )
    endpoint = base.rstrip("/")
    if not endpoint.endswith("/chat/completions"):
        endpoint = f"{endpoint}/v1/chat/completions"
    headers = {"Content-Type": "application/json"}
    api_key = os.environ.get("CODEX_API_KEY")
    if api_key:
        headers["Authorization"] = f"Bearer {api_key}"
    timeout = float(os.environ.get("CODEX_HTTP_TIMEOUT", "120"))
    payload = {
        "model": model,
        "temperature": 0.2,
        "messages": [
            {"role": "system", "content": system_prompt},
            {"role": "user", "content": user_prompt},
        ],
    }
    response = requests.post(endpoint, json=payload, headers=headers, timeout=timeout)
    if response.status_code >= 400:
        raise RuntimeError(
            "Model gateway returned error "
            f"{response.status_code}: {truncate(response.text, 1200)}"
        )
    try:
        data = response.json()
    except ValueError as exc:  # noqa: B902
        raise RuntimeError("Model gateway response is not valid JSON") from exc
    choices = data.get("choices")
    if not isinstance(choices, list) or not choices:
        raise RuntimeError("Model gateway response missing choices array")
    message = choices[0].get("message") if isinstance(choices[0], dict) else None
    if not isinstance(message, dict) or "content" not in message:
        raise RuntimeError("Model gateway response missing message content")
    return parse_json_response(message["content"])


def parse_json_response(content: str) -> Dict:
    cleaned = content.strip()
    if cleaned.startswith("```"):
        cleaned = "\n".join(cleaned.splitlines()[1:-1]).strip()
    try:
        data = json.loads(cleaned)
    except json.JSONDecodeError as exc:
        raise ValueError(f"Model response is not valid JSON: {exc}\n{cleaned}") from exc
    required_keys = {
        "pr_summary",
        "change_types",
        "affected_modules",
        "breaking_changes",
        "migrations",
        "semver_suggestion",
        "security_notes",
        "testing_updates",
        "observability_updates",
        "labels",
        "reviewers",
        "checklist",
        "changelog_entry",
        "adr",
        "doc_patches",
    }
    missing = required_keys - set(data.keys())
    if missing:
        raise ValueError(f"Model response missing keys: {sorted(missing)}")
    if not isinstance(data.get("doc_patches"), list):
        raise ValueError("doc_patches must be an array")
    return data


def apply_patch(patch_text: str) -> None:
    if not patch_text.strip():
        return
    process = subprocess.run(
        ["git", "apply", "-p0", "--whitespace=fix"],
        input=patch_text.encode("utf-8"),
        cwd=REPO_ROOT,
        capture_output=True,
    )
    if process.returncode != 0:
        raise RuntimeError(
            f"Failed to apply patch: {process.stderr.decode('utf-8')}\nPatch snippet:\n{patch_text[:500]}"
        )


def ensure_changelog_entry(entry: str) -> None:
    entry = entry.strip()
    if not entry:
        return
    changelog_path = REPO_ROOT / "CHANGELOG.md"
    today = datetime.now(ZoneInfo("Europe/Rome")).date().isoformat()
    if changelog_path.exists():
        content = changelog_path.read_text(encoding="utf-8")
    else:
        content = (
            "# Changelog\n\n"
            "All notable changes to this project will be documented in this file.\n\n"
            "The format is based on Keep a Changelog and this project adheres to Semantic Versioning.\n\n"
            "## [Unreleased]\n\n"
        )
    lines = content.splitlines()
    if "## [Unreleased]" not in lines:
        lines = [
            "# Changelog",
            "",
            "All notable changes to this project will be documented in this file.",
            "",
            "The format is based on Keep a Changelog and this project adheres to Semantic Versioning.",
            "",
            "## [Unreleased]",
            "",
        ] + lines
    index = lines.index("## [Unreleased]")
    insert_at = index + 1
    while insert_at < len(lines) and not lines[insert_at].startswith("## ["):
        insert_at += 1
    block = ["", f"## [{today}]", entry, ""]
    new_lines = lines[:insert_at] + block + lines[insert_at:]
    changelog_path.write_text("\n".join(new_lines).strip() + "\n", encoding="utf-8")


def apply_doc_updates(doc_patches: List[Dict], adr_payload: Dict, changelog_entry: str) -> None:
    changelog_touched = False
    for patch in doc_patches:
        patch_text = patch.get("patch", "")
        apply_patch(patch_text)
        if patch.get("path") == "CHANGELOG.md" or "CHANGELOG.md" in patch_text:
            changelog_touched = True
    adr_patch = (adr_payload or {}).get("patch", "")
    if adr_patch.strip():
        apply_patch(adr_patch)
    if changelog_entry.strip() and not changelog_touched:
        ensure_changelog_entry(changelog_entry)


def format_comment(data: Dict) -> str:
    checklist_lines = [f"- [{ 'x' if item.get('status') == 'done' else ' ' }] {item.get('item')} ({item.get('status')})" for item in data.get("checklist", [])]
    checklist_block = "\n".join(checklist_lines) if checklist_lines else "- [ ] No follow-up actions identified"
    labels = ", ".join(data.get("labels", [])) or "(none)"
    reviewers = ", ".join(data.get("reviewers", [])) or "(none)"
    change_types = ", ".join(data.get("change_types", [])) or "(unspecified)"
    affected_modules = ", ".join(data.get("affected_modules", [])) or "(none)"
    summary = data.get("pr_summary", "")
    breaking = data.get("breaking_changes", "")
    migrations = data.get("migrations", "")
    semver = data.get("semver_suggestion", "")
    security = data.get("security_notes", "")
    testing = data.get("testing_updates", "")
    observability = data.get("observability_updates", "")

    lines = [
        COMMENT_MARKER,
        "### Momentum Codex Doc Autopilot",
        f"**Summary:** {summary}",
        f"**Change types:** {change_types}",
        f"**Affected modules:** {affected_modules}",
        f"**Breaking changes:** {breaking or 'None reported'}",
        f"**Migrations:** {migrations or 'None'}",
        f"**SemVer suggestion:** {semver or 'patch'}",
        f"**Security notes:** {security or 'None'}",
        f"**Testing updates:** {testing or 'None'}",
        f"**Observability updates:** {observability or 'None'}",
        f"**Labels:** {labels}",
        f"**Reviewers:** {reviewers}",
        "**Checklist:**",
        checklist_block,
    ]
    return "\n\n".join(lines)


def post_comment(pr: Dict, body: str) -> None:
    token = os.environ.get("GITHUB_TOKEN")
    if not token:
        print("GITHUB_TOKEN not provided; skipping PR comment", file=sys.stderr)
        return
    repo = os.environ.get("GITHUB_REPOSITORY")
    if not repo:
        raise RuntimeError("GITHUB_REPOSITORY env var missing")
    url = f"https://api.github.com/repos/{repo}/issues/{pr['number']}/comments"
    existing = fetch_existing_comments(pr)
    if existing:
        comment_id = existing
        update_url = f"https://api.github.com/repos/{repo}/issues/comments/{comment_id}"
        run_command([
            "curl",
            "-sS",
            "-X",
            "PATCH",
            update_url,
            "-H",
            f"Authorization: Bearer {token}",
            "-H",
            "Accept: application/vnd.github+json",
            "-d",
            json.dumps({"body": body}),
        ], check=True)
    else:
        run_command([
            "curl",
            "-sS",
            "-X",
            "POST",
            url,
            "-H",
            f"Authorization: Bearer {token}",
            "-H",
            "Accept: application/vnd.github+json",
            "-d",
            json.dumps({"body": body}),
        ], check=True)


def fetch_existing_comments(pr: Dict) -> str | None:
    token = os.environ.get("GITHUB_TOKEN")
    repo = os.environ.get("GITHUB_REPOSITORY")
    if not token or not repo:
        return None
    comments_url = f"https://api.github.com/repos/{repo}/issues/{pr['number']}/comments"
    _, stdout, _ = run_command([
        "curl",
        "-sS",
        "-H",
        f"Authorization: Bearer {token}",
        "-H",
        "Accept: application/vnd.github+json",
        comments_url,
    ])
    try:
        comments = json.loads(stdout)
    except json.JSONDecodeError:
        return None
    for comment in comments:
        if isinstance(comment, dict) and COMMENT_MARKER in comment.get("body", ""):
            return str(comment.get("id"))
    return None


def main() -> None:
    parser = argparse.ArgumentParser(description="Momentum Codex doc autopilot")
    parser.add_argument("--export-env", action="store_true", help="Export PR metadata to environment")
    args = parser.parse_args()

    event = load_event()
    pr = event.get("pull_request")
    if not pr:
        raise RuntimeError("This workflow must be triggered by a pull_request event")

    if args.export_env:
        export_metadata_to_env(pr)
        return

    head_ref = pr["head"]["ref"]
    base_ref = pr["base"]["ref"]

    fetch_branches(base_ref)
    diff_text = build_diff(base_ref)
    commit_subjects = collect_commit_subjects()
    touched_modules = get_touched_modules_from_diff(diff_text)
    repository_context = collect_repository_context(touched_modules)

    pr_body = pr.get("body") or ""
    user_prompt = render_user_prompt(
        load_prompt_template(USER_PROMPT_PATH),
        {
            "PR_NUMBER": str(pr["number"]),
            "PR_TITLE": pr["title"],
            "PR_AUTHOR": pr["user"]["login"],
            "BASE_BRANCH": base_ref,
            "HEAD_BRANCH": head_ref,
            "PR_HTML_URL": pr["html_url"],
            "PR_BODY": truncate(pr_body, MAX_BODY_CHARS),
            "COMMIT_SUBJECTS": commit_subjects,
            "UNIFIED_DIFF": diff_text,
            "REPOSITORY_CONTEXT": truncate(repository_context, MAX_CONTEXT_CHARS),
        },
    )

    system_prompt = load_prompt_template(SYSTEM_PROMPT_PATH)
    model_output = call_model(system_prompt, user_prompt)

    apply_doc_updates(model_output.get("doc_patches", []), model_output.get("adr", {}), model_output.get("changelog_entry", ""))

    comment_body = format_comment(model_output)
    post_comment(pr, comment_body)

    print("Doc autopilot completed successfully")


if __name__ == "__main__":
    try:
        main()
    except Exception as exc:  # noqa: BLE001
        print(f"::error::{exc}", file=sys.stderr)
        raise

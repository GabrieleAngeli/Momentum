#!/usr/bin/env python3
"""Momentum Codex PR documentation autopilot runner.

Genera patch SOLO per file di documentazione (README, docs/**, CHANGELOG, .github/**, modules/*/README.md, docs/ADR/**),
deducendo gli aggiornamenti necessari dai commit e dal diff del PR. Applica le patch in modo sicuro (skip su errori) e
commenta il PR con un riassunto strutturato.
"""
from __future__ import annotations

import argparse
import fnmatch
import json
import os
import re
import subprocess
import sys
import time
from datetime import datetime
from pathlib import Path
from typing import Any, Dict, Iterable, List, Tuple
from zoneinfo import ZoneInfo

import requests

# --------------------------------------------------------------------------------------
# Costanti / limiti (configurabili via env)
# --------------------------------------------------------------------------------------

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

# limiti default; possono essere ridotti via env per evitare 429/TPM
MAX_CONTEXT_CHARS = int(os.environ.get("MAX_CONTEXT_CHARS", "16000"))
MAX_DIFF_CHARS = int(os.environ.get("MAX_DIFF_CHARS", "120000"))
MAX_BODY_CHARS = int(os.environ.get("MAX_BODY_CHARS", "6000"))

# Pattern ammessi per patch di documentazione (tutto il resto viene scartato)
ALLOWED_GLOBS = [
    "README.md",
    "CHANGELOG.md",
    "docs/**",
    ".github/**",
    "modules/*/README.md",
    f"{DOC_ADR_DIR}/**",
]

# --------------------------------------------------------------------------------------
# Utils
# --------------------------------------------------------------------------------------

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


def shrink_prompt(user_prompt: str, factor: float) -> str:
    """Ritaglia il prompt per ridurre i token mantenendo struttura e contesto minimo."""
    keep = max(1200, int(len(user_prompt) * factor))
    if len(user_prompt) <= keep:
        return user_prompt
    return user_prompt[:keep] + "\n\n[...prompt truncated to reduce tokens...]\n"


def load_prompt_template(path: Path) -> str:
    if not path.exists():
        raise FileNotFoundError(f"Prompt template missing: {path}")
    return path.read_text(encoding="utf-8")


def render_user_prompt(template: str, values: Dict[str, str]) -> str:
    # rinforzo: limitiamo il modello a DOC ONLY
    guard = (
        "\n\nIMPORTANT: Generate ONLY documentation changes. "
        "Only produce unified-diff patches that touch these paths: "
        "`README.md`, `docs/**`, `.github/**`, `modules/*/README.md`, `docs/ADR/**`, `CHANGELOG.md`. "
        "Do NOT modify or reference code files (e.g., `src/**`, `*.cs`, `*.ts`, etc.).\n"
    )
    template = template + guard
    prompt = template
    for key, value in values.items():
        prompt = prompt.replace(f"{{{{{key}}}}}", value)
    return prompt

# --------------------------------------------------------------------------------------
# GitHub event / REST helpers
# --------------------------------------------------------------------------------------

def load_event() -> Dict | None:
    event_path = os.environ.get("GITHUB_EVENT_PATH")
    if not event_path or not os.path.isfile(event_path):
        return None
    with open(event_path, "r", encoding="utf-8") as handle:
        return json.load(handle)


def gh_get(url: str, token: str, *, params: Dict[str, Any] | None = None, max_wait: int = 300) -> Dict:
    headers = {
        "Authorization": f"Bearer {token}",
        "Accept": "application/vnd.github+json",
        "X-GitHub-Api-Version": "2022-11-28",
        "User-Agent": "doc-autopilot-ci",
    }
    while True:
        r = requests.get(url, headers=headers, params=params, timeout=60)
        if r.status_code == 403 and "rate limit" in r.text.lower():
            reset = int(r.headers.get("x-ratelimit-reset", "0"))
            now = int(time.time())
            wait = max(5, min(max_wait, reset - now + 2)) if reset > now else 60
            print(f"[doc-autopilot] Rate limit hit. Sleeping {wait}s…", file=sys.stderr)
            time.sleep(wait)
            continue
        r.raise_for_status()
        return r.json()


def gh_post_or_patch(url: str, token: str, payload: Dict[str, Any], method: str = "POST") -> Dict:
    headers = {
        "Authorization": f"Bearer {token}",
        "Accept": "application/vnd.github+json",
        "X-GitHub-Api-Version": "2022-11-28",
        "User-Agent": "doc-autopilot-ci",
    }
    func = requests.post if method.upper() == "POST" else requests.patch
    r = func(url, headers=headers, json=payload, timeout=60)
    if r.status_code >= 400:
        raise RuntimeError(f"GitHub API error {r.status_code}: {truncate(r.text, 800)}")
    return r.json()


def load_pr_from_event_or_api(args) -> Dict:
    # 1) evento GitHub
    event = load_event()
    if event and "pull_request" in event:
        return event["pull_request"]

    # 2) via REST
    repo = args.repo or os.environ.get("REPO") or os.environ.get("GITHUB_REPOSITORY")
    pr_num = args.pr or os.environ.get("PR_NUMBER") or os.environ.get("GITHUB_PR_NUMBER")
    token = args.token or os.environ.get("GITHUB_TOKEN")
    if not repo or not pr_num or not token:
        raise RuntimeError("Missing inputs: need repo, pr number and GITHUB_TOKEN (via args or env).")
    pr_num = int(pr_num)
    url = f"https://api.github.com/repos/{repo}/pulls/{pr_num}"
    return gh_get(url, token)

# --------------------------------------------------------------------------------------
# Repository context / diff
# --------------------------------------------------------------------------------------

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


def fetch_branches(base_ref: str) -> None:
    run_command(["git", "fetch", "origin", base_ref], cwd=REPO_ROOT)


def build_diff(base_ref: str) -> str:
    _, diff_output, _ = run_command(["git", "diff", f"origin/{base_ref}...HEAD"], cwd=REPO_ROOT)
    diff_output = truncate(diff_output, MAX_DIFF_CHARS)
    if len(diff_output) >= MAX_DIFF_CHARS:
        diff_output += "\n\n[Note: unified diff truncated]\n"
    return diff_output


def collect_commit_subjects(max_count: int = 10) -> str:
    # limitiamo a 10 per contenere i token
    _, output, _ = run_command(["git", "log", "--pretty=format:%s", "HEAD", f"-n{max_count}"], cwd=REPO_ROOT)
    return output.strip()

# --------------------------------------------------------------------------------------
# Model calls + JSON handling (robust)
# --------------------------------------------------------------------------------------

def _normalize_result(data: Dict[str, Any]) -> Dict[str, Any]:
    """Assicura tutte le chiavi e i tipi attesi; mai eccezioni per campi mancanti."""
    defaults: Dict[str, Any] = {
        "pr_summary": "",
        "change_types": [],
        "affected_modules": [],
        "breaking_changes": "",
        "migrations": "",
        "semver_suggestion": "patch",
        "security_notes": "",
        "testing_updates": "",
        "observability_updates": "",
        "labels": [],
        "reviewers": [],
        "checklist": [],
        "changelog_entry": "",
        "adr": {},
        "doc_patches": [],
    }
    out = {**defaults, **(data or {})}

    # Coercizioni leggere
    for k in ("change_types", "affected_modules", "labels", "reviewers", "checklist", "doc_patches"):
        if not isinstance(out.get(k), list):
            out[k] = []
    if not isinstance(out.get("adr"), dict):
        out["adr"] = {}

    return out


def _parse_json_or_raise(content: str) -> Dict[str, Any]:
    cleaned = (content or "").strip()
    if cleaned.startswith("```"):
        cleaned = "\n".join(cleaned.splitlines()[1:-1]).strip()
    return json.loads(cleaned)


def call_model(system_prompt: str, user_prompt: str) -> Dict:
    provider = os.environ.get("CODEX_PROVIDER", "openai").lower()
    model = os.environ.get("MODEL", "gpt-4o-mini")  # default più “elastico”

    def _success_empty() -> Dict:
        # Output “valido ma vuoto” per non bloccare il job
        return {
            "pr_summary": "",
            "change_types": [],
            "affected_modules": [],
            "breaking_changes": "",
            "migrations": "",
            "semver_suggestion": "patch",
            "security_notes": "",
            "testing_updates": "",
            "observability_updates": "",
            "labels": [],
            "reviewers": [],
            "checklist": [],
            "changelog_entry": "",
            "adr": {},
            "doc_patches": [],
        }

    if provider == "openai":
        api_key = os.environ.get("OPENAI_API_KEY")
        if not api_key:
            raise RuntimeError("OPENAI_API_KEY is required when CODEX_PROVIDER=openai")
        try:
            from openai import OpenAI  # SDK v2+
        except ImportError as e:
            raise RuntimeError("Python package 'openai' is not installed. Add `openai>=1.0.0` to tools/ci/requirements.txt.") from e

        client_kwargs: Dict[str, Any] = {"api_key": api_key}
        base_url = os.environ.get("OPENAI_BASE_URL")
        if base_url:
            client_kwargs["base_url"] = base_url
        client = OpenAI(**client_kwargs)

        prompt = user_prompt
        # fino a 3 tentativi: shrink prompt su rate/size, 1 retry di "riparazione JSON"
        for attempt in range(3):
            try:
                resp = client.chat.completions.create(
                    model=model,
                    temperature=float(os.environ.get("TEMPERATURE", "0.2")),
                    max_tokens=int(os.environ.get("MAX_BODY_CHARS", "2000")),
                    response_format={"type": "json_object"},  # <— forza JSON valido
                    messages=[
                        {"role": "system", "content": system_prompt},
                        {"role": "user", "content": prompt},
                    ],
                )
                content = (resp.choices[0].message.content or "").strip()
                try:
                    data = _parse_json_or_raise(content)
                    return _normalize_result(data)
                except json.JSONDecodeError:
                    # Retry di riparazione: chiedi SOLO JSON minificato
                    repair = client.chat.completions.create(
                        model=model,
                        temperature=0.0,
                        max_tokens=int(os.environ.get("MAX_BODY_CHARS", "2000")),
                        response_format={"type": "json_object"},
                        messages=[
                            {"role": "system", "content": "You MUST return a single valid JSON object. No markdown, no comments."},
                            {"role": "user", "content": f"Fix and return a valid JSON object from this text (if needed, complete missing brackets/commas):\n{content}"},
                        ],
                    )
                    fixed = (repair.choices[0].message.content or "").strip()
                    data = _parse_json_or_raise(fixed)
                    return _normalize_result(data)
            except Exception as e:
                msg = str(e).lower()
                if "rate limit" in msg or "request too large" in msg or "tpm" in msg:
                    factor = 0.6 if attempt == 0 else 0.4
                    prompt = shrink_prompt(prompt, factor)
                    time.sleep(2 + attempt * 2)
                    continue
                # altri errori → non bloccare l'intero job, ritorna payload vuoto
                print(f"[doc-autopilot] LLM error: {e}", file=sys.stderr)
                return _success_empty()

        print("[doc-autopilot] Model call failed due to limits after retries. Skipping doc generation.", file=sys.stderr)
        return _success_empty()

    if provider in {"http", "self_hosted", "compat"}:
        try:
            return call_http_compatible_model(model, system_prompt, user_prompt)
        except Exception as e:
            msg = str(e).lower()
            if "rate limit" in msg or "too large" in msg:
                return call_http_compatible_model(model, system_prompt, shrink_prompt(user_prompt, 0.5))
            print(f"[doc-autopilot] HTTP-compatible model error: {e}", file=sys.stderr)
            return _success_empty()

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
        # Molti gateway già supportano response_format: json_object;
        # se il tuo lo supporta, puoi aggiungere:
        # "response_format": {"type": "json_object"},
    }
    response = requests.post(endpoint, json=payload, headers=headers, timeout=timeout)
    if response.status_code >= 400:
        raise RuntimeError(
            "Model gateway returned error "
            f"{response.status_code}: {truncate(response.text, 1200)}"
        )
    try:
        data = response.json()
    except ValueError as exc:
        raise RuntimeError("Model gateway response is not valid JSON") from exc
    choices = data.get("choices")
    if not isinstance(choices, list) or not choices:
        raise RuntimeError("Model gateway response missing choices array")
    message = choices[0].get("message") if isinstance(choices[0], dict) else None
    if not isinstance(message, dict) or "content" not in message:
        raise RuntimeError("Model gateway response missing message content")

    # Prova a parse-are e normalizzare
    parsed = _parse_json_or_raise(message["content"])
    return _normalize_result(parsed)

# --------------------------------------------------------------------------------------
# Patch / changelog helpers
# --------------------------------------------------------------------------------------

HUNK_HEADER_RE = re.compile(r"^@@ -\d+(?:,\d+)? \+\d+(?:,\d+)? @@")
PATCH_DIFF_HEADER = re.compile(r"^diff --git a/(.+) b/(.+)$")


def validate_unified_diff(patch_text: str) -> None:
    lines = [line for line in patch_text.splitlines() if line.strip()]
    if not lines:
        raise ValueError("Doc autopilot produced an empty patch")
    if not any(line.startswith("diff --git ") for line in lines):
        raise ValueError("Doc autopilot patch is missing the 'diff --git' header")
    invalid_headers = [line for line in lines if line.startswith("@@") and not HUNK_HEADER_RE.match(line)]
    if invalid_headers:
        sample = "\n".join(invalid_headers[:3])
        raise ValueError(f"Invalid unified diff hunk header(s):\n{sample}")


def extract_paths_from_patch(patch_text: str) -> list[str]:
    paths: list[str] = []
    for line in patch_text.splitlines():
        m = PATCH_DIFF_HEADER.match(line)
        if m:
            paths.append(m.group(2))  # path del nuovo file (b/)
    return paths


def is_allowed_doc_path(path: str) -> bool:
    norm = path.strip().lstrip("./")
    for pattern in ALLOWED_GLOBS:
        if fnmatch.fnmatch(norm, pattern):
            return True
    return False


def apply_patch(patch_text: str) -> None:
    if not patch_text.strip():
        return
    try:
        validate_unified_diff(patch_text)
    except ValueError as exc:
        print(f"[doc-autopilot] Invalid patch, skipping: {exc}", file=sys.stderr)
        return
    process = subprocess.run(
        ["git", "apply", "-p0", "--whitespace=fix"],
        input=patch_text.encode("utf-8"),
        cwd=REPO_ROOT,
        capture_output=True,
    )
    if process.returncode != 0:
        stderr = process.stderr.decode("utf-8", errors="replace")
        print(f"[doc-autopilot] Failed to apply patch, skipping:\n{stderr}", file=sys.stderr)
        return


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

    for patch in doc_patches or []:
        patch_text = (patch or {}).get("patch", "") or ""
        if not patch_text.strip():
            continue

        # Filtra per path consentiti
        paths = extract_paths_from_patch(patch_text)
        if not paths:
            print("[doc-autopilot] Skip patch: no diff headers found", file=sys.stderr)
            continue
        if not all(is_allowed_doc_path(p) for p in paths):
            print(f"[doc-autopilot] Skip patch touching non-doc files: {paths}", file=sys.stderr)
            continue

        # Verifica esistenza target (consenti nuovi file in docs/** e docs/ADR/**)
        missing = []
        for p in paths:
            tgt = REPO_ROOT / p
            if not tgt.exists() and not (p.startswith("docs/") or p.startswith(str(DOC_ADR_DIR))):
                missing.append(p)
        if missing:
            print(f"[doc-autopilot] Skip patch: target files not found {missing}", file=sys.stderr)
            continue

        apply_patch(patch_text)
        if any(p.endswith("CHANGELOG.md") for p in paths) or "CHANGELOG.md" in patch_text:
            changelog_touched = True

    # ADR proposta dal modello
    adr_patch = (adr_payload or {}).get("patch", "") or ""
    if adr_patch.strip():
        adr_paths = extract_paths_from_patch(adr_patch)
        if adr_paths and all(is_allowed_doc_path(p) for p in adr_paths):
            apply_patch(adr_patch)
        else:
            print(f"[doc-autopilot] Skip ADR patch (paths not allowed): {adr_paths}", file=sys.stderr)

    # Se non abbiamo toccato il CHANGELOG via patch ma c'è una voce, aggiungila in append
    if changelog_entry.strip() and not changelog_touched:
        ensure_changelog_entry(changelog_entry)

# --------------------------------------------------------------------------------------
# PR comment
# --------------------------------------------------------------------------------------

def format_comment(data: Dict) -> str:
    checklist_lines = [
        f"- [{'x' if (item.get('status') == 'done' or item.get('done') is True) else ' '}] {item.get('item')} ({item.get('status', 'todo')})"
        for item in data.get("checklist", [])
        if isinstance(item, dict)
    ]
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
        f"**Breaking changes:** {breaking or 'None'}",
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


def fetch_existing_comment_id(pr: Dict, token: str, repo: str) -> str | None:
    url = f"https://api.github.com/repos/{repo}/issues/{pr['number']}/comments"
    comments = gh_get(url, token)
    if isinstance(comments, list):
        for c in comments:
            if isinstance(c, dict) and COMMENT_MARKER in (c.get("body") or ""):
                return str(c.get("id"))
    return None


def post_comment(pr: Dict, body: str) -> None:
    token = os.environ.get("GITHUB_TOKEN")
    if not token:
        print("GITHUB_TOKEN not provided; skipping PR comment", file=sys.stderr)
        return
    repo = os.environ.get("GITHUB_REPOSITORY")
    if not repo:
        raise RuntimeError("GITHUB_REPOSITORY env var missing")
    existing_id = fetch_existing_comment_id(pr, token, repo)
    if existing_id:
        url = f"https://api.github.com/repos/{repo}/issues/comments/{existing_id}"
        gh_post_or_patch(url, token, {"body": body}, method="PATCH")
    else:
        url = f"https://api.github.com/repos/{repo}/issues/{pr['number']}/comments"
        gh_post_or_patch(url, token, {"body": body}, method="POST")

# --------------------------------------------------------------------------------------
# Main
# --------------------------------------------------------------------------------------

def main() -> None:
    parser = argparse.ArgumentParser(description="Momentum Codex doc autopilot")
    parser.add_argument("--export-env", action="store_true", help="Export PR metadata to environment")
    parser.add_argument("--pr", type=int, default=None, help="PR number (fallback: PR_NUMBER/GITHUB_PR_NUMBER)")
    parser.add_argument("--repo", type=str, default=None, help="owner/repo (fallback: REPO/GITHUB_REPOSITORY)")
    parser.add_argument("--token", type=str, default=None, help="GitHub token (fallback: GITHUB_TOKEN)")
    args = parser.parse_args()

    pr = load_pr_from_event_or_api(args)

    if args.export_env:
        github_env = os.environ.get("GITHUB_ENV")
        if github_env:
            with open(github_env, "a", encoding="utf-8") as handle:
                handle.write(f"PR_NUMBER={pr['number']}\n")
                handle.write(f"PR_TITLE<<EOF\n{pr['title']}\nEOF\n")
                body = pr.get("body") or ""
                handle.write(f"PR_BODY<<EOF\n{body}\nEOF\n")
                handle.write(f"PR_AUTHOR={pr['user']['login']}\n")
                handle.write(f"PR_HEAD={pr['head']['ref']}\n")
                handle.write(f"PR_BASE={pr['base']['ref']}\n")
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

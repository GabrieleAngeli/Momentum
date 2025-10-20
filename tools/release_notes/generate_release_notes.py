#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
Generate Release Notes (optionally LLM-enriched)

Compatibile con:
- nuova CLI: --range/--since-tag/--from --to -o/--output --title
- tua CLI esistente: --base --head --version --out

Esempi:
  python tools/release_notes/generate_release_notes.py --base origin/main --head HEAD --version v1.2.3 --out ReleaseNotes/v1.2.3.md
  python tools/release_notes/generate_release_notes.py --range origin/main..HEAD -o release-notes.md --title "Release Notes"
"""

from __future__ import annotations

import argparse
import datetime as dt
import json
import os
import re
import subprocess
import sys
from typing import Dict, List, Optional, Tuple

# -----------------------
# Utilità di sistema
# -----------------------

def _run(cmd: List[str], cwd: Optional[str] = None, check: bool = True) -> str:
    proc = subprocess.run(cmd, cwd=cwd, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
    if check and proc.returncode != 0:
        raise RuntimeError(f"Command failed: {' '.join(cmd)}\nSTDERR:\n{proc.stderr}")
    return proc.stdout.strip()

def _now_utc_iso() -> str:
    return dt.datetime.utcnow().replace(microsecond=0).isoformat() + "Z"

# -----------------------
# Git helpers
# -----------------------

def _get_last_tag() -> Optional[str]:
    try:
        return _run(["git", "describe", "--tags", "--abbrev=0"])
    except Exception:
        return None

def _resolve_range(args) -> str:
    """
    Ordine di precedenza:
      1) --range
      2) (--base + --head)
      3) (--from_ref + --to_ref)
      4) --since-tag
      5) <last_tag>..HEAD
      6) origin/main..HEAD
    """
    if args.range:
        return args.range
    if args.base or args.head:
        base = args.base or "HEAD~1000"
        head = args.head or "HEAD"
        return f"{base}..{head}"
    if args.from_ref or args.to_ref:
        fr = args.from_ref or "HEAD~1000"
        to = args.to_ref or "HEAD"
        return f"{fr}..{to}"
    if args.since_tag:
        return f"{args.since_tag}..HEAD"
    last_tag = _get_last_tag()
    if last_tag:
        return f"{last_tag}..HEAD"
    return "origin/main..HEAD"

def _git_commits(commit_range: str) -> List[Dict[str, str]]:
    fmt = "%H%x1f%an%x1f%ad%x1f%s%x1f%b%x1e"
    out = _run(["git", "log", commit_range, f"--pretty=format:{fmt}", "--date=short"], check=True)
    commits = []
    for rec in out.split("\x1e"):
        rec = rec.strip()
        if not rec:
            continue
        parts = rec.split("\x1f")
        if len(parts) < 5:
            continue
        h, author, date, subject, body = parts[:5]
        commits.append({
            "hash": h,
            "author": author,
            "date": date,
            "subject": subject.strip(),
            "body": (body or "").strip(),
        })
    return commits

# -----------------------
# Conventional Commits parsing
# -----------------------

_CONV_RE = re.compile(
    r"^(?P<type>build|chore|ci|docs|feat|fix|perf|refactor|revert|style|test)"
    r"(?:\((?P<scope>[^)]+)\))?!?:\s*(?P<subject>.+)"
)

def _parse_conv(msg: str) -> Dict[str, Optional[str]]:
    head = msg.splitlines()[0].strip()
    m = _CONV_RE.match(head)
    if m:
        t = m.group("type")
        scope = m.group("scope")
        subject = m.group("subject").strip()
        breaking = "!" in head
        return {"type": t, "scope": scope, "subject": subject, "breaking": "yes" if breaking else None}
    return {"type": "other", "scope": None, "subject": head, "breaking": None}

# -----------------------
# PR context (facoltativo)
# -----------------------

def _load_pr_context() -> Dict[str, str]:
    path = os.environ.get("GITHUB_EVENT_PATH")
    if not path or not os.path.exists(path):
        return {}
    try:
        with open(path, "r", encoding="utf-8") as f:
            ev = json.load(f)
        pr = ev.get("pull_request") or {}
        return {
            "pr_title": pr.get("title", "") or "",
            "pr_body": pr.get("body", "") or "",
            "pr_number": str(pr.get("number", "")) or "",
            "pr_user": (pr.get("user") or {}).get("login", "") or "",
            "pr_html_url": pr.get("html_url", "") or "",
        }
    except Exception:
        return {}

# -----------------------
# Rendering
# -----------------------

SECTION_ORDER = ["feat", "fix", "perf", "refactor", "docs", "test", "build", "ci", "style", "chore", "revert", "other"]
SECTION_TITLES = {
    "feat": "✨ Features",
    "fix": "🐞 Fix",
    "perf": "⚡ Performance",
    "refactor": "🧠 Refactor",
    "docs": "📝 Docs",
    "test": "🧪 Test",
    "build": "🏗️ Build",
    "ci": "🤖 CI",
    "style": "🎨 Style",
    "chore": "🧹 Chore",
    "revert": "⏪ Revert",
    "other": "🔧 Other",
}

def _format_commit_line(c: Dict[str, str], parsed: Dict[str, Optional[str]]) -> str:
    h = c["hash"][:7]
    scope = f"**{parsed['scope']}**: " if parsed.get("scope") else ""
    brk = " **(BREAKING)**" if parsed.get("breaking") else ""
    return f"- {scope}{parsed['subject']} ({h}) by {c['author']}{brk}"

def _summarize(commits: List[Dict[str, str]]) -> Tuple[Dict[str, List[str]], List[str]]:
    sections: Dict[str, List[str]] = {t: [] for t in SECTION_ORDER}
    breaking: List[str] = []
    for c in commits:
        parsed = _parse_conv(c["subject"])
        t = parsed["type"] if parsed["type"] in sections else "other"
        line = _format_commit_line(c, parsed)
        sections[t].append(line)
        if parsed.get("breaking"):
            breaking.append(line)
    return sections, breaking

def _ensure_trailing_newline(text: str) -> str:
    text = text.rstrip("\n")
    return text + "\n"

def _render_markdown(
    commits: List[Dict[str, str]],
    pr_ctx: Dict[str, str],
    title: Optional[str] = None,
    version: Optional[str] = None,
) -> str:
    sections, breaking = _summarize(commits)
    lines: List[str] = []
    title = title or (f"Release Notes {version}" if version else "Release Notes")

    lines.append(f"# {title}")
    lines.append("")
    lines.append(f"_Generated: {_now_utc_iso()}_")
    lines.append("")

    if version:
        lines.append(f"**Version**: {version}")
        lines.append("")

    if pr_ctx.get("pr_number"):
        lines.append(f"**PR**: #{pr_ctx['pr_number']} — {pr_ctx.get('pr_title','')}")
        if pr_ctx.get("pr_html_url"):
            lines.append(f"**URL**: {pr_ctx['pr_html_url']}")
        if pr_ctx.get("pr_user"):
            lines.append(f"**Opened by**: @{pr_ctx['pr_user']}")
        lines.append("")

    if breaking:
        lines.append("## ❗ Breaking Changes")
        lines.extend(breaking)
        lines.append("")

    for t in SECTION_ORDER:
        items = sections.get(t) or []
        if not items:
            continue
        lines.append(f"## {SECTION_TITLES.get(t, t.title())}")
        lines.extend(items)
        lines.append("")

    if not commits:
        lines.append("_No changes in the selected range._")
        lines.append("")

    return _ensure_trailing_newline("\n".join(lines).strip())

# -----------------------
# LLM enrichment (facoltativo)
# -----------------------

def _llm_enabled() -> bool:
    return (os.environ.get("ENABLE_LLM") or "").lower() in ("1", "true", "yes")

def _enrich_with_llm(markdown_notes: str) -> str:
    if not _llm_enabled():
        return markdown_notes

    provider = (os.environ.get("LLM_PROVIDER") or "openai").lower()
    try:
        import requests  # import locale, niente internet in fase di lint
        if provider == "azure":
            api_key = os.environ.get("AZURE_OPENAI_API_KEY")
            endpoint = os.environ.get("AZURE_OPENAI_ENDPOINT")
            deployment = os.environ.get("AZURE_OPENAI_DEPLOYMENT")
            if not (api_key and endpoint and deployment):
                return markdown_notes
            url = f"{endpoint}/openai/deployments/{deployment}/chat/completions?api-version=2024-02-15-preview"
            headers = {"api-key": api_key, "Content-Type": "application/json"}
        else:
            api_key = os.environ.get("OPENAI_API_KEY")
            if not api_key:
                return markdown_notes
            url = "https://api.openai.com/v1/chat/completions"
            headers = {"Authorization": f"Bearer {api_key}", "Content-Type": "application/json"}

        prompt = (
            "Sei un assistente che sintetizza release notes tecniche in inglese, in modo chiaro e conciso. "
            "Dato il seguente testo Markdown, produci SOLO:\n"
            "## Sintesi\n"
            "- un paragrafo di 3-4 frasi;\n"
            "## Highlights\n"
            "- 3-7 punti elenco.\n\n"
            "NON includere o ripetere il testo originale delle note: verrà aggiunto dal chiamante.\n"
            "NON aggiungere altro oltre a queste due sezioni.\n\n"
            "Testo:\n" + markdown_notes
        )
        payload = {
            "model": "gpt-4o-mini",
            "messages": [
                {"role": "system", "content": "You are a helpful release-notes editor."},
                {"role": "user", "content": prompt},
            ],
            "temperature": 0.2,
            "max_tokens": 800,
        }
        if provider == "azure":
            payload.pop("model", None)

        resp = requests.post(url, headers=headers, json=payload, timeout=60)
        resp.raise_for_status()
        data = resp.json()
        content = data["choices"][0]["message"]["content"].strip()
        # Se il modello ha (per errore) già incluso l'originale, evita di appenderlo di nuovo
        title_line = markdown_notes.splitlines()[0] if markdown_notes else ""
        if title_line and title_line in content:
            enriched = content  # contiene già tutto
        else:
            enriched = f"{content}\n\n---\n\n{markdown_notes}"
        return _ensure_trailing_newline(enriched)
    
    except Exception:
        # Fail-safe: mai bloccare il job
        return markdown_notes

# -----------------------
# CLI / main
# -----------------------

def parse_args(argv: Optional[List[str]] = None):
    p = argparse.ArgumentParser(description="Generate (LLM-enriched) release notes from git history.")
    # Nuova CLI
    src = p.add_mutually_exclusive_group(required=False)
    src.add_argument("--range", dest="range", help="Commit range (es: origin/main..HEAD)")
    src.add_argument("--since-tag", dest="since_tag", help="Usa <tag>..HEAD")
    p.add_argument("--from", dest="from_ref", help="Commit/Ref iniziale (usato con --to)")
    p.add_argument("--to", dest="to_ref", help="Commit/Ref finale (usato con --from)")
    p.add_argument("-o", "--output", dest="output", help="File di output (default: stdout)")
    p.add_argument("--title", dest="title", help="Titolo delle note")

    # Compat legacy (tua CLI)
    p.add_argument("--base", dest="base", help="Base ref per il range (legacy compat)")
    p.add_argument("--head", dest="head", help="Head ref per il range (legacy compat)")
    p.add_argument("--version", dest="version", help="Versione da riportare in testa (legacy compat)")
    p.add_argument("--out", dest="out", help="Percorso file output (alias di --output)")

    # LLM
    p.add_argument("--no-llm", action="store_true", help="Disabilita arricchimento LLM (ignora ENABLE_LLM)")
    return p.parse_args(argv)

def _build_notes(args) -> str:
    commit_range = _resolve_range(args)
    commits = _git_commits(commit_range)
    pr_ctx = _load_pr_context()

    # Titolo: priorità a --title, poi "Release Notes {version}" se presente
    title = args.title or (f"Release Notes {args.version}" if args.version else None)
    base_text = _render_markdown(commits, pr_ctx, title=title, version=args.version)

    if args.no_llm:
        return base_text
    return _enrich_with_llm(base_text)

def _write_output(text: str, path: Optional[str]) -> None:
    if path:
        os.makedirs(os.path.dirname(os.path.abspath(path)), exist_ok=True)
        with open(path, "w", encoding="utf-8", newline="\n") as f:
            f.write(text)
    else:
        print(text, end="")

def main(argv: Optional[List[str]] = None) -> int:
    args = parse_args(argv)

    # Normalizza alias legacy
    output_path = args.output or args.out  # --out è alias
    try:
        notes = _build_notes(args)
        _write_output(notes, output_path)
        return 0
    except Exception as ex:
        sys.stderr.write(f"[release-notes] ERROR: {ex}\n")
        return 1

if __name__ == "__main__":
    raise SystemExit(main())

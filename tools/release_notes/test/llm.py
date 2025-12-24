
# tools/release_notes/llm.py
from __future__ import annotations
import hashlib, json, os
from pathlib import Path
from typing import Optional, Dict
import subprocess

CACHE_DIR = Path(".llm-cache")
CACHE_DIR.mkdir(exist_ok=True)

def _read_git_diff(sha: str, max_bytes: int = 160_000) -> str:
    diff = subprocess.run(
        ["git", "show", sha, "--patch", "--no-ext-diff", "--find-renames", "--find-copies", "--unified=3"],
        check=True, text=True, stdout=subprocess.PIPE
    ).stdout
    return diff[:max_bytes]

def _cache_get(key: str) -> Optional[dict]:
    path = CACHE_DIR / f"{key}.json"
    if path.exists():
        try:
            return json.loads(path.read_text(encoding="utf-8"))
        except Exception:
            return None
    return None

def _cache_set(key: str, data: dict) -> None:
    path = CACHE_DIR / f"{key}.json"
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding="utf-8")

def _hash_inputs(*chunks: str) -> str:
    import hashlib
    h = hashlib.sha256()
    for c in chunks:
        h.update(c.encode("utf-8", errors="ignore"))
    return h.hexdigest()[:32]

def _llm_call(system_prompt: str, user_prompt: str, model: Optional[str]=None, max_tokens: int=400) -> Optional[str]:
    api_key = os.getenv("OPENAI_API_KEY")
    if not api_key:
        return None
    model = model or os.getenv("LLM_MODEL", "gpt-4o-mini")
    try:
        import requests
        resp = requests.post(
            "https://api.openai.com/v1/chat/completions",
            headers={"Authorization": f"Bearer {api_key}"},
            json={
                "model": model,
                "messages": [
                    {"role": "system", "content": system_prompt},
                    {"role": "user", "content": user_prompt},
                ],
                "temperature": float(os.getenv("LLM_TEMPERATURE", "0.2")),
                "max_tokens": max_tokens,
            },
            timeout=60,
        )
        resp.raise_for_status()
        data = resp.json()
        return data["choices"][0]["message"]["content"].strip()
    except Exception:
        return None

def summarize_diff(commit_sha: str, commit_subject: str) -> Optional[str]:
    diff = _read_git_diff(commit_sha)
    key = _hash_inputs("sum", commit_sha, commit_subject, diff)
    cached = _cache_get(key)
    if cached:
        return cached.get("summary")
    sys_prompt = ("Sei un assistente tecnico. Riassumi in 4-8 righe l'impatto delle modifiche "
                  "nel diff, citando API/contratti/DB se toccati. Evita fronzoli.")
    usr_prompt = f"Commit: {commit_subject}\n\nDiff (troncato):\n```\n{diff}\n```"
    out = _llm_call(sys_prompt, usr_prompt, max_tokens=300)
    if out:
        _cache_set(key, {"summary": out})
    return out

def classify_risk(commit_sha: str, commit_subject: str) -> Optional[Dict[str, bool]]:
    diff = _read_git_diff(commit_sha)
    key = _hash_inputs("risk", commit_sha, commit_subject, diff)
    cached = _cache_get(key)
    if cached:
        return cached.get("risk")
    sys_prompt = ("Sei un revisore. Valuta il rischio BC (breaking change), presenza migrazioni DB, "
                  "touch su API pubbliche/contratti. Rispondi JSON con chiavi: "
                  "{'breaking': bool, 'db_migration': bool, 'public_api': bool}.")
    usr_prompt = f"Commit: {commit_subject}\n\nDiff (troncato):\n```\n{diff}\n```"
    out = _llm_call(sys_prompt, usr_prompt, max_tokens=120)
    try:
        data = json.loads(out) if out else None
    except Exception:
        data = None
    if data:
        _cache_set(key, {"risk": data})
    return data

def classify_type(commit_subject: str) -> Optional[str]:
    key = _hash_inputs("type", commit_subject)
    cached = _cache_get(key)
    if cached:
        return cached.get("type")
    sys_prompt = ("Classifica il messaggio secondo Conventional Commits (feat, fix, perf, refactor, "
                  "docs, chore, ci, test, style, revert) o 'other'. Rispondi SOLO con la label.")
    out = _llm_call(sys_prompt, commit_subject, max_tokens=10)
    label = (out or "").strip().lower()
    if label:
        _cache_set(key, {"type": label})
        return label
    return None

"""Minimal lore store used for retrieval (the "RAG" step).

Lore is plain markdown in pipeline/lore/. Files are split into paragraph chunks and
ranked by keyword overlap with the query. Swap `retrieve` for an embedding search
later without touching callers.
"""

from __future__ import annotations

import re
from pathlib import Path

LORE_DIR = Path(__file__).resolve().parent.parent / "lore"
_WORD = re.compile(r"[a-z0-9']+")


def _chunks(lore_dir: Path) -> list[tuple[str, str]]:
    out = []
    for path in sorted(lore_dir.glob("*.md")):
        for para in re.split(r"\n\s*\n", path.read_text(encoding="utf-8")):
            para = para.strip()
            if para:
                out.append((path.name, para))
    return out


def retrieve(query: str, k: int = 5, lore_dir: Path = LORE_DIR) -> list[str]:
    q = set(_WORD.findall(query.lower()))
    scored = []
    for name, para in _chunks(lore_dir):
        score = len(q & set(_WORD.findall(para.lower())))
        scored.append((score, name, para))
    scored.sort(key=lambda t: -t[0])
    return [f"[{name}] {para}" for _, name, para in scored[:k]]


def style_guide(lore_dir: Path = LORE_DIR) -> str:
    return (lore_dir / "style_guide.md").read_text(encoding="utf-8")

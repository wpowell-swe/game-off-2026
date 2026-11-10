"""Write approved content into the game's content folder and register it with MGCB."""

from __future__ import annotations

import json
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
CONTENT_DIR = REPO_ROOT / "ShiftJamContent"
GENERATED_DIR = CONTENT_DIR / "Generated"
MGCB = CONTENT_DIR / "ShiftJamContent.mgcb"

BEGIN = "#--- generated-content:begin (managed by pipeline, do not edit) ---#"
END = "#--- generated-content:end ---#"


def write_content(kind: str, entries: list[dict], out_dir: Path = GENERATED_DIR) -> Path:
    out_dir.mkdir(parents=True, exist_ok=True)
    path = out_dir / f"{kind}.json"
    path.write_text(json.dumps(entries, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    return path


def sync_mgcb(mgcb: Path = MGCB, generated_dir: Path = GENERATED_DIR) -> None:
    """Rewrite the managed block of /copy entries so every generated file ships with the game."""
    blocks = []
    for f in sorted(generated_dir.glob("*.json")):
        rel = f"Generated/{f.name}"
        blocks.append(f"#begin {rel}\n/copy:{rel}\n#end {rel}\n")
    managed = BEGIN + "\n" + "\n".join(blocks) + END + "\n"

    text = mgcb.read_text(encoding="utf-8")
    if BEGIN in text:
        head, rest = text.split(BEGIN, 1)
        tail = rest.split(END, 1)[1].lstrip("\n")
        text = head + managed + ("\n" + tail if tail else "")
    else:
        text = text.rstrip("\n") + "\n\n" + managed
    mgcb.write_text(text, encoding="utf-8")

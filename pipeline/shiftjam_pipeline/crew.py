"""A small two-agent crew: a writer drafts content, a reviewer gates it.

Plain Python on purpose so the control flow is visible. Once the CrewAI lectures
land, `Crew.run` is the piece to replace with a CrewAI hierarchical crew.
"""

from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path
from typing import Callable

import jsonschema

from . import llm, lore

SCHEMA_DIR = Path(__file__).resolve().parent.parent / "schemas"
KINDS = ("dialogue", "item")  # each has schemas/<kind>.schema.json

LLM = Callable[[str, str], str]


def load_schema(kind: str) -> dict:
    return json.loads((SCHEMA_DIR / f"{kind}.schema.json").read_text(encoding="utf-8"))


@dataclass
class Agent:
    name: str
    system: str


WRITER = Agent(
    "writer",
    "You are the writer on a small game studio's content crew. Produce game content "
    "that follows the style guide and stays consistent with the lore you are given. "
    "Reply with ONLY a JSON array that matches the provided JSON schema.",
)
REVIEWER = Agent(
    "reviewer",
    "You are the strict reviewer on a game studio's content crew. Check the draft "
    'against the style guide and lore. Reply with ONLY JSON: {"approved": bool, '
    '"issues": [string]}. Approve only if there are no lore contradictions or style violations.',
)


class Crew:
    def __init__(self, llm_call: LLM | None = None, max_revisions: int = 2):
        self.llm_call = llm_call or (lambda system, user: llm.complete(system, user, 3000))
        self.max_revisions = max_revisions

    def run(self, kind: str, brief: str, count: int = 5) -> list[dict]:
        schema = load_schema(kind)
        context = "\n".join(lore.retrieve(brief))
        guide = lore.style_guide()
        base = (
            f"STYLE GUIDE:\n{guide}\n\nRELEVANT LORE:\n{context}\n\n"
            f"JSON SCHEMA (one array element):\n{json.dumps(schema)}\n\n"
            f"TASK: write {count} {kind} entries. Brief: {brief}"
        )
        feedback = ""
        for _ in range(self.max_revisions + 1):
            draft_text = self.llm_call(WRITER.system, base + feedback)
            try:
                draft = llm.extract_json(draft_text)
                if not isinstance(draft, list):
                    raise ValueError("expected a JSON array")
                for entry in draft:
                    jsonschema.validate(entry, schema)
            except (ValueError, jsonschema.ValidationError) as e:
                feedback = f"\n\nYour previous draft was invalid: {e}. Fix it."
                continue
            review = llm.extract_json(
                self.llm_call(
                    REVIEWER.system,
                    f"{base}\n\nDRAFT:\n{json.dumps(draft)}",
                )
            )
            if review.get("approved"):
                return draft
            feedback = "\n\nReviewer issues to fix:\n- " + "\n- ".join(review.get("issues", []))
        raise RuntimeError(f"{kind}: no approved draft after {self.max_revisions + 1} attempts")

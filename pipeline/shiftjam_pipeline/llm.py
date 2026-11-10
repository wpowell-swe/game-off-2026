"""Thin wrapper around the Claude API. The only module that talks to the network."""

from __future__ import annotations

import json
import os
import re
import subprocess

import anthropic

DEFAULT_MODEL = os.environ.get("SHIFTJAM_MODEL", "claude-sonnet-5-5")

_client: anthropic.Anthropic | None = None


def use_cli() -> bool:
    """Backend choice: the Claude Code CLI when only an OAuth token is available.

    OAuth tokens from Claude Code are meant for the `claude` CLI, not raw API calls
    (the API answers them with 429s), so the CLI is the supported way to use one.
    Force a backend with SHIFTJAM_BACKEND=api|cli.
    """
    forced = os.environ.get("SHIFTJAM_BACKEND")
    if forced:
        return forced == "cli"
    return not os.environ.get("ANTHROPIC_API_KEY") and bool(os.environ.get("CLAUDE_CODE_OAUTH_TOKEN"))


def _complete_cli(system: str, user: str) -> str:
    proc = subprocess.run(
        [
            "claude", "-p",
            "--system-prompt", system,
            "--model", DEFAULT_MODEL,
            "--tools", "",
            "--no-session-persistence",
        ],
        input=user,
        capture_output=True,
        text=True,
        timeout=300,
    )
    if proc.returncode != 0:
        raise RuntimeError(f"claude CLI failed ({proc.returncode}): {proc.stderr.strip()[:500]}")
    return proc.stdout


def make_client() -> anthropic.Anthropic:
    if not os.environ.get("ANTHROPIC_API_KEY"):
        raise RuntimeError("Set ANTHROPIC_API_KEY (or CLAUDE_CODE_OAUTH_TOKEN to use the claude CLI)")
    return anthropic.Anthropic()


def complete(system: str, user: str, max_tokens: int = 2048) -> str:
    if use_cli():
        return _complete_cli(system, user)
    global _client
    if _client is None:
        _client = make_client()
    resp = _client.messages.create(
        model=DEFAULT_MODEL,
        max_tokens=max_tokens,
        system=system,
        messages=[{"role": "user", "content": user}],
    )
    return "".join(b.text for b in resp.content if b.type == "text")


def extract_json(text: str):
    """Parse the first JSON value in a model reply, tolerating ```json fences."""
    fenced = re.search(r"```(?:json)?\s*(.*?)```", text, re.DOTALL)
    return json.loads((fenced.group(1) if fenced else text).strip())

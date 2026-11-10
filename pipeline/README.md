# ShiftJam content pipeline

A writer agent drafts content, a reviewer agent gates it (style guide, lore, JSON schema), and approved output is written to `ShiftJamContent/Generated/*.json`, which the game loads through `GeneratedContent` in `ShiftJam.Game`.

```
brief -> retrieve lore -> writer -> schema check -> reviewer -> (revise up to 2x) -> JSON + .mgcb /copy entry
```

## Run

```bash
cd pipeline
python3 -m venv .venv && .venv/bin/pip install -e '.[dev]'
export ANTHROPIC_API_KEY=...   # or set CLAUDE_CODE_OAUTH_TOKEN; it is used when no API key is set
.venv/bin/python -m shiftjam_pipeline.cli generate dialogue --brief "Mara greets the player after a late shift" --count 5
.venv/bin/pytest
```

Docker: a `Dockerfile` is included and builds, but it is not wired up yet. `export.py` locates the repo root relative to the package, so the container needs a configurable output path (for example a `SHIFTJAM_CONTENT_DIR` env var plus a volume mount) before it can write into the game folder.

## Edit these to steer output

- `lore/*.md`: world facts (placeholder for now). One topic per paragraph.
- `lore/style_guide.md`: voice and limits (placeholder).
- `schemas/*.schema.json`: the shape of each content type. Add a schema and a C# record to add a type.

## Roadmap (mirrors the course)

1. Replace `Crew.run` with a CrewAI hierarchical crew (session 04).
2. Replace keyword retrieval in `lore.py` with embeddings (session 05).
3. Add level layouts and a style-guide agent (sessions 08-09).
4. QA "chaos crew" and cost controls (sessions 11-12).

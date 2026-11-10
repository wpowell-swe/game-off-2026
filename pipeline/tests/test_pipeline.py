import json

from shiftjam_pipeline import crew, export, lore

GOOD = [{"id": "greet_1", "speaker": "Mara", "text": "You shifted late.", "mood": "tense"}]


def make_llm(replies):
    it = iter(replies)
    return lambda system, user: next(it)


def test_backend_selection(monkeypatch):
    from shiftjam_pipeline import llm

    monkeypatch.delenv("SHIFTJAM_BACKEND", raising=False)
    monkeypatch.delenv("ANTHROPIC_API_KEY", raising=False)
    monkeypatch.setenv("CLAUDE_CODE_OAUTH_TOKEN", "dummy-token")
    assert llm.use_cli() is True
    monkeypatch.setenv("ANTHROPIC_API_KEY", "dummy-key")
    assert llm.use_cli() is False


def test_retrieve_ranks_relevant_paragraph_first():
    top = lore.retrieve("shift shards trade places", k=1)[0]
    assert "shards" in top.lower()


def test_crew_approves_valid_draft():
    llm = make_llm([json.dumps(GOOD), '{"approved": true, "issues": []}'])
    assert crew.Crew(llm).run("dialogue", "a tense greeting", 1) == GOOD


def test_crew_retries_after_reviewer_rejection():
    llm = make_llm(
        [json.dumps(GOOD), '{"approved": false, "issues": ["too long"]}', json.dumps(GOOD), '{"approved": true, "issues": []}']
    )
    assert crew.Crew(llm).run("dialogue", "x", 1) == GOOD


def test_crew_retries_schema_violation():
    bad = [{"id": "Bad ID", "speaker": "M", "text": "t", "mood": "tense"}]
    llm = make_llm([json.dumps(bad), json.dumps(GOOD), '{"approved": true, "issues": []}'])
    assert crew.Crew(llm).run("dialogue", "x", 1) == GOOD


def test_export_writes_json_and_syncs_mgcb(tmp_path):
    gen = tmp_path / "Generated"
    export.write_content("dialogue", GOOD, gen)
    mgcb = tmp_path / "x.mgcb"
    mgcb.write_text("/config:\n")
    export.sync_mgcb(mgcb, gen)
    export.sync_mgcb(mgcb, gen)  # idempotent
    text = mgcb.read_text()
    assert text.count("/copy:Generated/dialogue.json") == 1
    assert json.loads((gen / "dialogue.json").read_text()) == GOOD

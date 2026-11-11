#!/usr/bin/env python3
"""One-off: turns literal `const` tunables in ShiftJam.Game into Cfg entries and writes their current values to
ShiftJamContent/Data/balance.json.

    private const float SwingWindup = 0.6f;
becomes
    private static float SwingWindup = Cfg.F("Pell.SwingWindup", 0.6f, v => SwingWindup = v);

Only plain numeric/bool literals are converted (not expressions, strings, or Metrics in Core/Contracts.cs).
Run once; running it again finds nothing left to convert. Standard library only.
"""
import json
import re
import sys
from collections import OrderedDict
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SRC = ROOT / "ShiftJam.Game"
OUT = ROOT / "ShiftJamContent" / "Data" / "balance.json"
SKIP = {"Core/Contracts.cs", "GeneratedContent.cs"}

CONST = re.compile(r"^(\s*)((?:private|public|internal|protected)\s+)const\s+(float|int|bool)\s+(\w+)\s*=\s*"
                   r"(-?\d+(?:\.\d+)?f?|true|false)\s*;(.*)$")
CLASS = re.compile(r"^(\s*)(?:(?:public|private|internal|protected)\s+)?(?:(?:static|sealed|abstract|partial)\s+)*(?:class|struct|record)\s+(\w+)")
FN = {"float": "F", "int": "I", "bool": "B"}

groups = OrderedDict()
changed = 0
for path in sorted(SRC.rglob("*.cs")):
    rel = path.relative_to(SRC).as_posix()
    if rel.startswith(("obj/", "bin/")) or rel in SKIP:
        continue
    raw = path.read_bytes()
    bom = raw.startswith(b"\xef\xbb\xbf")
    text = raw.decode("utf-8-sig")
    crlf = "\r\n" in text
    lines = text.replace("\r\n", "\n").split("\n")
    edited = False
    for i, line in enumerate(lines):
        m = CONST.match(line)
        if not m:
            continue
        indent, vis, typ, name, lit, rest = m.groups()
        cls = None
        for j in range(i - 1, -1, -1):
            c = CLASS.match(lines[j])
            if c and len(c.group(1)) < len(indent):
                cls = c.group(2)
                break
        if cls is None:
            sys.exit(f"no enclosing class for {rel}:{i + 1}")
        key = f"{cls}.{name}"
        value = {"true": True, "false": False}.get(lit)
        if value is None:
            value = float(lit.rstrip("f")) if typ == "float" else int(lit)
        grp = groups.setdefault(cls, OrderedDict())
        if name in grp:
            sys.exit(f"duplicate key {key}")
        grp[name] = value
        lines[i] = f'{indent}{vis}static {typ} {name} = Cfg.{FN[typ]}("{key}", {lit}, v => {name} = v);{rest}'
        edited = True
        changed += 1
    if edited:
        out = "\n".join(lines)
        if crlf:
            out = out.replace("\n", "\r\n")
        path.write_bytes((b"\xef\xbb\xbf" if bom else b"") + out.encode("utf-8"))

def fmt(v):
    if isinstance(v, bool):
        return "true" if v else "false"
    if isinstance(v, float) and v == int(v) and abs(v) < 1e9:
        return str(int(v))
    return repr(v)

if groups:
    parts = []
    for cls, items in groups.items():
        body = ",\n".join(f'    "{k}": {fmt(v)}' for k, v in items.items())
        parts.append(f'  "{cls}": {{\n{body.replace(chr(10) + "    ", chr(10) + "    ")}\n  }}')
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text("{\n" + ",\n".join(parts) + "\n}\n")
print(f"converted {changed} constants in {len(groups)} classes; wrote {OUT.relative_to(ROOT)}")

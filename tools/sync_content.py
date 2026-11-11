#!/usr/bin/env python3
"""Lists every sprite, UI image (Ui/*.png), sound effect and data file (Data/*.json, Data/levels/*.txt) in ShiftJamContent/ShiftJamContent.mgcb.

Run this after adding, renaming or deleting a file in ShiftJamContent/Sprites (*.png) or
ShiftJamContent/Sfx (*.wav) or ShiftJamContent/Data, then run `dotnet build`. Safe to run any number of times: it only
rewrites its own block (between the two marker comments) and leaves the rest of the file alone.
Standard library only. Keeps the file's existing line endings (CRLF).
"""
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
CONTENT = ROOT / "ShiftJamContent"
MGCB = CONTENT / "ShiftJamContent.mgcb"

BEGIN = "#--- sprites-and-sfx:begin (managed by tools/sync_content.py, do not edit) ---#"
END = "#--- sprites-and-sfx:end ---#"


def texture_entry(rel):
    return [
        f"#begin {rel}",
        "/importer:TextureImporter",
        "/processor:TextureProcessor",
        "/processorParam:ColorKeyColor=255,0,255,255",
        "/processorParam:ColorKeyEnabled=False",
        "/processorParam:GenerateMipmaps=False",
        "/processorParam:PremultiplyAlpha=False",
        "/processorParam:ResizeToPowerOfTwo=False",
        "/processorParam:TextureFormat=Color",
        f"/build:{rel}",
        f"#end {rel}",
    ]


def copy_entry(rel):
    return [f"#begin {rel}", f"/copy:{rel}", f"#end {rel}"]


def sound_entry(rel):
    return [
        f"#begin {rel}",
        "/importer:WavImporter",
        "/processor:SoundEffectProcessor",
        "/processorParam:Quality=Best",
        f"/build:{rel}",
        f"#end {rel}",
    ]


def files(folder, pattern):
    d = CONTENT / folder
    if not d.is_dir():
        return []
    return sorted(f"{folder}/{p.name}" for p in d.glob(pattern) if p.is_file())


def main():
    raw = MGCB.read_bytes()
    bom = b"\xef\xbb\xbf" if raw.startswith(b"\xef\xbb\xbf") else b""
    text = raw[len(bom):].decode("utf-8")
    nl = "\r\n" if "\r\n" in text else "\n"
    lines = text.replace("\r\n", "\n").split("\n")
    had_final_newline = lines and lines[-1] == ""
    if had_final_newline:
        lines.pop()

    block = [BEGIN]
    for rel in files("Sprites", "*.png"):
        block += [""] + texture_entry(rel)
    for rel in files("Ui", "*.png"):
        block += [""] + texture_entry(rel)
    for rel in files("Sfx", "*.wav"):
        block += [""] + sound_entry(rel)
    # Game data (balance numbers, text, campaign, sprite table, level maps): copied as they are, read by the game at runtime.
    for rel in files("Data", "*.json") + files("Data/levels", "*.txt"):
        block += [""] + copy_entry(rel)
    block.append(END)

    if BEGIN in lines and END in lines:
        a, b = lines.index(BEGIN), lines.index(END)
        if b < a:
            sys.exit("sync_content: end marker comes before begin marker in the .mgcb file.")
        new = lines[:a] + block + lines[b + 1:]
    elif BEGIN in lines or END in lines:
        sys.exit("sync_content: only one of the two marker comments was found; fix the .mgcb by hand.")
    else:
        new = lines + ([""] if lines and lines[-1] != "" else []) + block

    out = nl.join(new) + (nl if had_final_newline or BEGIN not in lines else "")
    data = bom + out.encode("utf-8")
    if data == raw:
        print("sync_content: already up to date.")
    else:
        MGCB.write_bytes(data)
        print(f"sync_content: updated {MGCB.relative_to(ROOT)}")
    print(f"  {len(files('Sprites', '*.png'))} sprites, {len(files('Sfx', '*.wav'))} sound effects, "
          f"{len(files('Data', '*.json')) + len(files('Data/levels', '*.txt'))} data files listed.")


if __name__ == "__main__":
    main()

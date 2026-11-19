#!/usr/bin/env python3
"""Writes 16x16 tile PNGs into ShiftJamContent/Tiles/ that look like the game's built-in code-drawn tiles.

They are templates to paint over: save your own PNG with the same name. A tile PNG may also be 32, 48, ... px wide
(several tiles side by side); the game then picks a variant per map cell. Needs Pillow.
Usage: python3 tools/make_placeholder_tiles.py [--force]   (only missing files are written, unless --force)
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw

OUT = Path(__file__).resolve().parent.parent / "ShiftJamContent" / "Tiles"
T = 16


def rect(d, x, y, w, h, c):
    d.rectangle([x, y, x + w - 1, y + h - 1], fill=c)


def solid(top):
    im = Image.new("RGBA", (T, T)); d = ImageDraw.Draw(im)
    body, light, dark = (78, 60, 52, 255), (150, 118, 92, 255), (52, 40, 36, 255)
    rect(d, 0, 0, T, T, body)
    if top:
        rect(d, 0, 0, T, 3, light); rect(d, 0, 3, T, 1, dark)
    rect(d, 0, T - 2, T, 2, dark)
    rect(d, 8, 6, 1, 4, dark)
    return im


def wall():
    im = Image.new("RGBA", (T, T)); d = ImageDraw.Draw(im)
    rect(d, 0, 0, T, T, (104, 70, 150, 255))
    for i in range(0, T, 4):
        rect(d, i % T, i, 4, 2, (138, 100, 190, 255))
    rect(d, 0, 0, 1, T, (66, 42, 100, 255)); rect(d, T - 1, 0, 1, T, (66, 42, 100, 255))
    return im


def astral():
    im = Image.new("RGBA", (T, T)); d = ImageDraw.Draw(im)
    def a(c, k): return (c[0], c[1], c[2], int(255 * k))
    rect(d, 0, 0, T, T, a((60, 150, 200), 0.35))
    e = (150, 235, 255)
    rect(d, 0, 0, T, 2, a(e, 0.9)); rect(d, 0, T - 1, T, 1, a(e, 0.5))
    rect(d, 0, 0, 1, T, a(e, 0.5)); rect(d, T - 1, 0, 1, T, a(e, 0.5)); rect(d, 7, 4, 2, 8, a(e, 0.4))
    return im


def gate():
    im = Image.new("RGBA", (T, T), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    rect(d, 2, 0, 3, T, (120, 120, 135, 255)); rect(d, 10, 0, 3, T, (120, 120, 135, 255))
    rect(d, 0, 6, T, 2, (85, 85, 100, 255)); rect(d, 6, 2, 4, 3, (255, 80, 70, 255))
    return im


def spikes():
    im = Image.new("RGBA", (T, T), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    for i in range(0, T, 4):
        rect(d, i, 10, 4, 6, (190, 190, 205, 255)); rect(d, i + 1, 6, 2, 4, (225, 225, 240, 255))
    return im


def shrine(lit):
    im = Image.new("RGBA", (T, T), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    iron = (70, 62, 78, 255)
    rect(d, 4, 14, 8, 2, iron); rect(d, 7, 4, 2, 10, iron); rect(d, 4, 0, 8, 6, iron)
    if lit:
        rect(d, 5, 1, 6, 4, (255, 214, 120, 255)); rect(d, 7, 2, 2, 2, (255, 255, 255, 255))
    else:
        rect(d, 5, 1, 6, 4, (36, 32, 44, 255))
    return im


TILES = {"solid": lambda: solid(False), "solid_top": lambda: solid(True), "wall": wall, "astral": astral,
         "gate": gate, "spikes": spikes, "shrine": lambda: shrine(False), "shrine_lit": lambda: shrine(True)}


def main():
    force = "--force" in sys.argv
    OUT.mkdir(parents=True, exist_ok=True)
    for name, make in TILES.items():
        p = OUT / f"{name}.png"
        if p.exists() and not force:
            continue
        make().save(p); print("wrote", p.name)


if __name__ == "__main__":
    main()

#!/usr/bin/env python3
"""Writes template PNGs for pickups, doors and UI frames into ShiftJamContent/Items and ShiftJamContent/Ui.

They look like the game's built-in code-drawn versions, so you can paint over them. Same name = replaced.
Sizes: see docs/ART_AND_GIT_GUIDE.md. Needs Pillow.
Usage: python3 tools/make_placeholder_ui.py [--force]   (only missing files are written, unless --force)
"""
import math
import sys
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parent.parent / "ShiftJamContent"
GOLD, DGOLD = (255, 225, 140, 255), (150, 115, 50, 255)
WOOD = (110, 80, 50, 255)


def img(w, h, c=(0, 0, 0, 0)):
    im = Image.new("RGBA", (w, h), c)
    return im, ImageDraw.Draw(im)


def r(d, x, y, w, h, c):
    d.rectangle([x, y, x + w - 1, y + h - 1], fill=c)


def ki():
    im, d = img(14, 14)
    r(d, 0, 0, 14, 14, (70, 190, 255, 64))
    for i, w in enumerate([2, 4, 6, 8, 10, 10, 8, 6, 4, 2]):
        r(d, 2 + (10 - w) // 2, 2 + i, w, 1, (120, 220, 255, 255))
    r(d, 5, 5, 2, 2, (255, 255, 255, 255))
    return im


def heart():
    im, d = img(14, 14)
    r(d, 0, 0, 14, 14, (255, 90, 90, 64))
    red = (235, 70, 80, 255)
    r(d, 2, 3, 4, 4, red); r(d, 8, 3, 4, 4, red); r(d, 2, 5, 10, 3, red); r(d, 4, 8, 6, 2, red); r(d, 6, 10, 2, 1, red)
    r(d, 3, 4, 2, 1, (255, 190, 190, 255))
    return im


def shard():
    im, d = img(14, 14)
    gold, rim = (255, 200, 80, 255), (255, 240, 190, 255)
    r(d, 0, 0, 14, 14, (255, 200, 80, 64))
    n = 10
    for row in range(n):
        dy = n - 1 - row
        w = int(math.sqrt(n * n - dy * dy))
        if w <= 0: continue
        r(d, 2, 2 + row, w, 1, gold); r(d, 2 + w - 1, 2 + row, 1, 1, rim)
    r(d, 2, 2 + n - 1, n, 1, rim); r(d, 2, 2, 1, n, rim); r(d, 4, 6, 2, 2, (255, 255, 255, 255))
    return im


def door(glow, a_inner):
    im, d = img(20, 51)
    r(d, 2, 3, 16, 48, glow[:3] + (int(255 * 0.45),))
    r(d, 5, 7, 12, 44, glow[:3] + (int(255 * a_inner),))
    r(d, 0, 3, 3, 48, WOOD); r(d, 17, 3, 3, 48, WOOD); r(d, 0, 0, 20, 3, WOOD)
    return im


def frame(w, h, c=(0, 0, 0, 255)):
    im, d = img(w, h)
    d.rectangle([0, 0, w - 1, h - 1], outline=c)
    return im


def badge(astral):
    im, d = img(26, 12, (0, 0, 0, 255))
    if not astral:
        r(d, 1, 1, 24, 10, (150, 80, 30, 255)); r(d, 3, 3, 20, 6, (240, 170, 60, 255)); r(d, 10, 2, 6, 8, (255, 225, 140, 255))
    else:
        r(d, 1, 1, 24, 10, (30, 40, 110, 255)); r(d, 3, 3, 20, 6, (90, 190, 255, 255)); r(d, 10, 4, 6, 4, (30, 40, 110, 255))
    return im


def charm():
    im, d = img(5, 5, (0, 0, 0, 255)); r(d, 1, 1, 3, 3, (255, 210, 100, 255)); return im


def dialogue_box():
    im, d = img(24, 24, GOLD)
    r(d, 1, 1, 22, 22, (0, 0, 0, 255)); r(d, 3, 3, 18, 18, DGOLD); r(d, 4, 4, 16, 16, (6, 4, 14, 255))
    return im


def tab():
    im, d = img(12, 12, GOLD); r(d, 1, 1, 10, 11, (0, 0, 0, 255)); return im


def arrow_more():
    im, d = img(7, 4)
    for i in range(4): r(d, i, i, 7 - 2 * i, 1, GOLD)
    return im


def arrow_end():
    im, d = img(7, 4); r(d, 1, 0, 5, 4, GOLD); return im


def cursor():
    im, d = img(9, 9)
    for i, w in enumerate([5, 0, 7, 0, 5, 0, 3]):
        if w: r(d, 2, 1 + i, w, 1, GOLD)
    return im


def pause_bg():
    return Image.new("RGBA", (320, 180), (0, 0, 10, 170))


FILES = {
    "Items/pickup_ki.png": ki, "Items/pickup_health.png": heart, "Items/pickup_shard.png": shard,
    "Items/door_exit.png": lambda: door((255, 225, 140), 0.55),
    "Items/door_open.png": lambda: door((255, 225, 140), 0.55),
    "Items/door_done.png": lambda: door((90, 170, 110), 0.45),
    "Items/door_shut.png": lambda: door((70, 60, 80), 0.45),
    "Ui/hud_health_frame.png": lambda: frame(82, 8), "Ui/hud_ki_frame.png": lambda: frame(82, 8),
    "Ui/hud_material.png": lambda: badge(False), "Ui/hud_astral.png": lambda: badge(True),
    "Ui/hud_charm.png": charm,
    "Ui/dialogue_box.png": dialogue_box, "Ui/dialogue_tab.png": tab,
    "Ui/dialogue_more.png": arrow_more, "Ui/dialogue_end.png": arrow_end,
    "Ui/pause_cursor.png": cursor, "Ui/pause_bg.png": pause_bg,
}


def main():
    force = "--force" in sys.argv
    for rel, make in FILES.items():
        p = ROOT / rel
        if p.exists() and not force: continue
        p.parent.mkdir(parents=True, exist_ok=True)
        make().save(p); print("wrote", rel)


if __name__ == "__main__":
    main()

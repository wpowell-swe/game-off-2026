#!/usr/bin/env python3
"""Draws the game icon (a glowing doorway: the threshold) and writes it where the launchers use it:
the desktop window icon (Icon.bmp), the executable icon (Icon.ico) and the web favicon.
Usage: python3 tools/make_icon.py
"""
from pathlib import Path
from PIL import Image

N = 32
BG = (20, 16, 28)
WOOD = (110, 80, 50)
GLOW = (255, 225, 140)
CORE = (255, 245, 200)
ASTRAL = (70, 130, 255)

img = Image.new("RGB", (N, N), BG)
px = img.load()

def rect(x0, y0, x1, y1, c):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            px[x, y] = c

# A faint astral glow behind the door, brightest low.
for y in range(N):
    for x in range(N):
        d = abs(x - 15.5) / 16 + (1 - y / N) * 0.35
        if d < 0.75:
            t = 1 - d / 0.75
            px[x, y] = tuple(int(BG[i] + (ASTRAL[i] * 0.45 - BG[i]) * t) for i in range(3))

rect(8, 4, 23, 29, WOOD)                 # frame
rect(10, 7, 21, 29, GLOW)                # light in the doorway
rect(12, 9, 19, 29, CORE)
rect(14, 12, 17, 29, (255, 255, 235))
rect(4, 29, 27, 31, (52, 40, 36))        # threshold stone
rect(4, 29, 27, 29, (150, 118, 92))

out = Path(__file__).resolve().parent.parent
bmp = img.resize((64, 64), Image.NEAREST)
bmp.save(out / "ShiftJam.SDL2.GL" / "Icon.bmp")
sizes = [(16, 16), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)]
big = img.resize((256, 256), Image.NEAREST)
big.save(out / "ShiftJam.SDL2.GL" / "Icon.ico", sizes=sizes)
big.save(out / "ShiftJam.Blazor.GL" / "wwwroot" / "favicon.ico", sizes=sizes)
print("wrote Icon.bmp, Icon.ico and favicon.ico")

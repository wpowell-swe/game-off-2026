#!/usr/bin/env python3
"""Draws simple placeholder pixel-art strips into ShiftJamContent/Sprites/.

These exist only so the game shows *something* before the real art arrives. Replace any of them by
saving your own PNG with the same name (see docs/ART_AND_GIT_GUIDE.md). Needs Pillow.
Frame sizes must match ShiftJam.Game/Gfx/SpriteDefs.cs. Art faces RIGHT.
Usage: python3 tools/make_placeholder_sprites.py [--force]   (existing files are kept unless --force)
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw

OUT = Path(__file__).resolve().parent.parent / "ShiftJamContent" / "Sprites"

# Small fixed palette.
K = (20, 16, 28)        # outline
ROBE = (235, 140, 40)   # orange robe
ROBE_D = (170, 90, 30)
SKIN = (240, 200, 150)
SASH = (150, 40, 40)
WHITE = (250, 245, 230)
FX = (255, 240, 160)
SHELL = (110, 100, 90)
SHELL_D = (70, 62, 56)
HOLLOW = (10, 8, 12)
RED = (255, 90, 60)
WR = (140, 230, 255)
WR_D = (70, 150, 200)
DUM = (150, 150, 150)
DUM_D = (100, 100, 100)
WOOD = (150, 100, 60)


class Frame:
    def __init__(self, w, h):
        self.im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        self.d = ImageDraw.Draw(self.im)

    def r(self, x0, y0, x1, y1, c):
        """Filled rectangle, inclusive coordinates."""
        self.d.rectangle([x0, y0, x1, y1], fill=c + (255,))

    def px(self, x, y, c):
        if 0 <= x < self.im.width and 0 <= y < self.im.height:
            self.im.putpixel((x, y), c + (255,))


def strip(name, w, h, frames):
    sheet = Image.new("RGBA", (w * len(frames), h), (0, 0, 0, 0))
    for i, f in enumerate(frames):
        sheet.paste(f.im, (i * w, 0))
    OUT.mkdir(parents=True, exist_ok=True)
    sheet.save(OUT / f"{name}.png")


# ---------------------------------------------------------------- Joseph (32x48, feet at bottom, hitbox 12 wide centred)
def joseph(bob=0, legs=0, lean=0, arm=None, crouch=0, eyes=True):
    """arm: None, 'punch' (forward), 'back', 'heavy', 'guard'.  legs: -1..1 stride phase."""
    f = Frame(32, 48)
    top = 8 + bob + crouch          # head top
    fy = 47                         # feet end here
    if legs == 0:
        f.r(12, 38, 15, fy, ROBE_D); f.r(17, 38, 20, fy, ROBE_D)
        f.r(11, fy - 1, 15, fy, K); f.r(17, fy - 1, 21, fy, K)
    else:
        a = legs * 3
        f.r(12 - a, 38, 15 - a, fy, ROBE_D); f.r(17 + a, 38, 20 + a, fy, ROBE_D)
        f.r(11 - a, fy - 1, 15 - a, fy, K); f.r(17 + a, fy - 1, 22 + a, fy, K)
    # Robe body and sash
    f.r(10 + lean, top + 12, 21 + lean, 39, ROBE)
    f.r(10 + lean, top + 12, 11 + lean, 39, ROBE_D)
    f.r(10 + lean, top + 24, 21 + lean, top + 25, SASH)
    # Head (shaved), eye on the FRONT (right) side
    f.r(12 + lean, top, 19 + lean, top + 9, SKIN)
    f.r(12 + lean, top, 19 + lean, top, K)
    if eyes:
        f.r(18 + lean, top + 4, 19 + lean, top + 5, K)
    # Arms
    ay = top + 14
    if arm == "punch":
        f.r(20 + lean, ay, 29 + lean, ay + 3, ROBE); f.r(29 + lean, ay, 31 + lean, ay + 3, SKIN)
    elif arm == "back":
        f.r(6 + lean, ay, 11 + lean, ay + 3, ROBE); f.r(4 + lean, ay, 6 + lean, ay + 3, SKIN)
    elif arm == "heavy":
        f.r(20 + lean, ay - 4, 26 + lean, ay + 5, ROBE); f.r(26 + lean, ay - 6, 31 + lean, ay + 7, SKIN)
    elif arm == "guard":
        f.r(18 + lean, ay - 6, 23 + lean, ay + 8, ROBE); f.r(19 + lean, ay - 8, 24 + lean, ay - 6, SKIN)
    else:
        f.r(9 + lean, ay + 2, 11 + lean, ay + 9, ROBE); f.r(20 + lean, ay + 2, 22 + lean, ay + 9, ROBE)
    return f


def make_joseph():
    strip("joseph_idle", 32, 48, [joseph(0), joseph(1)])
    strip("joseph_walk", 32, 48, [joseph(0, 1), joseph(1, 0), joseph(0, -1), joseph(1, 0)])
    strip("joseph_jump", 32, 48, [joseph(-2, 0, arm="back")])
    strip("joseph_light", 32, 48, [joseph(0, arm="back", lean=-1), joseph(0, arm="punch", lean=1), joseph(0, arm="punch")])
    strip("joseph_heavy", 32, 48, [joseph(1, arm="back", lean=-2, crouch=2), joseph(0, arm="back", lean=-2),
                                   joseph(0, arm="heavy", lean=2), joseph(0, arm="heavy", lean=1)])
    strip("joseph_block", 32, 48, [joseph(2, arm="guard", crouch=1)])
    strip("joseph_hurt", 32, 48, [joseph(1, lean=-3, eyes=False)])
    s1 = joseph(0, arm="guard")
    s2 = joseph(0, arm="guard")
    for fr, rad in ((s1, 3), (s2, 6)):
        for k in range(rad):
            fr.px(8 - k * 2, 30 + k, FX); fr.px(24 + k * 2, 14 - k, FX); fr.px(16, 4 - k % 4, FX)
    strip("joseph_shift", 32, 48, [s1, s2])
    d = Frame(32, 48)
    d.r(2, 41, 29, 47, ROBE); d.r(2, 41, 29, 41, ROBE_D)
    d.r(26, 39, 31, 46, SKIN); d.r(5, 44, 8, 47, K)
    d.r(14, 43, 17, 44, SASH)
    strip("joseph_dead", 32, 48, [d])


# ---------------------------------------------------------------- Shell (32x48): a hunched hollow body
def shell(legs=0, arm="down", hunch=0, white=False, eye=True):
    f = Frame(32, 48)
    body = WHITE if white else SHELL
    fy = 47
    a = legs * 2
    f.r(11 - a, 36, 15 - a, fy, SHELL_D); f.r(17 + a, 36, 21 + a, fy, SHELL_D)
    f.r(10 - a, fy - 1, 15 - a, fy, K); f.r(17 + a, fy - 1, 22 + a, fy, K)
    f.r(9 + hunch, 14, 22 + hunch, 37, body)             # wide torso
    f.r(9 + hunch, 14, 10 + hunch, 37, SHELL_D)
    f.r(11 + hunch * 2, 6, 20 + hunch * 2, 15, body)     # head, forward of body
    f.r(11 + hunch * 2, 6, 20 + hunch * 2, 6, K)
    if eye:
        f.r(16 + hunch * 2, 9, 19 + hunch * 2, 11, HOLLOW)   # hollow eye on the FRONT side
    if arm == "down":
        f.r(7, 18, 9, 34, SHELL_D); f.r(22, 18, 24, 34, SHELL_D)
    elif arm == "raised":
        f.r(22, 6, 25, 20, SHELL_D); f.r(23, 2, 28, 6, SHELL_D)
    elif arm == "strike":
        f.r(22, 20, 31, 24, SHELL_D); f.r(28, 18, 31, 26, RED)
    return f


def make_shell():
    strip("shell_walk", 32, 48, [shell(1), shell(0, hunch=1), shell(-1), shell(0, hunch=1)])
    strip("shell_windup", 32, 48, [shell(0, "raised"), shell(0, "raised", hunch=-1)])
    strip("shell_attack", 32, 48, [shell(0, "strike", hunch=1), shell(0, "strike", hunch=2)])
    strip("shell_hurt", 32, 48, [shell(0, "down", hunch=-2, white=True, eye=False)])


# ---------------------------------------------------------------- Wraith (24x32): wispy ghost, tail points down
def wraith(wave=0, lunge=False):
    f = Frame(24, 32)
    f.r(6, 6, 17, 22, WR)
    f.r(7, 4, 16, 5, WR); f.r(8, 3, 15, 3, WR)
    f.r(6, 6, 7, 22, WR_D)
    # wisp tail below the hitbox (hitbox bottom is y=24)
    f.r(8 + wave, 23, 15 + wave, 26, WR_D)
    f.r(10 - wave, 27, 13 - wave, 29, WR_D)
    f.r(11 + wave, 30, 12 + wave, 31, WR_D)
    # eyes on the FRONT (right) side
    if lunge:
        f.r(11, 10, 16, 14, HOLLOW)
        f.r(0, 12, 5, 13, WR_D); f.r(1, 16, 5, 16, WR_D)   # speed lines behind
    else:
        f.r(12, 10, 14, 13, HOLLOW)
    return f


def make_wraith():
    strip("wraith_float", 24, 32, [wraith(0), wraith(1)])
    strip("wraith_lunge", 24, 32, [wraith(0, True), wraith(1, True)])


# ---------------------------------------------------------------- Dummy (24x32)
def dummy(white=False, tilt=0):
    f = Frame(24, 32)
    c = WHITE if white else DUM
    f.r(11, 6, 12, 31, WOOD)                      # post
    f.r(5 + tilt, 8, 18 + tilt, 25, c)            # padded torso
    f.r(5 + tilt, 8, 6 + tilt, 25, DUM_D)
    f.r(8 + tilt, 0, 15 + tilt, 7, c)             # head
    f.r(7, 30, 16, 31, K)                         # foot
    f.r(5 + tilt, 8, 18 + tilt, 8, K)
    return f


def make_dummy():
    strip("dummy_idle", 24, 32, [dummy()])
    strip("dummy_hurt", 24, 32, [dummy(True, 2)])


if __name__ == "__main__":
    if OUT.exists() and any(OUT.glob("*.png")) and "--force" not in sys.argv:
        print("Sprites already exist in", OUT, "- not overwriting (use --force to replace them).")
        sys.exit(0)
    make_joseph(); make_shell(); make_wraith(); make_dummy()
    print("Wrote", len(list(OUT.glob("*.png"))), "placeholder strips to", OUT)

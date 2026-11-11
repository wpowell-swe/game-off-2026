#!/usr/bin/env python3
"""Draws simple placeholder pixel-art strips into ShiftJamContent/Sprites/<character>/<animation>.png.

These exist only so the game shows *something* before the real art arrives. Replace any of them by
saving your own PNG with the same name (see docs/ART_AND_GIT_GUIDE.md). Needs Pillow.
Frame sizes must match ShiftJamContent/Data/sprites.json. Art faces RIGHT.
Usage: python3 tools/make_placeholder_sprites.py [--force]   (only missing files are written, unless --force)
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


FORCE = "--force" in sys.argv


def strip(name, w, h, frames):
    """name is character_animation, written to Sprites/<character>/<animation>.png."""
    character, animation = name.split("_", 1)
    target = OUT / character / f"{animation}.png"
    if target.exists() and not FORCE:
        return                                     # never overwrite real art
    sheet = Image.new("RGBA", (w * len(frames), h), (0, 0, 0, 0))
    for i, f in enumerate(frames):
        sheet.paste(f.im, (i * w, 0))
    target.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(target)


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


# ---------------------------------------------------------------- Hollowed (32x48): a townsperson, drab, guard up
HOL = (120, 100, 82)
HOL_D = (84, 70, 58)


def hollowed(legs=0, arm="down", slump=0, white=False, sit=False):
    f = Frame(32, 48)
    body = WHITE if white else HOL
    fy = 47
    if sit:
        f.r(9, 34, 22, fy, body); f.r(9, 34, 10, fy, HOL_D)
        f.r(11, 26, 20, 35, body); f.r(16, 29, 19, 30, K)
        return f
    a = legs * 2
    f.r(11 - a, 36, 15 - a, fy, HOL_D); f.r(17 + a, 36, 21 + a, fy, HOL_D)
    f.r(10 - a, fy - 1, 15 - a, fy, K); f.r(17 + a, fy - 1, 22 + a, fy, K)
    f.r(10, 16 + slump, 21, 37, body); f.r(10, 16 + slump, 11, 37, HOL_D)
    f.r(12, 7 + slump, 19, 16 + slump, body); f.r(16, 10 + slump, 19, 12 + slump, HOLLOW)
    if arm == "down":
        f.r(8, 20, 9, 34, HOL_D); f.r(22, 20, 23, 34, HOL_D)
    elif arm == "raised":
        f.r(22, 8, 25, 22, HOL_D); f.r(23, 4, 28, 8, HOL_D)
    elif arm == "strike":
        f.r(22, 22, 31, 26, HOL_D); f.r(28, 20, 31, 28, RED)
    return f


def make_hollowed():
    strip("hollowed_idle", 32, 48, [hollowed(0), hollowed(0, slump=1)])
    strip("hollowed_walk", 32, 48, [hollowed(1), hollowed(0, slump=1), hollowed(-1), hollowed(0, slump=1)])
    strip("hollowed_windup", 32, 48, [hollowed(0, "raised"), hollowed(0, "raised", slump=-1)])
    strip("hollowed_attack", 32, 48, [hollowed(0, "strike", slump=1), hollowed(0, "strike", slump=2)])
    strip("hollowed_hurt", 32, 48, [hollowed(0, "down", slump=-2, white=True)])
    strip("hollowed_broken", 32, 48, [hollowed(0, "down", slump=6), hollowed(0, "down", slump=7)])
    strip("hollowed_freed", 32, 48, [hollowed(sit=True)])


# ---------------------------------------------------------------- Soul (24x32): a small pale wisp
def soul(wave=0):
    f = Frame(24, 32)
    f.r(7, 4, 16, 22, WR_D)
    f.r(8, 1, 15, 12, WR)
    f.r(9, 12, 14, 22, WR)
    f.r(10 + wave, 22, 13 + wave, 27, WR_D)
    f.r(11 - wave, 28, 12 - wave, 31, WR_D)
    f.r(10, 5, 11, 6, HOLLOW); f.r(13, 5, 14, 6, HOLLOW)
    return f


def make_soul():
    strip("soul_float", 24, 32, [soul(0), soul(1)])


# ---------------------------------------------------------------- Flicker (32x48): a thin figure that shimmers
FL = (220, 140, 80)
FL_D = (150, 90, 50)


def flicker(legs=0, arm="down", white=False):
    f = Frame(32, 48)
    body = WHITE if white else FL
    fy = 47
    a = legs * 2
    f.r(12 - a, 34, 14 - a, fy, FL_D); f.r(18 + a, 34, 20 + a, fy, FL_D)
    f.r(10, 16, 21, 35, body); f.r(10, 16, 11, 35, FL_D)
    f.r(12, 6, 19, 16, body); f.r(16, 9, 19, 11, K)
    if arm == "down":
        f.r(8, 20, 9, 32, FL_D); f.r(22, 20, 23, 32, FL_D)
    elif arm == "raised":
        f.r(22, 8, 24, 22, FL_D); f.r(22, 4, 27, 8, FL_D)
    elif arm == "strike":
        f.r(22, 20, 31, 24, FL_D); f.r(28, 18, 31, 26, RED)
    return f


def make_flicker():
    strip("flicker_idle", 32, 48, [flicker(0), flicker(0)])
    strip("flicker_walk", 32, 48, [flicker(1), flicker(0), flicker(-1), flicker(0)])
    strip("flicker_windup", 32, 48, [flicker(0, "raised"), flicker(0, "raised")])
    strip("flicker_attack", 32, 48, [flicker(0, "strike"), flicker(0, "strike")])
    strip("flicker_hurt", 32, 48, [flicker(0, white=True)])


# ---------------------------------------------------------------- Bosses: examples to paint over. Drawn facing RIGHT.
# One shared pose set per boss: idle, walk, windup (the telegraph), attack, hurt, freed.
def boss_frame(w, h, cx, body_w, body_h, coat, trim, skin, hair, pose="idle", bob=0, legs=0, flash=False, tall_hat=False,
               apron=None, extra=None):
    """A standing figure: feet on the bottom edge, centred on cx. pose: idle, walk, windup, attack, hurt, freed."""
    f = Frame(w, h)
    fy = h - 1
    c = WHITE if flash else coat
    x0 = cx - body_w // 2
    if pose == "freed":                                    # sitting, relieved
        top = fy - body_h // 2
        f.r(x0, top + 6, x0 + body_w - 1, fy, c)
        f.r(x0 + 2, top - 6, x0 + body_w - 3, top + 5, skin)
        f.r(x0 + 2, top - 8, x0 + body_w - 3, top - 5, hair)
        f.r(cx + 2, top - 2, cx + 3, top - 1, K)
        return f
    lean = -2 if pose == "hurt" else (2 if pose == "attack" else 0)
    top = fy - body_h - bob
    a = legs * 2
    f.r(x0 + 3 - a, fy - 9, x0 + 6 - a, fy, K); f.r(x0 + body_w - 7 + a, fy - 9, x0 + body_w - 4 + a, fy, K)
    f.r(x0 + lean, top + 10, x0 + body_w - 1 + lean, fy - 6, c)                       # coat / dress
    f.r(x0 + lean, top + 10, x0 + 1 + lean, fy - 6, K)
    if apron:
        f.r(x0 + 3 + lean, top + 15, x0 + body_w - 4 + lean, fy - 7, apron)
        f.r(cx + 3 + lean, top + 18, cx + 6 + lean, top + 22, FX)                     # price tag
    f.r(x0 + 3 + lean, top + 10, x0 + body_w - 4 + lean, top + 13, trim)               # collar / shoulders
    f.r(x0 + 2 + lean, top, x0 + body_w - 3 + lean, top + 10, skin)                    # head
    f.r(x0 + 2 + lean, top - 1, x0 + body_w - 3 + lean, top + 2, hair)
    if tall_hat:
        f.r(x0 + 3 + lean, top - 9, x0 + body_w - 4 + lean, top - 1, coat)
    f.r(cx + 3 + lean, top + 4, cx + 5 + lean, top + 5, HOLLOW if pose != "hurt" else K)     # eye on the FRONT (right)
    ax = x0 + body_w + lean                                                           # hand on the front side
    if pose == "windup":
        f.r(ax - 2, top - 12, ax + 1, top + 12, skin)
    elif pose == "attack":
        f.r(ax - 2, top + 14, ax + 12, top + 18, skin)
    else:
        f.r(ax - 1, top + 14, ax + 2, top + 24, skin)
    if extra:
        extra(f, pose, ax, top)
    return f


def pell_prop(f, pose, ax, top):
    """The lantern pole: upright when idle, raised for the windup, forward for the strike."""
    if pose == "windup":
        f.r(ax + 1, top - 18, ax + 2, top + 12, WOOD); f.r(ax - 2, top - 24, ax + 5, top - 18, FX)
    elif pose == "attack":
        f.r(ax, top + 15, ax + 20, top + 16, WOOD); f.r(ax + 18, top + 11, ax + 24, top + 20, FX)
    else:
        f.r(ax + 1, top + 4, ax + 2, top + 36, WOOD); f.r(ax - 2, top, ax + 5, top + 6, FX)


def dova_prop(f, pose, ax, top):
    """A crate for the toss, a fist-hammer for the slam."""
    if pose == "windup":
        f.r(ax - 8, top - 16, ax + 6, top - 8, WOOD); f.r(ax - 8, top - 16, ax + 6, top - 15, ROBE_D)
    elif pose == "attack":
        f.r(ax + 12, top + 12, ax + 20, top + 20, SKIN)


def clerk_prop(f, pose, ax, top):
    """The big stamp: raised for the windup, slammed forward for the strike, a ledger under the arm otherwise."""
    if pose == "windup":
        f.r(ax - 4, top - 20, ax + 5, top - 14, SASH); f.r(ax, top - 14, ax + 1, top - 8, WOOD)
    elif pose == "attack":
        f.r(ax + 12, top + 12, ax + 20, top + 22, SASH)
    else:
        f.r(ax - 6, top + 16, ax + 1, top + 24, WHITE)


def make_boss(name, w, h, body_w, body_h, coat, trim, skin, hair, prop, tall_hat=False, apron=None):
    def fr(pose, bob=0, legs=0, flash=False):
        return boss_frame(w, h, w // 2, body_w, body_h, coat, trim, skin, hair, pose, bob, legs, flash, tall_hat, apron, prop)
    strip(f"{name}_idle", w, h, [fr("idle"), fr("idle", bob=1)])
    strip(f"{name}_walk", w, h, [fr("walk", 0, 1), fr("walk", 1, 0), fr("walk", 0, -1), fr("walk", 1, 0)])
    strip(f"{name}_windup", w, h, [fr("windup"), fr("windup", bob=1)])
    strip(f"{name}_attack", w, h, [fr("attack"), fr("attack", bob=1)])
    strip(f"{name}_hurt", w, h, [fr("hurt", flash=True)])
    strip(f"{name}_freed", w, h, [fr("freed")])


def make_bosses():
    # Frames and hitboxes match ShiftJamContent/Data/sprites.json.
    make_boss("pell", 48, 56, 18, 36, (130, 90, 60), (200, 160, 90), SKIN, (90, 62, 40), pell_prop)
    make_boss("dova", 48, 56, 22, 34, (160, 70, 55), (235, 220, 180), SKIN, (90, 55, 35), dova_prop, apron=(235, 220, 180))
    make_boss("clerk", 40, 56, 18, 30, (48, 46, 66), (225, 220, 205), (205, 200, 190), (48, 46, 66), clerk_prop, tall_hat=True)


if __name__ == "__main__":
    before = {p.relative_to(OUT).as_posix() for p in OUT.rglob("*.png")} if OUT.exists() else set()
    make_joseph(); make_shell(); make_wraith(); make_dummy(); make_hollowed(); make_soul(); make_flicker(); make_bosses()
    after = {p.relative_to(OUT).as_posix() for p in OUT.rglob("*.png")}
    print("Wrote", len(after - before), "new placeholder strips to", OUT, f"({len(after)} in total)")

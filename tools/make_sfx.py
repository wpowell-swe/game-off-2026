#!/usr/bin/env python3
"""Synthesises placeholder retro sound effects (16-bit mono 22050 Hz WAV) into ShiftJamContent/Sfx/.
Pure stdlib. Deterministic (seeded). Run from anywhere: python3 tools/make_sfx.py"""
import math, os, random, struct, wave

RATE = 22050
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "ShiftJamContent", "Sfx")


def render(dur, fn):
    n = int(RATE * dur)
    return [fn(i / RATE, i / max(1, n - 1)) for i in range(n)]


def env(t, u, attack=0.004, power=2.0):
    """Fast attack, then decay over the normalised time u."""
    return min(1.0, t / attack) * (1.0 - u) ** power


def square(ph, duty=0.5):
    return 1.0 if (ph % 1.0) < duty else -1.0


def sweep(dur, f0, f1, wave_fn, vol=0.6, power=1.5):
    ph = [0.0]

    def fn(t, u):
        f = f0 + (f1 - f0) * u
        ph[0] += f / RATE
        return wave_fn(ph[0]) * env(t, u, power=power) * vol
    return render(dur, fn)


def noise(dur, vol=0.6, power=2.0, lp=0.0, seed=1):
    rng = random.Random(seed)
    last = [0.0]

    def fn(t, u):
        x = rng.uniform(-1, 1)
        last[0] = last[0] * lp + x * (1 - lp)
        return last[0] * env(t, u, power=power) * vol
    return render(dur, fn)


def mix(*tracks):
    n = max(len(t) for t in tracks)
    return [sum(t[i] for t in tracks if i < len(t)) for i in range(n)]


sine = lambda p: math.sin(2 * math.pi * p)
saw = lambda p: 2 * (p % 1.0) - 1
tri = lambda p: 4 * abs((p % 1.0) - 0.5) - 1

SOUNDS = {
    "jump": lambda: sweep(0.18, 260, 620, lambda p: square(p, 0.25), 0.35),
    "light_hit": lambda: mix(noise(0.07, 0.6, 3, 0.3, 2), sweep(0.07, 380, 160, square, 0.3, 2.5)),
    "heavy_hit": lambda: mix(noise(0.28, 0.8, 2, 0.6, 3), sweep(0.28, 150, 45, sine, 0.9, 1.5)),
    "swing": lambda: noise(0.14, 0.4, 1.2, 0.85, 4),
    "shift_in": lambda: mix(sweep(0.45, 640, 70, saw, 0.4, 1.2), sweep(0.45, 320, 35, sine, 0.5, 1.2)),
    "shift_out": lambda: mix(sweep(0.35, 160, 900, tri, 0.5, 1.4), sweep(0.35, 320, 1800, sine, 0.2, 1.8)),
    "hurt": lambda: mix(sweep(0.3, 330, 90, lambda p: square(p, 0.4), 0.45, 1.5), noise(0.1, 0.4, 3, 0.2, 5)),
    "enemy_defeat": lambda: mix(sweep(0.5, 420, 40, square, 0.35, 1.3), noise(0.5, 0.45, 1.5, 0.7, 6)),
    "block": lambda: mix(sweep(0.09, 1250, 1100, lambda p: square(p, 0.2), 0.3, 3), noise(0.05, 0.4, 4, 0.0, 7)),
}


def write(name, samples):
    path = os.path.join(OUT, name + ".wav")
    with wave.open(path, "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(RATE)
        w.writeframes(b"".join(struct.pack("<h", int(max(-1, min(1, s)) * 30000)) for s in samples))
    print("%-14s %5.2fs %6d bytes" % (name, len(samples) / RATE, os.path.getsize(path)))


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    for k, f in SOUNDS.items():
        write(k, f())

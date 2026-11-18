#!/usr/bin/env python3
"""Synthesises looping chiptune music (16-bit mono 16000 Hz WAV) into ShiftJamContent/Music/.
Pure stdlib and deterministic (seeded). Each track is a short spec: tempo, scale, one chord per bar, a lead style and a drum
pattern; the melody is composed from the chords with a seeded random walk. Edit TRACKS and re-run:  python3 tools/make_music.py
Then run tools/sync_content.py (or just dotnet build) and map stages to tracks in ShiftJamContent/Data/music.json."""
import math, os, random, struct, wave, sys

RATE = 16000
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "ShiftJamContent", "Music")

MINOR = [0, 2, 3, 5, 7, 8, 10]
PHRYG = [0, 1, 3, 5, 7, 8, 10]
HARM = [0, 2, 3, 5, 7, 8, 11]

# chords are scale degrees (0 = root) of the triad built on that degree, one per bar (8 bars, loops)
TRACKS = {
    "title":  dict(bpm=84,  root=57, scale=MINOR, prog=[0, 5, 3, 4, 0, 5, 6, 4], lead="soft", drums="none",  seed=1, density=0.45),
    "street": dict(bpm=122, root=50, scale=MINOR, prog=[0, 0, 5, 6, 0, 0, 5, 4], lead="pulse", drums="rock", seed=2, density=0.75),
    "depths": dict(bpm=98,  root=52, scale=PHRYG, prog=[0, 0, 1, 0, 0, 5, 1, 0], lead="arp",  drums="sparse", seed=3, density=0.5),
    "boss":   dict(bpm=152, root=45, scale=HARM,  prog=[0, 5, 0, 4, 0, 5, 6, 4], lead="pulse", drums="drive", seed=4, density=0.85),
    "keep":   dict(bpm=106, root=54, scale=MINOR, prog=[0, 3, 5, 4, 0, 3, 6, 4], lead="pulse", drums="march", seed=5, density=0.65),
    "finale": dict(bpm=164, root=49, scale=HARM,  prog=[0, 6, 5, 4, 0, 6, 5, 4], lead="arp",  drums="drive", seed=6, density=0.9),
}


def hz(m):
    return 440.0 * 2 ** ((m - 69) / 12.0)


def degree(scale, root, d):
    """Midi note of scale degree d (may exceed 6 or be negative: wraps by octave)."""
    return root + scale[d % 7] + 12 * (d // 7)


def chord(scale, root, d):
    return [degree(scale, root, d), degree(scale, root, d + 2), degree(scale, root, d + 4)]


def square(ph, duty): return 1.0 if (ph % 1.0) < duty else -1.0
def tri(ph): return 4 * abs((ph % 1.0) - 0.5) - 1


class Mix:
    def __init__(self, n): self.n = n; self.buf = [0.0] * n

    def add(self, start_s, dur_s, fn, vol):
        s0 = int(start_s * RATE); n = int(dur_s * RATE)
        for i in range(n):
            self.buf[(s0 + i) % self.n] += fn(i / RATE, i / max(1, n)) * vol   # wraps, so the loop has no seam


def voice(kind, freq, vibrato=0.0):
    def fn(t, u):
        f = freq * (1 + vibrato * math.sin(t * 38.0) * min(1.0, t * 4))
        ph = f * t
        a = min(1.0, t / 0.004) * (1 - u) ** 0.7
        if kind == "pulse": return square(ph, 0.25) * a
        if kind == "soft": return (square(ph, 0.5) * 0.5 + tri(ph) * 0.5) * a
        if kind == "arp": return square(ph, 0.125) * a * (1 - u) ** 0.8
        if kind == "bass": return tri(ph) * min(1.0, t / 0.003) * (1 - u) ** 0.35
        return 0
    return fn


def kick(t, u): return math.sin(2 * math.pi * (50 + 110 * math.exp(-t * 28)) * t) * (1 - u) ** 2


def make_noise(seed):
    rng = random.Random(seed)
    return lambda t, u, p=3.0: rng.uniform(-1, 1) * (1 - u) ** p


def render_track(name, spec):
    rng = random.Random(spec["seed"])
    beat = 60.0 / spec["bpm"]
    bars = len(spec["prog"])
    total = bars * 4 * beat
    mix = Mix(int(total * RATE))
    sc, root = spec["scale"], spec["root"]
    noise = make_noise(spec["seed"] * 11)

    # bass: eighth-note pulse on the chord root, an octave jump on the off-beat for drive
    bass_style = "walk" if spec["drums"] in ("drive", "rock") else "long"
    for b, d in enumerate(spec["prog"]):
        r = degree(sc, root, d) - 12
        t0 = b * 4 * beat
        if bass_style == "walk":
            for i in range(8):
                note = r + (12 if i % 4 == 3 else 0) + (7 if i == 6 else 0)
                mix.add(t0 + i * beat / 2, beat / 2 * 0.9, voice("bass", hz(note)), 0.34)
        else:
            mix.add(t0, beat * 2, voice("bass", hz(r)), 0.34)
            mix.add(t0 + 2 * beat, beat * 2, voice("bass", hz(r + (7 if b % 2 else 0))), 0.30)

    # lead / arpeggio
    motif = None
    for b, d in enumerate(spec["prog"]):
        ch = chord(sc, root + 12, d)
        t0 = b * 4 * beat
        if spec["lead"] == "arp":
            order = [0, 1, 2, 1, 2, 1, 2, 1] if b % 2 == 0 else [0, 2, 1, 2, 0, 2, 1, 2]
            step = beat / 4
            for i in range(16):
                n = ch[order[i % 8]] + (12 if i % 8 >= 6 and spec["drums"] == "drive" else 0)
                mix.add(t0 + i * step, step * 0.85, voice("arp", hz(n)), 0.12)
            # a slow melody over the arpeggio
            if b % 2 == 0:
                for q in range(2):
                    n = ch[(q + b // 2) % 3] + 12
                    mix.add(t0 + q * 2 * beat, 1.9 * beat, voice("pulse", hz(n), 0.004), 0.16)
        else:
            if b % 4 == 0 or motif is None:
                motif = []
                pos = rng.choice([0, 2, 4])
                for i in range(8):
                    if rng.random() > spec["density"] and i % 2 == 1:
                        motif.append(None); continue
                    pos += rng.choice([-2, -1, -1, 0, 1, 1, 2])
                    pos = max(-1, min(9, pos))
                    if i % 4 == 0: pos = min(range(-1, 10), key=lambda p, pos=pos: abs(p - pos) + (0 if (p % 7) in (0, 2, 4) else 3))
                    motif.append(pos)
            m = motif if b % 4 < 2 else [x if x is None else x + (1 if i > 4 else 0) for i, x in enumerate(motif)]
            for i, p in enumerate(m):
                if p is None: continue
                note = degree(sc, root + 12, p + (d if b % 2 else 0) * 0)
                dur = beat / 2 * (1.9 if (i + 1 < 8 and m[i + 1] is None) else 0.92)
                mix.add(t0 + i * beat / 2, dur, voice(spec["lead"], hz(note), 0.006), 0.17)
            if spec["lead"] != "soft":
                for k in ch[1:]:
                    mix.add(t0, 4 * beat, voice("soft", hz(k - 12)), 0.04)   # quiet sustained pad under the melody
            else:
                for k in ch:
                    mix.add(t0, 4 * beat, voice("soft", hz(k - 12)), 0.06)

    # drums
    pat = spec["drums"]
    for b in range(bars):
        t0 = b * 4 * beat
        for i in range(8):
            t = t0 + i * beat / 2
            on_beat = i % 2 == 0
            if pat == "none": continue
            if pat in ("rock", "drive", "march"):
                if i in (0, 4) or (pat == "drive" and i in (3, 6)): mix.add(t, 0.12, kick, 0.5)
                if i in (2, 6): mix.add(t, 0.1, lambda tt, u: noise(tt, u, 2.0), 0.22)
                if pat == "drive" or on_beat: mix.add(t, 0.04, lambda tt, u: noise(tt, u, 4.0), 0.07)
            elif pat == "sparse":
                if i == 0: mix.add(t, 0.14, kick, 0.5)
                if i == 4 and b % 2: mix.add(t, 0.1, lambda tt, u: noise(tt, u, 2.0), 0.18)
                if i % 2 == 1: mix.add(t, 0.03, lambda tt, u: noise(tt, u, 4.0), 0.05)

    peak = max(1e-6, max(abs(x) for x in mix.buf))
    gain = 0.85 / peak if peak > 0.85 else 1.0
    return [x * gain for x in mix.buf], total


def write(name, samples, total):
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name + ".wav")
    with wave.open(path, "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(RATE)
        w.writeframes(b"".join(struct.pack("<h", int(max(-1, min(1, s)) * 30000)) for s in samples))
    print("%-8s %5.1fs %7d bytes" % (name, total, os.path.getsize(path)), flush=True)


if __name__ == "__main__":
    names = sys.argv[1:] or list(TRACKS)
    for n in names:
        s, total = render_track(n, TRACKS[n])
        write(n, s, total)

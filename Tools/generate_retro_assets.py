#!/usr/bin/env python3
"""Generates the derived art and every sound effect for the slot machine.

Run from the repository root:  python3 Tools/generate_retro_assets.py
Requires Pillow for the image steps (pip install pillow). Audio uses only the
standard library: every effect is synthesised from square, triangle and noise
waves so it matches the retro look and carries no licensing questions.

Outputs
  Assets/UI/Symbols/Blur/*_blur.png   vertical motion-blur copies of each symbol
  Assets/UI/Machine/bulb.png          8x8 marquee bulb (tinted on/off in Unity)
  Assets/UI/Machine/pixel_white.png   4x4 white pixel for bars and overlays
  Assets/UI/Machine/frame_glow.png    9-slice frame for the anticipation glow
  Assets/UI/Machine/jackpot_sign_glow.png  the JACKPOT letters as a white overlay
  Assets/UI/Buttons/button_gold_*_sheet.png  blank gold buttons (wide and square), four states
  Assets/UI/Buttons/icons_toggle_sheet.png   16x16 sound and pixel-filter icons, on and off
  Assets/Sounds/*.wav                 22.05 kHz mono 16-bit effects
"""
import math
import os
import random
import struct
import wave

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SYMBOLS = os.path.join(ROOT, "Assets", "UI", "Symbols")
MACHINE = os.path.join(ROOT, "Assets", "UI", "Machine")
BUTTONS = os.path.join(ROOT, "Assets", "UI", "Buttons")
SOUNDS = os.path.join(ROOT, "Assets", "Sounds")
RATE = 22050


# ---------------------------------------------------------------- images ----

def make_blurred_symbols():
    from PIL import Image

    out_dir = os.path.join(SYMBOLS, "Blur")
    os.makedirs(out_dir, exist_ok=True)
    taps = 11  # blur length in pixels, tuned to read as fast vertical motion
    for name in ("symbol_seven", "symbol_cherry", "symbol_bell", "symbol_bar"):
        src = Image.open(os.path.join(SYMBOLS, name + ".png")).convert("RGBA")
        w, h = src.size
        px = src.load()
        dst = Image.new("RGBA", (w, h))
        dp = dst.load()
        half = taps // 2
        for x in range(w):
            for y in range(h):
                r = g = b = a = 0.0
                for k in range(-half, half + 1):
                    yy = min(max(y + k, 0), h - 1)
                    pr, pg, pb, pa = px[x, yy]
                    # premultiply so transparent pixels don't darken the edges
                    r += pr * pa; g += pg * pa; b += pb * pa; a += pa
                if a > 0:
                    dp[x, y] = (int(r / a), int(g / a), int(b / a), int(a / taps))
        dst.save(os.path.join(out_dir, name + "_blur.png"))
    print("blurred symbols ->", out_dir)


def make_bulb_and_pixel():
    from PIL import Image

    # 8x8 pixel-art bulb: white body with a soft rim, tinted in Unity.
    rows = [
        "..XXXX..",
        ".XHHXXX.",
        "XHHXXXXX",
        "XHXXXXXX",
        "XXXXXXXo",
        "XXXXXXoo",
        ".XXXXoo.",
        "..oooo..",
    ]
    colours = {"X": (255, 255, 255, 255), "H": (255, 255, 255, 255), "o": (190, 190, 190, 255), ".": (0, 0, 0, 0)}
    bulb = Image.new("RGBA", (8, 8))
    for y, row in enumerate(rows):
        for x, c in enumerate(row):
            bulb.putpixel((x, y), colours[c])
    bulb.save(os.path.join(MACHINE, "bulb.png"))
    Image.new("RGBA", (4, 4), (255, 255, 255, 255)).save(os.path.join(MACHINE, "pixel_white.png"))

    # 12x12 frame with a 3 px border and a clear centre, 9-sliced in Unity
    # into the glowing outline drawn round a reel during anticipation.
    frame = Image.new("RGBA", (12, 12), (0, 0, 0, 0))
    for y in range(12):
        for x in range(12):
            edge = min(x, y, 11 - x, 11 - y)
            if edge < 3:
                frame.putpixel((x, y), (255, 255, 255, 255 if edge < 2 else 140))
    frame.save(os.path.join(MACHINE, "frame_glow.png"))
    print("bulb, white pixel, frame glow ->", MACHINE)


def make_sign_glow():
    """Cuts the orange JACKPOT letters out of the machine art as a white overlay,
    so the sign can light up letter by letter instead of flashing a box."""
    from PIL import Image

    src = Image.open(os.path.join(MACHINE, "machine_frame.png")).convert("RGBA")
    w, h = src.size
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    sp, op = src.load(), out.load()
    for y in range(0, 160):  # the sign sits in the top band of the cabinet
        for x in range(w):
            r, g, b, a = sp[x, y]
            orange = a > 0 and r > 180 and 80 < g < 210 and b < 140 and r - b > 90
            if orange:
                op[x, y] = (255, 246, 214, 255)
    out.save(os.path.join(MACHINE, "jackpot_sign_glow.png"))
    print("sign glow ->", MACHINE)


def make_buttons():
    """Blank gold buttons cut from the provided YES/NO art (so a label can be
    drawn on top in Unity), a square version for icon buttons, and the pixel
    icons for the sound and filter toggles."""
    from PIL import Image

    src = Image.open(os.path.join(BUTTONS, "button_yes_no_sheet.png")).convert("RGBA")
    # The YES button in each state row (normal, highlighted, pressed), measured on the sheet.
    faces = [src.crop((70, top, 441, top + 142)) for top in (83, 274, 465)]

    blanks = []
    for face in faces:
        # The face shades only from top to bottom, so copying a clean column
        # across the lettering erases it without a seam.
        px = face.load()
        w, h = face.size
        for x in range(110, 260):
            for y in range(h):
                px[x, y] = px[50, y]
        blanks.append(face)

    # Disabled: the normal face in grey, a little darker.
    grey = blanks[0].convert("LA").convert("RGBA")
    gp = grey.load()
    for y in range(grey.size[1]):
        for x in range(grey.size[0]):
            r, g, b, a = gp[x, y]
            gp[x, y] = (int(r * 0.75), int(g * 0.75), int(b * 0.8), a)
    blanks.append(grey)

    w, h = blanks[0].size
    wide = Image.new("RGBA", (w, h * 4), (0, 0, 0, 0))
    square = Image.new("RGBA", (h, h * 4), (0, 0, 0, 0))
    half = h // 2
    for row, face in enumerate(blanks):
        wide.paste(face, (0, row * h))
        # Keep both rounded ends and drop the middle for a square button.
        square.paste(face.crop((0, 0, half, h)), (0, row * h))
        square.paste(face.crop((w - (h - half), 0, w, h)), (half, row * h))
    wide.save(os.path.join(BUTTONS, "button_gold_wide_sheet.png"))
    square.save(os.path.join(BUTTONS, "button_gold_square_sheet.png"))

    # 16x16 pixel icons, white with a dark drop shadow like the YES/NO lettering.
    speaker = [
        ".....X",
        "....XX",
        "...XXX",
        "XXXXXX",
        "XXXXXX",
        "XXXXXX",
        "XXXXXX",
        "XXXXXX",
        "XXXXXX",
        "...XXX",
        "....XX",
        ".....X",
    ]
    waves = [(8, 3), (9, 4), (9, 5), (9, 6), (9, 7), (8, 8),
             (10, 1), (11, 2), (12, 3), (12, 4), (12, 5), (12, 6), (12, 7), (12, 8), (11, 9), (10, 10)]
    cross = [(8 + i, 3 + i) for i in range(5)] + [(12 - i, 3 + i) for i in range(5)]
    speaker_px = [(x, y) for y, row in enumerate(speaker) for x, c in enumerate(row) if c == "X"]
    checker = [(x, y) for y in range(12) for x in range(12) if (x // 3 + y // 3) % 2 == 0]
    slash = [(11 - i, i) for i in range(12)] + [(12 - i, i) for i in range(1, 12)]

    def icon(pixels, dim=()):
        img = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
        for x, y in list(pixels) + list(dim):
            img.putpixel((x + 2, y + 3), (36, 18, 4, 230))  # shadow, one pixel down-right
        for x, y in dim:
            img.putpixel((x + 1, y + 2), (255, 255, 255, 110))
        for x, y in pixels:
            img.putpixel((x + 1, y + 2), (255, 255, 255, 255))
        return img

    icons = [
        icon(speaker_px + waves),                     # sound on
        icon(speaker_px + cross),                     # sound off
        icon(checker),                                # pixel filter on
        icon(slash, dim=[p for p in checker if p not in slash]),  # pixel filter off
    ]
    sheet = Image.new("RGBA", (16 * len(icons), 16), (0, 0, 0, 0))
    for i, img in enumerate(icons):
        sheet.paste(img, (i * 16, 0))
    sheet.save(os.path.join(BUTTONS, "icons_toggle_sheet.png"))
    print("gold buttons, toggle icons ->", BUTTONS)


# ----------------------------------------------------------------- audio ----

def square(phase, duty=0.5):
    return 1.0 if (phase % 1.0) < duty else -1.0


def triangle(phase):
    p = phase % 1.0
    return 4.0 * p - 1.0 if p < 0.5 else 3.0 - 4.0 * p


def env(t, length, attack=0.004, release=None):
    """Linear attack, exponential-ish decay to silence at `length`."""
    if t < attack:
        return t / attack
    release = release if release is not None else length
    x = max(0.0, 1.0 - (t - attack) / max(1e-6, release - attack))
    return x * x


class Track:
    def __init__(self, seconds):
        self.samples = [0.0] * int(seconds * RATE)

    def add(self, start, length, fn, gain=0.5):
        """Mixes fn(t, i) for `length` seconds starting at `start`."""
        s0 = int(start * RATE)
        for i in range(int(length * RATE)):
            j = s0 + i
            if j >= len(self.samples):
                break
            self.samples[j] += gain * fn(i / RATE, i)

    def note(self, start, length, freq, gain=0.35, wave_fn=square, duty=0.5, slide=0.0, vibrato=0.0):
        phase = [0.0]

        def fn(t, i):
            f = freq * (1.0 + slide * t / length) * (1.0 + vibrato * math.sin(2 * math.pi * 6 * t))
            phase[0] += f / RATE
            v = wave_fn(phase[0]) if wave_fn is triangle else wave_fn(phase[0], duty)
            return v * env(t, length)
        self.add(start, length, fn, gain)

    def noise(self, start, length, gain=0.3, seed=1, decay=True):
        rng = random.Random(seed)
        self.add(start, length, lambda t, i: rng.uniform(-1, 1) * (env(t, length) if decay else 1.0), gain)

    def save(self, name):
        peak = max(1e-6, max(abs(s) for s in self.samples))
        scale = 0.89 / peak if peak > 0.89 else 1.0
        path = os.path.join(SOUNDS, name + ".wav")
        with wave.open(path, "wb") as w:
            w.setnchannels(1)
            w.setsampwidth(2)
            w.setframerate(RATE)
            w.writeframes(b"".join(struct.pack("<h", int(max(-1, min(1, s * scale)) * 32767)) for s in self.samples))
        return path


NOTE = {n: 440.0 * 2 ** ((i - 9) / 12) for i, n in enumerate(["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"])}


def hz(name, octave):
    return NOTE[name] * 2 ** (octave - 4)


def make_sounds():
    os.makedirs(SOUNDS, exist_ok=True)

    t = Track(0.03); t.note(0, 0.025, 1400, 0.5, duty=0.25); t.save("reel_tick")

    t = Track(0.16)
    t.note(0, 0.14, 160, 0.6, duty=0.5, slide=-0.55)
    t.noise(0, 0.05, 0.35, seed=2)
    t.save("reel_stop")

    t = Track(0.42)
    for k in range(6):  # ratchet clicks while the lever comes down
        t.noise(k * 0.035, 0.018, 0.4, seed=10 + k)
        t.note(k * 0.035, 0.02, 900 - k * 90, 0.25, duty=0.2)
    t.note(0.24, 0.16, 120, 0.6, slide=-0.4)
    t.noise(0.24, 0.06, 0.4, seed=40)
    t.save("lever_pull")

    t = Track(0.03); t.note(0, 0.028, 660, 0.45, duty=0.25); t.save("button_click")

    t = Track(0.12)
    t.note(0, 0.04, hz("B", 5), 0.35, duty=0.25)
    t.note(0.04, 0.08, hz("E", 6), 0.35, duty=0.25)
    t.save("coin_tick")

    t = Track(0.62)
    for k, (n, o) in enumerate([("C", 5), ("E", 5), ("G", 5), ("C", 6)]):
        t.note(k * 0.085, 0.26 if k == 3 else 0.12, hz(n, o), 0.35, duty=0.5)
    t.save("win_small")

    t = Track(1.5)
    seq = [("C", 5), ("E", 5), ("G", 5), ("C", 6), ("E", 6), ("G", 6)]
    for k, (n, o) in enumerate(seq):
        t.note(k * 0.07, 0.1, hz(n, o), 0.3, duty=0.25)
    for n, o in [("C", 5), ("E", 5), ("G", 5)]:
        t.note(0.45, 1.0, hz(n, o), 0.22, duty=0.5, vibrato=0.006)
    t.note(0.45, 1.0, hz("C", 3), 0.3, wave_fn=triangle)
    t.save("win_big")

    t = Track(2.6)
    melody = [("G", 4, 0.12), ("C", 5, 0.12), ("E", 5, 0.12), ("G", 5, 0.24), ("E", 5, 0.12), ("G", 5, 0.5),
              ("A", 5, 0.12), ("B", 5, 0.12), ("C", 6, 0.9)]
    at = 0.0
    for n, o, d in melody:
        t.note(at, d + 0.04, hz(n, o), 0.32, duty=0.5, vibrato=0.004 if d > 0.3 else 0.0)
        t.note(at, d + 0.04, hz(n, o - 2), 0.28, wave_fn=triangle)
        at += d
    for k in range(10):  # coin shower on top
        t.note(1.0 + k * 0.11, 0.09, hz("E", 6) * (1 + 0.03 * (k % 3)), 0.12, duty=0.25)
    t.save("jackpot")

    t = Track(1.0)
    for k in range(12):
        f = hz("C", 5) * 2 ** (((k * 5) % 12) / 12) * (2 if k > 5 else 1)
        t.note(k * 0.06, 0.18, f, 0.22, duty=0.125)
    t.note(0.72, 0.28, hz("C", 7), 0.18, duty=0.125)
    t.save("free_spins")

    t = Track(1.3)
    t.note(0, 1.3, 260, 0.3, duty=0.5, slide=2.2, vibrato=0.03)
    t.note(0, 1.3, 130, 0.25, wave_fn=triangle, slide=2.2)
    t.save("anticipation")

    t = Track(0.45)
    t.note(0, 0.18, hz("E", 4), 0.35, duty=0.5)
    t.note(0.18, 0.26, hz("C", 4), 0.35, duty=0.5, slide=-0.1)
    t.save("no_credits")

    t = Track(0.2)
    t.note(0, 0.18, 400, 0.25, duty=0.5, slide=1.2)
    t.save("popup_open")

    print("sounds ->", SOUNDS)


if __name__ == "__main__":
    make_blurred_symbols()
    make_bulb_and_pixel()
    make_sign_glow()
    make_buttons()
    make_sounds()

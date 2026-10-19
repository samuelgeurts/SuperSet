#!/usr/bin/env python3
"""Makes the WAV files of the game sounds in Assets/Resources/Audio/Sfx.

The game plays them through AudioManager by name, so a sound can be replaced by putting another
WAV file with the same name in that folder. Run this script only to bring back the original sounds:

    python tools/make-sounds.py [output folder]

16 bit, mono, 44100 Hz. Everything is made from sine waves and noise, so the result is always the same.

    card-deal      a card slides over the table and lands with a soft tap
    card-discard   the same, a bit lower and softer
    set-found      a rising run of glassy bells with sparkles and a long airy tail
    set-wrong      a low, dull tone that falls a little
"""
import math
import os
import random
import struct
import sys
import wave

RATE = 44100
TWO_PI = 2 * math.pi


def write(folder, name, samples, peak=0.9):
    top = max(abs(s) for s in samples) or 1.0
    scale = peak / top
    path = os.path.join(folder, name + '.wav')
    with wave.open(path, 'wb') as f:
        f.setnchannels(1)
        f.setsampwidth(2)
        f.setframerate(RATE)
        f.writeframes(b''.join(struct.pack('<h', int(max(-1.0, min(1.0, s * scale)) * 32767)) for s in samples))
    print('written', path, '%.2f s' % (len(samples) / RATE))


def clamp01(v):
    return max(0.0, min(1.0, v))


# ------------------------------------------------------------------ set found: magical

def add_bell(samples, start, frequency, amplitude, length=1.0):
    """A glassy bell: a few partials that are not exact multiples, each dying away at its own speed.
    `length` stretches the decay (1 = about a second)."""
    partials = ((1.0, 1.0, 3.2), (2.76, 0.35, 5.5), (5.4, 0.16, 9.0), (0.5, 0.22, 2.4))
    first = int(round(start * RATE))
    for s in range(len(samples) - first):
        t = s / RATE
        if t > 3.0 * length:
            break
        attack = clamp01(t / 0.003)
        value = 0.0
        for ratio, level, decay in partials:
            value += level * math.sin(TWO_PI * frequency * ratio * t) * math.exp(-decay * t / length)
        samples[first + s] += value * attack * amplitude


def add_sparkle(samples, start, frequency, amplitude):
    """A tiny high ping."""
    first = int(round(start * RATE))
    for s in range(min(len(samples) - first, int(0.25 * RATE))):
        t = s / RATE
        samples[first + s] += (math.sin(TWO_PI * frequency * t) + 0.3 * math.sin(TWO_PI * frequency * 2.01 * t)) \
            * math.exp(-28 * t) * clamp01(t / 0.001) * amplitude


def add_glide(samples, start, length, f0, f1, amplitude):
    """A fast rising shimmer, like a hand over a harp."""
    first = int(round(start * RATE))
    phase = 0.0
    for s in range(min(len(samples) - first, int(length * RATE))):
        t = s / RATE
        k = t / length
        frequency = f0 * (f1 / f0) ** k
        phase += TWO_PI * frequency / RATE
        samples[first + s] += math.sin(phase) * math.sin(math.pi * k) ** 2 * amplitude


def add_pad(samples, frequency, amplitude):
    """A soft swell under the bells: three slightly detuned sines."""
    for s in range(len(samples)):
        t = s / RATE
        envelope = clamp01(t / 0.18) * math.exp(-1.9 * t)
        if envelope < 0.001 and t > 0.5:
            break
        samples[s] += amplitude * envelope * (math.sin(TWO_PI * frequency * t)
                                              + math.sin(TWO_PI * frequency * 1.004 * t)
                                              + math.sin(TWO_PI * frequency * 0.996 * t)) / 3.0


def reverb(dry, wet_level=0.34):
    """A small room of combs and all-pass filters (Schroeder), for the airy tail."""
    out = [0.0] * len(dry)
    for delay, feedback in ((1557, 0.80), (1617, 0.79), (1491, 0.81), (1422, 0.80), (1277, 0.78)):
        buf = [0.0] * delay
        i = 0
        damp = 0.0
        for n in range(len(dry)):
            y = buf[i]
            damp = y * 0.62 + damp * 0.38  # high frequencies die out first
            buf[i] = dry[n] + damp * feedback
            i = (i + 1) % delay
            out[n] += y
    for delay, gain in ((225, 0.5), (556, 0.5)):
        buf = [0.0] * delay
        i = 0
        for n in range(len(out)):
            y = buf[i]
            x = out[n]
            buf[i] = x + y * gain
            out[n] = y - x * gain
            i = (i + 1) % delay
    return [d + wet_level * w / 5.0 for d, w in zip(dry, out)]


def set_found():
    length = 2.0
    samples = [0.0] * int(length * RATE)
    rng = random.Random(3)

    add_pad(samples, 523.25, 0.10)
    add_glide(samples, 0.0, 0.30, 700.0, 3600.0, 0.10)

    # a rising run: C E G B E, each bell a bit brighter than the one before
    run = ((1046.5, 0.000, 0.40), (1318.5, 0.050, 0.45), (1568.0, 0.100, 0.50), (1975.5, 0.150, 0.55), (2637.0, 0.215, 0.70))
    for frequency, start, amplitude in run:
        add_bell(samples, start, frequency, amplitude * 0.5, length=1.2 if frequency < 2600 else 1.6)

    # sparkles: tiny pings from the top of the scale, mostly in the first half second
    notes = (2093.0, 2637.0, 3136.0, 3951.0, 4699.0)
    for _ in range(16):
        add_sparkle(samples, rng.uniform(0.04, 0.75), rng.choice(notes), rng.uniform(0.04, 0.10))

    samples = reverb(samples)
    fade = int(0.25 * RATE)
    for n in range(fade):
        samples[len(samples) - 1 - n] *= n / fade
    return samples


# ------------------------------------------------------------------ set wrong

def set_wrong():
    """A low, dull tone that falls a little."""
    samples = [0.0] * int(0.4 * RATE)
    phase = 0.0
    for s in range(len(samples)):
        t = s / RATE
        frequency = 190.0 + (140.0 - 190.0) * clamp01(t / 0.3)
        phase += TWO_PI * frequency / RATE
        tone = math.sin(phase) + 0.35 * math.sin(2 * phase) + 0.2 * (1.0 if math.sin(phase) >= 0 else -1.0)
        samples[s] = tone * clamp01(t / 0.005) * math.exp(-6 * t) * 0.4
    return samples


# ------------------------------------------------------------------ cards: a sliding card

def slide(seed, length, top_start, top_end, tap_frequency, tap_level):
    """A card sliding over a table: friction noise that is brightest at the start and slows down while it
    travels, with a rough grain, ending in a soft tap when it comes to rest.
    top_start and top_end are the upper edge of the noise in Hz at the start and the end."""
    rng = random.Random(seed)
    count = int(length * RATE)
    samples = [0.0] * count
    low_a = 1.0 - math.exp(-TWO_PI * 500.0 / RATE)  # the noise below ~500 Hz is cut away
    low = 0.0
    high = 0.0
    grain = 0.0
    tap_at = 0.86 * length
    for s in range(count):
        t = s / RATE
        k = t / length
        top = top_start + (top_end - top_start) * k
        high_a = 1.0 - math.exp(-TWO_PI * top / RATE)
        noise = rng.uniform(-1.0, 1.0)
        high += high_a * (noise - high)
        low += low_a * (noise - low)
        band = high - low
        # a rough, uneven grain like felt or paper fibres
        grain += 0.02 * (rng.uniform(0.3, 1.0) - grain)
        # it starts fast and fades while it slows down
        envelope = clamp01(t / 0.012) * (1.0 - k) ** 1.4 * (0.55 + 0.45 * math.sin(math.pi * min(1.0, k * 1.6)))
        samples[s] = band * envelope * (0.6 + 0.8 * grain)
        if t >= tap_at:
            tt = t - tap_at
            samples[s] += tap_level * math.sin(TWO_PI * tap_frequency * tt) * math.exp(-85 * tt) * clamp01(tt / 0.001)
    return samples


def main():
    default = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'Assets', 'Resources', 'Audio', 'Sfx')
    folder = sys.argv[1] if len(sys.argv) > 1 else default
    os.makedirs(folder, exist_ok=True)
    write(folder, 'card-deal', slide(7, 0.20, 6500.0, 2600.0, 190.0, 0.35))
    write(folder, 'card-discard', slide(11, 0.26, 4200.0, 1700.0, 125.0, 0.45))
    write(folder, 'set-found', set_found())
    write(folder, 'set-wrong', set_wrong())


if __name__ == '__main__':
    main()

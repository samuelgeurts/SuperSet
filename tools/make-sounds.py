#!/usr/bin/env python3
"""Makes the WAV files of the game sounds in Assets/Resources/Audio/Sfx.

The game plays them through AudioManager by name, so a sound can be replaced by putting another
WAV file with the same name in that folder. Run this script only to bring back the original sounds:

    python tools/make-sounds.py [output folder]

16 bit, mono, 44100 Hz. Everything is made from sine waves and noise, so the result is always the same.
"""
import math
import os
import random
import struct
import sys
import wave

RATE = 44100


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


def bell(samples, start, frequency, amplitude):
    """A soft bell: a tone with two overtones that dies away."""
    first = int(round(start * RATE))
    for s in range(len(samples) - first):
        t = s / RATE
        attack = clamp01(t / 0.004)
        tone = (math.sin(2 * math.pi * frequency * t)
                + 0.45 * math.sin(2 * math.pi * frequency * 2 * t) * math.exp(-4 * t)
                + 0.2 * math.sin(2 * math.pi * frequency * 3 * t) * math.exp(-8 * t))
        samples[first + s] += tone * attack * math.exp(-6.5 * t) * amplitude * 0.6


def set_found():
    """A bright two-note chime."""
    samples = [0.0] * int(0.7 * RATE)
    bell(samples, 0.0, 880.0, 0.5)
    bell(samples, 0.09, 1318.5, 0.5)
    return samples


def set_wrong():
    """A low, dull tone that falls a little."""
    samples = [0.0] * int(0.4 * RATE)
    phase = 0.0
    for s in range(len(samples)):
        t = s / RATE
        frequency = 190.0 + (140.0 - 190.0) * clamp01(t / 0.3)
        phase += 2 * math.pi * frequency / RATE
        tone = math.sin(phase) + 0.35 * math.sin(2 * phase) + 0.2 * (1.0 if math.sin(phase) >= 0 else -1.0)
        samples[s] = tone * clamp01(t / 0.005) * math.exp(-6 * t) * 0.4
    return samples


def flick(seed, length, decay, brightness):
    """One card: a short burst of noise that dies away fast. A higher brightness leaves more of the high part."""
    rng = random.Random(seed)
    samples = [0.0] * int(length * RATE)
    previous = 0.0
    for s in range(len(samples)):
        t = s / RATE
        noise = rng.uniform(-1.0, 1.0)
        high = noise - previous * brightness
        previous = noise
        samples[s] = high * math.exp(-decay * t) * clamp01(t / 0.0008)
    return samples


def main():
    default = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'Assets', 'Resources', 'Audio', 'Sfx')
    folder = sys.argv[1] if len(sys.argv) > 1 else default
    os.makedirs(folder, exist_ok=True)
    write(folder, 'card-deal', flick(7, 0.09, 70.0, 0.6))
    write(folder, 'card-discard', flick(11, 0.11, 52.0, 0.35))
    write(folder, 'set-found', set_found())
    write(folder, 'set-wrong', set_wrong())


if __name__ == '__main__':
    main()

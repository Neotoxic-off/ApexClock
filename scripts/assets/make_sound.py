import math, struct, wave, sys
# Soft two-note chime (E5 -> A5): near-pure sine, rounded attack, long gentle decay, low level.
rate, length = 44100, 1.6
notes = [(0.00, 659.25, 1.0), (0.18, 880.00, 0.85)]
samples = []
for n in range(int(rate * length)):
    t = n / rate
    v = 0.0
    for start, f, gain in notes:
        if t >= start:
            u = t - start
            attack = 0.5 - 0.5 * math.cos(math.pi * min(1.0, u / 0.025))   # 25 ms raised-cosine fade-in
            env = attack * math.exp(-u * 2.6)
            v += gain * env * (math.sin(2 * math.pi * f * u) + 0.06 * math.sin(2 * math.pi * f * 2 * u))
    samples.append(v)
# 80 ms fade-out so the tail never clicks
fade = int(rate * 0.08)
for i in range(fade):
    samples[-fade + i] *= 1 - i / fade
peak = max(abs(s) for s in samples)
with wave.open(sys.argv[1], "wb") as wf:
    wf.setnchannels(1); wf.setsampwidth(2); wf.setframerate(rate)
    wf.writeframes(b"".join(struct.pack("<h", int(s / peak * 0.32 * 32767)) for s in samples))
print("ok")

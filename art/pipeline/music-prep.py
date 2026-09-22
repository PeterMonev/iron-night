"""The menu theme: a public-domain military band march (US Marine Band, Sousa's "U.S. Field Artillery"), cut to a
length that loops, stereo 44.1 kHz 16-bit WAV, normalised, with a fade in and out.
Usage: python music-prep.py <in.mp3|ogg> <out name> [start s] [length s]"""
import sys, os, wave
import numpy as np, av

OUT = 'D:/Codes/Projects/lightswarm/unity/Assets/_Game/Resources/Audio'; R = 44100
src, name = sys.argv[1], sys.argv[2]; start = float(sys.argv[3]) if len(sys.argv) > 3 else 0.0; length = float(sys.argv[4]) if len(sys.argv) > 4 else None

c = av.open(src); s = c.streams.audio[0]; rs = av.AudioResampler(format='fltp', layout='stereo', rate=R); L, Rr = [], []
for fr in c.decode(s):
    for r in rs.resample(fr): a = r.to_ndarray(); L.append(a[0]); Rr.append(a[1])
c.close(); l = np.concatenate(L).astype(np.float32); r = np.concatenate(Rr).astype(np.float32)
i0 = int(start * R); i1 = len(l) if length is None else min(len(l), i0 + int(length * R)); l, r = l[i0:i1], r[i0:i1]
# a fade in and a fade out of a second and a half, so the loop does not click
f = int(1.5 * R); ramp = np.linspace(0, 1, f, dtype=np.float32)
l[:f] *= ramp; r[:f] *= ramp; l[-f:] *= ramp[::-1]; r[-f:] *= ramp[::-1]
peak = max(np.abs(l).max(), np.abs(r).max()); g = 0.9 / max(peak, 1e-6); l *= g; r *= g
inter = np.empty(len(l) * 2, dtype=np.float32); inter[0::2] = l; inter[1::2] = r
w = wave.open(os.path.join(OUT, name + '.wav'), 'w'); w.setnchannels(2); w.setsampwidth(2); w.setframerate(R)
w.writeframes((np.clip(inter, -1, 1) * 32767).astype('<i2').tobytes()); w.close()
print(f'{name}.wav {len(l) / R:.1f} s stereo')

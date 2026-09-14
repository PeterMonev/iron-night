"""Turns the downloaded library sounds (Pixabay Content License / Mixkit License, see Resources/Audio/SOURCES.md) into
the game's clips: mono 44.1 kHz 16-bit WAV, leading silence cut, trimmed to length with a fade, peak-normalised, and
seamless cross-faded loops for the engine, tracks, wind, rain and the front-line rumble.
Usage: python sfx-prep.py <download dir>   (the python_embeded of ComfyUI has PyAV and numpy)"""
import sys, os, wave
import numpy as np, av

SRC = sys.argv[1]; OUT = 'D:/Codes/Projects/lightswarm/unity/Assets/_Game/Resources/Audio'
os.makedirs(OUT, exist_ok=True); R = 44100

def decode(path):
    c = av.open(path); s = c.streams.audio[0]; rs = av.AudioResampler(format='fltp', layout='mono', rate=R); out = []
    for fr in c.decode(s):
        for r in rs.resample(fr): out.append(r.to_ndarray()[0])
    c.close(); return np.concatenate(out).astype(np.float32)

def clip(name, src, start=0.0, length=None, fade=0.15, loop=False, peak=0.9, cut_lead=True, gain=1.0):
    x = decode(os.path.join(SRC, src))
    if cut_lead:
        env = np.abs(x); nz = np.where(env > env.max() * 0.03)[0]
        if len(nz): x = x[max(0, nz[0] - int(0.01 * R)):]
    x = x[int(start * R):]
    if length: x = x[:int(length * R)]
    if loop:
        # the last half second is blended into the first, then that half second is dropped from the end
        n = int(0.5 * R); a = np.linspace(0, 1, n, dtype=np.float32)
        x[:n] = x[:n] * a + x[-n:] * (1 - a); x = x[:-n]
    elif fade > 0:
        n = min(len(x), int(fade * R)); x[-n:] *= np.linspace(1, 0, n, dtype=np.float32)
        n0 = min(len(x), int(0.004 * R)); x[:n0] *= np.linspace(0, 1, n0, dtype=np.float32)
    x = x / (np.abs(x).max() + 1e-9) * peak * gain
    with wave.open(os.path.join(OUT, name + '.wav'), 'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(R); w.writeframes((np.clip(x, -1, 1) * 32767).astype('<i2').tobytes())
    print(f'{name:12s} {len(x) / R:6.2f} s  from {src}')

# guns
clip('shot', 'cannonshot_352459.mp3', length=1.9)
clip('shotHeavy', 'mixkit_gunecho_1700.wav', length=3.2, fade=0.6)
clip('shotFar', 'distantexpl_39685.mp3', length=1.2)
clip('flak', 'antiair_85147.mp3', length=1.3, fade=0.3)
clip('reload', 'reload_47828.mp3', peak=0.7)
# hits and blasts
clip('hit', 'hammersteel_454390.mp3', length=0.9)
clip('ricochet', 'ricochet2_101553.mp3', length=0.8)
clip('ricochet2', 'whizzby_41134.mp3', length=1.1, fade=0.3)
clip('explosion', 'bigexpl_369789.mp3', length=4.2, fade=0.8)
clip('artillery', 'largeexpl_100420.mp3', length=3.0, fade=0.8, peak=0.85)
clip('whistle', 'mortar1_104653.mp3', length=1.05, fade=0.12)
# loops
clip('engine', 'tankengine_88503.mp3', start=0.2, length=4.5, loop=True, peak=0.8)
clip('tracks', 'tracks_197409.mp3', start=0.2, length=4.0, loop=True, peak=0.7)
clip('wind', 'nightwindy_17044.mp3', start=20.0, length=16.0, loop=True, cut_lead=False, peak=0.6)
clip('rain', 'rainroof_350531.mp3', start=2.0, length=12.0, loop=True, cut_lead=False, peak=0.7)
clip('front', 'bombardment_242655.mp3', start=1.0, length=24.0, loop=True, cut_lead=False, peak=0.7)
clip('turret', 'turret_14879.mp3', start=0.1, length=0.9, loop=True, peak=0.5)
# ui
clip('click', 'mixkit_click_1117.wav', length=0.25, peak=0.6)
clip('pickup', 'mixkit_radioping_2544.wav', length=1.0, fade=0.3, peak=0.7)
clip('levelUp', 'mixkit_win_265.wav', length=2.2, fade=0.7, peak=0.7)

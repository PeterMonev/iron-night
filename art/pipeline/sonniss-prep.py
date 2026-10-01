"""The real recordings from the Sonniss GDC Game Audio Bundles (2018, 2019; royalty-free, no attribution required; see
Resources/Audio/SOURCES.md) made into the game's clips: mono 44.1 kHz 16-bit WAV, cut from the steadiest stretch of each
take, seamless cross-faded loops for the engine, its idle, the tracks, the turret and the enemy's engine; the gun shots
trimmed with a fade; a fighter's pass made from a P-51 at full throttle with a Doppler sweep and a swell.
Usage: python sonniss-prep.py <folder with the Sonniss supplier folders>   (needs PyAV, numpy, scipy)"""
import sys, os, wave
import numpy as np, av
from scipy.signal import butter, sosfilt

SRC = sys.argv[1]; OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'unity', 'Assets', '_Game', 'Resources', 'Audio'); R = 44100


def decode(rel):
    c = av.open(os.path.join(SRC, rel)); s = c.streams.audio[0]; rs = av.AudioResampler(format='fltp', layout='mono', rate=R); out = []
    for fr in c.decode(s):
        for r in rs.resample(fr): out.append(r.to_ndarray()[0])
    c.close(); return np.concatenate(out).astype(np.float32)


def lowpass(x, hz): return sosfilt(butter(4, hz, 'low', fs=R, output='sos'), x).astype(np.float32)
def highpass(x, hz): return sosfilt(butter(2, hz, 'high', fs=R, output='sos'), x).astype(np.float32)


def save(name, x, peak):
    x = x / (np.abs(x).max() + 1e-9) * peak
    with wave.open(os.path.join(OUT, name + '.wav'), 'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(R); w.writeframes((np.clip(x, -1, 1) * 32767).astype('<i2').tobytes())
    print(f'{name:14s} {len(x) / R:6.2f} s')


def loop(name, rel, start, length, peak=0.8, lp=None, hp=None, xfade=0.6):
    """A stretch of the take made to loop: its last xfade seconds blended into its first, then dropped from the end."""
    x = decode(rel)[int(start * R):int((start + length + xfade) * R)]
    if hp: x = highpass(x, hp)
    if lp: x = lowpass(x, lp)
    n = int(xfade * R); a = np.sqrt(np.linspace(0, 1, n, dtype=np.float32))   # equal power
    x[:n] = x[:n] * a + x[-n:] * np.sqrt(1 - a ** 2); x = x[:-n]
    save(name, x, peak)


def shot(name, rel, length, fade=0.5, peak=0.95, lp=None):
    x = decode(rel); env = np.abs(x); nz = np.where(env > env.max() * 0.05)[0]
    x = x[max(0, nz[0] - int(0.004 * R)):][:int(length * R)]
    if lp: x = lowpass(x, lp)
    f = min(len(x), int(fade * R)); x[-f:] *= np.linspace(1, 0, f, dtype=np.float32) ** 2
    save(name, x, peak)


def flyby(name, rel, start, length=4.2, src_len=7.0, peak=0.9):
    """An aircraft passing: the engine at full power read faster then slower (Doppler, 1.12 to 0.86 about the middle),
    swelling to the pass and dying away, the far part dulled."""
    x = decode(rel)[int(start * R):int((start + src_len) * R)]
    t = np.linspace(0, 1, int(length * R), dtype=np.float32)
    rate = 0.86 + 0.26 / (1 + np.exp((t - 0.5) * 14))       # the pitch falls through the pass
    pos = np.cumsum(rate) ; pos = np.clip(pos, 0, len(x) - 2)
    y = np.interp(pos, np.arange(len(x)), x).astype(np.float32)
    swell = np.exp(-((t - 0.48) / 0.2) ** 2) * 0.85 + 0.15 * np.exp(-((t - 0.48) / 0.45) ** 2)
    far = lowpass(y, 1800); y = (y * swell + far * (1 - swell) * 0.4).astype(np.float32)
    y[:int(0.05 * R)] *= np.linspace(0, 1, int(0.05 * R)); y[-int(0.3 * R):] *= np.linspace(1, 0, int(0.3 * R))
    save(name, y, peak)


PP = 'Pole Position - '
# the leader's tank: a Sherman M4A3 driving steadily (inside, so dulled a little), its idle from the Chaffee standing in neutral
loop('engine', PP + 'Sherman M4A3 Medium Tank/sherman_m4a3_t3_onbrd_start_attempt_medium_drive_decelerate_slow_stop_off_engine_DPA4061.wav', 60, 6.0, peak=0.8, lp=6000)
loop('engine_idle', PP + 'Chaffee M24 Light Tank/chaffee_m24_t6_onbrd_start_idle_blips_steady_in_neutral_off_engine_DPA4061.wav', 13, 6.0, peak=0.7, lp=6000)
# the tracks: the microphone by the Sherman's right track
loop('tracks', PP + 'Sherman M4A3 Medium Tank/sherman_m4a3_t4_onbrd_medium_drive_steady_slow_short_tracks_right_RE50.wav', 30, 6.0, peak=0.75, hp=60)
# the turret: the Chaffee's hydraulic traverse
loop('turret', PP + 'Chaffee M24 Light Tank/chaffee_m24_t7_var_sfx_ext_hydraulic_traverse_XY_RSM191.wav', 30, 3.0, peak=0.55, xfade=0.4)
# the enemy's engine near us: a Panzer IV idling outside
loop('engine_enemy', PP + 'Panzer IV Ausf. G/panzer_iv_t10_ext_start_idle_steady_blips_main.wav', 42, 7.0, peak=0.8)
# the guns: the 105 mm howitzer, close, heavy and far
shot('shot', 'Airborne Sound - Battlefield Howitzers/Howitzer,M101,C1,105 mm,Distant,Right Side,Shot,Pound,Thick.wav', 2.6, fade=0.8)
shot('shotHeavy', 'Airborne Sound - Battlefield Howitzers/Howitzer,M101,C3,105 mm,Medium Distant,Right Side,Shot,Firm,Heavy,EQ Version,Soft Attack.wav', 2.8, fade=0.9)
shot('shotFar', 'Airborne Sound - Battlefield Howitzers/Howitzer,M101,C3,105 mm,Distant,Right Side,Shot,Explode,Crack,Sweetener.wav', 1.8, fade=0.6, peak=0.8, lp=5000)
# our fighter-bombers going over
flyby('fighter', PP + 'North American P-51D Mustang/P-51D_t2_ext_startup_ramps_shutdown_distant_behind_RSM191.L.wav', 42.0)

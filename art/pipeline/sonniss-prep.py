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
# the turret: the Chaffee's hydraulic traverse, only its motor's hum (the hiss above 1.8 kHz cut)
loop('turret', PP + 'Chaffee M24 Light Tank/chaffee_m24_t7_var_sfx_ext_hydraulic_traverse_XY_RSM191.wav', 30, 3.0, peak=0.45, lp=1800, hp=70, xfade=0.4)
# the enemy's engine near us: a Panzer IV idling outside
loop('engine_enemy', PP + 'Panzer IV Ausf. G/panzer_iv_t10_ext_start_idle_steady_blips_main.wav', 42, 7.0, peak=0.8)
# the guns: the 105 mm howitzer, close, heavy and far
shot('shot', 'Airborne Sound - Battlefield Howitzers/Howitzer,M101,C1,105 mm,Distant,Right Side,Shot,Pound,Thick.wav', 2.6, fade=0.8)
shot('shotHeavy', 'Airborne Sound - Battlefield Howitzers/Howitzer,M101,C3,105 mm,Medium Distant,Right Side,Shot,Firm,Heavy,EQ Version,Soft Attack.wav', 2.8, fade=0.9)
shot('shotFar', 'Airborne Sound - Battlefield Howitzers/Howitzer,M101,C3,105 mm,Distant,Right Side,Shot,Explode,Crack,Sweetener.wav', 1.8, fade=0.6, peak=0.8, lp=5000)
# our fighter-bombers going over
flyby('fighter', PP + 'North American P-51D Mustang/P-51D_t2_ext_startup_ramps_shutdown_distant_behind_RSM191.L.wav', 42.0)


def cut(name, rel, start=None, length=2.0, fade=0.5, peak=0.9, lp=None, hp=None, layer=None, reverse=False):
    """One sound out of a take: from start (or its first loud moment), length long, faded out; a second take mixed in
    under it (layer = (rel, seconds in, gain)), as debris after a blast; reversed for a shell coming in."""
    x = decode(rel)
    if start is None:
        env = np.abs(x); nz = np.where(env > env.max() * 0.05)[0]; s0 = max(0, nz[0] - int(0.004 * R))
    else: s0 = int(start * R)
    x = x[s0:s0 + int(length * R)].copy()
    if layer:
        y = decode(layer[0]); at = int(layer[1] * R); y = y[:max(0, len(x) - at)]
        x[at:at + len(y)] += y / (np.abs(y).max() + 1e-9) * np.abs(x).max() * layer[2]
    if hp: x = highpass(x, hp)
    if lp: x = lowpass(x, lp)
    if reverse: x = x[::-1].copy()
    f = min(len(x), int(fade * R))
    if f > 0: x[-f:] *= np.linspace(1, 0, f, dtype=np.float32) ** 2
    n0 = int(0.003 * R); x[:n0] *= np.linspace(0, 1, n0, dtype=np.float32)
    save(name, x, peak)


BZ = 'Bluezone - Tank - Explosion Sound Effects/'; DEBRIS = (BZ + 'Bluezone_BC0271_debris_falling_rock_dust_002.wav', 0.7, 0.35)
# a shell into armour: a heavy steel tank struck, a plate, a deep ringing blow; three to take turns
cut('hit', 'BlueZone - Heavy Metal Impact Sound Effects/Bluezone_BC0251_heavy_metal_impact_large_tank_01_03.wav', length=2.2, fade=0.9)
cut('hit2', 'BlueZone - Heavy Metal Impact Sound Effects/Bluezone_BC0251_heavy_metal_impact_metal_plate_medium.wav', length=1.6, fade=0.7)
cut('hit3', 'Bluezone - Cinematic Metal Impacts/Bluezone_BC0258_cinematic_metal_impact_052.wav', length=2.4, fade=0.9, peak=0.85)
# a ricochet: the clang of a plate struck a glancing blow, and the shot whining away
PLATE = 'BlueZone - Heavy Metal Impact Sound Effects/Bluezone_BC0251_heavy_metal_impact_metal_plate_medium.wav'
cut('ricochet', 'Bluezone Corporation - Metal Debris/Bluezone_BC0236_metal_whoosh_021.wav', start=0.55, length=1.9, fade=0.8, peak=0.8, layer=(PLATE, 0.0, 0.7))
cut('ricochet2', 'Bluezone Corporation - Metal Debris/Bluezone_BC0236_metal_whoosh_037.wav', start=0.95, length=2.0, fade=0.9, peak=0.8, layer=(PLATE, 0.0, 0.55))
# a vehicle going up, with the earth and stones coming down after it
cut('explosion', 'Stefano Cremona - Explosions/BigExplosion02.wav', length=5.0, fade=1.8, layer=DEBRIS)
cut('explosion2', 'Olivier Girardot - Guns & Explosions/Explosion 8.wav', length=4.6, fade=1.6, layer=DEBRIS)
cut('explosion3', BZ + 'Bluezone_BC0271_explosion_outdoors_large_005.wav', length=3.4, fade=1.2, layer=DEBRIS)
# artillery and mortar rounds landing: deep, near and far
cut('artillery', 'Stefano Cremona - Explosions/DeepExplosion02.wav', length=3.6, fade=1.4, layer=DEBRIS, peak=0.85)
cut('artillery2', 'Stefano Cremona - Explosions/FarExplosion03.wav', length=3.0, fade=1.0, peak=0.85)
cut('artillery3', 'Lukas Tvrdon - Distant Blast/Distant Blast 26.wav', length=3.0, fade=1.2, peak=0.85)
# the shell coming in: a shell's flight reversed, so it grows to the moment it lands
cut('whistle', BZ + 'Bluezone_BC0271_shell_trajectory_004.wav', start=0.0, length=1.7, fade=0.0, peak=0.75, reverse=True)
# an enemy gun firing: a tank's cannon
cut('shotEnemy', BZ + 'Bluezone_BC0271_tank_artillery_cannon_shot_012.wav', length=2.6, fade=0.9)
# machine guns: ours is the Browning M1919 the Shermans carried, theirs the MG 42
cut('mg', 'Super Thump - Weapons of World War II - Designed/STDSGN_WoWW2_Wep_MG_M1919_Machinegun_Auto-Burst_Shot_X6.wav', start=13.2, length=1.0, fade=0.35, peak=0.8)
cut('mg42', 'Pole Position - MG 42 machine gun/MG 42, Firing, t1, Burst, Long, Mountain Top, D100.wav', start=8.6, length=1.3, fade=0.4, peak=0.8)
# the country: a hull through a hedge (leaves and twigs, three takes, one with a branch snapping)
RUSTLE = 'Articulated Sounds - Rustle Tones/RUSTLE Studio performed green leaves twigs brush moves.LR.wav'
BRANCHES = ('BlueZone - Wood Sound Effects/Bluezone_BC0254_wood_crushing_branches_001_001.wav', 0.25, 0.8)
cut('brush', RUSTLE, start=21.2, length=1.3, fade=0.5, peak=0.75, layer=BRANCHES)
cut('brush2', RUSTLE, start=16.8, length=1.8, fade=0.6, peak=0.7)
cut('brush3', 'Soundrangers - Foley Elements Foliage/leaves_pile_pick_up_04.wav', start=0.3, length=1.8, fade=0.6, peak=0.7)
# wood giving way under the tracks: a fence smashed, planks thudding, branches and a woodpile
cut('crunch', 'InspectorJ - Wooden Fence Destruction/Destruction_Wooden_2.wav', length=1.5, fade=0.6, peak=0.8)
cut('crunch2', 'InspectorJ - Wooden Fence Destruction/WoodenFence_Thud_Multi_1.wav', length=1.0, fade=0.4, peak=0.8, layer=('InspectorJ - Wooden Fence Destruction/Dropping_WoodPile_79.wav', 0.1, 0.6))
cut('crunch3', 'BlueZone - Wood Sound Effects/Bluezone_BC0254_wood_crushing_branches_001_001.wav', length=0.8, fade=0.3, peak=0.8, layer=('Matt Script - You Me & Debris/impact_wood_debris_fall_hit_02.wav', 0.15, 0.5))
# a tree going over: the trunk cracking, then the crash as it lands, timed to the fall (1.1 s)
cut('timber', 'Soundrangers - Foley Elements Foliage/tree_falling_04.wav', start=6.9, length=3.4, fade=1.2, peak=0.9, layer=('Soundrangers - Foley Elements Foliage/tree_dead_tree_limb_impact_05.wav', 0.0, 0.7))
cut('timber2', 'Bluezone - Forest Creature Sound Effects/Bluezone_BC0269_creature_wood_texture_crack_heavy_rumble_006.wav', start=0.2, length=3.4, fade=1.2, peak=0.85)
cut('timber3', 'Rock The Speakerbox - Broken/BROKEN - DESIGNED - WOOD Break Small.wav', start=12.0, length=3.0, fade=1.0, peak=0.85)
# a house coming down: beams falling onto stone, and the wood after them
cut('collapse', 'Rock The Speakerbox - Broken/BROKEN - CK - BEAM Wood Ceiling Drop On Concrete.wav', start=2.9, length=2.2, fade=0.9, peak=0.9, layer=('Matt Script - You Me & Debris/impact_wood_debris_fall_hit_02.wav', 0.6, 0.5))

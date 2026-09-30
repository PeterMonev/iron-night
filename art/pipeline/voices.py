# The game's voices, spoken by Kokoro-82M (Apache 2.0, offline: D:/Tools/kokoro) and written as WAV clips into
# Resources/Audio/Voice. Three kinds:
#   radio_<commander>_<start|kill0|kill1|hit|tiger|dawn>  the commanders on the radio (Battle.Radio's lines)
#   spot_<us|su>_<what> and spot_<us|su>_clock<1-12>      a spotter calling a target, then its bearing
#   adj_<us|su>_<morning|afternoon|evening|night|congrats>_<rank 0-11>   the adjutant in the menu
# The radio ones go through a radio: a narrow band, a little overdrive, a hiss under the voice, the squelch and the
# click of the key before and after. The adjutant is in the room with him: clean. The Soviets speak English with
# British voices, as the war films have them (Kokoro has no Russian).
# D:/Tools/ComfyUI_windows_portable/python_trellis/python.exe -s voices.py [only-this-prefix]
import os, sys, wave
sys.path.insert(0, 'D:/Tools/kokoro/lib')
import numpy as np
from kokoro_onnx import Kokoro

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'unity', 'Assets', '_Game', 'Resources', 'Audio', 'Voice')
RATE = 24000
rng = np.random.default_rng(7)

COMMANDERS = {   # id: (voice, lines)
    'kowalski': ('am_puck', {'start': "Eyes open. Nobody dies tonight.", 'kill0': "Scratch one. Keep loading.", 'kill1': "That's how it's done.", 'hit': "We're hit! Driver, move!", 'tiger': "Tiger. Flank it, don't front it.", 'dawn': "Sun's up. Good work, all of you."}),
    'hale': ('am_michael', {'start': "Map says hedgerows all the way. Stay on the lanes.", 'kill0': "Target down. Mark it.", 'kill1': "Good shooting, gunner.", 'hit': "Damage report!", 'tiger': "Heavy armour, eleven o'clock. Use the hedge.", 'dawn': "Objective secured. Well done."}),
    'rivers': ('am_fenrir', {'start': "Come out fighting. Let's go.", 'kill0': "Got him. Next.", 'kill1': "Keep 'em coming.", 'hit': "Shake it off. We're still rolling.", 'tiger': "Big cat. Mine.", 'dawn': "We held. Told you."}),
    'orlov': ('bm_george', {'start': "Forward. Kursk was worse.", 'kill0': "Burn.", 'kill1': "One less.", 'hit': "Armour holds. Drive.", 'tiger': "Tiger. Close in, hit the side.", 'dawn': "Dawn. We are still here."}),
    'samusenko': ('bf_emma', {'start': "Check your engines. Go.", 'kill0': "Hit. Reload.", 'kill1': "Clean shot.", 'hit': "I'll patch it. Keep moving.", 'tiger': "Heavy one. Don't stop.", 'dawn': "Morning. Everyone still runs."}),
    'belov': ('bm_fable', {'start': "Engines warm. Let's run them.", 'kill0': "Ha! Next one.", 'kill1': "Too slow, Fritz.", 'hit': "Ouch. Faster, then.", 'tiger': "Tiger? We're faster.", 'dawn': "Sunrise. Good drive."}),
}
SPOTTERS = {'us': 'am_eric', 'su': 'bm_lewis'}
CALLS = {'tiger': "Tiger!", 'panther': "Panther!", 'guns': "Anti-tank gun, dug in!", 'eightyeight': "Eighty-eight! Flak gun!", 'flak': "Flak battery!",
         'infantry': "Infantry! Panzerfausts!", 'halftrack': "Half-track with infantry!", 'column': "Armoured column!", 'keil': "Panzerkeil!",
         'ace': "Ace on the field!", 'observer': "Forward observer!", 'mines': "Mines ahead!"}
HOURS = ['one', 'two', 'three', 'four', 'five', 'six', 'seven', 'eight', 'nine', 'ten', 'eleven', 'twelve']
ADJUTANTS = {'us': 'af_heart', 'su': 'bf_isabella'}
RANKS = ['Recruit', 'Trooper', 'Corporal', 'Sergeant', 'Lieutenant', 'Captain', 'Major', 'Lieutenant Colonel', 'Brigadier General', 'Major General', 'Lieutenant General', 'General']


def band(x, lo, hi):
    """A band-pass in the frequency domain with soft shoulders an octave wide."""
    n = len(x); f = np.fft.rfftfreq(n, 1.0 / RATE); X = np.fft.rfft(x)
    g = np.clip(np.log2(np.maximum(f, 1.0) / lo) + 1.0, 0.0, 1.0) * np.clip(np.log2(hi / np.maximum(f, 1.0)) + 1.0, 0.0, 1.0)
    return np.fft.irfft(X * g, n)


def radio(v):
    v = band(v, 320.0, 3200.0); v = v / (np.abs(v).max() + 1e-6)
    v = np.tanh(v * 2.2) / np.tanh(2.2)
    pre, post = int(0.16 * RATE), int(0.22 * RATE)
    x = np.concatenate([np.zeros(pre), v * 0.85, np.zeros(post)])
    hiss = band(rng.standard_normal(len(x)), 900.0, 6000.0); hiss /= np.abs(hiss).max() + 1e-6
    x += hiss * 0.035
    burst = lambda n, decay: rng.standard_normal(n) * np.exp(-np.arange(n) / (decay * RATE))
    x[:int(0.14 * RATE)] += band(burst(int(0.14 * RATE), 0.04), 600.0, 7000.0) * 0.35   # the squelch opening
    x[:int(0.006 * RATE)] += 0.9 * np.sign(rng.standard_normal(int(0.006 * RATE)))       # the key pressed
    tail = int(0.12 * RATE); x[-tail - int(0.03 * RATE):-int(0.03 * RATE)] += band(burst(tail, 0.05), 600.0, 7000.0) * 0.3
    x[-int(0.03 * RATE):-int(0.024 * RATE)] += 0.8 * np.sign(rng.standard_normal(int(0.006 * RATE)))  # and let go
    return x / (np.abs(x).max() + 1e-6) * 0.9


def clean(v):
    fade = int(0.03 * RATE); v = v.copy(); v[:fade] *= np.linspace(0, 1, fade); v[-fade:] *= np.linspace(1, 0, fade)
    return np.concatenate([np.zeros(int(0.08 * RATE)), v / (np.abs(v).max() + 1e-6) * 0.8, np.zeros(int(0.1 * RATE))])


def save(name, x):
    pcm = (np.clip(x, -1, 1) * 32767).astype(np.int16)
    with wave.open(os.path.join(OUT, name + '.wav'), 'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(RATE); w.writeframes(pcm.tobytes())


def main():
    only = sys.argv[1] if len(sys.argv) > 1 else ''
    os.makedirs(OUT, exist_ok=True)
    k = Kokoro('D:/Tools/kokoro/kokoro-v1.0.onnx', 'D:/Tools/kokoro/voices-v1.0.bin')
    jobs = []
    for cid, (voice, lines) in COMMANDERS.items():
        for key, text in lines.items(): jobs.append(('radio_%s_%s' % (cid, key), voice, text, 1.08, True))
    for nation, voice in SPOTTERS.items():
        for what, text in CALLS.items(): jobs.append(('spot_%s_%s' % (nation, what), voice, text, 1.15, True))
        for h, word in enumerate(HOURS): jobs.append(('spot_%s_clock%d' % (nation, h + 1), voice, word.capitalize() + " o'clock!", 1.12, True))
    for nation, voice in ADJUTANTS.items():
        for r, rank in enumerate(RANKS):
            addr = ('Comrade ' if nation == 'su' else '') + rank
            jobs.append(('adj_%s_morning_%d' % (nation, r), voice, "Good morning, %s." % addr, 0.95, False))
            jobs.append(('adj_%s_afternoon_%d' % (nation, r), voice, "Good afternoon, %s." % addr, 0.95, False))
            jobs.append(('adj_%s_evening_%d' % (nation, r), voice, "Good evening, %s." % addr, 0.95, False))
            jobs.append(('adj_%s_night_%d' % (nation, r), voice, ("Still awake, %s? So am I." if nation == 'su' else "Still up, %s? So am I.") % addr, 0.92, False))
            jobs.append(('adj_%s_congrats_%d' % (nation, r), voice, "Congratulations, %s. The whole %s heard. Tonight the drinks are on me." % (addr, 'regiment' if nation == 'su' else 'company'), 0.95, False))
    n = 0
    for name, voice, text, speed, is_radio in jobs:
        if only and not name.startswith(only): continue
        lang = 'en-gb' if voice.startswith('b') else 'en-us'
        v, rate = k.create(text, voice=voice, speed=speed, lang=lang); assert rate == RATE
        save(name, radio(np.asarray(v, dtype=np.float64)) if is_radio else clean(np.asarray(v, dtype=np.float64))); n += 1
    print(n, 'clips into', os.path.normpath(OUT))


if __name__ == '__main__':
    main()

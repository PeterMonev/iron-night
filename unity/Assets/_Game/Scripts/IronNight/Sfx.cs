using System.Collections.Generic;
using UnityEngine;

namespace IronNight
{
    /// <summary>
    /// The sounds of the night. The clips are library recordings (Resources/Audio, Pixabay and Mixkit licences, see
    /// SOURCES.md there); anything missing is synthesised at start-up with the small DSP kit below, so the game never
    /// goes silent. Guns, hits and blasts play from a pool of positional sources; the engine, the tracks, the turret
    /// motor, the wind or rain and the far front line are loops.
    /// </summary>
    public class Sfx : MonoBehaviour
    {
        const int Rate = 44100;
        static Sfx instance;
        AudioClip shot, shotHeavy, shotFar, hit, explosion, artillery, pickup, click, levelUp, engineLoop, tracksLoop, wind, rain, front, whistle, rumble, ricochet, ricochet2, flak, reload, turretLoop, mg, faust, stuka, drone, crunch, brush, whoosh, fighter, shutter, crack, clatter;
        readonly List<AudioSource> pool = new List<AudioSource>(); AudioSource engine, tracks, turret, ambient, frontLine, ui, weld, voice, diesel, idle, enemyEngine; AudioClip idleLoop, enemyLoop, mg42, shotEnemy; AudioClip[] hits, blasts, shells; float engineBase = 1f, dieselLevel; Transform listener; readonly Queue<AudioClip> voiceQueue = new Queue<AudioClip>();

        public static void Build(Camera cam)
        {
            var go = new GameObject("Sfx"); instance = go.AddComponent<Sfx>();
            if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
            instance.listener = cam.transform; instance.Make(); AudioListener.volume = Muted ? 0f : 1f;
        }

        /// <summary>The sound switch on the pause sheet; remembered between nights.</summary>
        public static bool Muted { get => PlayerPrefs.GetInt("sound", 1) == 0; set { PlayerPrefs.SetInt("sound", value ? 0 : 1); PlayerPrefs.Save(); AudioListener.volume = value ? 0f : 1f; } }
        public static bool MusicOff { get => PlayerPrefs.GetInt("music", 1) == 0; set { PlayerPrefs.SetInt("music", value ? 0 : 1); PlayerPrefs.Save(); if (instance != null) instance.MusicTick(0f); } }
        // ---- the music: a theme for each part of the evening (Resources/Audio/music_<name>), on two decks that crossfade ----
        class Deck { public AudioSource src; public float want, rate; public bool on, held; }   // on: given a clip to play; held: paused by the music switch
        readonly Deck[] decks = new Deck[2]; int live = -1; string theme;
        static float ThemeGain(string t) => t == "menu" ? 0.34f : t == "battle" ? 0.3f : t == "boss" ? 0.3f : 0.6f;   // the drums are 5 dB louder than the rest
        static float ThemeSeam(string t) => t == "menu" ? 5f : t == "battle" ? 3f : t == "boss" ? 4f : 0f;   // seconds before its end a looping theme crosses into its start (0: played once)
        static float ThemeFrom(string t) => t == "boss" ? 0.8f : 0f;   // past a silence at the head

        /// <summary>The theme to play now: "menu" in the hangar, "battle" under the fight, "boss" while the Tiger lives,
        /// "dawn" once at the win; null for silence. Whatever plays crossfades into it.</summary>
        public static void Theme(string name)
        {
            if (instance == null || instance.theme == name) return;
            instance.theme = name; instance.Cross(name, name == "dawn" ? 1.2f : 2.5f); Debug.Log("Music: " + (name ?? "silence"));
        }
        /// <summary>What the decks play now, for the self-test's log: each playing deck's clip, volume and place in it.</summary>
        public static string NowPlaying()
        {
            if (instance == null) return "no sound";
            var sb = new System.Text.StringBuilder(instance.theme ?? "silence");
            foreach (var d in instance.decks) if (d != null && d.on && d.src.clip != null) sb.Append(" | ").Append(d.src.clip.name).Append(' ').Append(d.src.volume.ToString("0.00")).Append(" @ ").Append(d.src.time.ToString("0.0")).Append('/').Append(d.src.clip.length.ToString("0.0")).Append(d.src.isPlaying ? "" : " stopped");
            return sb.ToString();
        }
        /// <summary>The title's switch: the hangar's theme, or silence as the night starts.</summary>
        public static void Music(bool on) => Theme(on ? "menu" : null);

        void Cross(string name, float seconds)
        {
            foreach (var d in decks) if (d != null && d.on) { d.want = 0f; d.rate = Mathf.Max(0.02f, d.src.volume / Mathf.Max(0.1f, seconds)); }
            if (name == null) { live = -1; return; }
            var clip = Resources.Load<AudioClip>("Audio/music_" + name); if (clip == null) { live = -1; return; }
            int k = live < 0 ? 0 : 1 - live; var dk = decks[k] ??= NewDeck(k);
            dk.src.Stop(); dk.src.clip = clip; dk.src.volume = 0f; dk.src.time = Mathf.Min(ThemeFrom(name), clip.length * 0.5f);
            dk.want = ThemeGain(name); dk.rate = dk.want / Mathf.Max(0.1f, seconds); dk.held = false; dk.on = true; dk.src.Play(); live = k;
        }
        Deck NewDeck(int k)
        {
            var go = new GameObject("Music " + k); go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>(); s.loop = false; s.playOnAwake = false; s.spatialBlend = 0f; s.priority = 0; s.ignoreListenerPause = true; return new Deck { src = s };
        }
        void Update() { MusicTick(Time.unscaledDeltaTime); if (voice != null && !voice.isPlaying && voiceQueue.Count > 0) { voice.clip = voiceQueue.Dequeue(); voice.Play(); } }

        /// <summary>A voice (Voices): its clips play one after another on a source of their own. While one speaks the next
        /// waits, three clips at most; the rest is dropped. Missing clips are skipped.</summary>
        public static void Voice(params AudioClip[] seq)
        {
            if (!instance || seq == null || instance.voice == null) return;
            if (instance.voiceQueue.Count + (instance.voice.isPlaying ? 1 : 0) >= 3) return;
            foreach (var c in seq) if (c != null) instance.voiceQueue.Enqueue(c);
        }

        void MusicTick(float dt)
        {
            bool off = MusicOff;
            foreach (var d in decks)
            {
                if (d == null || !d.on) continue;
                if (off) { if (d.src.isPlaying) { d.src.Pause(); d.held = true; } continue; }
                if (d.held) { d.src.UnPause(); d.held = false; }
                d.src.volume = dt <= 0f ? d.want : Mathf.MoveTowards(d.src.volume, d.want, Mathf.Max(d.rate, 0.02f) * dt);
                if ((d.want <= 0f && d.src.volume <= 0.001f) || !d.src.isPlaying) { d.src.Stop(); d.on = false; }   // faded out, or a theme played once has run to its end
            }
            if (off || live < 0 || theme == null) return;
            var lv = decks[live]; float seam = ThemeSeam(theme);
            if (lv != null && lv.on && seam > 0f && lv.src.clip != null && lv.src.time >= lv.src.clip.length - seam) Cross(theme, seam);   // a looping theme crosses into its own start
        }

        // ---- the DSP kit: everything works on float buffers at 44.1 kHz ----

        static class Dsp
        {
            public static System.Random rng = new System.Random(7);
            public static int N(float seconds) => Mathf.CeilToInt(seconds * Rate);
            public static float[] Noise(int n) { var d = new float[n]; for (int i = 0; i < n; i++) d[i] = (float)(rng.NextDouble() * 2.0 - 1.0); return d; }
            /// <summary>A sine whose frequency is a function of time (seconds).</summary>
            public static float[] Sine(int n, System.Func<float, float> freq) { var d = new float[n]; double ph = 0; for (int i = 0; i < n; i++) { ph += 2.0 * Mathf.PI * freq(i / (float)Rate) / Rate; d[i] = (float)System.Math.Sin(ph); } return d; }
            public static void Env(float[] d, System.Func<float, float> env) { for (int i = 0; i < d.Length; i++) d[i] *= env(i / (float)Rate); }
            public static void Add(float[] dst, float[] src, float gain, float atSeconds = 0f) { int o = (int)(atSeconds * Rate); for (int i = 0; i < src.Length && i + o < dst.Length; i++) dst[i + o] += src[i] * gain; }
            public static void Gain(float[] d, float g) { for (int i = 0; i < d.Length; i++) d[i] *= g; }
            public static void Clip(float[] d, float drive) { for (int i = 0; i < d.Length; i++) d[i] = (float)System.Math.Tanh(d[i] * drive); }
            public static void Normalize(float[] d, float peak) { float m = 1e-6f; foreach (var v in d) m = Mathf.Max(m, Mathf.Abs(v)); float g = peak / m; for (int i = 0; i < d.Length; i++) d[i] *= g; }

            // RBJ biquads, processed in place; type 0 low-pass, 1 high-pass, 2 band-pass
            static void Coefs(int type, float f, float q, out float b0, out float b1, out float b2, out float a1, out float a2)
            {
                f = Mathf.Clamp(f, 20f, 20000f); float w = 2f * Mathf.PI * f / Rate, cw = Mathf.Cos(w), sw = Mathf.Sin(w), al = sw / (2f * q), a0;
                if (type == 0) { b0 = (1f - cw) / 2f; b1 = 1f - cw; b2 = b0; }
                else if (type == 1) { b0 = (1f + cw) / 2f; b1 = -(1f + cw); b2 = b0; }
                else { b0 = al; b1 = 0f; b2 = -al; }
                a0 = 1f + al; a1 = -2f * cw; a2 = 1f - al;
                b0 /= a0; b1 /= a0; b2 /= a0; a1 /= a0; a2 /= a0;
            }
            public static void Filter(float[] d, int type, float f, float q = 0.707f)
            {
                Coefs(type, f, q, out float b0, out float b1, out float b2, out float a1, out float a2);
                float x1 = 0, x2 = 0, y1 = 0, y2 = 0;
                for (int i = 0; i < d.Length; i++) { float x = d[i], y = b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2; x2 = x1; x1 = x; y2 = y1; y1 = y; d[i] = y; }
            }
            public static void Lowpass(float[] d, float f, float q = 0.707f) => Filter(d, 0, f, q);
            public static void Highpass(float[] d, float f, float q = 0.707f) => Filter(d, 1, f, q);
            public static void Bandpass(float[] d, float f, float q = 1f) => Filter(d, 2, f, q);
            /// <summary>A band-pass whose centre moves with time: the whistle of a falling shell.</summary>
            public static void BandpassSweep(float[] d, System.Func<float, float> f, float q)
            {
                float x1 = 0, x2 = 0, y1 = 0, y2 = 0, b0 = 0, b1 = 0, b2 = 0, a1 = 0, a2 = 0;
                for (int i = 0; i < d.Length; i++)
                {
                    if (i % 32 == 0) Coefs(2, f(i / (float)Rate), q, out b0, out b1, out b2, out a1, out a2);
                    float x = d[i], y = b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2; x2 = x1; x1 = x; y2 = y1; y1 = y; d[i] = y;
                }
            }
            /// <summary>Outdoor echo: a few delayed, darker copies folded back into the buffer.</summary>
            public static void Echo(float[] d, params (float delay, float gain, float cutoff)[] taps)
            {
                var src = (float[])d.Clone();
                foreach (var t in taps) { var c = (float[])src.Clone(); Lowpass(c, t.cutoff); Add(d, c, t.gain, t.delay); }
            }
            public static float Exp(float t, float k) => Mathf.Exp(-t * k);
        }

        AudioClip Clip(string name, float[] data)
        {
            for (int i = 0; i < data.Length; i++) data[i] = Mathf.Clamp(data[i], -1f, 1f);
            var c = AudioClip.Create(name, data.Length, 1, Rate, false); c.SetData(data, 0); return c;
        }

        // ---- the sounds ----

        /// <summary>A gun: size 0 is the 75 mm, 1 the 88; the crack, the body, the sub thump and the tail, then the echo.</summary>
        static float[] Gun(float size, float length, bool far)
        {
            int n = Dsp.N(length);
            var crack = Dsp.Noise(n); Dsp.Highpass(crack, far ? 600f : 1200f); Dsp.Env(crack, t => Dsp.Exp(t, 140f));
            var body = Dsp.Noise(n); Dsp.Bandpass(body, 230f - size * 90f, 0.8f); Dsp.Env(body, t => Dsp.Exp(t, 16f - size * 6f));
            var sub = Dsp.Sine(n, t => Mathf.Lerp(110f - size * 30f, 32f - size * 8f, Mathf.Min(1f, t / 0.3f))); Dsp.Env(sub, t => Dsp.Exp(t, 8f - size * 3f));
            var tail = Dsp.Noise(n); Dsp.Lowpass(tail, 500f); Dsp.Env(tail, t => t < 0.04f ? 0f : Dsp.Exp(t - 0.04f, 3.2f - size));
            var mix = new float[n]; Dsp.Add(mix, crack, far ? 0.35f : 0.9f); Dsp.Add(mix, body, 1.3f); Dsp.Add(mix, sub, 1.1f + size * 0.3f); Dsp.Add(mix, tail, 0.55f + size * 0.2f);
            Dsp.Clip(mix, 1.8f);
            if (far) Dsp.Lowpass(mix, 1400f);
            Dsp.Echo(mix, (0.17f, 0.32f, 1200f), (0.29f, 0.2f, 800f), (0.5f, 0.11f, 500f), (0.83f, 0.06f, 400f));
            Dsp.Normalize(mix, 0.95f); return mix;
        }

        static float[] MakeExplosion()
        {
            int n = Dsp.N(3.4f);
            var crack = Dsp.Noise(n); Dsp.Highpass(crack, 800f); Dsp.Env(crack, t => Dsp.Exp(t, 60f));
            var sub = Dsp.Sine(n, t => Mathf.Lerp(60f, 18f, Mathf.Min(1f, t / 0.8f))); Dsp.Env(sub, t => Dsp.Exp(t, 3f));
            var body = Dsp.Noise(n); Dsp.Bandpass(body, 110f, 0.6f); Dsp.Env(body, t => Dsp.Exp(t, 2.2f));
            var mid = Dsp.Noise(n); Dsp.Bandpass(mid, 600f, 1f); Dsp.Env(mid, t => Dsp.Exp(t, 7f));
            var tail = Dsp.Noise(n); Dsp.Lowpass(tail, 300f); Dsp.Env(tail, t => t < 0.1f ? 0f : Dsp.Exp(t - 0.1f, 1.1f));
            var mix = new float[n]; Dsp.Add(mix, crack, 0.7f); Dsp.Add(mix, sub, 1.2f); Dsp.Add(mix, body, 1.5f); Dsp.Add(mix, mid, 0.6f); Dsp.Add(mix, tail, 0.6f);
            // debris pattering down for a second and a half
            for (int i = 0; i < 14; i++) { float at = 0.25f + (float)Dsp.rng.NextDouble() * 1.4f; var b = Dsp.Noise(Dsp.N(0.03f)); Dsp.Highpass(b, 1800f); Dsp.Env(b, t => Dsp.Exp(t, 220f)); Dsp.Add(mix, b, 0.3f * (1f - at / 2f), at); }
            Dsp.Clip(mix, 2.2f);
            Dsp.Echo(mix, (0.2f, 0.3f, 900f), (0.37f, 0.2f, 600f), (0.66f, 0.12f, 400f), (1.0f, 0.07f, 300f));
            Dsp.Normalize(mix, 0.98f); return mix;
        }

        /// <summary>Armour struck: an inharmonic ring of six partials over a click and a thud.</summary>
        static float[] ArmourHit()
        {
            int n = Dsp.N(0.9f); var mix = new float[n];
            float[] freqs = { 640f, 1130f, 1780f, 2500f, 3300f, 4400f }, decays = { 14f, 18f, 22f, 28f, 34f, 40f }, gains = { 1f, 0.8f, 0.6f, 0.45f, 0.3f, 0.2f };
            for (int k = 0; k < 6; k++) { float f = freqs[k] * (1f + ((float)Dsp.rng.NextDouble() - 0.5f) * 0.04f); var p = Dsp.Sine(n, t => f); float dk = decays[k]; Dsp.Env(p, t => Dsp.Exp(t, dk)); Dsp.Add(mix, p, gains[k] * 0.35f); }
            var click = Dsp.Noise(n); Dsp.Highpass(click, 3000f); Dsp.Env(click, t => Dsp.Exp(t, 400f)); Dsp.Add(mix, click, 0.8f);
            var thud = Dsp.Sine(n, t => Mathf.Lerp(130f, 70f, Mathf.Min(1f, t / 0.1f))); Dsp.Env(thud, t => Dsp.Exp(t, 22f)); Dsp.Add(mix, thud, 0.7f);
            var body = Dsp.Noise(n); Dsp.Bandpass(body, 900f, 1f); Dsp.Env(body, t => Dsp.Exp(t, 40f)); Dsp.Add(mix, body, 0.8f);
            Dsp.Clip(mix, 1.4f); Dsp.Echo(mix, (0.12f, 0.2f, 2000f)); Dsp.Normalize(mix, 0.9f); return mix;
        }

        /// <summary>A ricochet: a hard metallic slap, then the whizz of the shell tumbling away, made of noise squeezed
        /// through a narrow band that falls in pitch (no pure tones: those sound like a ray gun).</summary>
        static float[] MakeRicochet()
        {
            int n = Dsp.N(0.6f); var mix = new float[n];
            var slap = Dsp.Noise(n); Dsp.Bandpass(slap, 2600f, 1.2f); Dsp.Env(slap, t => Dsp.Exp(t, 180f)); Dsp.Add(mix, slap, 1f);
            var clank = Dsp.Noise(n); Dsp.Bandpass(clank, 700f, 2f); Dsp.Env(clank, t => Dsp.Exp(t, 60f)); Dsp.Add(mix, clank, 0.6f);
            var whizz = Dsp.Noise(n); Dsp.BandpassSweep(whizz, t => 3400f * Mathf.Pow(0.28f, t / 0.45f), 9f); Dsp.Normalize(whizz, 1f); Dsp.Env(whizz, t => t < 0.02f ? 0f : Dsp.Exp(t - 0.02f, 7f)); Dsp.Add(mix, whizz, 0.9f);
            Dsp.Clip(mix, 1.3f); Dsp.Normalize(mix, 0.85f); return mix;
        }

        /// <summary>The engine: thirty firing pulses a second (seamless in one second), exhaust harmonics, intake hiss.</summary>
        static float[] MakeEngine()
        {
            int n = Rate; var mix = new float[n]; int per = n / 30;
            for (int p = 0; p < 30; p++)
            {
                var burst = Dsp.Noise(per); Dsp.Lowpass(burst, 900f); Dsp.Env(burst, t => Dsp.Exp(t, 300f)); float g = 0.8f + (float)Dsp.rng.NextDouble() * 0.4f;
                Dsp.Add(mix, burst, g, p * per / (float)Rate);
            }
            var drone = Dsp.Sine(n, t => 30f); Dsp.Add(mix, drone, 0.5f); Dsp.Add(mix, Dsp.Sine(n, t => 60f), 0.25f); Dsp.Add(mix, Dsp.Sine(n, t => 90f), 0.12f);
            var hiss = Dsp.Noise(n); Dsp.Bandpass(hiss, 1500f, 1f); Dsp.Add(mix, hiss, 0.12f);
            Dsp.Lowpass(mix, 2500f); Dsp.Clip(mix, 1.3f); Dsp.Normalize(mix, 0.7f); return mix;
        }

        /// <summary>Track clatter: the running gear as a low rumble that pulses with the wheels, and a dozen loose
        /// metallic clanks a second, each a burst of noise in its own band. Louder and faster with the engine.</summary>
        static float[] Tracks()
        {
            int n = Rate; var mix = new float[n];
            var gear = Dsp.Noise(n); Dsp.Lowpass(gear, 250f); Dsp.Env(gear, t => 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 9f * t)); Dsp.Add(mix, gear, 0.5f);
            for (int k = 0; k < 12; k++)
            {
                float at = k / 12f + ((float)Dsp.rng.NextDouble() - 0.5f) * 0.05f; if (at < 0f) at += 1f; if (at > 0.97f) at -= 0.97f;
                float f = 900f + (float)Dsp.rng.NextDouble() * 2600f, g = 0.3f + (float)Dsp.rng.NextDouble() * 0.3f;
                var clank = Dsp.Noise(Dsp.N(0.03f)); Dsp.Bandpass(clank, f, 2f); Dsp.Env(clank, t => Dsp.Exp(t, 150f)); Dsp.Add(mix, clank, g, at);
                var thud = Dsp.Noise(Dsp.N(0.03f)); Dsp.Bandpass(thud, 300f, 1.5f); Dsp.Env(thud, t => Dsp.Exp(t, 120f)); Dsp.Add(mix, thud, g * 0.6f, at);
            }
            Dsp.Normalize(mix, 0.45f); return mix;
        }

        /// <summary>A Ju 87 diving: the wind-driven siren howling up the scale as it comes down, the engine roaring under
        /// it, both falling away as it pulls out over the target and climbs off. The loudest moment is at 3.6 s.</summary>
        static float[] MakeStukaDive()
        {
            const float len = 5.5f, pass = 3.6f; int n = Dsp.N(len);
            System.Func<float, float> doppler = t => t < pass ? 1f + 0.12f * (t / pass) : 1.12f - 0.3f * Mathf.Clamp01((t - pass) / 0.8f);   // up as it comes, down as it goes
            System.Func<float, float> siren = t => 240f * Mathf.Pow(2f, Mathf.Min(t, pass) / 2.1f) * doppler(t);
            var mix = new float[n];
            for (int h = 1; h <= 4; h++) { var s = Dsp.Sine(n, t => siren(t) * h); Dsp.Add(mix, s, 0.55f / h); }   // a rough, reedy howl
            for (int i = 0; i < n; i++) { float t = i / (float)Rate; mix[i] *= 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * 38f * t); }   // the chop of the siren's rotor
            var engine = new float[n];
            for (int h = 1; h <= 6; h++) { var s = Dsp.Sine(n, t => 78f * h * doppler(t)); Dsp.Add(engine, s, 0.6f / h); }
            var grit = Dsp.Noise(n); Dsp.Bandpass(grit, 320f, 1.2f); Dsp.Add(engine, grit, 0.5f); Dsp.Clip(engine, 2.2f);
            Dsp.Add(mix, engine, 0.8f);
            Dsp.Env(mix, t => t < pass ? Mathf.Pow(t / pass, 1.6f) : Mathf.Max(0f, 1f - (t - pass) / (len - pass)));
            Dsp.Normalize(mix, 0.9f); return mix;
        }

        /// <summary>Fighter-bombers going over low: a big radial engine's roar rising as they come and falling away after.</summary>
        static float[] MakeFighterPass()
        {
            const float len = 4f, pass = 1.7f; int n = Dsp.N(len); var mix = new float[n];
            System.Func<float, float> doppler = t => t < pass ? 1.1f : 1.1f - 0.28f * Mathf.Clamp01((t - pass) / 0.7f);
            for (int h = 1; h <= 7; h++) { var s = Dsp.Sine(n, t => 58f * h * doppler(t)); Dsp.Add(mix, s, 0.7f / h); }
            var air = Dsp.Noise(n); Dsp.Bandpass(air, 420f, 0.8f); Dsp.Add(mix, air, 0.7f); Dsp.Clip(mix, 1.8f);
            Dsp.Env(mix, t => t < pass ? Mathf.Pow(t / pass, 2f) : Mathf.Max(0f, 1f - (t - pass) / (len - pass)));
            Dsp.Normalize(mix, 0.9f); return mix;
        }

        /// <summary>A hull going through wood: a dull thump and a crackle of splintering.</summary>
        static float[] MakeCrunch()
        {
            const float len = 0.5f; int n = Dsp.N(len); var mix = new float[n];
            var thump = Dsp.Sine(n, t => 60f - 30f * t); Dsp.Env(thump, t => Mathf.Exp(-t * 14f)); Dsp.Add(mix, thump, 1f);
            var crack = Dsp.Noise(n); Dsp.Bandpass(crack, 1400f, 0.9f);
            var spikes = new float[n]; var r = new System.Random(3);
            for (int k = 0; k < 14; k++) { int at = r.Next(n * 3 / 4); for (int i = 0; i < 300 && at + i < n; i++) spikes[at + i] += Mathf.Exp(-i / 60f); }   // the splinters, one after another
            for (int i = 0; i < n; i++) crack[i] *= spikes[i] * Mathf.Exp(-3f * i / n);
            Dsp.Add(mix, crack, 0.9f); Dsp.Normalize(mix, 0.8f); return mix;
        }

        /// <summary>A hull shoving through a hedge: a rush of leaves that swells and dies away, twigs snapping in it, and
        /// the soft push of the bank under the tracks.</summary>
        static float[] MakeBrush()
        {
            const float len = 0.9f; int n = Dsp.N(len); var mix = new float[n];
            var rush = Dsp.Noise(n); Dsp.Bandpass(rush, 3200f, 0.6f); Dsp.Env(rush, t => Mathf.Sin(Mathf.Clamp01(t / len) * Mathf.PI) * Mathf.Exp(-t * 1.5f)); Dsp.Add(mix, rush, 0.8f);
            var hiss = Dsp.Noise(n); Dsp.Highpass(hiss, 5500f); Dsp.Env(hiss, t => Mathf.Exp(-t * 5f) * Mathf.Clamp01(t * 30f)); Dsp.Add(mix, hiss, 0.35f);
            var snaps = Dsp.Noise(n); Dsp.Bandpass(snaps, 2400f, 1.4f); var spikes = new float[n]; var r = new System.Random(11);
            for (int k = 0; k < 9; k++) { int at = r.Next(n * 4 / 5); float g = 0.5f + (float)r.NextDouble() * 0.5f; for (int i = 0; i < 180 && at + i < n; i++) spikes[at + i] += g * Mathf.Exp(-i / 35f); }   // the twigs, one after another
            for (int i = 0; i < n; i++) snaps[i] *= spikes[i];
            Dsp.Add(mix, snaps, 0.9f);
            var push = Dsp.Sine(n, t => 48f - 12f * t); Dsp.Env(push, t => Mathf.Exp(-t * 7f)); Dsp.Add(mix, push, 0.5f);
            Dsp.Normalize(mix, 0.75f); return mix;
        }

        /// <summary>A sheet coming in: air swept past, a band of noise rising in pitch as it swells and falling away.</summary>
        static float[] MakeWhoosh()
        {
            const float len = 0.42f; int n = Dsp.N(len); var mix = Dsp.Noise(n);
            Dsp.BandpassSweep(mix, t => 500f + 2600f * Mathf.Clamp01(t / len), 0.9f);
            Dsp.Env(mix, t => Mathf.Sin(Mathf.Clamp01(t / len) * Mathf.PI) * Mathf.Exp(-t * 2.5f));
            Dsp.Normalize(mix, 0.6f); return mix;
        }

        /// <summary>Aero engines somewhere overhead in the dark: a low, beating drone that swells and hangs.</summary>
        static float[] MakeDrone()
        {
            const float len = 6f; int n = Dsp.N(len); var mix = new float[n];
            foreach (var f in new[] { 68f, 70.5f }) for (int h = 1; h <= 5; h++) { var s = Dsp.Sine(n, t => f * h); Dsp.Add(mix, s, 0.5f / h); }   // two engines a little apart: the beat
            var air = Dsp.Noise(n); Dsp.Lowpass(air, 260f); Dsp.Add(mix, air, 0.6f);
            Dsp.Env(mix, t => Mathf.Min(1f, t / 2.5f) * Mathf.Min(1f, (len - t) / 1.5f));
            Dsp.Normalize(mix, 0.7f); return mix;
        }

        static float[] MakeWhistle()
        {
            int n = Dsp.N(1.3f); var air = Dsp.Noise(n);
            Dsp.BandpassSweep(air, t => 2600f * Mathf.Pow(0.25f, t / 1.3f), 6f);
            Dsp.Env(air, t => Mathf.Min(1f, t * 2.5f) * (t < 1.15f ? 1f : (1.3f - t) / 0.15f)); Dsp.Normalize(air, 1f);
            var tone = Dsp.Sine(n, t => 2600f * Mathf.Pow(0.25f, t / 1.3f)); Dsp.Env(tone, t => Mathf.Min(1f, t * 2.5f) * (t < 1.15f ? 1f : (1.3f - t) / 0.15f));
            var mix = new float[n]; Dsp.Add(mix, air, 1f); Dsp.Add(mix, tone, 0.35f); Dsp.Normalize(mix, 0.8f); return mix;
        }

        static float[] MakeRumble()
        {
            int n = Dsp.N(3f); var mix = new float[n];
            foreach (var at in new[] { 0f, 0.4f, 0.9f }) { var thud = Dsp.Noise(Dsp.N(2f)); Dsp.Lowpass(thud, 120f); Dsp.Env(thud, t => Mathf.Min(1f, t * 20f) * Dsp.Exp(t, 1.5f)); Dsp.Add(mix, thud, 1f, at); }
            Dsp.Lowpass(mix, 160f); Dsp.Normalize(mix, 0.7f); return mix;
        }

        static float[] Wind()
        {
            int n = Rate * 3; var w = Dsp.Noise(n); Dsp.Lowpass(w, 350f); Dsp.Env(w, t => 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * t / 3f));
            var whistle = Dsp.Noise(n); Dsp.Bandpass(whistle, 700f, 3f); Dsp.Env(whistle, t => Mathf.Pow(0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * t / 3f + 1f), 2f));
            var mix = new float[n]; Dsp.Add(mix, w, 1f); Dsp.Add(mix, whistle, 0.1f); Dsp.Normalize(mix, 0.5f); return mix;
        }

        static float[] Rain()
        {
            int n = Rate * 3; var mix = Dsp.Noise(n); Dsp.Highpass(mix, 2500f); Dsp.Gain(mix, 0.35f);
            var body = Dsp.Noise(n); Dsp.Lowpass(body, 500f); Dsp.Add(mix, body, 0.3f);
            for (int i = 0; i < 700; i++) { var drop = Dsp.Noise(Dsp.N(0.003f)); Dsp.Highpass(drop, 4000f); Dsp.Add(mix, drop, 0.5f, (float)Dsp.rng.NextDouble() * 2.99f); }
            Dsp.Normalize(mix, 0.6f); return mix;
        }

        /// <summary>Radio squelch and a tone or three: the pickup and the level-up.</summary>
        static float[] Radio(float[] notes, float noteLen)
        {
            int n = Dsp.N(0.09f + notes.Length * noteLen); var mix = new float[n];
            var sq = Dsp.Noise(Dsp.N(0.06f)); Dsp.Bandpass(sq, 1800f, 2f); Dsp.Env(sq, t => Dsp.Exp(t, 40f)); Dsp.Add(mix, sq, 0.6f);
            for (int k = 0; k < notes.Length; k++) { float f = notes[k]; var tone = Dsp.Sine(Dsp.N(noteLen), t => f * (1f + 0.004f * Mathf.Sin(2f * Mathf.PI * 6f * t))); Dsp.Clip(tone, 2.5f); Dsp.Env(tone, t => Mathf.Min(1f, t * 60f) * Dsp.Exp(t, 4f)); Dsp.Add(mix, tone, 0.35f, 0.07f + k * noteLen); }
            Dsp.Normalize(mix, 0.6f); return mix;
        }

        static float[] MakeClick()
        {
            int n = Dsp.N(0.08f); var mix = Dsp.Noise(n); Dsp.Highpass(mix, 2000f); Dsp.Env(mix, t => Dsp.Exp(t, 500f));
            var p = Dsp.Sine(n, t => 2400f); Dsp.Env(p, t => Dsp.Exp(t, 120f)); Dsp.Add(mix, p, 0.5f); Dsp.Normalize(mix, 0.5f); return mix;
        }

        /// <summary>A shell going faster than sound: a sharp, bright snap of a quarter second over the shot's boom.</summary>
        static float[] MakeCrack()
        {
            int n = Dsp.N(0.28f); var mix = new float[n];
            var snap = Dsp.Noise(n); Dsp.Highpass(snap, 2600f); Dsp.Env(snap, t => Dsp.Exp(t, 90f)); Dsp.Add(mix, snap, 1f);
            var body = Dsp.Noise(n); Dsp.Bandpass(body, 1200f, 0.9f); Dsp.Env(body, t => Dsp.Exp(t, 35f)); Dsp.Add(mix, body, 0.6f);
            Dsp.Clip(mix, 1.6f); Dsp.Echo(mix, (0.09f, 0.25f, 3000f)); Dsp.Normalize(mix, 0.9f); return mix;
        }

        /// <summary>A diesel's clatter, one second that loops: the injectors' knock fourteen times a second, never twice the same.</summary>
        static float[] MakeClatter()
        {
            int n = Dsp.N(1f); var mix = new float[n];
            for (int k = 0; k < 14; k++)
            {
                var knock = Dsp.Noise(Dsp.N(0.03f)); Dsp.Bandpass(knock, 1400f + (float)Dsp.rng.NextDouble() * 600f, 1.2f); Dsp.Env(knock, t => Dsp.Exp(t, 160f));
                Dsp.Add(mix, knock, 0.6f + (float)Dsp.rng.NextDouble() * 0.4f, k / 14f);
            }
            Dsp.Normalize(mix, 0.8f); return mix;
        }

        /// <summary>A camera of the time: the blind's two snaps, open and shut, each with a small spring's ring under it,
        /// then the lever winding the film on in a short ratchet.</summary>
        static float[] MakeShutter()
        {
            int n = Dsp.N(0.42f); var mix = new float[n];
            foreach (var (at, gain, ring) in new[] { (0f, 1f, 2100f), (0.055f, 0.75f, 1500f) })
            {
                var snap = Dsp.Noise(Dsp.N(0.05f)); Dsp.Highpass(snap, 1800f); Dsp.Env(snap, t => Dsp.Exp(t, 260f)); Dsp.Add(mix, snap, gain, at);
                var tone = Dsp.Sine(Dsp.N(0.08f), t => ring); Dsp.Env(tone, t => Dsp.Exp(t, 70f)); Dsp.Add(mix, tone, gain * 0.35f, at);
            }
            for (int k = 0; k < 7; k++) { var tick = Dsp.Noise(Dsp.N(0.012f)); Dsp.Highpass(tick, 3500f); Dsp.Env(tick, t => Dsp.Exp(t, 500f)); Dsp.Add(mix, tick, 0.22f, 0.2f + k * 0.022f); }
            Dsp.Normalize(mix, 0.8f); return mix;
        }

        /// <summary>The library clip when it is there, else the synthesised stand-in.</summary>
        AudioClip Load(string name, System.Func<float[]> fallback) { var c = Resources.Load<AudioClip>("Audio/" + name); return c != null ? c : Clip(name, fallback()); }

        void Make()
        {
            shot = Load("shot", () => Gun(0f, 1.8f, false)); shotHeavy = Load("shotHeavy", () => Gun(1f, 2.4f, false)); shotFar = Load("shotFar", () => Gun(0.4f, 2f, true));
            flak = Load("flak", () => Gun(0.3f, 0.6f, false)); reload = Resources.Load<AudioClip>("Audio/reload"); turretLoop = Resources.Load<AudioClip>("Audio/turret");
            mg = Load("mg", () => Gun(0f, 0.3f, false)); faust = Load("faust", () => Gun(0.2f, 0.8f, false));
            explosion = Load("explosion", MakeExplosion); artillery = Load("artillery", MakeExplosion); hit = Load("hit", ArmourHit); ricochet = Load("ricochet", MakeRicochet); ricochet2 = Resources.Load<AudioClip>("Audio/ricochet2");
            engineLoop = Load("engine", MakeEngine); tracksLoop = Load("tracks", Tracks);
            idleLoop = Resources.Load<AudioClip>("Audio/engine_idle"); enemyLoop = Resources.Load<AudioClip>("Audio/engine_enemy");
            mg42 = Resources.Load<AudioClip>("Audio/mg42"); shotEnemy = Resources.Load<AudioClip>("Audio/shotEnemy");
            hits = Takes(hit, "hit2", "hit3"); blasts = Takes(explosion, "explosion2", "explosion3"); shells = Takes(artillery, "artillery2", "artillery3");
            whistle = Load("whistle", MakeWhistle); stuka = Load("stuka", MakeStukaDive); drone = Load("drone", MakeDrone); crunch = Load("crunch", MakeCrunch); brush = Load("brush", MakeBrush); whoosh = Load("whoosh", MakeWhoosh); fighter = Load("fighter", MakeFighterPass); rumble = Load("shotFar", MakeRumble); wind = Load("wind", Wind); rain = Load("rain", Rain); front = Resources.Load<AudioClip>("Audio/front");
            pickup = Load("pickup", () => Radio(new[] { 880f, 1320f }, 0.12f)); levelUp = Load("levelUp", () => Radio(new[] { 523f, 659f, 784f }, 0.2f)); click = Load("click", MakeClick); shutter = Load("shutter", MakeShutter); crack = Clip("crack", MakeCrack()); clatter = Clip("clatter", MakeClatter());

            for (int i = 0; i < 12; i++) { var s = NewSource("Voice " + i); s.spatialBlend = 0.75f; s.rolloffMode = AudioRolloffMode.Linear; s.minDistance = 12f; s.maxDistance = 140f; pool.Add(s); }
            engine = NewSource("Engine"); engine.clip = engineLoop; engine.loop = true; engine.spatialBlend = 0f; engine.volume = 0f; engine.Play();
            idle = NewSource("Idle"); idle.clip = idleLoop; idle.loop = true; idle.spatialBlend = 0f; idle.volume = 0f; if (idleLoop != null) idle.Play();
            enemyEngine = NewSource("EnemyEngine"); enemyEngine.clip = enemyLoop; enemyEngine.loop = true; enemyEngine.spatialBlend = 0.6f; enemyEngine.volume = 0f; if (enemyLoop != null) enemyEngine.Play();
            tracks = NewSource("Tracks"); tracks.clip = tracksLoop; tracks.loop = true; tracks.spatialBlend = 0f; tracks.volume = 0f; tracks.Play();
            diesel = NewSource("Diesel"); diesel.clip = clatter; diesel.loop = true; diesel.spatialBlend = 0f; diesel.volume = 0f; diesel.Play();
            turret = NewSource("Turret"); turret.clip = turretLoop; turret.loop = true; turret.spatialBlend = 0f; turret.volume = 0f; if (turretLoop != null) turret.Play();
            frontLine = NewSource("Front"); frontLine.clip = front; frontLine.loop = true; frontLine.spatialBlend = 0f; frontLine.volume = 0.14f; if (front != null) frontLine.Play();
            ui = NewSource("Ui"); ui.spatialBlend = 0f;
            voice = NewSource("Voice"); voice.spatialBlend = 0f; voice.priority = 0;
            foreach (var own in new[] { engine, idle, tracks, turret, enemyEngine }) own.priority = 8;   // the tank's own sounds are never the ones dropped in a crowd of blasts
            ambient = NewSource("Wind"); ambient.clip = wind; ambient.loop = true; ambient.spatialBlend = 0f; ambient.volume = 0.18f; ambient.Play();
        }

        /// <summary>A sound and the other takes of it that are there, to take turns so no two blasts sound the same.</summary>
        static AudioClip[] Takes(AudioClip first, params string[] more)
        {
            var list = new List<AudioClip> { first }; foreach (var n in more) { var c = Resources.Load<AudioClip>("Audio/" + n); if (c != null) list.Add(c); }
            return list.ToArray();
        }
        static AudioClip Any(AudioClip[] takes) => takes[Random.Range(0, takes.Length)];

        AudioSource NewSource(string name) { var go = new GameObject(name); go.transform.SetParent(transform, false); var s = go.AddComponent<AudioSource>(); s.playOnAwake = false; s.dopplerLevel = 0f; return s; }

        void PlayAt(AudioClip clip, Vector3 pos, float volume, float pitch, float delay = 0f)
        {
            AudioSource best = null; float oldest = float.MaxValue;
            foreach (var s in pool) { if (!s.isPlaying) { best = s; break; } if (s.time < oldest) { oldest = s.time; best = s; } }
            best.transform.position = pos; best.clip = clip; best.volume = volume; best.pitch = pitch; if (delay > 0f) best.PlayDelayed(delay); else best.Play();
        }

        /// <summary>A gun firing: the heavy clip for the 17-pounder, the 88 and the Tigers; the far clip when the listener is more than 45 m away.</summary>
        /// <summary>A gun by what fires it (VehicleSpec id): the heavy clip or the 75's, how high it sings, and how much of the
        /// supersonic crack goes over it. The 88 cracks, the 17-pounder is the loudest thing on the field, the 122 booms.</summary>
        public static void Shot(Vector3 pos, bool friendly, string gun)
        {
            if (!instance) return;
            bool heavy; float pitch, snap;
            switch (gun)
            {
                case "tiger": case "tigerace": case "kingtiger": case "flak88": heavy = true; pitch = 1.12f; snap = 1f; break;
                case "firefly": case "m10": heavy = true; pitch = 1.08f; snap = 0.85f; break;
                case "is2": heavy = true; pitch = 0.78f; snap = 0.35f; break;
                case "su100": heavy = true; pitch = 0.88f; snap = 0.55f; break;
                case "pershing": heavy = true; pitch = 0.97f; snap = 0.6f; break;
                case "t34_85": heavy = true; pitch = 1.03f; snap = 0.5f; break;
                case "panther": case "easy8": case "hellcat": case "kv85": heavy = false; pitch = 1.1f; snap = 0.45f; break;
                case "chaffee": heavy = false; pitch = 1.18f; snap = 0f; break;
                default: heavy = false; pitch = 1f; snap = 0f; break;
            }
            bool far = (pos - instance.listener.position).magnitude > 45f;
            var clip = far ? instance.shotFar : !friendly && instance.shotEnemy != null && Random.value < 0.6f ? instance.shotEnemy : heavy ? instance.shotHeavy : instance.shot;   // the enemy's guns mostly their own report
            instance.PlayAt(clip, pos, friendly ? 0.9f : 0.8f, pitch * Random.Range(0.95f, 1.05f));
            if (snap > 0f && !far && instance.crack != null) instance.PlayAt(instance.crack, pos, 0.7f * snap, Random.Range(0.94f, 1.06f));
        }
        /// <summary>The leader's engine by its make: the Sherman's radial, the light tanks' high whine, the heavy ones' growl,
        /// and under the Soviet diesels a clatter.</summary>
        public static void EngineVoice(string tank)
        {
            if (!instance) return;
            switch (tank)
            {
                case "hellcat": instance.engineBase = 1.2f; instance.dieselLevel = 0f; break;
                case "chaffee": instance.engineBase = 1.15f; instance.dieselLevel = 0f; break;
                case "pershing": instance.engineBase = 0.92f; instance.dieselLevel = 0f; break;
                case "easy8": instance.engineBase = 0.97f; instance.dieselLevel = 0f; break;
                case "m10": instance.engineBase = 0.95f; instance.dieselLevel = 0.5f; break;
                case "t34_85": case "su100": instance.engineBase = 0.86f; instance.dieselLevel = 1f; break;
                case "kv85": instance.engineBase = 0.8f; instance.dieselLevel = 1f; break;
                case "is2": instance.engineBase = 0.78f; instance.dieselLevel = 1f; break;
                default: instance.engineBase = 1f; instance.dieselLevel = 0f; break;
            }
        }
        public static void Shot(Vector3 pos, bool friendly, bool heavy)
        {
            if (!instance) return; bool far = (pos - instance.listener.position).magnitude > 45f;
            instance.PlayAt(far ? instance.shotFar : heavy ? instance.shotHeavy : instance.shot, pos, friendly ? 0.9f : 0.8f, (heavy ? 0.9f : 1f) * Random.Range(0.94f, 1.06f));
        }
        public static void Hit(Vector3 pos) { if (instance) instance.PlayAt(Any(instance.hits), pos, 0.7f, Random.Range(0.9f, 1.1f)); }
        public static void Ricochet(Vector3 pos) { if (instance) instance.PlayAt(instance.ricochet2 != null && Random.value < 0.4f ? instance.ricochet2 : instance.ricochet, pos, 0.85f, Random.Range(0.92f, 1.1f)); }
        public static void Explosion(Vector3 pos) { if (instance) instance.PlayAt(Any(instance.blasts), pos, 1f, Random.Range(0.92f, 1.05f)); }
        /// <summary>An artillery shell landing: a shorter, harder blast than a vehicle going up.</summary>
        public static void Artillery(Vector3 pos) { if (instance) instance.PlayAt(Any(instance.shells), pos, 0.7f, Random.Range(0.95f, 1.1f)); }
        /// <summary>The breech after one of ours fires: a clank half a second later, from the tank.</summary>
        public static void Reload(Vector3 pos) { if (instance && instance.reload != null) instance.PlayAt(instance.reload, pos, 0.5f, Random.Range(0.95f, 1.05f), 0.5f); }
        /// <summary>The leader's turret motor: audible while the turret swings, quiet when it rests.</summary>
        public static void Turret(float swing) { if (!instance || instance.turretLoop == null) return; var t = instance.turret; t.volume = Mathf.Lerp(t.volume, Mathf.Clamp01(swing * 0.6f) * 0.3f, 0.2f); }
        public static void StukaDive(Vector3 pos) { if (instance) instance.PlayAt(instance.stuka, pos, 1f, Random.Range(0.97f, 1.03f)); }
        public static void FighterPass(Vector3 pos) { if (instance) instance.PlayAt(instance.fighter, pos, 1f, Random.Range(0.96f, 1.04f)); }
        public static void Crunch(Vector3 pos) { if (instance) instance.PlayAt(instance.crunch, pos, 0.55f, Random.Range(0.85f, 1.15f)); }
        public static void Whoosh() { if (instance && instance.whoosh != null) instance.ui.PlayOneShot(instance.whoosh, 0.28f); }
        public static void Brush(Vector3 pos) { if (instance) instance.PlayAt(instance.brush, pos, 0.7f, Random.Range(0.88f, 1.12f)); }
        public static void Drone(Vector3 pos) { if (instance) instance.PlayAt(instance.drone, pos, 0.9f, 1f); }
        public static void Whistle(Vector3 pos) { if (instance) instance.PlayAt(instance.whistle, pos, 0.7f, Random.Range(0.95f, 1.05f)); }
        public static void Mg(Vector3 pos) { if (instance) instance.PlayAt(instance.mg, pos, 0.55f, Random.Range(0.95f, 1.05f)); }
        /// <summary>A German machine gunner's burst: the MG 42's tearing rattle, the tanks' own Browning when it is missing.</summary>
        public static void Mg42(Vector3 pos) { if (instance) instance.PlayAt(instance.mg42 != null ? instance.mg42 : instance.mg, pos, 0.5f, Random.Range(0.96f, 1.04f)); }
        public static void Faust(Vector3 pos) { if (instance) instance.PlayAt(instance.faust, pos, 0.8f, Random.Range(0.95f, 1.05f)); }
        public static void Flak(Vector3 pos) { if (instance) instance.PlayAt(instance.flak, pos, 0.4f, Random.Range(1.1f, 1.3f)); }
        /// <summary>Distant barrage somewhere over the horizon.</summary>
        public static void Rumble() { if (instance) { instance.ui.pitch = 0.75f; instance.ui.PlayOneShot(instance.rumble, 0.5f); instance.ui.pitch = 1f; } }
        /// <summary>The night's ambient bed: wind, or rain on the hull.</summary>
        public static void Ambient(bool raining) { if (!instance) return; var a = instance.ambient; a.clip = raining ? instance.rain : instance.wind; a.volume = raining ? 0.32f : 0.18f; a.Play(); }
        public static void Pickup() { if (instance) instance.ui.PlayOneShot(instance.pickup, 0.6f); }
        /// <summary>A welder in the hangar: 1 while the arc burns, 0 between. The loop is made the first time it is wanted.</summary>
        public static void Weld(float level)
        {
            if (!instance) return; var s = instance.weld;
            if (s == null)
            {
                if (level <= 0f) return;
                s = instance.weld = instance.gameObject.AddComponent<AudioSource>(); s.clip = instance.Clip("weld", Crackle()); s.loop = true; s.playOnAwake = false; s.spatialBlend = 0f; s.volume = 0f; s.Play();
            }
            s.volume = level * 0.14f;
        }
        /// <summary>The arc's sound: the metal spitting (hundreds of short bright pops a second) over a hiss and the buzz of
        /// the current, in a loop whose seam falls on a whole number of the buzz's cycles.</summary>
        static float[] Crackle()
        {
            int n = Dsp.N(2.5f); var d = new float[n]; var r = Dsp.rng;
            var hiss = Dsp.Noise(n); Dsp.Highpass(hiss, 2500f); Dsp.Add(d, hiss, 0.1f);
            for (int k = 0; k < 950; k++)
            {
                int at = r.Next(n), len = 40 + r.Next(280); float g = (float)(0.15 + r.NextDouble() * 0.85);
                for (int i = 0; i < len && at + i < n; i++) d[at + i] += (float)(r.NextDouble() * 2.0 - 1.0) * g * Mathf.Exp(-i / (len * 0.25f));
            }
            Dsp.Add(d, Dsp.Sine(n, t => 100f), 0.04f);
            Dsp.Highpass(d, 450f); Dsp.Normalize(d, 0.8f); return d;
        }
        public static void Click() { if (instance) instance.ui.PlayOneShot(instance.click, 0.5f); }
        public static void Shutter() { if (instance) instance.ui.PlayOneShot(instance.shutter, 0.8f); }
        public static void LevelUp() { if (instance) instance.ui.PlayOneShot(instance.levelUp, 0.6f); }
        /// <summary>The leader's engine and tracks: louder and higher when driving.</summary>
        public static void Engine(float throttle)
        {
            if (!instance) return; var e = instance.engine;
            if (instance.idleLoop != null)
            {
                // two layers: the idle fades out as the drive comes in, so a tank at rest idles
                var id = instance.idle; id.volume = Mathf.Lerp(id.volume, 0.36f * (1f - throttle) + 0.06f, 0.1f); id.pitch = Mathf.Lerp(id.pitch, (0.95f + 0.15f * throttle) * instance.engineBase, 0.08f);
                e.volume = Mathf.Lerp(e.volume, 0.08f + 0.5f * throttle, 0.1f); e.pitch = Mathf.Lerp(e.pitch, (0.88f + 0.22f * throttle) * instance.engineBase, 0.08f);
            }
            else { e.volume = Mathf.Lerp(e.volume, 0.12f + 0.3f * throttle, 0.1f); e.pitch = Mathf.Lerp(e.pitch, (0.85f + 0.45f * throttle) * instance.engineBase, 0.08f); }
            var dz = instance.diesel; if (dz != null) { dz.volume = Mathf.Lerp(dz.volume, instance.dieselLevel * (0.05f + 0.13f * throttle), 0.1f); dz.pitch = Mathf.Lerp(dz.pitch, 0.9f + 0.35f * throttle, 0.08f); }
            var tr = instance.tracks; tr.volume = Mathf.Lerp(tr.volume, 0.24f * throttle, 0.15f); tr.pitch = Mathf.Lerp(tr.pitch, 0.8f + 0.4f * throttle, 0.1f);
        }
        /// <summary>The nearest enemy tank's engine: where it is, as loud as it is near the leader (closeness 1 beside him,
        /// 0 out of earshot); heavier tanks lower.</summary>
        public static void EnemyEngine(Vector3 pos, float closeness, float heavy)
        {
            if (!instance || instance.enemyLoop == null) return; var s = instance.enemyEngine; s.transform.position = pos;
            s.volume = Mathf.Lerp(s.volume, 0.45f * Mathf.Clamp01(closeness), 0.06f); s.pitch = Mathf.Lerp(s.pitch, 1.05f - 0.2f * Mathf.Clamp01(heavy), 0.05f);
        }

        public static void Quiet(bool q) { if (instance) { if (instance.idle != null) instance.idle.mute = q; if (instance.enemyEngine != null) instance.enemyEngine.mute = q; if (instance.diesel != null) instance.diesel.mute = q; instance.engine.mute = q; instance.tracks.mute = q; instance.turret.mute = q; instance.ambient.mute = q; instance.frontLine.mute = q; } }
    }
}

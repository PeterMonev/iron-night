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
        AudioClip shot, shotHeavy, shotFar, hit, explosion, artillery, pickup, click, levelUp, engineLoop, tracksLoop, wind, rain, front, whistle, rumble, ricochet, ricochet2, flak, reload, turretLoop;
        readonly List<AudioSource> pool = new List<AudioSource>(); AudioSource engine, tracks, turret, ambient, frontLine, ui; Transform listener;

        public static void Build(Camera cam)
        {
            var go = new GameObject("Sfx"); instance = go.AddComponent<Sfx>();
            if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
            instance.listener = cam.transform; instance.Make(); AudioListener.volume = Muted ? 0f : 1f;
        }

        /// <summary>The sound switch on the pause sheet; remembered between nights.</summary>
        public static bool Muted { get => PlayerPrefs.GetInt("sound", 1) == 0; set { PlayerPrefs.SetInt("sound", value ? 0 : 1); PlayerPrefs.Save(); AudioListener.volume = value ? 0f : 1f; } }

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

        /// <summary>The library clip when it is there, else the synthesised stand-in.</summary>
        AudioClip Load(string name, System.Func<float[]> fallback) { var c = Resources.Load<AudioClip>("Audio/" + name); return c != null ? c : Clip(name, fallback()); }

        void Make()
        {
            shot = Load("shot", () => Gun(0f, 1.8f, false)); shotHeavy = Load("shotHeavy", () => Gun(1f, 2.4f, false)); shotFar = Load("shotFar", () => Gun(0.4f, 2f, true));
            flak = Load("flak", () => Gun(0.3f, 0.6f, false)); reload = Resources.Load<AudioClip>("Audio/reload"); turretLoop = Resources.Load<AudioClip>("Audio/turret");
            explosion = Load("explosion", MakeExplosion); artillery = Load("artillery", MakeExplosion); hit = Load("hit", ArmourHit); ricochet = Load("ricochet", MakeRicochet); ricochet2 = Resources.Load<AudioClip>("Audio/ricochet2");
            engineLoop = Load("engine", MakeEngine); tracksLoop = Load("tracks", Tracks);
            whistle = Load("whistle", MakeWhistle); rumble = Load("shotFar", MakeRumble); wind = Load("wind", Wind); rain = Load("rain", Rain); front = Resources.Load<AudioClip>("Audio/front");
            pickup = Load("pickup", () => Radio(new[] { 880f, 1320f }, 0.12f)); levelUp = Load("levelUp", () => Radio(new[] { 523f, 659f, 784f }, 0.2f)); click = Load("click", MakeClick);

            for (int i = 0; i < 12; i++) { var s = NewSource("Voice " + i); s.spatialBlend = 0.75f; s.rolloffMode = AudioRolloffMode.Linear; s.minDistance = 12f; s.maxDistance = 140f; pool.Add(s); }
            engine = NewSource("Engine"); engine.clip = engineLoop; engine.loop = true; engine.spatialBlend = 0f; engine.volume = 0f; engine.Play();
            tracks = NewSource("Tracks"); tracks.clip = tracksLoop; tracks.loop = true; tracks.spatialBlend = 0f; tracks.volume = 0f; tracks.Play();
            turret = NewSource("Turret"); turret.clip = turretLoop; turret.loop = true; turret.spatialBlend = 0f; turret.volume = 0f; if (turretLoop != null) turret.Play();
            frontLine = NewSource("Front"); frontLine.clip = front; frontLine.loop = true; frontLine.spatialBlend = 0f; frontLine.volume = 0.14f; if (front != null) frontLine.Play();
            ui = NewSource("Ui"); ui.spatialBlend = 0f;
            ambient = NewSource("Wind"); ambient.clip = wind; ambient.loop = true; ambient.spatialBlend = 0f; ambient.volume = 0.18f; ambient.Play();
        }

        AudioSource NewSource(string name) { var go = new GameObject(name); go.transform.SetParent(transform, false); var s = go.AddComponent<AudioSource>(); s.playOnAwake = false; s.dopplerLevel = 0f; return s; }

        void PlayAt(AudioClip clip, Vector3 pos, float volume, float pitch, float delay = 0f)
        {
            AudioSource best = null; float oldest = float.MaxValue;
            foreach (var s in pool) { if (!s.isPlaying) { best = s; break; } if (s.time < oldest) { oldest = s.time; best = s; } }
            best.transform.position = pos; best.clip = clip; best.volume = volume; best.pitch = pitch; if (delay > 0f) best.PlayDelayed(delay); else best.Play();
        }

        /// <summary>A gun firing: the heavy clip for the 17-pounder, the 88 and the Tigers; the far clip when the listener is more than 45 m away.</summary>
        public static void Shot(Vector3 pos, bool friendly, bool heavy)
        {
            if (!instance) return; bool far = (pos - instance.listener.position).magnitude > 45f;
            instance.PlayAt(far ? instance.shotFar : heavy ? instance.shotHeavy : instance.shot, pos, friendly ? 0.9f : 0.8f, (heavy ? 0.9f : 1f) * Random.Range(0.94f, 1.06f));
        }
        public static void Hit(Vector3 pos) { if (instance) instance.PlayAt(instance.hit, pos, 0.85f, Random.Range(0.9f, 1.1f)); }
        public static void Ricochet(Vector3 pos) { if (instance) instance.PlayAt(instance.ricochet2 != null && Random.value < 0.4f ? instance.ricochet2 : instance.ricochet, pos, 0.85f, Random.Range(0.92f, 1.1f)); }
        public static void Explosion(Vector3 pos) { if (instance) instance.PlayAt(instance.explosion, pos, 1f, Random.Range(0.92f, 1.05f)); }
        /// <summary>An artillery shell landing: a shorter, harder blast than a vehicle going up.</summary>
        public static void Artillery(Vector3 pos) { if (instance) instance.PlayAt(instance.artillery, pos, 0.9f, Random.Range(0.95f, 1.1f)); }
        /// <summary>The breech after one of ours fires: a clank half a second later, from the tank.</summary>
        public static void Reload(Vector3 pos) { if (instance && instance.reload != null) instance.PlayAt(instance.reload, pos, 0.5f, Random.Range(0.95f, 1.05f), 0.5f); }
        /// <summary>The leader's turret motor: audible while the turret swings, quiet when it rests.</summary>
        public static void Turret(float swing) { if (!instance || instance.turretLoop == null) return; var t = instance.turret; t.volume = Mathf.Lerp(t.volume, Mathf.Clamp01(swing * 0.6f) * 0.35f, 0.2f); }
        public static void Whistle(Vector3 pos) { if (instance) instance.PlayAt(instance.whistle, pos, 0.7f, Random.Range(0.95f, 1.05f)); }
        public static void Flak(Vector3 pos) { if (instance) instance.PlayAt(instance.flak, pos, 0.4f, Random.Range(1.1f, 1.3f)); }
        /// <summary>Distant barrage somewhere over the horizon.</summary>
        public static void Rumble() { if (instance) { instance.ui.pitch = 0.75f; instance.ui.PlayOneShot(instance.rumble, 0.5f); instance.ui.pitch = 1f; } }
        /// <summary>The night's ambient bed: wind, or rain on the hull.</summary>
        public static void Ambient(bool raining) { if (!instance) return; var a = instance.ambient; a.clip = raining ? instance.rain : instance.wind; a.volume = raining ? 0.32f : 0.18f; a.Play(); }
        public static void Pickup() { if (instance) instance.ui.PlayOneShot(instance.pickup, 0.6f); }
        public static void Click() { if (instance) instance.ui.PlayOneShot(instance.click, 0.5f); }
        public static void LevelUp() { if (instance) instance.ui.PlayOneShot(instance.levelUp, 0.6f); }
        /// <summary>The leader's engine and tracks: louder and higher when driving.</summary>
        public static void Engine(float throttle)
        {
            if (!instance) return; var e = instance.engine; e.volume = Mathf.Lerp(e.volume, 0.12f + 0.3f * throttle, 0.1f); e.pitch = Mathf.Lerp(e.pitch, 0.85f + 0.45f * throttle, 0.08f);
            var tr = instance.tracks; tr.volume = Mathf.Lerp(tr.volume, 0.16f * throttle, 0.15f); tr.pitch = Mathf.Lerp(tr.pitch, 0.8f + 0.4f * throttle, 0.1f);
        }
        public static void Quiet(bool q) { if (instance) { instance.engine.mute = q; instance.tracks.mute = q; instance.turret.mute = q; instance.ambient.mute = q; instance.frontLine.mute = q; } }
    }
}

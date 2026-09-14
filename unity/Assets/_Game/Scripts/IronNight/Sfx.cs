using System.Collections.Generic;
using UnityEngine;

namespace IronNight
{
    /// <summary>
    /// All sound is synthesised at start-up (no audio files, no licenses): tank guns, armour hits, explosions, the engine
    /// drone, the flare pickup, UI ticks. Clips are short mono buffers played from a pool of positional sources.
    /// </summary>
    public class Sfx : MonoBehaviour
    {
        const int Rate = 44100;
        static Sfx instance;
        AudioClip shot, shotFar, hit, explosion, pickup, click, levelUp, engineLoop, wind, whistle;
        readonly List<AudioSource> pool = new List<AudioSource>(); AudioSource engine, ambient, ui;
        System.Random rng = new System.Random(3);

        public static void Build(Camera cam)
        {
            var go = new GameObject("Sfx"); instance = go.AddComponent<Sfx>();
            if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
            instance.Make(); AudioListener.volume = Muted ? 0f : 1f;
        }

        /// <summary>The sound switch on the pause sheet; remembered between nights.</summary>
        public static bool Muted { get => PlayerPrefs.GetInt("sound", 1) == 0; set { PlayerPrefs.SetInt("sound", value ? 0 : 1); PlayerPrefs.Save(); AudioListener.volume = value ? 0f : 1f; } }

        float Noise() => (float)(rng.NextDouble() * 2.0 - 1.0);

        AudioClip Clip(string name, float seconds, System.Func<int, float> sample)
        {
            int n = Mathf.CeilToInt(seconds * Rate); var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(sample(i), -1f, 1f);
            var c = AudioClip.Create(name, n, 1, Rate, false); c.SetData(data, 0); return c;
        }

        void Make()
        {
            // a tank gun: a hard crack of noise, a low thump, and a rolling tail
            float lp = 0f;
            shot = Clip("shot", 0.9f, i => {
                float t = i / (float)Rate; float crack = Noise() * Mathf.Exp(-t * 70f);
                lp = lp * 0.86f + Noise() * 0.14f; float tail = lp * 1.8f * Mathf.Exp(-t * 6f);
                float thump = Mathf.Sin(2f * Mathf.PI * 55f * t) * Mathf.Exp(-t * 14f) * 0.9f;
                return (crack * 0.9f + tail + thump) * 0.8f; });
            lp = 0f;
            shotFar = Clip("shotFar", 1.2f, i => { float t = i / (float)Rate; lp = lp * 0.94f + Noise() * 0.06f; return (lp * 2.2f * Mathf.Exp(-t * 4f) + Mathf.Sin(2f * Mathf.PI * 45f * t) * Mathf.Exp(-t * 8f) * 0.7f) * 0.7f; });
            // armour hit: a metallic ring on top of a short burst
            hit = Clip("hit", 0.5f, i => { float t = i / (float)Rate; float ring = (Mathf.Sin(2f * Mathf.PI * 1180f * t) + 0.6f * Mathf.Sin(2f * Mathf.PI * 1930f * t) + 0.4f * Mathf.Sin(2f * Mathf.PI * 2710f * t)) * Mathf.Exp(-t * 12f) * 0.35f; float burst = Noise() * Mathf.Exp(-t * 90f) * 0.8f; return ring + burst; });
            // explosion: deep, long, rumbling
            lp = 0f; float lp2 = 0f;
            explosion = Clip("explosion", 2.4f, i => { float t = i / (float)Rate; lp = lp * 0.97f + Noise() * 0.03f; lp2 = lp2 * 0.995f + Noise() * 0.005f;
                float body = lp * 3.5f * Mathf.Exp(-t * 2.2f) + lp2 * 8f * Mathf.Exp(-t * 1.2f); float sub = Mathf.Sin(2f * Mathf.PI * 38f * t) * Mathf.Exp(-t * 3f) * 0.8f; float crack = Noise() * Mathf.Exp(-t * 40f) * 0.6f; return (body + sub + crack) * 0.85f; });
            // flare pickup: a rising two-tone
            pickup = Clip("pickup", 0.45f, i => { float t = i / (float)Rate; float f = t < 0.2f ? 660f : 990f; return Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-((t % 0.2f) * 12f)) * 0.35f; });
            click = Clip("click", 0.06f, i => { float t = i / (float)Rate; return Mathf.Sin(2f * Mathf.PI * 1400f * t) * Mathf.Exp(-t * 90f) * 0.4f; });
            levelUp = Clip("levelUp", 0.9f, i => { float t = i / (float)Rate; int k = Mathf.Min(2, (int)(t / 0.22f)); float f = new[] { 523f, 659f, 784f }[k]; float lt = t - k * 0.22f; return Mathf.Sin(2f * Mathf.PI * f * lt) * Mathf.Exp(-lt * 5f) * 0.35f; });
            // engine: a low pulse train with grit, seamless loop
            engineLoop = Clip("engine", 1.0f, i => { float t = i / (float)Rate; float pulse = Mathf.Sin(2f * Mathf.PI * 36f * t); pulse = Mathf.Sign(pulse) * Mathf.Pow(Mathf.Abs(pulse), 0.3f); float grit = Noise() * 0.25f; return (pulse * 0.5f + grit) * 0.35f; });
            // an artillery shell coming down: a falling whistle, louder as it nears
            whistle = Clip("whistle", 1.1f, i => { float t = i / (float)Rate; float f = 2400f * Mathf.Pow(0.22f, t / 1.1f); float env = Mathf.Min(1f, t * 3f) * (t < 0.95f ? 1f : (1.1f - t) / 0.15f); return (Mathf.Sin(2f * Mathf.PI * f * t) * 0.3f + Noise() * 0.05f) * env; });
            lp = 0f;
            wind = Clip("wind", 3.0f, i => { lp = lp * 0.985f + Noise() * 0.015f; float t = i / (float)Rate; float env = 0.7f + 0.3f * Mathf.Sin(2f * Mathf.PI * t / 3f); return lp * 4f * env; });

            for (int i = 0; i < 12; i++) { var s = NewSource("Voice " + i); s.spatialBlend = 0.75f; s.rolloffMode = AudioRolloffMode.Linear; s.minDistance = 12f; s.maxDistance = 140f; pool.Add(s); }
            engine = NewSource("Engine"); engine.clip = engineLoop; engine.loop = true; engine.spatialBlend = 0f; engine.volume = 0f; engine.Play();
            ui = NewSource("Ui"); ui.spatialBlend = 0f;
            ambient = NewSource("Wind"); ambient.clip = wind; ambient.loop = true; ambient.spatialBlend = 0f; ambient.volume = 0.18f; ambient.Play();
        }

        AudioSource NewSource(string name) { var go = new GameObject(name); go.transform.SetParent(transform, false); var s = go.AddComponent<AudioSource>(); s.playOnAwake = false; s.dopplerLevel = 0f; return s; }

        void PlayAt(AudioClip clip, Vector3 pos, float volume, float pitch)
        {
            AudioSource best = null; float oldest = float.MaxValue;
            foreach (var s in pool) { if (!s.isPlaying) { best = s; break; } if (s.time < oldest) { oldest = s.time; best = s; } }
            best.transform.position = pos; best.clip = clip; best.volume = volume; best.pitch = pitch; best.Play();
        }

        public static void Shot(Vector3 pos, bool friendly, bool heavy) { if (instance) instance.PlayAt(heavy ? instance.shot : instance.shotFar, pos, heavy ? 0.9f : 0.7f, (friendly ? 1f : 0.85f) * Random.Range(0.94f, 1.06f)); }
        public static void Flak(Vector3 pos) { if (instance) instance.PlayAt(instance.shotFar, pos, 0.35f, 1.6f); }
        public static void Whistle(Vector3 pos) { if (instance) instance.PlayAt(instance.whistle, pos, 0.7f, Random.Range(0.95f, 1.05f)); }
        public static void Hit(Vector3 pos) { if (instance) instance.PlayAt(instance.hit, pos, 0.8f, Random.Range(0.9f, 1.1f)); }
        public static void Explosion(Vector3 pos) { if (instance) instance.PlayAt(instance.explosion, pos, 1f, Random.Range(0.9f, 1.05f)); }
        public static void Pickup() { if (instance) instance.ui.PlayOneShot(instance.pickup, 0.6f); }
        public static void Click() { if (instance) instance.ui.PlayOneShot(instance.click, 0.5f); }
        public static void LevelUp() { if (instance) instance.ui.PlayOneShot(instance.levelUp, 0.6f); }
        /// <summary>The leader's engine: louder and higher when driving.</summary>
        public static void Engine(float throttle) { if (!instance) return; var e = instance.engine; e.volume = Mathf.Lerp(e.volume, 0.12f + 0.3f * throttle, 0.1f); e.pitch = Mathf.Lerp(e.pitch, 0.85f + 0.45f * throttle, 0.08f); }
        public static void Quiet(bool q) { if (instance) { instance.engine.mute = q; instance.ambient.mute = q; } }
    }
}

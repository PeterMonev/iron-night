using System.Collections.Generic;
using UnityEngine;

namespace IronNight
{
    /// <summary>
    /// Every light effect of the fight as camera-facing quads plus a few point lights: muzzle flashes with a flame
    /// tongue and a smoke puff, tracer trails, armour hits that spray sparks, explosions with a fireball, a shockwave
    /// ring and falling debris, burning wrecks under a smoke column, the smoke screen, flak and artillery. Sprites
    /// are procedural (a noisy flame, a ragged smoke ball, a hard spark); quads are pooled and coloured through
    /// property blocks, so nothing but the puff records is allocated during a wave.
    /// </summary>
    public class Fx : MonoBehaviour
    {
        class Puff
        {
            public Transform t; public Renderer r; public float life, age, size, grow, spin, alphaPow, stretch; public Color color; public Vector3 vel, axis;
            public bool smoke, gravity, aligned, flat;
        }

        Material addGlow, addFlame, addSpark, smokeSoft, smokeRagged, addRing;
        Texture2D glowTex, flameTex, smokeTex, sparkTex, ringTex;
        readonly List<Puff> puffs = new List<Puff>(); readonly Stack<GameObject> quadPool = new Stack<GameObject>();
        readonly List<Light> lights = new List<Light>(); readonly List<float> lightLife = new List<float>(); readonly List<float> lightMax = new List<float>();
        readonly MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        Camera cam;

        public void Build(Camera camera)
        {
            cam = camera;
            var additive = Resources.Load<Material>("Additive"); var smoke = Resources.Load<Material>("Smoke");
            glowTex = Lightswarm.ProceduralSprites.Glow(128, 0.12f).texture; ringTex = Lightswarm.ProceduralSprites.Ring(128, 0.09f).texture;
            flameTex = Ragged(128, 0.5f, 0.22f, 7f, true); smokeTex = Ragged(128, 0.62f, 0.3f, 5f, false); sparkTex = Spark(32);
            Material Make(Material template, Texture2D tex) { var m = new Material(template); m.SetTexture("_BaseMap", tex); return m; }
            addGlow = Make(additive, glowTex); addFlame = Make(additive, flameTex); addSpark = Make(additive, sparkTex); addRing = Make(additive, ringTex);
            smokeSoft = Make(smoke, Lightswarm.ProceduralSprites.GroundShadow(128).texture); smokeRagged = Make(smoke, smokeTex);
        }

        /// <summary>A radial blob whose edge is eaten by noise: a flame when bright-cored, a smoke ball otherwise.</summary>
        static Texture2D Ragged(int s, float radius, float edge, float noiseScale, bool hotCore)
        {
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float ox = Random.value * 100f, oy = Random.value * 100f;
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float u = (x + 0.5f) / s - 0.5f, v = (y + 0.5f) / s - 0.5f, r = Mathf.Sqrt(u * u + v * v) * 2f;
                float n = Mathf.PerlinNoise(ox + x / (float)s * noiseScale, oy + y / (float)s * noiseScale) * 0.7f + Mathf.PerlinNoise(ox + x / (float)s * noiseScale * 2.3f, oy + y / (float)s * noiseScale * 2.3f) * 0.3f;
                float rim = radius * 2f * (0.72f + 0.45f * n); float a = Mathf.Clamp01((rim - r) / edge);
                a = Mathf.Pow(a, hotCore ? 1.1f : 1.5f); float core = hotCore ? Mathf.Clamp01(1f - r * 1.3f) : 0f;
                var c = hotCore ? new Color(1f, Mathf.Lerp(0.55f, 1f, core), Mathf.Lerp(0.15f, 0.9f, core * core), a) : new Color(1f, 1f, 1f, a * (0.75f + 0.25f * n));
                tex.SetPixel(x, y, c);
            }
            tex.Apply(); return tex;
        }

        static Texture2D Spark(int s)
        {
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) { float u = (x + 0.5f) / s - 0.5f, v = (y + 0.5f) / s - 0.5f, r = Mathf.Sqrt(u * u + v * v) * 2f; float a = Mathf.Clamp01((0.55f - r) / 0.2f) + 0.35f * Mathf.Clamp01(1f - r); tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(a))); }
            tex.Apply(); return tex;
        }

        GameObject Quad()
        {
            if (quadPool.Count > 0) { var q = quadPool.Pop(); q.SetActive(true); return q; }
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            var r = go.GetComponent<Renderer>(); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            return go;
        }

        Puff Spawn(Material m, Vector3 pos, float size, Color color, float life, Vector3 vel, float grow = 0.8f, bool smoke = false)
        {
            var go = Quad(); var r = go.GetComponent<Renderer>(); r.sharedMaterial = m;
            var p = new Puff { t = go.transform, r = r, life = life, age = 0f, size = size, color = color, smoke = smoke, vel = vel, grow = grow, spin = Random.Range(0f, 360f), alphaPow = smoke ? 2f : 1f, stretch = 1f };
            p.t.position = pos; p.t.localScale = Vector3.one * size;
            puffs.Add(p); return p;
        }

        void Flash(Vector3 pos, Color color, float intensity, float range, float life)
        {
            var go = new GameObject("FxLight"); go.transform.SetParent(transform, false); go.transform.position = pos;
            var l = go.AddComponent<Light>(); l.type = LightType.Point; l.color = color; l.intensity = intensity; l.range = range; l.shadows = LightShadows.None;
            lights.Add(l); lightLife.Add(life); lightMax.Add(life);
        }

        static Color Warm(float k) => new Color(1f, Mathf.Lerp(0.45f, 0.85f, k), Mathf.Lerp(0.1f, 0.4f, k), 1f);

        /// <summary>The gun going off: a white core, a flame tongue along the barrel, a smoke puff rolling forward,
        /// dust kicked up under the muzzle, and the flash of light on everything near.</summary>
        public void MuzzleFlash(Vector3 pos, Vector3 dir)
        {
            Spawn(addGlow, pos + dir * 0.8f, 3.6f, new Color(1f, 0.95f, 0.8f, 1f), 0.07f, Vector3.zero, 0.3f);
            var tongue = Spawn(addFlame, pos + dir * 2.2f, 2.6f, new Color(1f, 0.8f, 0.45f, 1f), 0.1f, dir * 6f, 0.6f); tongue.aligned = true; tongue.axis = dir; tongue.stretch = 2.2f;
            for (int i = 0; i < 3; i++) Spawn(smokeRagged, pos + dir * (1f + i * 1.1f) + Random.insideUnitSphere * 0.4f, 1.6f + Random.value, new Color(0.5f, 0.48f, 0.45f, 0.5f), 1.2f + Random.value * 0.6f, dir * (5f - i) + Vector3.up * 1.2f, 1.8f, true);
            for (int i = 0; i < 2; i++) Spawn(smokeSoft, new Vector3(pos.x, 0.3f, pos.z) + dir * 2f + Random.insideUnitSphere * 0.8f, 2.5f, new Color(0.38f, 0.33f, 0.26f, 0.5f), 1f, Vector3.up * 1.2f + dir * 2f, 1.5f, true);
            Flash(pos + dir * 1.2f + Vector3.up * 0.5f, new Color(1f, 0.8f, 0.5f), 45f, 16f, 0.1f);
        }

        /// <summary>A shell into armour: a hard white flash, a spray of sparks with weight, a flame lick and dark smoke.</summary>
        public void Hit(Vector3 pos, float power)
        {
            Spawn(addGlow, pos, 2.6f * power, new Color(1f, 0.95f, 0.85f, 1f), 0.06f, Vector3.zero, 0.3f);
            Spawn(addFlame, pos, 2.2f * power, Warm(0.5f), 0.22f, Vector3.up * 2f, 1f);
            for (int i = 0; i < 9; i++) { var v = (Random.insideUnitSphere + Vector3.up * 0.8f).normalized * (9f + Random.value * 14f); var s = Spawn(addSpark, pos, 0.25f + Random.value * 0.25f, new Color(1f, 0.85f, 0.45f, 1f), 0.35f + Random.value * 0.45f, v, 0f); s.gravity = true; }
            for (int i = 0; i < 4; i++) Spawn(smokeRagged, pos + Random.insideUnitSphere * 1f, 2f + Random.value * 2f, new Color(0.22f, 0.2f, 0.18f, 0.7f), 2.5f + Random.value, new Vector3(Random.Range(-0.6f, 0.6f), 1.6f + Random.value, Random.Range(-0.6f, 0.6f)), 2f, true);
            Flash(pos + Vector3.up * 1.5f, new Color(1f, 0.6f, 0.3f), 60f * power, 18f, 0.3f);
        }

        /// <summary>A shell glancing off: a white flash and a handful of sparks thrown off the armour.</summary>
        public void Spark(Vector3 pos, Vector3 away)
        {
            Spawn(addGlow, pos, 2.4f, new Color(1f, 0.97f, 0.85f, 1f), 0.08f, Vector3.zero, 0.3f);
            for (int i = 0; i < 7; i++) { var s = Spawn(addSpark, pos, 0.3f + Random.value * 0.3f, new Color(1f, 0.85f, 0.5f, 1f), 0.3f + Random.value * 0.3f, (away + Random.insideUnitSphere * 0.8f + Vector3.up * 0.6f).normalized * (14f + Random.value * 10f), 0f); s.gravity = true; }
            Flash(pos, new Color(1f, 0.9f, 0.7f), 30f, 10f, 0.08f);
        }

        /// <summary>A vehicle blowing up: a white core, a fireball of flame sprites, a shockwave ring on the ground,
        /// chunks thrown in arcs, and a column of black smoke that hangs for seconds.</summary>
        public void Explosion(Vector3 pos)
        {
            Spawn(addGlow, pos + Vector3.up * 1.5f, 10f, new Color(1f, 0.95f, 0.85f, 1f), 0.1f, Vector3.zero, 0.5f);
            for (int i = 0; i < 6; i++) Spawn(addFlame, pos + Random.insideUnitSphere * 1.6f + Vector3.up * (1f + Random.value * 2f), 4.5f + Random.value * 4f, Warm(Random.value), 0.45f + Random.value * 0.35f, Vector3.up * (2f + Random.value * 3f), 1.2f);
            var ring = Spawn(addRing, new Vector3(pos.x, 0.3f, pos.z), 3f, new Color(1f, 0.8f, 0.55f, 0.8f), 0.4f, Vector3.zero, 5f); ring.flat = true;
            for (int i = 0; i < 10; i++) { var v = (Random.insideUnitSphere + Vector3.up * 1.4f).normalized * (10f + Random.value * 16f); var d = Spawn(addSpark, pos + Vector3.up, 0.4f + Random.value * 0.5f, new Color(1f, 0.6f, 0.25f, 1f), 0.8f + Random.value * 0.7f, v, 0f); d.gravity = true; }
            for (int i = 0; i < 9; i++) Spawn(smokeRagged, pos + Random.insideUnitSphere * 2f + Vector3.up, 4f + Random.value * 4f, new Color(0.16f, 0.15f, 0.14f, 0.85f), 4f + Random.value * 3f, new Vector3(Random.Range(-1f, 1f), 2f + Random.value * 2f, Random.Range(-1f, 1f)), 2.4f, true);
            Flash(pos + Vector3.up * 2f, new Color(1f, 0.55f, 0.25f), 240f, 32f, 0.6f);
        }

        /// <summary>Flames licking from a wreck and, every other call, a puff into the smoke column above it.</summary>
        public void Burn(Vector3 pos)
        {
            Spawn(addFlame, pos + Vector3.up * 2f + Random.insideUnitSphere * 0.7f, 2f + Random.value * 1.6f, Warm(Random.value * 0.7f), 0.3f + Random.value * 0.15f, Vector3.up * (2f + Random.value * 1.5f), 0.6f);
            if (Random.value < 0.55f) Spawn(smokeRagged, pos + Vector3.up * 3f + Random.insideUnitSphere * 0.5f, 2.5f + Random.value * 2f, new Color(0.12f, 0.11f, 0.1f, 0.6f), 4.5f + Random.value * 2f, new Vector3(Random.Range(-0.4f, 0.4f), 1.8f + Random.value * 0.6f, Random.Range(-0.4f, 0.4f)), 2.6f, true);
        }

        /// <summary>A shell that missed everything and hit the dirt: a fountain of earth and a few clods.</summary>
        public void Dust(Vector3 pos)
        {
            for (int i = 0; i < 3; i++) Spawn(smokeRagged, pos + Vector3.up * 0.4f + Random.insideUnitSphere * 0.4f, 1.6f + Random.value * 0.8f, new Color(0.4f, 0.34f, 0.26f, 0.7f), 1.1f + Random.value * 0.5f, new Vector3(Random.Range(-1f, 1f), 3.5f + Random.value * 3f, Random.Range(-1f, 1f)), 1.6f, true);
            for (int i = 0; i < 4; i++) { var v = (Random.insideUnitSphere + Vector3.up * 1.6f).normalized * (5f + Random.value * 6f); var c = Spawn(smokeSoft, pos, 0.3f + Random.value * 0.2f, new Color(0.2f, 0.17f, 0.13f, 0.9f), 0.6f + Random.value * 0.4f, v, 0f, true); c.gravity = true; }
            Spawn(addGlow, pos + Vector3.up * 0.3f, 1.4f, new Color(1f, 0.8f, 0.5f, 0.8f), 0.05f, Vector3.zero, 0.2f);
        }

        /// <summary>The smoke screen: a big slow grey cloud that hangs for a while.</summary>
        public void SmokeCloud(Vector3 pos, float life)
        {
            Spawn(smokeRagged, pos, 6f + Random.value * 4f, new Color(0.62f, 0.62f, 0.64f, 0.8f), life, new Vector3(Random.Range(-0.4f, 0.4f), 0.35f, Random.Range(-0.4f, 0.4f)), 0.9f, true);
        }

        /// <summary>A machine-gun tracer: a short bright streak that is gone in a quarter second.</summary>
        public void MgTracer(Vector3 from, Vector3 dir)
        {
            var t = Spawn(addSpark, from, 0.55f, new Color(1f, 0.85f, 0.45f, 1f), 0.24f, dir * 110f, 0f); t.aligned = true; t.axis = dir; t.stretch = 5f;
            Spawn(addGlow, from, 1.1f, new Color(1f, 0.8f, 0.5f, 0.8f), 0.04f, Vector3.zero, 0f);
        }

        /// <summary>A flak tracer climbing from a post: a warm streak that burns out high up.</summary>
        public void Flak(Vector3 from, Vector3 dir)
        {
            var t = Spawn(addSpark, from, 1.2f, new Color(1f, 0.7f, 0.35f, 1f), 1.3f, dir * 130f, 0f); t.aligned = true; t.axis = dir; t.stretch = 4f;
            Spawn(addGlow, from + dir * 0.5f, 3f, new Color(1f, 0.8f, 0.5f, 0.8f), 0.08f, Vector3.zero, 0f);
        }

        /// <summary>An artillery shell falling onto the point: a glowing streak that arrives when the timer ends.</summary>
        public void Incoming(Vector3 at, float seconds)
        {
            float h = 70f; var s = Spawn(addSpark, at + Vector3.up * h, 1.6f, new Color(1f, 0.85f, 0.6f, 0.9f), seconds, Vector3.down * (h / seconds), 0f); s.aligned = true; s.axis = Vector3.down; s.stretch = 5f;
        }

        /// <summary>A shell in flight: a hard bright core with a long soft tail behind it; the root's up axis is the flight direction.</summary>
        public Transform Tracer(Color core, Color tail)
        {
            var root = new GameObject("Shell").transform; root.SetParent(transform, false);
            var c = Quad(); c.transform.SetParent(root, false); c.transform.localPosition = Vector3.zero; c.transform.localScale = new Vector3(0.5f, 2.6f, 1f); var cr = c.GetComponent<Renderer>(); cr.sharedMaterial = addSpark; mpb.SetColor(BaseColor, core); cr.SetPropertyBlock(mpb);
            var t = Quad(); t.transform.SetParent(root, false); t.transform.localPosition = new Vector3(0f, -2.4f, 0f); t.transform.localScale = new Vector3(1f, 6.5f, 1f); var tr = t.GetComponent<Renderer>(); tr.sharedMaterial = addGlow; mpb.SetColor(BaseColor, tail); tr.SetPropertyBlock(mpb);
            return root;
        }

        /// <summary>Gives a tracer's quads back to the pool.</summary>
        public void Release(Transform tracer)
        {
            for (int i = tracer.childCount - 1; i >= 0; i--) { var q = tracer.GetChild(i).gameObject; if (q.GetComponent<MeshRenderer>() == null) continue; q.transform.SetParent(transform, false); q.transform.localScale = Vector3.one; q.transform.localRotation = Quaternion.identity; q.SetActive(false); quadPool.Push(q); }
            Destroy(tracer.gameObject);
        }

        /// <summary>Signal smoke: a coloured puff rising slowly from a marker.</summary>
        public void Signal(Vector3 pos, Color color) { Spawn(smokeRagged, pos, 2.2f + Random.value, color, 2.6f, new Vector3(Random.Range(-0.3f, 0.3f), 1.6f, Random.Range(-0.3f, 0.3f)), 1.8f, true); }

        /// <summary>A ring on the ground with a light over it: the objective marker, the landed supply crate.</summary>
        public Transform Marker(Vector3 pos, Color color, float size)
        {
            var root = new GameObject("Marker").transform; root.SetParent(transform, false); root.position = pos;
            var q = Quad(); q.transform.SetParent(root, false); q.transform.localPosition = new Vector3(0f, 0.08f, 0f); q.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); q.transform.localScale = new Vector3(size, size, 1f);
            var r = q.GetComponent<Renderer>(); r.sharedMaterial = addRing; mpb.SetColor(BaseColor, color); r.SetPropertyBlock(mpb);
            var l = new GameObject("Light").AddComponent<Light>(); l.transform.SetParent(root, false); l.transform.localPosition = Vector3.up * 2f; l.type = LightType.Point; l.color = color; l.intensity = 8f; l.range = 14f; l.shadows = LightShadows.None;
            return root;
        }

        /// <summary>A star shell: a big white glow hanging under its parachute; moved by the caller every frame.</summary>
        public Transform StarFlare()
        {
            var root = new GameObject("StarShell").transform; root.SetParent(transform, false);
            var q = Quad(); q.transform.SetParent(root, false); q.transform.localScale = Vector3.one * 7f; var r = q.GetComponent<Renderer>(); r.sharedMaterial = addGlow; mpb.SetColor(BaseColor, new Color(1f, 0.97f, 0.9f, 1f)); r.SetPropertyBlock(mpb);
            return root;
        }

        /// <summary>A thin puff left behind a shell in flight.</summary>
        public void Trail(Vector3 pos) { Spawn(smokeSoft, pos, 0.9f, new Color(0.7f, 0.68f, 0.64f, 0.3f), 0.55f, Vector3.up * 0.3f, 1.6f, true); }

        public void Tick(float dt)
        {
            var camRot = cam.transform.rotation; var camFwd = cam.transform.forward;
            for (int i = puffs.Count - 1; i >= 0; i--)
            {
                var p = puffs[i]; p.age += dt; float q = p.age / p.life;
                if (q >= 1f) { p.t.gameObject.SetActive(false); quadPool.Push(p.t.gameObject); puffs.RemoveAt(i); continue; }
                if (p.gravity) p.vel += Vector3.down * (26f * dt);
                p.t.position += p.vel * dt;
                float s = p.size * (1f + q * p.grow);
                p.t.localScale = p.stretch > 1f ? new Vector3(s / Mathf.Sqrt(p.stretch), s * p.stretch, 1f) : Vector3.one * s;
                var c = p.color; c.a = p.color.a * Mathf.Pow(1f - q, p.alphaPow);
                mpb.SetColor(BaseColor, c); p.r.SetPropertyBlock(mpb);
                if (p.flat) p.t.rotation = Quaternion.Euler(90f, p.spin, 0f);
                else if (p.aligned) p.t.rotation = Quaternion.LookRotation(camFwd, p.axis);
                else p.t.rotation = camRot * Quaternion.Euler(0f, 0f, p.spin + (p.smoke ? q * 25f : 0f));
            }
            for (int i = lights.Count - 1; i >= 0; i--)
            {
                lightLife[i] -= dt;
                if (lightLife[i] <= 0f) { Destroy(lights[i].gameObject); lights.RemoveAt(i); lightLife.RemoveAt(i); lightMax.RemoveAt(i); continue; }
                lights[i].intensity *= Mathf.Clamp01(lightLife[i] / lightMax[i]) > 0.5f ? 1f : 0.85f;
            }
        }
    }
}

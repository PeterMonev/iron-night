using System.Collections.Generic;
using UnityEngine;

namespace IronNight
{
    /// <summary>
    /// Every light effect of the fight as camera-facing additive quads plus a few point lights: muzzle flashes, tracer
    /// shells, hits, burning wrecks with smoke and signal flares. Quads are pooled; nothing is
    /// allocated during a wave.
    /// </summary>
    public class Fx : MonoBehaviour
    {
        class Puff { public Transform t; public Renderer r; public Material m; public float life, age, size, rise; public Color color; public Vector3 vel; public bool smoke; }

        Material additiveTemplate, smokeTemplate; Texture2D glowTex, softTex;
        readonly List<Puff> puffs = new List<Puff>(); readonly Stack<GameObject> quadPool = new Stack<GameObject>();
        readonly List<Light> lights = new List<Light>(); readonly List<float> lightLife = new List<float>(); readonly List<float> lightMax = new List<float>();
        Camera cam;

        public void Build(Camera camera)
        {
            cam = camera;
            additiveTemplate = Resources.Load<Material>("Additive"); smokeTemplate = Resources.Load<Material>("Smoke");
            glowTex = Lightswarm.ProceduralSprites.Glow(128, 0.12f).texture;
            softTex = Lightswarm.ProceduralSprites.GroundShadow(128).texture;
        }

        GameObject Quad()
        {
            if (quadPool.Count > 0) { var q = quadPool.Pop(); q.SetActive(true); return q; }
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            var r = go.GetComponent<Renderer>(); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            return go;
        }

        Puff Spawn(Vector3 pos, float size, Color color, float life, bool smoke, Vector3 vel)
        {
            var go = Quad(); var r = go.GetComponent<Renderer>();
            var m = new Material(smoke ? smokeTemplate : additiveTemplate);
            m.SetTexture("_BaseMap", smoke ? softTex : glowTex); m.SetColor("_BaseColor", color);
            r.sharedMaterial = m;
            var p = new Puff { t = go.transform, r = r, m = m, life = life, age = 0f, size = size, color = color, smoke = smoke, vel = vel };
            p.t.position = pos; p.t.localScale = Vector3.one * size;
            puffs.Add(p); return p;
        }

        void Flash(Vector3 pos, Color color, float intensity, float range, float life)
        {
            var go = new GameObject("FxLight"); go.transform.SetParent(transform, false); go.transform.position = pos;
            var l = go.AddComponent<Light>(); l.type = LightType.Point; l.color = color; l.intensity = intensity; l.range = range; l.shadows = LightShadows.None;
            lights.Add(l); lightLife.Add(life); lightMax.Add(life);
        }

        public void MuzzleFlash(Vector3 pos, Vector3 dir)
        {
            Spawn(pos + dir * 0.6f, 3.2f, new Color(1f, 0.9f, 0.6f, 1f), 0.12f, false, Vector3.zero);
            Spawn(pos + dir * 1.6f, 1.6f, new Color(1f, 0.75f, 0.35f, 1f), 0.1f, false, Vector3.zero);
            Flash(pos + dir * 1f + Vector3.up * 0.5f, new Color(1f, 0.8f, 0.5f), 40f, 14f, 0.12f);
        }

        public void Hit(Vector3 pos, float power)
        {
            for (int i = 0; i < 3; i++) Spawn(pos + Random.insideUnitSphere * 0.8f, 2.5f * power + Random.value, new Color(1f, 0.6f + Random.value * 0.3f, 0.25f, 1f), 0.35f + Random.value * 0.2f, false, Vector3.up * 2f);
            for (int i = 0; i < 4; i++) Spawn(pos + Random.insideUnitSphere * 1.2f, 2f + Random.value * 2f, new Color(0.35f, 0.33f, 0.3f, 0.7f), 2.5f + Random.value, true, new Vector3(Random.Range(-0.6f, 0.6f), 1.5f + Random.value, Random.Range(-0.6f, 0.6f)));
            Flash(pos + Vector3.up * 1.5f, new Color(1f, 0.6f, 0.3f), 60f * power, 18f, 0.3f);
        }

        public void Explosion(Vector3 pos)
        {
            for (int i = 0; i < 6; i++) Spawn(pos + Random.insideUnitSphere * 1.5f + Vector3.up, 4f + Random.value * 3f, new Color(1f, 0.55f + Random.value * 0.3f, 0.2f, 1f), 0.5f + Random.value * 0.3f, false, Vector3.up * 3f);
            for (int i = 0; i < 8; i++) Spawn(pos + Random.insideUnitSphere * 2f, 4f + Random.value * 3f, new Color(0.3f, 0.28f, 0.26f, 0.8f), 4f + Random.value * 2f, true, new Vector3(Random.Range(-1f, 1f), 2f + Random.value * 1.5f, Random.Range(-1f, 1f)));
            Flash(pos + Vector3.up * 2f, new Color(1f, 0.55f, 0.25f), 220f, 30f, 0.6f);
        }

        /// <summary>Small flame and a smoke puff for a burning wreck; called every so often while it burns.</summary>
        public void Burn(Vector3 pos)
        {
            Spawn(pos + Vector3.up * 2f + Random.insideUnitSphere * 0.6f, 2.5f + Random.value * 1.5f, new Color(1f, 0.5f + Random.value * 0.3f, 0.15f, 0.9f), 0.35f, false, Vector3.up * 1.5f);
            if (Random.value < 0.5f) Spawn(pos + Vector3.up * 2.5f, 3f + Random.value * 2f, new Color(0.25f, 0.24f, 0.22f, 0.6f), 4f, true, new Vector3(Random.Range(-0.4f, 0.4f), 1.6f, Random.Range(-0.4f, 0.4f)));
        }

        /// <summary>A shell that missed everything and hit the dirt: a small brown puff.</summary>
        public void Dust(Vector3 pos)
        {
            for (int i = 0; i < 2; i++) Spawn(pos + Vector3.up * 0.6f + Random.insideUnitSphere * 0.5f, 1.8f + Random.value, new Color(0.42f, 0.36f, 0.28f, 0.6f), 1.2f + Random.value * 0.5f, true, new Vector3(Random.Range(-0.5f, 0.5f), 1.2f, Random.Range(-0.5f, 0.5f)));
        }

        /// <summary>The smoke screen: a big slow grey cloud that hangs for a while.</summary>
        public void SmokeCloud(Vector3 pos, float life)
        {
            Spawn(pos, 6f + Random.value * 4f, new Color(0.6f, 0.6f, 0.62f, 0.75f), life, true, new Vector3(Random.Range(-0.4f, 0.4f), 0.35f, Random.Range(-0.4f, 0.4f)));
        }

        /// <summary>A flak tracer climbing from a post: a warm streak that burns out high up.</summary>
        public void Flak(Vector3 from, Vector3 dir)
        {
            Spawn(from, 1.6f, new Color(1f, 0.7f, 0.35f, 0.9f), 1.3f, false, dir * 130f);
            Spawn(from + dir * 0.5f, 3f, new Color(1f, 0.8f, 0.5f, 0.8f), 0.08f, false, Vector3.zero);
        }

        /// <summary>An artillery shell falling onto the point: a glowing streak that arrives when the timer ends.</summary>
        public void Incoming(Vector3 at, float seconds)
        {
            float h = 70f; Spawn(at + Vector3.up * h, 2.2f, new Color(1f, 0.85f, 0.6f, 0.9f), seconds, false, Vector3.down * (h / seconds));
        }

        public void Tick(float dt)
        {
            for (int i = puffs.Count - 1; i >= 0; i--)
            {
                var p = puffs[i]; p.age += dt; float q = p.age / p.life;
                if (q >= 1f) { p.r.sharedMaterial = null; Destroy(p.m); p.t.gameObject.SetActive(false); quadPool.Push(p.t.gameObject); puffs.RemoveAt(i); continue; }
                p.t.position += p.vel * dt;
                float s = p.smoke ? p.size * (1f + q * 1.6f) : p.size * (1f + q * 0.8f);
                p.t.localScale = Vector3.one * s;
                var c = p.color; c.a = p.smoke ? p.color.a * (1f - q) * (1f - q) : p.color.a * (1f - q);
                p.m.SetColor("_BaseColor", c);
                p.t.rotation = cam.transform.rotation;
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

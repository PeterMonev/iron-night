using System.Collections.Generic;
using UnityEngine;

namespace IronNight
{
    /// <summary>
    /// A crewman in the hangar having a smoke: the ember at his lips glowing, brighter as he draws on it every seven to
    /// twelve seconds, a thin thread of smoke always rising from it, and after each draw a breath of smoke let out
    /// forward and up, swelling and thinning in the lamplight. Put on the figure (facing +Z, its feet at the origin).
    /// </summary>
    public class Cigarette : MonoBehaviour
    {
        static readonly Vector3 Lips = new Vector3(0f, 1.6f, 0.14f), Tip = new Vector3(0.03f, 1.58f, 0.24f);
        class Puff { public Transform t; public Material m; public Vector3 vel; public float age, life, size0, size1, alpha; }
        readonly List<Puff> puffs = new List<Puff>();
        Transform ember; Material emberMat; UnityEngine.Light glow; Material smokeTemplate; Texture2D smokeTex;
        float nextDraw, drawT = -1f, wisp; Camera cam;

        public static Cigarette Smoke(GameObject figure, float height)
        {
            var c = figure.AddComponent<Cigarette>(); c.Build(height / 1.8f); return c;
        }

        float scale = 1f;
        void Build(float s)
        {
            scale = s; cam = Camera.main;
            smokeTemplate = Resources.Load<Material>("Smoke"); smokeTex = Resources.Load<Texture2D>("Fx/smoke_puff_1");
            var add = Resources.Load<Material>("Additive");
            ember = GameObject.CreatePrimitive(PrimitiveType.Quad).transform; Destroy(ember.GetComponent<Collider>()); ember.name = "Ember"; ember.SetParent(transform, false); ember.localPosition = Tip * s;
            emberMat = new Material(add); emberMat.SetTexture("_BaseMap", Lightswarm.ProceduralSprites.Glow(32, 0.25f).texture); ember.GetComponent<Renderer>().sharedMaterial = emberMat;
            ember.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            glow = new GameObject("EmberLight").AddComponent<UnityEngine.Light>(); glow.transform.SetParent(ember, false); glow.type = LightType.Point; glow.color = new Color(1f, 0.45f, 0.15f); glow.range = 0.6f; glow.shadows = LightShadows.None;
            nextDraw = Random.Range(2f, 6f);
        }

        void Update()
        {
            float dt = Time.deltaTime; if (cam == null) cam = Camera.main;
            // the draw: the ember flares for a second, the breath of smoke comes a moment after
            nextDraw -= dt; if (nextDraw <= 0f) { nextDraw = Random.Range(7f, 12f); drawT = 0f; }
            float flare = 0f;
            if (drawT >= 0f)
            {
                drawT += dt; flare = Mathf.Clamp01(1f - Mathf.Abs(drawT - 0.6f) / 0.6f);
                if (drawT >= 1.9f && drawT - dt < 1.9f) for (int i = 0; i < 3; i++) Spawn(Lips * scale, (transform.forward * 0.35f + Vector3.up * 0.12f + Random.insideUnitSphere * 0.05f), 3f, 0.12f, 0.75f, 0.32f);
                if (drawT > 2.2f) drawT = -1f;
            }
            float flick = 0.85f + 0.15f * Mathf.PerlinNoise(Time.time * 6f, 0.3f);
            ember.localScale = Vector3.one * (0.035f + 0.03f * flare) * flick * scale;
            emberMat.SetColor("_BaseColor", new Color(1f, 0.5f + 0.25f * flare, 0.2f, 1f) * (1.2f + 1.8f * flare));
            glow.intensity = (0.25f + 0.6f * flare) * flick;
            if (cam != null) ember.rotation = cam.transform.rotation;
            // the thread of smoke off the tip
            wisp -= dt; if (wisp <= 0f) { wisp = 0.3f; Spawn(Tip * scale, Vector3.up * 0.16f + Random.insideUnitSphere * 0.02f, 2.6f, 0.04f, 0.28f, 0.16f); }
            for (int i = puffs.Count - 1; i >= 0; i--)
            {
                var p = puffs[i]; p.age += dt; float q = p.age / p.life;
                if (q >= 1f) { Destroy(p.t.gameObject); Destroy(p.m); puffs.RemoveAt(i); continue; }
                p.vel *= 1f - Mathf.Min(1f, 0.6f * dt); p.vel += Vector3.up * (0.05f * dt); p.t.position += p.vel * dt;
                p.t.localScale = Vector3.one * Mathf.Lerp(p.size0, p.size1, Mathf.Sqrt(q)) * scale;
                p.m.SetColor("_BaseColor", new Color(0.75f, 0.73f, 0.7f, p.alpha * Mathf.SmoothStep(0f, 1f, q / 0.15f) * (1f - q)));
                if (cam != null) p.t.rotation = cam.transform.rotation * Quaternion.Euler(0f, 0f, p.age * 20f);
            }
        }

        void Spawn(Vector3 local, Vector3 vel, float life, float size0, float size1, float alpha)
        {
            if (smokeTemplate == null || puffs.Count > 40) return;
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(q.GetComponent<Collider>()); q.name = "Smoke";
            var m = new Material(smokeTemplate); if (smokeTex != null) m.SetTexture("_BaseMap", smokeTex); var r = q.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            q.transform.position = transform.TransformPoint(local); q.transform.localScale = Vector3.one * size0 * scale;
            puffs.Add(new Puff { t = q.transform, m = m, vel = vel, life = life, size0 = size0, size1 = size1, alpha = alpha });
        }

        void OnDestroy() { foreach (var p in puffs) if (p.t != null) Destroy(p.t.gameObject); if (emberMat != null) Destroy(emberMat); }
    }
}

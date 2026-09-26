using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace IronNight
{
    /// <summary>
    /// A little stage far under the field where a crate is filmed: its own camera into a texture, a dark floor with a
    /// pool of warm light, a cool rim behind. It takes the crates' pictures for the buttons and the calendar, and plays
    /// a crate's opening: the crate drops in and settles, breathes while it waits, shakes when tapped, then throws its
    /// lid off in a burst of gold light and sparks.
    /// </summary>
    public class CrateStage : MonoBehaviour
    {
        static readonly Vector3 Home = new Vector3(0f, -900f, 0f);   // under the field, and under the hangar at -600: nothing else in shot
        static CrateStage instance;
        static readonly Dictionary<string, Sprite> pictures = new Dictionary<string, Sprite>();
        Camera cam; UniversalAdditionalCameraData camData; RenderTexture rt; GameObject floor;
        Transform crate, lid, beam; Light key, rim, inner; Material beamMat; ParticleSystem sparks;
        float t, shakeT = -1f, burstT = -1f, crateH; bool landed, fogWas; Vector3 lidVel, lidSpin;

        public RenderTexture Texture => rt;
        /// <summary>Seconds since the lid flew, or -1 while the crate is still shut.</summary>
        public float SinceBurst => burstT;
        public bool Waiting => crate != null && landed && shakeT < 0f;
        public float Age => t;

        public static CrateStage Get()
        {
            if (instance != null) return instance;
            instance = new GameObject("CrateStage").AddComponent<CrateStage>(); instance.Build(); return instance;
        }

        void Build()
        {
            transform.position = Home;
            rt = new RenderTexture(1024, 1024, 24) { antiAliasing = 4 };
            var cg = new GameObject("CrateCam"); cg.transform.SetParent(transform, false); cam = cg.AddComponent<Camera>();
            cam.targetTexture = rt; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.015f, 0.015f, 0.02f, 1f);
            cam.fieldOfView = 30f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 30f; cam.enabled = false;
            camData = cg.AddComponent<UniversalAdditionalCameraData>(); camData.renderPostProcessing = true; camData.volumeLayerMask = 1;   // the field's bloom and grading (Default layer), not the hangar's
            cam.transform.position = Home + new Vector3(2.5f, 2.1f, -3.9f); cam.transform.LookAt(Home + new Vector3(0f, 0.75f, 0f));   // room above the crate for the lid and the light
            floor = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(floor.GetComponent<Collider>()); floor.transform.SetParent(transform, false);
            floor.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); floor.transform.localScale = new Vector3(80f, 80f, 1f);   // wide enough that its far edge never shows
            var fm = new Material(Resources.Load<Material>("VehicleLit")); fm.SetColor("_BaseColor", new Color(0.035f, 0.033f, 0.032f)); floor.GetComponent<Renderer>().sharedMaterial = fm;
            key = MakeLight("Key", LightType.Spot, new Vector3(0.7f, 4.4f, -1.4f), new Color(1f, 0.9f, 0.76f), 34f, 12f); key.spotAngle = 46f; key.innerSpotAngle = 20f; key.shadows = LightShadows.Soft;
            key.transform.LookAt(Home + new Vector3(0f, 0.3f, 0f));
            rim = MakeLight("Rim", LightType.Point, new Vector3(-1.3f, 1.8f, 1.7f), new Color(0.55f, 0.7f, 1f), 7f, 6f);
            inner = MakeLight("Inner", LightType.Point, new Vector3(0f, 0.9f, 0f), new Color(1f, 0.74f, 0.32f), 0f, 7f);
            // the pillar of light out of the open crate: a tall soft glow that always faces the camera
            beamMat = new Material(Resources.Load<Material>("Additive")); beamMat.SetTexture("_BaseMap", Lightswarm.ProceduralSprites.Glow(64, 0.2f).texture); beamMat.SetColor("_BaseColor", new Color(1f, 0.78f, 0.4f, 0f));
            var bq = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(bq.GetComponent<Collider>()); bq.GetComponent<Renderer>().sharedMaterial = beamMat; beam = bq.transform; beam.SetParent(transform, false); beam.gameObject.SetActive(false);
            // the sparks: a burst of gold that rises and falls
            var sg = new GameObject("Sparks"); sg.transform.SetParent(transform, false); sg.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); sparks = sg.AddComponent<ParticleSystem>(); sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = sparks.main; main.playOnAwake = false; main.loop = false; main.useUnscaledTime = true; main.duration = 1f; main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.6f); main.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, 4.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f); main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.45f), new Color(1f, 0.6f, 0.2f)); main.gravityModifier = 0.55f; main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 200;
            var em = sparks.emission; em.rateOverTime = 0f; em.SetBursts(new[] { new ParticleSystem.Burst(0f, 90) });
            var sh = sparks.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 26f; sh.radius = 0.35f;
            var col = sparks.colorOverLifetime; col.enabled = true; var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) }); col.color = g;
            var sm = new Material(Resources.Load<Material>("Additive")); sm.SetTexture("_BaseMap", Lightswarm.ProceduralSprites.Glow(32, 0.35f).texture); sm.SetColor("_BaseColor", Color.white); sparks.GetComponent<ParticleSystemRenderer>().sharedMaterial = sm;
            RenderPipelineManager.beginCameraRendering += NoFogBegin; RenderPipelineManager.endCameraRendering += NoFogEnd;
        }
        void OnDestroy() { RenderPipelineManager.beginCameraRendering -= NoFogBegin; RenderPipelineManager.endCameraRendering -= NoFogEnd; if (rt != null) rt.Release(); }
        // the field's night fog would grey the crate: none on this stage
        void NoFogBegin(ScriptableRenderContext c, Camera camera) { if (camera != cam) return; fogWas = RenderSettings.fog; RenderSettings.fog = false; }
        void NoFogEnd(ScriptableRenderContext c, Camera camera) { if (camera != cam) return; RenderSettings.fog = fogWas; }

        Light MakeLight(string name, LightType type, Vector3 at, Color color, float intensity, float range)
        {
            var l = new GameObject(name).AddComponent<Light>(); l.transform.SetParent(transform, false); l.transform.position = Home + at;
            l.type = type; l.color = color; l.intensity = intensity; l.range = range; l.shadows = LightShadows.None; return l;
        }

        static Bounds BoundsOf(GameObject g) { var rs = g.GetComponentsInChildren<Renderer>(); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b; }

        /// <summary>A crate on the floor: the ammunition box for a supply crate, the airdrop crate in warm wood for an
        /// officer's; a plank lid over its top, the part that flies.</summary>
        void Place(string kind)
        {
            if (crate != null) { crate.gameObject.SetActive(false); Destroy(crate.gameObject); } if (lid != null) { lid.gameObject.SetActive(false); Destroy(lid.gameObject); }   // hidden at once: Destroy waits for the end of the frame
            bool officer = kind == "officer";
            var pf = Resources.Load<GameObject>(officer ? "Props/crate" : "Props/ammocrate") ?? Resources.Load<GameObject>("Props/crate");
            var holder = new GameObject("Crate").transform; holder.SetParent(transform, false); holder.position = Home;
            var model = Instantiate(pf, holder);
            var m = new Material(Resources.Load<Material>("VehicleLit")); m.SetTexture("_BaseMap", Resources.Load<Texture2D>(officer ? "Props/crate_tex" : "Props/ammocrate_tex")); m.SetFloat("_Cull", 0f);
            if (officer) m.SetColor("_BaseColor", new Color(1.08f, 0.9f, 0.68f));
            foreach (var r in model.GetComponentsInChildren<Renderer>()) { r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.On; }
            var b = BoundsOf(model); model.transform.localScale *= (officer ? 1.75f : 1.25f) / Mathf.Max(0.01f, Mathf.Max(b.size.x, b.size.z)); b = BoundsOf(model);   // a metre and a quarter across, the officer's long crate more
            model.transform.position += new Vector3(Home.x - b.center.x, Home.y - b.min.y, Home.z - b.center.z); b = BoundsOf(model); crateH = b.size.y;
            var lg = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(lg.GetComponent<Collider>()); lid = lg.transform; lid.SetParent(holder, false);
            lid.localScale = new Vector3(b.size.x * 1.03f, Mathf.Max(0.05f, b.size.y * 0.08f), b.size.z * 1.03f); lid.position = new Vector3(Home.x, b.max.y - lid.localScale.y * 0.25f, Home.z); lid.localRotation = Quaternion.identity;
            var lm = new Material(Resources.Load<Material>("VehicleLit")); lm.SetColor("_BaseColor", officer ? new Color(0.38f, 0.26f, 0.14f) : new Color(0.26f, 0.28f, 0.18f)); lg.GetComponent<Renderer>().sharedMaterial = lm; lg.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.On;
            crate = holder; crate.rotation = Quaternion.Euler(0f, 22f, 0f);
        }

        /// <summary>A crate's picture for the buttons and the calendar, on a clear background: rendered once, kept.</summary>
        public static Sprite Picture(string kind)
        {
            if (pictures.TryGetValue(kind, out var sp) && sp != null) return sp;
            var st = Get(); bool wasOn = st.cam.enabled; if (wasOn) return null;   // not while a crate is being opened: the picture waits for it
            st.Place(kind); st.floor.SetActive(false); st.inner.intensity = 0f; st.beam.gameObject.SetActive(false);
            var small = new RenderTexture(256, 256, 24) { antiAliasing = 4 };
            var camAt = st.cam.transform.position; st.cam.transform.position = Home + new Vector3(1.75f, 1.45f, -2.7f); st.cam.transform.LookAt(Home + new Vector3(0f, 0.5f, 0f));   // closer than for the opening: the crate fills its picture
            st.cam.targetTexture = small; st.cam.backgroundColor = new Color(0f, 0f, 0f, 0f); st.camData.renderPostProcessing = false;
            var req = new RenderPipeline.StandardRequest { destination = small };
            if (RenderPipeline.SupportsRenderRequest(st.cam, req)) RenderPipeline.SubmitRenderRequest(st.cam, req); else st.cam.Render();
            var prev = RenderTexture.active; RenderTexture.active = small; var tex = new Texture2D(256, 256, TextureFormat.RGBA32, false); tex.ReadPixels(new Rect(0, 0, 256, 256), 0, 0); tex.Apply(); RenderTexture.active = prev;
            st.cam.transform.position = camAt; st.cam.transform.LookAt(Home + new Vector3(0f, 0.75f, 0f));
            st.cam.targetTexture = st.rt; st.cam.backgroundColor = new Color(0.015f, 0.015f, 0.02f, 1f); st.camData.renderPostProcessing = true; st.floor.SetActive(true); small.Release();
            if (st.crate != null) { st.crate.gameObject.SetActive(false); Destroy(st.crate.gameObject); st.crate = null; st.lid = null; }
            sp = Sprite.Create(tex, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f), 100f); pictures[kind] = sp; return sp;
        }

        /// <summary>A crate drops in and waits to be opened.</summary>
        public void Play(string kind)
        {
            Place(kind); t = 0f; shakeT = -1f; burstT = -1f; landed = false; inner.intensity = 0f; beam.gameObject.SetActive(false); sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            cam.targetTexture = rt; cam.enabled = true; Pose(0f);
        }
        /// <summary>The tap: it shakes, then bursts.</summary>
        public void Tap() { if (Waiting) shakeT = 0f; }
        public void Stop() { cam.enabled = false; sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); if (crate != null) Destroy(crate.gameObject); if (lid != null) Destroy(lid.gameObject); crate = null; lid = null; }

        void Burst()
        {
            burstT = 0f; lid.SetParent(transform, true);
            lidVel = new Vector3(Random.Range(-0.7f, 0.7f), 4.4f, 1.3f); lidSpin = new Vector3(Random.Range(220f, 340f), Random.Range(-120f, 120f), Random.Range(-180f, 180f));
            beam.gameObject.SetActive(true); sparks.transform.position = Home + new Vector3(0f, crateH * 0.95f, 0f); sparks.Play(); Sfx.LevelUp();
        }

        void Update()
        {
            if (crate == null || !cam.enabled) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f); t += dt; Pose(dt);
        }

        void Pose(float dt)
        {
            // the drop: from above, faster and faster, a squash on landing, settled by 0.8 s
            float y = t < 0.42f ? Mathf.Lerp(2.8f, 0f, (t / 0.42f) * (t / 0.42f)) : 0f, squash = t >= 0.42f && t < 0.78f ? Mathf.Sin((t - 0.42f) / 0.36f * Mathf.PI) * 0.12f : 0f;
            if (t >= 0.42f && t - dt < 0.42f) Sfx.Click();
            landed = t >= 0.78f;
            // waiting: a slow breath, a small lean
            float bob = landed && burstT < 0f ? Mathf.Sin(t * 2.2f) * 0.015f : 0f, sway = Mathf.Sin(t * 0.9f) * 4f;
            Vector3 jitter = Vector3.zero; float hop = 0f;
            if (shakeT >= 0f && burstT < 0f)
            {
                shakeT += dt; float k = Mathf.Clamp01(shakeT / 0.75f);
                jitter = new Vector3(Mathf.Sin(shakeT * 61f), Mathf.Sin(shakeT * 47f + 1f), Mathf.Sin(shakeT * 53f + 2f)) * (2f + 9f * k);
                hop = Mathf.Abs(Mathf.Sin(shakeT * 18f)) * 0.05f * k; inner.intensity = 14f * k;   // light at the seams, stronger and stronger
                if (shakeT >= 0.75f) Burst();
            }
            crate.position = Home + new Vector3(0f, y + bob + hop, 0f);
            crate.localScale = new Vector3(1f + squash * 0.5f, 1f - squash, 1f + squash * 0.5f);
            crate.rotation = Quaternion.Euler(jitter.x, 22f + sway + jitter.y, jitter.z);
            if (burstT >= 0f)
            {
                burstT += dt;
                if (lid != null) { lidVel += Vector3.down * 9.8f * dt; lid.position += lidVel * dt; lid.Rotate(lidSpin * dt, Space.World); if (lid.position.y < Home.y - 3f) lid.gameObject.SetActive(false); }
                inner.intensity = burstT < 0.12f ? Mathf.Lerp(14f, 60f, burstT / 0.12f) : Mathf.Lerp(60f, 12f, Mathf.Clamp01((burstT - 0.12f) / 1.4f));
                float a = burstT < 0.15f ? burstT / 0.15f * 0.95f : Mathf.Lerp(0.95f, 0.35f, Mathf.Clamp01((burstT - 0.15f) / 1.6f));
                beamMat.SetColor("_BaseColor", new Color(1f, 0.78f, 0.4f, a));
                beam.position = Home + new Vector3(0f, crateH + 1.5f, 0f); beam.localScale = new Vector3(1.05f + 0.1f * Mathf.Sin(burstT * 5f), 3.6f, 1f);
                beam.rotation = Quaternion.LookRotation(beam.position - cam.transform.position);
            }
            rim.intensity = 7f + Mathf.Sin(t * 1.6f) * 1.2f;
        }
    }
}

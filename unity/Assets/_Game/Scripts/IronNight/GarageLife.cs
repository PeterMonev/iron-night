using UnityEngine;

namespace IronNight
{
    /// <summary>
    /// The hangar keeps the platoon's story. On the back wall hangs the hunt board: every enemy ace the platoon has met
    /// pinned up in his photograph, a red cross over each one who is dead, a dark print with a question mark for each
    /// still unknown, under a lamp of its own. And a tank back from a hard night stands still on its turntable for a few
    /// minutes with the mechanics at it: an arc hidden behind the hull, sparks over it and across the concrete, its blue
    /// light flickering on the walls.
    /// </summary>
    public partial class Garage
    {
        // ---- the hunt board ----
        static readonly Vector3 BoardAt = new Vector3(9f, 2.72f, 19.93f);   // on the brick left of the door: between the name and the tank in the title's picture, over the tank in the depot's
        const float BoardW = 4f, BoardH = 2.5f;
        Transform board, prints; Material photos, cross;

        void BuildBoard()
        {
            board = new GameObject("HuntBoard").transform; board.SetParent(transform, false); board.localPosition = BoardAt;
            var ply = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(ply.GetComponent<Collider>()); ply.transform.SetParent(board, false); ply.transform.localScale = new Vector3(BoardW, BoardH, 1f);
            var pm = new Material(Resources.Load<Material>("VehicleLit")); pm.SetTexture("_BaseMap", Resources.Load<Texture2D>("Textures/hunt_board")); pm.SetFloat("_Smoothness", 0.12f); ply.GetComponent<Renderer>().sharedMaterial = pm;
            var wood = new Material(Resources.Load<Material>("VehicleLit")); wood.SetColor("_BaseColor", new Color(0.09f, 0.07f, 0.05f)); wood.SetFloat("_Smoothness", 0.3f);
            Block(board, new Vector3(0f, BoardH / 2f + 0.05f, -0.02f), new Vector3(BoardW + 0.2f, 0.1f, 0.08f), wood); Block(board, new Vector3(0f, -BoardH / 2f - 0.05f, -0.02f), new Vector3(BoardW + 0.2f, 0.1f, 0.08f), wood);
            Block(board, new Vector3(-BoardW / 2f - 0.05f, 0f, -0.02f), new Vector3(0.1f, BoardH, 0.08f), wood); Block(board, new Vector3(BoardW / 2f + 0.05f, 0f, -0.02f), new Vector3(0.1f, BoardH, 0.08f), wood);
            // its lamp: a green enamel bar on two arms over the board, glowing underneath, its warm light down across the prints
            var enamel = new Material(Resources.Load<Material>("BarrelLit")); enamel.SetColor("_BaseColor", new Color(0.09f, 0.17f, 0.11f)); enamel.SetFloat("_Metallic", 0.2f); enamel.SetFloat("_Smoothness", 0.6f);
            var bar = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(bar.GetComponent<Collider>()); bar.transform.SetParent(board, false); bar.transform.localPosition = new Vector3(0f, BoardH / 2f + 0.32f, -0.36f); bar.transform.localRotation = Quaternion.Euler(0f, 0f, 90f); bar.transform.localScale = new Vector3(0.16f, 0.9f, 0.16f); bar.GetComponent<Renderer>().sharedMaterial = enamel;
            foreach (float ax in new[] { -0.7f, 0.7f }) Block(board, new Vector3(ax, BoardH / 2f + 0.32f, -0.18f), new Vector3(0.04f, 0.04f, 0.36f), enamel);
            var glowMat = new Material(Resources.Load<Material>("Additive")); glowMat.SetTexture("_BaseMap", Lightswarm.ProceduralSprites.Glow(64, 0.3f).texture); glowMat.SetColor("_BaseColor", new Color(1f, 0.8f, 0.5f, 1f));
            var glow = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(glow.GetComponent<Collider>()); glow.transform.SetParent(board, false); glow.transform.localPosition = new Vector3(0f, BoardH / 2f + 0.22f, -0.37f); glow.transform.localScale = new Vector3(2.1f, 0.34f, 1f); glow.GetComponent<Renderer>().sharedMaterial = glowMat;
            var spot = new GameObject("BoardLamp").AddComponent<Light>(); spot.transform.SetParent(board, false); spot.transform.localPosition = new Vector3(0f, BoardH / 2f + 0.25f, -0.6f); spot.transform.localRotation = Quaternion.LookRotation(new Vector3(0f, -1.6f, 0.62f));
            spot.type = LightType.Spot; spot.spotAngle = 108f; spot.range = 5.5f; spot.intensity = 10f; spot.color = new Color(1f, 0.84f, 0.62f); spot.shadows = LightShadows.None;
            photos = new Material(Resources.Load<Material>("VehicleLit")); photos.SetTexture("_BaseMap", Resources.Load<Texture2D>("Textures/hunt_photos")); photos.SetFloat("_Smoothness", 0.35f);   // a little gloss: photographic paper
            cross = new Material(Resources.Load<Material>("FoliageCut")); cross.SetTexture("_BaseMap", Resources.Load<Texture2D>("Textures/hunt_x")); cross.SetFloat("_Smoothness", 0.25f);
            RefreshBoard();
        }

        /// <summary>The prints as the roster stands: one place for each name, the same place always, pinned a little crooked.</summary>
        public void RefreshBoard()
        {
            if (board == null) return;
            if (prints != null) Destroy(prints.gameObject);
            prints = new GameObject("Prints").transform; prints.SetParent(board, false);
            var board8 = Nemesis.Board();
            for (int i = 0; i < board8.Length && i < 8; i++)
            {
                float x = -1.41f + (i % 4) * 0.94f + ((i * 7) % 5 - 2) * 0.012f, y = (i < 4 ? 0.23f : -0.72f) + ((i * 3) % 5 - 2) * 0.01f, tilt = ((i * 37) % 11 - 5) * 0.9f;
                var print = new GameObject("Print " + Nemesis.Surnames[i], typeof(MeshFilter), typeof(MeshRenderer)); print.transform.SetParent(prints, false);
                print.transform.localPosition = new Vector3(x, y, -0.012f); print.transform.localRotation = Quaternion.Euler(0f, 0f, tilt); print.transform.localScale = new Vector3(0.62f, 0.778f, 1f);
                print.GetComponent<MeshFilter>().sharedMesh = PrintQuad(board8[i] == 0 ? 8 : i); print.GetComponent<MeshRenderer>().sharedMaterial = photos;
                if (board8[i] != 2) continue;
                var x2 = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(x2.GetComponent<Collider>()); x2.name = "Cross"; x2.transform.SetParent(print.transform, false);
                x2.transform.localPosition = new Vector3(0f, 0.05f, -0.01f); x2.transform.localRotation = Quaternion.Euler(0f, 0f, tilt * -1.3f + 4f); x2.transform.localScale = new Vector3(1.05f, 0.84f, 1f); x2.GetComponent<Renderer>().sharedMaterial = cross;
            }
        }

        static void Block(Transform parent, Vector3 pos, Vector3 size, Material m)
        {
            var c = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(c.GetComponent<Collider>()); c.transform.SetParent(parent, false); c.transform.localPosition = pos; c.transform.localScale = size; c.GetComponent<Renderer>().sharedMaterial = m;
        }

        static readonly Mesh[] printQuads = new Mesh[10];
        /// <summary>A quad showing one cell of the prints sheet (5 x 2 cells of 204 x 256 on 1024 x 512, the first row at
        /// the top); made once and shared by every board.</summary>
        static Mesh PrintQuad(int cell) => printQuads[cell] != null ? printQuads[cell] : printQuads[cell] = MakePrintQuad(cell);
        static Mesh MakePrintQuad(int cell)
        {
            float u0 = (cell % 5) * 204f / 1024f, u1 = u0 + 204f / 1024f, v1 = 1f - (cell / 5) * 0.5f, v0 = v1 - 0.5f;
            var m = new Mesh
            {
                vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f) },
                uv = new[] { new Vector2(u0, v0), new Vector2(u1, v0), new Vector2(u0, v1), new Vector2(u1, v1) },
                triangles = new[] { 0, 2, 1, 2, 3, 1 },
            };
            m.RecalculateNormals(); m.RecalculateTangents(); m.RecalculateBounds(); return m;
        }

        // ---- the repair after a hard night ----
        const float RepairMinutes = 4f, RepairSpin = 250f;   // RepairSpin: the turntable stands with the gun toward the left of the picture, the far side to the wall
        /// <summary>A tank back from a hard night: the mechanics are at it for a few minutes of the real clock.</summary>
        public static void Damaged(string tank) { if (string.IsNullOrEmpty(tank)) return; PlayerPrefs.SetString("garage.repair", tank + "|" + GameClock.UtcNow.AddMinutes(RepairMinutes).Ticks); PlayerPrefs.Save(); }
        static bool InRepair(string tank)
        {
            var s = PlayerPrefs.GetString("garage.repair", ""); int bar = s.IndexOf('|');
            return bar > 0 && s.Substring(0, bar) == tank && long.TryParse(s.Substring(bar + 1), out long until) && GameClock.UtcNow.Ticks < until;
        }
        Welding welding; float repairCheck;

        /// <summary>Once a second: is the tank on the turntable under repair? The welder starts, or packs up.</summary>
        void TickRepair(float dt)
        {
            repairCheck -= dt; if (repairCheck > 0f) return; repairCheck = 1f;
            bool now = titleOn && shown != null && InRepair(shownId);
            if (now == (welding != null)) return;
            if (welding != null) { Destroy(welding.gameObject); welding = null; }
            if (!now) return;
            if (!framedDepot) { spin = RepairSpin; spinVel = 0f; stage.localRotation = Quaternion.Euler(0f, spin, 0f); }   // stopped where it shows best (the title opens behind the curtain)
            welding = Welding.Behind(shown, titleCam, floor);
        }
    }

    /// <summary>A welder at work behind a hull: the arc's blue-white light flickering in bursts with pauses between, sparks
    /// thrown up over the armour to bounce on the floor, a glow over the hull's edge, and the crackle of it.</summary>
    public class Welding : MonoBehaviour
    {
        ParticleSystem sparks, smoke; Light arc; Transform glow; Camera eye; float burst, pause = 0.8f;

        /// <summary>Sets the welder to work on the tank's side away from the camera, at the edge of the rear deck.</summary>
        public static Welding Behind(Vehicle tank, Camera eye, Transform floor)
        {
            var t = tank.transform; var hull = t.Find("Hull"); var lo = Vector3.one * 1e9f; var hi = -lo;
            foreach (var mf in (hull != null ? hull : t).GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null) continue; var b = mf.sharedMesh.bounds;
                for (int c = 0; c < 8; c++)
                {
                    var p = t.InverseTransformPoint(mf.transform.TransformPoint(b.center + Vector3.Scale(b.extents, new Vector3((c & 1) == 0 ? -1f : 1f, (c & 2) == 0 ? -1f : 1f, (c & 4) == 0 ? -1f : 1f))));
                    lo = Vector3.Min(lo, p); hi = Vector3.Max(hi, p);
                }
            }
            if (hi.x < lo.x) { lo = new Vector3(-1.5f, 0f, -3f); hi = new Vector3(1.5f, 2.4f, 3f); }
            float side = eye != null && t.InverseTransformPoint(eye.transform.position).x > 0f ? -1f : 1f;   // the far side: the man and his arc hidden, their light not
            float fz = 0.1f, fy = 0.97f; var inv = System.Globalization.CultureInfo.InvariantCulture;
            foreach (var a in System.Environment.GetCommandLineArgs())
            {
                if (a.StartsWith("--weldz=")) float.TryParse(a.Substring(8), System.Globalization.NumberStyles.Float, inv, out fz);
                if (a.StartsWith("--weldy=")) float.TryParse(a.Substring(8), System.Globalization.NumberStyles.Float, inv, out fy);
            }
            var at = new Vector3(side > 0f ? hi.x - 0.25f : lo.x + 0.25f, Mathf.Lerp(lo.y, hi.y, fy), Mathf.Lerp(lo.z, hi.z, fz));
            Debug.Log("Iron Night: welder on " + tank.spec.id + " at " + at.ToString("0.00") + ", hull " + lo.ToString("0.00") + " .. " + hi.ToString("0.00") + ", side " + side + ", world " + t.TransformPoint(at).ToString("0.0") + ", tank forward " + t.forward.ToString("0.00"));
            var go = new GameObject("Welding"); go.transform.SetParent(t, false); go.transform.localPosition = at;
            go.transform.localRotation = Quaternion.LookRotation(new Vector3(side * 0.55f, 1.6f, -0.35f));   // up, a little outward and back: a fountain over the rear deck
            var w = go.AddComponent<Welding>(); w.eye = eye; w.Build(floor); return w;
        }

        void Build(Transform floor)
        {
            sparks = gameObject.AddComponent<ParticleSystem>(); sparks.Stop();
            var main = sparks.main; main.loop = true; main.useUnscaledTime = true; main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 600;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.5f); main.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, 5.8f); main.startSize = new ParticleSystem.MinMaxCurve(0.028f, 0.05f); main.gravityModifier = 1.1f;
            var shrink = sparks.sizeOverLifetime; shrink.enabled = true; shrink.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.7f, 0.8f), new Keyframe(1f, 0f)));   // cooling: they go out rather than vanish
            var em = sparks.emission; em.rateOverTime = 0f;
            var sh = sparks.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 30f; sh.radius = 0.03f;
            if (floor != null) { var cl = sparks.collision; cl.enabled = true; cl.type = ParticleSystemCollisionType.Planes; cl.AddPlane(floor); cl.bounce = 0.35f; cl.dampen = 0.45f; cl.lifetimeLoss = 0.12f; cl.radiusScale = 0.5f; }   // they skitter across the concrete
            var r = GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.07f; r.lengthScale = 2f; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            var m = new Material(Resources.Load<Material>("Additive")); m.SetTexture("_BaseMap", Lightswarm.ProceduralSprites.Glow(32, 0.45f).texture); m.SetColor("_BaseColor", new Color(4.2f, 1.45f, 0.32f, 1f)); r.sharedMaterial = m;   // past the bloom's threshold in red and green only: orange-hot, not white
            sparks.Play();
            arc = new GameObject("Arc").AddComponent<Light>(); arc.transform.SetParent(transform, false); arc.transform.position = transform.position + Vector3.up * 1.1f;   // over the rear deck: its light on the turret's back, the deck and the floor
            arc.type = LightType.Point; arc.range = 16f; arc.intensity = 0f; arc.color = new Color(0.66f, 0.8f, 1f); arc.shadows = LightShadows.None;
            var gm = new Material(Resources.Load<Material>("Additive")); gm.SetTexture("_BaseMap", Lightswarm.ProceduralSprites.Glow(64, 0.18f).texture); gm.SetColor("_BaseColor", new Color(1.1f, 1.35f, 1.8f, 1f));
            // a thin smoke off the work, drifting up
            var sg = new GameObject("Fume"); sg.transform.SetParent(transform, false); sg.transform.rotation = Quaternion.LookRotation(Vector3.up); smoke = sg.AddComponent<ParticleSystem>(); smoke.Stop();
            var sm = smoke.main; sm.loop = true; sm.useUnscaledTime = true; sm.simulationSpace = ParticleSystemSimulationSpace.World; sm.maxParticles = 60; sm.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4f); sm.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.6f); sm.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.7f); sm.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            var sem = smoke.emission; sem.rateOverTime = 0f; var ssh = smoke.shape; ssh.shapeType = ParticleSystemShapeType.Cone; ssh.angle = 12f; ssh.radius = 0.08f;
            var grow = smoke.sizeOverLifetime; grow.enabled = true; grow.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 2.2f));
            var sr = sg.GetComponent<ParticleSystemRenderer>(); sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; sr.receiveShadows = false;
            var smat = new Material(Resources.Load<Material>("Smoke")); smat.SetTexture("_BaseMap", Lightswarm.ProceduralSprites.Glow(64, 0.25f).texture); smat.SetColor("_BaseColor", new Color(0.5f, 0.52f, 0.56f, 0.07f)); sr.sharedMaterial = smat;
            smoke.Play();
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(q.GetComponent<Collider>()); q.name = "ArcGlow"; q.transform.SetParent(transform, false); q.GetComponent<Renderer>().sharedMaterial = gm; glow = q.transform; glow.localScale = Vector3.zero;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (burst > 0f) { burst -= dt; if (burst <= 0f) pause = Random.Range(0.6f, 2.4f); }
            else { pause -= dt; if (pause <= 0f) burst = Random.Range(0.9f, 3.8f); }
            bool on = burst > 0f; var fume = smoke.emission; fume.rateOverTime = on ? 9f : 1.5f; var em = sparks.emission; em.rateOverTime = on ? Random.Range(150f, 380f) : 0f;
            arc.intensity = on ? Random.Range(12f, 40f) * (Random.value < 0.12f ? 0.25f : 1f) : Mathf.MoveTowards(arc.intensity, 0f, dt * 120f);   // an arc stutters
            float s = on ? Random.Range(1.6f, 2.6f) : 0f; glow.localScale = new Vector3(s, s, 1f);
            if (eye != null) glow.rotation = Quaternion.LookRotation(glow.position - eye.transform.position);
            Sfx.Weld(on ? 1f : 0f);
        }

        void OnDisable() { Sfx.Weld(0f); }
    }
}

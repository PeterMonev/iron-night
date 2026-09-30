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
                HangTrophy(i, x, y, tilt);
            }
        }

        static readonly System.Collections.Generic.Dictionary<string, Material> trophyMats = new System.Collections.Generic.Dictionary<string, Material>(); static Material trophyShade;

        /// <summary>A dead ace's trophy (Resources/UI/trophy_<kind>) hung by a corner over his print, a thin shadow of it on
        /// the board: his binoculars, his cap, his turret's number. Nothing while its picture is not there.</summary>
        void HangTrophy(int i, float x, float y, float tilt)
        {
            var ace = Nemesis.Dead.Find(a => a.name == Nemesis.Surnames[i]); if (ace == null || string.IsNullOrEmpty(ace.trophy)) return;
            string key = "trophy_" + ace.trophy.Replace(' ', '_');
            if (!trophyMats.TryGetValue(key, out var mat))
            {
                var tex = Resources.Load<Texture2D>("UI/" + key); mat = null;
                if (tex != null) { mat = new Material(Resources.Load<Material>("FoliageCut")); mat.SetTexture("_BaseMap", tex); mat.SetFloat("_Smoothness", 0.3f); }
                trophyMats[key] = mat;
            }
            if (mat == null) return;
            var pic = mat.GetTexture("_BaseMap"); float w = 0.52f, h = w * pic.height / pic.width, turn = -tilt * 0.6f - 7f;
            if (trophyShade == null) { trophyShade = new Material(Resources.Load<Material>("FoliageCut")); trophyShade.SetColor("_BaseColor", Color.black); }
            var shade = Pinned(prints, new Vector3(x + 0.23f, y - 0.27f, -0.02f), turn, w, h, trophyShade);
            var mpb = new MaterialPropertyBlock(); mpb.SetTexture("_BaseMap", pic); shade.GetComponent<Renderer>().SetPropertyBlock(mpb);
            Pinned(prints, new Vector3(x + 0.2f, y - 0.24f, -0.035f), turn, w, h, mat).name = "Trophy " + ace.trophy;
        }

        static Transform Pinned(Transform parent, Vector3 at, float turn, float w, float h, Material m)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(q.GetComponent<Collider>()); q.transform.SetParent(parent, false);
            q.transform.localPosition = at; q.transform.localRotation = Quaternion.Euler(0f, 0f, turn); q.transform.localScale = new Vector3(w, h, 1f); q.GetComponent<Renderer>().sharedMaterial = m;
            return q.transform;
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

        /// <summary>The turntable's plate: a flat disc height thick, its picture laid on from above (the plate's circle
        /// filling uvFill of the picture), and its rim, the second material.</summary>
        static Mesh Disc(float radius, float height, int segments, float uvFill)
        {
            var v = new System.Collections.Generic.List<Vector3>(); var uv = new System.Collections.Generic.List<Vector2>();
            var top = new System.Collections.Generic.List<int>(); var side = new System.Collections.Generic.List<int>();
            v.Add(new Vector3(0f, height, 0f)); uv.Add(new Vector2(0.5f, 0.5f));
            for (int i = 0; i <= segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments, x = Mathf.Cos(a), z = Mathf.Sin(a);
                v.Add(new Vector3(x * radius, height, z * radius)); uv.Add(new Vector2(0.5f + x * 0.5f * uvFill, 0.5f + z * 0.5f * uvFill));
            }
            for (int i = 1; i <= segments; i++) { top.Add(0); top.Add(i + 1); top.Add(i); }   // clockwise seen from above
            int s0 = v.Count;
            for (int i = 0; i <= segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments, x = Mathf.Cos(a), z = Mathf.Sin(a);
                v.Add(new Vector3(x * radius, height, z * radius)); uv.Add(new Vector2(i * 8f / segments, 1f));
                v.Add(new Vector3(x * radius, 0f, z * radius)); uv.Add(new Vector2(i * 8f / segments, 0f));
            }
            for (int i = 0; i < segments; i++) { int a = s0 + i * 2, b = a + 2; side.Add(a); side.Add(b); side.Add(a + 1); side.Add(b); side.Add(b + 1); side.Add(a + 1); }
            var m = new Mesh(); m.SetVertices(v); m.SetUVs(0, uv); m.subMeshCount = 2; m.SetTriangles(top, 0); m.SetTriangles(side, 1);
            m.RecalculateNormals(); m.RecalculateTangents(); m.RecalculateBounds(); return m;
        }

        // ---- the workshop: what stands about in a tank depot (Props from Petar's pictures, through TRELLIS) ----
        Material chainMat, bulbMat;

        void DressWorkshop()
        {
            var t = transform;
            // at the right, the Sherman's radial engine out on its stand under a work lamp, spare road wheels and track by it
            Prop(t, "engine_radial", new Vector3(7.2f, 0f, 4.2f), -115f);
            Prop(t, "wheels", new Vector3(9.9f, 0f, 5.9f), -49f);   // the stack, the leaning wheel and the track side by side to the camera
            WorkLamp(new Vector3(7.2f, 3.1f, 4.2f));
            // at the left under the hunt board, the workbench with a lamp of its own, and a diesel on its stand
            Prop(t, "workbench", new Vector3(2.6f, 0f, 12.8f), -65f);
            Prop(t, "engine_inline", new Vector3(5.6f, 0f, 15f), 147f);
            WorkLamp(new Vector3(2.6f, 2.9f, 12.8f));
            // the grime of years at the foot of the back wall
            var grime = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(grime.GetComponent<Collider>()); grime.name = "Grime"; grime.transform.SetParent(t, false);
            grime.transform.localPosition = new Vector3(0f, 0.8f, 19.96f); grime.transform.localScale = new Vector3(46f, 1.6f, 1f);
            var gm = new Material(Resources.Load<Material>("Smoke")); gm.SetTexture("_BaseMap", Lightswarm.ProceduralSprites.GradientDown(64, 1.3f).texture); gm.SetColor("_BaseColor", new Color(0.03f, 0.025f, 0.02f, 0.55f)); grime.GetComponent<Renderer>().sharedMaterial = gm;
        }

        /// <summary>A work lamp hung low on a chain from the roof: the green enamel shade (Props/lamp, a metre tall with its
        /// chain, the bulb at its foot), a warm pool of light under it, the bulb aglow.</summary>
        void WorkLamp(Vector3 at)
        {
            if (Prop(transform, "lamp", at, 0f) == null) return;
            if (chainMat == null)
            {
                chainMat = new Material(Resources.Load<Material>("BarrelLit")); chainMat.SetColor("_BaseColor", new Color(0.16f, 0.15f, 0.14f)); chainMat.SetFloat("_Metallic", 0.6f); chainMat.SetFloat("_Smoothness", 0.4f);
                bulbMat = new Material(Resources.Load<Material>("Additive")); bulbMat.SetTexture("_BaseMap", Lightswarm.ProceduralSprites.Glow(64, 0.22f).texture); bulbMat.SetColor("_BaseColor", new Color(2.2f, 1.55f, 0.85f, 1f));   // past the bloom's threshold
            }
            float top = at.y + 0.98f, roof = 11.1f;
            var chain = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(chain.GetComponent<Collider>()); chain.name = "Chain"; chain.transform.SetParent(transform, false);
            chain.transform.localPosition = new Vector3(at.x, (top + roof) * 0.5f, at.z); chain.transform.localScale = new Vector3(0.03f, (roof - top) * 0.5f, 0.03f); chain.GetComponent<Renderer>().sharedMaterial = chainMat;
            var light = new GameObject("WorkLight").AddComponent<Light>(); light.transform.SetParent(transform, false); light.transform.localPosition = at + new Vector3(0f, 0.12f, 0f); light.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            light.type = LightType.Spot; light.spotAngle = 125f; light.range = 7.5f; light.intensity = 14f; light.color = new Color(1f, 0.8f, 0.55f); light.shadows = LightShadows.None;
            var bulb = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(bulb.GetComponent<Collider>()); bulb.name = "Bulb"; bulb.transform.SetParent(transform, false);
            bulb.transform.localPosition = at + new Vector3(0f, 0.08f, 0f); bulb.transform.localScale = Vector3.one * 0.75f; bulb.GetComponent<Renderer>().sharedMaterial = bulbMat;
            bulb.transform.rotation = Quaternion.LookRotation(bulb.transform.position - (transform.position + new Vector3(-11f, 3.8f, -18f)));   // turned to the menu's camera, between its two framings
        }

        // ---- the repair after a hard night ----
        const float RepairMinutes = 4f, RepairSpin = 270f;   // RepairSpin: the turntable stands with the gun toward the camera's left, the rear corner the mechanic works at clear of the crew
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
            spin = RepairSpin; spinVel = 0f; stage.localRotation = Quaternion.Euler(0f, spin, 0f);   // stopped where it shows best (a repair begins as the title opens, behind the curtain)
            welding = Welding.AtWork(shown, titleCam, floor);
        }
    }

    /// <summary>A tank under repair: a mechanic kneeling at its rear with his torch on the armour and his gas cart beside
    /// him (without the figure, a welder unseen behind the hull), the arc's blue-white light flickering in bursts with
    /// pauses between, sparks thrown off the plate to bounce on the floor, a glow at the torch, a thin smoke, the crackle.</summary>
    public class Welding : MonoBehaviour
    {
        static readonly Vector3 TorchTip = new Vector3(-0.43f, 0.7f, 0.51f);   // the nozzle of the mechanic's torch in the figure's own frame (Props/mechanic, measured)
        ParticleSystem sparks, smoke; Light arc; Transform glow; Camera eye; float burst, pause = 0.8f, glowSize = 2.1f, arcMax = 40f; bool visible;   // visible: the torch in sight, its sparks fewer and closer

        /// <summary>Sets the repair going: the mechanic kneeling on the rear deck behind the turret, facing it, his torch on
        /// its back; his gas cart on the floor behind the tank's left rear corner (the side the menu's camera sees with the
        /// turntable stood at the repair angle).</summary>
        public static Welding AtWork(Vehicle tank, Camera eye, Transform floor)
        {
            var t = tank.transform; PartBounds(t.Find("Hull"), t, out var lo, out var hi);
            var root = new GameObject("Repair"); root.transform.SetParent(t, false);
            var w = root.AddComponent<Welding>(); w.eye = eye;
            var torch = new GameObject("Torch").transform;
            var tur = t.Find("Turret"); PartBounds(tur, t, out var tlo, out var thi);
            var man = tur != null && thi.z > tlo.z ? Garage.Prop(root.transform, "mechanic", new Vector3((tlo.x + thi.x) * 0.5f - TorchTip.x, tank.spec.ringHeight - 0.03f, tlo.z - TorchTip.z - 0.03f), 0f) : null;
            if (man != null)
            {
                Garage.Prop(root.transform, "weldcart", new Vector3(lo.x - 0.75f, 0f, lo.z - 1.4f), 135f);   // its coiled hoses to the camera
                torch.SetParent(man.transform, false); torch.localPosition = TorchTip;
                torch.rotation = Quaternion.LookRotation(t.TransformDirection(new Vector3(-0.55f, 0.25f, -1f)));   // off the turret's back, over the deck, to fall down the armour
                w.glowSize = 0.32f; w.arcMax = 6f; w.visible = true;
            }
            else
            {
                float side = eye != null && t.InverseTransformPoint(eye.transform.position).x > 0f ? -1f : 1f;   // the far side: the man and his arc hidden, their light not
                torch.SetParent(root.transform, false); torch.localPosition = new Vector3(side > 0f ? hi.x - 0.25f : lo.x + 0.25f, Mathf.Lerp(lo.y, hi.y, 0.97f), Mathf.Lerp(lo.z, hi.z, 0.1f));
                torch.localRotation = Quaternion.LookRotation(new Vector3(side * 0.55f, 1.6f, -0.35f));   // a fountain over the rear deck
            }
            Debug.Log("Iron Night: repair on " + tank.spec.id + (man != null ? ", the mechanic at his torch " : ", a welder unseen ") + torch.position.ToString("0.0"));
            w.Build(torch, floor);
            if (man != null) w.arc.transform.position = torch.position + t.forward * 0.3f + Vector3.up * 0.15f;   // before the torch, toward the turret
            return w;
        }

        /// <summary>A part's box (the hull, the turret) in the tank's own frame, from its meshes' bounds.</summary>
        static void PartBounds(Transform part, Transform t, out Vector3 lo, out Vector3 hi)
        {
            lo = Vector3.one * 1e9f; hi = -lo;
            foreach (var mf in (part != null ? part : t).GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null) continue; var b = mf.sharedMesh.bounds;
                for (int c = 0; c < 8; c++)
                {
                    var p = t.InverseTransformPoint(mf.transform.TransformPoint(b.center + Vector3.Scale(b.extents, new Vector3((c & 1) == 0 ? -1f : 1f, (c & 2) == 0 ? -1f : 1f, (c & 4) == 0 ? -1f : 1f))));
                    lo = Vector3.Min(lo, p); hi = Vector3.Max(hi, p);
                }
            }
            if (hi.x < lo.x && part != null && part.name == "Hull") { lo = new Vector3(-1.5f, 0f, -3f); hi = new Vector3(1.5f, 2.4f, 3f); }
        }

        void Build(Transform torch, Transform floor)
        {
            var spray = new GameObject("Sparks"); spray.transform.SetParent(torch, false); sparks = spray.AddComponent<ParticleSystem>(); sparks.Stop();
            var main = sparks.main; main.loop = true; main.useUnscaledTime = true; main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 600;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.5f); main.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, 5.8f); main.startSize = new ParticleSystem.MinMaxCurve(0.028f, 0.05f); main.gravityModifier = 1.1f;
            var shrink = sparks.sizeOverLifetime; shrink.enabled = true; shrink.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.7f, 0.8f), new Keyframe(1f, 0f)));   // cooling: they go out rather than vanish
            if (visible) { main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.9f); main.startSpeed = new ParticleSystem.MinMaxCurve(1.4f, 3.6f); main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.04f); }
            var em = sparks.emission; em.rateOverTime = 0f;
            var sh = sparks.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = visible ? 38f : 30f; sh.radius = 0.03f;
            if (floor != null) { var cl = sparks.collision; cl.enabled = true; cl.type = ParticleSystemCollisionType.Planes; cl.AddPlane(floor); cl.bounce = 0.35f; cl.dampen = 0.45f; cl.lifetimeLoss = 0.12f; cl.radiusScale = 0.5f; }   // they skitter across the concrete
            var r = spray.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.07f; r.lengthScale = 2f; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            var m = new Material(Resources.Load<Material>("Additive")); m.SetTexture("_BaseMap", Lightswarm.ProceduralSprites.Glow(32, 0.45f).texture); m.SetColor("_BaseColor", new Color(4.2f, 1.45f, 0.32f, 1f)); r.sharedMaterial = m;   // past the bloom's threshold in red and green only: orange-hot, not white
            sparks.Play();
            arc = new GameObject("Arc").AddComponent<Light>(); arc.transform.SetParent(torch, false); arc.transform.position = torch.position + Vector3.up * (glowSize > 1f ? 1.1f : 0.35f);   // unseen over the rear deck, or a little above the torch
            arc.type = LightType.Point; arc.range = glowSize > 1f ? 16f : 12f; arc.intensity = 0f; arc.color = new Color(0.66f, 0.8f, 1f); arc.shadows = LightShadows.None;
            var gm = new Material(Resources.Load<Material>("Additive")); gm.SetTexture("_BaseMap", Lightswarm.ProceduralSprites.Glow(64, 0.18f).texture); gm.SetColor("_BaseColor", new Color(1.1f, 1.35f, 1.8f, 1f));
            // a thin smoke off the work, drifting up
            var sg = new GameObject("Fume"); sg.transform.SetParent(torch, false); sg.transform.rotation = Quaternion.LookRotation(Vector3.up); smoke = sg.AddComponent<ParticleSystem>(); smoke.Stop();
            var sm = smoke.main; sm.loop = true; sm.useUnscaledTime = true; sm.simulationSpace = ParticleSystemSimulationSpace.World; sm.maxParticles = 60; sm.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4f); sm.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.6f); sm.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.7f); sm.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            var sem = smoke.emission; sem.rateOverTime = 0f; var ssh = smoke.shape; ssh.shapeType = ParticleSystemShapeType.Cone; ssh.angle = 12f; ssh.radius = 0.08f;
            var grow = smoke.sizeOverLifetime; grow.enabled = true; grow.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 2.2f));
            var sr = sg.GetComponent<ParticleSystemRenderer>(); sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; sr.receiveShadows = false;
            var smat = new Material(Resources.Load<Material>("Smoke")); smat.SetTexture("_BaseMap", Lightswarm.ProceduralSprites.Glow(64, 0.25f).texture); smat.SetColor("_BaseColor", new Color(0.5f, 0.52f, 0.56f, 0.07f)); sr.sharedMaterial = smat;
            smoke.Play();
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(q.GetComponent<Collider>()); q.name = "ArcGlow"; q.transform.SetParent(torch, false); q.GetComponent<Renderer>().sharedMaterial = gm; glow = q.transform; glow.localScale = Vector3.zero;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (burst > 0f) { burst -= dt; if (burst <= 0f) pause = Random.Range(0.6f, 2.4f); }
            else { pause -= dt; if (pause <= 0f) burst = Random.Range(0.9f, 3.8f); }
            bool on = burst > 0f; var fume = smoke.emission; fume.rateOverTime = on ? 9f : 1.5f; var em = sparks.emission; em.rateOverTime = on ? Random.Range(150f, 380f) * (visible ? 0.45f : 1f) : 0f;
            arc.intensity = on ? Random.Range(arcMax * 0.3f, arcMax) * (Random.value < 0.12f ? 0.25f : 1f) : Mathf.MoveTowards(arc.intensity, 0f, dt * 120f);   // an arc stutters
            float s = on ? Random.Range(0.75f, 1.25f) * glowSize : 0f; glow.localScale = new Vector3(s, s, 1f);
            if (eye != null) glow.rotation = Quaternion.LookRotation(glow.position - eye.transform.position);
            Sfx.Weld(on ? 1f : 0f);
        }

        void OnDisable() { Sfx.Weld(0f); }
    }
}

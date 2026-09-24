using System.Collections.Generic;
using UnityEngine;

namespace IronNight
{
    /// <summary>
    /// The land the night is fought over: a bocage of 40 m fields (plough, pasture, mown hay, stubble) parted by
    /// hedges with gates and hedgerow trees, dirt lanes along the field lines, a farmstead in every tenth field and an
    /// anti-aircraft searchlight post in every sixth. Everything is decided by a hash of the cell coordinates, so the
    /// country is endless and the same every night; only the 5x5 cells around the camera exist as objects. Every solid
    /// thing is a set of circles on the ground that vehicles are pushed out of; buildings and wrecks also stop shells.
    /// </summary>
    public class Props : MonoBehaviour
    {
        class Kind { public string mesh; public float length, height; public float[] circles; }
        enum What { Model, Lane, Hedge, Tree, Decal, Searchlight, Fire }
        class Prop
        {
            public What what; public Kind kind; public Vector3 pos; public float yaw, size, bound = 8f, height = -1f; public int seed;
            public Vector2[] circleCenters = new Vector2[0]; public float[] radii = new float[0];
            public Vector3 a, b; public float[] gaps; public float clearA, clearB;              // hedges: the line and its openings
            public GameObject go; public Mesh mesh, leaves; public float az, el = 42f, track; public LightShaft shaft; public Transform yoke, drum, lamp; public float flakTimer = 4f; public int burst; public Light glow;
            public int state; public float hp = -1f, fallYaw, burn; public bool drivable;   // 0 standing, 1 knocked over, 2 crushed, 3 ruined; a ruin is driven over
        }

        // circles: triples (offsetAlong, offsetSide, radius) in metres, along the prop's own forward axis
        static readonly Kind[] Kinds =
        {
            new Kind { mesh = "farmhouse", length = 13f, height = 7f, circles = new[] { -4f, 0f, 3.6f, 0f, 0f, 3.8f, 4f, 0f, 3.6f } },
            new Kind { mesh = "barn", length = 10f, height = 6.5f, circles = new[] { -2.8f, 0f, 3f, 2.8f, 0f, 3f } },
            new Kind { mesh = "truck", length = 6f, height = 2.6f, circles = new[] { -1.6f, 0f, 1.4f, 1.6f, 0f, 1.4f } },
            new Kind { mesh = "haystack", length = 3.2f, height = 2.2f, circles = new[] { 0f, 0f, 1.7f } },
            new Kind { mesh = "deadtree", length = 6f, height = -1f, circles = new[] { 0f, 0f, 0.7f } },
            new Kind { mesh = "sandbags", length = 7f, height = -1f, circles = new[] { 3f, 0f, 1.1f, -3f, 0f, 1.1f, 0f, 3f, 1.1f, 0f, -3f, 1.1f } },   // a ring of sandbags: the wall, not the pit
            new Kind { mesh = "cottage", length = 9f, height = 6f, circles = new[] { -2.4f, 0f, 3.2f, 2.4f, 0f, 3.2f } },
            new Kind { mesh = "church", length = 22f, height = 12f, circles = new[] { -8f, 0f, 5f, -3f, 0f, 5.6f, 2f, 0f, 5.6f, 7f, 0f, 5.4f } },   // the nave is eleven metres wide, the rubble skirt is driveable
            new Kind { mesh = "wall_a", length = 6f, height = -1f, circles = new[] { -2f, 0f, 1f, 0f, 0f, 1f, 2f, 0f, 1f } },
            new Kind { mesh = "wall_b", length = 6f, height = -1f, circles = new[] { -2f, 0f, 1f, 0f, 0f, 1f, 2f, 0f, 1f } },
            new Kind { mesh = "cart", length = 3.5f, height = 1.8f, circles = new[] { 0f, 0f, 1.4f } },
            new Kind { mesh = "pole", length = 1f, height = -1f, circles = new[] { 0f, 0f, 0.35f } },
            new Kind { mesh = "tree_oak", length = 10f, height = -1f, circles = new[] { 0f, 0f, 0.7f } },
            new Kind { mesh = "tree_poplar", length = 4f, height = -1f, circles = new[] { 0f, 0f, 0.45f } },
            new Kind { mesh = "spruce_snow", length = 8f, height = -1f, circles = new[] { 0f, 0f, 0.7f } },
            new Kind { mesh = "bunker", length = 6f, height = 2.6f, circles = new[] { -1.5f, 0f, 2.2f, 1.5f, 0f, 2.2f } },
            new Kind { mesh = "barrels", length = 2f, height = 1.2f, circles = new[] { 0f, 0f, 1f } },
            new Kind { mesh = "well", length = 2.5f, height = 2.4f, circles = new[] { 0f, 0f, 1.3f } },
            new Kind { mesh = "gate", length = 3.5f, height = -1f, circles = new[] { -1.6f, 0f, 0.5f, 1.6f, 0f, 0.5f } },
            new Kind { mesh = "signpost", length = 1f, height = -1f, circles = new[] { 0f, 0f, 0.3f } },
            new Kind { mesh = "wreck", length = 6f, height = 2.2f, circles = new[] { -1.6f, 0f, 1.7f, 1.6f, 0f, 1.7f } },
            new Kind { mesh = "marker_smoke", length = 0.5f, height = -1f, circles = new float[0] },
            new Kind { mesh = "hedge", length = 8f, height = -1f, circles = new float[0] },   // placed by the hedge lines, which carry the collision
            // Kursk: stand-ins for the Normandy models, and the steppe's own
            new Kind { mesh = "k_izba", length = 9f, height = 6f, circles = new[] { -2.4f, 0f, 3.2f, 2.4f, 0f, 3.2f } },
            new Kind { mesh = "k_khata", length = 9f, height = 5.5f, circles = new[] { -2.4f, 0f, 3.2f, 2.4f, 0f, 3.2f } },
            new Kind { mesh = "k_church", length = 16f, height = 14f, circles = new[] { -4.5f, 0f, 4.2f, 0f, 0f, 4.6f, 4.5f, 0f, 4.2f } },
            new Kind { mesh = "k_well", length = 5f, height = -1f, circles = new[] { 0f, 0f, 1.1f } },
            new Kind { mesh = "k_birches", length = 6f, height = -1f, circles = new[] { 0f, 0f, 0.8f } },
            new Kind { mesh = "k_wattle", length = 6f, height = -1f, circles = new[] { -2f, 0f, 0.6f, 0f, 0f, 0.6f, 2f, 0f, 0.6f } },
            new Kind { mesh = "k_hedgehogs", length = 4f, height = -1f, circles = new[] { 0f, 0f, 1.7f } },
            new Kind { mesh = "k_sunflowers", length = 3f, height = -1f, circles = new float[0] },
            new Kind { mesh = "k_sheaves", length = 1.8f, height = -1f, circles = new float[0] },
        };

        const float Cell = 40f, Half = 20f;
        readonly Dictionary<long, List<Prop>> cells = new Dictionary<long, List<Prop>>();
        readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
        readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        readonly List<Prop> active = new List<Prop>(); readonly HashSet<Prop> wanted = new HashSet<Prop>();
        Transform cam; Material patchMaterial, laneMaterial, yardMaterial, craterMaterial, hedgeMaterial, canopyMaterial, trunkMaterial;
        class Falling { public Prop p; public float a; public Quaternion rot0; public Vector3 axis; }
        readonly List<Falling> falling = new List<Falling>(); Prop blocker; Material burntMaterial; readonly Dictionary<string, Material> sooty = new Dictionary<string, Material>();
        Mesh[] blobs; GameObject lampTemplate; Material leafMaterial; readonly Mesh[] cards = new Mesh[4];   // one leaf-cluster quad per cell of the leaves sheet
        // the ground itself: one grid of 2 m quads that follows the camera; a vertex colour channel per field type
        const float GroundSize = 240f, GroundStep = 2f; const int GroundN = (int)(GroundSize / GroundStep);
        Transform ground; Mesh groundMesh; Material groundMat; Color[] groundColors; int gcx = int.MinValue, gcz;
        readonly List<GameObject> craters = new List<GameObject>(); int nextCrater;   // shell craters of the night, oldest reused
        public Fx fx;

        public bool winter;   // the Ardennes: snow on the fields, bare trees
        public static string Theatre = "normandy";   // "normandy", "ardennes" or "kursk": set before the night is built
        static bool Kursk => Theatre == "kursk";
        public bool wet;      // a rainy night: puddles in the fields
        public Vector3 platoon; public bool alert;          // where the leader is, and whether the posts have been told to look for him
        public bool Lit { get; private set; } public Vector3 LitBy { get; private set; }   // a beam is on the platoon
        readonly HashSet<int> deadLamps = new HashSet<int>();   // posts shot out tonight, by seed
        Material puddleMaterial;

        public void Build(Camera camera)
        {
            cam = camera.transform; LightShaft.cam = camera; string sn = winter ? "_snow" : Kursk ? "_kursk" : "";
            var lit = Resources.Load<Material>("VehicleLit"); var decal = Resources.Load<Material>("GroundDecal");
            foreach (var k in Kinds)
            {
                if (k.mesh.StartsWith("k_") && !Kursk) continue;
                var pf = Resources.Load<GameObject>("Props/" + k.mesh); if (pf == null) continue;
                prefabs[k.mesh] = pf;
                var m = new Material(lit); m.SetTexture("_BaseMap", Resources.Load<Texture2D>("Props/" + k.mesh + "_tex")); m.SetColor("_BaseColor", Tint(k.mesh)); m.SetFloat("_Smoothness", 0.15f); m.SetFloat("_Cull", 0f);
                materials[k.mesh] = m;
                var rp = Resources.Load<GameObject>("Props/" + k.mesh + "_ruin");   // what it looks like knocked down, when there is a model of that
                if (rp != null) { prefabs[k.mesh + "_ruin"] = rp; var rm = new Material(lit); rm.SetTexture("_BaseMap", Resources.Load<Texture2D>("Props/" + k.mesh + "_ruin_tex")); rm.SetColor("_BaseColor", Tint(k.mesh)); rm.SetFloat("_Smoothness", 0.15f); rm.SetFloat("_Cull", 0f); materials[k.mesh + "_ruin"] = rm; }
            }
            BuildGround();
            Material Decal(string tex, int queue) { var m = new Material(decal); m.SetTexture("_BaseMap", Resources.Load<Texture2D>("Textures/" + tex)); m.SetTexture("_BumpMap", Resources.Load<Texture2D>("Textures/" + tex + "_n")); m.SetColor("_BaseColor", new Color(0.95f, 0.95f, 0.95f)); m.renderQueue = queue; return m; }
            scorchMaterial = Decal("scorch", 2449); scorchMaterial.SetTexture("_BumpMap", null);
            puddleMaterial = Decal("puddle", 2447); puddleMaterial.SetTexture("_BumpMap", null); puddleMaterial.SetFloat("_Smoothness", 0.92f);   // still water: the moon on it
            yardMaterial = Decal("yard" + sn, 2440); laneMaterial = Decal("lane" + sn, 2442); laneMaterial.SetTextureScale("_BaseMap", new Vector2(1f, 2f)); craterMaterial = Decal("crater", 2446);
            patchMaterial = new Material(Resources.Load<Material>("Smoke")); patchMaterial.SetTexture("_BaseMap", Lightswarm.ProceduralSprites.GroundShadow(128).texture); patchMaterial.SetColor("_BaseColor", new Color(0.05f, 0.04f, 0.03f, 0.3f)); patchMaterial.renderQueue = 2450;
            hedgeMaterial = new Material(Resources.Load<Material>("FoliageLit")); hedgeMaterial.SetTexture("_BaseMap", Resources.Load<Texture2D>("Textures/hedge")); hedgeMaterial.SetTexture("_BumpMap", Resources.Load<Texture2D>("Textures/hedge_n")); hedgeMaterial.SetColor("_BaseColor", winter ? new Color(0.72f, 0.78f, 0.82f) : new Color(0.9f, 0.95f, 0.85f)); hedgeMaterial.SetFloat("_Smoothness", 0.08f); hedgeMaterial.SetFloat("_Cull", 0f);
            canopyMaterial = new Material(hedgeMaterial); canopyMaterial.SetColor("_BaseColor", new Color(0.95f, 1f, 0.8f));
            trunkMaterial = new Material(Resources.Load<Material>("BarrelLit")); trunkMaterial.SetColor("_BaseColor", new Color(0.26f, 0.21f, 0.15f)); trunkMaterial.SetFloat("_Smoothness", 0.1f); trunkMaterial.SetFloat("_Metallic", 0f);
            blobs = new Mesh[4]; for (int i = 0; i < 4; i++) blobs[i] = Blob(11 + i * 7);
            leafMaterial = Resources.Load<Material>("FoliageCut"); for (int i = 0; i < 4; i++) cards[i] = Card(i);
            // the blobs under the leaves go dark: they are the shadowed inside of the bush
            hedgeMaterial.SetColor("_BaseColor", winter ? new Color(0.72f, 0.78f, 0.82f) : new Color(0.55f, 0.62f, 0.5f)); canopyMaterial.SetColor("_BaseColor", winter ? new Color(0.72f, 0.78f, 0.82f) : new Color(0.52f, 0.6f, 0.48f));
            lampTemplate = LampTemplate();
        }

        // ---- the layout: pure functions of the cell coordinates ----

        static uint Hash(int a, int b, int c) { uint h = 2166136261u; h = (h ^ (uint)a) * 16777619u; h = (h ^ (uint)b) * 16777619u; h = (h ^ (uint)c) * 16777619u; h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15; return h; }
        static float Rnd(int a, int b, int c) => (Hash(a, b, c) & 0xffffff) / 16777216f;
        static int FloorDiv(int a, int b) => a >= 0 ? a / b : -((-a + b - 1) / b);
        static bool Start(int ix, int iz) => ix == 0 && iz == 0;

        /// <summary>0 plough, 1 pasture, 2 mown hay, 3 stubble. Fields come in clumps of two by two with the odd one
        /// out, so the hedges do not box in every single cell.</summary>
        static int FieldType(int ix, int iz) => Rnd(ix, iz, 902) < 0.2f ? (int)(Hash(ix, iz, 903) % 4) : (int)(Hash(FloorDiv(ix, 2), FloorDiv(iz, 2), 901) % 4);
        static bool LaneX(int ix) => ix == 0 || Hash(ix, 1, 910) % 3 == 0;                    // a lane on the line x = ix*40+20
        static bool LaneZ(int iz) => Hash(1, iz, 911) % 4 == 0;                               // a lane on the line z = iz*40+20
        public static string Route = "open";   // "village", "open" or "bocage": set before the night is built
        static float FarmChance => Route == "village" ? 0.17f : Route == "bocage" ? 0.08f : 0.07f;
        static float VillageChance => Route == "village" ? 0.24f : Route == "bocage" ? 0.03f : 0.04f;
        static float HedgeBias => (Route == "bocage" ? 1.45f : Route == "open" ? 0.5f : 0.85f) * (Kursk ? 0.6f : 1f);   // the steppe is open country
        static float TreeBias => Route == "bocage" ? 1.8f : Route == "open" ? 0.6f : 1f;
        static bool Farm(int ix, int iz) => (ix == 1 && iz == 1) || (!Start(ix, iz) && Rnd(ix, iz, 930) < FarmChance);
        static bool Battery(int ix, int iz) => (ix == -1 && iz == 0) || (!Start(ix, iz) && !Farm(ix, iz) && Rnd(ix, iz, 940) < 0.16f);
        static bool Village(int ix, int iz) => (ix == 0 && iz == 3) || (!Start(ix, iz) && !Farm(ix, iz) && !Battery(ix, iz) && Rnd(ix, iz, 1100) < VillageChance);
        static bool HedgeX(int ix, int iz) { if (LaneX(ix)) return false; if (Farm(ix, iz) || Farm(ix + 1, iz)) return true; return Rnd(ix, iz, 920) < Mathf.Min(0.97f, (FieldType(ix, iz) != FieldType(ix + 1, iz) ? 0.85f : 0.3f) * HedgeBias); }
        static bool HedgeZ(int ix, int iz) { if (LaneZ(iz)) return false; if (Farm(ix, iz) || Farm(ix, iz + 1)) return true; return Rnd(ix, iz, 921) < Mathf.Min(0.97f, (FieldType(ix, iz) != FieldType(ix, iz + 1) ? 0.85f : 0.3f) * HedgeBias); }
        static Vector3 In(int ix, int iz, int salt, float r) => new Vector3((Rnd(ix, iz, salt) - 0.5f) * 2f * r, 0f, (Rnd(ix, iz, salt + 1) - 0.5f) * 2f * r);

        // the generated textures come out at different brightnesses; this evens them under the moon
        static Color Tint(string mesh) { switch (mesh) { case "tree_poplar": case "tree_oak": case "spruce_snow": case "hedge": return new Color(0.42f, 0.5f, 0.4f); case "deadtree": return new Color(0.78f, 0.72f, 0.66f); case "haystack": return new Color(0.82f, 0.76f, 0.6f); case "k_birches": return new Color(0.58f, 0.64f, 0.54f); case "k_sunflowers": return new Color(0.7f, 0.68f, 0.56f); case "k_sheaves": return new Color(0.8f, 0.74f, 0.6f); case "sandbags": return new Color(0.78f, 0.74f, 0.66f); default: return new Color(0.8f, 0.78f, 0.74f); } }

        Kind K(string mesh) { foreach (var k in Kinds) if (k.mesh == mesh) return k; return null; }

        /// <summary>What stands in for a Normandy model on the steppe; null when it has none (the gates).</summary>
        static string Local(string mesh)
        {
            if (!Kursk) return mesh;
            switch (mesh)
            {
                case "farmhouse": return "k_izba"; case "cottage": return "k_khata"; case "church": return "k_church"; case "well": return "k_well";
                case "wall_a": case "wall_b": return "k_wattle"; case "gate": return null;
                case "tree_oak": case "tree_poplar": return "k_birches"; case "haystack": return "k_sheaves";
                default: return mesh;
            }
        }

        Prop Place(List<Prop> list, string mesh, Vector3 pos, float yaw)
        {
            mesh = Local(mesh); if (mesh == null) return null;
            var kind = K(mesh); if (kind == null || !prefabs.ContainsKey(mesh)) return null;
            foreach (var o in list) if (o.what == What.Model && (o.pos - pos).sqrMagnitude < 5f * 5f) return null;
            var p = new Prop { what = What.Model, kind = kind, pos = pos, yaw = yaw, height = kind.height, bound = kind.length };
            int n = kind.circles.Length / 3; p.circleCenters = new Vector2[n]; p.radii = new float[n];
            var f = new Vector2(Mathf.Sin(yaw), Mathf.Cos(yaw)); var r = new Vector2(f.y, -f.x);
            for (int c = 0; c < n; c++) { p.circleCenters[c] = new Vector2(pos.x, pos.z) + f * kind.circles[c * 3] + r * kind.circles[c * 3 + 1]; p.radii[c] = kind.circles[c * 3 + 2]; }
            list.Add(p); return p;
        }

        /// <summary>A hedgerow tree: the blob crown, cheap enough for a dozen a screen; the generated oak is for the few that stand alone.</summary>
        void HedgeTree(List<Prop> list, Vector3 pos, int seed)
        {
            if (winter) { Place(list, "deadtree", pos, (seed % 360) * Mathf.Deg2Rad); return; }
            if (PlayerPrefs.GetInt("quality", 1) != 0 && Place(list, "tree_oak", pos, (seed % 360) * Mathf.Deg2Rad) != null) return;
            list.Add(new Prop { what = What.Tree, pos = pos, seed = seed, yaw = (seed % 360) * Mathf.Deg2Rad, bound = 5f, circleCenters = new[] { new Vector2(pos.x, pos.z) }, radii = new[] { 0.8f } });
        }

        void Tree(List<Prop> list, Vector3 pos, int seed)
        {
            if (winter) { if (Place(list, seed % 3 == 0 ? "deadtree" : "spruce_snow", pos, (seed % 360) * Mathf.Deg2Rad) == null) Place(list, "deadtree", pos, (seed % 360) * Mathf.Deg2Rad); return; }   // firs and bare trees in the snow
            if (Place(list, "tree_oak", pos, (seed % 360) * Mathf.Deg2Rad) != null) return;
            list.Add(new Prop { what = What.Tree, pos = pos, seed = seed, yaw = (seed % 360) * Mathf.Deg2Rad, bound = 5f, circleCenters = new[] { new Vector2(pos.x, pos.z) }, radii = new[] { 0.8f } });
        }

        /// <summary>A hedge from a to b with one or two gates, kept clear of the lanes at its ends, and a hedgerow tree
        /// or two. The bushes are built when the cell is spawned.</summary>
        void Hedge(List<Prop> list, Vector3 a, Vector3 b, float clearA, float clearB, int ix, int iz, int salt)
        {
            var dir = (b - a).normalized; float len = (b - a).magnitude;
            var gaps = new List<float> { len * (0.25f + Rnd(ix, iz, 960 + salt) * 0.5f) };
            if (Rnd(ix, iz, 962 + salt) < 0.4f) gaps.Add(len * (0.15f + Rnd(ix, iz, 964 + salt) * 0.7f));
            var p = new Prop { what = What.Hedge, pos = (a + b) * 0.5f, yaw = Mathf.Atan2(dir.x, dir.z), size = len, bound = len * 0.5f + 3f, seed = (int)(Hash(ix, iz, 966 + salt) & 0x7fffffff), a = a, b = b, gaps = gaps.ToArray(), clearA = clearA, clearB = clearB };
            var centers = new List<Vector2>(); var radii = new List<float>();
            for (float u = 1.25f; u < len; u += 2.5f) if (!Open(p, u)) { var c = a + dir * u; centers.Add(new Vector2(c.x, c.z)); radii.Add(1.6f); }
            p.circleCenters = centers.ToArray(); p.radii = radii.ToArray();
            list.Add(p);
            if (Rnd(ix, iz, 968 + salt) < Mathf.Min(0.95f, 0.55f * TreeBias))
            {
                int n = 1 + (int)(Rnd(ix, iz, 969 + salt) * 2f * TreeBias); var side = new Vector3(dir.z, 0f, -dir.x);
                for (int i = 0; i < n; i++) { float u = len * (0.1f + 0.8f * (i + Rnd(ix, iz, 970 + salt + i)) / n); if (Open(p, u)) continue; HedgeTree(list, a + dir * u + side * 0.4f, (int)(Hash(ix, iz, 975 + salt + i) & 0xffff)); }
            }
        }

        static bool Open(Prop hedge, float u)
        {
            if (u < hedge.clearA || u > hedge.size - hedge.clearB) return true;
            foreach (var g in hedge.gaps) if (Mathf.Abs(u - g) < 5f) return true;
            return false;
        }

        /// <summary>House, barn and hay around a trodden yard, the way farms sit in the corner of their fields.</summary>
        void FarmYard(List<Prop> list, int ix, int iz, Vector3 c)
        {
            var o = c + In(ix, iz, 980, 3f);
            float yaw = (Rnd(ix, iz, 982) < 0.5f ? 0f : Mathf.PI / 2f) + (Rnd(ix, iz, 983) - 0.5f) * 0.2f;
            var f = new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw)); var r = new Vector3(f.z, 0f, -f.x);
            list.Add(new Prop { what = What.Decal, pos = o, yaw = yaw, size = 30f, seed = 0 });
            Place(list, Rnd(ix, iz, 1110) < 0.35f ? "cottage" : "farmhouse", o - r * 7f, yaw);
            // a low wall closes the south side of the yard, with a gap for the gate
            Place(list, Rnd(ix, iz, 1111) < 0.5f ? "wall_a" : "wall_b", o - f * 13f - r * 4f, yaw + Mathf.PI / 2f); Place(list, Rnd(ix, iz, 1112) < 0.5f ? "wall_a" : "wall_b", o - f * 13f + r * 10f, yaw + Mathf.PI / 2f);
            if (Rnd(ix, iz, 1113) < 0.6f) Place(list, "cart", o + r * 4f - f * 4f, yaw + Rnd(ix, iz, 1114) * 6.28f);
            Place(list, "gate", o - f * 13f + r * 3f, yaw + Mathf.PI / 2f);
            Place(list, "barn", o + f * 9.5f + r * 3f, yaw + Mathf.PI / 2f);
            if (Rnd(ix, iz, 1145) < 0.35f) Place(list, "barrels", o + f * 4f + r * 9f, Rnd(ix, iz, 1146) * 6.28f);
            int hay = 2 + (int)(Rnd(ix, iz, 984) * 2f);
            for (int h = 0; h < hay; h++) Place(list, "haystack", o + r * (8f + Rnd(ix, iz, 985 + h) * 3f) + f * (-6f + h * 4.5f), Rnd(ix, iz, 995 + h) * 6.28f);
            if (Rnd(ix, iz, 986) < 0.45f) Place(list, "truck", o - f * 9f + r * (Rnd(ix, iz, 987) * 5f - 1f), yaw + 1.2f + (Rnd(ix, iz, 988) - 0.5f) * 0.8f);
            if (Rnd(ix, iz, 989) < 0.5f) Tree(list, o - f * 8f - r * 9f, (int)(Hash(ix, iz, 990) & 0xffff));
        }

        /// <summary>A shelled village: the church, two cottages, a bit of wall, a cart, on a trodden square.</summary>
        void VillageSquare(List<Prop> list, int ix, int iz, Vector3 c)
        {
            var o = c + In(ix, iz, 1120, 2f); float yaw = (Rnd(ix, iz, 1122) < 0.5f ? 0f : Mathf.PI / 2f) + (Rnd(ix, iz, 1123) - 0.5f) * 0.2f;
            var f = new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw)); var r = new Vector3(f.z, 0f, -f.x);
            list.Add(new Prop { what = What.Decal, pos = o, yaw = yaw, size = 34f, seed = 0 });
            Place(list, "church", o + f * 4f, yaw);
            Place(list, "cottage", o - r * 16f - f * 6f, yaw + Mathf.PI / 2f);
            Place(list, "cottage", o + r * 16f - f * 2f, yaw - Mathf.PI / 2f);
            Place(list, Rnd(ix, iz, 1124) < 0.5f ? "wall_a" : "wall_b", o - f * 14f - r * 6f, yaw + Mathf.PI / 2f); Place(list, Rnd(ix, iz, 1125) < 0.5f ? "wall_a" : "wall_b", o - f * 14f + r * 8f, yaw + Mathf.PI / 2f);
            Place(list, "cart", o - r * 6f - f * 8f, yaw + Rnd(ix, iz, 1126) * 6.28f);
            Place(list, "well", o - r * 4f - f * 12f, yaw);
            if (Rnd(ix, iz, 1127) < 0.6f) Place(list, "truck", o + r * 9f - f * 11f, yaw + 0.4f);
            Tree(list, o + r * 9f + f * 12f, (int)(Hash(ix, iz, 1128) & 0xffff));
        }

        /// <summary>An anti-aircraft searchlight on its trailer, sandbagged, with the generator lorry beside it.</summary>
        void SearchlightPost(List<Prop> list, int ix, int iz, Vector3 c)
        {
            var o = c + In(ix, iz, 1000, 5f); float yaw = Rnd(ix, iz, 1002) * 6.28f;
            var f = new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw)); var r = new Vector3(f.z, 0f, -f.x);
            list.Add(new Prop { what = What.Decal, pos = o, yaw = yaw, size = 18f, seed = 0 });
            list.Add(new Prop { what = What.Searchlight, pos = o, yaw = yaw, seed = (int)(Hash(ix, iz, 1003) & 0xffff), bound = 4f, circleCenters = new[] { new Vector2(o.x, o.z) }, radii = new[] { 1.8f } });
            Place(list, "sandbags", o, yaw + Mathf.PI);   // the ring round the lamp, its opening away from the front
            Place(list, "truck", o + f * 9f + r * 3f, yaw + 1.3f + (Rnd(ix, iz, 1004) - 0.5f) * 0.6f);
            Place(list, "barrels", o + f * 9f - r * 3f, Rnd(ix, iz, 1005) * 6.28f);
            if (Rnd(ix, iz, 1006) < 0.5f) Place(list, "bunker", o - f * 8f + r * 2f, yaw);
        }

        /// <summary>Hay bales in the mown and stubble fields, a wreck or a crater anywhere, a lone tree, a dead one.</summary>
        void Loose(List<Prop> list, int ix, int iz, Vector3 c, int type)
        {
            if (Kursk)
            {
                if (type == 1 && Rnd(ix, iz, 1200) < 0.5f) { int m = 2 + (int)(Rnd(ix, iz, 1201) * 4f); for (int h = 0; h < m; h++) Place(list, "k_sunflowers", c + In(ix, iz, 1202 + h * 2, 14f), Rnd(ix, iz, 1215 + h) * 6.28f); }
                if (Rnd(ix, iz, 1220) < 0.12f) Place(list, "k_hedgehogs", c + In(ix, iz, 1221, 13f), Rnd(ix, iz, 1223) * 6.28f);
                if (type == 2 && Rnd(ix, iz, 1230) < 0.22f)
                {
                    var mid = c + In(ix, iz, 1231, 9f); float ang = Rnd(ix, iz, 1233) * 6.28f; var d = new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang));
                    list.Add(new Prop { what = What.Fire, pos = mid, yaw = ang, a = mid - d * 8f, b = mid + d * 8f, seed = (int)(Hash(ix, iz, 1234) & 0xffff), bound = 10f });
                }
            }
            if (type >= 2 && !(Kursk && type == 2)) { int n = (int)(Rnd(ix, iz, 1010) * 4f); for (int h = 0; h < n; h++) Place(list, "haystack", c + In(ix, iz, 1011 + h * 2, 13f), Rnd(ix, iz, 1030 + h) * 6.28f); }
            if (Rnd(ix, iz, 1040) < 0.1f) Place(list, "truck", c + In(ix, iz, 1041, 12f), Rnd(ix, iz, 1043) * 6.28f);
            if (Rnd(ix, iz, 1044) < 0.1f) Place(list, "deadtree", c + In(ix, iz, 1045, 14f), Rnd(ix, iz, 1047) * 6.28f);
            if (Rnd(ix, iz, 1048) < 0.08f * TreeBias) Tree(list, c + In(ix, iz, 1049, 12f), (int)(Hash(ix, iz, 1051) & 0xffff));
            if (wet) { int puddles = (type == 0 ? 3 : 1) + (int)(Rnd(ix, iz, 1080) * 3f); for (int k = 0; k < puddles; k++) list.Add(new Prop { what = What.Decal, seed = 2, pos = c + In(ix, iz, 1081 + k * 2, 17f), yaw = Rnd(ix, iz, 1090 + k) * 6.28f, size = 3f + Rnd(ix, iz, 1096 + k) * 4f }); }
            int craters = Rnd(ix, iz, 1052) < 0.35f ? 1 + (int)(Rnd(ix, iz, 1053) * 2f) : 0;
            for (int k = 0; k < craters; k++) list.Add(new Prop { what = What.Decal, seed = 1, pos = c + In(ix, iz, 1054 + k * 2, 16f), yaw = Rnd(ix, iz, 1060 + k) * 6.28f, size = 4f + Rnd(ix, iz, 1064 + k) * 3f });
            if (Rnd(ix, iz, 1070) < 0.05f) Place(list, "sandbags", c + In(ix, iz, 1071, 12f), Rnd(ix, iz, 1073) * 6.28f);
            if (Rnd(ix, iz, 1074) < 0.04f) Place(list, "wreck", c + In(ix, iz, 1075, 12f), Rnd(ix, iz, 1077) * 6.28f);
        }

        List<Prop> CellProps(int ix, int iz)
        {
            long key = ((long)ix << 32) ^ (uint)iz; if (cells.TryGetValue(key, out var list)) return list;
            list = new List<Prop>(); var c = new Vector3(ix * Cell, 0f, iz * Cell);
            // the lanes and hedges on the cell's east and north lines; the west and south ones belong to the neighbours
            if (LaneX(ix)) { list.Add(new Prop { what = What.Lane, pos = c + new Vector3(Half, 0f, 0f), yaw = 0f, size = Cell }); foreach (var u in new[] { -12f, 8f }) Place(list, "pole", c + new Vector3(Half + 3.6f, 0f, u), 0f); }
            if (LaneZ(iz)) { list.Add(new Prop { what = What.Lane, pos = c + new Vector3(0f, 0f, Half), yaw = Mathf.PI / 2f, size = Cell }); foreach (var u in new[] { -12f, 8f }) Place(list, "pole", c + new Vector3(u, 0f, Half + 3.6f), Mathf.PI / 2f); }
            // a row of poplars on the other side of every third lane; a signpost where two lanes cross
            if (LaneX(ix) && !winter && Rnd(ix, iz, 1140) < 0.35f) foreach (var u in new[] { -16f, -4f, 8f }) Place(list, "tree_poplar", c + new Vector3(Half - 4.2f, 0f, u + Rnd(ix, iz, 1141 + (int)u) * 2f), Rnd(ix, iz, 1150 + (int)u) * 6.28f);
            if (LaneZ(iz) && !winter && Rnd(ix, iz, 1142) < 0.35f) foreach (var u in new[] { -16f, -4f, 8f }) Place(list, "tree_poplar", c + new Vector3(u + Rnd(ix, iz, 1143 + (int)u) * 2f, 0f, Half - 4.2f), Rnd(ix, iz, 1160 + (int)u) * 6.28f);
            if (LaneX(ix) && LaneZ(iz)) Place(list, "signpost", c + new Vector3(Half + 4.5f, 0f, Half + 4.5f), Rnd(ix, iz, 1144) * 6.28f);
            if (HedgeX(ix, iz)) Hedge(list, c + new Vector3(Half, 0f, -Half), c + new Vector3(Half, 0f, Half), LaneZ(iz - 1) ? 4f : 0f, LaneZ(iz) ? 4f : 0f, ix, iz, 0);
            if (HedgeZ(ix, iz)) Hedge(list, c + new Vector3(-Half, 0f, Half), c + new Vector3(Half, 0f, Half), LaneX(ix - 1) ? 4f : 0f, LaneX(ix) ? 4f : 0f, ix, iz, 1);
            if (Farm(ix, iz)) FarmYard(list, ix, iz, c);
            else if (Battery(ix, iz)) SearchlightPost(list, ix, iz, c);
            else if (Village(ix, iz)) VillageSquare(list, ix, iz, c);
            else if (!Start(ix, iz)) Loose(list, ix, iz, c, FieldType(ix, iz));
            cells[key] = list; return list;
        }

        // ---- the ground: a 240 m grid under the camera, vertex colours saying which field each corner is in ----

        void BuildGround()
        {
            groundMat = new Material(Resources.Load<Material>("Ground"));
            if (winter || Kursk)
            {
                string[] set = { "plough", "pasture", "mown", "stubble" }; string pre = winter ? "Textures/ground_snow_" : "Textures/ground_kursk_";   // Kursk: black earth, steppe, standing wheat, stubble
                for (int i = 0; i < 4; i++) { groundMat.SetTexture("_Tex" + i, Resources.Load<Texture2D>(pre + set[i])); groundMat.SetTexture("_Nrm" + i, Resources.Load<Texture2D>(pre + set[i] + "_n")); }
                groundMat.SetFloat("_VariationStrength", 0.25f);
            }
            int n = GroundN + 1; var v = new Vector3[n * n]; var nm = new Vector3[n * n]; groundColors = new Color[n * n];
            for (int z = 0; z < n; z++) for (int x = 0; x < n; x++) { v[z * n + x] = new Vector3(x * GroundStep - GroundSize / 2f, 0f, z * GroundStep - GroundSize / 2f); nm[z * n + x] = Vector3.up; }
            var t = new int[GroundN * GroundN * 6]; int k = 0;
            for (int z = 0; z < GroundN; z++) for (int x = 0; x < GroundN; x++) { int a = z * n + x; t[k++] = a; t[k++] = a + n; t[k++] = a + 1; t[k++] = a + 1; t[k++] = a + n; t[k++] = a + n + 1; }
            groundMesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; groundMesh.vertices = v; groundMesh.normals = nm; groundMesh.triangles = t; groundMesh.colors = groundColors;
            groundMesh.bounds = new Bounds(Vector3.zero, new Vector3(GroundSize, 2f, GroundSize));
            var go = new GameObject("Ground"); go.transform.SetParent(transform, false); go.AddComponent<MeshFilter>().sharedMesh = groundMesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = groundMat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; ground = go.transform;
        }

        /// <summary>A shell near a searchlight's drum puts the light out for the night.</summary>
        public bool HitLamp(Vector3 at)
        {
            foreach (var p in active)
            {
                if (p.what != What.Searchlight || deadLamps.Contains(p.seed) || p.drum == null) continue;
                if ((p.drum.position - at).sqrMagnitude > 2.4f * 2.4f) continue;
                deadLamps.Add(p.seed); KillLamp(p); return true;
            }
            return false;
        }
        void KillLamp(Prop p) { if (p.shaft != null) p.shaft.gameObject.SetActive(false); if (p.lamp != null) p.lamp.gameObject.SetActive(false); }

        /// <summary>Moves the grid to the cell under the camera and recolours it: a corner deep inside a field is that
        /// field, a corner near a boundary is a mix of the two, so the textures cross-fade over five metres or so.</summary>
        void Recentre(int cx, int cz)
        {
            gcx = cx; gcz = cz; var origin = new Vector3(cx * Cell, 0f, cz * Cell); ground.position = origin; int n = GroundN + 1;
            for (int z = 0; z < n; z++) for (int x = 0; x < n; x++)
            {
                float wx = origin.x + x * GroundStep - GroundSize / 2f, wz = origin.z + z * GroundStep - GroundSize / 2f; var c = new Color(0f, 0f, 0f, 0f);
                for (int dz = -1; dz <= 1; dz++) for (int dx = -1; dx <= 1; dx++) c[FieldType(Mathf.RoundToInt((wx + dx * 2.5f) / Cell), Mathf.RoundToInt((wz + dz * 2.5f) / Cell))] += 1f / 9f;
                groundColors[z * n + x] = c;
            }
            groundMesh.colors = groundColors;
        }

        /// <summary>One model for the battle to place itself (no collision): null when the model is not there.</summary>
        public GameObject Spawn(string mesh, Vector3 pos, float yawDeg)
        {
            if (!prefabs.ContainsKey(mesh)) return null;
            var go = Instantiate(prefabs[mesh], transform); go.name = mesh; go.transform.position = pos; go.transform.rotation = Quaternion.Euler(0f, yawDeg, 0f);
            foreach (var r in go.GetComponentsInChildren<Renderer>()) { r.sharedMaterial = materials[mesh]; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
            return go;
        }

        /// <summary>Rain: the ground goes glossy under the moon.</summary>
        public void SetWet(float smoothness) { groundMat.SetFloat("_Smoothness", smoothness); }

        // ---- objects: only the cells around the camera exist ----

        /// <summary>Keeps objects alive in the 5x5 cells around the camera's ground point and turns the searchlights.</summary>
        public void Tick()
        {
            var g = cam.position + cam.forward * 45f; int cx = Mathf.RoundToInt(g.x / Cell), cz = Mathf.RoundToInt(g.z / Cell);
            if (cx != gcx || cz != gcz) Recentre(cx, cz);
            wanted.Clear();
            for (int ix = cx - 2; ix <= cx + 2; ix++) for (int iz = cz - 2; iz <= cz + 2; iz++) foreach (var p in CellProps(ix, iz)) wanted.Add(p);
            for (int i = active.Count - 1; i >= 0; i--) if (!wanted.Contains(active[i])) { Unload(active[i]); active.RemoveAt(i); }
            foreach (var p in wanted) if (p.go == null) { Spawn(p); active.Add(p); }
            float time = Time.time; bool litNow = false; Vector3 litBy = Vector3.zero;
            for (int i = falling.Count - 1; i >= 0; i--)
            {
                var f = falling[i]; if (f.p.go == null) { falling.RemoveAt(i); continue; }
                f.a = Mathf.Min(1f, f.a + Time.deltaTime / 1.1f); f.p.go.transform.rotation = Quaternion.AngleAxis(88f * f.a * f.a, f.axis) * f.rot0;   // slow to start, fast at the end, like a tree going over
                if (f.a >= 1f) { if (fx != null) fx.Dust(f.p.pos + FallDir(f.p) * 3.5f); falling.RemoveAt(i); }
            }
            foreach (var p in active)
            {
                if (p.burn <= 0f) continue;
                p.burn -= Time.deltaTime; if (p.glow != null) p.glow.intensity = Mathf.Min(1f, p.burn / 4f) * (3f + Mathf.PerlinNoise(time * 4f, p.seed * 0.01f) * 2.5f);
                p.flakTimer -= Time.deltaTime; if (p.flakTimer > 0f || fx == null) continue; p.flakTimer = p.burn > 4f ? 0.12f : 0.3f;
                fx.Burn(p.pos + new Vector3(Random.Range(-2f, 2f), -1.4f, Random.Range(-2f, 2f)));
            }
            foreach (var p in active)
            {
                if (p.what != What.Fire || fx == null) continue;
                p.glow.intensity = 4f + Mathf.PerlinNoise(time * 3f, p.seed * 0.01f) * 3f;
                p.flakTimer -= Time.deltaTime; if (p.flakTimer > 0f) continue; p.flakTimer = 0.07f;
                fx.Burn(Vector3.Lerp(p.a, p.b, Random.value) + new Vector3(Random.Range(-0.8f, 0.8f), -1.6f, Random.Range(-0.8f, 0.8f)));
            }
            foreach (var p in active)
            {
                if (p.what != What.Searchlight || deadLamps.Contains(p.seed)) continue;
                // the beam wanders round the sky between 22 and 62 degrees up; the lamp glow faces the camera. Once the posts are
                // alerted, one within 75 m swings down and follows the leader; the sweep is picked up again when he is gone
                float ph = p.seed * 0.37f, az = time * 7f * (p.seed % 2 == 0 ? 1f : -1f) + ph * 57f, el = 42f + Mathf.Sin(time * 0.17f + ph) * 20f;
                var toL = platoon - p.pos; float distL = new Vector2(toL.x, toL.z).magnitude; bool tracking = false;   // the AA posts keep to the sky: turning a Flak searchlight on tanks was a rare thing (Seelow, 1945), not a Normandy night
                if (tracking) { az = Mathf.Atan2(toL.x, toL.z) * Mathf.Rad2Deg - p.yaw * Mathf.Rad2Deg; el = Mathf.Max(3f, Mathf.Atan2(2.4f, distL) * Mathf.Rad2Deg); p.track = Mathf.Min(1f, p.track + Time.deltaTime * 0.5f); }
                else p.track = Mathf.Max(0f, p.track - Time.deltaTime * 0.5f);
                p.az = Mathf.MoveTowardsAngle(p.az, az, (tracking ? 30f : 90f) * Time.deltaTime); p.el = Mathf.MoveTowards(p.el, el, 25f * Time.deltaTime);
                p.yoke.localRotation = Quaternion.Euler(0f, p.az, 0f); p.drum.localRotation = Quaternion.Euler(-p.el, 0f, 0f);
                if (tracking && p.track >= 1f && Vector3.Angle(p.drum.forward, (platoon + Vector3.up - p.drum.position).normalized) < 5f) { litNow = true; litBy = p.pos; }
                p.lamp.rotation = cam.rotation;
                p.shaft.Set(p.drum.position + p.drum.forward * 0.6f, p.drum.forward);
                // the post's gun fires a burst at the sky now and then: five tracers climbing along the beam
                p.flakTimer -= Time.deltaTime;
                if (p.flakTimer <= 0f) { if (p.burst == 0) p.burst = 5; p.flakTimer = p.burst > 1 ? 0.13f : 7f + Rnd(p.seed, (int)(time * 10f), 3) * 12f; p.burst--; if (fx != null) { var from = p.pos + Quaternion.Euler(0f, p.yaw * Mathf.Rad2Deg, 0f) * new Vector3(5f, 1.2f, 1f); fx.Flak(from, (p.drum.forward + Random.insideUnitSphere * 0.06f).normalized); if (p.burst == 4) Sfx.Flak(from); } }
            }
            Lit = litNow; LitBy = litBy;
        }

        void Unload(Prop p)
        {
            if (p.shaft != null) Destroy(p.shaft.gameObject);
            Destroy(p.go); p.go = null; if (p.mesh != null) { Destroy(p.mesh); p.mesh = null; } if (p.leaves != null) { Destroy(p.leaves); p.leaves = null; }
        }

        static GameObject Quad(Transform parent, Vector3 pos, float yawDeg, float w, float h, Material m, float y)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(q.GetComponent<Collider>()); q.transform.SetParent(parent, false);
            q.transform.position = new Vector3(pos.x, y, pos.z); q.transform.rotation = Quaternion.Euler(90f, yawDeg, 0f); q.transform.localScale = new Vector3(w, h, 1f);
            var r = q.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return q;
        }

        void Spawn(Prop p)
        {
            float yawDeg = p.yaw * Mathf.Rad2Deg;
            switch (p.what)
            {
                case What.Lane: p.go = Quad(transform, p.pos, yawDeg, 6f, Cell, laneMaterial, 0.02f); p.go.name = "Lane"; break;
                case What.Decal: p.go = Quad(transform, p.pos, yawDeg, p.size, p.size, p.seed == 0 ? yardMaterial : p.seed == 2 ? puddleMaterial : craterMaterial, p.seed == 0 ? 0.03f : p.seed == 2 ? 0.04f : 0.05f); p.go.name = p.seed == 0 ? "Yard" : p.seed == 2 ? "Puddle" : "Crater"; break;
                case What.Hedge: SpawnHedge(p); break;
                case What.Tree: SpawnTree(p); if (p.state == 1 && p.go != null) p.go.transform.rotation = Fallen(p); break;
                case What.Searchlight: SpawnSearchlight(p); break;
                case What.Fire:
                {
                    var go = new GameObject("FieldFire"); go.transform.SetParent(transform, false); go.transform.position = p.pos;
                    Quad(go.transform, p.pos, yawDeg, 5f, 18f, scorchMaterial, 0.045f);
                    var lg = new GameObject("FireLight"); lg.transform.SetParent(go.transform, false); lg.transform.position = p.pos + Vector3.up * 2.5f;
                    p.glow = lg.AddComponent<Light>(); p.glow.type = LightType.Point; p.glow.color = new Color(1f, 0.55f, 0.2f); p.glow.range = 22f; p.glow.intensity = 5f; p.glow.shadows = LightShadows.None;
                    p.flakTimer = 0f; p.go = go; break;
                }
                default:
                {
                    if (p.state == 3) { SpawnRuin(p); break; }
                    var go = Instantiate(prefabs[p.kind.mesh], transform); go.name = p.kind.mesh;
                    go.transform.position = p.pos; go.transform.rotation = Quaternion.Euler(0f, yawDeg, 0f);
                    foreach (var r in go.GetComponentsInChildren<Renderer>()) { r.sharedMaterial = materials[p.kind.mesh]; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
                    // trodden earth under it: a soft dark patch a little wider than the footprint
                    for (int c = 0; c < p.radii.Length; c++) { var q = Quad(go.transform, new Vector3(p.circleCenters[c].x, 0f, p.circleCenters[c].y), 0f, p.radii[c] * 2.4f, p.radii[c] * 2.4f, patchMaterial, 0.06f); q.GetComponent<Renderer>().receiveShadows = false; }
                    if (p.state == 1) go.transform.rotation = Fallen(p); else if (p.state == 2) go.transform.localScale = new Vector3(1f, 0.22f, 1f);
                    p.go = go; break;
                }
            }
        }

        /// <summary>Two staggered rows of bushes along the line, skipping the gates, combined into one mesh.</summary>
        /// <summary>The hedge as the generated hawthorn model (from the reference picture), one 8 m section after another
        /// along the line, skipping the gates; the blob hedge stays for the low quality setting.</summary>
        void SpawnHedge(Prop p)
        {
            string hm = Kursk ? "k_wattle" : "hedge";
            if (prefabs.ContainsKey(hm) && PlayerPrefs.GetInt("quality", 1) != 0) { SpawnHedgeModel(p, hm); return; }
            var rng = new System.Random(p.seed); var dir = (p.b - p.a).normalized; var side = new Vector3(dir.z, 0f, -dir.x);
            var parts = new List<CombineInstance>();
            for (float u = 0.7f; u < p.size - 0.5f; u += 1.1f)
            {
                if (Open(p, u)) continue;
                for (int row = -1; row <= 1; row += 2)
                {
                    float sx = 1.3f + (float)rng.NextDouble() * 0.6f, sy = 0.85f + (float)rng.NextDouble() * 0.4f, sz = sx * (0.8f + (float)rng.NextDouble() * 0.3f);
                    var local = p.a - p.pos + dir * (u + ((float)rng.NextDouble() - 0.5f) * 0.5f) + side * (row * 0.5f + ((float)rng.NextDouble() - 0.5f) * 0.3f) + Vector3.up * (0.45f * sy);
                    parts.Add(new CombineInstance { mesh = blobs[rng.Next(blobs.Length)], transform = Matrix4x4.TRS(local, Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f), new Vector3(sx, sy, sz)) });
                }
            }
            var go = new GameObject("Hedge"); go.transform.SetParent(transform, false); go.transform.position = p.pos;
            var mesh = new Mesh(); mesh.CombineMeshes(parts.ToArray(), true, true); mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh; var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = hedgeMaterial;
            if (!winter) p.leaves = Leaves(go.transform, parts, rng, 6, 1.8f, 0f);
            p.go = go; p.mesh = mesh;
        }

        void SpawnHedgeModel(Prop p, string hm)
        {
            var rng = new System.Random(p.seed); var dir = (p.b - p.a).normalized; float yawDeg = p.yaw * Mathf.Rad2Deg;
            var go = new GameObject("Hedge"); go.transform.SetParent(transform, false); go.transform.position = p.pos;
            float Section = hm == "hedge" ? 7.6f : 5.8f;   // the models are 8 and 6 m: a little overlap hides the joins
            for (float u = 0f; u + Section * 0.6f < p.size; u += Section)
            {
                float mid = u + Section * 0.5f; if (Open(p, u + 0.8f) || Open(p, mid) || Open(p, u + Section - 0.8f)) continue;
                var sec = Instantiate(prefabs[hm], go.transform); sec.transform.position = p.a + dir * mid; sec.transform.rotation = Quaternion.Euler(0f, yawDeg + (rng.Next(2) == 0 ? 0f : 180f), 0f);
                sec.transform.localScale = new Vector3(1f, 0.85f + (float)rng.NextDouble() * 0.3f, 1f);
                foreach (var r in sec.GetComponentsInChildren<Renderer>()) { r.sharedMaterial = materials[hm]; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
            }
            p.go = go;
        }

        /// <summary>A hedgerow tree: a trunk and a crown of three or four leaf blobs, nine metres tall.</summary>
        void SpawnTree(Prop p)
        {
            var rng = new System.Random(p.seed); var go = new GameObject("Tree"); go.transform.SetParent(transform, false); go.transform.position = p.pos;
            float h = 4.6f + (float)rng.NextDouble() * 1.6f;
            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(trunk.GetComponent<Collider>()); trunk.transform.SetParent(go.transform, false);
            trunk.transform.localPosition = new Vector3(0f, h * 0.5f, 0f); trunk.transform.localScale = new Vector3(0.55f, h * 0.5f, 0.55f); trunk.GetComponent<Renderer>().sharedMaterial = trunkMaterial;
            var parts = new List<CombineInstance>(); int n = 3 + rng.Next(2);
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n + (float)rng.NextDouble(), d = i == 0 ? 0f : 0.8f + (float)rng.NextDouble() * 1f, s = 2f + (float)rng.NextDouble() * 0.9f;
                var local = new Vector3(Mathf.Cos(a) * d, h + 0.6f + (float)rng.NextDouble() * 1.4f - (i == 0 ? 0f : 0.8f), Mathf.Sin(a) * d);
                parts.Add(new CombineInstance { mesh = blobs[rng.Next(blobs.Length)], transform = Matrix4x4.TRS(local, Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f), new Vector3(s, s * 0.85f, s)) });
            }
            var crown = new GameObject("Crown"); crown.transform.SetParent(go.transform, false);
            var mesh = new Mesh(); mesh.CombineMeshes(parts.ToArray(), true, true); mesh.RecalculateBounds();
            crown.AddComponent<MeshFilter>().sharedMesh = mesh; crown.AddComponent<MeshRenderer>().sharedMaterial = canopyMaterial;
            p.leaves = Leaves(crown.transform, parts, rng, 8, 1.9f, 0f);
            p.go = go; p.mesh = mesh;
        }

        void SpawnSearchlight(Prop p)
        {
            var go = Instantiate(lampTemplate, transform); go.SetActive(true); go.name = "Searchlight";
            go.transform.position = p.pos; go.transform.rotation = Quaternion.Euler(0f, p.yaw * Mathf.Rad2Deg, 0f);
            p.yoke = go.transform.Find("Yoke"); p.drum = p.yoke.Find("Drum"); p.lamp = p.drum.Find("Lamp");
            var shaft = new GameObject("Beam"); shaft.transform.SetParent(transform, false); p.shaft = shaft.AddComponent<LightShaft>();
            p.go = go; if (deadLamps.Contains(p.seed)) KillLamp(p);
        }

        /// <summary>The searchlight itself: a drum on a yoke on a pedestal on a four-wheel trailer, its face glowing.</summary>
        static readonly Vector3 LampPivot = new Vector3(0.01f, 2.39f, -0.5f); const float LampLensZ = 1.0f;   // where the drum turns on the trailer model (split_lamp.py), and how far ahead its lens is

        GameObject LampTemplate()
        {
            var metal = new Material(Resources.Load<Material>("BarrelLit")); metal.SetColor("_BaseColor", new Color(0.38f, 0.4f, 0.4f)); metal.SetFloat("_Smoothness", 0.45f); metal.SetFloat("_Metallic", 0.5f);
            var lens = new Material(Resources.Load<Material>("Smoke")); lens.SetColor("_BaseColor", new Color(2.5f, 2.7f, 3.2f, 1f)); lens.renderQueue = 3005;
            var glowMat = new Material(Resources.Load<Material>("Additive")); glowMat.SetTexture("_BaseMap", Lightswarm.ProceduralSprites.Glow(64, 0.25f).texture); glowMat.SetColor("_BaseColor", new Color(1.2f, 1.3f, 1.6f, 0.9f));
            GameObject Part(PrimitiveType t, Transform parent, Vector3 pos, Vector3 scale, Vector3 euler, Material m)
            {
                var g = GameObject.CreatePrimitive(t); Destroy(g.GetComponent<Collider>()); g.transform.SetParent(parent, false);
                g.transform.localPosition = pos; g.transform.localScale = scale; g.transform.localRotation = Quaternion.Euler(euler); g.GetComponent<Renderer>().sharedMaterial = m; return g;
            }
            var root = new GameObject("SearchlightTemplate"); root.transform.SetParent(transform, false);
            var basePf = Resources.Load<GameObject>("Props/searchlight_base"); var drumPf = Resources.Load<GameObject>("Props/searchlight_drum");
            if (basePf != null && drumPf != null)
            {
                var lm = new Material(Resources.Load<Material>("VehicleLit")); lm.SetTexture("_BaseMap", Resources.Load<Texture2D>("Props/searchlight_tex")); lm.SetColor("_BaseColor", new Color(0.85f, 0.85f, 0.85f)); lm.SetFloat("_Smoothness", 0.3f); lm.SetFloat("_Cull", 0f);
                var b = Instantiate(basePf, root.transform); foreach (var rr in b.GetComponentsInChildren<Renderer>()) rr.sharedMaterial = lm;
                var yoke2 = new GameObject("Yoke"); yoke2.transform.SetParent(root.transform, false); yoke2.transform.localPosition = LampPivot;
                var drum2 = new GameObject("Drum"); drum2.transform.SetParent(yoke2.transform, false);
                var dm = Instantiate(drumPf, drum2.transform); foreach (var rr in dm.GetComponentsInChildren<Renderer>()) rr.sharedMaterial = lm;
                var glow2 = Part(PrimitiveType.Quad, drum2.transform, new Vector3(0f, 0f, LampLensZ), Vector3.one * 4.5f, Vector3.zero, glowMat); glow2.name = "Lamp"; glow2.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var light2 = new GameObject("LampLight").AddComponent<Light>(); light2.transform.SetParent(drum2.transform, false); light2.transform.localPosition = new Vector3(0f, 0f, LampLensZ + 0.5f);
                light2.type = LightType.Point; light2.color = new Color(0.75f, 0.82f, 1f); light2.intensity = 26f; light2.range = 26f; light2.shadows = LightShadows.None;
                root.SetActive(false); return root;
            }
            Part(PrimitiveType.Cube, root.transform, new Vector3(0f, 0.8f, 0f), new Vector3(2.6f, 0.3f, 1.8f), Vector3.zero, metal);
            Part(PrimitiveType.Cube, root.transform, new Vector3(0f, 0.55f, -1.7f), new Vector3(0.2f, 0.12f, 1.4f), Vector3.zero, metal);       // the tow bar
            foreach (var x in new[] { -0.95f, 0.95f }) foreach (var z in new[] { -0.7f, 0.7f }) Part(PrimitiveType.Cylinder, root.transform, new Vector3(x, 0.5f, z), new Vector3(1f, 0.14f, 1f), new Vector3(0f, 0f, 90f), metal);
            Part(PrimitiveType.Cylinder, root.transform, new Vector3(0f, 1.25f, 0f), new Vector3(0.6f, 0.3f, 0.6f), Vector3.zero, metal);
            var yoke = new GameObject("Yoke"); yoke.transform.SetParent(root.transform, false); yoke.transform.localPosition = new Vector3(0f, 1.55f, 0f);
            Part(PrimitiveType.Cylinder, yoke.transform, Vector3.zero, new Vector3(0.9f, 0.06f, 0.9f), Vector3.zero, metal);
            foreach (var x in new[] { -1f, 1f }) Part(PrimitiveType.Cube, yoke.transform, new Vector3(x, 0.55f, 0f), new Vector3(0.12f, 1.1f, 0.16f), Vector3.zero, metal);
            var drum = new GameObject("Drum"); drum.transform.SetParent(yoke.transform, false); drum.transform.localPosition = new Vector3(0f, 1f, 0f);
            Part(PrimitiveType.Cylinder, drum.transform, Vector3.zero, new Vector3(1.7f, 0.5f, 1.7f), new Vector3(90f, 0f, 0f), metal);
            Part(PrimitiveType.Cylinder, drum.transform, new Vector3(0f, 0f, 0.5f), new Vector3(1.5f, 0.02f, 1.5f), new Vector3(90f, 0f, 0f), lens);
            var glow = Part(PrimitiveType.Quad, drum.transform, new Vector3(0f, 0f, 0.7f), Vector3.one * 4.5f, Vector3.zero, glowMat); glow.name = "Lamp";
            glow.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var light = new GameObject("LampLight").AddComponent<Light>(); light.transform.SetParent(drum.transform, false); light.transform.localPosition = new Vector3(0f, 0f, 1.2f);
            light.type = LightType.Point; light.color = new Color(0.75f, 0.82f, 1f); light.intensity = 26f; light.range = 26f; light.shadows = LightShadows.None;
            root.SetActive(false); return root;
        }

        /// <summary>A lumpy, flat-bottomed sphere: one bush, or one clump of a tree crown. V runs from the ground up.</summary>
        /// <summary>A unit quad showing one cell of the leaves sheet, both sides.</summary>
        static Mesh Card(int cell)
        {
            float u0 = (cell % 2) * 0.5f, v0 = 1f - (cell / 2 + 1) * 0.5f;
            var m = new Mesh();
            m.vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f) };
            m.uv = new[] { new Vector2(u0, v0), new Vector2(u0 + 0.5f, v0), new Vector2(u0 + 0.5f, v0 + 0.5f), new Vector2(u0, v0 + 0.5f) };
            m.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back }; m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            return m;
        }

        /// <summary>Leaf cards round each blob of a bush or crown: a few at random headings and tilts, one lying nearly
        /// flat on top (the camera looks down), all in one mesh with the cutout material.</summary>
        Mesh Leaves(Transform parent, List<CombineInstance> blobParts, System.Random rng, int perBlob, float size, float spread)
        {
            if (leafMaterial == null) return null; var parts = new List<CombineInstance>();
            foreach (var b in blobParts)
            {
                // the blob's own frame: origin at its base, sx across, sy tall (the mesh runs 0..1 up)
                var c = b.transform.GetColumn(3); var centre = new Vector3(c.x, c.y, c.z); float sx = b.transform.GetColumn(0).magnitude, sy = b.transform.GetColumn(1).magnitude;
                for (int i = 0; i < perBlob; i++)
                {
                    bool top = i < perBlob / 2; float ang = (float)rng.NextDouble() * Mathf.PI * 2f; Vector3 off; float tilt, yaw;
                    if (top) { off = new Vector3(Mathf.Cos(ang) * sx * 0.35f, sy * (0.95f + (float)rng.NextDouble() * 0.15f), Mathf.Sin(ang) * sx * 0.35f); tilt = 80f + (float)rng.NextDouble() * 10f; yaw = (float)rng.NextDouble() * 360f; }   // lying on the crown
                    else { off = new Vector3(Mathf.Cos(ang) * sx * 0.55f, sy * (0.45f + (float)rng.NextDouble() * 0.35f), Mathf.Sin(ang) * sx * 0.55f); tilt = 30f + (float)rng.NextDouble() * 35f; yaw = -ang * Mathf.Rad2Deg + 90f; }   // leaning out of the side
                    float s = size * sx * (0.8f + (float)rng.NextDouble() * 0.4f);
                    parts.Add(new CombineInstance { mesh = cards[rng.Next(4)], transform = Matrix4x4.TRS(centre + off, Quaternion.Euler(tilt, yaw, (float)rng.NextDouble() * 360f), new Vector3(s, s, 1f)) });
                }
            }
            var go = new GameObject("Leaves"); go.transform.SetParent(parent, false);
            var mesh = new Mesh(); mesh.CombineMeshes(parts.ToArray(), true, true); mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh; var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = leafMaterial; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; return mesh;
        }

        static Mesh Blob(int seed)
        {
            var rng = new System.Random(seed); const int rings = 6, segs = 10;
            var lumpDir = new Vector3[5]; var lumpAmp = new float[5];
            for (int i = 0; i < 5; i++) { lumpDir[i] = new Vector3((float)rng.NextDouble() - 0.5f, (float)rng.NextDouble() - 0.2f, (float)rng.NextDouble() - 0.5f).normalized; lumpAmp[i] = 0.15f + (float)rng.NextDouble() * 0.2f; }
            var noise = new float[rings + 1, segs]; for (int r = 0; r <= rings; r++) for (int s = 0; s < segs; s++) noise[r, s] = ((float)rng.NextDouble() - 0.5f) * 0.12f;
            var verts = new Vector3[(rings + 1) * (segs + 1)]; var uvs = new Vector2[verts.Length]; var tris = new int[rings * segs * 6];
            for (int r = 0; r <= rings; r++)
            {
                float phi = Mathf.Lerp(-0.55f, Mathf.PI / 2f, r / (float)rings);
                for (int s = 0; s <= segs; s++)
                {
                    float th = s * Mathf.PI * 2f / segs; var d = new Vector3(Mathf.Cos(phi) * Mathf.Cos(th), Mathf.Sin(phi), Mathf.Cos(phi) * Mathf.Sin(th));
                    float rad = 1f + (r == rings ? noise[r, 0] : noise[r, s % segs]);
                    for (int i = 0; i < 5; i++) { float k = Mathf.Max(0f, Vector3.Dot(d, lumpDir[i])); rad += lumpAmp[i] * k * k * k; }
                    int idx = r * (segs + 1) + s; verts[idx] = d * rad; uvs[idx] = new Vector2(s / (float)segs, r / (float)rings);
                }
            }
            int t = 0;
            for (int r = 0; r < rings; r++) for (int s = 0; s < segs; s++)
            {
                int a = r * (segs + 1) + s, b = a + segs + 1;
                tris[t++] = a; tris[t++] = b; tris[t++] = a + 1; tris[t++] = a + 1; tris[t++] = b; tris[t++] = b + 1;
            }
            var mesh = new Mesh { vertices = verts, uv = uvs, triangles = tris }; mesh.RecalculateNormals();
            var n = mesh.normals; for (int r = 0; r <= rings; r++) { int a = r * (segs + 1), b = a + segs; var avg = (n[a] + n[b]).normalized; n[a] = avg; n[b] = avg; } mesh.normals = n;
            mesh.RecalculateBounds(); return mesh;
        }

        /// <summary>Pushes a ground position out of every footprint it overlaps; returns the corrected position.</summary>
        public Vector3 PushOut(Vector3 pos, float radius)
        {
            for (int pass = 0; pass < 2; pass++) foreach (var p in active)
            {
                if (p.radii.Length == 0 || p.drivable) continue; float reach = p.bound + 6f; if ((p.pos - pos).sqrMagnitude > reach * reach) continue;
                for (int c = 0; c < p.radii.Length; c++)
                {
                    var d = new Vector2(pos.x, pos.z) - p.circleCenters[c]; float min = p.radii[c] + radius; float sq = d.sqrMagnitude;
                    if (sq < min * min && sq > 1e-4f) { float len = Mathf.Sqrt(sq); var push = d / len * (min - len); pos.x += push.x; pos.z += push.y; }
                }
            }
            return pos;
        }

        /// <summary>True when a vehicle of this radius could stand at the point without overlapping anything solid.</summary>
        public bool Free(Vector3 pos, float radius)
        {
            foreach (var p in active)
            {
                if (p.radii.Length == 0 || p.drivable) continue; float reach = p.bound + 6f; if ((p.pos - pos).sqrMagnitude > reach * reach) continue;
                for (int c = 0; c < p.radii.Length; c++) { float min = p.radii[c] + radius; if ((new Vector2(pos.x, pos.z) - p.circleCenters[c]).sqrMagnitude < min * min) return false; }
            }
            return true;
        }

        /// <summary>Sandbag positions ahead of a point: where an anti-tank gun would dig in.</summary>
        public List<Vector3> Nests(Vector3 from, Vector3 dir, float min, float max)
        {
            var list = new List<Vector3>();
            foreach (var p in active)
            {
                if (p.what != What.Model || p.kind.mesh != "sandbags" || p.state != 0) continue;
                var d = p.pos - from; d.y = 0f; float len = d.magnitude; if (len < min || len > max) continue;
                if (Vector3.Dot(d / len, dir) > 0.35f) list.Add(p.pos);
            }
            return list;
        }

        /// <summary>Searchlight posts ahead of a point: where a flak gun would stand.</summary>
        public List<Vector3> Posts(Vector3 from, Vector3 dir, float min, float max)
        {
            var list = new List<Vector3>();
            foreach (var p in active)
            {
                if (p.what != What.Searchlight) continue;
                var d = p.pos - from; d.y = 0f; float len = d.magnitude; if (len < min || len > max) continue;
                if (Vector3.Dot(d / len, dir) > 0.35f) list.Add(p.pos);
            }
            return list;
        }

        /// <summary>A fresh shell crater on the ground; the field keeps the last forty.</summary>
        /// <summary>Burnt ground under a wreck, tied to it so it goes when the wreck does.</summary>
        public void Scorch(Transform wreck, float size)
        {
            var q = Quad(transform, wreck.position, Random.value * 360f, size, size, scorchMaterial, 0.075f); q.name = "Scorch"; q.transform.SetParent(wreck, true);
        }
        Material scorchMaterial;

        public void Crater(Vector3 pos, float size)
        {
            GameObject q;
            if (craters.Count < 40) { q = Quad(transform, pos, Random.value * 360f, size, size, craterMaterial, 0.07f); q.name = "ShellCrater"; craters.Add(q); }
            else { q = craters[nextCrater]; nextCrater = (nextCrater + 1) % craters.Count; q.transform.position = new Vector3(pos.x, 0.07f, pos.z); q.transform.rotation = Quaternion.Euler(90f, Random.value * 360f, 0f); q.transform.localScale = new Vector3(size, size, 1f); }
        }

        /// <summary>True when a shell at this point is inside something solid that is taller than the shell's flight.</summary>
        /// <summary>Within a metre of a hedge bank: cover from shells coming across it.</summary>
        public bool InCover(Vector3 pos)
        {
            foreach (var p in active)
            {
                if (p.what != What.Hedge || p.radii.Length == 0) continue; float reach = p.bound + 4f; if ((p.pos - pos).sqrMagnitude > reach * reach) continue;
                for (int c = 0; c < p.radii.Length; c++) { float r = p.radii[c] + 1.2f; if ((new Vector2(pos.x, pos.z) - p.circleCenters[c]).sqrMagnitude < r * r) return true; }
            }
            return false;
        }


        /// <summary>How a prop gives way: 0 never, 1 it goes over (trees, poles), 2 any tank crushes it, 3 explosions bring
        /// it down, 4 explosions or a dozer blade.</summary>
        static int BreakKind(Prop p)
        {
            if (p.what == What.Tree) return 1;
            if (p.what == What.Hedge) return 4;
            if (p.what != What.Model) return 0;
            switch (p.kind.mesh)
            {
                case "tree_oak": case "tree_poplar": case "deadtree": case "spruce_snow": case "k_birches": case "pole": case "signpost": return 1;
                case "k_wattle": case "cart": case "barrels": case "haystack": case "k_sheaves": case "gate": case "k_well": case "k_sunflowers": return 2;
                case "farmhouse": case "cottage": case "barn": case "church": case "k_khata": case "k_izba": case "k_church": case "bunker": case "truck": return 3;
                case "wall_a": case "wall_b": case "sandbags": case "well": return 4;
                default: return 0;
            }
        }
        static float MaxHp(string mesh) { switch (mesh) { case "church": return 12f; case "k_church": return 9f; case "bunker": return 8f; case "farmhouse": return 6f; case "cottage": case "barn": case "k_izba": return 4f; case "k_khata": return 3f; case "truck": case "sandbags": return 1.5f; default: return 2f; } }
        static bool Burns(string mesh) => mesh == "barn" || mesh == "cottage" || mesh == "k_khata" || mesh == "k_izba" || mesh == "truck" || mesh == "haystack" || mesh == "k_sheaves" || mesh == "cart";
        static Vector3 FallDir(Prop p) => new Vector3(Mathf.Sin(p.fallYaw), 0f, Mathf.Cos(p.fallYaw));
        static Quaternion Fallen(Prop p) => Quaternion.AngleAxis(88f, Vector3.Cross(Vector3.up, FallDir(p))) * Quaternion.Euler(0f, p.yaw * Mathf.Rad2Deg, 0f);

        /// <summary>A hull against the country: trees and poles go over the way it drives, fences, carts and hay are
        /// crushed; with a dozer blade the walls, sandbags and hedges give way too.</summary>
        public void Ram(Vector3 pos, float radius, Vector3 forward, bool dozer)
        {
            for (int i = 0; i < active.Count; i++)
            {
                var p = active[i]; if (p.radii.Length == 0 || p.drivable || p.state != 0) continue;
                float reach = p.bound + radius + 1f; if ((p.pos - pos).sqrMagnitude > reach * reach) continue;
                int kind = BreakKind(p); if (kind == 0 || kind == 3 || (kind == 4 && !dozer)) continue;
                for (int c = 0; c < p.radii.Length; c++)
                {
                    float min = p.radii[c] + radius * 0.97f; var cc = p.circleCenters[c];   // the hull is pushed back out every frame: only a hull driving in reaches this
                    if ((new Vector2(pos.x, pos.z) - cc).sqrMagnitude >= min * min) continue;
                    if (p.what == What.Hedge) Gap(p, new Vector3(cc.x, 0f, cc.y)); else if (kind == 1) Fall(p, forward); else Crush(p, false);
                    break;
                }
            }
        }

        /// <summary>An explosion among the props: buildings and walls in reach lose hit points, trees go over away from
        /// it, fences and hay are flattened, fuel goes up, a heavy one opens a gap in a hedge.</summary>
        public void Blast(Vector3 at, float radius, float dmg)
        {
            for (int i = 0; i < active.Count; i++)
            {
                var p = active[i]; if (p.state != 0) continue;
                float reach = p.bound + radius; var d = p.pos - at; d.y = 0f; if (d.sqrMagnitude > reach * reach) continue;
                float near = p.radii.Length == 0 ? d.magnitude : float.MaxValue;
                for (int c = 0; c < p.radii.Length; c++) near = Mathf.Min(near, (new Vector2(at.x, at.z) - p.circleCenters[c]).magnitude - p.radii[c]);
                if (near > radius) continue;
                float k = 1f - Mathf.Clamp01(near / radius) * 0.6f; int kind = BreakKind(p);   // full at contact, less at the edge
                if (p.what == What.Hedge) { if (dmg >= 1.5f) Gap(p, at); }
                else if (kind == 1) { if (dmg * k >= 0.8f) Fall(p, d.sqrMagnitude > 0.01f ? d.normalized : Vector3.forward); }
                else if (kind == 2) { if (p.kind.mesh == "barrels") Explode(p); else Crush(p, true); }
                else if (kind >= 3) Hurt(p, dmg * k, at);
            }
        }

        /// <summary>A shell stopped by whatever Blocks found: a building or a wall loses hit points, a cart or hay is
        /// knocked flat, fuel drums go up.</summary>
        public void Strike(Vector3 at, float dmg)
        {
            var p = blocker; blocker = null; if (p == null || p.state != 0) return;
            int kind = BreakKind(p);
            if (p.what == What.Model && p.kind.mesh == "barrels") Explode(p);
            else if (kind == 2) Crush(p, true);
            else if (kind >= 3) Hurt(p, dmg, at);
        }

        void Hurt(Prop p, float dmg, Vector3 at)
        {
            if (p.what != What.Model || p.state != 0) return;
            if (p.hp < 0f) p.hp = MaxHp(p.kind.mesh);
            p.hp -= dmg; if (fx != null) fx.Dust(new Vector3(at.x, 0.4f, at.z));   // plaster and splinters off the wall
            if (p.hp > 0f) return;
            if (p.kind.mesh == "truck") Explode(p); else Collapse(p);
        }

        void Fall(Prop p, Vector3 dir)
        {
            if (p.state != 0) return;
            p.state = 1; p.radii = new float[0]; p.circleCenters = new Vector2[0]; p.fallYaw = Mathf.Atan2(dir.x, dir.z);
            if (p.go != null) falling.Add(new Falling { p = p, rot0 = p.go.transform.rotation, axis = Vector3.Cross(Vector3.up, FallDir(p)) });
        }

        void Crush(Prop p, bool byShell)
        {
            if (p.state != 0) return;
            p.state = 2; p.radii = new float[0]; p.circleCenters = new Vector2[0]; p.height = -1f;
            if (byShell && Burns(p.kind.mesh)) p.burn = 12f + Random.value * 10f;
            if (p.go != null) p.go.transform.localScale = new Vector3(1f, 0.22f, 1f);
            if (fx != null) fx.Dust(p.pos);
        }

        /// <summary>A building coming down: it sinks into its own dust and a heap of rubble is left in its place.</summary>
        void Collapse(Prop p)
        {
            p.state = 3; p.drivable = true; p.height = 1.2f; if (Burns(p.kind.mesh) || Random.value < 0.35f) p.burn = 18f + Random.value * 14f;
            if (fx != null) fx.Collapse(p.pos, p.kind.length);
            Sfx.Explosion(p.pos);
            if (p.go != null) StartCoroutine(Sink(p, p.go)); 
        }

        System.Collections.IEnumerator Sink(Prop p, GameObject old)
        {
            float h = Mathf.Max(3f, p.kind.height); var start = old.transform.position; var rot0 = old.transform.rotation; var tilt = Quaternion.Euler(Random.Range(-9f, 9f), 0f, Random.Range(-9f, 9f)) * rot0;
            for (float a = 0f; a < 1f; a += Time.deltaTime / 0.9f)
            {
                if (old == null) yield break;   // its cell was unloaded: the ruin is built when it comes back
                old.transform.position = start - Vector3.up * (h * 0.85f * a * a); old.transform.rotation = Quaternion.Slerp(rot0, tilt, a); yield return null;
            }
            if (old == null || p.go != old) yield break;
            Destroy(old); p.go = null; SpawnRuin(p);
        }

        /// <summary>A truck or fuel drums going up: a fireball, a fire that burns on, and whatever stands next to it hit.</summary>
        void Explode(Prop p)
        {
            if (p.state != 0) return;
            p.state = 3; p.drivable = true; p.height = -1f; p.burn = 20f + Random.value * 15f;
            if (fx != null) fx.Explosion(p.pos + Vector3.up);
            Sfx.Explosion(p.pos);
            if (p.go != null) { Unload(p); Spawn(p); }
            Blast(p.pos, 5f, 1.5f);
        }

        /// <summary>A hedge opened where the blast or the blade struck it; the gap stays for the night.</summary>
        void Gap(Prop h, Vector3 at)
        {
            var dir = (h.b - h.a).normalized; float u = Vector3.Dot(at - h.a, dir); if (u < 0f || u > h.size) return;
            foreach (var g in h.gaps) if (Mathf.Abs(g - u) < 4f) return;
            var list = new List<float>(h.gaps) { u }; h.gaps = list.ToArray();
            var centers = new List<Vector2>(); var radii = new List<float>();
            for (float s = 1.25f; s < h.size; s += 2.5f) if (!Open(h, s)) { var c = h.a + dir * s; centers.Add(new Vector2(c.x, c.z)); radii.Add(1.6f); }
            h.circleCenters = centers.ToArray(); h.radii = radii.ToArray();
            if (fx != null) { fx.Dust(h.a + dir * u); fx.Dust(h.a + dir * (u + 2f)); }
            if (h.go != null) { Unload(h); Spawn(h); }
        }

        Material Sooty(string mesh)
        {
            if (sooty.TryGetValue(mesh, out var m)) return m;
            m = new Material(materials[mesh]); var c = m.GetColor("_BaseColor"); m.SetColor("_BaseColor", new Color(c.r * 0.72f, c.g * 0.7f, c.b * 0.67f, 1f)); sooty[mesh] = m; return m;
        }
        Material Burnt()
        {
            if (burntMaterial == null) { burntMaterial = new Material(Resources.Load<Material>("VehicleLit")); burntMaterial.SetColor("_BaseColor", new Color(0.1f, 0.09f, 0.08f)); burntMaterial.SetFloat("_Smoothness", 0.08f); }
            return burntMaterial;
        }
        static GameObject Piece(Transform parent, PrimitiveType shape, Vector3 localPos, Quaternion localRot, Vector3 size, Material m)
        {
            var g = GameObject.CreatePrimitive(shape); Destroy(g.GetComponent<Collider>()); g.transform.SetParent(parent, false);
            g.transform.localPosition = localPos; g.transform.localRotation = localRot; g.transform.localScale = size;
            var r = g.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; return g;
        }

        /// <summary>What is left: a ruin model when there is one (<mesh>_ruin), else a heap of the building's own stones and
        /// beams, sooted, with stubs of wall still standing; a truck or a cart is a burnt husk, fuel drums leave
        /// scorched ground. A ruin that burns carries its own light.</summary>
        void SpawnRuin(Prop p)
        {
            float yawDeg = p.yaw * Mathf.Rad2Deg; var mesh = p.kind.mesh; float L = p.kind.length, W = 2f;
            foreach (var r in p.radii) W = Mathf.Max(W, r * 2f);
            var go = new GameObject("Ruin " + mesh); go.transform.SetParent(transform, false); go.transform.position = p.pos; go.transform.rotation = Quaternion.Euler(0f, yawDeg, 0f); p.go = go;
            Quad(go.transform, p.pos, yawDeg, Mathf.Max(L, W) * 1.3f, Mathf.Max(L, W) * 1.3f, scorchMaterial, 0.06f);
            var rng = new System.Random(p.seed * 7919 + 17); float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            if (mesh == "truck" || mesh == "cart")
            {
                var husk = Instantiate(prefabs[mesh], go.transform); husk.transform.localPosition = new Vector3(0f, -0.12f, 0f); husk.transform.localRotation = Quaternion.Euler(R(-4f, 4f), 0f, R(-6f, 6f));
                foreach (var r in husk.GetComponentsInChildren<Renderer>()) { r.sharedMaterial = Burnt(); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
            }
            else if (mesh != "barrels" && prefabs.ContainsKey(mesh + "_ruin"))
            {
                var ruin = Instantiate(prefabs[mesh + "_ruin"], go.transform);
                foreach (var r in ruin.GetComponentsInChildren<Renderer>()) { r.sharedMaterial = materials[mesh + "_ruin"]; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
            }
            else if (mesh != "barrels")
            {
                var mat = Sooty(mesh); float h = Mathf.Clamp(p.kind.height * 0.2f, 0.6f, 1.6f); bool big = L > 7f;
                // the building itself fallen in: its roof and walls flattened into a low heap, tilted the way it came down
                var fallen = Instantiate(prefabs[mesh], go.transform); fallen.name = "Fallen";
                fallen.transform.localPosition = new Vector3(R(-0.3f, 0.3f), -0.05f, R(-0.3f, 0.3f)); fallen.transform.localRotation = Quaternion.Euler(R(-5f, 5f), R(-10f, 10f), R(-5f, 5f)); fallen.transform.localScale = new Vector3(0.95f, 0.16f, 0.95f);
                foreach (var r in fallen.GetComponentsInChildren<Renderer>()) { r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
                // stones, plaster, soot and tile thrown over it and round it, each its own shade
                var tints = new[] { new Color(0.66f, 0.63f, 0.58f), new Color(0.78f, 0.74f, 0.66f), new Color(0.32f, 0.3f, 0.28f), new Color(0.55f, 0.36f, 0.28f) };
                var block = new MaterialPropertyBlock(); int chunks = big ? 24 : 12;
                for (int i = 0; i < chunks; i++)
                {
                    float s = R(0.25f, 0.85f);
                    var c = Piece(go.transform, PrimitiveType.Cube, new Vector3(R(-L, L) * 0.5f, R(0.05f, h), R(-W, W) * 0.5f), Quaternion.Euler(R(0f, 360f), R(0f, 360f), R(0f, 360f)), new Vector3(s, s * R(0.4f, 0.9f), s * R(0.6f, 1.2f)), mat);
                    block.SetColor("_BaseColor", tints[rng.Next(tints.Length)]); c.GetComponent<Renderer>().SetPropertyBlock(block);
                }
                for (int i = 0; i < (big ? 6 : 3); i++) Piece(go.transform, PrimitiveType.Cube, new Vector3(R(-L, L) * 0.35f, R(0.4f, h + 0.4f), R(-W, W) * 0.35f), Quaternion.Euler(R(12f, 42f), R(0f, 360f), 0f), new Vector3(0.18f, 0.18f, L * R(0.3f, 0.5f)), trunkMaterial);   // roof beams sticking out
                if (mesh != "sandbags" && mesh != "well")
                {
                    int stubs = big ? 3 : 1;   // stubs of wall still standing round the heap
                    for (int i = 0; i < stubs; i++)
                    {
                        bool side = rng.Next(2) == 0; float along = R(-0.35f, 0.35f), sh = R(1.2f, Mathf.Max(1.4f, p.kind.height * 0.45f)), sw = R(1.8f, 3.4f);
                        var at = side ? new Vector3((rng.Next(2) == 0 ? -1f : 1f) * W * 0.44f, sh * 0.5f - 0.1f, along * L) : new Vector3(along * W, sh * 0.5f - 0.1f, (rng.Next(2) == 0 ? -1f : 1f) * L * 0.44f);
                        Piece(go.transform, PrimitiveType.Cube, at, Quaternion.Euler(R(-4f, 4f), side ? 0f : 90f, R(-4f, 4f)), new Vector3(0.35f, sh, sw), mat);
                    }
                }
            }
            if (p.burn > 0f)
            {
                var lg = new GameObject("RuinFire"); lg.transform.SetParent(go.transform, false); lg.transform.localPosition = Vector3.up * 2.5f;
                p.glow = lg.AddComponent<Light>(); p.glow.type = LightType.Point; p.glow.color = new Color(1f, 0.55f, 0.22f); p.glow.range = 16f; p.glow.intensity = 4f; p.glow.shadows = LightShadows.None;
            }
        }

        public bool Blocks(Vector3 pos)
        {
            foreach (var p in active)
            {
                if (p.radii.Length == 0 || pos.y > p.height) continue; float reach = p.bound + 4f; if ((p.pos - pos).sqrMagnitude > reach * reach) continue;
                for (int c = 0; c < p.radii.Length; c++) if ((new Vector2(pos.x, pos.z) - p.circleCenters[c]).sqrMagnitude < p.radii[c] * p.radii[c]) { blocker = p; return true; }
            }
            return false;
        }
    }
}

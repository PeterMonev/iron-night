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
        enum What { Model, Lane, Hedge, Tree, Decal, Searchlight, Fire, Stream, Forest, Abatis, Logs }
        class Prop
        {
            public What what; public Kind kind; public Vector3 pos; public float yaw, size, bound = 8f, height = -1f; public int seed;
            public Vector2[] circleCenters = new Vector2[0]; public float[] radii = new float[0];
            public Vector3 a, b; public float[] gaps; public float clearA, clearB;              // hedges: the line and its openings
            public GameObject go; public Mesh mesh, leaves; public float az, el = 42f; public LightShaft shaft; public Transform yoke, drum, lamp; public float flakTimer = 4f; public int burst; public Light glow;
            public int state; public float hp = -1f, fallYaw, burn; public bool drivable;   // 0 standing, 1 knocked over, 2 crushed, 3 ruined; a ruin is driven over
            public Vector4[] firs; public float[] down; public int[] treeOf; public Vector3[] stumps; public HashSet<int> lying3D;   // lying3D: the felled firs lying as the model, not the card   // a forest: its firs (x, z, size, turn), each one's fall (-1 standing, else the way it lies), which fir each circle is; the stumps in its clearings
            public bool manned;   // trenches and nests: their garrison has been put in (or never will be: the player's own sandbags)
            public List<Vector3> trodden;   // hedges: where hulls went through, as (along the line, half the width pressed flat, the side it lies to)
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
            // the fifth batch: houses, a landmark, the field works and the water
            new Kind { mesh = "house_normandy", length = 11f, height = 8f, circles = new[] { -2.8f, 0f, 3.2f, 2.8f, 0f, 3.2f } },
            new Kind { mesh = "house_ruin", length = 11f, height = 7f, circles = new[] { -2.8f, 0f, 3.2f, 2.8f, 0f, 3.2f } },
            new Kind { mesh = "izba", length = 10f, height = 6f, circles = new[] { -2.6f, 0f, 3.2f, 2.6f, 0f, 3.2f } },
            new Kind { mesh = "windmill", length = 6f, height = 13f, circles = new[] { 0f, 0f, 2.6f } },
            new Kind { mesh = "well_b", length = 2.6f, height = 3f, circles = new[] { 0f, 0f, 1.3f } },
            new Kind { mesh = "truck_burnt", length = 6.5f, height = 2.6f, circles = new[] { -1.7f, 0f, 1.4f, 1.7f, 0f, 1.4f } },
            new Kind { mesh = "barbed_wire", length = 6f, height = -1f, circles = new[] { 0f, 0f, 0.8f } },   // a hull flattens it
            new Kind { mesh = "trench", length = 8f, height = -1f, circles = new[] { -3f, 0f, 0.9f, -1f, 0f, 0.9f, 1f, 0f, 0.9f, 3f, 0f, 0.9f } },   // caved in by the first hull across it
            new Kind { mesh = "bridge_stone", length = 16f, height = -1f, circles = new float[0] },
            new Kind { mesh = "bridge_wood", length = 14f, height = -1f, circles = new float[0] },
            new Kind { mesh = "tree_apple", length = 6f, height = -1f, circles = new[] { 0f, 0f, 0.5f } },
            new Kind { mesh = "tree_birch", length = 5f, height = -1f, circles = new[] { 0f, 0f, 0.4f } },
            new Kind { mesh = "tree_poplar_b", length = 4f, height = -1f, circles = new[] { 0f, 0f, 0.45f } },
            // the sixth batch: the Ardennes
            new Kind { mesh = "house_belgian", length = 10f, height = 8f, circles = new[] { -2.4f, 0f, 3.1f, 2.4f, 0f, 3.1f } },
            new Kind { mesh = "farm_belgian", length = 15f, height = 7f, circles = new[] { -5f, 0f, 3.2f, 0f, 0f, 3.4f, 5f, 0f, 3.2f } },
            new Kind { mesh = "sawmill", length = 10f, height = 4.5f, circles = new[] { -2.5f, 0f, 2.6f, 2.5f, 0f, 2.6f } },
            new Kind { mesh = "foxhole_logs", length = 5f, height = -1f, circles = new[] { 0f, 0f, 1.4f } },
            new Kind { mesh = "truck_snow", length = 6.5f, height = 2.8f, circles = new[] { -1.7f, 0f, 1.4f, 1.7f, 0f, 1.4f } },
            new Kind { mesh = "chapel_wayside", length = 4.5f, height = 6f, circles = new[] { 0f, 0f, 2f } },
            new Kind { mesh = "fir_snow", length = 6f, height = -1f, circles = new[] { 0f, 0f, 0.5f } },
            new Kind { mesh = "fir_snow_b", length = 6f, height = -1f, circles = new[] { 0f, 0f, 0.5f } },
            new Kind { mesh = "pine_snow", length = 6f, height = -1f, circles = new[] { 0f, 0f, 0.45f } },
            new Kind { mesh = "fir_young", length = 3f, height = -1f, circles = new[] { 0f, 0f, 0.6f } },
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
        public Vector3 platoon;          // where the leader is
        readonly HashSet<int> deadLamps = new HashSet<int>();   // posts shot out tonight, by seed
        Material puddleMaterial;

        public void Build(Camera camera)
        {
            cam = camera.transform; LightShaft.cam = camera; string sn = winter ? "_snow" : Kursk ? "_kursk" : "";
            var lit = Resources.Load<Material>("VehicleLit"); var decal = Resources.Load<Material>("GroundDecal");
            foreach (var k in Kinds)
            {
                if (k.mesh.StartsWith("k_") && !Kursk && k.mesh != "k_hedgehogs") continue;
                if (!Ardennes && ArdennesOnly(k.mesh)) continue;   // the Belgian houses and the snowy trees stay on disk elsewhere   // the hedgehogs are loaded on every front: a last stand digs them in anywhere
                var pf = Resources.Load<GameObject>("Props/" + k.mesh); if (pf == null) continue;
                prefabs[k.mesh] = pf;
                var m = new Material(lit); m.SetTexture("_BaseMap", Resources.Load<Texture2D>("Props/" + k.mesh + "_tex")); m.SetColor("_BaseColor", Tint(k.mesh)); m.SetFloat("_Smoothness", 0.15f); m.SetFloat("_Cull", 0f);
                materials[k.mesh] = m;
                string ruinOf = k.mesh == "house_normandy" ? "house_ruin" : k.mesh + "_ruin";   // the Normandy house shelled is the ruined one
                var rp = Resources.Load<GameObject>("Props/" + ruinOf);   // what it looks like knocked down, when there is a model of that
                if (rp != null) { prefabs[k.mesh + "_ruin"] = rp; var rm = new Material(lit); rm.SetTexture("_BaseMap", Resources.Load<Texture2D>("Props/" + ruinOf + "_tex")); rm.SetColor("_BaseColor", Tint(k.mesh)); rm.SetFloat("_Smoothness", 0.15f); rm.SetFloat("_Cull", 0f); materials[k.mesh + "_ruin"] = rm; }
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
            waterMaterial = new Material(lit); waterMaterial.SetColor("_BaseColor", winter ? new Color(0.5f, 0.55f, 0.6f) : new Color(0.05f, 0.07f, 0.08f)); waterMaterial.SetFloat("_Smoothness", winter ? 0.62f : 0.7f); waterMaterial.SetFloat("_Metallic", 0f);   // still dark water with a soft sheen: smoother, and the flare over the platoon showed in it as a second sun; ice in the Ardennes
            bankMaterial = new Material(laneMaterial); bankMaterial.SetTextureScale("_BaseMap", new Vector2(1f, 1f)); bankMaterial.renderQueue = 2441;
            { string hm = Kursk ? "k_wattle" : "hedge"; if (prefabs.ContainsKey(hm) && PlayerPrefs.GetInt("quality", 1) != 0) Slices(hm, out _); }   // the hedge cut into its slices now, while the night loads
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
        public static bool SneakNight;   // a night raid: more flak posts near the depot (their beams still keep to the sky)
        public static string Route = "open";   // "village", "open" or "bocage": set before the night is built
        static float FarmChance => Route == "village" ? 0.17f : Route == "bocage" ? 0.08f : 0.07f;
        static float VillageChance => Route == "village" ? 0.24f : Route == "bocage" ? 0.03f : 0.04f;
        static float HedgeBias => (Route == "bocage" ? 1.45f : Route == "open" ? 0.5f : 0.85f) * (Kursk ? 0.6f : 1f);   // the steppe is open country
        static float TreeBias => Route == "bocage" ? 1.8f : Route == "open" ? 0.6f : 1f;
        static bool Farm(int ix, int iz) => (ix == 1 && iz == 1) || (!Start(ix, iz) && Rnd(ix, iz, 930) < FarmChance);
        static bool Battery(int ix, int iz) => (ix == -1 && iz == 0) || (!Start(ix, iz) && !Farm(ix, iz) && Rnd(ix, iz, 940) < (SneakNight ? 0.32f : 0.16f));
        static bool Village(int ix, int iz) => (ix == 0 && iz == 3) || (!Start(ix, iz) && !Farm(ix, iz) && !Battery(ix, iz) && Rnd(ix, iz, 1100) < VillageChance);
        static bool Ardennes => Theatre == "ardennes";
        static bool ArdennesOnly(string mesh) => mesh == "house_belgian" || mesh == "farm_belgian" || mesh == "sawmill" || mesh == "foxhole_logs" || mesh == "truck_snow" || mesh == "chapel_wayside" || mesh.StartsWith("fir_") || mesh == "pine_snow";
        static float ForestChance => Route == "bocage" ? 0.62f : Route == "open" ? 0.38f : 0.3f;
        /// <summary>A cell of fir forest: the Ardennes only, never where the night starts, a farm, a post or a village stands.</summary>
        static bool Forest(int ix, int iz) => Ardennes && !Start(ix, iz) && !(ix == 0 && iz == 1) && !Farm(ix, iz) && !Battery(ix, iz) && !Village(ix, iz) && Rnd(ix, iz, 1400) < ForestChance;
        static bool HedgeX(int ix, int iz) { if (LaneX(ix) || Ardennes) return false; if (Farm(ix, iz) || Farm(ix + 1, iz)) return true; return Rnd(ix, iz, 920) < Mathf.Min(0.97f, (FieldType(ix, iz) != FieldType(ix + 1, iz) ? 0.85f : 0.3f) * HedgeBias); }
        static bool HedgeZ(int ix, int iz) { if (LaneZ(iz) || StreamZ(iz) || Ardennes) return false; if (Farm(ix, iz) || Farm(ix, iz + 1)) return true; return Rnd(ix, iz, 921) < Mathf.Min(0.97f, (FieldType(ix, iz) != FieldType(ix, iz + 1) ? 0.85f : 0.3f) * HedgeBias); }
        // a stream now and then along a line z = iz*40+20 where no lane runs, winding a few metres either side of it
        static bool StreamZ(int iz) => iz != 0 && iz != -1 && !LaneZ(iz) && Hash(3, iz, 950) % 6 == 0;
        static float StreamAt(int iz, float x) => iz * Cell + Half + Mathf.Sin(x * 0.045f + iz) * 5f + Mathf.Sin(x * 0.11f + iz * 2.3f) * 2f;
        const float StreamHalf = 3.5f;
        /// <summary>How far a model's footprint reaches from its centre, whichever way it is turned.</summary>
        static float Reach(Kind k)
        {
            float r = 0f; for (int c = 0; c + 2 < k.circles.Length; c += 3) r = Mathf.Max(r, Mathf.Sqrt(k.circles[c] * k.circles[c] + k.circles[c + 1] * k.circles[c + 1]) + k.circles[c + 2]);
            return r;
        }

        /// <summary>A field with a fire fight in it: about one in eight of the open fields (not where the night starts, a
        /// farm, a post, a village or a forest).</summary>
        static bool FightCell(int ix, int iz)
        {
            if (Start(ix, iz) || (ix == 0 && iz == 1) || (ix == 1 && iz == 1) || Battery(ix, iz)) return false;
            float p = Village(ix, iz) ? 0.5f : Farm(ix, iz) ? 0.4f : Forest(ix, iz) ? 0.3f : TrenchCell(ix, iz) ? 0.6f : 0.04f;   // where the fighting was: houses, yards, woods, trenches; seldom the open field
            return Rnd(ix, iz, 1800) < p;
        }
        /// <summary>A field with a trench line across it (TrenchLine).</summary>
        static bool TrenchCell(int ix, int iz) => !Start(ix, iz) && !Farm(ix, iz) && !Battery(ix, iz) && !Village(ix, iz) && !Forest(ix, iz) && Rnd(ix, iz, 1240) < (Kursk ? 0.14f : Route == "open" ? 0.08f : 0.05f);

        /// <summary>The fire fights in the fields within reach of a point: where ours lie, where theirs do, and the field's
        /// key; ours and theirs some 26 m apart across the field, clear of the lanes.</summary>
        public List<(Vector3 ours, Vector3 theirs, long key, bool trench)> FightSpots(Vector3 from, float max)
        {
            var list = new List<(Vector3, Vector3, long, bool)>(); int cx = Mathf.RoundToInt(from.x / Cell), cz = Mathf.RoundToInt(from.z / Cell), n = Mathf.CeilToInt(max / Cell);
            for (int ix = cx - n; ix <= cx + n; ix++) for (int iz = cz - n; iz <= cz + n; iz++)
            {
                if (!FightCell(ix, iz)) continue;
                var c = new Vector3(ix * Cell, 0f, iz * Cell); if ((c - from).magnitude > max) continue;
                Vector3 ours, theirs; bool trench = TrenchCell(ix, iz);
                if (trench)
                {
                    // the trench line as TrenchLine lays it: theirs in it and just behind, ours coming from the wire side
                    float yaw = Rnd(ix, iz, 1241) * Mathf.PI; var f = new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw)); var r = new Vector3(f.z, 0f, -f.x); var o = c + In(ix, iz, 1242, 4f);
                    theirs = o - r * 2.5f; ours = o + r * 28f;
                }
                else
                {
                    float a = Rnd(ix, iz, 1801) * 6.283f; var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                    ours = c + In(ix, iz, 1802, 4f) - dir * 15f; theirs = ours + dir * 30f;
                    if (!Village(ix, iz) && !Farm(ix, iz) && (OnLane(ours, 2f) || OnLane(theirs, 2f))) continue;   // in a village the lanes run between the houses
                }
                if (InStream(ours, 2f) || InStream(theirs, 2f)) continue;
                list.Add((ours, theirs, ((long)ix << 32) ^ (uint)iz, trench));
            }
            return list;
        }

        /// <summary>True when something reaching this far from a point would lie on a lane (three metres either side of its line).</summary>
        static bool OnLane(Vector3 pos, float reach)
        {
            if (reach <= 0f) return false; float clear = 3f + reach;
            int ix = Mathf.RoundToInt((pos.x - Half) / Cell), iz = Mathf.RoundToInt((pos.z - Half) / Cell);
            for (int k = ix - 1; k <= ix + 1; k++) if (LaneX(k) && Mathf.Abs(pos.x - (k * Cell + Half)) < clear) return true;
            for (int k = iz - 1; k <= iz + 1; k++) if (LaneZ(k) && Mathf.Abs(pos.z - (k * Cell + Half)) < clear) return true;
            return false;
        }

        /// <summary>True within the water (and the margin round it) of a stream.</summary>
        public static bool InStream(Vector3 pos, float margin)
        {
            int iz = Mathf.RoundToInt((pos.z - Half) / Cell);
            for (int k = iz - 1; k <= iz + 1; k++) if (StreamZ(k) && Mathf.Abs(pos.z - StreamAt(k, pos.x)) < StreamHalf + margin) return true;
            return false;
        }
        static Vector3 In(int ix, int iz, int salt, float r) => new Vector3((Rnd(ix, iz, salt) - 0.5f) * 2f * r, 0f, (Rnd(ix, iz, salt + 1) - 0.5f) * 2f * r);

        // the generated textures come out at different brightnesses; this evens them under the moon
        static Color Tint(string mesh) { switch (mesh) { case "tree_poplar": case "tree_poplar_b": case "tree_apple": case "tree_birch": case "tree_oak": case "spruce_snow": case "hedge": return new Color(0.42f, 0.5f, 0.4f); case "deadtree": return new Color(0.78f, 0.72f, 0.66f); case "haystack": return new Color(0.82f, 0.76f, 0.6f); case "k_birches": return new Color(0.58f, 0.64f, 0.54f); case "k_sunflowers": return new Color(0.7f, 0.68f, 0.56f); case "k_sheaves": return new Color(0.8f, 0.74f, 0.6f); case "sandbags": return new Color(0.78f, 0.74f, 0.66f); default: return new Color(0.8f, 0.78f, 0.74f); } }

        Kind K(string mesh) { foreach (var k in Kinds) if (k.mesh == mesh) return k; return null; }

        /// <summary>What stands in for a Normandy model on the steppe; null when it has none (the gates).</summary>
        static string Local(string mesh)
        {
            if (Ardennes)
                switch (mesh) { case "farmhouse": return "farm_belgian"; case "cottage": case "house_normandy": return "house_belgian"; case "truck": return "truck_snow"; default: return mesh; }
            if (!Kursk) return mesh;
            switch (mesh)
            {
                case "farmhouse": return "k_izba"; case "cottage": return "k_khata"; case "church": return "k_church"; case "well": return "k_well";
                case "wall_a": case "wall_b": return "k_wattle"; case "gate": return null;
                case "tree_oak": case "tree_poplar": case "tree_poplar_b": case "tree_apple": return "k_birches"; case "haystack": return "k_sheaves";
                case "house_normandy": return "izba"; case "well_b": return "k_well";
                default: return mesh;
            }
        }

        Prop Place(List<Prop> list, string mesh, Vector3 pos, float yaw, bool water = false)
        {
            string local = Local(mesh); if (local == null) return null;
            if (Ardennes && !prefabs.ContainsKey(local) && prefabs.ContainsKey(mesh)) local = mesh;   // the Belgian model missing: the Norman one stands in
            mesh = local; var kind = K(mesh); if (kind == null || !prefabs.ContainsKey(mesh)) return null;
            if (!water && InStream(pos, kind.length * 0.35f)) return null;   // nothing stands in a stream but its bridge
            if (!water && OnLane(pos, Reach(kind))) return null;   // nor on a lane
            foreach (var o in list) if (o.what == What.Model && (o.pos - pos).sqrMagnitude < 5f * 5f) return null;
            var p = new Prop { what = What.Model, kind = kind, pos = pos, yaw = yaw, height = kind.height, bound = kind.length };
            int n = kind.circles.Length / 3; p.circleCenters = new Vector2[n]; p.radii = new float[n];
            var f = new Vector2(Mathf.Sin(yaw), Mathf.Cos(yaw)); var r = new Vector2(f.y, -f.x);
            for (int c = 0; c < n; c++) { p.circleCenters[c] = new Vector2(pos.x, pos.z) + f * kind.circles[c * 3] + r * kind.circles[c * 3 + 1]; p.radii[c] = kind.circles[c * 3 + 2]; }
            list.Add(p); return p;
        }

        /// <summary>A model the player puts down (the last stand's sandbags and hedgehogs): into the cell it stands in,
        /// solid like the rest, without the spacing the country's own props keep.</summary>
        public void AddModel(string mesh, Vector3 pos, float yaw)
        {
            var kind = K(mesh); if (kind == null || !prefabs.ContainsKey(mesh)) return;
            var list = CellProps(Mathf.RoundToInt(pos.x / Cell), Mathf.RoundToInt(pos.z / Cell));
            var p = new Prop { what = What.Model, kind = kind, pos = pos, yaw = yaw, height = kind.height, bound = kind.length, manned = true };
            int n = kind.circles.Length / 3; p.circleCenters = new Vector2[n]; p.radii = new float[n];
            var f = new Vector2(Mathf.Sin(yaw), Mathf.Cos(yaw)); var r = new Vector2(f.y, -f.x);
            for (int c = 0; c < n; c++) { p.circleCenters[c] = new Vector2(pos.x, pos.z) + f * kind.circles[c * 3] + r * kind.circles[c * 3 + 1]; p.radii[c] = kind.circles[c * 3 + 2]; }
            list.Add(p);
        }

        /// <summary>The field works between two distances that have no garrison yet, as the spots to put men in: two in
        /// a length of trench, three in a sandbag nest, three round the outside of a searchlight's ring. Every searchlight
        /// post and three in four of the rest are manned; each is asked once, so the dead are not replaced.</summary>
        public List<(Vector3 pos, bool trench)> Unmanned(Vector3 from, float min, float max)
        {
            var spots = new List<(Vector3, bool)>();
            foreach (var p in active)
            {
                if (p.what != What.Model || p.manned || p.state != 0 || (p.kind.mesh != "trench" && p.kind.mesh != "sandbags" && p.kind.mesh != "foxhole_logs")) continue;
                var d = p.pos - from; d.y = 0f; float len = d.magnitude; if (len < min || len > max) continue;
                p.manned = true;
                bool lamp = false; foreach (var q in active) if (q.what == What.Searchlight && (q.pos - p.pos).sqrMagnitude < 4f) { lamp = true; break; }
                if (!lamp && Rnd(Mathf.RoundToInt(p.pos.x), Mathf.RoundToInt(p.pos.z), 1300) > 0.75f) continue;
                var f = new Vector3(Mathf.Sin(p.yaw), 0f, Mathf.Cos(p.yaw)); var r = new Vector3(f.z, 0f, -f.x);
                if (lamp) { foreach (float a in new[] { 90f, 210f, 330f }) spots.Add((p.pos + Quaternion.Euler(0f, a, 0f) * f * 4.3f, false)); continue; }   // the lamp fills its ring: its guard stands round the outside
                if (p.kind.mesh == "trench") { spots.Add((p.pos - f * 2.2f, true)); spots.Add((p.pos + f * 2.2f, true)); }
                else if (p.kind.mesh == "foxhole_logs") { spots.Add((p.pos - r * 0.8f, true)); spots.Add((p.pos + r * 0.8f, true)); }
                else { spots.Add((p.pos + r * 0.9f, false)); spots.Add((p.pos - r * 0.9f, false)); spots.Add((p.pos + f * 0.9f, false)); }
            }
            return spots;
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
            if (InStream(pos, 3f)) return;   // no tree stands in the water
            if (winter && Place(list, seed % 4 == 0 ? "pine_snow" : seed % 4 == 1 ? "fir_snow_b" : "fir_snow", pos, (seed % 360) * Mathf.Deg2Rad) != null) return;
            if (winter) { if (Place(list, seed % 3 == 0 ? "deadtree" : "spruce_snow", pos, (seed % 360) * Mathf.Deg2Rad) == null) Place(list, "deadtree", pos, (seed % 360) * Mathf.Deg2Rad); return; }   // firs and bare trees in the snow
            if (Kursk && seed % 2 == 0 && Place(list, "tree_birch", pos, (seed % 360) * Mathf.Deg2Rad) != null) return;
            if (Place(list, "tree_oak", pos, (seed % 360) * Mathf.Deg2Rad) != null || prefabs.ContainsKey("tree_oak")) return;   // the model refused here (too near another, on a lane): no tree, not the drawn one
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
            HedgeCircles(p);
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

        /// <summary>How far down a hedge is pressed at a point along it: 1 flat where a hull went through, easing off
        /// over a metre at the edges of its track, 0 standing; and the side it lies to.</summary>
        static float Crush(Prop hedge, float u, out float side)
        {
            side = 0f; float c = 0f; if (hedge.trodden == null) return 0f;
            foreach (var t in hedge.trodden) { float k = 1f - Mathf.Clamp01((Mathf.Abs(u - t.x) - t.y) / 0.9f); if (k > c) { c = k; side = t.z; } }
            return c * c * (3f - 2f * c);
        }

        /// <summary>The hedge's footprint: a circle every 2.5 m but for its gates and where hulls have pressed it down,
        /// wide enough there for the hull that did it.</summary>
        static void HedgeCircles(Prop h)
        {
            var dir = (h.b - h.a).normalized; var centers = new List<Vector2>(); var radii = new List<float>();
            for (float u = 1.25f; u < h.size; u += 2.5f)
            {
                if (Open(h, u)) continue; bool down = false;
                if (h.trodden != null) foreach (var t in h.trodden) if (Mathf.Abs(u - t.x) < t.y + 1.4f) { down = true; break; }
                if (!down) { var c = h.a + dir * u; centers.Add(new Vector2(c.x, c.z)); radii.Add(1.6f); }
            }
            h.circleCenters = centers.ToArray(); h.radii = radii.ToArray();
        }

        /// <summary>House, barn and hay around a trodden yard, the way farms sit in the corner of their fields.</summary>
        void FarmYard(List<Prop> list, int ix, int iz, Vector3 c)
        {
            var o = c + In(ix, iz, 980, 3f);
            float yaw = (Rnd(ix, iz, 982) < 0.5f ? 0f : Mathf.PI / 2f) + (Rnd(ix, iz, 983) - 0.5f) * 0.2f;
            var f = new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw)); var r = new Vector3(f.z, 0f, -f.x);
            list.Add(new Prop { what = What.Decal, pos = o, yaw = yaw, size = 30f, seed = 0 });
            float house = Rnd(ix, iz, 1110); Place(list, house < 0.3f ? "cottage" : house < 0.6f ? "house_normandy" : "farmhouse", o - r * 7f, yaw);
            if (!winter && !Kursk && PlayerPrefs.GetInt("quality", 1) != 0 && Rnd(ix, iz, 1115) < 0.6f)   // the apple orchard: two rows of two past the house (each tree is some 44 000 triangles)
                for (int row = 0; row < 2; row++) for (int k = 0; k < 2; k++) Place(list, "tree_apple", o - r * (14f + row * 5.5f) + f * (-5f + k * 7f + row * 3f) + In(ix, iz, 1116 + row * 8 + k * 2, 0.8f), Rnd(ix, iz, 1135 + row * 4 + k) * 6.28f);
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
            Place(list, Rnd(ix, iz, 1129) < 0.5f ? "house_normandy" : "cottage", o - r * 16f - f * 6f, yaw + Mathf.PI / 2f);
            Place(list, Rnd(ix, iz, 1130) < 0.45f ? "house_ruin" : "cottage", o + r * 16f - f * 2f, yaw - Mathf.PI / 2f);
            Place(list, Rnd(ix, iz, 1124) < 0.5f ? "wall_a" : "wall_b", o - f * 14f - r * 6f, yaw + Mathf.PI / 2f); Place(list, Rnd(ix, iz, 1125) < 0.5f ? "wall_a" : "wall_b", o - f * 14f + r * 8f, yaw + Mathf.PI / 2f);
            Place(list, "cart", o - r * 6f - f * 8f, yaw + Rnd(ix, iz, 1126) * 6.28f);
            Place(list, Rnd(ix, iz, 1131) < 0.5f ? "well_b" : "well", o - r * 4f - f * 12f, yaw);
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
            if (Ardennes) { int young = Rnd(ix, iz, 1600) < 0.55f ? 1 + (int)(Rnd(ix, iz, 1601) * 3f) : 0; for (int y = 0; y < young; y++) Place(list, "fir_young", c + In(ix, iz, 1602 + y * 2, 15f), Rnd(ix, iz, 1610 + y) * 6.28f); }
            if (Rnd(ix, iz, 1240) < (Kursk ? 0.14f : Route == "open" ? 0.08f : 0.05f)) TrenchLine(list, ix, iz, c);
            else if (Rnd(ix, iz, 1250) < (Route == "open" ? 0.05f : 0.025f)) Place(list, "windmill", c + In(ix, iz, 1251, 9f), Rnd(ix, iz, 1253) * 6.28f);
            if (Rnd(ix, iz, 1254) < 0.04f) Place(list, "house_ruin", c + In(ix, iz, 1255, 11f), Rnd(ix, iz, 1257) < 0.5f ? 0f : Mathf.PI / 2f);
            if (Rnd(ix, iz, 1258) < 0.06f) Place(list, "truck_burnt", c + In(ix, iz, 1259, 13f), Rnd(ix, iz, 1261) * 6.28f);
        }

        /// <summary>Fir forest over the cell: a fir every five metres or so, jittered, a few gaps left as clearings with a stump
        /// in them; clear of the lanes along its edges (six metres either side) and of any stream. A lane through it may be
        /// blocked with felled firs, and logs are piled at its side.</summary>
        void ForestCell(List<Prop> list, int ix, int iz, Vector3 c)
        {
            var firs = new List<Vector4>(); var stumps = new List<Vector3>(); int k = 0;
            Vector3? mill = null;
            if (LaneX(ix) && Rnd(ix, iz, 1420) < 0.14f && prefabs.ContainsKey("sawmill")) { mill = new Vector3(c.x + Half - 11f, 0f, c.z + (Rnd(ix, iz, 1421) - 0.5f) * 16f); Place(list, "sawmill", mill.Value, 0f); }
            for (float gx = -Half + 2.2f; gx < Half; gx += 4.6f) for (float gz = -Half + 2.2f; gz < Half; gz += 4.6f, k++)
            {
                var pos = c + new Vector3(gx + (Rnd(ix, iz, 1500 + k) - 0.5f) * 3.4f, 0f, gz + (Rnd(ix, iz, 1700 + k) - 0.5f) * 3.4f);
                if ((LaneX(ix) && Mathf.Abs(pos.x - (c.x + Half)) < 6f) || (LaneX(ix - 1) && Mathf.Abs(pos.x - (c.x - Half)) < 6f)) continue;
                if ((LaneZ(iz) && Mathf.Abs(pos.z - (c.z + Half)) < 6f) || (LaneZ(iz - 1) && Mathf.Abs(pos.z - (c.z - Half)) < 6f)) continue;
                if (InStream(pos, 1.5f)) continue;
                if (mill != null && (pos - mill.Value).sqrMagnitude < 9f * 9f) continue;   // the sawmill's yard
                if (Rnd(ix, iz, 1900 + k) < 0.12f) { if (Rnd(ix, iz, 2100 + k) < 0.6f) stumps.Add(new Vector3(pos.x, 0.7f + Rnd(ix, iz, 2300 + k) * 0.5f, pos.z)); continue; }   // a clearing: a stump, now and then
                firs.Add(new Vector4(pos.x, pos.z, 0.8f + Rnd(ix, iz, 2500 + k) * 0.55f, Rnd(ix, iz, 2700 + k) * 6.28f));
            }
            if (firs.Count == 0) return;
            var p = new Prop { what = What.Forest, pos = c, bound = Half + 6f, height = 9f, seed = (int)(Hash(ix, iz, 1401) & 0xffff), firs = firs.ToArray(), down = new float[firs.Count], stumps = stumps.ToArray() };
            for (int i = 0; i < p.down.Length; i++) p.down[i] = -1f;
            ForestCircles(p); list.Add(p);
            // the lanes through it: felled firs across one now and then, logs at the side of another
            if (LaneX(ix) && Rnd(ix, iz, 1410) < 0.35f) Roadblock(list, new Vector3(c.x + Half, 0f, c.z + (Rnd(ix, iz, 1411) - 0.5f) * 20f), 0f, ix, iz);
            else if (LaneZ(iz) && Rnd(ix, iz, 1412) < 0.35f) Roadblock(list, new Vector3(c.x + (Rnd(ix, iz, 1413) - 0.5f) * 20f, 0f, c.z + Half), Mathf.PI / 2f, ix, iz);
            if (LaneX(ix) && Rnd(ix, iz, 1414) < 0.4f) LogPile(list, new Vector3(c.x + Half - 4.8f, 0f, c.z + (Rnd(ix, iz, 1415) - 0.5f) * 24f), 0f);
        }

        /// <summary>Felled firs dragged across a lane, criss-cross: a hull pushes through slowly, flattening them.</summary>
        void Roadblock(List<Prop> list, Vector3 at, float yaw, int ix, int iz)
        {
            var f = new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw)); var r = new Vector3(f.z, 0f, -f.x);
            var p = new Prop { what = What.Abatis, pos = at, yaw = yaw, bound = 7f, height = 1.6f, seed = (int)(Hash(ix, iz, 1416) & 0xffff) };
            p.circleCenters = new[] { new Vector2(at.x, at.z), new Vector2((at + r * 2.6f).x, (at + r * 2.6f).z), new Vector2((at - r * 2.6f).x, (at - r * 2.6f).z) }; p.radii = new[] { 1.5f, 1.4f, 1.4f };
            list.Add(p);
        }

        /// <summary>Cut fir trunks stacked at the side of a forest lane.</summary>
        void LogPile(List<Prop> list, Vector3 at, float yaw)
        {
            var f = new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw));
            var p = new Prop { what = What.Logs, pos = at, yaw = yaw, bound = 4f, height = 1.4f };
            p.circleCenters = new[] { new Vector2((at + f * 1.4f).x, (at + f * 1.4f).z), new Vector2((at - f * 1.4f).x, (at - f * 1.4f).z) }; p.radii = new[] { 1.1f, 1.1f };
            list.Add(p);
        }

        /// <summary>A forest's footprint: a small circle round every fir still standing, and which fir each is.</summary>
        static void ForestCircles(Prop p)
        {
            var centers = new List<Vector2>(); var radii = new List<float>(); var of = new List<int>();
            for (int i = 0; i < p.firs.Length; i++) { if (p.down[i] >= 0f) continue; centers.Add(new Vector2(p.firs[i].x, p.firs[i].y)); radii.Add(0.55f * p.firs[i].z); of.Add(i); }
            p.circleCenters = centers.ToArray(); p.radii = radii.ToArray(); p.treeOf = of.ToArray();
        }

        /// <summary>A fir knocked down the given way: it lies in the forest from now on, and is heard going.</summary>
        void FellFir(Prop p, int fir, Vector3 dir, bool loud)
        {
            if (p.down[fir] >= 0f) return;
            p.down[fir] = Mathf.Repeat(Mathf.Atan2(dir.x, dir.z) + (Random.value - 0.5f) * 0.6f, 6.283f); ForestCircles(p);
            var at = new Vector3(p.firs[fir].x, 0f, p.firs[fir].y);
            if (loud) Sfx.TreeFall(at);
            if (fx != null) { fx.Dust(at); fx.Leaves(at + Vector3.up * 3f, dir, new Color(0.84f, 0.87f, 0.9f)); }   // snow shaken off the branches
            if (p.go != null && FirModel(fir, p) != null) { (p.lying3D ??= new HashSet<int>()).Add(fir); lyingFirs.Enqueue((p, fir)); FirFalling(p, fir, false); while (lyingFirs.Count > MaxLyingFirs) Flatten(lyingFirs.Dequeue()); }
            RebuildForest(p);
        }

        /// <summary>The forest's mesh made again after a fir has gone over.</summary>
        void RebuildForest(Prop p)
        {
            if (p.go == null) return; var mf = p.go.GetComponent<MeshFilter>(); if (p.mesh != null) Destroy(p.mesh);
            p.mesh = ForestMesh(p); mf.sharedMesh = p.mesh; p.go.GetComponent<MeshRenderer>().sharedMaterials = FirMaterials(p.mesh);
        }

        const int MaxLyingFirs = 16;   // felled firs lying as the model at once, ~12 000 triangles each; older ones lie as their card
        readonly Queue<(Prop p, int fir)> lyingFirs = new Queue<(Prop, int)>();
        class FirFall { public Transform t; public Quaternion rot0; public Vector3 axis, tip; public float a; }
        readonly List<FirFall> firFalls = new List<FirFall>();

        /// <summary>The spruce model a fir's card was made from (the first two cards fir_snow, the others fir_snow_b), at
        /// the card's size; null when it is not there.</summary>
        GameObject FirModel(int fir, Prop p)
        {
            string m = ((int)(p.firs[fir].w * 10f) % FirCard.Length) < 2 ? "fir_snow" : "fir_snow_b"; return prefabs.TryGetValue(m, out var pf) ? pf : null;
        }

        /// <summary>The fir as the model, child of its forest: falling from upright (now) or already lying (built again with
        /// its cell). Its mesh two-sided, so the underside it shows lying is lit.</summary>
        void FirFalling(Prop p, int fir, bool lying)
        {
            var pf = FirModel(fir, p); if (pf == null || p.go == null) return;
            var f = p.firs[fir]; var at = new Vector3(f.x, 0f, f.y);
            var go = Instantiate(pf, p.go.transform); go.name = "Fir " + fir; go.transform.position = at; go.transform.localScale = Vector3.one * (f.z * CardScale);
            var rot0 = Quaternion.Euler(0f, f.w * Mathf.Rad2Deg, 0f); var dir = new Vector3(Mathf.Sin(p.down[fir]), 0f, Mathf.Cos(p.down[fir])); var axis = Vector3.Cross(Vector3.up, dir);
            foreach (var r in go.GetComponentsInChildren<Renderer>()) { r.sharedMaterial = materials.TryGetValue(pf.name, out var mat) ? mat : materials["fir_snow"]; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
            TwoSidedGo(go);
            if (lying) { go.transform.rotation = Quaternion.AngleAxis(86f, axis) * rot0; return; }
            go.transform.rotation = rot0; firFalls.Add(new FirFall { t = go.transform, rot0 = rot0, axis = axis, tip = at + dir * (16f * f.z * CardScale) });
        }

        /// <summary>The oldest model lying turned back into its card, to keep the number of models down.</summary>
        void Flatten((Prop p, int fir) old)
        {
            if (old.p.lying3D == null || !old.p.lying3D.Remove(old.fir)) return;
            if (old.p.go != null) { var t = old.p.go.transform.Find("Fir " + old.fir); if (t != null) Destroy(t.gameObject); }
            RebuildForest(old.p);
        }

        /// <summary>A dug-in line across the field: four lengths of trench zigzagging, wire on stakes seven metres out in
        /// front, a sandbagged nest at one end where an anti-tank gun may wait.</summary>
        void TrenchLine(List<Prop> list, int ix, int iz, Vector3 c)
        {
            float yaw = Rnd(ix, iz, 1241) * Mathf.PI; var f = new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw)); var r = new Vector3(f.z, 0f, -f.x);
            var o = c + In(ix, iz, 1242, 4f);
            string dug = Ardennes && prefabs.ContainsKey("foxhole_logs") ? "foxhole_logs" : "trench";   // the Ardennes dug in under logs
            for (int k = 0; k < 4; k++) Place(list, dug, o + f * (-12f + k * 8f) + r * (k % 2 == 0 ? 0f : 1.6f), yaw + (k % 2 == 0 ? 0.18f : -0.18f));
            for (int k = 0; k < 3; k++) Place(list, "barbed_wire", o + f * (-9f + k * 7f) + r * 7f, yaw + (Rnd(ix, iz, 1244 + k) - 0.5f) * 0.3f);
            Place(list, "sandbags", o + f * (Rnd(ix, iz, 1248) < 0.5f ? 19f : -19f) - r * 1.5f, yaw);
        }

        List<Prop> CellProps(int ix, int iz)
        {
            long key = ((long)ix << 32) ^ (uint)iz; if (cells.TryGetValue(key, out var list)) return list;
            list = new List<Prop>(); var c = new Vector3(ix * Cell, 0f, iz * Cell);
            // the lanes and hedges on the cell's east and north lines; the west and south ones belong to the neighbours
            if (LaneX(ix)) { list.Add(new Prop { what = What.Lane, pos = c + new Vector3(Half, 0f, 0f), yaw = 0f, size = Cell }); foreach (var u in new[] { -12f, 8f }) Place(list, "pole", c + new Vector3(Half + 3.6f, 0f, u), 0f); }
            if (LaneZ(iz)) { list.Add(new Prop { what = What.Lane, pos = c + new Vector3(0f, 0f, Half), yaw = Mathf.PI / 2f, size = Cell }); foreach (var u in new[] { -12f, 8f }) Place(list, "pole", c + new Vector3(u, 0f, Half + 3.6f), Mathf.PI / 2f); }
            // a row of poplars on the other side of every third lane; a signpost where two lanes cross
            if (LaneX(ix) && !winter && Rnd(ix, iz, 1140) < 0.35f) foreach (var u in new[] { -16f, -4f, 8f }) Place(list, Rnd(ix, iz, 1170 + (int)u) < 0.5f ? "tree_poplar_b" : "tree_poplar", c + new Vector3(Half - 4.2f, 0f, u + Rnd(ix, iz, 1141 + (int)u) * 2f), Rnd(ix, iz, 1150 + (int)u) * 6.28f);
            if (LaneZ(iz) && !winter && Rnd(ix, iz, 1142) < 0.35f) foreach (var u in new[] { -16f, -4f, 8f }) Place(list, Rnd(ix, iz, 1180 + (int)u) < 0.5f ? "tree_poplar_b" : "tree_poplar", c + new Vector3(u + Rnd(ix, iz, 1143 + (int)u) * 2f, 0f, Half - 4.2f), Rnd(ix, iz, 1160 + (int)u) * 6.28f);
            if (LaneX(ix) && LaneZ(iz)) Place(list, "signpost", c + new Vector3(Half + 4.5f, 0f, Half + 4.5f), Rnd(ix, iz, 1144) * 6.28f);
            if (Ardennes && LaneX(ix) && LaneZ(iz) && Rnd(ix, iz, 1620) < 0.45f) Place(list, "chapel_wayside", c + new Vector3(Half - 7.5f, 0f, Half - 7.5f), Mathf.PI * 0.25f);   // a wayside chapel at the crossing, facing it
            if (HedgeX(ix, iz)) Hedge(list, c + new Vector3(Half, 0f, -Half), c + new Vector3(Half, 0f, Half), StreamZ(iz - 1) ? 11f : LaneZ(iz - 1) ? 4f : 0f, StreamZ(iz) ? 11f : LaneZ(iz) ? 4f : 0f, ix, iz, 0);
            if (StreamZ(iz))
            {
                list.Add(new Prop { what = What.Stream, pos = c + new Vector3(0f, 0f, Half), seed = iz, bound = Half + 8f });
                // a bridge where a lane crosses it: stone in the west, timber on the steppe or now and then a sapper's one
                if (LaneX(ix)) { float bx = c.x + Half, bz = StreamAt(iz, bx); var b = Place(list, Kursk || Rnd(ix, iz, 951) < 0.3f ? "bridge_wood" : "bridge_stone", new Vector3(bx, 0f, bz), 0f, true); if (b != null) { Parapets(b); bridges.Add(b); } }
            }
            if (HedgeZ(ix, iz)) Hedge(list, c + new Vector3(-Half, 0f, Half), c + new Vector3(Half, 0f, Half), LaneX(ix - 1) ? 4f : 0f, LaneX(ix) ? 4f : 0f, ix, iz, 1);
            if (Farm(ix, iz)) FarmYard(list, ix, iz, c);
            else if (Battery(ix, iz)) SearchlightPost(list, ix, iz, c);
            else if (Village(ix, iz)) VillageSquare(list, ix, iz, c);
            else if (Forest(ix, iz)) ForestCell(list, ix, iz, c);
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
        /// <summary>Every searchlight whose post is within the radius of a blast on the ground goes out; returns where they stood.</summary>
        public List<Vector3> BreakLamps(Vector3 at, float radius)
        {
            var hit = new List<Vector3>();
            foreach (var p in active)
            {
                if (p.what != What.Searchlight || deadLamps.Contains(p.seed)) continue;
                var d = p.pos - at; d.y = 0f; if (d.sqrMagnitude > radius * radius) continue;
                deadLamps.Add(p.seed); KillLamp(p); hit.Add(p.drum != null ? p.drum.position : p.pos + Vector3.up * 2.4f);
            }
            return hit;
        }

        /// <summary>A searchlight put out: its beam and glow gone, the drum drooping on its yoke.</summary>
        void KillLamp(Prop p) { if (p.shaft != null) p.shaft.gameObject.SetActive(false); if (p.lamp != null) p.lamp.gameObject.SetActive(false); if (p.drum != null) p.drum.localRotation = Quaternion.Euler(28f, 0f, 0f); }

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

        // ---- the firs: made here, four shapes, snow on their tiers; a forest is one mesh of them ----
        static readonly Vector4[] FirCard =   // each card at size 1: its width and height in metres, the tree's foot on it (from the left, from the bottom)
        {
            new Vector4(9.09f, 16.53f, 0.500f, 0.069f), new Vector4(8.88f, 16.13f, 0.490f, 0.086f),
            new Vector4(6.61f, 12.02f, 0.500f, 0.069f), new Vector4(6.71f, 12.30f, 0.498f, 0.067f),
        };
        const float CardScale = 0.75f;   // the forest's firs at three quarters of the models' height: some 12 to 22 m
        Mesh[] cardStanding, cardLying; Material cardMaterial;
        Mesh[] firShapes, firCards; Mesh stumpShape; Material firMaterial, logMaterial, branchMaterial;   // firCards: each shape's ring of branch cards; null without the branch texture or on low quality

        /// <summary>The fir shapes and their material (a strip of bark, needles and snow along u), made the first time.</summary>
        void FirKit()
        {
            if (firShapes != null) return;
            var tex = new Texture2D(64, 32, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 32; y++) for (int x = 0; x < 64; x++)
            {
                float u = x / 63f, n = Mathf.PerlinNoise(x * 0.37f, y * 0.41f);
                Color c = u < 0.1f ? new Color(0.24f, 0.19f, 0.14f) : u < 0.55f ? Color.Lerp(new Color(0.07f, 0.12f, 0.09f), new Color(0.12f, 0.18f, 0.13f), n) : Color.Lerp(new Color(0.16f, 0.22f, 0.18f), new Color(0.86f, 0.89f, 0.93f), Mathf.SmoothStep(0.55f, 0.9f, u) * (0.75f + 0.25f * n));
                tex.SetPixel(x, y, c);
            }
            tex.Apply(true);
            firMaterial = new Material(Resources.Load<Material>("VehicleLit")); firMaterial.SetTexture("_BaseMap", tex); firMaterial.SetColor("_BaseColor", new Color(0.85f, 0.88f, 0.9f)); firMaterial.SetFloat("_Smoothness", 0.12f); firMaterial.SetFloat("_Cull", 2f);
            logMaterial = new Material(trunkMaterial); logMaterial.SetColor("_BaseColor", new Color(0.33f, 0.26f, 0.19f));
            var atlas = Resources.Load<Texture2D>("Textures/fir_cards");
            if (atlas != null)
            {
                cardMaterial = new Material(Resources.Load<Material>("FoliageCut")); cardMaterial.SetTexture("_BaseMap", atlas); cardMaterial.SetColor("_BaseColor", new Color(0.8f, 0.83f, 0.88f)); cardMaterial.SetFloat("_Cull", 0f);
                cardStanding = new Mesh[FirCard.Length]; cardLying = new Mesh[FirCard.Length];
                for (int i = 0; i < FirCard.Length; i++) { cardStanding[i] = FirCardMesh(i, false); cardLying[i] = FirCardMesh(i, true); }
            }
            var branch = Resources.Load<Texture2D>("Textures/fir_branch"); bool cards = branch != null && PlayerPrefs.GetInt("quality", 1) != 0;
            if (cards) { branchMaterial = new Material(Resources.Load<Material>("FoliageCut")); branchMaterial.SetTexture("_BaseMap", branch); branchMaterial.SetColor("_BaseColor", new Color(0.82f, 0.86f, 0.9f)); branchMaterial.SetFloat("_Cull", 0f); }
            firShapes = new Mesh[4]; firCards = cards ? new Mesh[4] : null; var rng = new System.Random(7);
            for (int s = 0; s < 4; s++) { firShapes[s] = FirShape(rng, 10f, 2.6f + s * 0.15f, out var c); if (cards) firCards[s] = c; else Destroy(c); }
            stumpShape = Prism(0.3f, 1f, 7, 0.05f);
        }

        /// <summary>A fir card: standing, upright to the camera's view (its up the screen's up, leaning away) and moved
        /// towards the camera far enough that its lower edge clears the ground, which does not change where it is seen;
        /// lying, flat on the ground along +Z, a little longer for the view's foreshortening. Lit as if facing up.</summary>
        static Mesh FirCardMesh(int i, bool lying)
        {
            var c = FirCard[i]; float w = c.x, h = c.y, fu = c.z, fv = c.w;
            var toCam = new Vector3(0f, 44f, -40f).normalized; Vector3 up, at;
            if (!lying) { up = new Vector3(0f, 40f, 44f).normalized; at = toCam * (fv * h * up.y / toCam.y + 0.05f); }
            else { up = Vector3.forward * 1.3f; at = Vector3.up * 0.3f; }
            float u0 = i / (float)FirCard.Length, u1 = (i + 1) / (float)FirCard.Length;
            var m = new Mesh { name = lying ? "fir card lying" : "fir card" };
            m.vertices = new[] { at + Vector3.right * (-fu * w) + up * (-fv * h), at + Vector3.right * ((1f - fu) * w) + up * (-fv * h), at + Vector3.right * (-fu * w) + up * ((1f - fv) * h), at + Vector3.right * ((1f - fu) * w) + up * ((1f - fv) * h) };
            m.uv = new[] { new Vector2(u0, 0f), new Vector2(u1, 0f), new Vector2(u0, 1f), new Vector2(u1, 1f) };
            m.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            m.triangles = new[] { 0, 2, 1, 1, 2, 3 }; m.RecalculateBounds(); return m;
        }

        /// <summary>A fir H metres tall: a trunk and five tiers, each a drooping ring of branch tips (every other one drawn
        /// in) rising to its top, the lower half of a tier needles, the upper half snow.</summary>
        static Mesh FirShape(System.Random rng, float H, float R, out Mesh cards)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            var cv = new List<Vector3>(); var cuv = new List<Vector2>(); var cn = new List<Vector3>(); var ct = new List<int>();
            void Add(Mesh m) { int o = v.Count; v.AddRange(m.vertices); uv.AddRange(m.uv); foreach (var i in m.triangles) t.Add(o + i); Destroy(m); }
            Add(Prism(0.22f, 1.6f, 6, 0.05f));
            const int T = 5, N = 9;
            for (int i = 0; i < T; i++)
            {
                float y0 = 1.1f + i * (H - 1.1f) * 0.17f, th = (H - 1.1f) * (i == T - 1 ? 0.32f : 0.3f), r = R * (1f - i / (float)(T + 0.6f)) * (0.9f + 0.2f * (float)rng.NextDouble());
                int o = v.Count; float spin = (float)rng.NextDouble() * 6.28f;
                for (int k = 0; k < N; k++) { float a = spin + k * 6.283f / N, rr = r * (k % 2 == 0 ? 1f : 0.74f); v.Add(new Vector3(Mathf.Cos(a) * rr, y0 - 0.2f, Mathf.Sin(a) * rr)); uv.Add(new Vector2(0.2f, 0.5f)); }
                for (int k = 0; k < N; k++) { float a = spin + (k + 0.5f) * 6.283f / N, rr = r * 0.5f; v.Add(new Vector3(Mathf.Cos(a) * rr, y0 + th * 0.45f, Mathf.Sin(a) * rr)); uv.Add(new Vector2(0.62f, 0.5f)); }
                v.Add(new Vector3(0f, y0 + th, 0f)); uv.Add(new Vector2(0.98f, 0.5f)); int apex = v.Count - 1;
                // the tier's branches: eight cards standing out from the trunk, base at the trunk (the picture's left edge),
                // drooping to their tips; lit as if facing up and out, so a ring reads as one bough
                for (int k = 0; k < 8; k++)
                {
                    float a = spin + (k + 0.25f) * 6.283f / 8f; var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)); float len = r * 1.08f, hgt = r * 0.5f;
                    int o2 = cv.Count; var nrm = (Vector3.up + d * 0.4f).normalized;
                    cv.Add(d * 0.12f * r + Vector3.up * (y0 + th * 0.28f)); cv.Add(d * 0.12f * r + Vector3.up * (y0 + th * 0.28f + hgt));
                    cv.Add(d * len + Vector3.up * (y0 - 0.45f)); cv.Add(d * len + Vector3.up * (y0 - 0.45f + hgt));
                    cuv.Add(new Vector2(0f, 0f)); cuv.Add(new Vector2(0f, 1f)); cuv.Add(new Vector2(1f, 0f)); cuv.Add(new Vector2(1f, 1f));
                    for (int q = 0; q < 4; q++) cn.Add(nrm);
                    ct.Add(o2); ct.Add(o2 + 1); ct.Add(o2 + 2); ct.Add(o2 + 2); ct.Add(o2 + 1); ct.Add(o2 + 3);
                }
                for (int k = 0; k < N; k++)
                {
                    int a0 = o + k, a1 = o + (k + 1) % N, b0 = o + N + k, b1 = o + N + (k + 1) % N;
                    t.Add(a0); t.Add(b0); t.Add(a1); t.Add(a1); t.Add(b0); t.Add(b1);   // the lower ring up to the middle one
                    t.Add(b0); t.Add(apex); t.Add(b1);
                }
            }
            cards = new Mesh { name = "fir branches" }; cards.SetVertices(cv); cards.SetUVs(0, cuv); cards.SetNormals(cn); cards.SetTriangles(ct, 0); cards.RecalculateBounds();
            var m2 = new Mesh { name = "fir" }; m2.SetVertices(v); m2.SetUVs(0, uv); m2.SetTriangles(t, 0); m2.RecalculateNormals(); m2.RecalculateBounds(); return m2;
        }

        /// <summary>A short n-sided prism (a trunk, a stump) of radius r and height h, coloured as bark (u).</summary>
        static Mesh Prism(float r, float h, int n, float u)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            for (int k = 0; k <= n; k++) { float a = k * 6.283f / n; var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)); v.Add(d * r); v.Add(d * r * 0.85f + Vector3.up * h); uv.Add(new Vector2(u, 0.2f)); uv.Add(new Vector2(u, 0.8f)); }
            for (int k = 0; k < n; k++) { int a = k * 2; t.Add(a); t.Add(a + 1); t.Add(a + 2); t.Add(a + 2); t.Add(a + 1); t.Add(a + 3); }
            int top = v.Count; v.Add(Vector3.up * h); uv.Add(new Vector2(0.12f, 0.5f));
            for (int k = 0; k < n; k++) { t.Add(top); t.Add(k * 2 + 3); t.Add(k * 2 + 1); }
            var m = new Mesh(); m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0); m.RecalculateNormals(); return m;
        }

        /// <summary>The forest as one mesh: its firs standing or lying the way they fell, and its stumps.</summary>
        Mesh ForestMesh(Prop p)
        {
            var parts = new List<CombineInstance>(); var boughs = new List<CombineInstance>(); var origin = p.pos;
            if (cardStanding != null)
            {
                // the cards: standing, facing the camera; felled, laid along the ground the way they went, a stump left
                for (int i = 0; i < p.firs.Length; i++)
                {
                    var f = p.firs[i]; var at = new Vector3(f.x, 0f, f.y) - origin; int ci = (int)(f.w * 10f) % FirCard.Length; var size = Vector3.one * (f.z * CardScale);
                    if (p.down[i] < 0f) { boughs.Add(new CombineInstance { mesh = cardStanding[ci], transform = Matrix4x4.TRS(at, Quaternion.identity, size) }); continue; }
                    if (p.lying3D == null || !p.lying3D.Contains(i)) boughs.Add(new CombineInstance { mesh = cardLying[ci], transform = Matrix4x4.TRS(at, Quaternion.Euler(0f, p.down[i] * Mathf.Rad2Deg, 0f), size) });
                    parts.Add(new CombineInstance { mesh = stumpShape, transform = Matrix4x4.TRS(at, Quaternion.identity, new Vector3(1.3f, 0.6f, 1.3f) * f.z) });
                }
                foreach (var s in p.stumps) parts.Add(new CombineInstance { mesh = stumpShape, transform = Matrix4x4.TRS(new Vector3(s.x, 0f, s.z) - origin, Quaternion.Euler(0f, s.x * 37f, 0f), new Vector3(1.2f, s.y, 1.2f)) });
                return TwoParts(parts, boughs, "forest");
            }
            for (int i = 0; i < p.firs.Length; i++)
            {
                var f = p.firs[i]; var at = new Vector3(f.x, 0f, f.y) - origin; var turn = Quaternion.Euler(0f, f.w * Mathf.Rad2Deg, 0f); int si = (int)(f.w * 10f) % firShapes.Length;
                Matrix4x4 m4;
                if (p.down[i] < 0f) m4 = Matrix4x4.TRS(at, turn, Vector3.one * f.z);
                else
                {
                    var dir = new Vector3(Mathf.Sin(p.down[i]), 0f, Mathf.Cos(p.down[i])); var lay = Quaternion.AngleAxis(84f, Vector3.Cross(Vector3.up, dir));
                    m4 = Matrix4x4.TRS(at + Vector3.up * 0.5f, lay * turn, Vector3.one * f.z);
                    parts.Add(new CombineInstance { mesh = stumpShape, transform = Matrix4x4.TRS(at, turn, new Vector3(1.3f, 0.6f, 1.3f) * f.z) });
                }
                parts.Add(new CombineInstance { mesh = firShapes[si], transform = m4 });
                if (firCards != null) boughs.Add(new CombineInstance { mesh = firCards[si], transform = m4 });
            }
            foreach (var s in p.stumps) parts.Add(new CombineInstance { mesh = stumpShape, transform = Matrix4x4.TRS(new Vector3(s.x, 0f, s.z) - origin, Quaternion.Euler(0f, s.x * 37f, 0f), new Vector3(1.2f, s.y, 1.2f)) });
            return TwoParts(parts, boughs, "forest");
        }

        /// <summary>One mesh of two parts: the solid insides (firMaterial), then the branch cards (branchMaterial), when there are any.</summary>
        Mesh TwoParts(List<CombineInstance> solid, List<CombineInstance> boughs, string name)
        {
            var a = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; a.CombineMeshes(solid.ToArray(), true, true);
            if (boughs.Count == 0) { a.name = name; a.RecalculateBounds(); return a; }
            if (solid.Count == 0) solid.Add(new CombineInstance { mesh = stumpShape, transform = Matrix4x4.TRS(Vector3.down * 5f, Quaternion.identity, Vector3.one * 0.01f) });   // the first part never empty: a speck out of sight
            a.CombineMeshes(solid.ToArray(), true, true);
            var b = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; b.CombineMeshes(boughs.ToArray(), true, true);
            var m = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32, name = name };
            m.CombineMeshes(new[] { new CombineInstance { mesh = a, transform = Matrix4x4.identity }, new CombineInstance { mesh = b, transform = Matrix4x4.identity } }, false, true);
            Destroy(a); Destroy(b); m.RecalculateBounds(); return m;
        }
        Material[] FirMaterials(Mesh m) => m.subMeshCount > 1 ? new[] { firMaterial, cardStanding != null ? cardMaterial : branchMaterial } : new[] { firMaterial };

        void SpawnForest(Prop p)
        {
            FirKit(); var go = new GameObject("Forest"); go.transform.SetParent(transform, false); go.transform.position = p.pos;
            p.mesh = ForestMesh(p); go.AddComponent<MeshFilter>().sharedMesh = p.mesh; var r = go.AddComponent<MeshRenderer>(); r.sharedMaterials = FirMaterials(p.mesh); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            p.go = go;
            if (p.lying3D != null) foreach (var i in p.lying3D) FirFalling(p, i, true);   // the firs felled before, lying as they fell
        }

        /// <summary>Three firs felled across the lane, crossing; pressed into the snow once a hull has gone through.</summary>
        void SpawnAbatis(Prop p)
        {
            FirKit(); var rng = new System.Random(p.seed); var parts = new List<CombineInstance>(); var boughs = new List<CombineInstance>(); var f = new Vector3(Mathf.Sin(p.yaw), 0f, Mathf.Cos(p.yaw)); var r = new Vector3(f.z, 0f, -f.x);
            for (int i = 0; i < 3; i++)
            {
                float across = (i - 1) * 1.6f, ang = (i % 2 == 0 ? 70f : 110f) + (float)rng.NextDouble() * 14f; var dir = Quaternion.Euler(0f, ang, 0f) * f;
                if (cardLying != null) { boughs.Add(new CombineInstance { mesh = cardLying[i % FirCard.Length], transform = Matrix4x4.TRS(f * across - dir * 5f + Vector3.up * (i * 0.15f), Quaternion.LookRotation(dir), Vector3.one * 0.7f) }); continue; }
                var lay = Quaternion.AngleAxis(86f, Vector3.Cross(Vector3.up, dir)); var at = f * across - dir * 4.5f + Vector3.up * (0.4f + i * 0.25f);
                var m4 = Matrix4x4.TRS(at, lay * Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f), Vector3.one * 0.9f);
                parts.Add(new CombineInstance { mesh = firShapes[i], transform = m4 }); if (firCards != null) boughs.Add(new CombineInstance { mesh = firCards[i], transform = m4 });
            }
            var go = new GameObject("Roadblock"); go.transform.SetParent(transform, false); go.transform.position = p.pos;
            p.mesh = TwoParts(parts, boughs, "roadblock");
            go.AddComponent<MeshFilter>().sharedMesh = p.mesh; var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterials = FirMaterials(p.mesh); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            if (p.state == 2) go.transform.localScale = new Vector3(1f, 0.3f, 1f);
            p.go = go;
        }

        /// <summary>Cut trunks stacked three, two and one, their ends to the lane.</summary>
        void SpawnLogs(Prop p)
        {
            FirKit(); var parts = new List<CombineInstance>(); var cyl = LogShape();
            int k = 0; for (int row = 0; row < 3; row++) for (int i = 0; i < 3 - row; i++, k++)
                parts.Add(new CombineInstance { mesh = cyl, transform = Matrix4x4.TRS(new Vector3((i - (2 - row) * 0.5f) * 0.56f, 0.28f + row * 0.48f, (k % 2) * 0.2f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.55f, 2.7f, 0.55f)) });
            var go = new GameObject("Logs"); go.transform.SetParent(transform, false); go.transform.position = p.pos; go.transform.rotation = Quaternion.Euler(0f, p.yaw * Mathf.Rad2Deg, 0f);
            p.mesh = new Mesh { name = "logs" }; p.mesh.CombineMeshes(parts.ToArray(), true, true); p.mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = p.mesh; var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = logMaterial; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            if (p.state == 2) go.transform.localScale = new Vector3(1f, 0.35f, 1f);
            p.go = go;
        }
        static Mesh logShape;
        static Mesh LogShape() { if (logShape == null) { var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder); logShape = g.GetComponent<MeshFilter>().sharedMesh; Destroy(g); } return logShape; }

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
            float time = Time.time;
            for (int i = firFalls.Count - 1; i >= 0; i--)
            {
                var ff = firFalls[i]; if (ff.t == null) { firFalls.RemoveAt(i); continue; }
                ff.a = Mathf.Min(1f, ff.a + Time.deltaTime / 1.4f); ff.t.rotation = Quaternion.AngleAxis(86f * ff.a * ff.a, ff.axis) * ff.rot0;   // slow to start, fast at the end
                if (ff.a >= 1f) { if (fx != null) { fx.Dust(ff.tip); fx.Leaves(ff.tip + Vector3.up * 1.5f, ff.axis, new Color(0.84f, 0.87f, 0.9f)); } firFalls.RemoveAt(i); }
            }
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
                // the beam wanders round the sky between 22 and 62 degrees up on every night: a flak searchlight looks for
                // aircraft and never down at a tank; the lamp glow faces the camera
                float ph = p.seed * 0.37f, az = time * 7f * (p.seed % 2 == 0 ? 1f : -1f) + ph * 57f, el = 42f + Mathf.Sin(time * 0.17f + ph) * 20f;
                p.az = Mathf.MoveTowardsAngle(p.az, az, 90f * Time.deltaTime); p.el = Mathf.MoveTowards(p.el, el, 25f * Time.deltaTime);
                p.yoke.localRotation = Quaternion.Euler(0f, p.az, 0f); p.drum.localRotation = Quaternion.Euler(-p.el, 0f, 0f);
                p.lamp.rotation = cam.rotation;
                p.shaft.Set(p.drum.position + p.drum.forward * 0.6f, p.drum.forward);
                // the post's gun fires a burst at the sky now and then: five tracers climbing along the beam
                p.flakTimer -= Time.deltaTime;
                if (!postGuns && p.flakTimer <= 0f) { if (p.burst == 0) p.burst = 5; p.flakTimer = p.burst > 1 ? 0.13f : 7f + Rnd(p.seed, (int)(time * 10f), 3) * 12f; p.burst--; if (fx != null) { var from = p.pos + Quaternion.Euler(0f, p.yaw * Mathf.Rad2Deg, 0f) * new Vector3(5f, 1.2f, 1f); fx.Flak(from, (p.drum.forward + Random.insideUnitSphere * 0.06f).normalized); if (p.burst == 4) Sfx.Flak(from); } }
            }
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
                case What.Stream: SpawnStream(p); break;
                case What.Forest: SpawnForest(p); break;
                case What.Abatis: SpawnAbatis(p); break;
                case What.Logs: SpawnLogs(p); break;
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
                    // trodden earth under it: a soft dark patch a little wider than the footprint (not under a bridge's parapets, over the water)
                    if (!p.kind.mesh.StartsWith("bridge_")) for (int c = 0; c < p.radii.Length; c++) { var q = Quad(go.transform, new Vector3(p.circleCenters[c].x, 0f, p.circleCenters[c].y), 0f, p.radii[c] * 2.4f, p.radii[c] * 2.4f, patchMaterial, 0.06f); q.GetComponent<Renderer>().receiveShadows = false; }
                    if (p.state == 1) { go.transform.rotation = Fallen(p); TwoSided(p); } else if (p.state == 2 && p.kind.mesh == "trench") CaveIn(p, go);
                    else if (p.state == 2) go.transform.localScale = new Vector3(1f, 0.22f, 1f);
                    else if (p.kind.mesh.StartsWith("bridge_")) go.transform.localScale = new Vector3(1f, BridgeFlat, 1f);
                    else if (p.kind.mesh == "trench") { go.transform.localScale = new Vector3(1f, TrenchFlat, 1f); go.transform.position = p.pos + Vector3.up * TrenchSink; }
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
                if (Open(p, u)) continue; float crush = Crush(p, u, out float lean);   // pressed down where a hull went through
                for (int row = -1; row <= 1; row += 2)
                {
                    float sx = 1.3f + (float)rng.NextDouble() * 0.6f, sy = 0.85f + (float)rng.NextDouble() * 0.4f, sz = sx * (0.8f + (float)rng.NextDouble() * 0.3f);
                    sy *= 1f - 0.7f * crush; sx *= 1f + 0.2f * crush;
                    var local = p.a - p.pos + dir * (u + ((float)rng.NextDouble() - 0.5f) * 0.5f) + side * (row * 0.5f + lean * crush * 0.5f + ((float)rng.NextDouble() - 0.5f) * 0.3f) + Vector3.up * (0.45f * sy);
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
                var rot = Quaternion.Euler(0f, yawDeg + (rng.Next(2) == 0 ? 0f : 180f), 0f); var scale = new Vector3(1f, 0.85f + (float)rng.NextDouble() * 0.3f, 1f);
                bool pressed = false; if (p.trodden != null) foreach (var t in p.trodden) if (Mathf.Abs(t.x - mid) < Section * 0.5f + t.y + 0.9f) pressed = true;
                var sec = pressed ? SlicedSection(hm, go.transform) : Instantiate(prefabs[hm], go.transform);   // where a hull went through, the section is built of its slices
                sec.transform.SetPositionAndRotation(p.a + dir * mid, rot); sec.transform.localScale = scale;
                if (pressed) LaySlices(p, sec.transform);
                else foreach (var r in sec.GetComponentsInChildren<Renderer>()) { r.sharedMaterial = materials[hm]; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
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

        /// <summary>True when nothing solid stands on the line between two points: the camera's view of a tank. Hedges
        /// count three metres high, trees by their crowns (five metres round, up to twelve), low walls and gates two and a half.</summary>
        public bool SightClear(Vector3 from, Vector3 to)
        {
            for (int i = 0; i <= 12; i++)
            {
                var q = Vector3.Lerp(from, to, i / 13f);
                foreach (var p in active)
                {
                    if (p.radii.Length == 0 || p.drivable) continue;
                    bool tree = p.what == What.Tree || p.what == What.Forest || (p.kind != null && (p.kind.mesh.StartsWith("tree_") || p.kind.mesh.StartsWith("spruce") || p.kind.mesh.StartsWith("fir_") || p.kind.mesh == "pine_snow"));
                    float h = tree ? 12f : p.what == What.Hedge ? 3.2f : p.height > 0f ? p.height : 2.5f, crown = p.what == What.Tree ? 5f : p.what == What.Forest ? 2.4f : p.kind != null ? Mathf.Max(3f, p.kind.length * 0.5f) : 3f; if (q.y > h) continue;
                    float dx = p.pos.x - q.x, dz = p.pos.z - q.z, reach = p.bound + 4f; if (dx * dx + dz * dz > reach * reach) continue;
                    for (int c = 0; c < p.radii.Length; c++) { float rad = tree && q.y > 2f ? Mathf.Max(p.radii[c], crown) : p.radii[c]; if ((new Vector2(q.x, q.z) - p.circleCenters[c]).sqrMagnitude < rad * rad) return false; }
                }
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
        /// <summary>The searchlight posts within reach that have no gun yet, as where their gun goes (beside the ring, away
        /// from the lorry and the bunker) and its seed; each is given once.</summary>
        public List<(Vector3 at, int seed)> UnarmedPosts(Vector3 from, float max, HashSet<int> armed)
        {
            var list = new List<(Vector3, int)>();
            foreach (var p in active)
            {
                if (p.what != What.Searchlight || armed.Contains(p.seed)) continue;
                var d = p.pos - from; d.y = 0f; if (d.magnitude > max) continue;
                var f = new Vector3(Mathf.Sin(p.yaw), 0f, Mathf.Cos(p.yaw)); var r = new Vector3(f.z, 0f, -f.x);
                armed.Add(p.seed); list.Add((p.pos - f * 4.5f - r * 5.5f, p.seed));
            }
            return list;
        }
        /// <summary>The bunkers still standing within reach of a point: where their gun is (its slit, a metre up, on the side
        /// towards the point) and a key to keep its bursts by.</summary>
        public List<(Vector3 slit, int key)> Bunkers(Vector3 from, float max)
        {
            var list = new List<(Vector3, int)>();
            foreach (var p in active)
            {
                if (p.what != What.Model || p.state != 0 || p.kind.mesh != "bunker") continue;
                var d = from - p.pos; d.y = 0f; float len = d.magnitude; if (len > max || len < 0.1f) continue;
                list.Add((p.pos + d / len * 2.6f + Vector3.up * 1.1f, Mathf.RoundToInt(p.pos.x) * 7919 + Mathf.RoundToInt(p.pos.z)));
            }
            return list;
        }
        /// <summary>A bunker brought down: the battle pays for it.</summary>
        public System.Action<Vector3> bunkerDown;

        /// <summary>Set when the posts have guns of their own (the battle's): the lamps no longer fire tracers themselves.</summary>
        public bool postGuns;

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
            if (InStream(wreck.position, 0.5f)) return;   // no burnt ground on water
            var q = Quad(transform, wreck.position, Random.value * 360f, size, size, scorchMaterial, 0.075f); q.name = "Scorch"; q.transform.SetParent(wreck, true);
        }
        Material scorchMaterial;

        public void Crater(Vector3 pos, float size)
        {
            if (InStream(pos, 0.5f)) { if (fx != null) fx.WaterColumn(pos, size); return; }   // water keeps no crater
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
        /// it down, 4 explosions or a dozer blade (a hedge also gives under any hull, pressed down: see Trample).</summary>
        static int BreakKind(Prop p)
        {
            if (p.what == What.Tree) return 1;
            if (p.what == What.Hedge) return 4;
            if (p.what == What.Abatis || p.what == What.Logs) return 2;
            if (p.what != What.Model) return 0;
            switch (p.kind.mesh)
            {
                case "tree_oak": case "tree_poplar": case "tree_poplar_b": case "tree_apple": case "tree_birch": case "deadtree": case "spruce_snow": case "k_birches": case "pole": case "signpost": return 1;
                case "barbed_wire": case "foxhole_logs": case "trench": return 2;
                case "fir_snow": case "fir_snow_b": case "pine_snow": case "fir_young": return 1;
                case "house_belgian": case "farm_belgian": case "sawmill": case "chapel_wayside": case "truck_snow": return 3;
                case "house_normandy": case "house_ruin": case "izba": case "windmill": return 3;
                case "well_b": return 4;
                case "k_wattle": case "cart": case "barrels": case "haystack": case "k_sheaves": case "gate": case "k_well": case "k_sunflowers": return 2;
                case "farmhouse": case "cottage": case "barn": case "church": case "k_khata": case "k_izba": case "k_church": case "bunker": case "truck": return 3;
                case "wall_a": case "wall_b": case "sandbags": case "well": return 4;
                default: return 0;
            }
        }
        static float MaxHp(string mesh) { switch (mesh) { case "farm_belgian": return 7f; case "house_belgian": return 5f; case "chapel_wayside": return 4f; case "sawmill": return 3f; case "truck_snow": return 1.5f; case "windmill": return 7f; case "house_normandy": return 5f; case "izba": return 4f; case "house_ruin": return 3f; case "church": return 12f; case "k_church": return 9f; case "bunker": return 8f; case "farmhouse": return 6f; case "cottage": case "barn": case "k_izba": return 4f; case "k_khata": return 3f; case "truck": case "sandbags": return 1.5f; default: return 2f; } }
        static bool Burns(string mesh) => mesh == "sawmill" || mesh == "truck_snow" || mesh == "house_normandy" || mesh == "izba" || mesh == "barn" || mesh == "cottage" || mesh == "k_khata" || mesh == "k_izba" || mesh == "truck" || mesh == "haystack" || mesh == "k_sheaves" || mesh == "cart";
        static Vector3 FallDir(Prop p) => new Vector3(Mathf.Sin(p.fallYaw), 0f, Mathf.Cos(p.fallYaw));
        static Quaternion Fallen(Prop p) => Quaternion.AngleAxis(88f, Vector3.Cross(Vector3.up, FallDir(p))) * Quaternion.Euler(0f, p.yaw * Mathf.Rad2Deg, 0f);

        /// <summary>A hull against the country: trees and poles go over the way it drives, fences, carts and hay are
        /// crushed, a hedge is shouldered through and pressed down; with a dozer blade the walls and sandbags give way too
        /// and a hedge is cleared away. Returns the share of its speed the hull keeps: 1 when it went through nothing.</summary>
        public float Ram(Vector3 pos, float radius, Vector3 forward, bool dozer)
        {
            float keep = 1f;
            for (int i = 0; i < active.Count; i++)
            {
                var p = active[i]; if (p.radii.Length == 0 || p.drivable || p.state != 0) continue;
                float reach = p.bound + radius + 1f; if ((p.pos - pos).sqrMagnitude > reach * reach) continue;
                if (p.what == What.Forest)
                {
                    // into the forest: every fir the hull drives into goes down ahead of it, and each one holds it back
                    for (int c = p.radii.Length - 1; c >= 0; c--)
                    {
                        float min = p.radii[c] + radius * 0.97f; if ((new Vector2(pos.x, pos.z) - p.circleCenters[c]).sqrMagnitude >= min * min) continue;
                        keep = Mathf.Min(keep, 0.5f); FellFir(p, p.treeOf[c], forward, true); break;
                    }
                    continue;
                }
                int kind = BreakKind(p); bool hedge = p.what == What.Hedge; if (kind == 0 || kind == 3 || (kind == 4 && !dozer && !hedge)) continue;
                for (int c = 0; c < p.radii.Length; c++)
                {
                    float min = p.radii[c] + radius * 0.97f; var cc = p.circleCenters[c];   // the hull is pushed back out every frame: only a hull driving in reaches this
                    if ((new Vector2(pos.x, pos.z) - cc).sqrMagnitude >= min * min) continue;
                    keep = Mathf.Min(keep, hedge && dozer ? 0.8f : Drag(p));
                    if (hedge) { if (dozer) Gap(p, new Vector3(cc.x, 0f, cc.y)); else Trample(p, pos, forward, radius * 1.15f); }
                    else if (kind == 1) Fall(p, forward); else Crush(p, false);
                    break;
                }
            }
            return keep;
        }

        /// <summary>How much of its speed a hull keeps going through a thing: a tree or a wall stops it hard, a fence
        /// hardly, a telegraph pole not at all.</summary>
        static float Drag(Prop p)
        {
            if (p.what == What.Hedge) return Kursk ? 0.72f : 0.58f;   // bushes and wattle give: a hull shoulders through
            if (p.what == What.Tree) return 0.45f;
            if (p.what == What.Abatis) return 0.42f;
            if (p.what == What.Logs) return 0.5f;
            switch (p.kind.mesh)
            {
                case "tree_oak": case "tree_poplar": case "tree_poplar_b": case "tree_apple": case "tree_birch": case "spruce_snow": case "k_birches": return 0.45f;
                case "well_b": return 0.5f;
                case "barbed_wire": return 0.9f;
                case "fir_snow": case "fir_snow_b": case "pine_snow": return 0.45f;
                case "fir_young": return 0.75f;
                case "foxhole_logs": return 0.55f;
                case "trench": return 0.7f;
                case "deadtree": return 0.6f;
                case "pole": case "signpost": return 0.85f;
                case "wall_a": case "wall_b": return 0.4f;
                case "well": return 0.5f;
                case "sandbags": return 0.55f;
                case "cart": return 0.65f;
                case "k_well": return 0.7f;
                case "k_wattle": case "gate": return 0.75f;
                case "haystack": case "k_sheaves": case "barrels": return 0.8f;
                default: return 0.9f;
            }
        }

        /// <summary>Rubble under the tracks: a heap or a burnt-out husk is driven over, at half speed; a pressed-down
        /// hedge is crossed at four fifths.</summary>
        public float Rough(Vector3 pos)
        {
            if (InStream(pos, 0f) && Deck(pos) <= 0f) return 0.6f;
            foreach (var p in active)
            {
                if (p.what == What.Hedge)
                {
                    if (p.trodden == null) continue;
                    var dir = (p.b - p.a).normalized; var off = pos - p.a; off.y = 0f; float u = Vector3.Dot(off, dir);
                    if (u > 0f && u < p.size && (off - dir * u).sqrMagnitude < 1.8f * 1.8f && Crush(p, u, out _) > 0.5f) return 0.8f;
                    continue;
                }
                if (!p.drivable || p.radii.Length == 0) continue; float reach = p.bound + 3f; if ((p.pos - pos).sqrMagnitude > reach * reach) continue;
                for (int c = 0; c < p.radii.Length; c++) if ((new Vector2(pos.x, pos.z) - p.circleCenters[c]).sqrMagnitude < p.radii[c] * p.radii[c]) return 0.55f;
            }
            return 1f;
        }

        /// <summary>An explosion among the props: buildings and walls in reach lose hit points, trees go over away from
        /// it, fences and hay are flattened, fuel goes up, a heavy one opens a gap in a hedge.</summary>
        public void Blast(Vector3 at, float radius, float dmg)
        {
            for (int i = 0; i < active.Count; i++)
            {
                var p = active[i]; if (p.state != 0) continue;
                float reach = p.bound + radius; var d = p.pos - at; d.y = 0f; if (d.sqrMagnitude > reach * reach) continue;
                if (p.what == What.Forest) { BlastForest(p, at, radius, dmg); continue; }
                float near = p.radii.Length == 0 ? d.magnitude : float.MaxValue;
                for (int c = 0; c < p.radii.Length; c++) near = Mathf.Min(near, (new Vector2(at.x, at.z) - p.circleCenters[c]).magnitude - p.radii[c]);
                if (near > radius) continue;
                float k = 1f - Mathf.Clamp01(near / radius) * 0.6f; int kind = BreakKind(p);   // full at contact, less at the edge
                if (p.what == What.Hedge) { if (dmg >= 1.5f) Gap(p, at); }
                else if (kind == 1) { if (dmg * k >= 0.8f) Fall(p, d.sqrMagnitude > 0.01f ? d.normalized : Vector3.forward); }
                else if (kind == 2) { if (p.kind != null && p.kind.mesh == "barrels") Explode(p); else Crush(p, true); }
                else if (kind >= 3) Hurt(p, dmg * k, at);
            }
        }

        /// <summary>Shells and barrages in the forest: the firs within half the blast go over, away from it (at most five; one
        /// heard for them all), the others shed their snow.</summary>
        void BlastForest(Prop p, Vector3 at, float radius, float dmg)
        {
            if (dmg < 0.8f) return; float reach = radius * 0.55f; int felled = 0;
            for (int c = p.radii.Length - 1; c >= 0 && felled < 5; c--)
            {
                var cc = p.circleCenters[c]; var d = new Vector3(cc.x - at.x, 0f, cc.y - at.z); if (d.magnitude > reach) continue;
                FellFir(p, p.treeOf[c], d.sqrMagnitude > 0.01f ? d.normalized : Vector3.forward, felled == 0); felled++;
            }
        }

        /// <summary>A shell stopped by whatever Blocks found: a building or a wall loses hit points, a cart or hay is
        /// knocked flat, fuel drums go up.</summary>
        public void Strike(Vector3 at, float dmg)
        {
            var p = blocker; blocker = null; if (p == null || p.state != 0) return;
            if (p.what == What.Forest) { BlastForest(p, at, 3f, 1f); return; }   // a shell into a fir: it goes over
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
            bool wood = p.what == What.Tree || (p.kind != null && (p.kind.mesh.StartsWith("tree_") || p.kind.mesh.StartsWith("fir_") || p.kind.mesh == "pine_snow" || p.kind.mesh == "deadtree" || p.kind.mesh == "spruce_snow" || p.kind.mesh == "k_birches"));
            if (wood) Sfx.TreeFall(p.pos); else Sfx.Crunch(p.pos);   // a tree cracks and crashes down; a pole or a signpost just snaps
            if (p.go != null) { TwoSided(p); falling.Add(new Falling { p = p, rot0 = p.go.transform.rotation, axis = Vector3.Cross(Vector3.up, FallDir(p)) }); }
        }

        /// <summary>The two-sided mesh and back-culled material on any model (see TwoSided).</summary>
        void TwoSidedGo(GameObject go) { var tmp = new Prop { what = What.Model, go = go }; TwoSided(tmp); }

        /// <summary>A model tree lying down: its mesh made two-sided (each face also the other way round, normal flipped)
        /// under a material that culls back faces, so whichever side of a leaf faces up is lit. Made once per model.</summary>
        void TwoSided(Prop p)
        {
            if (p.what != What.Model || p.go == null) return;
            foreach (var mf in p.go.GetComponentsInChildren<MeshFilter>())
            {
                var src = mf.sharedMesh; if (src == null || !src.isReadable) continue;
                if (!twoSided.TryGetValue(src, out var both))
                {
                    var v = src.vertices; var n = src.normals; var uv = src.uv; var t = src.triangles; int c = v.Length;
                    var v2 = new Vector3[c * 2]; var n2 = new Vector3[c * 2]; var uv2 = new Vector2[c * 2]; var t2 = new int[t.Length * 2];
                    for (int i = 0; i < c; i++) { v2[i] = v2[i + c] = v[i]; if (n.Length == c) { n2[i] = n[i]; n2[i + c] = -n[i]; } if (uv.Length == c) uv2[i] = uv2[i + c] = uv[i]; }
                    for (int i = 0; i < t.Length; i += 3) { t2[i] = t[i]; t2[i + 1] = t[i + 1]; t2[i + 2] = t[i + 2]; int j = t.Length + i; t2[j] = t[i] + c; t2[j + 1] = t[i + 2] + c; t2[j + 2] = t[i + 1] + c; }
                    both = new Mesh { indexFormat = c * 2 > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16, name = src.name + " two-sided" };
                    both.vertices = v2; both.normals = n2; both.uv = uv2; both.triangles = t2; both.RecalculateBounds(); both.UploadMeshData(true); twoSided[src] = both;
                }
                mf.sharedMesh = both;
                var r = mf.GetComponent<Renderer>(); if (r == null) continue;
                if (!backCulled.TryGetValue(r.sharedMaterial, out var m)) { m = new Material(r.sharedMaterial); m.SetFloat("_Cull", 2f); backCulled[r.sharedMaterial] = m; }
                r.sharedMaterial = m;
            }
        }
        readonly Dictionary<Mesh, Mesh> twoSided = new Dictionary<Mesh, Mesh>(); readonly Dictionary<Material, Material> backCulled = new Dictionary<Material, Material>();

        /// <summary>A trench caved in: its walls sunk almost level with the field, a low broken line left showing.</summary>
        static void CaveIn(Prop p, GameObject go) { go.transform.localScale = new Vector3(1.05f, TrenchFlat * 0.35f, 1.15f); go.transform.position = p.pos + Vector3.up * (TrenchSink * 0.4f); }

        void Crush(Prop p, bool byShell)
        {
            if (p.state != 0) return;
            p.state = 2; p.radii = new float[0]; p.circleCenters = new Vector2[0]; p.height = -1f;
            if (byShell && p.kind != null && Burns(p.kind.mesh)) p.burn = 12f + Random.value * 10f;
            if (p.what == What.Abatis && p.go != null) { p.go.transform.localScale = new Vector3(1f, 0.3f, 1f); if (fx != null) fx.Dust(p.pos); Sfx.TreeFall(p.pos); return; }
            if (p.what == What.Logs && p.go != null) { p.go.transform.localScale = new Vector3(1f, 0.35f, 1f); if (fx != null) fx.Dust(p.pos); Sfx.Crunch(p.pos); return; }
            if (p.kind != null && p.kind.mesh == "trench")
            {
                if (p.go != null) CaveIn(p, p.go);
                var f = new Vector3(Mathf.Sin(p.yaw), 0f, Mathf.Cos(p.yaw));
                if (fx != null) { fx.Dust(p.pos - f * 2.5f); fx.Dust(p.pos + f * 2.5f); }
                Crater(p.pos - f * 2f, 3.6f); Crater(p.pos + f * 2f, 3.6f); Sfx.Crunch(p.pos); return;   // the earth churned where it was
            }
            if (p.go != null) p.go.transform.localScale = new Vector3(1f, 0.22f, 1f);
            if (fx != null) fx.Dust(p.pos);
            Sfx.Crunch(p.pos);
        }

        /// <summary>A building coming down: it sinks into its own dust and a heap of rubble is left in its place.</summary>
        void Collapse(Prop p)
        {
            p.state = 3; if (Burns(p.kind.mesh) || Random.value < 0.35f) p.burn = 18f + Random.value * 14f;
            if (prefabs.ContainsKey(p.kind.mesh + "_ruin")) { p.height = 2.2f; for (int c = 0; c < p.radii.Length; c++) p.radii[c] *= 0.75f; }   // a ruin with walls standing: tanks go round it, low shells stop in it
            else { p.drivable = true; p.height = 1.2f; }   // a heap: driven over
            if (fx != null) fx.Collapse(p.pos, p.kind.length);
            Sfx.Collapse(p.pos);
            if (p.kind.mesh == "bunker") bunkerDown?.Invoke(p.pos);
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
            HedgeCircles(h);
            if (fx != null) { fx.Dust(h.a + dir * u); fx.Dust(h.a + dir * (u + 2f)); }
            if (h.go != null) { Unload(h); Spawn(h); }
        }

        /// <summary>A hull shouldering through a hedge: the bushes under it go down, as wide as the hull and no wider,
        /// pressed flat the way it is going, and stay down for the night; torn sprigs fly up ahead, the bushes rustle and
        /// crack. half is half the width pressed flat.</summary>
        void Trample(Prop h, Vector3 hull, Vector3 forward, float half)
        {
            var dir = (h.b - h.a).normalized; float u = Mathf.Clamp(Vector3.Dot(hull - h.a, dir), 0f, h.size);
            if (h.trodden != null) foreach (var t in h.trodden) if (Mathf.Abs(u - t.x) < 0.5f) return;
            var sideW = new Vector3(dir.z, 0f, -dir.x); float down = Vector3.Dot(forward, sideW) >= 0f ? 1f : -1f;
            if (h.trodden == null) h.trodden = new List<Vector3>();
            h.trodden.Add(new Vector3(u, half, down)); HedgeCircles(h);
            var at = h.a + dir * u;
            if (fx != null) fx.Leaves(new Vector3(at.x, 1f, at.z), forward, winter ? new Color(0.62f, 0.6f, 0.58f) : Kursk ? new Color(0.78f, 0.66f, 0.46f) : new Color(0.72f, 0.8f, 0.62f));
            Sfx.Brush(at); if (Battle.QaOn) Battle.QaNote("hedge", at);
            if (h.go == null) return;
            string hm = Kursk ? "k_wattle" : "hedge";
            if (!prefabs.ContainsKey(hm) || PlayerPrefs.GetInt("quality", 1) == 0) { Unload(h); Spawn(h); return; }   // the blob hedge of the low setting is built again, pressed
            float reach = (hm == "hedge" ? 7.6f : 5.8f) * 0.5f + half + 0.9f; var hit = new List<Transform>();
            foreach (Transform sec in h.go.transform) if (Mathf.Abs(Vector3.Dot(sec.position - h.a, dir) - u) < reach) hit.Add(sec);
            foreach (var whole in hit)
            {
                var sec = whole;
                if (sec.childCount == 0 || sec.GetChild(0).name != "Slice")   // still in one piece: cut into its slices, laid exactly where it stood
                {
                    var cut = SlicedSection(hm, h.go.transform).transform; cut.SetPositionAndRotation(sec.position, sec.rotation); cut.localScale = sec.localScale; Destroy(sec.gameObject); sec = cut;
                }
                StartCoroutine(PressSlices(h, sec));
            }
        }

        const int SliceCount = 8;
        static readonly Dictionary<string, Mesh[]> slices = new Dictionary<string, Mesh[]>(); static readonly Dictionary<string, float[]> sliceAt = new Dictionary<string, float[]>();

        /// <summary>The hedge model cut crosswise into eight slices, each its own mesh standing on its own base, so that a
        /// hull presses down only the ones under it. Cut once and shared by every hedge.</summary>
        Mesh[] Slices(string hm, out float[] at)
        {
            if (slices.TryGetValue(hm, out var s) && s[0] != null) { at = sliceAt[hm]; return s; }
            var mf = prefabs[hm].GetComponentInChildren<MeshFilter>(); var src = mf.sharedMesh;
            var toRoot = prefabs[hm].transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
            var v = src.vertices; var n = src.normals; var uv = src.uv; bool hasN = n.Length == v.Length, hasUv = uv.Length == v.Length;
            float z0 = float.MaxValue, z1 = float.MinValue;
            for (int i = 0; i < v.Length; i++) { v[i] = toRoot.MultiplyPoint3x4(v[i]); if (hasN) n[i] = toRoot.MultiplyVector(n[i]).normalized; z0 = Mathf.Min(z0, v[i].z); z1 = Mathf.Max(z1, v[i].z); }
            float w = (z1 - z0) / SliceCount; var tris = new List<int>[SliceCount]; for (int k = 0; k < SliceCount; k++) tris[k] = new List<int>();
            for (int sm = 0; sm < src.subMeshCount; sm++)
            {
                var t = src.GetTriangles(sm);
                for (int i = 0; i + 2 < t.Length; i += 3) { int k = Mathf.Clamp((int)(((v[t[i]].z + v[t[i + 1]].z + v[t[i + 2]].z) / 3f - z0) / w), 0, SliceCount - 1); tris[k].Add(t[i]); tris[k].Add(t[i + 1]); tris[k].Add(t[i + 2]); }
            }
            s = new Mesh[SliceCount]; at = new float[SliceCount];
            for (int k = 0; k < SliceCount; k++)
            {
                float zc = z0 + (k + 0.5f) * w; at[k] = zc; var map = new Dictionary<int, int>(); var sv = new List<Vector3>(); var sn = new List<Vector3>(); var suv = new List<Vector2>(); var st = new List<int>();
                foreach (int i in tris[k])
                {
                    if (!map.TryGetValue(i, out int j)) { j = sv.Count; map[i] = j; sv.Add(v[i] - new Vector3(0f, 0f, zc)); if (hasN) sn.Add(n[i]); if (hasUv) suv.Add(uv[i]); }
                    st.Add(j);
                }
                var m = new Mesh { name = hm + " slice " + k, indexFormat = sv.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
                m.SetVertices(sv); if (hasN) m.SetNormals(sn); if (hasUv) m.SetUVs(0, suv); m.SetTriangles(st, 0); m.RecalculateBounds(); m.UploadMeshData(true);
                s[k] = m;
            }
            slices[hm] = s; sliceAt[hm] = at; return s;
        }

        /// <summary>A hedge section built of its slices, each where it sits in the whole model.</summary>
        GameObject SlicedSection(string hm, Transform parent)
        {
            var ms = Slices(hm, out var at); var sec = new GameObject("Section"); sec.transform.SetParent(parent, false);
            for (int k = 0; k < ms.Length; k++)
            {
                var sl = new GameObject("Slice"); sl.transform.SetParent(sec.transform, false); sl.transform.localPosition = new Vector3(0f, 0f, at[k]);
                sl.AddComponent<MeshFilter>().sharedMesh = ms[k]; var r = sl.AddComponent<MeshRenderer>(); r.sharedMaterial = materials[hm]; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
            return sec;
        }

        /// <summary>How a slice lies where the hedge is pressed down: squashed, spread a little and leaning away from the
        /// hull, in the section's own axes (a section may stand turned end for end).</summary>
        static void SlicePose(Prop h, Transform sec, Transform sl, out Vector3 pos, out Quaternion rot, out Vector3 scale)
        {
            var dir = (h.b - h.a).normalized; float c = Crush(h, Vector3.Dot(sl.position - h.a, dir), out float lean);
            float s = sec.InverseTransformDirection(new Vector3(dir.z, 0f, -dir.x) * lean).x >= 0f ? 1f : -1f;
            pos = new Vector3(s * 0.45f * c, 0f, sl.localPosition.z); rot = Quaternion.Euler(0f, 0f, -s * 22f * c); scale = new Vector3(1f + 0.25f * c, 1f - 0.7f * c, 1f);
        }

        /// <summary>A section's slices laid as far down as the hedge is pressed, at once: a section coming back into view.</summary>
        static void LaySlices(Prop h, Transform sec)
        {
            foreach (Transform sl in sec) { SlicePose(h, sec, sl, out var p, out var r, out var s); sl.localPosition = p; sl.localRotation = r; sl.localScale = s; }
        }

        /// <summary>The slices under a hull going down, in a third of a second.</summary>
        System.Collections.IEnumerator PressSlices(Prop h, Transform sec)
        {
            int n = sec.childCount; var p0 = new Vector3[n]; var r0 = new Quaternion[n]; var s0 = new Vector3[n]; var p1 = new Vector3[n]; var r1 = new Quaternion[n]; var s1 = new Vector3[n];
            for (int i = 0; i < n; i++) { var sl = sec.GetChild(i); p0[i] = sl.localPosition; r0[i] = sl.localRotation; s0[i] = sl.localScale; SlicePose(h, sec, sl, out p1[i], out r1[i], out s1[i]); }
            for (float a = 0f; a < 1f; a += Time.deltaTime / 0.35f)
            {
                if (sec == null) yield break;   // its cell was unloaded: it is laid pressed when it comes back
                float e = 1f - (1f - a) * (1f - a) * (1f - a);
                for (int i = 0; i < n; i++) { var sl = sec.GetChild(i); sl.localPosition = Vector3.Lerp(p0[i], p1[i], e); sl.localRotation = Quaternion.Slerp(r0[i], r1[i], e); sl.localScale = Vector3.Lerp(s0[i], s1[i], e); }
                yield return null;
            }
            if (sec == null) yield break;
            for (int i = 0; i < n; i++) { var sl = sec.GetChild(i); sl.localPosition = p1[i]; sl.localRotation = r1[i]; sl.localScale = s1[i]; }
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

        /// <summary>A bridge's sides: a circle every two metres along each parapet, but for the last three and a half metres
        /// at either end where the lane comes on; set out wide enough that a hull on the roadway passes between them.</summary>
        static void Parapets(Prop b)
        {
            float half = b.kind.length * 0.5f, side = b.kind.mesh == "bridge_stone" ? 3.4f : 2.6f; var centers = new List<Vector2>(); var radii = new List<float>();
            for (float z = -half + 3.5f; z <= half - 3.5f + 0.01f; z += 2f) foreach (float s in new[] { -side, side }) { centers.Add(new Vector2(b.pos.x + s, b.pos.z + z)); radii.Add(0.35f); }   // the bridges lie along +Z
            b.circleCenters = centers.ToArray(); b.radii = radii.ToArray();
        }

        /// <summary>The height of a bridge's roadway under a point: up a ramp from either bank to the deck, 0 off it.</summary>
        public float Deck(Vector3 pos)
        {
            float best = 0f;
            foreach (var b in bridges)
            {
                var d = pos - b.pos; float along = Mathf.Abs(d.z), across = Mathf.Abs(d.x), half = b.kind.length * 0.5f;   // the bridges lie along +Z
                if (along > half || across > 3.2f) continue;
                best = Mathf.Max(best, BridgeDeck(b.kind.mesh) * Mathf.SmoothStep(0f, 1f, (half - along) / 3.5f));
            }
            return best;
        }
        static float BridgeDeck(string mesh) => mesh == "bridge_stone" ? StoneDeck : WoodDeck;
        const float StoneDeck = 0.68f, WoodDeck = 0.66f, BridgeFlat = 0.25f;   // the roadway's height once the bridge is flattened to BridgeFlat of its own
        readonly List<Prop> bridges = new List<Prop>();
        const float TrenchFlat = 0.5f, TrenchSink = -0.45f;   // the trench model is a block of earth: squashed and sunk it reads as dug in

        /// <summary>How deep a hull sits in a stream at a point: up to FordDepth mid-stream, shallowing to the banks.</summary>
        static float Ford(Vector3 pos)
        {
            int iz = Mathf.RoundToInt((pos.z - Half) / Cell); float deep = 0f;
            for (int k = iz - 1; k <= iz + 1; k++)
            {
                if (!StreamZ(k)) continue;
                float off = Mathf.Abs(pos.z - StreamAt(k, pos.x)) / (StreamHalf + 1.5f); if (off < 1f) deep = Mathf.Max(deep, FordDepth * Mathf.SmoothStep(0f, 1f, 1f - off));
            }
            return deep;
        }
        const float FordDepth = 0.9f;

        /// <summary>Lifts a vehicle onto a bridge's roadway, and lets it down into a stream it fords; the ground elsewhere.</summary>
        public void Ride(Transform v)
        {
            var p = v.position; float y = Deck(p); if (y <= 0f) y = -Ford(p); if (Mathf.Abs(p.y - y) > 0.001f) v.position = new Vector3(p.x, Mathf.MoveTowards(p.y, y, 3f * Time.deltaTime), p.z);
        }

        /// <summary>The water and its muddy banks over one cell's forty metres of a stream: two strips following the
        /// bends, the banks under the water and wider.</summary>
        void SpawnStream(Prop p)
        {
            var go = new GameObject("Stream"); go.transform.SetParent(transform, false); go.transform.position = Vector3.zero;
            p.mesh = Strip(p.pos.x - Half - 1f, p.pos.x + Half + 1f, p.seed, StreamHalf, 0.05f); p.leaves = Strip(p.pos.x - Half - 1f, p.pos.x + Half + 1f, p.seed, StreamHalf + 2.6f, 0.035f);   // the banks' mesh rides in the leaves slot, so Unload frees it too
            var w = new GameObject("Water"); w.transform.SetParent(go.transform, false); w.AddComponent<MeshFilter>().sharedMesh = p.mesh; var wr = w.AddComponent<MeshRenderer>(); wr.sharedMaterial = waterMaterial; wr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var bk = new GameObject("Banks"); bk.transform.SetParent(go.transform, false); bk.AddComponent<MeshFilter>().sharedMesh = p.leaves; var br = bk.AddComponent<MeshRenderer>(); br.sharedMaterial = bankMaterial; br.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            p.go = go;
        }
        Material waterMaterial, bankMaterial;

        static Mesh Strip(float x0, float x1, int iz, float half, float y)
        {
            int n = Mathf.CeilToInt((x1 - x0) / 2f) + 1; var v = new Vector3[n * 2]; var uv = new Vector2[n * 2]; var nm = new Vector3[n * 2]; var t = new int[(n - 1) * 6];
            for (int i = 0; i < n; i++)
            {
                float x = Mathf.Lerp(x0, x1, i / (float)(n - 1)), z = StreamAt(iz, x), w = half * (1f + 0.15f * Mathf.Sin(x * 0.21f + iz));
                v[i * 2] = new Vector3(x, y, z - w); v[i * 2 + 1] = new Vector3(x, y, z + w); uv[i * 2] = new Vector2(0f, x / 6f); uv[i * 2 + 1] = new Vector2(1f, x / 6f); nm[i * 2] = nm[i * 2 + 1] = Vector3.up;
                if (i < n - 1) { int k = i * 6, a = i * 2; t[k] = a; t[k + 1] = a + 1; t[k + 2] = a + 2; t[k + 3] = a + 2; t[k + 4] = a + 1; t[k + 5] = a + 3; }
            }
            var m = new Mesh { vertices = v, uv = uv, normals = nm, triangles = t }; m.RecalculateBounds(); return m;
        }

        /// <summary>What the last thing Blocks found is made of: 0 stone or earth, 1 wood or leaves, 2 steel.</summary>
        public int BlockerStuff()
        {
            var p = blocker; if (p == null) return 0;
            if (p.what == What.Tree || p.what == What.Hedge || p.what == What.Forest || p.what == What.Abatis || p.what == What.Logs) return 1;
            if (p.what == What.Searchlight || p.kind == null) return 2;
            switch (p.kind.mesh)
            {
                case "truck": case "truck_burnt": case "wreck": case "barrels": return 2;
                case "fir_snow": case "fir_snow_b": case "pine_snow": case "fir_young": case "foxhole_logs": case "sawmill":
                case "cart": case "gate": case "pole": case "signpost": case "haystack": case "k_wattle": case "k_sheaves": case "k_sunflowers": case "barbed_wire": case "deadtree": case "spruce_snow": case "k_birches": return 1;
                default: return p.kind.mesh.StartsWith("tree_") ? 1 : 0;
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

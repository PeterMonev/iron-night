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
        enum What { Model, Field, Lane, Hedge, Tree, Decal, Searchlight }
        class Prop
        {
            public What what; public Kind kind; public Vector3 pos; public float yaw, size, bound = 8f, height = -1f; public int seed;
            public Vector2[] circleCenters = new Vector2[0]; public float[] radii = new float[0];
            public Vector3 a, b; public float[] gaps; public float clearA, clearB;              // hedges: the line and its openings
            public GameObject go; public Mesh mesh; public LightShaft shaft; public Transform yoke, drum, lamp; public float flakTimer = 4f; public int burst;
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
        };

        const float Cell = 40f, Half = 20f;
        readonly Dictionary<long, List<Prop>> cells = new Dictionary<long, List<Prop>>();
        readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
        readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        readonly List<Prop> active = new List<Prop>(); readonly HashSet<Prop> wanted = new HashSet<Prop>();
        Transform cam; Material patchMaterial, laneMaterial, yardMaterial, craterMaterial, hedgeMaterial, canopyMaterial, trunkMaterial;
        Material[] fieldMaterials; Mesh[] blobs; GameObject lampTemplate;
        readonly List<GameObject> craters = new List<GameObject>(); int nextCrater;   // shell craters of the night, oldest reused
        public Fx fx;

        public bool winter;   // the Ardennes: snow on the fields, bare trees

        public void Build(Camera camera)
        {
            cam = camera.transform; LightShaft.cam = camera; string sn = winter ? "_snow" : "";
            var lit = Resources.Load<Material>("VehicleLit"); var groundLit = Resources.Load<Material>("GroundLit"); var decal = Resources.Load<Material>("GroundDecal");
            foreach (var k in Kinds)
            {
                var pf = Resources.Load<GameObject>("Props/" + k.mesh); if (pf == null) continue;
                prefabs[k.mesh] = pf;
                var m = new Material(lit); m.SetTexture("_BaseMap", Resources.Load<Texture2D>("Props/" + k.mesh + "_tex")); m.SetColor("_BaseColor", Tint(k.mesh)); m.SetFloat("_Smoothness", 0.15f); m.SetFloat("_Cull", 0f);
                materials[k.mesh] = m;
            }
            fieldMaterials = new Material[4]; string[] names = { "field" + sn + "_plough", "field" + sn + "_pasture", "field" + sn + "_mown", "field" + sn + "_stubble" };
            for (int i = 0; i < 4; i++) { var m = new Material(groundLit); m.SetTexture("_BaseMap", Resources.Load<Texture2D>("Textures/" + names[i])); m.SetColor("_BaseColor", new Color(0.95f, 0.95f, 0.95f)); fieldMaterials[i] = m; }
            Material Decal(string tex, int queue) { var m = new Material(decal); m.SetTexture("_BaseMap", Resources.Load<Texture2D>("Textures/" + tex)); m.SetColor("_BaseColor", new Color(0.95f, 0.95f, 0.95f)); m.renderQueue = queue; return m; }
            yardMaterial = Decal("yard" + sn, 2440); laneMaterial = Decal("lane" + sn, 2442); laneMaterial.SetTextureScale("_BaseMap", new Vector2(1f, 2f)); craterMaterial = Decal("crater", 2446);
            patchMaterial = new Material(Resources.Load<Material>("Smoke")); patchMaterial.SetTexture("_BaseMap", Lightswarm.ProceduralSprites.GroundShadow(128).texture); patchMaterial.SetColor("_BaseColor", new Color(0.05f, 0.04f, 0.03f, 0.3f)); patchMaterial.renderQueue = 2450;
            hedgeMaterial = new Material(lit); hedgeMaterial.SetTexture("_BaseMap", Resources.Load<Texture2D>("Textures/hedge")); hedgeMaterial.SetColor("_BaseColor", winter ? new Color(0.72f, 0.78f, 0.82f) : new Color(0.9f, 0.95f, 0.85f)); hedgeMaterial.SetFloat("_Smoothness", 0.08f); hedgeMaterial.SetFloat("_Cull", 0f);
            canopyMaterial = new Material(hedgeMaterial); canopyMaterial.SetColor("_BaseColor", new Color(0.95f, 1f, 0.8f));
            trunkMaterial = new Material(Resources.Load<Material>("BarrelLit")); trunkMaterial.SetColor("_BaseColor", new Color(0.26f, 0.21f, 0.15f)); trunkMaterial.SetFloat("_Smoothness", 0.1f); trunkMaterial.SetFloat("_Metallic", 0f);
            blobs = new Mesh[4]; for (int i = 0; i < 4; i++) blobs[i] = Blob(11 + i * 7);
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
        static bool Farm(int ix, int iz) => (ix == 1 && iz == 1) || (!Start(ix, iz) && Rnd(ix, iz, 930) < 0.09f);
        static bool Battery(int ix, int iz) => (ix == -1 && iz == 0) || (!Start(ix, iz) && !Farm(ix, iz) && Rnd(ix, iz, 940) < 0.16f);
        static bool Village(int ix, int iz) => (ix == 0 && iz == 3) || (!Start(ix, iz) && !Farm(ix, iz) && !Battery(ix, iz) && Rnd(ix, iz, 1100) < 0.05f);
        static bool HedgeX(int ix, int iz) { if (LaneX(ix)) return false; if (Farm(ix, iz) || Farm(ix + 1, iz)) return true; return Rnd(ix, iz, 920) < (FieldType(ix, iz) != FieldType(ix + 1, iz) ? 0.85f : 0.3f); }
        static bool HedgeZ(int ix, int iz) { if (LaneZ(iz)) return false; if (Farm(ix, iz) || Farm(ix, iz + 1)) return true; return Rnd(ix, iz, 921) < (FieldType(ix, iz) != FieldType(ix, iz + 1) ? 0.85f : 0.3f); }
        static Vector3 In(int ix, int iz, int salt, float r) => new Vector3((Rnd(ix, iz, salt) - 0.5f) * 2f * r, 0f, (Rnd(ix, iz, salt + 1) - 0.5f) * 2f * r);

        // the generated textures come out at different brightnesses; this evens them under the moon
        static Color Tint(string mesh) { switch (mesh) { case "deadtree": return new Color(0.78f, 0.72f, 0.66f); case "haystack": return new Color(0.82f, 0.76f, 0.6f); case "sandbags": return new Color(0.78f, 0.74f, 0.66f); default: return new Color(0.8f, 0.78f, 0.74f); } }

        Kind K(string mesh) { foreach (var k in Kinds) if (k.mesh == mesh) return k; return null; }

        Prop Place(List<Prop> list, string mesh, Vector3 pos, float yaw)
        {
            var kind = K(mesh); if (kind == null || !prefabs.ContainsKey(mesh)) return null;
            foreach (var o in list) if (o.what == What.Model && (o.pos - pos).sqrMagnitude < 5f * 5f) return null;
            var p = new Prop { what = What.Model, kind = kind, pos = pos, yaw = yaw, height = kind.height, bound = kind.length };
            int n = kind.circles.Length / 3; p.circleCenters = new Vector2[n]; p.radii = new float[n];
            var f = new Vector2(Mathf.Sin(yaw), Mathf.Cos(yaw)); var r = new Vector2(f.y, -f.x);
            for (int c = 0; c < n; c++) { p.circleCenters[c] = new Vector2(pos.x, pos.z) + f * kind.circles[c * 3] + r * kind.circles[c * 3 + 1]; p.radii[c] = kind.circles[c * 3 + 2]; }
            list.Add(p); return p;
        }

        void Tree(List<Prop> list, Vector3 pos, int seed)
        {
            if (winter) { Place(list, "deadtree", pos, (seed % 360) * Mathf.Deg2Rad); return; }   // bare in the snow
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
            if (Rnd(ix, iz, 968 + salt) < 0.55f)
            {
                int n = 1 + (int)(Rnd(ix, iz, 969 + salt) * 2f); var side = new Vector3(dir.z, 0f, -dir.x);
                for (int i = 0; i < n; i++) { float u = len * (0.1f + 0.8f * (i + Rnd(ix, iz, 970 + salt + i)) / n); if (Open(p, u)) continue; Tree(list, a + dir * u + side * 0.4f, (int)(Hash(ix, iz, 975 + salt + i) & 0xffff)); }
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
            Place(list, "barn", o + f * 9.5f + r * 3f, yaw + Mathf.PI / 2f);
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
            if (Rnd(ix, iz, 1127) < 0.6f) Place(list, "truck", o + r * 6f - f * 9f, yaw + 0.4f);
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
        }

        /// <summary>Hay bales in the mown and stubble fields, a wreck or a crater anywhere, a lone tree, a dead one.</summary>
        void Loose(List<Prop> list, int ix, int iz, Vector3 c, int type)
        {
            if (type >= 2) { int n = (int)(Rnd(ix, iz, 1010) * 4f); for (int h = 0; h < n; h++) Place(list, "haystack", c + In(ix, iz, 1011 + h * 2, 13f), Rnd(ix, iz, 1030 + h) * 6.28f); }
            if (Rnd(ix, iz, 1040) < 0.1f) Place(list, "truck", c + In(ix, iz, 1041, 12f), Rnd(ix, iz, 1043) * 6.28f);
            if (Rnd(ix, iz, 1044) < 0.1f) Place(list, "deadtree", c + In(ix, iz, 1045, 14f), Rnd(ix, iz, 1047) * 6.28f);
            if (Rnd(ix, iz, 1048) < 0.08f) Tree(list, c + In(ix, iz, 1049, 12f), (int)(Hash(ix, iz, 1051) & 0xffff));
            int craters = Rnd(ix, iz, 1052) < 0.35f ? 1 + (int)(Rnd(ix, iz, 1053) * 2f) : 0;
            for (int k = 0; k < craters; k++) list.Add(new Prop { what = What.Decal, seed = 1, pos = c + In(ix, iz, 1054 + k * 2, 16f), yaw = Rnd(ix, iz, 1060 + k) * 6.28f, size = 4f + Rnd(ix, iz, 1064 + k) * 3f });
            if (Rnd(ix, iz, 1070) < 0.05f) Place(list, "sandbags", c + In(ix, iz, 1071, 12f), Rnd(ix, iz, 1073) * 6.28f);
        }

        List<Prop> CellProps(int ix, int iz)
        {
            long key = ((long)ix << 32) ^ (uint)iz; if (cells.TryGetValue(key, out var list)) return list;
            list = new List<Prop>(); var c = new Vector3(ix * Cell, 0f, iz * Cell); int type = FieldType(ix, iz);
            list.Add(new Prop { what = What.Field, pos = c, seed = type, yaw = (Hash(ix, iz, 950) % 4) * Mathf.PI / 2f, size = Cell });
            // the lanes and hedges on the cell's east and north lines; the west and south ones belong to the neighbours
            if (LaneX(ix)) { list.Add(new Prop { what = What.Lane, pos = c + new Vector3(Half, 0f, 0f), yaw = 0f, size = Cell }); foreach (var u in new[] { -12f, 8f }) Place(list, "pole", c + new Vector3(Half + 3.6f, 0f, u), 0f); }
            if (LaneZ(iz)) { list.Add(new Prop { what = What.Lane, pos = c + new Vector3(0f, 0f, Half), yaw = Mathf.PI / 2f, size = Cell }); foreach (var u in new[] { -12f, 8f }) Place(list, "pole", c + new Vector3(u, 0f, Half + 3.6f), Mathf.PI / 2f); }
            if (HedgeX(ix, iz)) Hedge(list, c + new Vector3(Half, 0f, -Half), c + new Vector3(Half, 0f, Half), LaneZ(iz - 1) ? 4f : 0f, LaneZ(iz) ? 4f : 0f, ix, iz, 0);
            if (HedgeZ(ix, iz)) Hedge(list, c + new Vector3(-Half, 0f, Half), c + new Vector3(Half, 0f, Half), LaneX(ix - 1) ? 4f : 0f, LaneX(ix) ? 4f : 0f, ix, iz, 1);
            if (Farm(ix, iz)) FarmYard(list, ix, iz, c);
            else if (Battery(ix, iz)) SearchlightPost(list, ix, iz, c);
            else if (Village(ix, iz)) VillageSquare(list, ix, iz, c);
            else if (!Start(ix, iz)) Loose(list, ix, iz, c, type);
            cells[key] = list; return list;
        }

        // ---- objects: only the cells around the camera exist ----

        /// <summary>Keeps objects alive in the 5x5 cells around the camera's ground point and turns the searchlights.</summary>
        public void Tick()
        {
            var g = cam.position + cam.forward * 45f; int cx = Mathf.RoundToInt(g.x / Cell), cz = Mathf.RoundToInt(g.z / Cell);
            wanted.Clear();
            for (int ix = cx - 2; ix <= cx + 2; ix++) for (int iz = cz - 2; iz <= cz + 2; iz++) foreach (var p in CellProps(ix, iz)) wanted.Add(p);
            for (int i = active.Count - 1; i >= 0; i--) if (!wanted.Contains(active[i])) { Unload(active[i]); active.RemoveAt(i); }
            foreach (var p in wanted) if (p.go == null) { Spawn(p); active.Add(p); }
            float time = Time.time;
            foreach (var p in active)
            {
                if (p.what != What.Searchlight) continue;
                // the beam wanders round the sky between 22 and 62 degrees up; the lamp glow faces the camera
                float ph = p.seed * 0.37f, az = time * 7f * (p.seed % 2 == 0 ? 1f : -1f) + ph * 57f, el = 42f + Mathf.Sin(time * 0.17f + ph) * 20f;
                p.yoke.localRotation = Quaternion.Euler(0f, az, 0f); p.drum.localRotation = Quaternion.Euler(-el, 0f, 0f);
                p.lamp.rotation = cam.rotation;
                p.shaft.Set(p.drum.position + p.drum.forward * 0.6f, p.drum.forward);
                // the post's gun fires a burst at the sky now and then: five tracers climbing along the beam
                p.flakTimer -= Time.deltaTime;
                if (p.flakTimer <= 0f) { if (p.burst == 0) p.burst = 5; p.flakTimer = p.burst > 1 ? 0.13f : 7f + Rnd(p.seed, (int)(time * 10f), 3) * 12f; p.burst--; if (fx != null) { var from = p.pos + Quaternion.Euler(0f, p.yaw * Mathf.Rad2Deg, 0f) * new Vector3(5f, 1.2f, 1f); fx.Flak(from, (p.drum.forward + Random.insideUnitSphere * 0.06f).normalized); if (p.burst == 4) Sfx.Flak(from); } }
            }
        }

        void Unload(Prop p)
        {
            if (p.shaft != null) Destroy(p.shaft.gameObject);
            Destroy(p.go); p.go = null; if (p.mesh != null) { Destroy(p.mesh); p.mesh = null; }
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
                case What.Field:
                {
                    p.go = Quad(transform, p.pos, yawDeg, Cell, Cell, fieldMaterials[p.seed], 0f); p.go.name = "Field";
                    var mpb = new MaterialPropertyBlock(); float v = 0.9f + (p.seed * 0.03f) + Rnd(p.seed, (int)p.pos.x, (int)p.pos.z) * 0.12f;
                    mpb.SetColor("_BaseColor", new Color(v, v, v * 0.98f)); p.go.GetComponent<Renderer>().SetPropertyBlock(mpb);
                    break;
                }
                case What.Lane: p.go = Quad(transform, p.pos, yawDeg, 6f, Cell, laneMaterial, 0.02f); p.go.name = "Lane"; break;
                case What.Decal: p.go = Quad(transform, p.pos, yawDeg, p.size, p.size, p.seed == 0 ? yardMaterial : craterMaterial, p.seed == 0 ? 0.03f : 0.05f); p.go.name = p.seed == 0 ? "Yard" : "Crater"; break;
                case What.Hedge: SpawnHedge(p); break;
                case What.Tree: SpawnTree(p); break;
                case What.Searchlight: SpawnSearchlight(p); break;
                default:
                {
                    var go = Instantiate(prefabs[p.kind.mesh], transform); go.name = p.kind.mesh;
                    go.transform.position = p.pos; go.transform.rotation = Quaternion.Euler(0f, yawDeg, 0f);
                    foreach (var r in go.GetComponentsInChildren<Renderer>()) { r.sharedMaterial = materials[p.kind.mesh]; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
                    // trodden earth under it: a soft dark patch a little wider than the footprint
                    for (int c = 0; c < p.radii.Length; c++) { var q = Quad(go.transform, new Vector3(p.circleCenters[c].x, 0f, p.circleCenters[c].y), 0f, p.radii[c] * 2.4f, p.radii[c] * 2.4f, patchMaterial, 0.06f); q.GetComponent<Renderer>().receiveShadows = false; }
                    p.go = go; break;
                }
            }
        }

        /// <summary>Two staggered rows of bushes along the line, skipping the gates, combined into one mesh.</summary>
        void SpawnHedge(Prop p)
        {
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
            p.go = go; p.mesh = mesh;
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
            p.go = go; p.mesh = mesh;
        }

        void SpawnSearchlight(Prop p)
        {
            var go = Instantiate(lampTemplate, transform); go.SetActive(true); go.name = "Searchlight";
            go.transform.position = p.pos; go.transform.rotation = Quaternion.Euler(0f, p.yaw * Mathf.Rad2Deg, 0f);
            p.yoke = go.transform.Find("Yoke"); p.drum = p.yoke.Find("Drum"); p.lamp = p.drum.Find("Lamp");
            var shaft = new GameObject("Beam"); shaft.transform.SetParent(transform, false); p.shaft = shaft.AddComponent<LightShaft>();
            p.go = go;
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
                if (p.radii.Length == 0) continue; float reach = p.bound + 6f; if ((p.pos - pos).sqrMagnitude > reach * reach) continue;
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
                if (p.radii.Length == 0) continue; float reach = p.bound + 6f; if ((p.pos - pos).sqrMagnitude > reach * reach) continue;
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
                if (p.what != What.Model || p.kind.mesh != "sandbags") continue;
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
        public void Crater(Vector3 pos, float size)
        {
            GameObject q;
            if (craters.Count < 40) { q = Quad(transform, pos, Random.value * 360f, size, size, craterMaterial, 0.07f); q.name = "ShellCrater"; craters.Add(q); }
            else { q = craters[nextCrater]; nextCrater = (nextCrater + 1) % craters.Count; q.transform.position = new Vector3(pos.x, 0.07f, pos.z); q.transform.rotation = Quaternion.Euler(90f, Random.value * 360f, 0f); q.transform.localScale = new Vector3(size, size, 1f); }
        }

        /// <summary>True when a shell at this point is inside something solid that is taller than the shell's flight.</summary>
        public bool Blocks(Vector3 pos)
        {
            foreach (var p in active)
            {
                if (p.radii.Length == 0 || pos.y > p.height) continue; float reach = p.bound + 4f; if ((p.pos - pos).sqrMagnitude > reach * reach) continue;
                for (int c = 0; c < p.radii.Length; c++) if ((new Vector2(pos.x, pos.z) - p.circleCenters[c]).sqrMagnitude < p.radii[c] * p.radii[c]) return true;
            }
            return false;
        }
    }
}

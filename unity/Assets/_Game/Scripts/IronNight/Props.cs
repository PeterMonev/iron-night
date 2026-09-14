using System.Collections.Generic;
using UnityEngine;

namespace IronNight
{
    /// <summary>
    /// The things on the field: ruined farmhouses, barns, stone walls, wrecked trucks, hay bales, dead trees, sandbag
    /// positions. Scattered deterministically per 40 m cell from a hash, so the field is endless but the same every night;
    /// only cells near the camera exist as objects. Each prop is a set of circles on the ground that vehicles are pushed out
    /// of and that shells burst on.
    /// </summary>
    public class Props : MonoBehaviour
    {
        public class Kind { public string mesh; public float length, yaw; public float[] circles; public float height; public Color tint = Color.white; public float weight; }
        public class Prop { public Kind kind; public Vector3 pos; public float yaw; public GameObject go; public Vector2[] circleCenters; public float[] radii; }

        // circles: triples (offsetAlong, offsetSide, radius) in metres, along the prop's own forward axis
        public static readonly Kind[] Kinds =
        {
            new Kind { mesh = "farmhouse", length = 13f, height = 7f, weight = 0.45f, circles = new[] { -4f, 0f, 3.6f, 0f, 0f, 3.8f, 4f, 0f, 3.6f } },
            new Kind { mesh = "barn", length = 10f, height = 6.5f, weight = 0.4f, circles = new[] { -2.8f, 0f, 3f, 2.8f, 0f, 3f } },
            new Kind { mesh = "wall", length = 6.5f, height = 1.6f, weight = 2.2f, circles = new[] { -2.6f, 0f, 1f, -1.3f, 0f, 1f, 0f, 0f, 1f, 1.3f, 0f, 1f, 2.6f, 0f, 1f } },
            new Kind { mesh = "truck", length = 6f, height = 2.6f, weight = 0.7f, circles = new[] { -1.6f, 0f, 1.4f, 1.6f, 0f, 1.4f } },
            new Kind { mesh = "haystack", length = 3.2f, height = 2.2f, weight = 2.5f, circles = new[] { 0f, 0f, 1.7f } },
            new Kind { mesh = "deadtree", length = 6f, height = 7f, weight = 2f, circles = new[] { 0f, 0f, 0.7f } },
            new Kind { mesh = "sandbags", length = 5f, height = 1.2f, weight = 1f, circles = new[] { 0f, 0f, 2.2f } },
        };

        const float Cell = 40f;
        readonly Dictionary<long, List<Prop>> cells = new Dictionary<long, List<Prop>>();
        readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
        readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        readonly List<Prop> active = new List<Prop>();
        Transform cam; Material template;

        public void Build(Camera camera)
        {
            cam = camera.transform; template = Resources.Load<Material>("VehicleLit");
            foreach (var k in Kinds)
            {
                var pf = Resources.Load<GameObject>("Props/" + k.mesh); if (pf == null) continue;
                prefabs[k.mesh] = pf;
                var m = new Material(template); m.SetTexture("_BaseMap", Resources.Load<Texture2D>("Props/" + k.mesh + "_tex")); m.SetColor("_BaseColor", new Color(0.78f, 0.76f, 0.72f)); m.SetFloat("_Smoothness", 0.15f); m.SetFloat("_Cull", 0f);
                materials[k.mesh] = m;
            }
        }

        static uint Hash(int a, int b, int c) { uint h = 2166136261u; h = (h ^ (uint)a) * 16777619u; h = (h ^ (uint)b) * 16777619u; h = (h ^ (uint)c) * 16777619u; h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15; return h; }
        static float Rnd(int a, int b, int c) => (Hash(a, b, c) & 0xffffff) / 16777216f;

        List<Prop> CellProps(int ix, int iz)
        {
            long key = ((long)ix << 32) ^ (uint)iz; if (cells.TryGetValue(key, out var list)) return list;
            list = new List<Prop>();
            // the start area stays clear so the platoon can form up
            bool home = Mathf.Abs(ix) <= 0 && Mathf.Abs(iz) <= 0;
            int count = home ? 0 : 2 + (int)(Rnd(ix, iz, 1) * 3f);
            float total = 0f; foreach (var k in Kinds) if (prefabs.ContainsKey(k.mesh)) total += k.weight;
            for (int i = 0; i < count; i++)
            {
                float pick = Rnd(ix, iz, 10 + i) * total; Kind kind = null;
                foreach (var k in Kinds) { if (!prefabs.ContainsKey(k.mesh)) continue; pick -= k.weight; if (pick <= 0f) { kind = k; break; } }
                if (kind == null) continue;
                var pos = new Vector3((ix + Rnd(ix, iz, 20 + i)) * Cell, 0f, (iz + Rnd(ix, iz, 30 + i)) * Cell);
                float yaw = Rnd(ix, iz, 40 + i) * Mathf.PI * 2f;
                var p = new Prop { kind = kind, pos = pos, yaw = yaw };
                // keep props apart from each other inside the cell
                bool clash = pos.sqrMagnitude < 30f * 30f; foreach (var o in list) if ((o.pos - pos).sqrMagnitude < 14f * 14f) { clash = true; break; }
                if (clash) continue;
                int n = kind.circles.Length / 3; p.circleCenters = new Vector2[n]; p.radii = new float[n];
                var f = new Vector2(Mathf.Sin(yaw), Mathf.Cos(yaw)); var r = new Vector2(f.y, -f.x);
                for (int c = 0; c < n; c++) { p.circleCenters[c] = new Vector2(pos.x, pos.z) + f * kind.circles[c * 3] + r * kind.circles[c * 3 + 1]; p.radii[c] = kind.circles[c * 3 + 2]; }
                list.Add(p);
            }
            cells[key] = list; return list;
        }

        /// <summary>Keeps objects alive in the 5x5 cells around the camera's ground point.</summary>
        public void Tick()
        {
            var g = cam.position + cam.forward * 45f; int cx = Mathf.FloorToInt(g.x / Cell), cz = Mathf.FloorToInt(g.z / Cell);
            var wanted = new HashSet<Prop>();
            for (int ix = cx - 2; ix <= cx + 2; ix++) for (int iz = cz - 2; iz <= cz + 2; iz++) foreach (var p in CellProps(ix, iz)) wanted.Add(p);
            for (int i = active.Count - 1; i >= 0; i--) if (!wanted.Contains(active[i])) { Destroy(active[i].go); active[i].go = null; active.RemoveAt(i); }
            foreach (var p in wanted) if (p.go == null) { Spawn(p); active.Add(p); }
        }

        void Spawn(Prop p)
        {
            var go = Instantiate(prefabs[p.kind.mesh], transform); go.name = p.kind.mesh;
            go.transform.position = p.pos; go.transform.rotation = Quaternion.Euler(0f, p.yaw * Mathf.Rad2Deg, 0f);
            foreach (var r in go.GetComponentsInChildren<Renderer>()) { r.sharedMaterial = materials[p.kind.mesh]; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
            p.go = go;
        }

        /// <summary>Pushes a ground position out of every prop footprint it overlaps; returns the corrected position.</summary>
        public Vector3 PushOut(Vector3 pos, float radius)
        {
            for (int pass = 0; pass < 2; pass++) foreach (var p in active)
            {
                if ((p.pos - pos).sqrMagnitude > 20f * 20f) continue;
                for (int c = 0; c < p.radii.Length; c++)
                {
                    var d = new Vector2(pos.x, pos.z) - p.circleCenters[c]; float min = p.radii[c] + radius; float sq = d.sqrMagnitude;
                    if (sq < min * min && sq > 1e-4f) { float len = Mathf.Sqrt(sq); var push = d / len * (min - len); pos.x += push.x; pos.z += push.y; }
                }
            }
            return pos;
        }

        /// <summary>True when a shell at this point is inside something solid that is taller than the shell's flight.</summary>
        public bool Blocks(Vector3 pos)
        {
            foreach (var p in active)
            {
                if ((p.pos - pos).sqrMagnitude > 20f * 20f || pos.y > p.kind.height) continue;
                for (int c = 0; c < p.radii.Length; c++) if ((new Vector2(pos.x, pos.z) - p.circleCenters[c]).sqrMagnitude < p.radii[c] * p.radii[c]) return true;
            }
            return false;
        }
    }
}

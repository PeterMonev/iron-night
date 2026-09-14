using System.Collections.Generic;
using UnityEngine;

namespace IronNight
{
    /// <summary>
    /// Track marks: every vehicle leaves two soft dark dabs behind it each metre it drives. All marks live in one mesh
    /// (a ring buffer of quads, the oldest overwritten), so the whole field of ruts is a single draw call.
    /// </summary>
    public class Tracks : MonoBehaviour
    {
        const int Max = 500;
        Mesh mesh; readonly Vector3[] verts = new Vector3[Max * 4]; int next; bool dirty;
        readonly Dictionary<Vehicle, Vector3> last = new Dictionary<Vehicle, Vector3>();

        public void Build()
        {
            var uv = new Vector2[Max * 4]; var tris = new int[Max * 6];
            for (int i = 0; i < Max; i++)
            {
                int b = i * 4; uv[b] = new Vector2(0f, 0f); uv[b + 1] = new Vector2(1f, 0f); uv[b + 2] = new Vector2(1f, 1f); uv[b + 3] = new Vector2(0f, 1f);
                int t = i * 6; tris[t] = b; tris[t + 1] = b + 3; tris[t + 2] = b + 2; tris[t + 3] = b; tris[t + 4] = b + 2; tris[t + 5] = b + 1;
            }
            mesh = new Mesh { vertices = verts, uv = uv, triangles = tris }; mesh.MarkDynamic();
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var m = new Material(Resources.Load<Material>("Smoke")); m.SetTexture("_BaseMap", Lightswarm.ProceduralSprites.GroundShadow(64).texture); m.SetColor("_BaseColor", new Color(0.05f, 0.04f, 0.03f, 0.3f)); m.renderQueue = 2448;
            var mr = gameObject.AddComponent<MeshRenderer>(); mr.sharedMaterial = m; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
        }

        /// <summary>Call every frame for every moving vehicle; marks are dropped once per metre of travel.</summary>
        public void Mark(Vehicle v)
        {
            var p = v.transform.position;
            if (last.TryGetValue(v, out var l) && (p - l).sqrMagnitude < 1f) return;
            last[v] = p;
            var f = v.Forward; var r = new Vector3(f.z, 0f, -f.x); float half = v.spec.radius * 0.42f;
            Quad(p + r * half, f, 0.6f, 1.3f); Quad(p - r * half, f, 0.6f, 1.3f);
        }

        void Quad(Vector3 c, Vector3 f, float w, float l)
        {
            var r = new Vector3(f.z, 0f, -f.x); int b = next * 4; next = (next + 1) % Max; c.y = 0.045f;
            verts[b] = c - r * (w * 0.5f) - f * (l * 0.5f); verts[b + 1] = c + r * (w * 0.5f) - f * (l * 0.5f);
            verts[b + 2] = c + r * (w * 0.5f) + f * (l * 0.5f); verts[b + 3] = c - r * (w * 0.5f) + f * (l * 0.5f);
            dirty = true;
        }

        public void Forget(Vehicle v) { last.Remove(v); }

        void LateUpdate() { if (dirty) { mesh.vertices = verts; mesh.RecalculateBounds(); dirty = false; } }
    }
}

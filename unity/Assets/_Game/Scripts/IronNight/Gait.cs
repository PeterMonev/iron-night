using UnityEngine;

namespace IronNight
{
    /// <summary>
    /// A walk or a run made of two still figures in mid-stride: one with the left foot forward, one with the right
    /// (Props/inf_walk_a/b, inf_run_a/b). Each is turned into a few meshes with its legs swung in from the hips towards
    /// the passing pose, the body lifted as the legs straighten under it, and a man goes through them stride by stride:
    /// legs apart, closing, the other figure takes over where the legs pass, opening again. The meshes are made once
    /// when the night loads and shared by every soldier, so a step costs a man nothing but a mesh swap.
    /// </summary>
    public sealed class Gait
    {
        const int Levels = 8;   // from legs apart to passing, in eight steps: smooth enough to watch close up
        readonly Mesh[] a = new Mesh[Levels], b = new Mesh[Levels];
        public readonly Material matA, matB; public readonly float step;   // the metres one step covers

        Gait(Mesh ma, Mesh mb, Material ta, Material tb, float stepLength)
        {
            for (int i = 0; i < Levels; i++) { float k = 0.9f * i / (Levels - 1); a[i] = Close(ma, k); b[i] = Close(mb, k); }   // never quite together: the two legs would sit in one another
            matA = ta; matB = tb; step = stepLength;
        }

        /// <summary>The gait from Props/&lt;name&gt;_a and _b, or null when either is missing or its mesh is not readable.</summary>
        public static Gait Load(string name, float stepLength, Material template)
        {
            var ma = MeshOf("Props/" + name + "_a"); var mb = MeshOf("Props/" + name + "_b");
            if (ma == null || mb == null) return null;
            return new Gait(ma, mb, Skin(name + "_a", template), Skin(name + "_b", template), stepLength);
        }

        public static Mesh MeshOf(string path)
        {
            var go = Resources.Load<GameObject>(path); if (go == null) return null;
            var mf = go.GetComponentInChildren<MeshFilter>(); return mf != null && mf.sharedMesh != null && mf.sharedMesh.isReadable ? mf.sharedMesh : null;
        }

        public static Material Skin(string name, Material template)
        {
            var m = new Material(template); m.SetTexture("_BaseMap", Resources.Load<Texture2D>("Props/" + name + "_tex")); m.SetColor("_BaseColor", new Color(0.7f, 0.7f, 0.68f)); m.SetFloat("_Smoothness", 0.1f); m.SetFloat("_Cull", 0f);
            return m;
        }

        /// <summary>The figure at a point of its stride cycle: 0..1 is one step (the a figure's legs closing, then the
        /// b figure's opening), 1..2 the next (b closing, a opening).</summary>
        public Mesh Frame(float cycle, out Material mat)
        {
            float s = Mathf.Repeat(cycle, 2f) * 2f; int seg = Mathf.Min(3, (int)s); float u = s - seg;
            float closed = seg == 0 || seg == 2 ? u : 1f - u;   // closing in the first half of a step, opening in the second
            bool useA = seg == 0 || seg == 3; mat = useA ? matA : matB;
            int i = Mathf.Clamp(Mathf.RoundToInt(closed * (Levels - 1)), 0, Levels - 1);
            return useA ? a[i] : b[i];
        }

        static float Ramp(float x, float lo, float hi) { float t = Mathf.Clamp01((x - lo) / (hi - lo)); return t * t * (3f - 2f * t); }

        /// <summary>The figure with both legs swung from the hip towards straight down by the share k, and lifted so
        /// the lower foot still touches the ground. The legs are told apart by height: a point below the hips belongs
        /// to the leg whose line from the hip to its shin passes nearer it.</summary>
        static Mesh Close(Mesh src, float k)
        {
            var m = Object.Instantiate(src); m.name = src.name + " stride " + k.ToString("0.00");
            if (k <= 0f) { m.UploadMeshData(true); return m; }
            var v = src.vertices; var n = src.normals; bool hasN = n != null && n.Length == v.Length;
            float bottom = src.bounds.min.y, H = src.bounds.size.y, hipY = bottom + H * 0.5f, shinY = bottom + H * 0.22f;
            Vector3 hip = Vector3.zero, front = Vector3.zero, back = Vector3.zero; int nh = 0, nf = 0, nb = 0;
            foreach (var p in v) if (Mathf.Abs(p.y - hipY) < H * 0.05f) { hip += p; nh++; }
            hip = nh > 0 ? hip / nh : new Vector3(0f, hipY, 0f); hip.y = hipY;
            foreach (var p in v) { if (p.y > shinY) continue; if (p.z > hip.z) { front += p; nf++; } else { back += p; nb++; } }
            if (nf == 0 || nb == 0) { m.UploadMeshData(true); return m; }
            front /= nf; back /= nb;
            float angF = Mathf.Atan2(front.z - hip.z, hipY - front.y) * Mathf.Rad2Deg, angB = Mathf.Atan2(back.z - hip.z, hipY - back.y) * Mathf.Rad2Deg;
            var outV = new Vector3[v.Length]; var outN = hasN ? new Vector3[v.Length] : null; float low = float.MaxValue;
            for (int i = 0; i < v.Length; i++)
            {
                var p = v[i]; float w = Ramp(hipY + H * 0.02f - p.y, 0f, H * 0.14f);   // nothing at the hip joint, all of it from the upper thigh down
                if (w <= 0f) { outV[i] = p; if (hasN) outN[i] = n[i]; continue; }
                float down = hipY - p.y, zf = hip.z + (front.z - hip.z) * down / Mathf.Max(0.05f, hipY - front.y), zb = hip.z + (back.z - hip.z) * down / Mathf.Max(0.05f, hipY - back.y);
                float sep = Mathf.Abs(zf - zb) + 0.04f, t = Mathf.Clamp01(0.5f + (Mathf.Abs(p.z - zb) - Mathf.Abs(p.z - zf)) / sep);
                var q = Quaternion.AngleAxis(Mathf.Lerp(angB, angF, t) * k * w, Vector3.right);
                outV[i] = hip + q * (p - hip); if (hasN) outN[i] = q * n[i];
                if (outV[i].y < low) low = outV[i].y;
            }
            float lift = low < float.MaxValue ? bottom - low : 0f;   // straighter legs reach lower: the body rides up over them
            if (lift > 0f) for (int i = 0; i < outV.Length; i++) outV[i].y += lift;
            m.vertices = outV; if (hasN) m.normals = outN; m.RecalculateBounds(); m.UploadMeshData(true);
            return m;
        }
    }
}

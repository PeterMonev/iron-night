using UnityEngine;

namespace IronNight
{
    /// <summary>
    /// A figure from the crew pictures standing at ease: it breathes, shifts its weight from foot to foot, sways a little
    /// back and forth, turns at the waist and looks about. The mesh is one piece with no skeleton, so the moves are blend
    /// shapes made here from the figure's own heights: the chest rises and fills out, the body leans from the ankles, the
    /// upper body turns about the spine, the head about the neck, fading out beyond the head itself (a hand held to the
    /// ear follows it only in part). Heights are taken from the top of the figure down, so the commander cut off at the
    /// waist in his hatch moves the same way.
    /// </summary>
    public class CrewIdle : MonoBehaviour
    {
        const int Breath = 0, SwayR = 1, SwayL = 2, LeanF = 3, LeanB = 4, TwistL = 5, TwistR = 6, HeadL = 7, HeadR = 8, HeadDown = 9;
        SkinnedMeshRenderer skin; float seed, period, headAmp = 1f;
        float sway, swayTo, swayV, swayNext, head, headTo, headV, down, downTo, downV, headNext;

        /// <summary>Makes a standing figure (a prop with one mesh, read-enabled) move at ease. headAmp scales how far the
        /// head turns: less for a man holding a handset to his ear.</summary>
        public static void Bring(GameObject figure, int seed, float headAmp = 1f)
        {
            var mf = figure.GetComponentInChildren<MeshFilter>(); var mr = mf != null ? mf.GetComponent<MeshRenderer>() : null;
            if (mr == null || mf.sharedMesh == null || !mf.sharedMesh.isReadable) return;
            var go = new GameObject("Idle"); go.transform.SetParent(mf.transform, false);
            var s = go.AddComponent<SkinnedMeshRenderer>(); s.sharedMesh = Shapes(mf.sharedMesh); s.sharedMaterial = mr.sharedMaterial; s.shadowCastingMode = mr.shadowCastingMode;
            var b = s.sharedMesh.bounds; b.Expand(0.3f); s.localBounds = b; mr.enabled = false;
            var idle = figure.AddComponent<CrewIdle>(); idle.skin = s; idle.seed = seed * 13.7f + 3.1f; idle.headAmp = headAmp;
            idle.period = 3.6f + (seed % 4) * 0.35f; idle.swayTo = (seed % 2 == 0 ? 0.4f : -0.4f); idle.headNext = 1f + seed * 0.7f;
        }

        void Update()
        {
            if (skin == null) return;
            float t = Time.unscaledTime, dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            // breathing: about fifteen breaths a minute, each man his own
            skin.SetBlendShapeWeight(Breath, 50f + 50f * Mathf.Sin((t + seed) * 2f * Mathf.PI / period));
            // the weight from foot to foot: a new stance every seven to fourteen seconds, eased into, and a restless drift on top
            if (t > swayNext) { swayTo = Random.Range(-1f, 1f); swayNext = t + Random.Range(7f, 14f); }
            sway = Mathf.SmoothDamp(sway, swayTo, ref swayV, 1.8f, Mathf.Infinity, dt);
            float s = Mathf.Clamp(sway + (Mathf.PerlinNoise(seed, t * 0.2f) - 0.5f) * 0.5f, -1f, 1f);
            skin.SetBlendShapeWeight(SwayR, Mathf.Max(0f, s) * 100f); skin.SetBlendShapeWeight(SwayL, Mathf.Max(0f, -s) * 100f);
            float l = (Mathf.PerlinNoise(seed + 5.3f, t * 0.11f) - 0.5f) * 2f;
            skin.SetBlendShapeWeight(LeanF, Mathf.Max(0f, l) * 100f); skin.SetBlendShapeWeight(LeanB, Mathf.Max(0f, -l) * 100f);
            float w = (Mathf.PerlinNoise(seed + 9.1f, t * 0.08f) - 0.5f) * 2f;
            skin.SetBlendShapeWeight(TwistL, Mathf.Max(0f, w) * 100f); skin.SetBlendShapeWeight(TwistR, Mathf.Max(0f, -w) * 100f);
            // the head: a look somewhere else every few seconds, turned to in under a second; now and then a glance down
            if (t > headNext) { headTo = Random.value < 0.35f ? 0f : Random.Range(-1f, 1f); downTo = Random.value < 0.2f ? Random.Range(0.5f, 1f) : 0f; headNext = t + Random.Range(2.5f, 7f); }
            head = Mathf.SmoothDamp(head, headTo, ref headV, 0.45f, Mathf.Infinity, dt); down = Mathf.SmoothDamp(down, downTo, ref downV, 0.6f, Mathf.Infinity, dt);
            skin.SetBlendShapeWeight(HeadL, Mathf.Max(0f, head) * 100f * headAmp); skin.SetBlendShapeWeight(HeadR, Mathf.Max(0f, -head) * 100f * headAmp);
            skin.SetBlendShapeWeight(HeadDown, down * 100f * headAmp);
        }

        static float Ramp(float x, float a, float b) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }
        static float Bump(float x, float c, float w) { float t = Mathf.Clamp01(1f - Mathf.Abs(x - c) / w); return t * t * (3f - 2f * t); }

        /// <summary>The middle, seen from above, of what lies between two heights.</summary>
        static Vector3 Mid(Vector3[] v, float y0, float y1)
        {
            Vector3 sum = Vector3.zero; int n = 0;
            foreach (var p in v) if (p.y >= y0 && p.y <= y1) { sum += p; n++; }
            return n > 0 ? new Vector3(sum.x / n, 0f, sum.z / n) : Vector3.zero;
        }

        delegate float Weight(Vector3 p);

        /// <summary>A shape that turns each point by angle × its weight about an axis through a pivot, normals with it.</summary>
        static void Turn(Mesh m, string name, Vector3[] v, Vector3[] n, Vector3 pivot, Vector3 axis, float angle, Weight weight)
        {
            var d = new Vector3[v.Length]; var dn = n != null && n.Length == v.Length ? new Vector3[v.Length] : null;
            for (int i = 0; i < v.Length; i++)
            {
                float k = weight(v[i]); if (k <= 0f) continue;
                var q = Quaternion.AngleAxis(angle * k, axis);
                d[i] = pivot + q * (v[i] - pivot) - v[i];
                if (dn != null) dn[i] = q * n[i] - n[i];
            }
            m.AddBlendShapeFrame(name, 100f, d, dn, null);
        }

        /// <summary>The figure's mesh with its idle shapes, in the order of the constants above.</summary>
        static Mesh Shapes(Mesh src)
        {
            var m = Instantiate(src); m.name = src.name + " at ease";
            var v = src.vertices; var n = src.normals;
            float top = src.bounds.max.y, bottom = src.bounds.min.y, neck = top - 0.27f;
            Vector3 chest = Mid(v, top - 0.62f, top - 0.38f), headC = Mid(v, top - 0.16f, top);
            // breath: the chest rises and fills out, most at the chest, nothing at the hips or the head
            var d = new Vector3[v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                float k = Bump(v[i].y, top - 0.48f, 0.3f); if (k <= 0f) continue;
                d[i] = new Vector3(0f, 0.01f * k, 0f) + new Vector3(v[i].x - chest.x, 0f, v[i].z - chest.z) * (0.03f * k);
            }
            m.AddBlendShapeFrame("breath", 100f, d, null, null);
            // the weight on one foot or the other: the whole figure leans from the ankles, the legs taking it up gradually
            var ankle = new Vector3(chest.x, bottom + 0.05f, chest.z);
            Weight legs = p => Ramp(p.y, bottom + 0.05f, bottom + 0.9f);
            Turn(m, "swayR", v, n, ankle, Vector3.forward, 2f, legs); Turn(m, "swayL", v, n, ankle, Vector3.forward, -2f, legs);
            Turn(m, "leanF", v, n, ankle, Vector3.right, 0.8f, legs); Turn(m, "leanB", v, n, ankle, Vector3.right, -0.8f, legs);
            // the upper body turning about the spine, from the hips up
            Weight waist = p => Ramp(p.y, top - 0.9f, top - 0.35f);
            Turn(m, "twistL", v, n, chest, Vector3.up, 4f, waist); Turn(m, "twistR", v, n, chest, Vector3.up, -4f, waist);
            // the head about the neck: only the head, not the shoulders or a hand beside it
            Weight headW = p => Ramp(p.y, neck - 0.02f, neck + 0.06f) * (1f - Ramp(new Vector2(p.x - headC.x, p.z - headC.z).magnitude, 0.15f, 0.21f));
            var headPivot = new Vector3(headC.x, neck, headC.z);
            Turn(m, "headL", v, n, headPivot, Vector3.up, 22f, headW); Turn(m, "headR", v, n, headPivot, Vector3.up, -22f, headW);
            Turn(m, "headDown", v, n, headPivot, Vector3.right, 9f, headW);
            return m;
        }
    }
}

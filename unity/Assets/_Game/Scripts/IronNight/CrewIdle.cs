using UnityEngine;

namespace IronNight
{
    /// <summary>
    /// A figure from the crew pictures standing at ease: it breathes, stands on one leg and then the other (the hips go
    /// out, the shoulders lean back over them), sways a little back and forth, turns at the waist and looks about; the
    /// two men of a pair now and then turn to each other and talk, nodding. The mesh is one piece with no skeleton, so
    /// the moves are blend shapes made here from the figure's own heights, taken from the top down (so the commander cut
    /// off at the waist in his hatch moves the same way). The head turns about the neck and fades out beyond the head
    /// itself, so a hand held to the ear follows it only in part.
    /// </summary>
    public class CrewIdle : MonoBehaviour
    {
        const int Breath = 0, ShiftR = 1, ShiftL = 2, LeanF = 3, LeanB = 4, TwistL = 5, TwistR = 6, HeadL = 7, HeadR = 8, HeadDown = 9;
        const int Look = 0, Ahead = 1, Talk = 2, Down = 3;   // what the head is doing
        SkinnedMeshRenderer skin; float seed, period, headAmp = 1f;
        public CrewIdle partner; public float partnerSide;   // the man to talk to, and which way to turn the head to him (+1 or -1)
        float shift, shiftTo, shiftV, shiftNext, head, headTo, headV, down, downTo, downV, twist, twistV, modeEnd; int mode = Ahead;

        /// <summary>Makes a standing figure (a prop with one mesh, read-enabled) move at ease. headAmp scales how far the
        /// head turns: less for a man holding a handset to his ear.</summary>
        public static CrewIdle Bring(GameObject figure, int seed, float headAmp = 1f)
        {
            var mf = figure.GetComponentInChildren<MeshFilter>(); var mr = mf != null ? mf.GetComponent<MeshRenderer>() : null;
            if (mr == null || mf.sharedMesh == null || !mf.sharedMesh.isReadable) return null;
            var go = new GameObject("Idle"); go.transform.SetParent(mf.transform, false);
            var s = go.AddComponent<SkinnedMeshRenderer>(); s.sharedMesh = Shapes(mf.sharedMesh); s.sharedMaterial = mr.sharedMaterial; s.shadowCastingMode = mr.shadowCastingMode;
            var b = s.sharedMesh.bounds; b.Expand(0.4f); s.localBounds = b; mr.enabled = false;
            var idle = figure.AddComponent<CrewIdle>(); idle.skin = s; idle.seed = seed * 13.7f + 3.1f; idle.headAmp = headAmp;
            idle.period = 3.4f + (seed % 4) * 0.4f; idle.shiftTo = seed % 2 == 0 ? 0.8f : -0.8f; idle.shiftNext = 2f + seed * 1.3f; idle.modeEnd = 0.8f + seed * 0.9f;
            return idle;
        }

        /// <summary>Turns to the partner and talks for a while; he may turn back.</summary>
        void StartTalk(float until, bool ask)
        {
            mode = Talk; modeEnd = until; headTo = partnerSide; downTo = 0f;
            if (ask && partner != null && partner.mode != Talk && Random.value < 0.75f) partner.StartTalk(until + Random.Range(-0.6f, 0.6f), false);
        }

        void Update()
        {
            if (skin == null) return;
            float t = Time.unscaledTime, dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            // breathing: about fifteen breaths a minute, each man his own
            skin.SetBlendShapeWeight(Breath, 50f + 50f * Mathf.Sin((t + seed) * 2f * Mathf.PI / period));
            // the weight on one leg, then the other: a new stance every five to ten seconds, and a restless drift on top
            if (t > shiftNext) { shiftTo = (Random.value < 0.5f ? -1f : 1f) * Random.Range(0.55f, 1f); if (Random.value < 0.2f) shiftTo = 0f; shiftNext = t + Random.Range(5f, 10f); }
            shift = Mathf.SmoothDamp(shift, shiftTo, ref shiftV, 1.1f, Mathf.Infinity, dt);
            float s = Mathf.Clamp(shift + (Mathf.PerlinNoise(seed, t * 0.25f) - 0.5f) * 0.3f, -1f, 1f);
            skin.SetBlendShapeWeight(ShiftR, Mathf.Max(0f, s) * 100f); skin.SetBlendShapeWeight(ShiftL, Mathf.Max(0f, -s) * 100f);
            float l = (Mathf.PerlinNoise(seed + 5.3f, t * 0.13f) - 0.5f) * 2f;
            skin.SetBlendShapeWeight(LeanF, Mathf.Max(0f, l) * 100f); skin.SetBlendShapeWeight(LeanB, Mathf.Max(0f, -l) * 100f);
            // the head: looks about, looks ahead, glances down, or turns to the partner and talks
            if (t > modeEnd)
            {
                float r = Random.value;
                if (partner != null && r < 0.3f) StartTalk(t + Random.Range(3.5f, 6.5f), true);
                else if (r < 0.62f) { mode = Look; headTo = Random.Range(-0.75f, 0.75f); downTo = Random.value < 0.3f ? Random.Range(0.1f, 0.35f) : 0f; modeEnd = t + Random.Range(2f, 4.5f); }
                else if (r < 0.9f) { mode = Ahead; headTo = Random.Range(-0.12f, 0.12f); downTo = 0f; modeEnd = t + Random.Range(2.5f, 5f); }
                else { mode = Down; headTo = Random.Range(-0.3f, 0.3f); downTo = Random.Range(0.6f, 1f); modeEnd = t + Random.Range(1.5f, 3f); }
            }
            if (mode == Talk) downTo = 0.18f + 0.18f * Mathf.Sin((t + seed) * 2f * Mathf.PI * 0.6f);   // nodding along
            head = Mathf.SmoothDamp(head, headTo, ref headV, 0.32f, Mathf.Infinity, dt); down = Mathf.SmoothDamp(down, downTo, ref downV, 0.35f, Mathf.Infinity, dt);
            skin.SetBlendShapeWeight(HeadL, Mathf.Max(0f, head) * 100f * headAmp); skin.SetBlendShapeWeight(HeadR, Mathf.Max(0f, -head) * 100f * headAmp);
            skin.SetBlendShapeWeight(HeadDown, down * 100f * headAmp);
            // the waist goes a little way with the head, and drifts on its own
            float tw = Mathf.Clamp(head * 0.45f + (Mathf.PerlinNoise(seed + 9.1f, t * 0.1f) - 0.5f) * 0.5f, -1f, 1f);
            twist = Mathf.SmoothDamp(twist, tw, ref twistV, 0.6f, Mathf.Infinity, dt);
            skin.SetBlendShapeWeight(TwistL, Mathf.Max(0f, twist) * 100f); skin.SetBlendShapeWeight(TwistR, Mathf.Max(0f, -twist) * 100f);
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

        /// <summary>The weight on one leg: the legs lean from the ankles so the hips go out to the side, and the body
        /// above leans back over them by part of the angle, the shoulders tilting the other way.</summary>
        static void Shift(Mesh m, string name, Vector3[] v, Vector3[] n, Vector3 ankle, float hips, float shoulders, float angle)
        {
            var d = new Vector3[v.Length]; var dn = n != null && n.Length == v.Length ? new Vector3[v.Length] : null;
            var hip = new Vector3(ankle.x, hips, ankle.z); var hipAfter = ankle + Quaternion.AngleAxis(angle, Vector3.forward) * (hip - ankle);
            for (int i = 0; i < v.Length; i++)
            {
                var q1 = Quaternion.AngleAxis(angle * Ramp(v[i].y, ankle.y, hips), Vector3.forward); var p = ankle + q1 * (v[i] - ankle);
                var q2 = Quaternion.AngleAxis(-0.55f * angle * Ramp(v[i].y, hips, shoulders), Vector3.forward); p = hipAfter + q2 * (p - hipAfter);
                d[i] = p - v[i]; if (dn != null) dn[i] = q2 * q1 * n[i] - n[i];
            }
            m.AddBlendShapeFrame(name, 100f, d, dn, null);
        }

        /// <summary>The figure's mesh with its idle shapes, in the order of the constants above.</summary>
        static Mesh Shapes(Mesh src)
        {
            var m = Instantiate(src); m.name = src.name + " at ease";
            var v = src.vertices; var n = src.normals;
            float top = src.bounds.max.y, bottom = src.bounds.min.y, neck = top - 0.27f, hips = Mathf.Max(bottom + 0.1f, top - 0.85f);
            Vector3 chest = Mid(v, top - 0.62f, top - 0.38f), headC = Mid(v, top - 0.16f, top);
            // breath: the chest rises and fills out, most at the chest, nothing at the hips or the head
            var d = new Vector3[v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                float k = Bump(v[i].y, top - 0.48f, 0.3f); if (k <= 0f) continue;
                d[i] = new Vector3(0f, 0.015f * k, 0f) + new Vector3(v[i].x - chest.x, 0f, v[i].z - chest.z) * (0.04f * k);
            }
            m.AddBlendShapeFrame("breath", 100f, d, null, null);
            // the weight on one leg or the other
            var ankle = new Vector3(chest.x, bottom + 0.05f, chest.z);
            Shift(m, "shiftR", v, n, ankle, hips, top - 0.45f, 3.5f); Shift(m, "shiftL", v, n, ankle, hips, top - 0.45f, -3.5f);
            // a little back and forth, from the ankles
            Weight legs = p => Ramp(p.y, bottom + 0.05f, hips);
            Turn(m, "leanF", v, n, ankle, Vector3.right, 1.1f, legs); Turn(m, "leanB", v, n, ankle, Vector3.right, -1.1f, legs);
            // the upper body turning about the spine, from the hips up
            Weight waist = p => Ramp(p.y, hips, top - 0.35f);
            Turn(m, "twistL", v, n, chest, Vector3.up, 9f, waist); Turn(m, "twistR", v, n, chest, Vector3.up, -9f, waist);
            // the head about the neck: only the head, not the shoulders or a hand beside it
            Weight headW = p => Ramp(p.y, neck - 0.04f, neck + 0.07f) * (1f - Ramp(new Vector2(p.x - headC.x, p.z - headC.z).magnitude, 0.15f, 0.21f));
            var headPivot = new Vector3(headC.x, neck, headC.z);
            Turn(m, "headL", v, n, headPivot, Vector3.up, 35f, headW); Turn(m, "headR", v, n, headPivot, Vector3.up, -35f, headW);
            Turn(m, "headDown", v, n, headPivot, Vector3.right, 14f, headW);
            return m;
        }
    }
}

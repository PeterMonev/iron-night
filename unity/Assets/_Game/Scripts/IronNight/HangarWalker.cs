using UnityEngine;

namespace IronNight
{
    /// <summary>
    /// Someone going about the hangar: a mechanic with his toolbox, a man with a crate of shells (Props/walk_*_a and _b,
    /// two figures in mid-stride made into a walk by Gait), along a path behind and beside the tank, there and back,
    /// stopping now and then for a few seconds before he goes on.
    /// </summary>
    public class HangarWalker : MonoBehaviour
    {
        const float Speed = 1.2f;
        Gait gait; Vector3[] path; int next, dir = 1; float wait, cycle; MeshFilter mf; MeshRenderer mr;

        /// <summary>The walker named (walk_us_mech and the like) on the path from one of its points; null without his figures.</summary>
        public static HangarWalker Make(Transform hangar, string name, Vector3[] path, int start)
        {
            var g = Gait.Load(name, 0.72f, Resources.Load<Material>("VehicleLit")); var pf = Resources.Load<GameObject>("Props/" + name + "_a");
            if (g == null || pf == null) return null;
            var go = Instantiate(pf, hangar); go.name = name; go.transform.localPosition = path[start];
            var w = go.AddComponent<HangarWalker>(); w.gait = g; w.path = path; w.next = (start + 1) % path.Length; w.cycle = Random.value * 2f; w.wait = Random.Range(0f, 3f);
            w.mf = go.GetComponentInChildren<MeshFilter>(); w.mr = go.GetComponentInChildren<MeshRenderer>(); w.mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            w.mf.sharedMesh = g.Frame(w.cycle, out var m); w.mr.sharedMaterial = m;
            return w;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (wait > 0f) { wait -= dt; return; }   // stopped a while, as he was
            var to = path[next] - transform.localPosition; to.y = 0f;
            if (to.magnitude < 0.15f)
            {
                if (next == path.Length - 1) dir = -1; else if (next == 0) dir = 1;
                next += dir; if (Random.value < 0.45f) wait = Random.Range(2f, 6f);
                return;
            }
            var step = to.normalized * Mathf.Min(to.magnitude, Speed * dt); var p = transform.localPosition + step;
            p.y = Mathf.Abs(Mathf.Sin(cycle * Mathf.PI)) * 0.035f; transform.localPosition = p;   // up a little as the leg straightens under him
            var want = Quaternion.LookRotation(to.normalized, Vector3.up) * Quaternion.Euler(0f, 0f, Mathf.Sin(cycle * Mathf.PI) * 2.2f); transform.localRotation = Quaternion.RotateTowards(transform.localRotation, want, 220f * dt);   // the shoulders rock from foot to foot
            cycle += Speed / (2f * gait.step) * dt; mf.sharedMesh = gait.Frame(cycle, out var m); mr.sharedMaterial = m;
        }
    }
}

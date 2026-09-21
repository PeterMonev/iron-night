using System.Collections.Generic;
using UnityEngine;

namespace IronNight
{
    /// <summary>
    /// Enemy infantry: squads of four tank hunters with Panzerfausts. They come out of the dark on foot, close to
    /// within fifteen metres of the nearest tank and fire; each man reloads for nine seconds. The tanks' machine guns
    /// cut them down, HE and artillery blasts kill them in bunches, and a tank simply runs over the ones in its way.
    /// A soldier is a capsule with a helmet; the dead lie where they fell for a while.
    /// </summary>
    public class Infantry : MonoBehaviour
    {
        public class Soldier { public Transform t; public Vector3 pos; public float reload, phase, deadAge; public bool dead, still; public Vector3 face = Vector3.forward; }   // still: an observer, he stays where he is and does not shoot
        public class Squad { public readonly List<Soldier> men = new List<Soldier>(); }

        public readonly List<Squad> squads = new List<Squad>();
        class Runner { public Transform t; public Vector3 dir; public float age; }
        readonly List<Runner> runners = new List<Runner>();

        /// <summary>Two crewmen out of a knocked-out platoon tank, running for the rear; gone after eight seconds.</summary>
        public void BailOut(Vector3 at, Vector3 rear)
        {
            if (figure == null) return;
            for (int i = 0; i < 2; i++)
            {
                var go = new GameObject("Crewman"); var dir = (rear + new Vector3(Random.Range(-0.5f, 0.5f), 0f, Random.Range(-0.5f, 0.5f))).normalized;
                go.transform.position = at + dir * 2.5f + new Vector3(Random.Range(-1.5f, 1.5f), 0f, 0f); go.transform.rotation = Quaternion.LookRotation(dir);
                var fg = Instantiate(figure, go.transform); fg.transform.localRotation = Quaternion.Euler(0f, SoldierYaw, 0f); fg.transform.localScale = Vector3.one * 0.95f;
                foreach (var rr in fg.GetComponentsInChildren<Renderer>()) { rr.sharedMaterial = skin; rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
                runners.Add(new Runner { t = go.transform, dir = dir });
            }
        }
        const float SoldierYaw = -90f, SoldierBYaw = 0f; GameObject figureB; Material skinB;   // the figure's facing in its own mesh, corrected here if the export looks the wrong way
        readonly List<Soldier> fallen = new List<Soldier>();
        Material uniform, helmet, skin; GameObject figure;

        public void Build()
        {
            uniform = new Material(Resources.Load<Material>("BarrelLit")); uniform.SetColor("_BaseColor", new Color(0.22f, 0.24f, 0.2f)); uniform.SetFloat("_Metallic", 0f); uniform.SetFloat("_Smoothness", 0.15f);
            helmet = new Material(uniform); helmet.SetColor("_BaseColor", new Color(0.18f, 0.2f, 0.18f)); helmet.SetFloat("_Smoothness", 0.4f);
            // the figure from the reference render, when it is there; the capsules stay as the fallback
            figure = Resources.Load<GameObject>("Props/soldier"); figureB = Resources.Load<GameObject>("Props/soldier_b");   // the second pose: kneeling with the Panzerfaust up
            if (figure != null) { skin = new Material(Resources.Load<Material>("VehicleLit")); skin.SetTexture("_BaseMap", Resources.Load<Texture2D>("Props/soldier_tex")); skin.SetColor("_BaseColor", new Color(0.7f, 0.7f, 0.68f)); skin.SetFloat("_Smoothness", 0.1f); skin.SetFloat("_Cull", 0f); }
        }

        public int Alive { get { int n = 0; foreach (var s in squads) foreach (var m in s.men) if (!m.dead) n++; return n; } }

        /// <summary>Four men in a loose line at the point, facing the way they were sent.</summary>
        public Squad Spawn(Vector3 at, Vector3 facing)
        {
            var sq = new Squad(); var side = new Vector3(facing.z, 0f, -facing.x);
            for (int i = 0; i < 4; i++)
            {
                var go = new GameObject("Soldier"); go.transform.SetParent(transform, false);
                if (figure != null)
                {
                    bool kneel = figureB != null && i % 2 == 1; if (kneel && skinB == null) { skinB = new Material(skin); skinB.SetTexture("_BaseMap", Resources.Load<Texture2D>("Props/soldier_b_tex")); }
                    var fg = Instantiate(kneel ? figureB : figure, go.transform); fg.transform.localRotation = Quaternion.Euler(0f, kneel ? SoldierBYaw : SoldierYaw, 0f); foreach (var rr in fg.GetComponentsInChildren<Renderer>()) { rr.sharedMaterial = kneel ? skinB : skin; rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
                    var m2 = new Soldier { t = go.transform, pos = at + side * ((i - 1.5f) * 2.2f) + facing * Random.Range(-1f, 1f), reload = 2f + Random.value * 3f, phase = Random.value * 6.28f, face = facing };
                    m2.t.position = m2.pos; sq.men.Add(m2); continue;
                }
                var body = GameObject.CreatePrimitive(PrimitiveType.Capsule); Destroy(body.GetComponent<Collider>()); body.transform.SetParent(go.transform, false);
                body.transform.localPosition = new Vector3(0f, 0.85f, 0f); body.transform.localScale = new Vector3(0.62f, 0.78f, 0.62f); body.GetComponent<Renderer>().sharedMaterial = uniform;
                var head = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(head.GetComponent<Collider>()); head.transform.SetParent(go.transform, false);
                head.transform.localPosition = new Vector3(0f, 1.72f, 0f); head.transform.localScale = new Vector3(0.44f, 0.34f, 0.48f); head.GetComponent<Renderer>().sharedMaterial = helmet;
                var tube = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(tube.GetComponent<Collider>()); tube.transform.SetParent(go.transform, false);
                tube.transform.localPosition = new Vector3(0.28f, 1.35f, 0.2f); tube.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); tube.transform.localScale = new Vector3(0.08f, 0.5f, 0.08f); tube.GetComponent<Renderer>().sharedMaterial = helmet;
                var m = new Soldier { t = go.transform, pos = at + side * ((i - 1.5f) * 2.2f) + facing * Random.Range(-1f, 1f), reload = 2f + Random.value * 3f, phase = Random.value * 6.28f, face = facing };
                m.t.position = m.pos; sq.men.Add(m);
            }
            squads.Add(sq); return sq;
        }

        /// <summary>Moves every man toward the nearest tank and calls fire for the ones in range and ready.</summary>
        public void Tick(float dt, List<Vehicle> platoon, Props props, System.Action<Soldier, Vehicle> fire)
        {
            float time = Time.time;
            for (int i = runners.Count - 1; i >= 0; i--) { var r = runners[i]; r.age += dt; r.t.position += r.dir * (3.2f * dt); r.t.position = new Vector3(r.t.position.x, Mathf.Abs(Mathf.Sin(r.age * 9f)) * 0.08f, r.t.position.z); if (r.age > 8f) { Destroy(r.t.gameObject); runners.RemoveAt(i); } }
            for (int q = squads.Count - 1; q >= 0; q--)
            {
                var sq = squads[q]; bool any = false;
                foreach (var m in sq.men)
                {
                    if (m.dead) continue; any = true; if (m.still) continue;
                    Vehicle target = null; float best = float.MaxValue;
                    foreach (var v in platoon) { if (v.dead) continue; var d = v.transform.position - m.pos; d.y = 0f; if (d.sqrMagnitude < best) { best = d.sqrMagnitude; target = v; } }
                    if (target == null) continue;
                    var to = target.transform.position - m.pos; to.y = 0f; float dist = to.magnitude; to /= Mathf.Max(dist, 0.01f);
                    if (dist > 13f) { m.pos += to * (2.4f * dt); m.face = to; }
                    else if (dist < 7f) { m.pos -= to * (1.8f * dt); m.face = to; }   // too close for the rocket: back off
                    m.pos = props.PushOut(m.pos, 0.5f);
                    m.reload -= dt;
                    if (dist < 15f && m.reload <= 0f) { m.reload = 9f + Random.value * 3f; fire(m, target); }
                    bool moving = dist > 13f || dist < 7f;
                    m.t.position = m.pos + Vector3.up * (moving ? Mathf.Abs(Mathf.Sin(time * 9f + m.phase)) * 0.08f : 0f);
                    m.t.rotation = Quaternion.LookRotation(m.face, Vector3.up);
                }
                if (!any) squads.RemoveAt(q);
            }
            for (int i = fallen.Count - 1; i >= 0; i--) { var m = fallen[i]; m.deadAge += dt; if (m.deadAge > 25f) { Destroy(m.t.gameObject); fallen.RemoveAt(i); } }
        }

        /// <summary>One kneeling man who stays put: the forward observer with his radio.</summary>
        public Soldier SpawnObserver(Vector3 at, Vector3 facing)
        {
            var sq = Spawn(at, facing); var keep = sq.men[1]; keep.still = true; keep.pos = at; keep.t.position = at; keep.t.rotation = Quaternion.LookRotation(facing, Vector3.up);
            foreach (var m in sq.men) if (m != keep) { Destroy(m.t.gameObject); m.dead = true; }
            sq.men.RemoveAll(m => m != keep);
            // the radio: a box and a whip aerial beside him
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(box.GetComponent<Collider>()); box.transform.SetParent(keep.t, false); box.transform.localPosition = new Vector3(0.7f, 0.25f, -0.2f); box.transform.localScale = new Vector3(0.4f, 0.5f, 0.3f); box.GetComponent<Renderer>().sharedMaterial = helmet;
            var whip = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(whip.GetComponent<Collider>()); whip.transform.SetParent(keep.t, false); whip.transform.localPosition = new Vector3(0.7f, 1.4f, -0.2f); whip.transform.localScale = new Vector3(0.02f, 0.9f, 0.02f); whip.GetComponent<Renderer>().sharedMaterial = helmet;
            return keep;
        }

        /// <summary>The nearest living soldier to a point within reach, or null.</summary>
        public Soldier Nearest(Vector3 from, float reach)
        {
            Soldier best = null; float bd = reach * reach;
            foreach (var s in squads) foreach (var m in s.men) { if (m.dead) continue; var d = m.pos - from; d.y = 0f; float q = d.sqrMagnitude; if (q < bd) { bd = q; best = m; } }
            return best;
        }

        public void Kill(Soldier m)
        {
            if (m.dead) return; m.dead = true; m.deadAge = 0f; fallen.Add(m);
            m.t.position = m.pos + Vector3.up * 0.25f; m.t.rotation = Quaternion.LookRotation(m.face, Vector3.up) * Quaternion.Euler(-90f, 0f, Random.Range(-30f, 30f));
        }

        /// <summary>Kills every man within the radius of a blast; returns how many.</summary>
        public int Blast(Vector3 at, float radius)
        {
            int n = 0;
            foreach (var s in squads) foreach (var m in s.men) { if (m.dead) continue; var d = m.pos - at; d.y = 0f; if (d.magnitude < radius) { Kill(m); n++; } }
            return n;
        }

        /// <summary>Anyone under the tracks of a moving tank.</summary>
        public int Crush(List<Vehicle> vehicles)
        {
            int n = 0;
            foreach (var s in squads) foreach (var m in s.men)
            {
                if (m.dead) continue;
                foreach (var v in vehicles) { if (v.dead || v.spec.isGun) continue; var d = m.pos - v.transform.position; d.y = 0f; if (d.magnitude < v.spec.radius * 0.7f) { Kill(m); n++; break; } }
            }
            return n;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace IronNight
{
    /// <summary>
    /// Enemy infantry: squads of six out of the dark. Three tank hunters with Panzerfausts close to within fifteen
    /// metres of the nearest tank, kneel and fire, nine seconds to the next; two riflemen kneel at about twenty metres
    /// and a machine gunner goes down at twenty-five, their tracers sparking off the armour. Far off they walk, closer
    /// in they run (Gait: the walking and running figures stepping stride by stride), and they back off a tank that
    /// comes too close. The tanks' machine guns cut them down, HE and artillery blasts kill them in bunches, a tank
    /// simply runs over the ones in its way; the dead lie where they fell for a while.
    /// The guns have their crews here too: a layer, a loader, an ammunition man and a spotter round every PaK, 88,
    /// flak gun and rocket launcher, turning with it. Kill them all and the gun falls silent.
    /// Without the new figures the soldiers are the older still figure sliding along, or capsules.
    /// </summary>
    public class Infantry : MonoBehaviour
    {
        public enum Role { Faust, Rifle, Mg, Crew }
        public class Soldier
        {
            public Transform t; public Vector3 pos; public float reload, phase, deadAge, cycle, stop; public bool dead, still; public Vector3 face = Vector3.forward;   // still: an observer or a gun crewman, he stays where he is and does not shoot
            public Role role; public Vehicle gun; public int burst; public bool posable, dug, ally; public string side; public MeshFilter mf; public MeshRenderer mr; public Transform fig;   // ally: one of ours in a fire fight; side: his nation's pose prefix (us, su)
        }
        public class Squad { public readonly List<Soldier> men = new List<Soldier>(); }

        public readonly List<Squad> squads = new List<Squad>();
        class Runner { public Transform t; public Vector3 dir; public float age, cycle; public MeshFilter mf; public MeshRenderer mr; }
        readonly List<Runner> runners = new List<Runner>();
        /// <summary>A gun whose last crewman has just died: the battle tells the player.</summary>
        public System.Action<Vehicle> gunSilenced;

        /// <summary>A still figure: its mesh, its skin, and its turn when the export faces another way than +Z.</summary>
        class Pose { public Mesh mesh; public Material mat; public float yaw; public GameObject prefab; }
        readonly Dictionary<string, Pose> poses = new Dictionary<string, Pose>();
        Gait walk, run; GameObject frame;   // frame: the prefab every gait figure is built on (the meshes are swapped into it)

        /// <summary>A knocked-out tank's crew getting out and running: German tankers from theirs, our own men from ours
        /// when there is a figure of them (Props/tanker_us_run, tanker_su_run) - and nobody rather than the wrong men.</summary>
        public void BailOut(Vector3 at, Vector3 rear, string who, int count)
        {
            GameObject fig = figure; Material mat = skin; float yaw = SoldierYaw; bool gait = who == "de" && run != null;
            if (who != "de")
            {
                if (!tankers.TryGetValue(who, out var tk)) { tk = new Tanker { fig = Resources.Load<GameObject>("Props/tanker_" + who + "_run") }; if (tk.fig != null) { tk.mat = new Material(Resources.Load<Material>("VehicleLit")); tk.mat.SetTexture("_BaseMap", Resources.Load<Texture2D>("Props/tanker_" + who + "_run_tex")); tk.mat.SetFloat("_Cull", 0f); } tankers[who] = tk; }
                fig = tk.fig; mat = tk.mat; yaw = 0f;   // exported facing +Z
            }
            if (gait) { fig = frame; yaw = 0f; }
            if (fig == null) return;
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Crewman"); var dir = (rear + new Vector3(Random.Range(-0.5f, 0.5f), 0f, Random.Range(-0.5f, 0.5f))).normalized;
                go.transform.position = at + dir * 2.5f + new Vector3(Random.Range(-1.5f, 1.5f), 0f, 0f); go.transform.rotation = Quaternion.LookRotation(dir);
                var fg = Instantiate(fig, go.transform); fg.transform.localRotation = Quaternion.Euler(0f, yaw, 0f); fg.transform.localScale = Vector3.one * 0.95f;
                foreach (var rr in fg.GetComponentsInChildren<Renderer>()) { rr.sharedMaterial = mat; rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
                var r = new Runner { t = go.transform, dir = dir, cycle = Random.value * 2f };
                if (gait) { r.mf = fg.GetComponentInChildren<MeshFilter>(); r.mr = fg.GetComponentInChildren<MeshRenderer>(); }
                runners.Add(r);
            }
        }
        class Tanker { public GameObject fig; public Material mat; }
        readonly Dictionary<string, Tanker> tankers = new Dictionary<string, Tanker>();
        const float SoldierYaw = -90f;   // the older figure's facing in its own mesh
        readonly List<Soldier> fallen = new List<Soldier>();
        Material uniform, helmet, skin; GameObject figure;

        public void Build()
        {
            uniform = new Material(Resources.Load<Material>("BarrelLit")); uniform.SetColor("_BaseColor", new Color(0.22f, 0.24f, 0.2f)); uniform.SetFloat("_Metallic", 0f); uniform.SetFloat("_Smoothness", 0.15f);
            helmet = new Material(uniform); helmet.SetColor("_BaseColor", new Color(0.18f, 0.2f, 0.18f)); helmet.SetFloat("_Smoothness", 0.4f);
            var lit = Resources.Load<Material>("VehicleLit");
            // the older still figure and its kneeling second pose, when the new ones are not there; the capsules after that
            figure = Resources.Load<GameObject>("Props/soldier");
            if (figure != null) skin = Gait.Skin("soldier", lit);
            walk = Gait.Load("inf_walk", 0.72f, lit); run = Gait.Load("inf_run", 1.15f, lit);
            frame = Resources.Load<GameObject>("Props/inf_walk_a");
            AddPose("faust", "inf_faust", 0f, lit); AddPose("rifle", "inf_rifle_kneel", 0f, lit); AddPose("mg", "inf_mg_prone", 0f, lit); AddPose("dead", "inf_dead", 0f, lit);
            foreach (var n in new[] { "us", "su" }) foreach (var p in new[] { "kneel", "stand", "prone", "dead" }) AddPose(n + "_" + p, "ally_" + n + "_" + p, 0f, lit);
            AddPose("layer", "crew_layer", 0f, lit); AddPose("loader", "crew_loader", 0f, lit); AddPose("ammo", "crew_ammo", 0f, lit); AddPose("spotter", "crew_spotter", 0f, lit);
            if (!poses.ContainsKey("faust")) AddPose("faust", "soldier_b", 0f, lit);
            if (!poses.ContainsKey("rifle") && poses.ContainsKey("faust")) poses["rifle"] = poses["faust"];
            if (!poses.ContainsKey("mg") && poses.ContainsKey("rifle")) poses["mg"] = poses["rifle"];
        }

        void AddPose(string key, string name, float yaw, Material lit)
        {
            var pf = Resources.Load<GameObject>("Props/" + name); if (pf == null) return;
            var mf = pf.GetComponentInChildren<MeshFilter>(); if (mf == null) return;
            poses[key] = new Pose { mesh = mf.sharedMesh, mat = Gait.Skin(name, lit), yaw = yaw, prefab = pf };
        }

        /// <summary>The squads on foot, not counting the crews at their guns or the garrisons in the trenches.</summary>
        public int Squads { get { int n = 0; foreach (var s in squads) if (s.men.Count == 0 || (s.men[0].gun == null && !s.men[0].dug && !s.men[0].ally)) n++; return n; } }

        public int Alive { get { int n = 0; foreach (var s in squads) foreach (var m in s.men) if (!m.dead) n++; return n; } }

        /// <summary>A soldier's body: the gait frame when there is one, else the pose's own figure, else the older
        /// figure; the capsule man when there is nothing at all.</summary>
        Soldier Man(Transform parent, Vector3 at, Vector3 facing, Role role, string still)
        {
            var go = new GameObject("Soldier"); go.transform.SetParent(parent, false); go.transform.position = at; go.transform.rotation = Quaternion.LookRotation(facing, Vector3.up);
            var m = new Soldier { t = go.transform, pos = at, face = facing, role = role, reload = 2f + Random.value * 3f, phase = Random.value * 6.28f, cycle = Random.value * 2f };
            Pose sp = null; if (still != null) poses.TryGetValue(still, out sp);
            GameObject src = sp != null ? sp.prefab : frame != null && walk != null && run != null ? frame : figure;
            if (src != null)
            {
                var fg = Instantiate(src, go.transform); m.fig = fg.transform; m.mf = fg.GetComponentInChildren<MeshFilter>(); m.mr = fg.GetComponentInChildren<MeshRenderer>(); m.posable = src != figure;
                fg.transform.localRotation = Quaternion.Euler(0f, src == figure ? SoldierYaw : 0f, 0f);
                foreach (var rr in fg.GetComponentsInChildren<Renderer>()) { rr.sharedMaterial = sp != null ? sp.mat : src == figure ? skin : walk.matA; rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
                return m;
            }
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule); Destroy(body.GetComponent<Collider>()); body.transform.SetParent(go.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.85f, 0f); body.transform.localScale = new Vector3(0.62f, 0.78f, 0.62f); body.GetComponent<Renderer>().sharedMaterial = uniform;
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(head.GetComponent<Collider>()); head.transform.SetParent(go.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.72f, 0f); head.transform.localScale = new Vector3(0.44f, 0.34f, 0.48f); head.GetComponent<Renderer>().sharedMaterial = helmet;
            var tube = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(tube.GetComponent<Collider>()); tube.transform.SetParent(go.transform, false);
            tube.transform.localPosition = new Vector3(0.28f, 1.35f, 0.2f); tube.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); tube.transform.localScale = new Vector3(0.08f, 0.5f, 0.08f); tube.GetComponent<Renderer>().sharedMaterial = helmet;
            return m;
        }

        /// <summary>Puts a still pose on a man built on the gait frame (the same importer's hierarchy, so the mesh
        /// simply goes in); a man on the older figure keeps his.</summary>
        void Show(Soldier m, string key)
        {
            if (!m.posable || m.mf == null || !poses.TryGetValue(key, out var p) || m.mf.sharedMesh == p.mesh) return;
            m.mf.sharedMesh = p.mesh; m.mr.sharedMaterial = p.mat;
        }

        /// <summary>Six men in a loose line at the point, facing the way they were sent: three tank hunters in the
        /// middle, a rifleman to either side, the machine gunner behind.</summary>
        public Squad Spawn(Vector3 at, Vector3 facing)
        {
            var sq = new Squad(); var side = new Vector3(facing.z, 0f, -facing.x);
            for (int i = 0; i < 6; i++)
            {
                Role role = i < 3 ? Role.Faust : i < 5 ? Role.Rifle : Role.Mg;
                float across = i < 3 ? (i - 1f) * 2.2f : i == 3 ? -5.2f : i == 4 ? 5.2f : 0f, along = i == 5 ? -3f : Random.Range(-1f, 1f);
                var m = Man(transform, at + side * across + facing * along, facing, role, null);
                m.stop = role == Role.Faust ? 13f : role == Role.Rifle ? Random.Range(18f, 24f) : Random.Range(24f, 29f);
                sq.men.Add(m);
            }
            squads.Add(sq); return sq;
        }

        /// <summary>Men put into a trench or a nest: they hold it, turn to the nearest tank and fight from where they are -
        /// the riflemen and the machine gunner at up to thirty-five metres, the Panzerfausts when a tank comes within
        /// fifteen. One spot in three has a Panzerfaust, the first of a trench line's men lies behind a machine gun.</summary>
        public void Garrison(List<(Vector3 pos, bool trench)> spots, Vector3 toward)
        {
            if (spots.Count == 0) return; var sq = new Squad();
            for (int i = 0; i < spots.Count; i++)
            {
                var (pos, trench) = spots[i]; var face = toward - pos; face.y = 0f; face = face.sqrMagnitude > 0.01f ? face.normalized : Vector3.forward;
                Role role = i == 0 ? Role.Mg : i % 3 == 2 ? Role.Faust : Role.Rifle;
                var m = Man(transform, pos, face, role, role == Role.Mg ? "mg" : role == Role.Faust ? "faust" : "rifle"); m.dug = true; m.stop = 31f;
                if (role == Role.Mg && trench) m.pos += Vector3.up * 0.25f;   // on the parapet, behind the gun
                m.t.position = m.pos; sq.men.Add(m);
            }
            squads.Add(sq);
        }

        /// <summary>The crew round a gun, as (role, across, along, turn in degrees) in the gun's own frame with its muzzle
        /// along +Z, scaled to the gun: the layer at the left of the breech, the loader behind it, the ammunition man at
        /// the crates further back, the spotter off to the side with his glasses.</summary>
        static (string pose, float x, float z, float yaw)[] CrewPlaces(VehicleSpec spec)
        {
            if (spec == VehicleSpec.Flak88) return new[] { ("layer", -1.3f, 1.1f, 0f), ("loader", 1.1f, -1.6f, 10f), ("ammo", 2.1f, -2.8f, -60f), ("spotter", -3.4f, 2.2f, 0f) };
            if (spec == VehicleSpec.Flak38) return new[] { ("layer", -1.1f, -0.6f, 0f), ("loader", 1.2f, -0.9f, -20f), ("ammo", 1.8f, -2.3f, -60f), ("spotter", -2.2f, 1.2f, 20f) };
            if (spec == VehicleSpec.Nebelwerfer) return new[] { ("loader", -1.3f, -2.0f, 30f), ("ammo", 1.5f, -2.4f, -50f), ("spotter", -2.6f, 1.4f, 0f) };
            return new[] { ("layer", -0.9f, -0.3f, 0f), ("loader", 0.6f, -1.2f, 0f), ("ammo", -1.5f, -1.9f, 60f), ("spotter", -2.6f, 0.8f, 0f) };   // the PaK 40
        }

        /// <summary>Puts the crew round an enemy gun and returns how many: they turn with it and stand their ground.</summary>
        public int ManGun(Vehicle g)
        {
            if (!poses.ContainsKey("loader")) return -1;   // no crew figures: the gun keeps firing on its own as before
            var sq = new Squad(); int n = 0, seed = Random.Range(0, 1000);
            foreach (var (pose, x, z, yaw) in CrewPlaces(g.spec))
            {
                if (!poses.ContainsKey(pose)) continue;
                var m = Man(g.transform, g.transform.position, g.transform.forward, Role.Crew, pose);
                m.t.localPosition = new Vector3(x, 0f, z); m.t.localRotation = Quaternion.Euler(0f, yaw, 0f); m.pos = m.t.position; m.still = true; m.gun = g;
                if (pose == "loader" || pose == "spotter") CrewIdle.Bring(m.fig.gameObject, seed + n, pose == "spotter" ? 0.4f : 1f);
                sq.men.Add(m); n++;
            }
            squads.Add(sq); g.crew = n; return n;
        }

        /// <summary>Moves every man toward the nearest tank, shows him walking, running, kneeling or prone, and calls
        /// fire for the tank hunters in range and ready, and shoot for the riflemen and the machine gunner.</summary>
        public void Tick(float dt, List<Vehicle> platoon, Props props, System.Action<Soldier, Vehicle> fire, System.Action<Soldier, Vehicle> shoot, System.Action<Soldier, Vector3> shootAt)
        {
            TickFights(dt, shootAt);
            float time = Time.time;
            for (int i = runners.Count - 1; i >= 0; i--)
            {
                var r = runners[i]; r.age += dt; r.t.position += r.dir * (3.2f * dt);
                if (r.mf != null) { r.cycle += 3.2f / (2f * run.step) * dt; r.mf.sharedMesh = run.Frame(r.cycle, out var rm); r.mr.sharedMaterial = rm; }
                else r.t.position = new Vector3(r.t.position.x, Mathf.Abs(Mathf.Sin(r.age * 9f)) * 0.08f, r.t.position.z);
                if (r.age > 8f) { Destroy(r.t.gameObject); runners.RemoveAt(i); }
            }
            for (int q = squads.Count - 1; q >= 0; q--)
            {
                var sq = squads[q]; bool any = false;
                foreach (var m in sq.men)
                {
                    if (m.dead) continue; any = true;
                    if (m.gun != null) { m.pos = m.t.position; continue; }   // a crewman goes round with his gun
                    if (m.ally) continue;   // ours fight in TickFights
                    if (m.still) continue;
                    Vehicle target = null; float best = float.MaxValue;
                    foreach (var v in platoon) { if (v.dead) continue; var d = v.transform.position - m.pos; d.y = 0f; if (d.sqrMagnitude < best) { best = d.sqrMagnitude; target = v; } }
                    if (target == null) continue;
                    var to = target.transform.position - m.pos; to.y = 0f; float dist = to.magnitude; to /= Mathf.Max(dist, 0.01f);
                    if (m.dug)
                    {
                        // a garrison holds its trench: it turns to the tank and fights from where it is
                        m.face = to; m.reload -= dt; m.t.rotation = Quaternion.LookRotation(m.face, Vector3.up);
                        if (m.role == Role.Faust) { if (dist < 15f && m.reload <= 0f) { m.reload = 9f + Random.value * 3f; fire(m, target); } }
                        else if (dist < m.stop + 4f) Shoot(m, target, dt, shoot);
                        else { var foe = NearestOf(m.pos, 45f, true); if (foe != null) FightOn(m, foe, shootAt, 0.05f); }   // no tank near: the Germans of a fire fight shoot at ours
                        continue;
                    }
                    float backOff = m.role == Role.Faust ? 7f : 12f; bool ahead = dist > m.stop, back = dist < backOff;
                    var gait = ahead && dist > 45f ? walk : ahead ? run : walk;   // walking up out of the dark, running in, stepping back from a tank
                    float speed = ahead ? (dist > 45f ? 1.5f : 2.8f) : back ? 1.6f : 0f;
                    if (ahead) m.pos += to * (speed * dt); else if (back) m.pos -= to * (speed * dt);
                    m.face = to; m.pos = props.PushOut(m.pos, 0.5f);
                    m.reload -= dt;
                    if (m.role == Role.Faust) { if (dist < 15f && m.reload <= 0f) { m.reload = 9f + Random.value * 3f; fire(m, target); } }
                    else if (!ahead && !back && dist < m.stop + 4f) Shoot(m, target, dt, shoot);
                    bool moving = ahead || back;
                    if (moving && gait != null && m.posable && m.mf != null) { m.cycle += speed / (2f * gait.step) * dt * (back ? -1f : 1f); m.mf.sharedMesh = gait.Frame(m.cycle, out var gm); m.mr.sharedMaterial = gm; m.t.position = m.pos; }
                    else
                    {
                        if (!moving && walk != null) Show(m, m.role == Role.Faust ? "faust" : m.role == Role.Mg ? "mg" : "rifle");
                        m.t.position = m.pos + Vector3.up * (moving ? Mathf.Abs(Mathf.Sin(time * 9f + m.phase)) * 0.08f : 0f);   // the older figure only bobs
                    }
                    m.t.rotation = Quaternion.LookRotation(m.face, Vector3.up);
                }
                if (!any) squads.RemoveAt(q);
            }
            for (int i = fallen.Count - 1; i >= 0; i--) { var m = fallen[i]; m.deadAge += dt; if (m.deadAge > 30f) { Destroy(m.t.gameObject); fallen.RemoveAt(i); } }
        }

        /// <summary>A rifleman fires every second or two; the machine gunner in bursts of six with a pause between.</summary>
        void Shoot(Soldier m, Vehicle target, float dt, System.Action<Soldier, Vehicle> shoot)
        {
            if (m.reload > 0f) return;
            if (m.role == Role.Rifle) { m.reload = 1.3f + Random.value * 1.6f; shoot(m, target); return; }
            if (m.burst <= 0) m.burst = 6;
            shoot(m, target); m.burst--; m.reload = m.burst > 0 ? 0.11f : 2.8f + Random.value * 2f;
        }

        /// <summary>A man of ours standing where he is put (the crew waiting by their wreck): one of the nation's tankers when
        /// there is a figure of them, nobody rather than a German; not a target, not a shooter.</summary>
        public Transform Figure(Vector3 at, Vector3 facing, string nation)
        {
            var go = new GameObject("Crewman"); go.transform.SetParent(transform, false); go.transform.position = at; go.transform.rotation = Quaternion.LookRotation(facing, Vector3.up);
            string id = "crew_" + nation + "_driver"; var pf = Resources.Load<GameObject>("Props/" + id); if (pf == null) { id = "tanker_" + nation + "_run"; pf = Resources.Load<GameObject>("Props/" + id); }
            if (pf != null)
            {
                if (!figureMats.TryGetValue(id, out var mat)) { mat = new Material(Resources.Load<Material>("VehicleLit")); mat.SetTexture("_BaseMap", Resources.Load<Texture2D>("Props/" + id + "_tex")); mat.SetFloat("_Cull", 0f); figureMats[id] = mat; }
                var fg = Instantiate(pf, go.transform); fg.transform.localScale = Vector3.one * 0.95f;   // exported facing +Z
                foreach (var rr in fg.GetComponentsInChildren<Renderer>()) { rr.sharedMaterial = mat; rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
            }
            return go.transform;
        }
        readonly Dictionary<string, Material> figureMats = new Dictionary<string, Material>();

        /// <summary>One man who stays put: the forward observer with his glasses and his radio.</summary>
        public Soldier SpawnObserver(Vector3 at, Vector3 facing)
        {
            var sq = new Squad(); var keep = Man(transform, at, facing, Role.Rifle, poses.ContainsKey("spotter") ? "spotter" : poses.ContainsKey("rifle") ? "rifle" : null);
            keep.still = true; sq.men.Add(keep); squads.Add(sq);
            // the radio: a box and a whip aerial beside him
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(box.GetComponent<Collider>()); box.transform.SetParent(keep.t, false); box.transform.localPosition = new Vector3(0.7f, 0.25f, -0.2f); box.transform.localScale = new Vector3(0.4f, 0.5f, 0.3f); box.GetComponent<Renderer>().sharedMaterial = helmet;
            var whip = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(whip.GetComponent<Collider>()); whip.transform.SetParent(keep.t, false); whip.transform.localPosition = new Vector3(0.7f, 1.4f, -0.2f); whip.transform.localScale = new Vector3(0.02f, 0.9f, 0.02f); whip.GetComponent<Renderer>().sharedMaterial = helmet;
            return keep;
        }

        // ---- fire fights: ours against theirs ----
        class Fight { public Squad ours, theirs; public bool over; }
        readonly List<Fight> fights = new List<Fight>();
        /// <summary>A fire fight won (the Germans in it all dead, some of ours alive): where ours are.</summary>
        public System.Action<Vector3> fightWon;
        public int Fights { get { int n = 0; foreach (var f in fights) if (!f.over) n++; return n; } }
        /// <summary>Whether a nation's soldiers are there to fight.</summary>
        public bool CanFight(string nation) => poses.ContainsKey(nation + "_kneel");

        /// <summary>Four of ours at a point (two kneeling, one standing, one behind a machine gun on the ground) dug in
        /// facing four Germans at another; false when there are no figures of ours.</summary>
        public bool Skirmish(Vector3 at, Vector3 enemy, string nation)
        {
            if (!CanFight(nation)) return false;
            var face = enemy - at; face.y = 0f; face.Normalize(); var side = new Vector3(face.z, 0f, -face.x); var ours = new Squad();
            string[] poseOf = { "kneel", "stand", "kneel", "prone" };
            for (int i = 0; i < 4; i++)
            {
                string key = nation + "_" + poseOf[i]; if (!poses.ContainsKey(key)) key = nation + "_kneel";
                var m = Man(transform, at + side * ((i - 1.5f) * 3f) - face * (i == 3 ? 1.5f : 0f), face, poseOf[i] == "prone" ? Role.Mg : Role.Rifle, key);
                m.ally = true; m.dug = true; m.side = nation; m.reload = Random.value * 2f; ours.men.Add(m);
            }
            squads.Add(ours);
            var spots = new List<(Vector3, bool)>(); for (int i = 0; i < 4; i++) spots.Add((enemy + side * ((i - 1.5f) * 3f), true));
            Garrison(spots, at); var theirs = squads[squads.Count - 1];
            fights.Add(new Fight { ours = ours, theirs = theirs }); return true;
        }

        /// <summary>Ours fire on the nearest German within 45 m; a fight is over once one side is all down.</summary>
        void TickFights(float dt, System.Action<Soldier, Vector3> shootAt)
        {
            foreach (var f in fights)
            {
                if (f.over) continue;
                bool oursUp = false, theirsUp = false;
                foreach (var m in f.theirs.men) if (!m.dead) theirsUp = true;
                foreach (var m in f.ours.men)
                {
                    if (m.dead) continue; oursUp = true;
                    var foe = NearestOf(m.pos, 45f, false); if (foe == null) continue;
                    FightOn(m, foe, shootAt, 0.07f);
                }
                if (!theirsUp || !oursUp) { f.over = true; if (!theirsUp && oursUp) fightWon?.Invoke(f.ours.men[0].pos); }
            }
        }

        /// <summary>One man firing on another: he turns to him, a rifleman a round every second or two, a machine gunner in
        /// bursts of six; a round drops the man aimed at one time in so many (hit).</summary>
        void FightOn(Soldier m, Soldier foe, System.Action<Soldier, Vector3> shootAt, float hit)
        {
            var to = foe.pos - m.pos; to.y = 0f; if (to.sqrMagnitude > 0.01f) { m.face = to.normalized; m.t.rotation = Quaternion.LookRotation(m.face, Vector3.up); }
            m.reload -= Time.deltaTime; if (m.reload > 0f) return;
            if (m.role == Role.Mg) { if (m.burst <= 0) m.burst = 6; m.burst--; m.reload = m.burst > 0 ? 0.11f : 2.5f + Random.value * 2f; hit *= 0.5f; }
            else m.reload = 1.2f + Random.value * 1.4f;
            shootAt(m, foe.pos + Vector3.up * (0.8f + Random.value * 0.6f) + Random.insideUnitSphere * 0.8f);
            if (Random.value < hit) Kill(foe);
        }

        /// <summary>The nearest living soldier of one side to a point within reach: ours (true) or theirs.</summary>
        Soldier NearestOf(Vector3 from, float reach, bool ours)
        {
            Soldier best = null; float bd = reach * reach;
            foreach (var s in squads) foreach (var m in s.men) { if (m.dead || m.ally != ours || m.gun != null) continue; var d = m.pos - from; d.y = 0f; float q = d.sqrMagnitude; if (q < bd) { bd = q; best = m; } }
            return best;
        }

        /// <summary>The nearest living soldier to a point within reach, or null.</summary>
        public Soldier Nearest(Vector3 from, float reach)
        {
            Soldier best = null; float bd = reach * reach;
            foreach (var s in squads) foreach (var m in s.men) { if (m.dead || m.ally) continue; var d = m.pos - from; d.y = 0f; float q = d.sqrMagnitude; if (q < bd) { bd = q; best = m; } }
            return best;
        }

        /// <summary>Down where he stood: the fallen figure when there is one (turned any way, off his gun), else the man
        /// tipped over on his back.</summary>
        public void Kill(Soldier m)
        {
            if (m.dead) return; m.dead = true; m.deadAge = 0f; fallen.Add(m);
            if (m.gun != null) { m.t.SetParent(transform, true); if (--m.gun.crew == 0 && !m.gun.dead) gunSilenced?.Invoke(m.gun); }
            foreach (var idle in m.t.GetComponentsInChildren<CrewIdle>()) { var sk = idle.GetComponentInChildren<SkinnedMeshRenderer>(); if (sk != null) Destroy(sk.gameObject); if (m.mr != null) m.mr.enabled = true; Destroy(idle); }
            if (m.ally && m.mf != null && poses.TryGetValue(m.side + "_dead", out var ad)) { m.mf.sharedMesh = ad.mesh; m.mr.sharedMaterial = ad.mat; m.t.position = new Vector3(m.pos.x, 0f, m.pos.z); m.t.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f); if (m.fig != null) m.fig.localRotation = Quaternion.identity; return; }
            if (m.mf != null && poses.TryGetValue("dead", out var d))
            {
                m.mf.sharedMesh = d.mesh; m.mr.sharedMaterial = d.mat; m.t.position = new Vector3(m.pos.x, 0f, m.pos.z); m.t.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                if (m.fig != null) m.fig.localRotation = Quaternion.identity;
                return;
            }
            m.t.position = m.pos + Vector3.up * 0.25f; m.t.rotation = Quaternion.LookRotation(m.face, Vector3.up) * Quaternion.Euler(-90f, 0f, Random.Range(-30f, 30f));
        }

        /// <summary>Kills every man within the radius of a blast; returns how many.</summary>
        public int Blast(Vector3 at, float radius)
        {
            int n = 0;
            foreach (var s in squads) foreach (var m in s.men) { if (m.dead || m.ally) continue; var d = m.pos - at; d.y = 0f; if (d.magnitude < radius) { Kill(m); n++; } }
            return n;
        }

        /// <summary>Anyone under the tracks of a moving tank.</summary>
        public int Crush(List<Vehicle> vehicles)
        {
            int n = 0;
            foreach (var s in squads) foreach (var m in s.men)
            {
                if (m.dead || m.ally) continue;   // ours step aside
                foreach (var v in vehicles) { if (v.dead || v.spec.isGun) continue; var d = m.pos - v.transform.position; d.y = 0f; if (d.magnitude < v.spec.radius * 0.7f) { Kill(m); n++; break; } }
            }
            return n;
        }
    }
}

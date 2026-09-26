using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IronNight
{
    /// <summary>
    /// The night raid: no lights and no shooting, the platoon slips through the searchlight line to a depot some 500 m
    /// ahead and blows it. The searchlights sweep the ground in slow circles; a pool of light resting on a tank for half a
    /// second raises the alarm, as does coming within 18 m of a sentry (an anti-tank gun and a tank at every post beyond
    /// 70 m of the start) or 24 m of a patrol, a shot fired, a tank hunter's Panzerfaust. Until then the enemy sleeps and our guns hold their
    /// fire unless told (a tap on an enemy is the order to open up - and the alarm). After the alarm the night is an
    /// ordinary one: flares, the near searchlights on the leader, the enemy from all sides. The depot blown: 1500, and
    /// 1500 more when it was reached unseen. Dawn with the depot standing is a failure. The best on each front is kept.
    /// </summary>
    public partial class Battle
    {
        class SneakPatrol { public Vector3 a, b; public bool toB = true; }
        bool sneak, sneakAlarm, sneakDone, sneakUnseen = true; float sneakSeen, sneakDawn, sneakPatrol = 25f, sneakWarned; Vector3 sneakDepot;
        readonly HashSet<long> sneakManned = new HashSet<long>(); readonly Dictionary<Vehicle, SneakPatrol> sneakPatrols = new Dictionary<Vehicle, SneakPatrol>();
        const float SneakDistance = 520f;
        /// <summary>The best raid on a front: 0 none, 1 the depot destroyed, 2 destroyed unseen.</summary>
        public static int SneakBest(string theatre) => PlayerPrefs.GetInt("sneak.best." + theatre, 0);

        /// <summary>The start: the depot far ahead with its guard, the platoon's own light out.</summary>
        void SneakBuild()
        {
            var L = Leader; var w = Vehicle.Create(Wingman, true, props.PushOut(L.transform.position + new Vector3(-5f, 0f, -7f), 2f), L.yaw); w.hp += Depot.WingmanHpBonus + wingmanBonus; platoon.Add(w);   // the raiding party: the leader and one
            var pos = props.PushOut(new Vector3(Random.Range(-50f, 50f), 0f, SneakDistance), 7f); sneakDepot = pos;
            objective = new Objective { pos = pos, n = 1, kind = "dump", marker = fx.Marker(pos, new Color(1f, 0.45f, 0.3f), 12f, true) };
            for (int i = 0; i < 7; i++) { float a = i * 0.9f + Random.value * 0.4f, r = i < 5 ? 3f : 5.5f; var p = props.Spawn(i < 5 ? "barrels" : "crate", pos + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * r, Random.value * 360f); if (p != null) objective.props.Add(p); }
            infantry.Spawn(pos + new Vector3(0f, 0f, -9f), Vector3.back);
            flareLight.enabled = false;
        }

        /// <summary>Before the alarm: the beams watched, the posts ahead manned as the platoon nears them, patrols crossing.</summary>
        void TickSneakQuiet(float dt)
        {
            var L = Leader;
            bool inLight = false; foreach (var v in platoon) if (props.InBeam(v.transform.position)) { inLight = true; break; }
            if (inLight)
            {
                sneakSeen += dt; if (Time.time > sneakWarned) { sneakWarned = Time.time + 1.5f; hud.Toast("In the light! Out of the beam", 1.2f); }
                if (sneakSeen >= 0.5f) { RaiseAlarm("Caught in a searchlight"); return; }
            }
            else sneakSeen = Mathf.Max(0f, sneakSeen - dt);
            // each post ahead gets its guard when the platoon comes within 110 m: an anti-tank gun and a tank, asleep
            foreach (var post in props.Posts(L.transform.position, L.Forward, 30f, 110f))
            {
                long key = ((long)Mathf.RoundToInt(post.x) << 32) ^ (Mathf.RoundToInt(post.z) & 0xffffffffL);
                if (post.magnitude < 70f || !sneakManned.Add(key)) continue;   // the posts by the start stay empty: the party has to get going first
                var toL = L.transform.position - post; toL.y = 0f; toL.Normalize(); var side = new Vector3(toL.z, 0f, -toL.x); float yaw = Mathf.Atan2(toL.x, toL.z);
                Foe(VehicleSpec.Pak40, props.PushOut(post + toL * 7f, 2.5f), yaw); Foe(VehicleSpec.PanzerIV, props.PushOut(post - toL * 6f + side * 8f, 3f), yaw);
            }
            // now and then a patrol of two crossing the way ahead, from one side to the other and back
            sneakPatrol -= dt;
            if (sneakPatrol <= 0f && sneakPatrols.Count < 4)
            {
                sneakPatrol = Random.Range(38f, 50f); float s = Random.value < 0.5f ? -1f : 1f; var f = L.Forward; var r = new Vector3(f.z, 0f, -f.x);
                var a = L.transform.position + f * 75f + r * s * 60f; var b = L.transform.position + f * 75f - r * s * 60f;
                for (int k = 0; k < 2; k++) { var e = Foe(VehicleSpec.PanzerIV, props.PushOut(a + f * (k * 9f), 3f), Mathf.Atan2(-r.x * s, -r.z * s)); sneakPatrols[e] = new SneakPatrol { a = a + f * (k * 9f), b = b + f * (k * 9f) }; }
                hud.Toast("A patrol crossing ahead · keep clear of it", 2.4f);
            }
            hud.SetGoals("Unseen · the depot " + Mathf.CeilToInt(Vector3.Distance(L.transform.position, sneakDepot)) + " m");
        }

        /// <summary>An enemy before the alarm: a patrol drives its beat at half speed, a sentry sits; either wakes the rest
        /// when one of ours comes close. True when it has been dealt with and the ordinary fight is to skip it.</summary>
        bool SneakAsleep(Vehicle e, float dist, float dt)
        {
            if (!sneak || sneakAlarm) return false;
            bool patrol = sneakPatrols.TryGetValue(e, out var beat);
            if (dist < (patrol ? 24f : 18f)) { RaiseAlarm(patrol ? "A patrol saw you" : "A sentry saw you"); return false; }
            if (patrol)
            {
                var goal = beat.toB ? beat.b : beat.a; var d = goal - e.transform.position; d.y = 0f;
                if (d.magnitude < 5f) beat.toB = !beat.toB; else e.Drive(Steer(e, new Vector2(d.x, d.z).normalized * 0.5f), dt);
            }
            e.Apply(); return true;
        }

        /// <summary>The alarm: flares over the platoon, the searchlights on it, the ordinary night from now on. Shells into
        /// the depot raise it too, but the depot was still reached unseen.</summary>
        void RaiseAlarm(string why, bool atDepot = false)
        {
            if (!sneak || sneakAlarm) return;
            sneakAlarm = true; if (!atDepot) sneakUnseen = false; sneakPatrols.Clear(); flareLight.enabled = true; spawnTimer = 3f;
            var L = Leader; HangStar(L.transform.position + L.Forward * 10f, "ALARM · " + why); Radio("tiger"); shake = Mathf.Max(shake, 0.4f);
        }

        /// <summary>Whether our guns may fire: before the alarm only when told (a tap on an enemy, or the depot to shell).</summary>
        bool SneakMayFire() => !sneak || sneakAlarm || focus != null || pointLeft > 0f;

        /// <summary>A shot of ours before the alarm is heard.</summary>
        void SneakShot() { if (sneak && !sneakAlarm) { bool atDepot = pointLeft > 0f && objective != null && (point - objective.pos).sqrMagnitude < 36f; RaiseAlarm(atDepot ? "The depot is under fire" : "Shots fired", atDepot); } }

        /// <summary>After the alarm: the goal line, and dawn when the depot is gone.</summary>
        void TickSneakLoud(float dt)
        {
            if (sneakDone) { sneakDawn = Mathf.Min(1f, sneakDawn + dt / 5f); hud.SetGoals("The depot is burning · dawn"); if (sneakDawn >= 1f && phase == Phase.Play) { Radio("dawn"); End(true); } return; }
            hud.SetGoals("Alarm · the depot " + Mathf.CeilToInt(Vector3.Distance(Leader.transform.position, sneakDepot)) + " m");
        }

        /// <summary>The depot blown: its bonus, the unseen one on top, the record.</summary>
        void SneakDone()
        {
            sneakDone = true; int bonus = 1500 + (sneakUnseen ? 1500 : 0); score += bonus; SneakRecord(sneakUnseen ? 2 : 1);
            hud.Toast(sneakUnseen ? "Depot destroyed unseen · +" + bonus : "Depot destroyed · +" + bonus, 3.4f); Sfx.Pickup();
        }

        void SneakRecord(int how) { if (how > SneakBest(theatre)) { PlayerPrefs.SetInt("sneak.best." + theatre, how); PlayerPrefs.Save(); } }

        /// <summary>The end of a raid: the depot destroyed (and unseen or not) or still standing; Again runs it once more.</summary>
        void SneakEnd(bool dawn, string statLine)
        {
            int best = SneakBest(theatre);
            string line = statLine + "\nNight raid · " + (sneakDone ? (sneakUnseen ? "the depot destroyed unseen" : "the depot destroyed") : "the depot still standing") + " · best on this front: " + (best == 2 ? "unseen" : best == 1 ? "destroyed" : "not yet");
            hud.ShowEnd(dawn, line, dawn ? !doubled : !revived, sneakDone ? (sneakUnseen ? "NIGHT RAID · UNSEEN" : "NIGHT RAID · DONE") : "NIGHT RAID · FAILED", sneakDone ? "Depot destroyed" : "Raid failed", "Raid again");
            hud.ShowHold(false);
            hud.OnAgain = () => { PlayerPrefs.SetString("sneak.launch", theatre); PlayerPrefs.Save(); SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); };
        }
    }
}

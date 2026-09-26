using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IronNight
{
    /// <summary>
    /// The convoy: five trucks drive north up the lane at x = 20 - a GMC column of the Red Ball Express in Normandy and
    /// the Ardennes, Lend-Lease Studebakers at Kursk - and the platoon brings them through 900 m of ambushes, every
    /// twenty-five seconds or so from one side and ahead: tanks out of the fields (StuGs from the third, Tigers from the
    /// fourth, Panthers from the sixth), an anti-tank gun dug in by the road, tank hunters in the ditch, mines laid on the
    /// lane (shoot them before the trucks get there). The enemy goes for the trucks. A truck on a mine stops until its
    /// wheel is changed and the ones behind wait; a burning one is driven round. Each truck through is worth 600; the
    /// night ends when the lead truck reaches the checkpoint, or when the last one burns. The best run on each front is kept.
    /// </summary>
    public partial class Battle
    {
        bool convoy, convoyDone; readonly List<Vehicle> trucks = new List<Vehicle>(); readonly Dictionary<Vehicle, float> truckSide = new Dictionary<Vehicle, float>();
        int trucksThrough, convoyAmbushes; float convoyAlong, convoyAmbush = 20f, convoyDawn, convoyWarned; Transform convoyGoal;
        public const int ConvoyTrucks = 5; const float ConvoyLane = 20f, ConvoyLength = 900f, ConvoySpeed = 4.5f, ConvoyGap = 14f;

        public static VehicleSpec TruckSpec(string theatre) => theatre == "kursk" ? VehicleSpec.Studebaker : VehicleSpec.Gmc;
        /// <summary>A convoy can be run on a front once its trucks are built.</summary>
        public static bool ConvoyReady(string theatre) => VehicleSpec.Available(TruckSpec(theatre));
        public static int ConvoyBest(string theatre) => PlayerPrefs.GetInt("convoy.best." + theatre, 0);

        /// <summary>The start: the trucks lined up on the lane behind the platoon, the full platoon, the checkpoint ahead.</summary>
        void ConvoyBuild()
        {
            var L = Leader; var spec = TruckSpec(theatre);
            for (int i = 0; i < ConvoyTrucks; i++) { var tr = Vehicle.Create(spec, true, new Vector3(ConvoyLane, 0f, -8f - i * ConvoyGap), 0f); trucks.Add(tr); truckSide[tr] = 0f; }
            convoyAlong = -8f;
            foreach (var side in new[] { -1f, 1f })
            {
                var v = Vehicle.Create(Wingman, true, props.PushOut(L.transform.position + new Vector3(side * 6f, 0f, -7f), 2f), L.yaw); v.hp += Depot.WingmanHpBonus + wingmanBonus; platoon.Add(v);
            }
            convoyGoal = fx.Marker(new Vector3(ConvoyLane, 0f, ConvoyLength), new Color(0.55f, 0.9f, 0.55f), 14f, true);
        }

        Vehicle LeadTruck() { foreach (var tr in trucks) if (!tr.dead) return tr; return null; }
        int TrucksLeft() { int n = 0; foreach (var tr in trucks) if (!tr.dead) n++; return n; }

        /// <summary>The convoy's own clock: the column rolling on, the ambushes, the checkpoint, dawn after it.</summary>
        void TickConvoy(float dt)
        {
            if (boss != null && !boss.dead) hud.SetBoss(boss.hp / boss.spec.hp);
            if (convoyDone)
            {
                convoyDawn = Mathf.Min(1f, convoyDawn + dt / 5f); hud.SetGoals("The convoy is through · " + trucksThrough + " of " + ConvoyTrucks + " trucks");
                if (convoyDawn >= 1f && phase == Phase.Play) { Radio("dawn"); End(true); }
                return;
            }
            var lead = LeadTruck(); if (lead == null) return;
            // the column: the lead truck sets the pace, each one keeps its distance from the one ahead, a stopped one holds the rest
            float ahead = float.MaxValue; bool first = true;
            foreach (var tr in trucks)
            {
                if (tr.dead) continue;
                float z = tr.transform.position.z, want = first ? convoyAlong : ahead - ConvoyGap;
                if (tr.trackOut > 0f) { tr.trackOut -= dt; want = z; }
                float step = Mathf.Clamp(want - z, 0f, ConvoySpeed * 1.3f * dt); first = false;
                // a burning truck or a wreck on the lane is driven round on the verge
                float side = 0f; foreach (var w in wrecks) { var d = w.v.transform.position - tr.transform.position; if (Mathf.Abs(d.x) < 3.6f && d.z > -4f && d.z < 11f) side = 3.8f; }
                truckSide[tr] = Mathf.MoveTowards(truckSide[tr], side, 2.4f * dt);
                var pos = props.PushOut(new Vector3(ConvoyLane + truckSide[tr], 0f, z + step), 1.2f);
                float dx = pos.x - tr.transform.position.x; tr.yaw = Mathf.LerpAngle(tr.yaw * Mathf.Rad2Deg, Mathf.Atan2(dx, Mathf.Max(0.05f, step)) * Mathf.Rad2Deg, 6f * dt) * Mathf.Deg2Rad;
                tr.transform.position = pos; tr.Apply(); ahead = pos.z;
            }
            if (lead.trackOut <= 0f) convoyAlong = Mathf.Min(convoyAlong + ConvoySpeed * dt, lead.transform.position.z + 3f);
            float togo = Mathf.Max(0f, ConvoyLength - lead.transform.position.z);
            hud.SetGoals("Convoy · " + TrucksLeft() + " of " + ConvoyTrucks + " trucks · " + Mathf.CeilToInt(togo) + " m to go");
            var L = Leader; var od = lead.transform.position - L.transform.position; od.y = 0f; hud.Objective(lead.transform.position, od.magnitude, cam, true);
            if (od.magnitude > 90f && Time.time > convoyWarned) { convoyWarned = Time.time + 8f; hud.Toast("Stay with the convoy", 2f); }
            // the trucks are solid: the platoon and the enemy are pushed off them
            foreach (var tr in trucks) { if (tr.dead) continue; foreach (var v in platoon) Shove(v, tr); foreach (var e in foes) if (!e.spec.isGun) Shove(e, tr); }
            convoyAmbush -= dt; if (convoyAmbush <= 0f) { convoyAmbush = Random.Range(24f, 30f); ConvoyAmbush(lead); }
            if (togo <= 0f)
            {
                trucksThrough = TrucksLeft(); int bonus = 600 * trucksThrough; score += bonus; convoyDone = true; ConvoyRecord(trucksThrough);
                hud.Popup(lead.transform.position, "+" + bonus, new Color(0.6f, 1f, 0.6f)); hud.Toast("The convoy is through · " + trucksThrough + " of " + ConvoyTrucks + " trucks · +" + bonus, 3.6f); Sfx.Pickup();
            }
        }

        static void Shove(Vehicle v, Vehicle tr)
        {
            var d = v.transform.position - tr.transform.position; d.y = 0f; float min = v.spec.radius * 0.7f + tr.spec.radius * 0.8f;
            if (d.magnitude < min && d.magnitude > 0.01f) v.transform.position = tr.transform.position + d.normalized * min + Vector3.up * v.transform.position.y;
        }

        /// <summary>An ambush from one side, ahead of the lead truck; the later, the heavier.</summary>
        void ConvoyAmbush(Vehicle lead)
        {
            convoyAmbushes++; int n = convoyAmbushes; float side = Random.value < 0.5f ? -1f : 1f; float z = lead.transform.position.z + Random.Range(45f, 70f);
            if (z > ConvoyLength + 20f) return;
            int tanks = Mathf.Min(4, 1 + n / 2);
            for (int k = 0; k < tanks; k++)
            {
                var pos = props.PushOut(new Vector3(ConvoyLane + side * Random.Range(46f, 60f), 0f, z + Random.Range(-16f, 16f)), 3f); float roll = Random.value;
                var spec = VehicleSpec.PanzerIV;
                if (n >= 4 && roll < 0.3f) spec = VehicleSpec.Tiger; else if (n >= 6 && roll < 0.5f && VehicleSpec.Available(VehicleSpec.Panther)) spec = VehicleSpec.Panther; else if (n >= 3 && roll < 0.65f) spec = VehicleSpec.StuG;
                Foe(spec, pos, Mathf.Atan2(-side, 0f));
            }
            if (n % 2 == 0 || n >= 5) Foe(VehicleSpec.Pak40, props.PushOut(new Vector3(ConvoyLane - side * Random.Range(22f, 30f), 0f, z + 22f), 3f), Mathf.Atan2(side, 0f));
            if (n >= 2) infantry.Spawn(props.PushOut(new Vector3(ConvoyLane + side * 9f, 0f, z - 8f), 2f), new Vector3(-side, 0f, 0f));
            if (n >= 3 && Random.value < 0.5f)
            {
                for (int k = 0; k < 3; k++) LayMine(new Vector3(ConvoyLane + Random.Range(-1.2f, 1.2f), 0f, z + 28f + k * 5f));
                hud.Toast("Mines on the road ahead · shoot them before the trucks get there", 3.2f);
            }
            else hud.Toast("Ambush · " + Clock(new Vector3(ConvoyLane + side * 50f, 0f, z)), 2.8f);
            Radio(n >= 4 ? "tiger" : "start");
            if (n == 6) Boss();   // the ace waits by the road for the last stretch
        }

        /// <summary>Of a truck and the tanks, whom an enemy shoots at: a truck, unless one of ours is nearer (an escort close by draws the fire).</summary>
        Vehicle ConvoyTarget(Vehicle e, Vehicle tank)
        {
            Vehicle best = null; float bd = float.MaxValue;
            foreach (var tr in trucks) { if (tr.dead) continue; float d = Dist(e, tr); if (d < bd) { bd = d; best = tr; } }
            if (best == null || tank == null) return best ?? tank;
            return bd < Dist(e, tank) * 1.15f ? best : tank;
        }

        /// <summary>A truck burnt out: its driver gets away; the last one ends the night.</summary>
        void TruckLost(Vehicle tr)
        {
            infantry.BailOut(tr.transform.position, new Vector3(-1f, 0f, 0f), Depot.Nation, 1);
            int left = TrucksLeft(); hud.Toast(left > 0 ? "Truck lost · " + left + " left" : "The last truck is burning", 2.6f); if (Random.value < 0.5f) Radio("hit");
            if (left == 0) End(false);
        }

        void ConvoyRecord(int through) { if (through > ConvoyBest(theatre)) { PlayerPrefs.SetInt("convoy.best." + theatre, through); PlayerPrefs.Save(); } }

        /// <summary>The end of a convoy: how many trucks got through and the best on this front; Again runs it once more.</summary>
        void ConvoyEnd(bool dawn, string statLine)
        {
            string line = statLine + "\nConvoy · " + (dawn ? trucksThrough : 0) + " of " + ConvoyTrucks + " trucks through · best on this front " + ConvoyBest(theatre);
            hud.ShowEnd(dawn, line, dawn ? !doubled : !revived, dawn ? "CONVOY · " + trucksThrough + " OF " + ConvoyTrucks + " THROUGH" : "CONVOY · " + TrucksLeft() + (TrucksLeft() == 1 ? " TRUCK LEFT" : " TRUCKS LEFT"), dawn ? "Convoy through" : "Convoy lost", "Run it again");
            hud.ShowHold(false);
            hud.OnAgain = () => { PlayerPrefs.SetString("convoy.launch", theatre); PlayerPrefs.Save(); SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); };
        }
    }
}

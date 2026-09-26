using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IronNight
{
    /// <summary>
    /// The last stand: the platoon holds a crossroads in a village through ten waves, each heavier than the last -
    /// Panzer IVs first, then StuGs and Tigers, Panthers from the sixth, a Panzerkeil or a column with the fifth, a
    /// counterattack from the rear with the eighth, the Tiger ace with the tenth; half-tracks and tank hunters on foot
    /// from the second. Before the first wave and between the waves, fifteen seconds to dig in: sandbags (cover for a
    /// tank behind them), hedgehogs (the enemy has to drive round them) and mines, paid for with the supplies every kill
    /// brings. After each wave the racks are half filled again and every tank patched by one. The leader stays inside
    /// the ring. Dawn comes when the tenth wave is beaten; the best wave held on each front is kept.
    /// </summary>
    public partial class Battle
    {
        bool stand, standWon; int standWave, standSupply = 5, standQuota, standSpawned; float standBreak, standSpawnTimer, standDawn, standBearing, standEdgeSaid; string standArmed;
        Vector3 standAt; LineRenderer standRing;
        readonly List<Vector3> standBags = new List<Vector3>();
        public const int StandWaves = 10; const float StandHold = 32f, StandBreakTime = 15f;

        public static int StandCost(string item) => item == "mines" ? 3 : 2;
        public static int StandBest(string theatre) => PlayerPrefs.GetInt("stand.best." + theatre, 0);

        /// <summary>The position at the start: the full platoon, the ring on the ground, the kit on the screen.</summary>
        void StandBuild()
        {
            var L = Leader; standAt = L.transform.position; standBreak = 12f; var f = L.Forward; var r = new Vector3(f.z, 0f, -f.x);
            foreach (var arg in System.Environment.GetCommandLineArgs()) if (arg.StartsWith("--standwave=")) standWave = Mathf.Clamp(int.Parse(arg.Substring(12)), 1, StandWaves) - 1;   // test switch: the first wave is this one
            foreach (var side in new[] { -1f, 1f })
            {
                var v = Vehicle.Create(Wingman, true, props.PushOut(standAt - f * 7f + r * side * 6f, 2f), L.yaw); v.hp += Depot.WingmanHpBonus + wingmanBonus; platoon.Add(v);
            }
            var go = new GameObject("StandRing"); standRing = go.AddComponent<LineRenderer>(); standRing.loop = true; standRing.useWorldSpace = true; standRing.positionCount = 96;
            for (int i = 0; i < 96; i++) { float a = i * Mathf.PI * 2f / 96f; standRing.SetPosition(i, standAt + new Vector3(Mathf.Sin(a) * StandHold, 0.12f, Mathf.Cos(a) * StandHold)); }
            standRing.widthMultiplier = 0.45f; standRing.material = new Material(shellTemplate); standRing.material.mainTexture = glowTex;
            standRing.startColor = standRing.endColor = new Color(1f, 0.6f, 0.22f, 0.6f); standRing.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; standRing.receiveShadows = false;
            hud.OnStandItem = StandArm;
            hud.OnStandReady = () => { if (standBreak > 0.5f && !standWon) { standBreak = 0.01f; int early = 50 * (standWave + 1); score += early; hud.Toast("Early · +" + early, 1.4f); } };
            hud.ShowStandKit(true); hud.SetStandKit(standSupply, null, true);
        }

        /// <summary>The stand's own clock: the break to dig in, the wave coming in by ones and twos, dawn after the tenth.</summary>
        void TickStand(float dt)
        {
            if (boss != null && !boss.dead) hud.SetBoss(boss.hp / boss.spec.hp);
            if (standWon)
            {
                standDawn = Mathf.Min(1f, standDawn + dt / 6f); hud.SetGoals("The crossroads held · dawn");
                if (standDawn >= 1f && phase == Phase.Play) { Radio("dawn"); End(true); }
                return;
            }
            if (standBreak > 0f)
            {
                standBreak -= dt;
                hud.SetGoals((standWave == 0 ? "Dig in · the first wave in " : "Wave " + standWave + " held · the next in ") + Mathf.Max(1, Mathf.CeilToInt(standBreak)) + " s");
                hud.SetStandKit(standSupply, standArmed, true);
                if (standBreak <= 0f) StandWave();
                return;
            }
            standSpawnTimer -= dt;
            if (standSpawned < standQuota && standSpawnTimer <= 0f) { standSpawnTimer = Mathf.Lerp(1.8f, 0.9f, standWave / (float)StandWaves); StandFoe(); }
            int left = standQuota - standSpawned + foes.Count;
            hud.SetGoals("Wave " + standWave + " of " + StandWaves + " · " + left + " left"); hud.SetStandKit(standSupply, standArmed, false);
            if (standSpawned >= standQuota && foes.Count == 0) StandCleared();
        }

        /// <summary>The next wave: its tanks from one quarter, the extras of the fifth, eighth and tenth, the infantry.</summary>
        void StandWave()
        {
            standWave++; standSpawned = 0; standQuota = 2 + standWave; standSpawnTimer = 0.4f; standBearing = Random.value * Mathf.PI * 2f; standArmed = null;
            var dir = new Vector3(Mathf.Sin(standBearing), 0f, Mathf.Cos(standBearing));
            hud.Toast("Wave " + standWave + " · tanks, " + Clock(standAt + dir * 60f), 2.8f); Radio("start");
            if (standWave == 5) { if (theatre == "kursk") Panzerkeil(4); else Column(); }
            if (standWave == 8) Counterattack();
            if (standWave == StandWaves) Boss();
            if (standWave >= 2 && VehicleSpec.Available(VehicleSpec.Halftrack) && Random.value < 0.65f)
            {
                var hd = Quaternion.Euler(0f, 40f, 0f) * dir; var hp = props.PushOut(standAt + hd * 62f, 3f); Foe(VehicleSpec.Halftrack, hp, Mathf.Atan2(-hd.x, -hd.z));
            }
            if (standWave >= 4)
            {
                var id = Quaternion.Euler(0f, Random.value < 0.5f ? -70f : 70f, 0f) * dir; var ip = props.PushOut(standAt + id * 38f, 2f);
                infantry.Spawn(ip, -id); hud.Toast("Infantry! Panzerfausts, " + Clock(ip), 2.8f);
            }
        }

        /// <summary>One tank of the wave, from its quarter, the heavier the later.</summary>
        void StandFoe()
        {
            standSpawned++; float a = standBearing + Random.Range(-0.6f, 0.6f); var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
            var pos = props.PushOut(standAt + dir * Random.Range(58f, 66f), 3f); int w = standWave; bool ks = theatre == "kursk"; float roll = Random.value;
            var spec = VehicleSpec.PanzerIV;
            if (w >= 4 && roll < Mathf.Lerp(ks ? 0.22f : 0.15f, 0.4f, (w - 4) / 6f)) spec = VehicleSpec.Tiger;
            else if (w >= 6 && roll < 0.55f && VehicleSpec.Available(VehicleSpec.Panther)) spec = VehicleSpec.Panther;
            else if (w >= 3 && roll < 0.72f) spec = !ks && Random.value < 0.5f && VehicleSpec.Available(VehicleSpec.Hetzer) ? VehicleSpec.Hetzer : VehicleSpec.StuG;
            if (w >= 9 && spec == VehicleSpec.Tiger && winter && VehicleSpec.Available(VehicleSpec.KingTiger) && Random.value < 0.3f) spec = VehicleSpec.KingTiger;
            Foe(spec, pos, Mathf.Atan2(-dir.x, -dir.z));
            if (spec == VehicleSpec.Tiger) { hud.Toast("Tiger! " + Clock(pos), 2.4f); if (Random.value < 0.5f) Radio("tiger"); }
            else if (spec == VehicleSpec.Panther) hud.Toast("Panther! " + Clock(pos), 2.4f);
        }

        /// <summary>A wave beaten: its bonus, two supplies, the racks half filled again, every tank patched by one; after
        /// the tenth, dawn.</summary>
        void StandCleared()
        {
            int bonus = 150 * standWave; score += bonus; standSupply += 2; StandRecord(standWave);
            apRounds = Mathf.Min(apMax, apRounds + Mathf.CeilToInt(apMax * 0.5f)); heRounds = Mathf.Min(heMax, heRounds + Mathf.CeilToInt(heMax * 0.5f)); hud.SetAmmo(apRounds, heRounds, loadHe);
            foreach (var v in platoon) v.hp = v == Leader ? Mathf.Min(Depot.LeaderHp, v.hp + 1f) : Mathf.Min(v.spec.hp + Depot.WingmanHpBonus + wingmanBonus, v.hp + 1f);
            hud.SetLeader(Mathf.CeilToInt(Leader.hp), Mathf.CeilToInt(Depot.LeaderHp)); hud.Popup(Leader.transform.position, "+" + bonus, new Color(1f, 0.75f, 0.35f)); Sfx.Pickup();
            if (standWave >= StandWaves) { standWon = true; score += 2000; hud.Toast("The crossroads held · dawn is coming · +2000", 3.6f); hud.ShowStandKit(false); return; }
            standBreak = StandBreakTime; hud.Toast("Wave " + standWave + " held · +" + bonus + " · ammunition up · 2 supplies", 3f);
        }

        /// <summary>A kill in the stand: a supply for the kit, two for the big cats.</summary>
        void StandKill(Vehicle v)
        {
            bool big = v.spec == VehicleSpec.Tiger || v.spec == VehicleSpec.TigerAce || v.spec == VehicleSpec.KingTiger || v.spec == VehicleSpec.Panther;
            standSupply += big ? 2 : 1;
        }

        /// <summary>The waves held, kept when it is the best on this front.</summary>
        void StandRecord(int held) { if (held > StandBest(theatre)) { PlayerPrefs.SetInt("stand.best." + theatre, held); PlayerPrefs.Save(); } }

        /// <summary>A kit button: takes the item up for the next tap on the ground, or puts it away.</summary>
        void StandArm(string item)
        {
            if (phase != Phase.Play || standWon) return;
            if (standArmed == item) { standArmed = null; hud.Toast("Put away", 1.2f); }
            else if (standSupply < StandCost(item)) { hud.Toast("Not enough supplies · every kill brings one", 2f); return; }
            else { standArmed = item; airArmed = false; hud.Toast(item == "bags" ? "Tap the ground for the sandbags" : item == "hogs" ? "Tap the ground for the hedgehogs" : "Tap the ground for the mines", 2.2f); }
            Sfx.Click(); hud.SetStandKit(standSupply, standArmed, standBreak > 0f);
        }

        /// <summary>Puts the armed item down where the ground was tapped, inside the ring, facing out from the crossroads;
        /// true when the tap was taken for it.</summary>
        bool StandPlace(Vector3 g)
        {
            if (!stand || standArmed == null) return false;
            var off = g - standAt; off.y = 0f;
            if (off.magnitude > StandHold) { hud.Toast("Inside the ring", 1.4f); return true; }
            int cost = StandCost(standArmed); if (standSupply < cost) { standArmed = null; return true; }
            var face = off.magnitude > 1f ? off.normalized : Leader.Forward; var side = new Vector3(face.z, 0f, -face.x); float yaw = Mathf.Atan2(face.x, face.z);
            if (standArmed == "bags") { props.AddModel("sandbags", g, yaw); standBags.Add(g); hud.Popup(g, "Sandbags", new Color(0.85f, 0.8f, 0.6f)); }
            else if (standArmed == "hogs") { foreach (var k in new[] { -3.8f, 0f, 3.8f }) props.AddModel("k_hedgehogs", g + side * k, Random.value * 6.28f); hud.Popup(g, "Hedgehogs", new Color(0.8f, 0.8f, 0.75f)); }
            else { foreach (var k in new[] { -2.4f, 0f, 2.4f }) LayMine(g + side * k, true); hud.Popup(g, "Mines", new Color(1f, 0.8f, 0.5f)); }
            standSupply -= cost; standArmed = null; fx.Dust(g); Sfx.Pickup(); hud.SetStandKit(standSupply, null, standBreak > 0f);
            return true;
        }

        /// <summary>A shell at one of ours is stopped by the sandbags when they stand between: a bag ring within six and a
        /// half metres of the tank, on the side the shell comes from.</summary>
        bool StandCovered(Vector3 tank, Vector3 shell)
        {
            if (!stand) return false;
            var toShell = shell - tank; toShell.y = 0f; if (toShell.sqrMagnitude < 0.01f) return false; toShell.Normalize();
            foreach (var b in standBags) { var toBag = b - tank; toBag.y = 0f; float d = toBag.magnitude; if (d < 6.5f && d > 0.5f && Vector3.Dot(toBag / d, toShell) > 0.35f) return true; }
            return false;
        }

        /// <summary>The leader does not leave the position.</summary>
        void StandKeep(Vehicle L)
        {
            var off = L.transform.position - standAt; off.y = 0f;
            if (off.magnitude > StandHold) { var p = standAt + off.normalized * StandHold; L.transform.position = new Vector3(p.x, L.transform.position.y, p.z); if (Time.time > standEdgeSaid) { standEdgeSaid = Time.time + 6f; hud.Toast("Hold the crossroads · stay inside the ring", 1.8f); } }
        }

        /// <summary>The end of a stand: how many waves held and the best on this front; Again holds it once more.</summary>
        void StandEnd(bool dawn, string statLine)
        {
            int held = dawn ? StandWaves : standBreak > 0f ? standWave : Mathf.Max(0, standWave - 1); StandRecord(held);
            string line = statLine + "\nLast stand · " + held + (held == 1 ? " wave" : " waves") + " held · best on this front " + StandBest(theatre) + " of " + StandWaves;
            hud.ShowStandKit(false);
            hud.ShowEnd(dawn, line, dawn ? !doubled : !revived, dawn ? "LAST STAND · ALL TEN WAVES" : "LAST STAND · WAVE " + standWave, dawn ? "Held till dawn" : "The crossroads fell", "Hold again");
            hud.ShowHold(false);
            hud.OnAgain = () => { PlayerPrefs.SetString("stand.launch", theatre); PlayerPrefs.Save(); SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); };
        }
    }
}

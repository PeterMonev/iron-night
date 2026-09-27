using System.Collections.Generic;
using UnityEngine;

namespace IronNight
{
    /// <summary>
    /// What brings a player back. The quartermaster gathers supplies while the game is closed, up to eight hours (three
    /// times as much for an ad). Mail call: a gift a day for seven days, the seventh the best, a missed day starting the
    /// row again. The crates: supply crates and officer's crates, opened in their own little stage for three rewards,
    /// from the calendar, the boss, dawn now and then, and a free one for an ad every four hours. Crates are never sold
    /// for gold or money: nothing random is ever paid for.
    /// </summary>
    public static class Rewards
    {
        static System.DateTime Now => GameClock.UtcNow;
        static System.DateTime Ticks(string key) => long.TryParse(PlayerPrefs.GetString(key, ""), out var t) ? new System.DateTime(t, System.DateTimeKind.Utc) : System.DateTime.MinValue;
        static void SetTicks(string key, System.DateTime at) { PlayerPrefs.SetString(key, at.Ticks.ToString()); PlayerPrefs.Save(); }

        // ---- the quartermaster ----
        public const float QmCapHours = 8f;
        public const int QmXpPerHour = 40, QmAdMul = 3;
        public static int QmPointsPerHour => 120 + 20 * Depot.RankIndex;
        /// <summary>Hours gathered since the last collection, at most eight. A clock turned back starts him again.</summary>
        public static float QmHours
        {
            get
            {
                var since = Ticks("qm.since"); if (since == System.DateTime.MinValue || since > Now) { SetTicks("qm.since", Now); return 0f; }
                return Mathf.Min(QmCapHours, (float)(Now - since).TotalHours);
            }
        }
        static float PremiumMul => Depot.Premium ? Depot.PremiumMul : 1f;
        public static int QmPoints => Mathf.FloorToInt(QmHours * QmPointsPerHour * PremiumMul);
        public static int QmXp => Mathf.FloorToInt(QmHours * QmXpPerHour * PremiumMul);
        public static void QmCollect(int mul) { int p = QmPoints, x = QmXp; SetTicks("qm.since", Now); Depot.AddPoints(p * mul); Depot.AddCrewXp(x * mul); }

        // ---- mail call: a gift a day ----
        public class Gift { public string kind; public int amount; }   // points, xp, gold, supply, officer
        public static readonly Gift[] Week =
        {
            new Gift { kind = "points", amount = 300 },
            new Gift { kind = "xp", amount = 200 },
            new Gift { kind = "supply", amount = 1 },
            new Gift { kind = "gold", amount = 10 },
            new Gift { kind = "points", amount = 800 },
            new Gift { kind = "xp", amount = 400 },
            new Gift { kind = "officer", amount = 1 },
        };
        static string Day(int back) => GameClock.Now.AddDays(-back).ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
        /// <summary>Days of the row already collected (0..7) as the calendar shows them today.</summary>
        public static int MailDone
        {
            get
            {
                string last = PlayerPrefs.GetString("mail.last", ""); int row = PlayerPrefs.GetInt("mail.row", 0);
                if (last == Day(0)) return row;
                return last == Day(1) && row < 7 ? row : 0;   // a missed day, or the week done: a new row
            }
        }
        /// <summary>Today's gift still waiting.</summary>
        public static bool MailReady => PlayerPrefs.GetString("mail.last", "") != Day(0);
        /// <summary>Takes today's gift (twice over after an ad). Returns it, or null if today's is taken.</summary>
        public static Gift MailCollect(int mul)
        {
            if (!MailReady) return null;
            int day = MailDone; var g = Week[day];
            Give(g.kind, g.amount * mul);
            PlayerPrefs.SetString("mail.last", Day(0)); PlayerPrefs.SetInt("mail.row", day + 1); PlayerPrefs.Save();
            return g;
        }

        // ---- the crates ----
        public static int Crates(string kind) => PlayerPrefs.GetInt("crates." + kind, 0);
        public static int CratesTotal => Crates("supply") + Crates("officer");
        public static void AddCrates(string kind, int n) { PlayerPrefs.SetInt("crates." + kind, Mathf.Max(0, Crates(kind) + n)); PlayerPrefs.Save(); }
        public static string CrateName(string kind) => kind == "officer" ? "Officer's crate" : "Supply crate";
        /// <summary>The crate to open next: the officer's first.</summary>
        public static string NextCrate => Crates("officer") > 0 ? "officer" : Crates("supply") > 0 ? "supply" : null;

        // a free supply crate for an ad, every four hours
        public const float FreeCrateHours = 4f;
        public static float FreeCrateWait { get { var t = Ticks("crates.freeAt"); return t == System.DateTime.MinValue || t > Now.AddHours(FreeCrateHours) ? 0f : Mathf.Max(0f, (float)(t - Now).TotalHours); } }
        public static bool FreeCrateReady => FreeCrateWait <= 0f;
        public static void FreeCrateTaken() { SetTicks("crates.freeAt", Now.AddHours(FreeCrateHours)); AddCrates("supply", 1); }

        public class Loot { public string kind, id; public int amount; }   // points, xp, gold, premium (days), camo (id)
        /// <summary>Opens a crate of the kind: three rewards, rolled and given at once (the crate is gone).</summary>
        public static List<Loot> Open(string kind)
        {
            if (Crates(kind) <= 0) return null;
            AddCrates(kind, -1); Depot.Tally("cratesOpened", 1);
            var list = new List<Loot>(); bool officer = kind == "officer";
            list.Add(new Loot { kind = "points", amount = Round(officer ? Random.Range(1200, 2500) : Random.Range(250, 700), 50) });
            list.Add(new Loot { kind = "xp", amount = Round(officer ? Random.Range(600, 1200) : Random.Range(150, 400), 25) });
            float r = Random.value;
            if (officer) list.Add(r < 0.4f ? new Loot { kind = "gold", amount = Round(Random.Range(25, 61), 5) } : r < 0.7f ? new Loot { kind = "premium", amount = 1 } : CamoOr(50));
            else list.Add(r < 0.55f ? new Loot { kind = "points", amount = Round(Random.Range(400, 900), 50) } : r < 0.8f ? new Loot { kind = "xp", amount = Round(Random.Range(250, 500), 25) } : r < 0.95f ? new Loot { kind = "gold", amount = Round(Random.Range(5, 16), 5) } : CamoOr(20));
            foreach (var l in list) GiveLoot(l);
            return list;
        }
        /// <summary>What can come out of each crate, for the player to read: nothing in them is ever paid for.</summary>
        public static string Odds(string kind) => kind == "officer"
            ? "1,200–2,500 points · 600–1,200 crew XP · and one of: 25–60 gold (40%), a day of premium (30%), a camouflage (30%, gold if all are yours)"
            : "250–700 points · 150–400 crew XP · and one of: more points (55%), more crew XP (25%), 5–15 gold (15%), a camouflage (5%, gold if all are yours)";

        static int Round(int v, int step) => Mathf.Max(step, Mathf.RoundToInt(v / (float)step) * step);
        /// <summary>A camouflage not yet owned, or gold when every one is.</summary>
        static Loot CamoOr(int gold)
        {
            var free = new List<Depot.Camo>(); foreach (var c in Depot.Camos) if (!Depot.OwnsCamo(c)) free.Add(c);
            return free.Count > 0 ? new Loot { kind = "camo", id = free[Random.Range(0, free.Count)].id } : new Loot { kind = "gold", amount = gold };
        }
        /// <summary>A reward into the depot; a camouflage already owned turns into twenty gold.</summary>
        public static void GiveLoot(Loot l)
        {
            if (l.kind == "camo") { var c = System.Array.Find(Depot.Camos, x => x.id == l.id); if (c != null && !Depot.OwnsCamo(c)) { PlayerPrefs.SetInt("depot.camo." + c.id, 1); PlayerPrefs.Save(); } else Depot.AddGold(20); return; }
            Give(l.kind, l.amount);
        }
        static void Give(string kind, int amount)
        {
            switch (kind)
            {
                case "points": Depot.AddPoints(amount); break;
                case "xp": Depot.AddCrewXp(amount); break;
                case "gold": Depot.AddGold(amount); break;
                case "premium": Depot.AddPremiumDays(amount); break;
                case "supply": case "officer": AddCrates(kind, amount); break;
            }
        }
    }
}

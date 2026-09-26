using System.Collections.Generic;
using UnityEngine;

namespace IronNight
{
    /// <summary>
    /// War bonds: the season pass. A season runs five weeks (the first from 27 September 2026), each named for a campaign
    /// and with a camouflage of its own. Stars come from the nights (one for fighting, one for dawn, one for a boss or
    /// an ace) and from five weekly orders; six stars make a tier, forty tiers a season. Every tier has a free reward and
    /// a gold one. The gold bond (500 gold) gives all the gold rewards and a star more every night; without it, the gold
    /// rewards that are not gold or the season's camouflage can be taken for a rewarded ad. Whatever was reached and
    /// not taken is given out when the season ends.
    /// </summary>
    public static class Bonds
    {
        static readonly System.DateTime Epoch = new System.DateTime(2026, 9, 27);
        public const int Tiers = 40, StarsPerTier = 6, SeasonDays = 35, BondGold = 500, TierGold = 60;

        static int DaysIn => Mathf.Max(0, (System.DateTime.Now.Date - Epoch).Days);
        public static int Season => DaysIn / SeasonDays + 1;
        public static string SeasonName(int s) => ((s - 1) % 3) == 0 ? "Operation Cobra" : ((s - 1) % 3) == 1 ? "The Bulge" : "Citadel";
        public static string SeasonCamo(int s) => ((s - 1) % 3) == 0 ? "hedgerow" : ((s - 1) % 3) == 1 ? "whitewash" : "steppe";
        /// <summary>Time left in the season, short: "34 d 12 h", "5 h 20 min".</summary>
        public static string Left
        {
            get
            {
                var end = Epoch.AddDays(Season * SeasonDays); var left = end - System.DateTime.Now;
                return left.TotalDays >= 1 ? (int)left.TotalDays + " d " + left.Hours + " h" : (int)left.TotalHours + " h " + left.Minutes + " min";
            }
        }

        static string K(int s, string k) => "bonds." + s + "." + k;
        public static int StarsOf(int s) => PlayerPrefs.GetInt(K(s, "stars"), 0);
        public static int Stars => StarsOf(Season);
        public static int TierOf(int s) => Mathf.Min(Tiers, StarsOf(s) / StarsPerTier);
        /// <summary>Tiers reached this season (0..40).</summary>
        public static int Tier => TierOf(Season);
        public static bool GoldBondOf(int s) => PlayerPrefs.GetInt(K(s, "bond"), 0) == 1;
        public static bool GoldBond => GoldBondOf(Season);
        /// <summary>Test switch --bondstars=N: the season's stars set outright.</summary>
        public static void TestStars(int n) { PlayerPrefs.SetInt(K(Season, "stars"), Mathf.Clamp(n, 0, Tiers * StarsPerTier)); PlayerPrefs.Save(); }
        public static void AddStars(int n) { if (n <= 0) return; PlayerPrefs.SetInt(K(Season, "stars"), Mathf.Min(Tiers * StarsPerTier, Stars + n)); PlayerPrefs.Save(); }

        /// <summary>A tier's free reward: points and crew experience in turn, a supply crate every fifth, gold every tenth,
        /// an officer's crate at the top.</summary>
        public static Rewards.Loot FreeReward(int t)
        {
            if (t == Tiers) return new Rewards.Loot { kind = "officer", amount = 1 };
            if (t % 10 == 0) return new Rewards.Loot { kind = "gold", amount = 15 };
            if (t % 5 == 0) return new Rewards.Loot { kind = "supply", amount = 1 };
            return t % 2 == 1 ? new Rewards.Loot { kind = "points", amount = 300 + 30 * t } : new Rewards.Loot { kind = "xp", amount = 150 + 15 * t };
        }
        /// <summary>A tier's gold reward: gold straight away at the first, crates, premium days, gold every third, and the
        /// season's own camouflage with a hundred gold at the top.</summary>
        public static Rewards.Loot GoldReward(int t, int season)
        {
            if (t == Tiers) return new Rewards.Loot { kind = "camo", id = SeasonCamo(season) };
            if (t == 1) return new Rewards.Loot { kind = "gold", amount = 50 };
            if (t == 10 || t == 30) return new Rewards.Loot { kind = "officer", amount = 1 };
            if (t == 20) return new Rewards.Loot { kind = "premium", amount = 3 };
            if (t % 5 == 0) return new Rewards.Loot { kind = "premium", amount = 1 };
            if (t % 3 == 0) return new Rewards.Loot { kind = "gold", amount = 25 };
            return t % 2 == 1 ? new Rewards.Loot { kind = "supply", amount = 1 } : new Rewards.Loot { kind = "points", amount = 1000 + 50 * t };
        }
        public static Rewards.Loot GoldReward(int t) => GoldReward(t, Season);
        /// <summary>Gold rewards an ad can take without the gold bond: not gold, and not the season's camouflage.</summary>
        public static bool AdClaimable(Rewards.Loot l) => l.kind != "gold" && l.kind != "camo";

        public static bool ClaimedOf(int s, int t, bool gold) => PlayerPrefs.GetInt(K(s, (gold ? "g" : "f") + t), 0) == 1;
        public static bool Claimed(int t, bool gold) => ClaimedOf(Season, t, gold);
        /// <summary>Takes a tier's reward (a gold one needs the bond, or an ad already watched). False if not due.</summary>
        public static bool Claim(int t, bool gold, bool byAd = false)
        {
            if (t < 1 || t > Tier || Claimed(t, gold)) return false;
            if (gold && !GoldBond && !(byAd && AdClaimable(GoldReward(t)))) return false;
            GiveReward(gold ? GoldReward(t) : FreeReward(t));
            PlayerPrefs.SetInt(K(Season, (gold ? "g" : "f") + t), 1); PlayerPrefs.Save(); return true;
        }
        /// <summary>Everything due that needs no ad; how many rewards that was.</summary>
        public static int ClaimAll() { int n = 0; for (int t = 1; t <= Tier; t++) { if (Claim(t, false)) n++; if (GoldBond && Claim(t, true)) n++; } return n; }
        /// <summary>Rewards waiting to be taken without an ad (the badge on the title's button).</summary>
        public static int Waiting { get { int n = 0; for (int t = 1; t <= Tier; t++) { if (!Claimed(t, false)) n++; if (GoldBond && !Claimed(t, true)) n++; } return n; } }
        static void GiveReward(Rewards.Loot l)
        {
            if (l.kind == "camo") { var c = System.Array.Find(Depot.Camos, x => x.id == l.id); if (c != null && !Depot.OwnsCamo(c)) { PlayerPrefs.SetInt("depot.camo." + c.id, 1); PlayerPrefs.Save(); } else Depot.AddGold(100); Depot.AddGold(100); return; }   // the top reward: the camouflage and a hundred gold
            Rewards.GiveLoot(l);
        }

        public static bool BuyBond() { if (GoldBond || !Depot.SpendGold(BondGold)) return false; PlayerPrefs.SetInt(K(Season, "bond"), 1); PlayerPrefs.Save(); return true; }
        public static bool BuyTier() { if (Tier >= Tiers || !Depot.SpendGold(TierGold)) return false; int need = (Tier + 1) * StarsPerTier - Stars; AddStars(need); return true; }

        /// <summary>The night's stars: one for fighting, one for dawn, one for a boss or an ace, one more with the bond.</summary>
        public static int NightStars(bool dawn, bool bigKill) => 1 + (dawn ? 1 : 0) + (bigKill ? 1 : 0) + (GoldBond ? 1 : 0);

        // ---- weekly orders: five a week, counted from the running tallies ----
        public class Order { public string key, text; public int need, stars; }
        static readonly Order[] Pool =
        {
            new Order { key = "nights", text = "Fight {0} nights", need = 8, stars = 6 },
            new Order { key = "kills", text = "Destroy {0} enemy tanks", need = 40, stars = 6 },
            new Order { key = "tigers", text = "Knock out {0} Tigers or Panthers", need = 8, stars = 6 },
            new Order { key = "guns", text = "Silence {0} anti-tank guns", need = 10, stars = 6 },
            new Order { key = "dawns", text = "Hold until dawn {0} times", need = 4, stars = 6 },
            new Order { key = "infantry", text = "Rout {0} infantry", need = 60, stars = 6 },
            new Order { key = "objectives", text = "Carry out {0} objectives", need = 6, stars = 6 },
            new Order { key = "acesNamed", text = "Finish {0} named aces", need = 2, stars = 6 },
            new Order { key = "tracked", text = "Break the tracks of {0} tanks", need = 10, stars = 6 },
            new Order { key = "lamps", text = "Shoot out {0} searchlights", need = 6, stars = 6 },
            new Order { key = "dailies", text = "Fight {0} daily challenges", need = 2, stars = 6 },
            new Order { key = "standWaves", text = "Hold {0} waves in a last stand", need = 10, stars = 6 },
            new Order { key = "convoyTrucks", text = "Bring {0} convoy trucks through", need = 8, stars = 6 },
            new Order { key = "sneakDepots", text = "Blow {0} depots on night raids", need = 2, stars = 6 },
            new Order { key = "cratesOpened", text = "Open {0} crates", need = 3, stars = 6 },
        };
        public static int Week => DaysIn / 7;
        /// <summary>Time to the next orders, short.</summary>
        public static string WeekLeft { get { var left = Epoch.AddDays((Week + 1) * 7) - System.DateTime.Now; return left.TotalDays >= 1 ? (int)left.TotalDays + " d " + left.Hours + " h" : (int)left.TotalHours + " h " + left.Minutes + " min"; } }
        /// <summary>The week's five: the first always "fight nights" or "destroy tanks", the rest drawn from the pool.</summary>
        public static Order[] WeekOrders
        {
            get
            {
                var rng = new System.Random(Week * 7919 + 17); var rest = new List<Order>(); for (int i = 2; i < Pool.Length; i++) rest.Add(Pool[i]);
                var list = new List<Order> { Pool[rng.Next(2)] };
                while (list.Count < 5) { int k = rng.Next(rest.Count); list.Add(rest[k]); rest.RemoveAt(k); }
                return list.ToArray();
            }
        }
        static int Count(string key) => key == "nights" ? Depot.NightsFought : Depot.Total(key);
        static string W(string k) => "bonds.w" + Week + "." + k;
        /// <summary>The week's starting counts, taken the first time the week is seen (at start, before any night).</summary>
        public static void EnsureWeek() { foreach (var o in WeekOrders) if (!PlayerPrefs.HasKey(W("base." + o.key))) PlayerPrefs.SetInt(W("base." + o.key), Count(o.key)); PlayerPrefs.Save(); }
        public static int Progress(Order o) => Mathf.Clamp(Count(o.key) - PlayerPrefs.GetInt(W("base." + o.key), Count(o.key)), 0, o.need);
        public static bool OrderClaimed(int i) => PlayerPrefs.GetInt(W("done" + i), 0) == 1;
        /// <summary>Orders finished and not yet paid: their stars go in, their lines come back for the end sheet.</summary>
        public static List<string> ClaimOrders()
        {
            var lines = new List<string>(); var orders = WeekOrders;
            for (int i = 0; i < orders.Length; i++)
            {
                var o = orders[i]; if (OrderClaimed(i) || Progress(o) < o.need) continue;
                PlayerPrefs.SetInt(W("done" + i), 1); AddStars(o.stars); lines.Add(string.Format(o.text, o.need) + " · +" + o.stars + " stars");
            }
            PlayerPrefs.Save(); return lines;
        }

        /// <summary>At start: the week's counts taken, and a season that has ended gives out what was reached and not taken.
        /// Returns a line to show when that happened, else null.</summary>
        public static string EnsureSeason()
        {
            EnsureWeek();
            int seen = PlayerPrefs.GetInt("bonds.seen", 0), now = Season; string line = null;
            if (seen == 0) { PlayerPrefs.SetInt("bonds.seen", now); PlayerPrefs.Save(); return null; }
            for (int s = seen; s < now; s++)
            {
                int n = 0;
                for (int t = 1; t <= TierOf(s); t++)
                {
                    if (!ClaimedOf(s, t, false)) { GiveReward(FreeReward(t)); PlayerPrefs.SetInt(K(s, "f" + t), 1); n++; }
                    if (GoldBondOf(s) && !ClaimedOf(s, t, true)) { GiveReward(GoldReward(t, s)); PlayerPrefs.SetInt(K(s, "g" + t), 1); n++; }
                }
                if (n > 0) line = "Season " + s + " is over · " + n + " rewards you had not taken are in the depot";
            }
            PlayerPrefs.SetInt("bonds.seen", now); PlayerPrefs.Save(); return line;
        }
    }
}

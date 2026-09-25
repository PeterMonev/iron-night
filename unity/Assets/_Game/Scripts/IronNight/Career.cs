using UnityEngine;

namespace IronNight
{
    /// <summary>
    /// Every tank's own service. The experience it earns in the nights it leads (a tenth of the night's score, a hundred
    /// more for a dawn) is spent on its gun, its engine and its armour, three steps each. It keeps its record - nights,
    /// kills, dawns, best night - and a white ring is painted round its barrel for every fifty kills. The steps belong to
    /// the tank, not the platoon: a new tank starts green and grows with the one who drives it.
    /// </summary>
    public static class Career
    {
        public class Module { public string id, name, picture; public string[] steps; }
        public static readonly Module[] Modules =
        {
            new Module { id = "gun", name = "Gun", picture = "card_gunner", steps = new[] { "+10% damage · loads 5% faster", "+20% damage · loads 10% faster", "+30% damage · loads 15% faster" } },
            new Module { id = "engine", name = "Engine", picture = "card_engines", steps = new[] { "+7% speed", "+14% speed", "+21% speed" } },
            new Module { id = "armour", name = "Armour", picture = "card_armour", steps = new[] { "+1 hit", "+2 hits", "+3 hits" } },
        };
        public static readonly int[] Costs = { 300, 900, 2000 };
        public const int KillsPerRing = 50, MaxRings = 12;
        static readonly int[] RankAt = { 0, 800, 2500, 6000, 12000 };
        static readonly string[] RankNames = { "New", "Proven", "Veteran", "Elite", "Legend" };

        /// <summary>Once, when the careers begin: every tank already owned gets 300 experience, enough for its first step.</summary>
        static Career()
        {
            if (PlayerPrefs.GetInt("career.v1", 0) != 0) return;
            foreach (var c in Depot.Leaders) if (Depot.OwnsLeader(c) && TotalXp(c.id) == 0) { PlayerPrefs.SetInt(K(c.id, "xp"), 300); PlayerPrefs.SetInt(K(c.id, "xpTotal"), 300); }
            PlayerPrefs.SetInt("career.v1", 1); PlayerPrefs.Save();
        }

        static string K(string tank, string what) => "tank." + tank + "." + what;
        public static int Xp(string tank) => PlayerPrefs.GetInt(K(tank, "xp"), 0);             // earned and not yet spent
        public static int TotalXp(string tank) => PlayerPrefs.GetInt(K(tank, "xpTotal"), 0);   // ever earned: the rank
        public static int Step(string tank, string module) => PlayerPrefs.GetInt(K(tank, module), 0);
        public static int Kills(string tank) => PlayerPrefs.GetInt(K(tank, "kills"), 0);
        public static int Nights(string tank) => PlayerPrefs.GetInt(K(tank, "nights"), 0);
        public static int Dawns(string tank) => PlayerPrefs.GetInt(K(tank, "dawns"), 0);
        public static int Best(string tank) => PlayerPrefs.GetInt(K(tank, "best"), 0);
        public static int Rings(string tank) => Mathf.Min(MaxRings, Kills(tank) / KillsPerRing);

        public static int Stars(string tank) { int x = TotalXp(tank), s = 0; for (int i = 1; i < RankAt.Length; i++) if (x >= RankAt[i]) s = i; return s; }
        public static string Rank(string tank) => RankNames[Stars(tank)];
        /// <summary>The experience the next rank needs, 0 at the top.</summary>
        public static int NextRank(string tank) { int s = Stars(tank); return s + 1 < RankAt.Length ? RankAt[s + 1] : 0; }
        public static int RankFloor(string tank) => RankAt[Stars(tank)];

        public static int NextCost(string tank, string module) { int s = Step(tank, module); return s >= Costs.Length ? 0 : Costs[s]; }
        public static bool CanUpgrade(string tank, string module) { int c = NextCost(tank, module); return c > 0 && Xp(tank) >= c; }
        public static bool AnyUpgrade(string tank) { foreach (var m in Modules) if (CanUpgrade(tank, m.id)) return true; return false; }
        public static bool Upgrade(string tank, string module)
        {
            if (!CanUpgrade(tank, module)) return false;
            PlayerPrefs.SetInt(K(tank, "xp"), Xp(tank) - NextCost(tank, module)); PlayerPrefs.SetInt(K(tank, module), Step(tank, module) + 1); PlayerPrefs.Save(); return true;
        }

        // what the steps do in the fight
        public static float DamageMul(string tank) => 1f + 0.1f * Step(tank, "gun");
        public static float ReloadMul(string tank) => 1f - 0.05f * Step(tank, "gun");
        public static float SpeedMul(string tank) => 1f + 0.07f * Step(tank, "engine");
        public static int ExtraHits(string tank) => Step(tank, "armour");

        /// <summary>A night's service into the tank's record. A night that ends twice (a revive) comes back with only what
        /// is new since the first time.</summary>
        public static void Add(string tank, int xp, int kills, bool night, bool dawn, int score)
        {
            if (string.IsNullOrEmpty(tank)) return;
            if (xp > 0) { PlayerPrefs.SetInt(K(tank, "xp"), Xp(tank) + xp); PlayerPrefs.SetInt(K(tank, "xpTotal"), TotalXp(tank) + xp); }
            if (kills > 0) PlayerPrefs.SetInt(K(tank, "kills"), Kills(tank) + kills);
            if (night) PlayerPrefs.SetInt(K(tank, "nights"), Nights(tank) + 1);
            if (dawn) PlayerPrefs.SetInt(K(tank, "dawns"), Dawns(tank) + 1);
            if (score > Best(tank)) PlayerPrefs.SetInt(K(tank, "best"), score);
            PlayerPrefs.Save();
        }
    }
}

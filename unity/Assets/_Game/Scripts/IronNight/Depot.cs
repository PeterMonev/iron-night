using System.Collections.Generic;
using UnityEngine;

namespace IronNight
{
    /// <summary>
    /// The meta game: points earned in each night assault buy permanent upgrades in the depot. Saved on the device with
    /// PlayerPrefs (single player, no server). Upgrades are five levels each; the cost grows with the level.
    /// </summary>
    public static class Depot
    {
        public class Upgrade { public string id, title, desc; public int level; public int MaxLevel = 5; public int Cost => 200 + level * 250; }

        public static readonly List<Upgrade> Upgrades = new List<Upgrade>
        {
            new Upgrade { id = "armor", title = "Applique armour", desc = "+1 hit the leader can take, per level." },
            new Upgrade { id = "loaders", title = "Veteran loaders", desc = "The platoon reloads 6% faster per level." },
            new Upgrade { id = "optics", title = "Night optics", desc = "Gunners engage 5% farther per level." },
            new Upgrade { id = "engines", title = "Tuned engines", desc = "The platoon drives 5% faster per level." },
            new Upgrade { id = "reserve", title = "Reserve crews", desc = "Every reinforcement arrives with one more hit it can take, per level.", MaxLevel = 2 },
        };

        public static int Points { get; private set; }
        public static int NightsFought { get; private set; }
        public static int BestKills { get; private set; }
        public static float BestTime { get; private set; }

        static bool loaded;

        public static void Load()
        {
            if (loaded) return; loaded = true;
            Points = PlayerPrefs.GetInt("depot.points", 0);
            NightsFought = PlayerPrefs.GetInt("depot.nights", 0);
            BestKills = PlayerPrefs.GetInt("depot.bestKills", 0);
            BestTime = PlayerPrefs.GetFloat("depot.bestTime", 0f);
            foreach (var u in Upgrades) u.level = PlayerPrefs.GetInt("depot." + u.id, 0);
        }

        public static void Save()
        {
            PlayerPrefs.SetInt("depot.points", Points); PlayerPrefs.SetInt("depot.nights", NightsFought);
            PlayerPrefs.SetInt("depot.bestKills", BestKills); PlayerPrefs.SetFloat("depot.bestTime", BestTime);
            foreach (var u in Upgrades) PlayerPrefs.SetInt("depot." + u.id, u.level);
            PlayerPrefs.Save();
        }

        public static int Level(string id) { Load(); foreach (var u in Upgrades) if (u.id == id) return u.level; return 0; }

        public static bool Buy(Upgrade u)
        {
            Load();
            if (u.level >= u.MaxLevel || Points < u.Cost) return false;
            Points -= u.Cost; u.level++; Save(); return true;
        }

        public static void AddPoints(int points) { Load(); Points += points; Save(); }

        /// <summary>Called once per night, at its end, with what the player did; the points go in through AddPoints.</summary>
        public static void RecordNight(int kills, float seconds)
        {
            Load(); NightsFought++;
            if (kills > BestKills) BestKills = kills; if (seconds > BestTime) BestTime = seconds;
            Save();
        }

        // camouflage: a tint over the platoon's paint; the first one is free, the others cost points (an IAP later)
        public class Camo { public string id, name; public Color tint; public int cost; }
        public static readonly Camo[] Camos =
        {
            new Camo { id = "olive", name = "Olive drab", tint = Color.white, cost = 0 },
            new Camo { id = "winter", name = "Winter", tint = new Color(1.7f, 1.7f, 1.95f), cost = 800 },
            new Camo { id = "desert", name = "Desert", tint = new Color(1.55f, 1.35f, 0.95f), cost = 800 },
            new Camo { id = "night", name = "Night", tint = new Color(0.62f, 0.68f, 0.85f), cost = 1500 },
        };
        public static string CamoId { get { Load(); return PlayerPrefs.GetString("depot.camo", "olive"); } }
        public static Color CamoTint { get { foreach (var c in Camos) if (c.id == CamoId) return c.tint; return Color.white; } }
        public static bool OwnsCamo(Camo c) => c.cost == 0 || PlayerPrefs.GetInt("depot.camo." + c.id, 0) == 1;
        public static bool PickCamo(Camo c)
        {
            Load();
            if (!OwnsCamo(c)) { if (Points < c.cost) return false; Points -= c.cost; PlayerPrefs.SetInt("depot.camo." + c.id, 1); }
            PlayerPrefs.SetString("depot.camo", c.id); Save(); return true;
        }

        // what the upgrades mean in the fight
        public static float LeaderHp => 8f + Level("armor");
        public static float ReloadMul => 1f - 0.06f * Level("loaders");
        public static float RangeMul => 1f + 0.05f * Level("optics");
        public static float SpeedMul => 1f + 0.05f * Level("engines");
        public static float WingmanHpBonus => Level("reserve");
    }
}

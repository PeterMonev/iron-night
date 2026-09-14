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
            new Upgrade { id = "reserve", title = "Reserve crews", desc = "Level 1: start the night with two wingmen. Level 2: wingmen take one more hit.", MaxLevel = 2 },
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

        /// <summary>Called once at the end of a night with what the player earned and did.</summary>
        public static void RecordNight(int score, int kills, float seconds)
        {
            Load();
            Points += score; NightsFought++;
            if (kills > BestKills) BestKills = kills; if (seconds > BestTime) BestTime = seconds;
            Save();
        }

        // what the upgrades mean in the fight
        public static float LeaderHp => 8f + Level("armor");
        public static float ReloadMul => 1f - 0.06f * Level("loaders");
        public static float RangeMul => 1f + 0.05f * Level("optics");
        public static float SpeedMul => 1f + 0.05f * Level("engines");
        public static int StartWingmen => Level("reserve") >= 1 ? 2 : 1;
        public static float WingmanHpBonus => Level("reserve") >= 2 ? 1f : 0f;
    }
}

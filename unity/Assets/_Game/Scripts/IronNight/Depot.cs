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
            // once: the test runs of the third batch left the nation and paint changed on this machine; back to olive Americans
            if (PlayerPrefs.GetInt("depot.version", 0) < 2) { PlayerPrefs.SetString("depot.nation", "us"); PlayerPrefs.SetString("depot.camo", "olive"); PlayerPrefs.SetInt("depot.version", 2); PlayerPrefs.Save(); }
            if (PlayerPrefs.GetInt("depot.version", 0) < 3) { foreach (var k in new[] { "camp.night", "camp.kills", "camp.score", "camp.won", "camp.best" }) PlayerPrefs.DeleteKey(k); PlayerPrefs.DeleteKey("camp.platoon"); PlayerPrefs.DeleteKey("camp.leaderHp"); PlayerPrefs.SetInt("depot.version", 3); PlayerPrefs.Save(); }   // the campaign test runs wiped
            if (PlayerPrefs.GetInt("depot.version", 0) < 4) { foreach (var k in new[] { "camp.night", "camp.kills", "camp.score", "camp.platoon", "camp.leaderHp", "depot.leader.us", "depot.leader.su", "depot.commander.us", "depot.commander.su" }) PlayerPrefs.DeleteKey(k); PlayerPrefs.SetInt("depot.version", 4); PlayerPrefs.Save(); }   // the garage test runs wiped: back to the Sherman, no commander
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

        // the daily supply drop: 300 points once a day, claimed on the title screen
        public const int DailyPoints = 300;
        public static bool DailyReady => PlayerPrefs.GetString("depot.daily", "") != System.DateTime.Now.ToString("yyyyMMdd");
        public static void ClaimDaily() { if (!DailyReady) return; PlayerPrefs.SetString("depot.daily", System.DateTime.Now.ToString("yyyyMMdd")); AddPoints(DailyPoints); }

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
            new Camo { id = "winter", name = "Winter", tint = new Color(1.3f, 1.32f, 1.42f), cost = 800 },
            new Camo { id = "desert", name = "Desert", tint = new Color(1.25f, 1.15f, 0.88f), cost = 800 },
            new Camo { id = "night", name = "Night", tint = new Color(0.62f, 0.68f, 0.85f), cost = 1500 },
        };
        public static string CamoId { get { Load(); return PlayerPrefs.GetString("depot.camo", "olive"); } }

        // the nation: which tree of tanks the platoon comes from, and which commanders
        public static string Nation { get { Load(); return PlayerPrefs.GetString("depot.nation", "us"); } set { PlayerPrefs.SetString("depot.nation", value); Save(); } }
        public static string WingmanId => Nation == "su" ? "t34_85" : "sherman";

        // the leader's tank: one of the nation's tree, bought once and kept
        public class LeaderChoice { public string id, name, desc, nation; public int cost; }
        public static readonly LeaderChoice[] Leaders =
        {
            new LeaderChoice { id = "sherman", name = "M4 Sherman", nation = "us", cost = 0, desc = "The workhorse: quick turret, quick loader, an ordinary gun." },
            new LeaderChoice { id = "chaffee", name = "M24 Chaffee", nation = "us", cost = 1200, desc = "Light and fast, a small gun, thin armour: run rings round them." },
            new LeaderChoice { id = "m10", name = "M10 Wolverine", nation = "us", cost = 1600, desc = "A tank destroyer: a good 3-inch gun on an open turret, thin armour." },
            new LeaderChoice { id = "hellcat", name = "M18 Hellcat", nation = "us", cost = 1800, desc = "The fastest thing on tracks, a 76 mm gun, no armour to speak of." },
            new LeaderChoice { id = "easy8", name = "M4A3E8 Easy Eight", nation = "us", cost = 2000, desc = "The Sherman grown up: 76 mm gun, wide tracks, a hit more." },
            new LeaderChoice { id = "firefly", name = "Sherman Firefly", nation = "us", cost = 2000, desc = "The 17-pounder hits twice as hard and reaches farther; the turret is slower." },
            new LeaderChoice { id = "pershing", name = "M26 Pershing", nation = "us", cost = 3500, desc = "A heavy: the 90 mm gun and thick armour, slow to load and to turn." },
            new LeaderChoice { id = "t34_85", name = "T-34-85", nation = "su", cost = 0, desc = "Fast, sloped, an 85 mm gun: the best all-rounder of the war." },
            new LeaderChoice { id = "kv85", name = "KV-1", nation = "su", cost = 2000, desc = "The heavy of 1941: slow, thick, and it takes a beating." },
            new LeaderChoice { id = "su100", name = "SU-100", nation = "su", cost = 2200, desc = "No turret: the 100 mm gun aims with the hull. Point the tank, kill anything." },
            new LeaderChoice { id = "is2", name = "IS-2", nation = "su", cost = 4000, desc = "The 122 mm gun: one shot, one wreck. Slow to load, slow to turn, hard to kill." },
        };
        public static string LeaderId { get { Load(); return PlayerPrefs.GetString("depot.leader." + Nation, Nation == "su" ? "t34_85" : "sherman"); } }
        public static bool OwnsLeader(LeaderChoice c) => c.cost == 0 || PlayerPrefs.GetInt("depot.leader." + c.id, 0) == 1;
        public static bool PickLeader(LeaderChoice c)
        {
            Load();
            if (!OwnsLeader(c)) { if (Points < c.cost) return false; Points -= c.cost; PlayerPrefs.SetInt("depot.leader." + c.id, 1); }
            PlayerPrefs.SetString("depot.leader." + c.nation, c.id); Save(); return true;
        }

        // the commanders: one bonus each, three to a nation, bought once and kept; the chosen one rides with the leader
        public class Commander { public string id, name, nation, bonus, desc, picture; public int cost; }
        public static readonly Commander[] Commanders =
        {
            new Commander { id = "kowalski", name = "Sgt. Kowalski", nation = "us", bonus = "reload", desc = "Iron nerves: the crew loads 15% faster.", picture = "cmd_us_sergeant", cost = 1500 },
            new Commander { id = "hale", name = "Lt. Hale", nation = "us", bonus = "radar", desc = "Map reader: the radar reaches a third farther.", picture = "cmd_us_lieutenant", cost = 1500 },
            new Commander { id = "rivers", name = "Cpl. Rivers, 761st", nation = "us", bonus = "heavy", desc = "Panther hunter: +50% damage against heavy tanks.", picture = "cmd_us_corporal", cost = 2000 },
            new Commander { id = "orlov", name = "Kpt. Orlov", nation = "su", bonus = "armour", desc = "Kursk armour: the leader takes one hit more.", picture = "cmd_su_captain", cost = 1500 },
            new Commander { id = "samusenko", name = "Lt. Samusenko", nation = "su", bonus = "repair", desc = "Field mechanic: repair kits mend one hit more.", picture = "cmd_su_woman", cost = 1500 },
            new Commander { id = "belov", name = "Serzh. Belov", nation = "su", bonus = "speed", desc = "Siberian engines: the platoon drives 12% faster.", picture = "cmd_su_siberian", cost = 2000 },
        };
        public static string CommanderId { get { Load(); return PlayerPrefs.GetString("depot.commander." + Nation, ""); } }
        public static string CommanderBonus { get { var c = System.Array.Find(Commanders, x => x.id == CommanderId && x.nation == Nation); return c != null ? c.bonus : ""; } }
        public static bool OwnsCommander(Commander c) => PlayerPrefs.GetInt("depot.commander." + c.id, 0) == 1;
        public static bool PickCommander(Commander c)
        {
            Load();
            if (!OwnsCommander(c)) { if (Points < c.cost) return false; Points -= c.cost; PlayerPrefs.SetInt("depot.commander." + c.id, 1); }
            PlayerPrefs.SetString("depot.commander." + c.nation, CommanderId == c.id ? "" : c.id); Save(); return true;   // picking the chosen one again rides without a commander
        }

        // the campaign in progress: which night is next (1..3, 0 = none), what the platoon carried over, the running totals
        public static int CampaignNight { get => PlayerPrefs.GetInt("camp.night", 0); set { PlayerPrefs.SetInt("camp.night", value); PlayerPrefs.Save(); } }
        public static int CampaignKills { get => PlayerPrefs.GetInt("camp.kills", 0); set => PlayerPrefs.SetInt("camp.kills", value); }
        public static int CampaignScore { get => PlayerPrefs.GetInt("camp.score", 0); set => PlayerPrefs.SetInt("camp.score", value); }
        public static float CampaignLeaderHp { get => PlayerPrefs.GetFloat("camp.leaderHp", -1f); set => PlayerPrefs.SetFloat("camp.leaderHp", value); }
        public static string CampaignPlatoon { get => PlayerPrefs.GetString("camp.platoon", ""); set => PlayerPrefs.SetString("camp.platoon", value); }   // "id:hp,id:hp" of the wingmen that lived
        public static int CampaignsWon => PlayerPrefs.GetInt("camp.won", 0);
        public static int CampaignBest => PlayerPrefs.GetInt("camp.best", 0);
        public static void CampaignStart() { CampaignNight = 1; CampaignKills = 0; CampaignScore = 0; CampaignLeaderHp = -1f; CampaignPlatoon = ""; PlayerPrefs.Save(); }
        public static void CampaignClear() { CampaignNight = 0; CampaignPlatoon = ""; CampaignLeaderHp = -1f; PlayerPrefs.Save(); }
        public static void CampaignWon(int score) { PlayerPrefs.SetInt("camp.won", CampaignsWon + 1); if (score > CampaignBest) PlayerPrefs.SetInt("camp.best", score); CampaignClear(); }

        /// <summary>Veteran nights: the enemy takes half again as many hits, the night pays half again as much.</summary>
        public static bool Veteran { get => PlayerPrefs.GetInt("depot.veteran", 0) == 1; set { PlayerPrefs.SetInt("depot.veteran", value ? 1 : 0); PlayerPrefs.Save(); } }

        // running tallies across every night (kills, tigers, guns, infantry, objectives, aces, dawns...) for the medals
        public static int Total(string key) => PlayerPrefs.GetInt("tally." + key, 0);
        public static void Tally(string key, int add) { if (add <= 0) return; PlayerPrefs.SetInt("tally." + key, Total(key) + add); }

        /// <summary>The last ten nights, newest first: date, sector, kills, time, score, dawn.</summary>
        public static void LogNight(string sector, int kills, float seconds, int score, bool dawn)
        {
            var lines = new List<string> { System.DateTime.Now.ToString("dd MMM") + "|" + sector + "|" + kills + "|" + Mathf.FloorToInt(seconds / 60f) + ":" + (Mathf.FloorToInt(seconds % 60f)).ToString("00") + "|" + score + "|" + (dawn ? "1" : "0") };
            for (int i = 0; i < 9; i++) { var s = PlayerPrefs.GetString("log." + i, ""); if (s != "") lines.Add(s); }
            for (int i = 0; i < lines.Count && i < 10; i++) PlayerPrefs.SetString("log." + i, lines[i]);
            PlayerPrefs.Save();
        }
        public static List<string[]> NightLog() { var list = new List<string[]>(); for (int i = 0; i < 10; i++) { var s = PlayerPrefs.GetString("log." + i, ""); if (s != "") list.Add(s.Split('|')); } return list; }

        /// <summary>A rank for the title screen, by nights fought.</summary>
        public static int RankIndex { get { Load(); int n = NightsFought; return n < 1 ? 0 : n < 5 ? 0 : n < 10 ? 1 : n < 20 ? 2 : n < 40 ? 3 : n < 80 ? 4 : 5; } }   // the cell of the insignia sheet: private .. captain
        public static string Rank { get { Load(); int n = NightsFought; return n < 1 ? "Recruit" : n < 5 ? "Trooper" : n < 10 ? "Corporal" : n < 20 ? "Sergeant" : n < 40 ? "Lieutenant" : n < 80 ? "Captain" : "Major"; } }
        public static Color CamoTint { get { foreach (var c in Camos) if (c.id == CamoId) return c.tint; return Color.white; } }
        public static bool OwnsCamo(Camo c) => c.cost == 0 || PlayerPrefs.GetInt("depot.camo." + c.id, 0) == 1;
        public static bool PickCamo(Camo c)
        {
            Load();
            if (!OwnsCamo(c)) { if (Points < c.cost) return false; Points -= c.cost; PlayerPrefs.SetInt("depot.camo." + c.id, 1); }
            PlayerPrefs.SetString("depot.camo", c.id); Save(); return true;
        }

        // what the upgrades mean in the fight
        public static float LeaderHp => 8f + Level("armor") + (CrewLevel >= 2 ? 1f : 0f);
        // the leader's crew: nights survived together, per nation; lost with the leader unless the field repair pulls them out
        public static int CrewNights { get { Load(); return PlayerPrefs.GetInt("crew.nights." + Nation, 0); } set { PlayerPrefs.SetInt("crew.nights." + Nation, Mathf.Max(0, value)); Save(); } }
        public static int CrewLevel => CrewNights >= 10 ? 3 : CrewNights >= 5 ? 2 : CrewNights >= 2 ? 1 : 0;
        public static string CrewName => CrewLevel == 3 ? "Old hands" : CrewLevel == 2 ? "Veteran crew" : CrewLevel == 1 ? "Blooded crew" : "Green crew";
        public static string CrewBonusText => CrewLevel == 3 ? "reloads 10% faster, one hit more, sees 5% farther" : CrewLevel == 2 ? "reloads 5% faster, one hit more" : CrewLevel == 1 ? "reloads 5% faster" : "no bonus yet · survive two nights";
        public static float ReloadMul => (1f - 0.06f * Level("loaders")) * (CrewLevel >= 3 ? 0.9f : CrewLevel >= 1 ? 0.95f : 1f);
        public static float RangeMul => 1f + 0.05f * Level("optics");
        public static float SpeedMul => 1f + 0.05f * Level("engines");
        public static float WingmanHpBonus => Level("reserve");
    }
}

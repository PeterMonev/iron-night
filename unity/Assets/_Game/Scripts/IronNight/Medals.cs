using System.Collections.Generic;
using UnityEngine;

namespace IronNight
{
    /// <summary>
    /// Twelve medals for the things worth remembering: the first night, the first dawn, a hundred vehicles, ten Tigers,
    /// the Tiger Ace three times. They are judged from the running tallies the depot keeps, each is awarded once, and
    /// every one is worth 250 depot points.
    /// </summary>
    public static class Medals
    {
        public class Medal { public string id, name, desc; public System.Func<bool> test; }
        public const int Reward = 250;

        public static readonly Medal[] All =
        {
            new Medal { id = "first", name = "Recruit's Star", desc = "Fight your first night.", test = () => Depot.NightsFought >= 1 },
            new Medal { id = "dawn", name = "Night Fighter", desc = "Hold until dawn.", test = () => Depot.Total("dawns") >= 1 },
            new Medal { id = "ten", name = "Bocage Veteran", desc = "Fight ten nights.", test = () => Depot.NightsFought >= 10 },
            new Medal { id = "sharp", name = "Sharpshooter", desc = "A night with 15 kills and half the shots on target.", test = () => Depot.Total("sharp") >= 1 },
            new Medal { id = "hundred", name = "Tank Ace", desc = "Destroy a hundred enemy vehicles.", test = () => Depot.Total("kills") >= 100 },
            new Medal { id = "tigers", name = "Tiger Slayer", desc = "Destroy ten Tigers.", test = () => Depot.Total("tigers") >= 10 },
            new Medal { id = "guns", name = "Gun Buster", desc = "Knock out twenty anti-tank guns.", test = () => Depot.Total("guns") >= 20 },
            new Medal { id = "broom", name = "Trench Broom", desc = "Cut down two hundred tank hunters.", test = () => Depot.Total("infantry") >= 200 },
            new Medal { id = "path", name = "Pathfinder", desc = "Reach twenty-five objectives.", test = () => Depot.Total("objectives") >= 25 },
            new Medal { id = "ace", name = "Ace of Aces", desc = "Destroy the Tiger Ace three times.", test = () => Depot.Total("aces") >= 3 },
            new Medal { id = "veteran", name = "Old Guard", desc = "See the dawn on a veteran night.", test = () => Depot.Total("veteranDawns") >= 1 },
            new Medal { id = "fifty", name = "Iron Night", desc = "Fight fifty nights.", test = () => Depot.NightsFought >= 50 },
            new Medal { id = "campaigner", name = "Campaigner", desc = "Win a three-night campaign.", test = () => Depot.CampaignsWon >= 1 },
            new Medal { id = "wrecker", name = "Track Wrecker", desc = "Throw twenty enemy tracks.", test = () => Depot.Total("tracked") >= 20 },
            new Medal { id = "daybreak", name = "Daybreak", desc = "Hold two minutes into daylight.", test = () => Depot.Total("daybreak") >= 1 },
        };

        public static bool Earned(Medal m) => PlayerPrefs.GetInt("medal." + m.id, 0) == 1;
        public static int Count { get { int n = 0; foreach (var m in All) if (Earned(m)) n++; return n; } }

        /// <summary>Awards whatever is newly deserved; returns those medals, their points already in the depot.</summary>
        public static List<Medal> Check()
        {
            var fresh = new List<Medal>();
            foreach (var m in All) if (!Earned(m) && m.test()) { PlayerPrefs.SetInt("medal." + m.id, 1); Depot.AddPoints(Reward); fresh.Add(m); }
            if (fresh.Count > 0) PlayerPrefs.Save();
            return fresh;
        }
    }
}

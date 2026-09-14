using System.Collections.Generic;
using UnityEngine;

namespace IronNight
{
    /// <summary>
    /// Standing orders: three missions at a time, shown on the title screen, each worth depot points when done. Some
    /// count across nights (destroy 30 vehicles), some must happen in one night (hold until 3:30). Progress and the
    /// finished ones live in PlayerPrefs; a finished mission is replaced by the next one from the list.
    /// </summary>
    public static class Missions
    {
        public class Mission { public int index; public string stat, text; public int goal, reward, progress, start; public bool perNight; public string Title => string.Format(text, goal); public string Key => stat + goal; }
        public class Night { public int kills, tigers, paks, flares, level, objectives; public float time; public bool boss; }

        static readonly Mission[] Pool =
        {
            new Mission { stat = "kills", goal = 10, reward = 200, text = "Destroy {0} enemy vehicles" },
            new Mission { stat = "time", goal = 120, reward = 300, text = "Hold until 2:00 in one night", perNight = true },
            new Mission { stat = "flares", goal = 6, reward = 200, text = "Pick up {0} flares" },
            new Mission { stat = "tigers", goal = 1, reward = 250, text = "Destroy a Tiger" },
            new Mission { stat = "objectives", goal = 3, reward = 300, text = "Reach {0} objectives" },
            new Mission { stat = "kills", goal = 30, reward = 500, text = "Destroy {0} enemy vehicles" },
            new Mission { stat = "paks", goal = 3, reward = 250, text = "Knock out {0} anti-tank guns" },
            new Mission { stat = "level", goal = 5, reward = 300, text = "Reach level {0} in one night", perNight = true },
            new Mission { stat = "nightkills", goal = 15, reward = 300, text = "Destroy {0} enemies in one night", perNight = true },
            new Mission { stat = "time", goal = 210, reward = 600, text = "Hold until 3:30 in one night", perNight = true },
            new Mission { stat = "tigers", goal = 4, reward = 600, text = "Destroy {0} Tigers" },
            new Mission { stat = "flares", goal = 20, reward = 600, text = "Pick up {0} flares" },
            new Mission { stat = "kills", goal = 80, reward = 1200, text = "Destroy {0} enemy vehicles" },
            new Mission { stat = "boss", goal = 1, reward = 1500, text = "Destroy the Tiger Ace", perNight = true },
            new Mission { stat = "objectives", goal = 12, reward = 900, text = "Reach {0} objectives" },
            new Mission { stat = "paks", goal = 10, reward = 700, text = "Knock out {0} anti-tank guns" },
            new Mission { stat = "level", goal = 8, reward = 700, text = "Reach level {0} in one night", perNight = true },
            new Mission { stat = "nightkills", goal = 35, reward = 800, text = "Destroy {0} enemies in one night", perNight = true },
            new Mission { stat = "time", goal = 300, reward = 1200, text = "See the dawn", perNight = true },
        };

        public static readonly List<Mission> Active = new List<Mission>();
        static bool loaded;

        public static void Load()
        {
            if (loaded) return; loaded = true;
            for (int i = 0; i < Pool.Length; i++) Pool[i].index = i;
            var keys = PlayerPrefs.GetString("missions.active", "");
            foreach (var key in keys.Split(',')) foreach (var m in Pool) if (m.Key == key && !Active.Contains(m)) { m.progress = m.start = PlayerPrefs.GetInt("missions.p." + key, 0); Active.Add(m); }
            Fill(); Save();
        }

        static bool Done(Mission m) => PlayerPrefs.GetInt("missions.done." + m.Key, 0) == 1;

        /// <summary>Keeps three missions open: the first unfinished ones from the list, one per kind of stat.</summary>
        static void Fill()
        {
            foreach (var m in Pool)
            {
                if (Active.Count >= 3) break;
                if (Done(m) || Active.Contains(m) || Active.Exists(a => a.stat == m.stat)) continue;
                m.progress = m.start = 0; Active.Add(m);
            }
        }

        static void Save()
        {
            var keys = new List<string>(); foreach (var m in Active) { keys.Add(m.Key); PlayerPrefs.SetInt("missions.p." + m.Key, m.progress); }
            PlayerPrefs.SetString("missions.active", string.Join(",", keys)); PlayerPrefs.Save();
        }

        static int Stat(Night n, string stat)
        {
            switch (stat)
            {
                case "kills": case "nightkills": return n.kills; case "tigers": return n.tigers; case "paks": return n.paks; case "flares": return n.flares;
                case "level": return n.level; case "time": return Mathf.FloorToInt(n.time); case "boss": return n.boss ? 1 : 0; case "objectives": return n.objectives; default: return 0;
            }
        }

        /// <summary>Called at the end of a night (again after a rewarded revive: the totals are the whole night's, so it
        /// is safe to call twice). Returns the missions finished tonight; their points are already in the depot.</summary>
        public static List<Mission> Report(Night night)
        {
            Load(); var done = new List<Mission>();
            for (int i = Active.Count - 1; i >= 0; i--)
            {
                var m = Active[i]; int s = Stat(night, m.stat);
                m.progress = m.perNight ? Mathf.Max(m.progress, s) : m.start + s;
                if (m.progress >= m.goal)
                {
                    PlayerPrefs.SetInt("missions.done." + m.Key, 1); Depot.AddPoints(m.reward); done.Add(m); Active.RemoveAt(i);
                }
            }
            Fill(); Save(); return done;
        }

        /// <summary>"12/30" for the running ones, "tonight" for the one-night ones.</summary>
        public static string Progress(Mission m) => m.perNight ? "tonight" : Mathf.Min(m.progress, m.goal) + "/" + m.goal;
    }
}

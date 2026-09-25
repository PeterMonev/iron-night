using UnityEngine;

namespace IronNight
{
    /// <summary>
    /// The daily challenge: one night a day, the same for every commander in the world. The front, the way in, the
    /// weather, the rule of the night and the first throw of the dice all come from the date (UTC). Each day keeps its
    /// best score; the first run of a day pays for the days in a row it continues (500, then 100 more a day, to 1200).
    /// </summary>
    public static class Daily
    {
        public class Rule { public string id, name, line; }
        public static readonly Rule[] Rules =
        {
            new Rule { id = "tigers", name = "Tiger Night", line = "Every other tank is a Tiger or a Panther, and they come from the start." },
            new Rule { id = "short", name = "Short Rations", line = "The racks are half empty and the wrecks leave less behind." },
            new Rule { id = "stukas", name = "Stuka Weather", line = "A clear sky, and the dive bombers come every minute from 1:00." },
            new Rule { id = "alone", name = "Lone Tank", line = "No wingmen come up tonight. The leader fights alone." },
            new Rule { id = "hunters", name = "Tank Hunters", line = "Twice the infantry, with a Panzerfaust behind every hedge." },
            new Rule { id = "aces", name = "Ace Hunt", line = "A named ace takes the field every minute from 1:00." },
            new Rule { id = "noair", name = "Grounded", line = "No flying weather for our side: no air strikes tonight." },
            new Rule { id = "veteran", name = "Veterans", line = "The enemy's best crews: their tanks take half again the hits. Points ×1.5." },
        };

        static readonly System.Globalization.CultureInfo En = System.Globalization.CultureInfo.InvariantCulture;
        /// <summary>Today, in UTC: the whole world changes challenge at the same moment.</summary>
        public static string Today => System.DateTime.UtcNow.ToString("yyyyMMdd", En);
        public static string DaysAgo(int n) => System.DateTime.UtcNow.Date.AddDays(-n).ToString("yyyyMMdd", En);
        static System.DateTime Date(string day) => System.DateTime.ParseExact(day, "yyyyMMdd", En);
        public static System.TimeSpan Left => System.DateTime.UtcNow.Date.AddDays(1) - System.DateTime.UtcNow;
        public static string Heading(string day) => Date(day).ToString("dddd d MMMM", En).ToUpperInvariant();

        /// <summary>The day's number: the same on every phone, whatever it runs.</summary>
        public static int Seed(string day) { unchecked { int h = 17; foreach (var c in "iron-night-daily-" + day) h = h * 31 + c; return h & 0x7fffffff; } }
        static System.Random Dice(string day, int salt) => new System.Random(Seed(day) ^ (salt * 7919));

        public static Rule RuleOf(string day) => Rules[Dice(day, 1).Next(Rules.Length)];
        public static string Theatre(string day) { int r = Dice(day, 2).Next(10); return r < 4 ? "normandy" : r < 7 ? "kursk" : "ardennes"; }
        public static string Route(string day) { int r = Dice(day, 3).Next(3); return r == 0 ? "village" : r == 1 ? "open" : "bocage"; }
        public static string Weather(string day)
        {
            if (RuleOf(day).id == "stukas") return "clear";   // nobody flies in fog or rain
            int r = Dice(day, 4).Next(10); return r < 4 ? "clear" : r < 6 ? "overcast" : r < 8 ? "fog" : "rain";
        }

        /// <summary>The front and the way in, as the briefing puts them.</summary>
        public static string Place(string day)
        {
            string t = Theatre(day), ro = Route(day); bool kursk = t == "kursk";
            string front = kursk ? "Kursk" : t == "ardennes" ? "The Ardennes" : "Normandy";
            string way = ro == "village" ? "through the village" : ro == "bocage" ? (kursk ? "along the tree belts" : "through the bocage") : (kursk ? "across the steppe" : "across the open fields");
            string w = Weather(day), sky = w == "fog" ? "fog" : w == "rain" ? (t == "ardennes" ? "snow" : "rain") : w == "overcast" ? "overcast" : t == "ardennes" ? "clear and freezing" : "clear, a moon";
            return front + " · " + way + " · " + sky;
        }

        /// <summary>A picture of the day's country: the way in on the steppe or in Normandy, Bastogne for the Ardennes.</summary>
        public static string Picture(string day)
        {
            string t = Theatre(day), ro = Route(day);
            if (t == "kursk") return ro == "village" ? "route_kursk_village" : ro == "bocage" ? "route_kursk_belts" : "route_kursk_steppe";
            if (t == "ardennes") return "op_bastogne_" + (1 + Dice(day, 5).Next(5));
            return "route_" + ro;
        }

        // ---- the records: each day's best, and the days in a row
        static string Key(string day) => "daily." + day;
        public static int Best(string day) => PlayerPrefs.GetInt(Key(day), -1);   // -1: not played that day
        public static bool Played(string day) => Best(day) >= 0;
        /// <summary>Days in a row, counting only while the run is alive: the last day played was today or yesterday.</summary>
        public static int Streak
        {
            get
            {
                var last = PlayerPrefs.GetString("daily.last", ""); if (last.Length != 8) return 0;
                return (Date(Today) - Date(last)).Days <= 1 ? PlayerPrefs.GetInt("daily.streak", 0) : 0;
            }
        }
        public static int BestStreak => PlayerPrefs.GetInt("daily.bestStreak", 0);
        /// <summary>What the first run of a day pays on this many days in a row.</summary>
        public static int Reward(int streak) => 500 + 100 * (Mathf.Clamp(streak, 1, 8) - 1);
        /// <summary>What the first run today would pay: the run continued, or a new one started.</summary>
        public static int NextReward => Reward(Played(Today) ? Streak : Streak + 1);

        /// <summary>A run of a day's challenge: its best is kept, and the first run of the day advances the days in a
        /// row and is paid for them. Returns what was paid.</summary>
        public static int Record(string day, int score)
        {
            bool first = !Played(day);
            if (score > Best(day)) PlayerPrefs.SetInt(Key(day), score);
            int pay = 0;
            if (first)
            {
                var last = PlayerPrefs.GetString("daily.last", ""); int streak = 1;
                if (last.Length == 8 && (Date(day) - Date(last)).Days == 1) streak = PlayerPrefs.GetInt("daily.streak", 0) + 1;
                else if (last == day) streak = Mathf.Max(1, PlayerPrefs.GetInt("daily.streak", 1));
                if (last.Length != 8 || Date(day) >= Date(last)) { PlayerPrefs.SetString("daily.last", day); PlayerPrefs.SetInt("daily.streak", streak); }
                if (streak > BestStreak) PlayerPrefs.SetInt("daily.bestStreak", streak);
                pay = Reward(streak); Depot.AddPoints(pay); Depot.Tally("dailies", 1);
            }
            PlayerPrefs.Save(); return pay;
        }
    }
}

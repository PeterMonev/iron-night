using System.Collections.Generic;
using UnityEngine;

namespace IronNight
{
    /// <summary>
    /// The weekly event: a special night for the week, the same for every commander, with no server. Eight events turn
    /// in order, one a week from Monday 00:00 UTC, and come round again after eight weeks. Each brings a rule of the
    /// night, a front and a goal in three steps, counted over the week's event nights and paid as each step is reached;
    /// the count starts again every Monday.
    /// </summary>
    public static class Weekly
    {
        public class Step { public int at, amount; public string kind, id; }
        public class Event { public string id, name, line, rule, theatre, route, stat, goal, picture; public Step[] steps; }

        static Step S(int at, string kind, int amount, string id = null) => new Step { at = at, kind = kind, amount = amount, id = id };
        public static readonly Event[] Events =
        {
            new Event { id = "tigers", name = "Tiger Week", rule = "tigers", theatre = "normandy", route = "open", picture = "op_cobra_4", stat = "tigers", goal = "Tigers and Panthers knocked out",
                line = "Every other tank is a Tiger or a Panther, and they come from the start.",
                steps = new[] { S(8, "points", 1000), S(20, "gold", 40), S(40, "camo", 0, "hedgerow") } },
            new Event { id = "stukas", name = "Stuka Summer", rule = "stukas", theatre = "kursk", route = "open", picture = "op_prokhorovka_1", stat = "dawns", goal = "Dawns held on the steppe",
                line = "A clear sky over Kursk, and the dive bombers come every minute from 1:00.",
                steps = new[] { S(1, "supply", 1), S(2, "gold", 50), S(4, "camo", 0, "steppe") } },
            new Event { id = "hunters", name = "Panzerfaust Alley", rule = "hunters", theatre = "normandy", route = "village", picture = "op_cobra_3", stat = "infantry", goal = "Tank hunters cut down",
                line = "Twice the infantry in the village, with a Panzerfaust behind every wall.",
                steps = new[] { S(60, "xp", 500), S(150, "supply", 1), S(300, "gold", 60) } },
            new Event { id = "aces", name = "Ace Hunt", rule = "aces", theatre = "normandy", route = "bocage", picture = "op_cobra_2", stat = "aces", goal = "Named aces destroyed",
                line = "A named ace takes the field every minute from 1:00, somewhere in the hedgerows.",
                steps = new[] { S(2, "points", 1500), S(5, "gold", 50), S(10, "officer", 1) } },
            new Event { id = "winter", name = "Winter Offensive", rule = "", theatre = "ardennes", route = "open", picture = "campaign_ardennes", stat = "dawns", goal = "Dawns held in the snow",
                line = "The Ardennes in December: snow, frozen fields and the enemy's last great push.",
                steps = new[] { S(1, "xp", 600), S(2, "supply", 1), S(4, "camo", 0, "whitewash") } },
            new Event { id = "rations", name = "Short Rations", rule = "short", theatre = "kursk", route = "village", picture = "route_kursk_village", stat = "kills", goal = "Enemies destroyed",
                line = "The racks are half empty and the wrecks leave less behind. Make every shell count.",
                steps = new[] { S(60, "points", 1200), S(150, "gold", 40), S(300, "officer", 1) } },
            new Event { id = "alone", name = "Lone Wolf", rule = "alone", theatre = "normandy", route = "bocage", picture = "route_bocage", stat = "dawns", goal = "Dawns held alone",
                line = "No wingmen come up this week. The leader fights every night alone.",
                steps = new[] { S(1, "gold", 40), S(2, "officer", 1), S(4, "gold", 80) } },
            new Event { id = "veteran", name = "Veterans' Week", rule = "veteran", theatre = "ardennes", route = "open", picture = "op_bastogne_5", stat = "score", goal = "Points scored",
                line = "The enemy's best crews: their tanks take half again the hits, and every point counts half again.",
                steps = new[] { S(8000, "xp", 800), S(20000, "gold", 60), S(40000, "officer", 1) } },
        };

        static readonly System.Globalization.CultureInfo En = System.Globalization.CultureInfo.InvariantCulture;
        static readonly System.DateTime Epoch = new System.DateTime(2026, 9, 28, 0, 0, 0, System.DateTimeKind.Utc);   // a Monday: week 0
        /// <summary>The week's number since the first, in UTC: the whole world turns to the next event at the same moment.</summary>
        public static int Week => (int)System.Math.Floor((GameClock.UtcNow - Epoch).TotalDays / 7.0);
        static int Mod(int a, int n) => ((a % n) + n) % n;
        public static Event Now => Events[Mod(Week, Events.Length)];
        public static Event Next => Events[Mod(Week + 1, Events.Length)];
        public static System.TimeSpan Left => Epoch.AddDays(7 * (Week + 1)) - GameClock.UtcNow;
        /// <summary>The time left, as the tile and the sheet say it: "6 days left", "1 day 5 h left", "5 h left".</summary>
        public static string LeftText { get { var l = Left; return l.TotalDays >= 2 ? (int)l.TotalDays + " days left" : l.TotalDays >= 1 ? "1 day " + l.Hours + " h left" : l.TotalHours >= 1 ? (int)l.TotalHours + " h left" : Mathf.Max(1, (int)l.TotalMinutes) + " min left"; } }

        static string K(string what) => "weekly." + Week + "." + what;
        /// <summary>This week's count toward the event's goal.</summary>
        public static int Count => PlayerPrefs.GetInt(K("count"), 0);
        /// <summary>How many of the three steps are reached and paid this week.</summary>
        public static int StepsDone => PlayerPrefs.GetInt(K("steps"), 0);
        public static bool AllDone => StepsDone >= Now.steps.Length;

        /// <summary>A night's count toward the event: its own number of the thing the goal counts.</summary>
        public static int NightCount(Event ev, int kills, int tigers, int infantry, int aces, bool dawn, int score)
            => ev.stat == "tigers" ? tigers : ev.stat == "infantry" ? infantry : ev.stat == "aces" ? aces : ev.stat == "dawns" ? (dawn ? 1 : 0) : ev.stat == "score" ? score : kills;

        /// <summary>Adds to the week's count and pays each step it reaches; a line for each step paid.</summary>
        public static List<string> Add(int n)
        {
            var paid = new List<string>(); if (n <= 0) return paid;
            var ev = Now; int count = Count + n, done = StepsDone; PlayerPrefs.SetInt(K("count"), count);
            while (done < ev.steps.Length && count >= ev.steps[done].at) { paid.Add(Pay(ev.steps[done])); done++; }
            PlayerPrefs.SetInt(K("steps"), done); PlayerPrefs.Save(); return paid;
        }

        /// <summary>A step's reward into the depot. A camouflage already owned turns into sixty gold.</summary>
        static string Pay(Step s)
        {
            if (s.kind == "camo")
            {
                var c = System.Array.Find(Depot.Camos, x => x.id == s.id);
                if (c != null && !Depot.OwnsCamo(c)) { PlayerPrefs.SetInt("depot.camo." + c.id, 1); return c.name + " camouflage"; }
                Depot.AddGold(60); return (c != null ? c.name + " camouflage already yours · " : "") + "+60 gold";
            }
            Rewards.GiveLoot(new Rewards.Loot { kind = s.kind, amount = s.amount });
            return Describe(s);
        }

        /// <summary>A step's reward in words.</summary>
        public static string Describe(Step s)
        {
            switch (s.kind)
            {
                case "points": return "+" + s.amount.ToString("N0", En) + " points";
                case "gold": return "+" + s.amount + " gold";
                case "xp": return "+" + s.amount.ToString("N0", En) + " crew XP";
                case "supply": return "A supply crate";
                case "officer": return "An officer's crate";
                case "camo": { var c = System.Array.Find(Depot.Camos, x => x.id == s.id); return (c != null ? c.name : "A") + " camouflage"; }
            }
            return "";
        }

        /// <summary>The front and the way in, as the briefing puts them.</summary>
        public static string Place(Event ev)
        {
            bool kursk = ev.theatre == "kursk";
            string front = kursk ? "Kursk" : ev.theatre == "ardennes" ? "The Ardennes" : "Normandy";
            string way = ev.route == "village" ? "through the village" : ev.route == "bocage" ? (kursk ? "along the tree belts" : "through the bocage") : (kursk ? "across the steppe" : "across the open fields");
            return front + " · " + way;
        }
    }
}

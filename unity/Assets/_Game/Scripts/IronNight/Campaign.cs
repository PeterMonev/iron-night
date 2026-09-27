using System.Collections.Generic;
using UnityEngine;

namespace IronNight
{
    /// <summary>
    /// The road to Berlin: the war map. Two roads of sectors meet in Berlin: the western from the Normandy beaches
    /// through Paris, the Ardennes and the Rhine to the Elbe (American tanks), the eastern from Kursk through Kiev and
    /// Warsaw to the Seelow Heights (Soviet tanks); Berlin wants both. A sector is taken by holding its night until
    /// dawn, and pays for it; the later ones are harder, the last ones veteran nights. The enemy strikes back: now and
    /// then a counterattack falls on a sector at the front, and if it is not beaten off within a day, the sector is lost.
    /// Progress is kept for good, across seasons: the long goal of the game.
    /// </summary>
    public static class Campaign
    {
        public class Sector
        {
            public string id, name, road, when, theatre, route, weather, crate, country;
            public float x, y;            // on the painted map (Resources/UI/map_europe, 1536 x 1024), in pixels from its top left
            public int depth;             // how far along its road
            public bool veteran;          // the last sectors: the enemy takes half again as many hits
            public string[] after;        // what must be taken first
        }

        static Sector S(string id, string name, string road, int depth, string when, string theatre, string route, string weather, float x, float y, string after, string crate = null, bool veteran = false)
            => new Sector { id = id, name = name, road = road, depth = depth, when = when, theatre = theatre, route = route, weather = weather, x = x, y = y, after = after == null ? new string[0] : after.Split(','), crate = crate, veteran = veteran, country = theatre == "kursk" ? (x < 720f ? "GERMANY" : x < 1000f ? "POLAND" : "SOVIET UNION") : x < 320f ? "FRANCE" : x < 360f ? "BELGIUM" : "GERMANY" };   // for the opening titles

        public static readonly Sector[] All =
        {
            // the western road: American tanks
            S("omaha", "Omaha Beach", "west", 0, "JUNE 1944", "normandy", "open", "overcast", 165f, 462f, null),
            S("saintlo", "Saint-Lô", "west", 1, "JULY 1944", "normandy", "bocage", "fog", 150f, 492f, "omaha"),
            S("falaise", "Falaise", "west", 2, "AUGUST 1944", "normandy", "bocage", "clear", 195f, 502f, "saintlo"),
            S("paris", "Paris", "west", 3, "AUGUST 1944", "normandy", "village", "clear", 232f, 530f, "falaise", "supply"),
            S("reims", "Reims", "west", 4, "AUGUST 1944", "normandy", "open", "rain", 300f, 495f, "paris"),
            S("bastogne", "Bastogne", "west", 5, "DECEMBER 1944", "ardennes", "village", "fog", 345f, 468f, "reims"),
            S("aachen", "Aachen", "west", 6, "OCTOBER 1944", "normandy", "village", "rain", 382f, 415f, "bastogne"),
            S("hurtgen", "Hürtgen Forest", "west", 7, "NOVEMBER 1944", "ardennes", "bocage", "fog", 408f, 446f, "aachen"),
            S("remagen", "Remagen", "west", 8, "MARCH 1945", "normandy", "open", "overcast", 438f, 475f, "hurtgen", "supply"),
            S("ruhr", "The Ruhr", "west", 9, "APRIL 1945", "normandy", "village", "rain", 420f, 385f, "remagen", null, true),
            S("magdeburg", "Magdeburg", "west", 10, "APRIL 1945", "normandy", "open", "clear", 548f, 405f, "ruhr", null, true),
            // the eastern road: Soviet tanks
            S("kursk", "Kursk", "east", 0, "JULY 1943", "kursk", "open", "clear", 1470f, 310f, null),
            S("prokhorovka", "Prokhorovka", "east", 1, "JULY 1943", "kursk", "open", "overcast", 1478f, 380f, "kursk"),
            S("kharkov", "Kharkov", "east", 2, "AUGUST 1943", "kursk", "village", "clear", 1462f, 462f, "prokhorovka"),
            S("kiev", "Kiev", "east", 3, "NOVEMBER 1943", "kursk", "bocage", "rain", 1352f, 490f, "kharkov", "supply"),
            S("korsun", "Korsun", "east", 4, "FEBRUARY 1944", "kursk", "bocage", "fog", 1320f, 585f, "kiev"),
            S("lvov", "Lvov", "east", 5, "JULY 1944", "kursk", "village", "overcast", 1010f, 545f, "korsun"),
            S("warsaw", "Warsaw", "east", 6, "JANUARY 1945", "kursk", "village", "fog", 900f, 438f, "lvov", "supply"),
            S("poznan", "Poznań", "east", 7, "FEBRUARY 1945", "kursk", "village", "overcast", 795f, 390f, "warsaw", null, true),
            S("seelow", "Seelow Heights", "east", 8, "APRIL 1945", "kursk", "bocage", "fog", 688f, 372f, "poznan", null, true),
            // where the roads meet
            S("berlin", "Berlin", "both", 11, "APRIL 1945", "kursk", "village", "overcast", 632f, 368f, "magdeburg,seelow", "officer", true),
        };
        public static readonly int WestCount = 11, EastCount = 9;
        public static Sector ById(string id) { foreach (var s in All) if (s.id == id) return s; return null; }

        public static bool Taken(Sector s) => PlayerPrefs.GetInt("map.taken." + s.id, 0) == 1;
        public static int TakenOn(string road) { int n = 0; foreach (var s in All) if (s.road == road && Taken(s)) n++; return n; }
        public static bool Victory => Taken(ById("berlin"));
        /// <summary>The eastern road waits for the Kursk front to open (a first dawn).</summary>
        public static bool RoadOpen(Sector s) => s.road != "east" || Depot.TheatreOpen("kursk");
        /// <summary>A sector that can be attacked now: not taken, its road open, everything before it taken.</summary>
        public static bool Open(Sector s)
        {
            if (Taken(s) || !RoadOpen(s)) return false;
            foreach (var a in s.after) { var p = ById(a); if (p != null && !Taken(p)) return false; }
            return true;
        }
        /// <summary>The next sector along the road, once this one is taken (Berlin only when both roads are there).</summary>
        public static Sector Next(Sector s)
        {
            foreach (var n in All) if (System.Array.IndexOf(n.after, s.id) >= 0 && Open(n)) return n;
            return null;
        }

        // ---- counterattacks ----
        static System.DateTime Now => GameClock.UtcNow;
        static System.DateTime Ticks(string key) => long.TryParse(PlayerPrefs.GetString(key, ""), out var t) ? new System.DateTime(t, System.DateTimeKind.Utc) : System.DateTime.MinValue;
        static void SetTicks(string key, System.DateTime at) => PlayerPrefs.SetString(key, at.Ticks.ToString());
        public const float CounterHours = 24f, CounterEvery = 36f;
        /// <summary>The sector under counterattack, or null.</summary>
        public static Sector Counter => Victory ? null : ById(PlayerPrefs.GetString("map.counter", ""));
        public static string CounterLeft { get { var left = Ticks("map.counterUntil") - Now; return left.TotalHours >= 1 ? (int)left.TotalHours + " h " + left.Minutes + " min" : Mathf.Max(1, left.Minutes) + " min"; } }
        /// <summary>Time moves the war on: a counterattack begins when one is due, and one not beaten off in time takes its
        /// sector back. Returns what happened, for the map to say, or null.</summary>
        public static string Tick()
        {
            if (Victory) return null;
            var c = Counter;
            if (c != null && Now > Ticks("map.counterUntil"))
            {
                PlayerPrefs.SetInt("map.taken." + c.id, 0); PlayerPrefs.DeleteKey("map.counter"); SetTicks("map.counterNext", Now.AddHours(CounterEvery)); PlayerPrefs.Save();
                return c.name + " fell to a counterattack · take it back";
            }
            if (c != null || Ticks("map.counterNext") == System.DateTime.MinValue || Now < Ticks("map.counterNext")) return null;
            // the front line of each road: the deepest sector taken, never the landing or Kursk itself
            var fronts = new List<Sector>();
            foreach (var road in new[] { "west", "east" }) { Sector deepest = null; foreach (var s in All) if (s.road == road && Taken(s) && s.depth >= 1 && (deepest == null || s.depth > deepest.depth)) deepest = s; if (deepest != null) fronts.Add(deepest); }
            if (fronts.Count == 0) return null;
            var hit = fronts[Random.Range(0, fronts.Count)];
            PlayerPrefs.SetString("map.counter", hit.id); SetTicks("map.counterUntil", Now.AddHours(CounterHours)); PlayerPrefs.Save();
            return "Counterattack at " + hit.name + " · beat it off within a day";
        }

        /// <summary>Test switches --mapseed (the western road taken to Falaise, a counterattack due now) and --mapfall (one
        /// under way at Falaise whose day is up): the war moving on without waiting a day and a half for it.</summary>
        public static void TestSeed(bool fall)
        {
            foreach (var id in new[] { "omaha", "saintlo", "falaise" }) PlayerPrefs.SetInt("map.taken." + id, 1);
            if (fall) { PlayerPrefs.SetString("map.counter", "falaise"); SetTicks("map.counterUntil", Now.AddHours(-1)); }
            else { PlayerPrefs.DeleteKey("map.counter"); SetTicks("map.counterNext", Now.AddMinutes(-1)); }
            PlayerPrefs.Save();
        }

        /// <summary>A night in the sector held until dawn: the sector taken (or held against the counterattack) and paid
        /// for. Returns the line for the end sheet.</summary>
        public static string Win(Sector s)
        {
            if (Counter == s)
            {
                PlayerPrefs.DeleteKey("map.counter"); SetTicks("map.counterNext", Now.AddHours(CounterEvery)); PlayerPrefs.Save();
                Depot.AddPoints(800); Rewards.AddCrates("supply", 1); Bonds.AddStars(1);
                return s.name + " held against the counterattack · +800 points · a supply crate · +1 war bonds star";
            }
            if (Taken(s)) return null;
            PlayerPrefs.SetInt("map.taken." + s.id, 1);
            if (Ticks("map.counterNext") == System.DateTime.MinValue) SetTicks("map.counterNext", Now.AddHours(CounterEvery));   // the enemy starts striking back a day and a half after the first sector falls
            PlayerPrefs.Save();
            int pts = 1000 + 150 * s.depth; Depot.AddPoints(pts); Depot.AddCrewXp(300); Bonds.AddStars(2); Depot.Tally("sectors", 1);
            if (s.crate != null) Rewards.AddCrates(s.crate, 1);
            if (s.id == "berlin") Depot.AddGold(200);
            return s.name + " liberated · +" + pts.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + " points · +300 crew XP · +2 war bonds stars" + (s.crate != null ? " · " + Rewards.CrateName(s.crate).ToLowerInvariant() : "") + (s.id == "berlin" ? " · +200 gold · the war in Europe is won" : "");
        }
        /// <summary>What the sector pays when it is taken, for its card on the map.</summary>
        public static string Pay(Sector s) => "+" + (1000 + 150 * s.depth).ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + " points · +300 crew XP · +2 stars" + (s.crate != null ? " · " + Rewards.CrateName(s.crate).ToLowerInvariant() : "") + (s.id == "berlin" ? " · +200 gold" : "");
    }
}

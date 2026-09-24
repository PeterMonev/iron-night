using UnityEngine;

namespace IronNight
{
    /// <summary>
    /// The historical operations: three fronts, five nights each. A night has its place, its way in and its weather,
    /// a briefing, and three stars: held until dawn, and two goals of its own. Stars are kept as a mask per night, so a
    /// star won once stays won; a night opens when the one before it has been held until dawn.
    /// </summary>
    public static class Operations
    {
        public class Goal { public string stat, text, tag; public int n; }
        public class Night { public string name, picture, brief, route, weather; public Goal second, third; }
        public class Op { public string id, name, theatre, front, cover, blurb; public Night[] nights; }

        static Goal G(string stat, int n, string text, string tag) => new Goal { stat = stat, n = n, text = text, tag = tag };
        static Goal Kills(int n) => G("kills", n, "Destroy " + n + " vehicles", "vehicles");
        static readonly Goal Guns = G("paks", 4, "Knock out 4 anti-tank guns", "guns");
        static readonly Goal Hunters = G("infantry", 60, "Cut down 60 tank hunters", "tank hunters");
        static readonly Goal Intact = G("intact", 1, "Lose no wingman", "no wingman lost");
        static readonly Goal Objectives = G("objectives", 3, "Reach 3 objectives", "objectives");
        static readonly Goal Cats = G("tigers", 5, "Knock out 5 Tigers or Panthers", "big cats");
        static readonly Goal Ace = G("aces", 1, "Kill the named ace", "the ace");
        static readonly Goal Boss = G("boss", 1, "Destroy the boss before dawn", "the boss");
        static readonly Goal Lamps = G("lamps", 3, "Shoot out 3 searchlights", "searchlights");
        static readonly Goal Crates = G("crates", 8, "Pick up 8 ammo crates", "crates");
        static readonly Goal Tracks = G("tracked", 5, "Knock the tracks off 5 tanks", "tracks off");

        public static readonly Op[] All =
        {
            new Op
            {
                id = "cobra", name = "Operation Cobra", theatre = "normandy", cover = "op_cobra",
                front = "Normandy · 25-31 July 1944 · US Army",
                blurb = "Seven weeks in the hedgerows. Now the bombers open a gap at Saint-Lo and the armour goes through it.",
                nights = new[]
                {
                    new Night { name = "After the Carpet", picture = "op_cobra_1", route = "open", weather = "clear", second = Guns, third = Kills(30),
                        brief = "Fifteen hundred bombers have turned the Panzer Lehr line into a field of craters. Push through before they dig in again." },
                    new Night { name = "Hedgerow Cutters", picture = "op_cobra_2", route = "bocage", weather = "fog", second = Hunters, third = Intact,
                        brief = "Steel teeth welded to the bows cut through the hedges. Behind every one of them, the enemy waits." },
                    new Night { name = "Marigny Crossroads", picture = "op_cobra_3", route = "village", weather = "overcast", second = Objectives, third = Kills(40),
                        brief = "The whole breakout has to pass through this village. Take the crossroads and keep them." },
                    new Night { name = "Panzer Lehr", picture = "op_cobra_4", route = "open", weather = "clear", second = Cats, third = Ace,
                        brief = "What is left of Panzer Lehr comes back with everything it has, and its best crews lead." },
                    new Night { name = "The Breakout", picture = "op_cobra_5", route = "open", weather = "rain", second = Boss, third = Kills(50),
                        brief = "Coutances by morning. A Tiger ace sits across the road south." },
                },
            },
            new Op
            {
                id = "bastogne", name = "Bastogne", theatre = "ardennes", cover = "op_bastogne",
                front = "The Ardennes · 19-26 December 1944 · US Army",
                blurb = "Seven roads meet at Bastogne, and the Germans need every one of them. The town is surrounded. Hold it.",
                nights = new[]
                {
                    new Night { name = "Out of the Fog", picture = "op_bastogne_1", route = "bocage", weather = "fog", second = Lamps, third = Kills(30),
                        brief = "They came out of the forest at dawn. Now the fog is down and nobody knows where the front is." },
                    new Night { name = "The Crossroads", picture = "op_bastogne_2", route = "village", weather = "overcast", second = Objectives, third = Intact,
                        brief = "Hold the villages on the roads into town, whatever comes down them." },
                    new Night { name = "Nuts!", picture = "op_bastogne_3", route = "open", weather = "rain", second = Hunters, third = Cats,
                        brief = "Surrender, says the German note. The general's answer is one word. Now make it stick." },
                    new Night { name = "Clear Skies", picture = "op_bastogne_4", route = "village", weather = "clear", second = Crates, third = Ace,
                        brief = "The sky has cleared and the C-47s are dropping ammunition. Get to the crates before they do." },
                    new Night { name = "Patton's Tanks", picture = "op_bastogne_5", route = "open", weather = "clear", second = Boss, third = Kills(50),
                        brief = "Third Army is fighting up from the south. Hold until it breaks through. A King Tiger is out there." },
                },
            },
            new Op
            {
                id = "prokhorovka", name = "Prokhorovka", theatre = "kursk", cover = "op_prokhorovka",
                front = "Kursk · 10-12 July 1943 · Red Army",
                blurb = "The II SS Panzer Corps drives for the railway at Prokhorovka. Five hundred tanks against five hundred.",
                nights = new[]
                {
                    new Night { name = "Fire on the Steppe", picture = "op_prokhorovka_1", route = "open", weather = "clear", second = Cats, third = Kills(30),
                        brief = "The wheat is burning to the horizon and the Tigers are coming through it. Stop them in the fields." },
                    new Night { name = "The Tree Belts", picture = "op_prokhorovka_2", route = "bocage", weather = "overcast", second = Hunters, third = Intact,
                        brief = "Grenadiers creep along the windbreaks. Burn them out before they reach the guns." },
                    new Night { name = "Oktyabrsky", picture = "op_prokhorovka_3", route = "village", weather = "clear", second = Objectives, third = Kills(40),
                        brief = "The Oktyabrsky state farm must not fall. Its huts are the last cover before the railway." },
                    new Night { name = "Hill 252.2", picture = "op_prokhorovka_4", route = "open", weather = "fog", second = Ace, third = Tracks,
                        brief = "The hill changes hands every hour. Tonight the Leibstandarte's best crew holds it." },
                    new Night { name = "Prokhorovka", picture = "op_prokhorovka_5", route = "open", weather = "overcast", second = Boss, third = Kills(60),
                        brief = "Close the distance, get to their flanks and do not stop. There is no line behind this one." },
                },
            },
        };

        public static Op ById(string id) { foreach (var o in All) if (o.id == id) return o; return null; }

        static string Key(string op, int night) => "op." + op + "." + night;
        /// <summary>Which of the night's stars have been won: bit 0 the dawn, bits 1 and 2 its two goals.</summary>
        public static int Mask(string op, int night) => PlayerPrefs.GetInt(Key(op, night), 0);
        public static int Stars(string op, int night) { int m = Mask(op, night), n = 0; for (int i = 0; i < 3; i++) if ((m & (1 << i)) != 0) n++; return n; }
        public static int Stars(Op op) { int n = 0; for (int i = 1; i <= op.nights.Length; i++) n += Stars(op.id, i); return n; }
        public static int TotalStars { get { int n = 0; foreach (var o in All) n += Stars(o); return n; } }
        public static bool Held(string op, int night) => (Mask(op, night) & 1) != 0;
        public static bool Open(Op op, int night) => Depot.TheatreOpen(op.theatre) && (night == 1 || Held(op.id, night - 1));
        public static int Completed { get { int n = 0; foreach (var o in All) if (Held(o.id, o.nights.Length)) n++; return n; } }

        /// <summary>Adds tonight's stars to the ones already won; returns how many are new.</summary>
        public static int Record(string op, int night, int mask)
        {
            int before = Stars(op, night); PlayerPrefs.SetInt(Key(op, night), Mask(op, night) | mask); PlayerPrefs.Save();
            return Stars(op, night) - before;
        }

        /// <summary>The operation to offer first: the first one with a night still to hold, else the first.</summary>
        public static Op Current { get { foreach (var o in All) if (Depot.TheatreOpen(o.theatre) && !Held(o.id, o.nights.Length)) return o; return All[0]; } }

        /// <summary>The way in and the weather, as the briefing puts them.</summary>
        public static string Conditions(Op op, Night n)
        {
            bool kursk = op.theatre == "kursk", winter = op.theatre == "ardennes";
            string way = n.route == "village" ? "Through the village" : n.route == "bocage" ? (kursk ? "Along the tree belts" : "Through the bocage") : (kursk ? "Across the steppe" : "Across the open fields");
            string sky = n.weather == "fog" ? "fog" : n.weather == "rain" ? (winter ? "snow" : "rain") : n.weather == "overcast" ? "overcast" : winter ? "clear and freezing" : "clear, a moon";
            return way + " · " + sky;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace IronNight
{
    /// <summary>
    /// The enemy aces remember. The named ace of a night is one of a roster of eight (Keller, Brandt, ...), kept from
    /// night to night: one who gets away, or knocks out the leader, or picks off wingmen, is promoted (Leutnant up to
    /// Oberst), rides a heavier tank (a Panzer IV up to a King Tiger), takes more beating, and earns a name for it:
    /// the Butcher, the Hunter or the Ghost of the place where it happened. Knocked out, a promoted ace may still bail
    /// out of the burning tank and come back scarred; one who does not is dead, pays his bounty, and leaves a trophy.
    /// An old enemy comes back more often the more there is to settle.
    /// </summary>
    public static class Nemesis
    {
        [System.Serializable]
        public class Ace
        {
            public string name, nick, place, trophy, killedAt, killedOn; public int level = 1, met, leaders, wingmen, escapes, scars, portrait, flanked; public bool dead, studied;
            public string Rank => Ranks[Mathf.Clamp(level, 1, 5) - 1];
            public string Title => Rank + " " + name;                      // "Hptm. Keller"
            public string FullRank => RankNames[Mathf.Clamp(level, 1, 5) - 1];
        }
        [System.Serializable] class Roster { public List<Ace> aces = new List<Ace>(); }

        public static readonly string[] Surnames = { "Keller", "Brandt", "Hoffmann", "Ziegler", "Vogel", "Reinhardt", "Stahl", "Neumann" };
        static readonly string[] Ranks = { "Ltn.", "Oblt.", "Hptm.", "Maj.", "Obst." }, RankNames = { "Leutnant", "Oberleutnant", "Hauptmann", "Major", "Oberst" };
        static readonly string[] Trophies = { "binoculars", "map case", "turret number", "headset", "field cap", "logbook", "gunsight", "pistol holster" };

        static Roster data;
        static Roster Data { get { if (data == null) { var json = PlayerPrefs.GetString("nemesis", ""); data = string.IsNullOrEmpty(json) ? new Roster() : JsonUtility.FromJson<Roster>(json); if (data == null) data = new Roster(); } return data; } }
        static void Save() { PlayerPrefs.SetString("nemesis", JsonUtility.ToJson(Data)); PlayerPrefs.Save(); }
        public static bool PreferOld;   // test switch --ace: always an old enemy when there is one
        public static List<Ace> Living => Data.aces.FindAll(a => !a.dead);
        public static List<Ace> Dead => Data.aces.FindAll(a => a.dead);
        /// <summary>The most dangerous one at large, for the title's tile.</summary>
        public static Ace Worst { get { Ace w = null; foreach (var a in Living) if (a.met > 0 && (w == null || a.level > w.level || (a.level == w.level && a.leaders > w.leaders))) w = a; return w; } }

        /// <summary>His tank by rank: a Panzer IV, a Panther, a Tiger, a Tiger with more armour, a King Tiger.</summary>
        public static VehicleSpec Tank(Ace a)
        {
            var s = a.level >= 5 ? VehicleSpec.KingTiger : a.level >= 3 ? VehicleSpec.Tiger : a.level == 2 ? VehicleSpec.Panther : VehicleSpec.PanzerIV;
            if (!VehicleSpec.Available(s)) s = a.level >= 3 && VehicleSpec.Available(VehicleSpec.Tiger) ? VehicleSpec.Tiger : VehicleSpec.PanzerIV;
            return s;
        }
        public static float HpMul(Ace a) => 2.6f + 0.5f * (a.level - 1);   // the most wanted: hard to kill, harder each time he comes back
        public static int BountyPoints(Ace a) => 600 + 500 * (a.level - 1);
        public static int BountyGold(Ace a) => 10 * (a.level - 1);

        /// <summary>The ace for tonight: an old enemy (the more to settle, the likelier), or a new name while there are some.</summary>
        public static Ace Pick()
        {
            var living = Living;
            if (living.Count > 0 && (PreferOld || living.Count >= 4 || Random.value < 0.6f))
            {
                float total = 0f; foreach (var a in living) total += 1f + a.met + a.level + a.leaders * 2f;
                float r = Random.value * total; foreach (var a in living) { r -= 1f + a.met + a.level + a.leaders * 2f; if (r <= 0f) return a; }
                return living[living.Count - 1];
            }
            var free = new List<int>(); for (int i = 0; i < Surnames.Length; i++) if (!Data.aces.Exists(a => a.name == Surnames[i])) free.Add(i);
            if (free.Count == 0) { var old = Dead; if (old.Count == 0) return living[0]; var gone = old[0]; Data.aces.Remove(gone); free.Add(gone.portrait); }   // all eight names used: the longest dead makes room for a new man of that name
            int k = free[Random.Range(0, free.Count)];
            var ace = new Ace { name = Surnames[k], portrait = k };
            Data.aces.Add(ace); Save(); return ace;
        }
        public static void Met(Ace a, string place) { a.met++; a.place = place; Save(); }
        /// <summary>The hunt board's eight places, one for each name on the roster: 0 not met yet, 1 at large, 2 dead.</summary>
        public static int[] Board()
        {
            var s = new int[Surnames.Length];
            foreach (var a in Data.aces) { int i = System.Array.IndexOf(Surnames, a.name); if (i >= 0 && (a.dead || a.met > 0)) s[i] = a.dead ? 2 : 1; }
            return s;
        }

        /// <summary>His tank knocked out. A promoted ace may bail out of the burning wreck (true: he will be back, scarred
        /// and a rank higher); otherwise he is dead and his trophy goes on the wall. Either way the bounty is paid.</summary>
        public static bool Knocked(Ace a, string place)
        {
            bool escape = a.level >= 2 && Random.value < 0.2f + 0.06f * a.level;
            Depot.AddPoints(BountyPoints(a)); Depot.AddGold(BountyGold(a));
            if (escape) { a.escapes++; a.scars++; a.level = Mathf.Min(5, a.level + 1); a.nick = "the Phoenix"; a.place = place; a.studied = false; Save(); return true; }
            a.dead = true; a.killedAt = place; a.killedOn = GameClock.Now.ToString("d MMM yyyy", System.Globalization.CultureInfo.InvariantCulture);
            a.trophy = Trophies[Random.Range(0, Trophies.Length)]; a.studied = false; Save(); return false;
        }

        /// <summary>He was still on the field when the night ended: promoted if he knocked out the leader, or two wingmen,
        /// or got away once too often, and named for it. Returns the line for the end sheet.</summary>
        public static string Survived(Ace a, string place, int wingmen, bool leader)
        {
            string was = a.Title; a.place = place; a.wingmen += wingmen; a.studied = false;
            bool up;
            if (leader) { a.leaders++; up = true; a.nick = "the Butcher of " + place; }
            else if (wingmen > 0) { up = wingmen >= 2; if (string.IsNullOrEmpty(a.nick) || !a.nick.StartsWith("the Butcher")) a.nick = "the Hunter of " + place; }
            else { a.escapes++; up = a.escapes % 2 == 0; if (string.IsNullOrEmpty(a.nick)) a.nick = "the Ghost of " + place; }
            if (up) a.level = Mathf.Min(5, a.level + 1);
            Save();
            return was + " got away" + (up ? " · promoted to " + a.FullRank : "") + (!string.IsNullOrEmpty(a.nick) ? " · they call him " + a.nick : "");
        }

        /// <summary>Test switch --wanted: a roster to look at when there is none (three at large, one dead).</summary>
        public static void TestSeed()
        {
            if (Data.aces.Count > 0) return;
            Data.aces.Add(new Ace { name = "Keller", portrait = 0, level = 3, met = 3, leaders = 1, wingmen = 2, nick = "the Butcher of Paris", place = "Paris" });
            Data.aces.Add(new Ace { name = "Brandt", portrait = 1, level = 2, met = 2, wingmen = 3, nick = "the Hunter of Normandy", place = "Normandy" });
            Data.aces.Add(new Ace { name = "Hoffmann", portrait = 2, level = 1, met = 1, place = "Kursk" });
            Data.aces.Add(new Ace { name = "Stahl", portrait = 6, level = 2, met = 2, dead = true, nick = "the Ghost of Bastogne", killedAt = "Bastogne", killedOn = GameClock.Now.ToString("d MMM yyyy", System.Globalization.CultureInfo.InvariantCulture), trophy = "binoculars" });
            Save();
        }

        /// <summary>A rewarded ad: his tactics studied, a third more damage against him the next time he is met.</summary>
        public static void Study(Ace a) { a.studied = true; Save(); }
        /// <summary>What he learnt from being knocked out: flanked, he keeps his front to us after.</summary>
        public static void Learned(Ace a, bool flanked) { if (flanked) { a.flanked++; Save(); } }
        /// <summary>His record, short, for the poster.</summary>
        public static string Record(Ace a)
        {
            var parts = new List<string>();
            if (a.leaders > 0) parts.Add("knocked out your leader " + (a.leaders == 1 ? "once" : a.leaders + " times"));
            if (a.wingmen > 0) parts.Add(a.wingmen + (a.wingmen == 1 ? " wingman" : " wingmen"));
            if (a.escapes > 0) parts.Add("got away " + (a.escapes == 1 ? "once" : a.escapes + " times"));
            if (a.scars > 0) parts.Add("bailed out of a burning tank");
            return parts.Count == 0 ? "Met " + (a.met == 1 ? "once" : a.met + " times") + " · nothing settled yet" : char.ToUpper(parts[0][0]) + string.Join(" · ", parts).Substring(1);
        }
    }
}

using UnityEngine;

namespace IronNight
{
    /// <summary>
    /// The leader's kits, bought in the depot for points and carried into the night: repair kits for a thrown track and
    /// fire extinguishers. A kit does the job in a couple of seconds instead of the crew's ten or twelve; five gold does
    /// it at once. One of each to start with, five of each at most.
    /// </summary>
    public static class Kits
    {
        public const int TrackCost = 300, FireCost = 300, Max = 5;
        public const float TrackSecs = 2f, FireSecs = 1.5f;   // what is left of the job once a kit is on it
        static string Key(string kind) => "kit." + kind;
        /// <summary>How many of a kind are carried: "track" or "fire".</summary>
        public static int Count(string kind) => PlayerPrefs.GetInt(Key(kind), 1);
        public static int Cost(string kind) => kind == "fire" ? FireCost : TrackCost;
        public static bool Buy(string kind)
        {
            if (Count(kind) >= Max || !Depot.Spend(Cost(kind))) return false;
            PlayerPrefs.SetInt(Key(kind), Count(kind) + 1); PlayerPrefs.Save(); return true;
        }
        public static bool Use(string kind)
        {
            if (Count(kind) <= 0) return false;
            PlayerPrefs.SetInt(Key(kind), Count(kind) - 1); PlayerPrefs.Save(); return true;
        }
    }
}

using UnityEngine;

namespace IronNight
{
    /// <summary>
    /// What the crew eat before the night, bought in the depot: coffee and a K-ration (tea and sukhari for the Red Army),
    /// a D-ration chocolate bar (Lend-Lease chocolate), a hot meal from the field kitchen (kasha). The best one carried
    /// is eaten as the night begins: the crew's hands quicker, the turret faster, the aim steadier, the tank a little
    /// quicker; the hot meal also gives the leader one hit more.
    /// </summary>
    public static class Rations
    {
        public static readonly string[] Kinds = { "coffee", "chocolate", "meal" };
        public const int Max = 5;
        public static int Cost(string kind) => kind == "meal" ? 10 : kind == "chocolate" ? 500 : 200;   // the meal in gold, the others in points
        public static bool ForGold(string kind) => kind == "meal";
        public static float Boost(string kind) => kind == "meal" ? 0.15f : kind == "chocolate" ? 0.10f : 0.05f;
        public static string Name(string kind, string nation)
        {
            bool su = nation == "su";
            return kind == "meal" ? (su ? "Field kitchen kasha" : "Hot meal from the field kitchen") : kind == "chocolate" ? (su ? "Lend-Lease chocolate" : "D-ration chocolate") : (su ? "Tea and sukhari" : "Coffee and a K-ration");
        }
        public static string Line(string kind) => kind == "meal" ? "All the crew's skills +15% for a night, and the leader takes one hit more." : kind == "chocolate" ? "All the crew's skills +10% for a night: reload, turret, aim and speed." : "Reload and turret +5% for a night.";
        static string Key(string kind) => "ration." + kind;
        public static int Count(string kind) => PlayerPrefs.GetInt(Key(kind), 0);
        public static bool Buy(string kind)
        {
            if (Count(kind) >= Max) return false;
            if (ForGold(kind) ? !Depot.SpendGold(Cost(kind)) : !Depot.Spend(Cost(kind))) return false;
            PlayerPrefs.SetInt(Key(kind), Count(kind) + 1); PlayerPrefs.Save(); return true;
        }
        public const int AdsPerDay = 3;
        static string AdKey => "ration.ads." + GameClock.Now.ToString("yyyyMMdd");
        /// <summary>Chocolates still to be had for an advert today.</summary>
        public static int AdsLeft => Mathf.Max(0, AdsPerDay - PlayerPrefs.GetInt(AdKey, 0));
        /// <summary>The advert watched: one chocolate more (past the five if need be), one fewer left today.</summary>
        public static void FromAd()
        {
            PlayerPrefs.SetInt(AdKey, PlayerPrefs.GetInt(AdKey, 0) + 1); PlayerPrefs.SetInt(Key("chocolate"), Count("chocolate") + 1); PlayerPrefs.Save();
        }

        /// <summary>The best ration carried, eaten now (one fewer); null when there is none.</summary>
        public static string Eat()
        {
            for (int i = Kinds.Length - 1; i >= 0; i--)
                if (Count(Kinds[i]) > 0) { PlayerPrefs.SetInt(Key(Kinds[i]), Count(Kinds[i]) - 1); PlayerPrefs.Save(); return Kinds[i]; }
            return null;
        }
    }
}

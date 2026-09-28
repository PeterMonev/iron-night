using UnityEngine;

namespace IronNight
{
    /// <summary>
    /// The lucky dispatch: a wheel of eight prizes from the quartermaster, spun once a day for nothing and twice more
    /// for a rewarded ad each. The prizes: points, crew experience, gold, a supply crate, and now and then an officer's
    /// crate. The day turns at midnight UTC, as the daily challenge's does.
    /// </summary>
    public static class Dispatch
    {
        public class Prize { public string kind; public int amount, weight; }
        /// <summary>Round the wheel clockwise from the top, the order the sheet draws them in.</summary>
        public static readonly Prize[] Prizes =
        {
            new Prize { kind = "points", amount = 200, weight = 18 },
            new Prize { kind = "xp", amount = 150, weight = 16 },
            new Prize { kind = "gold", amount = 10, weight = 12 },
            new Prize { kind = "supply", amount = 1, weight = 10 },
            new Prize { kind = "points", amount = 500, weight = 12 },
            new Prize { kind = "xp", amount = 300, weight = 12 },
            new Prize { kind = "gold", amount = 25, weight = 6 },
            new Prize { kind = "officer", amount = 1, weight = 4 },
        };
        public const int AdSpins = 2;

        /// <summary>The spins taken today.</summary>
        public static int SpinsToday => PlayerPrefs.GetString("dispatch.day", "") == Daily.Today ? PlayerPrefs.GetInt("dispatch.spins", 0) : 0;
        public static bool FreeSpin => SpinsToday == 0;
        /// <summary>A spin for an ad is there: the free one is taken, the ad ones are not all.</summary>
        public static bool AdSpin => SpinsToday >= 1 && SpinsToday <= AdSpins;
        public static int AdSpinsLeft => Mathf.Clamp(AdSpins + 1 - SpinsToday, 0, AdSpins);

        /// <summary>A prize by the weights, its place on the wheel.</summary>
        public static int Draw()
        {
            int total = 0; foreach (var p in Prizes) total += p.weight;
            int r = Random.Range(0, total);
            for (int i = 0; i < Prizes.Length; i++) { r -= Prizes[i].weight; if (r < 0) return i; }
            return 0;
        }

        /// <summary>A spin taken and its prize given.</summary>
        public static void Spun(int prize)
        {
            PlayerPrefs.SetInt("dispatch.spins", SpinsToday + 1); PlayerPrefs.SetString("dispatch.day", Daily.Today); PlayerPrefs.Save();
            var p = Prizes[prize]; Rewards.GiveLoot(new Rewards.Loot { kind = p.kind, amount = p.amount });
        }
    }
}

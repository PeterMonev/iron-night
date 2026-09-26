using UnityEngine;

namespace IronNight
{
    /// <summary>
    /// Gold for real money. On Android the packs are Google Play purchases (Unity IAP, product ids as below, set up in the
    /// Play Console); until that is in, the phone build says the shop opens with the release, and the desktop build makes
    /// test purchases that cost nothing, so the whole shop can be tried.
    /// </summary>
    public static class Store
    {
        public class Pack { public string id, bonus, price; public int gold; public bool best; }
        public static readonly Pack[] Packs =
        {
            new Pack { id = "gold_100", gold = 100, price = "$0.99" },
            new Pack { id = "gold_550", gold = 550, price = "$4.99", bonus = "+10%" },
            new Pack { id = "gold_1200", gold = 1200, price = "$9.99", bonus = "+20%" },
            new Pack { id = "gold_2600", gold = 2600, price = "$19.99", bonus = "+30%" },
            new Pack { id = "gold_7000", gold = 7000, price = "$49.99", bonus = "+40%", best = true },
        };

        /// <summary>True where nothing is really charged: the desktop build and the editor.</summary>
        public static bool TestPurchases => !Application.isMobilePlatform;

        /// <summary>The price as the store shows it in the player's own currency; the dollar price until the store answers.</summary>
        public static string Price(Pack p) => p.price;

        /// <summary>Buys a pack. done(true, line) once the gold is in the depot; done(false, why) if not.</summary>
        public static void Buy(Pack p, System.Action<bool, string> done)
        {
            if (TestPurchases) { Depot.AddGold(p.gold); done(true, "Test purchase · +" + p.gold.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + " gold · nothing charged"); return; }
            done(false, "The shop opens with the Google Play release");
        }
    }
}

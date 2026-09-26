using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine.Purchasing;
#endif

namespace IronNight
{
    /// <summary>
    /// Gold for real money. On Android the packs are Google Play purchases (Unity IAP); their ids below are the ones to
    /// make in the Play Console, as consumable in-app products. The desktop build makes test purchases that cost nothing,
    /// so the whole shop can be tried. The gold goes in before the purchase is confirmed to Google Play, so a purchase is
    /// never lost: one the game did not finish comes back at the next start.
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

        /// <summary>The price as Google Play shows it in the player's own currency; the dollar price until it answers.</summary>
        public static string Price(Pack p) => p.price;

#if UNITY_ANDROID && !UNITY_EDITOR
        static StoreController store; static bool connected, connecting; static System.Action<bool, string> waiting;

        /// <summary>Connects to Google Play and asks for the packs and their prices. Once, at start; again if it failed.</summary>
        public static async void Start()
        {
            if (connected || connecting) return; connecting = true;
            try { await Unity.Services.Core.UnityServices.InitializeAsync(); } catch (System.Exception e) { Debug.LogWarning("Iron Night: services: " + e.Message); }
            if (store == null)
            {
                store = UnityIAPServices.StoreController();
                store.OnProductsFetched += products => { foreach (var pr in products) { var p = System.Array.Find(Packs, x => x.id == pr.definition.id); if (p != null && pr.metadata != null && !string.IsNullOrEmpty(pr.metadata.localizedPriceString)) p.price = pr.metadata.localizedPriceString; } };
                store.OnPurchasePending += Pending;
                store.OnPurchaseFailed += f => Finish(false, f.FailureReason == PurchaseFailureReason.UserCancelled ? "Purchase cancelled" : "The purchase did not go through");
                store.OnPurchaseDeferred += d => Finish(false, "Waiting for the payment · the gold comes when it clears");
                store.OnStoreDisconnected += d => connected = false;
            }
            try { await store.Connect(); connected = true; }
            catch (System.Exception e) { Debug.LogWarning("Iron Night: Google Play: " + e.Message); connecting = false; return; }
            var defs = new List<ProductDefinition>(); foreach (var p in Packs) defs.Add(new ProductDefinition(p.id, ProductType.Consumable));
            store.FetchProducts(defs);
            store.FetchPurchases();   // a purchase paid for but not given out (the game closed half way) comes back as pending
            connecting = false;
        }

        static void Pending(PendingOrder order)
        {
            // each transaction pays once, even if Google Play offers it again before it hears the confirmation
            string tx = order.Info != null ? order.Info.TransactionID : null, paid = PlayerPrefs.GetString("store.paid", "");
            int gold = 0;
            if (string.IsNullOrEmpty(tx) || !paid.Contains("|" + tx + "|"))
            {
                foreach (var item in order.CartOrdered.Items()) { var p = System.Array.Find(Packs, x => x.id == item.Product.definition.id); if (p != null) gold += p.gold * item.Quantity; }
                if (gold > 0) Depot.AddGold(gold);
                if (!string.IsNullOrEmpty(tx)) { paid = "|" + tx + "|" + paid; if (paid.Length > 4000) paid = paid.Substring(0, 4000); PlayerPrefs.SetString("store.paid", paid); PlayerPrefs.Save(); }
            }
            store.ConfirmPurchase(order);
            Finish(true, gold > 0 ? "+" + gold.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + " gold" : "Purchase done");
        }

        static void Finish(bool ok, string line) { var d = waiting; waiting = null; d?.Invoke(ok, line); }
#else
        public static void Start() { }
#endif

        /// <summary>Buys a pack. done(true, line) once the gold is in the depot; done(false, why) if not.</summary>
        public static void Buy(Pack p, System.Action<bool, string> done)
        {
            if (TestPurchases) { Depot.AddGold(p.gold); done(true, "Test purchase · +" + p.gold.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + " gold · nothing charged"); return; }
#if UNITY_ANDROID && !UNITY_EDITOR
            if (store == null || !connected) { Start(); done(false, "Google Play is not answering · try again in a moment"); return; }
            var product = store.GetProductById(p.id);
            if (product == null || !product.availableToPurchase) { done(false, "This pack is not on sale yet"); return; }
            waiting = done; store.PurchaseProduct(product);
#else
            done(false, "Purchases need the Google Play version");
#endif
        }
    }
}

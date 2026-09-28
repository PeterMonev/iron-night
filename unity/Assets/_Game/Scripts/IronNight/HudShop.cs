using UnityEngine;
using UnityEngine.UI;

namespace IronNight
{
    /// <summary>
    /// The gold side of the HUD: the gold pill in the top bars (a tap opens the shop), the gold bar drawn in code, and the
    /// shop itself (the post exchange): gold packs for money or a free handful for an ad, premium service, and gold
    /// changed into points or crew experience. Gold is only ever spent on a second tap.
    /// </summary>
    public partial class Hud
    {
        public System.Action OnGoldAd;   // the end sheet's repair (or double score) paid with gold instead of an ad
        static readonly Color GoldInk = new Color(1f, 0.84f, 0.42f);
        static Sprite goldSprite;
        Text goldLine, depotGold, shopGold; GameObject shopSheet, goldBtn; Transform shopBody; Text shopNote; string armed; float armedAt;

        /// <summary>A gold bar seen from the front and a little above: a light top, a warm front, a dark side.</summary>
        public static Sprite GoldSprite()
        {
            if (goldSprite != null) return goldSprite;
            goldSprite = UiSprite("icon_gold"); if (goldSprite != null) return goldSprite;   // the painted stack of bars, or the drawn bar below when it is missing
            const int W = 96, H = 64; var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            // the three faces we see, as quads from the bottom left: the front, the top, the right side
            var front = new[] { new Vector2(6, 6), new Vector2(78, 6), new Vector2(66, 30), new Vector2(18, 30) };
            var top = new[] { new Vector2(18, 30), new Vector2(66, 30), new Vector2(78, 46), new Vector2(30, 46) };
            var side = new[] { new Vector2(78, 6), new Vector2(90, 22), new Vector2(78, 46), new Vector2(66, 30) };
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
            {
                var sum = new Color(0f, 0f, 0f, 0f); int hits = 0;
                for (int sy = 0; sy < 4; sy++) for (int sx = 0; sx < 4; sx++)
                {
                    var p = new Vector2(x + (sx + 0.5f) / 4f, y + (sy + 0.5f) / 4f); Color c;
                    if (Inside(top, p)) c = Color.Lerp(new Color(1f, 0.82f, 0.42f), new Color(1f, 0.93f, 0.66f), (p.x - 18f) / 60f);
                    else if (Inside(front, p)) c = Color.Lerp(new Color(0.8f, 0.52f, 0.13f), new Color(0.98f, 0.76f, 0.3f), (p.y - 6f) / 24f);
                    else if (Inside(side, p)) c = new Color(0.62f, 0.39f, 0.09f);
                    else continue;
                    sum += c; hits++;
                }
                if (hits == 0) { tex.SetPixel(x, y, new Color(0f, 0f, 0f, 0f)); continue; }
                var col = sum / hits; col.a = hits / 16f; tex.SetPixel(x, y, col);
            }
            tex.Apply(); return goldSprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 100f);
        }
        /// <summary>A point inside a convex quad wound counter-clockwise: to the left of every edge.</summary>
        static bool Inside(Vector2[] q, Vector2 p)
        {
            for (int i = 0; i < q.Length; i++) { var a = q[i]; var b = q[(i + 1) % q.Length]; if ((b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x) < 0f) return false; }
            return true;
        }

        /// <summary>Gold bars stacked in a box: one, or rows of them piled into a pyramid.</summary>
        static void GoldPile(Transform parent, Vector2 center, Vector2 box, int bars)
        {
            int rows = bars <= 1 ? 1 : bars <= 3 ? 2 : bars <= 6 ? 3 : bars <= 10 ? 4 : 5;
            var gs = GoldSprite(); float aspect = gs.rect.width / gs.rect.height, w = Mathf.Min(box.x / (rows * 0.78f + 0.22f), box.y * aspect / (1f + (rows - 1) * 0.42f)), h = w / aspect;   // as wide against its height as the picture
            for (int r = 0; r < rows; r++)
            {
                int n = rows - r; float y = center.y - box.y * 0.5f + h * 0.5f + r * h * 0.42f;
                for (int i = 0; i < n; i++) { var img = MakeImage(parent, "Bar", new Vector2(0.5f, 0.5f), new Vector2(center.x + (i - (n - 1) * 0.5f) * w * 0.78f, y), new Vector2(w, h), Color.white); img.sprite = GoldSprite(); }
            }
        }
        static int Bars(int gold) => gold >= 7000 ? 15 : gold >= 2600 ? 10 : gold >= 1200 ? 6 : gold >= 550 ? 3 : 1;

        /// <summary>The gold pill of a top bar: a bar of gold, the amount and, where a tap opens the shop, a plus.</summary>
        Text GoldPill(Transform parent, Vector2 topRight, float width, bool opens)
        {
            var pill = MakeCard(parent, "Gold", new Vector2(1f, 1f), topRight, new Vector2(width, 60f), new Color(0.06f, 0.07f, 0.09f, 0.75f), 0.2f); pill.rectTransform.pivot = new Vector2(1f, 1f);
            pill.transform.Find("Edge").GetComponent<Image>().color = new Color(1f, 0.8f, 0.35f, 0.4f);
            var bar = MakeImage(pill.transform, "Bar", new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(45f, 30f), Color.white); bar.sprite = GoldSprite(); bar.preserveAspect = true; bar.rectTransform.pivot = new Vector2(0f, 0.5f);
            var t = MakeText(pill.transform, "Text", new Vector2(0f, 0.5f), new Vector2(64f, 0f), TextAnchor.MiddleLeft, 28, GoldInk); t.rectTransform.pivot = new Vector2(0f, 0.5f); t.rectTransform.sizeDelta = new Vector2(width - 130f, 60f); t.font = BoldFont();
            if (!opens) return t;
            var plus = MakeImage(pill.transform, "Plus", new Vector2(1f, 0.5f), new Vector2(-10f, 0f), new Vector2(40f, 40f), OpAmber); plus.sprite = Rounded(); plus.type = Image.Type.Sliced;
            var pt = MakeText(plus.transform, "T", new Vector2(0.5f, 0.5f), new Vector2(0f, 1f), TextAnchor.MiddleCenter, 34, new Color(0.12f, 0.09f, 0.04f)); pt.text = "+"; pt.font = BoldFont(); pt.rectTransform.sizeDelta = new Vector2(40f, 40f);
            pill.raycastTarget = true; pill.gameObject.AddComponent<PressFeel>();
            pill.gameObject.AddComponent<Button>().onClick.AddListener(() => { Sfx.Click(); ShowShop(); });
            return t;
        }

        /// <summary>Every gold pill shows what is in the depot now.</summary>
        void RefreshGold()
        {
            if (goldLine != null) Tick(goldLine, Depot.Gold); if (depotGold != null) Tick(depotGold, Depot.Gold);
            if (shopGold != null) shopGold.text = Depot.Gold.ToString("N0", En);
        }

        /// <summary>The shop, over the title or the depot.</summary>
        public void ShowShop()
        {
            if (shopSheet == null)
            {
                shopSheet = new GameObject("Shop", typeof(RectTransform), typeof(Image)); shopSheet.transform.SetParent(canvas.transform, false); Stretch(shopSheet); shopSheet.GetComponent<Image>().color = new Color(0.02f, 0.02f, 0.03f, 1f);
                if (curtain != null) shopSheet.transform.SetSiblingIndex(curtain.transform.GetSiblingIndex());   // under the curtain, over the title and the depot
                var body = new GameObject("Body", typeof(RectTransform)); body.transform.SetParent(shopSheet.transform, false); Stretch(body); shopBody = body.transform;
            }
            shopSheet.SetActive(true); armed = null; BuildShop(null);
        }

        void BuildShop(string line)
        {
            foreach (Transform c in shopBody) Destroy(c.gameObject);
            var b = shopBody;
            { var glow = MakeImage(b, "Glow", new Vector2(0.5f, 1f), new Vector2(0f, 250f), new Vector2(1600f, 1100f), new Color(1f, 0.72f, 0.28f, 0.1f)); glow.sprite = Lightswarm.ProceduralSprites.Glow(64, 0.1f); }
            MakeGhost(b, "BACK", new Vector2(0f, 1f), new Vector2(130, -95), new Vector2(200, 80), 26, () => { shopSheet.SetActive(false); RefreshGold(); });
            shopGold = GoldPill(b, new Vector2(-50f, -65f), 300f, false); shopGold.text = Depot.Gold.ToString("N0", En);
            var ey = MakeText(b, "Eyebrow", new Vector2(0.5f, 1f), new Vector2(0, -150), TextAnchor.MiddleCenter, 26, OpAmber); ey.text = Spaced("POST EXCHANGE"); ey.font = LabelFont(); ey.rectTransform.sizeDelta = new Vector2(600, 40);
            var ti = MakeText(b, "Title", new Vector2(0.5f, 1f), new Vector2(0, -190), TextAnchor.MiddleCenter, 120, OpInk); ti.text = "SHOP"; Engrave(ti); ti.rectTransform.sizeDelta = new Vector2(900, 150); ti.verticalOverflow = VerticalWrapMode.Overflow;

            // gold: a free handful for an ad, then the packs
            ShopHeading(b, -380f, "GOLD");
            FreeCard(-250f, -424f);
            for (int i = 0; i < Store.Packs.Length; i++) PackCard(i % 2 == 0 ? 250f : -250f, -424f - ((i + 1) / 2) * 304f, Store.Packs[i]);   // room between the rows for the tags on their edges

            // premium service
            ShopHeading(b, -1340f, "PREMIUM SERVICE");
            var pl = MakeText(b, "Premium", new Vector2(0.5f, 1f), new Vector2(0, -1382), TextAnchor.MiddleCenter, 24, Depot.Premium ? OpAmber : OpDim); pl.rectTransform.sizeDelta = new Vector2(980, 40);
            pl.text = Depot.Premium ? "Active · " + Depot.PremiumLeft + " left · +50% points, crew and tank experience" : "+50% points, crew and tank experience from every night";
            for (int i = 0; i < Depot.PremiumOffers.Length; i++)
            {
                var o = Depot.PremiumOffers[i]; string days = o.days + (o.days == 1 ? " DAY" : " DAYS");
                GoldButton(b, new Vector2((i - 1) * 330f, -1428f), new Vector2(310f, 110f), days, o.gold, () => SpendConfirm("premium" + o.days, o.gold, "Tap again: " + days.ToLowerInvariant() + " of premium service for " + o.gold + " gold", () => Depot.BuyPremium(o), "Premium service · +" + days.ToLowerInvariant()));
            }

            // exchange
            ShopHeading(b, -1580f, "EXCHANGE");
            var give = Depot.ExchangeGives.ToString("N0", En);
            GoldButton(b, new Vector2(-250f, -1622f), new Vector2(480f, 100f), "GET " + give + " POINTS", Depot.ExchangeGold, () => SpendConfirm("points", Depot.ExchangeGold, "Tap again: " + Depot.ExchangeGold + " gold for " + give + " points", () => Depot.Exchange(false), "+" + give + " points"));
            GoldButton(b, new Vector2(250f, -1622f), new Vector2(480f, 100f), "GET " + give + " CREW XP", Depot.ExchangeGold, () => SpendConfirm("crew", Depot.ExchangeGold, "Tap again: " + Depot.ExchangeGold + " gold for " + give + " crew XP", () => Depot.Exchange(true), "+" + give + " crew XP"));

            shopNote = MakeText(b, "Note", new Vector2(0.5f, 1f), new Vector2(0, -1762), TextAnchor.MiddleCenter, 28, OpAmber); shopNote.rectTransform.sizeDelta = new Vector2(980, 50); shopNote.font = BoldFont(); shopNote.text = line ?? "";
            var foot = MakeText(b, "Small", new Vector2(0.5f, 1f), new Vector2(0, -1806), TextAnchor.MiddleCenter, 20, OpDim); foot.rectTransform.sizeDelta = new Vector2(980, 60);
            foot.text = Store.TestPurchases ? "Test build: purchases here charge nothing." : "Payments go through Google Play. Gold is kept on this device.";
        }

        /// <summary>A section name between two thin rules.</summary>
        static void ShopHeading(Transform parent, float y, string name)
        {
            var t = MakeText(parent, "Heading", new Vector2(0.5f, 1f), new Vector2(0, y), TextAnchor.MiddleCenter, 24, OpDim); t.text = Spaced(name); t.font = LabelFont(); t.rectTransform.sizeDelta = new Vector2(600, 36);
            for (int s = -1; s <= 1; s += 2) { var r = MakeImage(parent, "Rule", new Vector2(0.5f, 1f), new Vector2(s * 330f, y - 18f), new Vector2(280f, 2f), new Color(1f, 1f, 1f, 0.12f)); r.rectTransform.pivot = new Vector2(0.5f, 0.5f); }
        }

        /// <summary>A card of gold for money: the pile, the amount, its bonus, the price to press.</summary>
        void PackCard(float x, float yTop, Store.Pack p)
        {
            var card = MakeCard(shopBody, "Pack", new Vector2(0.5f, 1f), new Vector2(x, yTop), new Vector2(480f, 270f), new Color(0.07f, 0.075f, 0.09f, 0.96f), 0.16f); card.rectTransform.pivot = new Vector2(0.5f, 1f);
            if (p.best) card.transform.Find("Edge").GetComponent<Image>().color = new Color(1f, 0.8f, 0.35f, 0.85f);
            var t = card.transform;
            var glow = MakeImage(t, "Glow", new Vector2(0.5f, 0.5f), new Vector2(-115f, 30f), new Vector2(340f, 240f), new Color(1f, 0.75f, 0.3f, 0.18f)); glow.sprite = Lightswarm.ProceduralSprites.Glow(64, 0.1f);
            GoldPile(t, new Vector2(-112f, 34f), new Vector2(224f, 132f), Bars(p.gold));
            var amt = MakeText(t, "Gold", new Vector2(0.5f, 0.5f), new Vector2(105f, 44f), TextAnchor.MiddleCenter, 80, GoldInk); amt.text = p.gold.ToString("N0", En); Serif(amt); amt.rectTransform.sizeDelta = new Vector2(250f, 110f); amt.verticalOverflow = VerticalWrapMode.Overflow;
            var lb = MakeText(t, "Label", new Vector2(0.5f, 0.5f), new Vector2(105f, -6f), TextAnchor.MiddleCenter, 20, OpDim); lb.text = Spaced("GOLD"); lb.font = LabelFont(); lb.rectTransform.sizeDelta = new Vector2(250f, 30f);
            if (p.bonus != null) Ribbon(t, p.best ? "BEST VALUE · " + p.bonus : p.bonus, p.best);
            var buy = MakePrimary(t, Store.Price(p), new Vector2(0.5f, 0f), new Vector2(0f, 52f), new Vector2(440f, 68f), 30, () => Store.Buy(p, (ok, why) => { if (ok) { Sfx.Pickup(); RefreshGold(); } BuildShop(why); }));
            buy.transform.Find("Label").GetComponent<Text>().text = Store.Price(p);
        }

        /// <summary>The free card: a handful of gold for a rewarded ad, a few times a day.</summary>
        void FreeCard(float x, float yTop)
        {
            int left = Depot.GoldAdsLeft;
            var card = MakeCard(shopBody, "Free", new Vector2(0.5f, 1f), new Vector2(x, yTop), new Vector2(480f, 270f), new Color(0.06f, 0.08f, 0.11f, 0.96f), 0.22f); card.rectTransform.pivot = new Vector2(0.5f, 1f);
            var t = card.transform;
            var glow = MakeImage(t, "Glow", new Vector2(0.5f, 0.5f), new Vector2(-115f, 30f), new Vector2(340f, 240f), new Color(1f, 0.75f, 0.3f, 0.12f)); glow.sprite = Lightswarm.ProceduralSprites.Glow(64, 0.1f);
            GoldPile(t, new Vector2(-112f, 34f), new Vector2(170f, 100f), 1);
            var amt = MakeText(t, "Gold", new Vector2(0.5f, 0.5f), new Vector2(105f, 44f), TextAnchor.MiddleCenter, 80, GoldInk); amt.text = "+" + Depot.GoldPerAd; Serif(amt); amt.rectTransform.sizeDelta = new Vector2(250f, 110f); amt.verticalOverflow = VerticalWrapMode.Overflow;
            var lb = MakeText(t, "Label", new Vector2(0.5f, 0.5f), new Vector2(105f, -6f), TextAnchor.MiddleCenter, 20, OpDim); lb.text = Spaced("FREE GOLD"); lb.font = LabelFont(); lb.rectTransform.sizeDelta = new Vector2(250f, 30f);
            Ribbon(t, left > 0 ? left + " of " + Depot.GoldAdsPerDay + " today" : "more tomorrow", false);
            var ad = MakeButton(t, left > 0 ? "Watch an ad" : "Come back tomorrow", new Vector2(0.5f, 0f), new Vector2(0f, 52f), new Vector2(440f, 68f), 28, () =>
            {
                if (Depot.GoldAdsLeft <= 0) return;
                Ads.Rewarded("gold", () => { if (Depot.GoldAdWatched()) { Sfx.Pickup(); RefreshGold(); BuildShop("+" + Depot.GoldPerAd + " gold"); } });
            });
            ad.GetComponent<Image>().color = left > 0 ? XpDeep : new Color(0.12f, 0.12f, 0.13f, 0.9f);
            ad.transform.Find("Label").GetComponent<Text>().color = left > 0 ? new Color(0.96f, 0.97f, 1f) : new Color(0.5f, 0.48f, 0.45f);
        }

        /// <summary>A small tag sitting on a card's top edge at the right, clear of what the card shows.</summary>
        static void Ribbon(Transform card, string text, bool gold)
        {
            var rb = MakeImage(card, "Ribbon", new Vector2(1f, 1f), new Vector2(-22f, 0f), new Vector2(Mathf.Max(110f, 16f * text.Length + 30f), 40f), gold ? OpAmber : new Color(0.2f, 0.21f, 0.24f, 1f)); rb.sprite = Rounded(); rb.type = Image.Type.Sliced; rb.rectTransform.pivot = new Vector2(1f, 0.5f);
            var rt = MakeText(rb.transform, "T", new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleCenter, 20, gold ? new Color(0.12f, 0.09f, 0.04f) : OpInk); rt.text = text; rt.font = BoldFont(); rt.rectTransform.sizeDelta = rb.rectTransform.sizeDelta;
        }

        /// <summary>A button that costs gold: a line of what it gives, and the price with a bar of gold beside it.</summary>
        void GoldButton(Transform parent, Vector2 topCenter, Vector2 size, string what, int gold, System.Action onClick)
        {
            var btn = MakeButton(parent, "", new Vector2(0.5f, 1f), topCenter + new Vector2(0f, -size.y * 0.5f), size, 24, onClick);
            btn.GetComponent<Image>().color = new Color(0.08f, 0.085f, 0.1f, 0.96f);
            var edge = MakeImage(btn.transform, "Edge", new Vector2(0.5f, 0.5f), Vector2.zero, size, new Color(1f, 0.8f, 0.35f, 0.35f)); edge.sprite = Outline(); edge.type = Image.Type.Sliced; edge.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            bool tall = size.y > 105f;
            var w = MakeText(btn.transform, "What", new Vector2(0.5f, 0.5f), new Vector2(tall ? 0f : 60f, tall ? 22f : 0f), TextAnchor.MiddleCenter, tall ? 26 : 24, OpInk); w.text = what; w.font = BoldFont(); w.rectTransform.sizeDelta = new Vector2(size.x - 20f, 40f);
            float px = tall ? -24f : -size.x * 0.5f + 60f, py = tall ? -22f : 0f;
            var bar = MakeImage(btn.transform, "Bar", new Vector2(0.5f, 0.5f), new Vector2(px - 26f, py), new Vector2(42f, 28f), Color.white); bar.sprite = GoldSprite(); bar.preserveAspect = true;
            var g = MakeText(btn.transform, "Gold", new Vector2(0.5f, 0.5f), new Vector2(px + 32f, py), TextAnchor.MiddleCenter, 30, GoldInk); g.text = gold.ToString("N0", En); g.font = BoldFont(); g.rectTransform.sizeDelta = new Vector2(90f, 40f);
            btn.transform.Find("Label").gameObject.SetActive(false);
        }

        /// <summary>Gold is only spent on a second tap: the first one says what the second will do.</summary>
        void SpendConfirm(string key, int gold, string ask, System.Func<bool> spend, string done)
        {
            if (Depot.Gold < gold) { armed = null; shopNote.text = "Not enough gold · " + gold.ToString("N0", En) + " needed"; return; }
            if (armed != key || Time.unscaledTime - armedAt > 4f) { armed = key; armedAt = Time.unscaledTime; shopNote.text = ask; return; }
            armed = null; if (spend()) { Sfx.Pickup(); RefreshGold(); BuildShop(done); }
        }
    }
}

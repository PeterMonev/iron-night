using UnityEngine;
using UnityEngine.UI;

namespace IronNight
{
    /// <summary>
    /// War bonds on the screen: the medal button on the title (top right, the tier and a badge for rewards waiting), and
    /// the season's sheet: how far along, the forty tiers side by side (free above, gold below; drag to see them all),
    /// the gold bond, and the week's orders. Gold is only spent on a second tap.
    /// </summary>
    public partial class Hud
    {
        GameObject bondsSheet, bondsBtn, bondsBadge; Transform bondsBody; ScrollRect bondsScroll; Text bondsNote, bondsTier, bondsBadgeN; string bondsArmed; float bondsArmedAt;
        const float TierW = 230f;

        /// <summary>The medal under the currency pills, top right of the title.</summary>
        void BuildBondsButton()
        {
            var b = MakeButton(titleSheet.transform, "", new Vector2(1f, 1f), new Vector2(-145, -385), new Vector2(190, 190), 20, () => ShowBonds()); bondsBtn = b;
            b.GetComponent<Image>().color = new Color(0.05f, 0.055f, 0.07f, 0.7f); b.transform.Find("Label").gameObject.SetActive(false);
            var edge = MakeImage(b.transform, "Edge", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(190, 190), new Color(1f, 0.8f, 0.35f, 0.35f)); edge.sprite = Outline(); edge.type = Image.Type.Sliced; edge.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var glow = MakeImage(b.transform, "Glow", new Vector2(0.5f, 0.5f), new Vector2(0, 22), new Vector2(180, 160), new Color(1f, 0.72f, 0.3f, 0.2f)); glow.sprite = Lightswarm.ProceduralSprites.Glow(64, 0.1f); glow.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var ring = MakeImage(b.transform, "Ring", new Vector2(0.5f, 0.5f), new Vector2(0, 24), new Vector2(104, 104), GoldInk); ring.sprite = Lightswarm.ProceduralSprites.Ring(64, 0.09f); ring.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var star = MakeImage(b.transform, "Star", new Vector2(0.5f, 0.5f), new Vector2(0, 24), new Vector2(64, 64), GoldInk); star.sprite = StarSprite(); star.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var lb = MakeText(b.transform, "Name", new Vector2(0.5f, 0f), new Vector2(0, 34), TextAnchor.LowerCenter, 18, OpAmber); lb.text = Spaced("WAR BONDS"); lb.font = BoldFont(); lb.rectTransform.sizeDelta = new Vector2(184, 26);
            bondsTier = MakeText(b.transform, "Tier", new Vector2(0.5f, 0f), new Vector2(0, 8), TextAnchor.LowerCenter, 20, OpInk); bondsTier.font = BoldFont(); bondsTier.rectTransform.sizeDelta = new Vector2(184, 28);
            var badge = MakeImage(b.transform, "Badge", new Vector2(1f, 1f), new Vector2(10, 10), new Vector2(52, 52), new Color(0.86f, 0.22f, 0.16f)); badge.sprite = Lightswarm.ProceduralSprites.Glow(64, 0.9f); badge.rectTransform.pivot = new Vector2(1f, 1f); bondsBadge = badge.gameObject;
            bondsBadgeN = MakeText(badge.transform, "N", new Vector2(0.5f, 0.5f), new Vector2(0, 1), TextAnchor.MiddleCenter, 28, Color.white); bondsBadgeN.font = BoldFont(); bondsBadgeN.rectTransform.sizeDelta = new Vector2(52, 52); bondsBadgeN.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }
        void RefreshBondsButton()
        {
            if (bondsBtn == null) return;
            bondsTier.text = Spaced("TIER " + Bonds.Tier); int n = Bonds.Waiting; bondsBadge.SetActive(n > 0); bondsBadgeN.text = n > 99 ? "99" : n.ToString();
        }

        /// <summary>The season's sheet, over the title.</summary>
        public void ShowBonds()
        {
            if (bondsSheet == null)
            {
                bondsSheet = new GameObject("Bonds", typeof(RectTransform), typeof(Image)); bondsSheet.transform.SetParent(canvas.transform, false); Stretch(bondsSheet); bondsSheet.GetComponent<Image>().color = new Color(0.02f, 0.02f, 0.03f, 1f);
                var body = new GameObject("Body", typeof(RectTransform)); body.transform.SetParent(bondsSheet.transform, false); Stretch(body); bondsBody = body.transform;
            }
            if (curtain != null) bondsSheet.transform.SetSiblingIndex(curtain.transform.GetSiblingIndex());   // under the curtain, over the title
            bondsSheet.SetActive(true); bondsArmed = null; BuildBonds(null, float.NaN);
        }
        void CloseBonds() { bondsSheet.SetActive(false); RefreshRewardButtons(); RefreshGold(); if (pointsLine != null) Tick(pointsLine, Depot.Points); if (xpLine != null) Tick(xpLine, Depot.CrewXp, "", " XP"); }
        /// <summary>The sheet built again after a change, the tiers left where they were scrolled to.</summary>
        void RebuildBonds(string line) { BuildBonds(line, bondsScroll != null ? bondsScroll.content.anchoredPosition.x : float.NaN); }

        void BuildBonds(string line, float scrollX)
        {
            foreach (Transform c in bondsBody) Destroy(c.gameObject);
            var b = bondsBody; int tier = Bonds.Tier, stars = Bonds.Stars, season = Bonds.Season;
            { var glow = MakeImage(b, "Glow", new Vector2(0.5f, 1f), new Vector2(0f, 250f), new Vector2(1600f, 1100f), new Color(1f, 0.72f, 0.28f, 0.1f)); glow.sprite = Lightswarm.ProceduralSprites.Glow(64, 0.1f); }
            MakeGhost(b, "BACK", new Vector2(0f, 1f), new Vector2(130, -95), new Vector2(200, 80), 26, CloseBonds);
            var gp = GoldPill(b, new Vector2(-50f, -65f), 300f, false); gp.text = Depot.Gold.ToString("N0", En);
            var ey = MakeText(b, "Eyebrow", new Vector2(0.5f, 1f), new Vector2(0, -150), TextAnchor.MiddleCenter, 26, OpAmber); ey.text = Spaced("SEASON " + season + " · " + Bonds.SeasonName(season).ToUpperInvariant()); ey.font = BoldFont(); ey.rectTransform.sizeDelta = new Vector2(900, 40);
            var ti = MakeText(b, "Title", new Vector2(0.5f, 1f), new Vector2(0, -190), TextAnchor.MiddleCenter, 110, OpInk); ti.text = "WAR BONDS"; ti.font = DisplayFont(); ti.rectTransform.sizeDelta = new Vector2(900, 140); ti.verticalOverflow = VerticalWrapMode.Overflow;
            var ends = MakeText(b, "Ends", new Vector2(0.5f, 1f), new Vector2(0, -318), TextAnchor.MiddleCenter, 24, OpDim); ends.text = "The season ends in " + Bonds.Left + " · a tier is " + Bonds.StarsPerTier + " stars"; ends.rectTransform.sizeDelta = new Vector2(980, 40);

            // how far along
            {
                var card = MakeCard(b, "Progress", new Vector2(0.5f, 1f), new Vector2(0f, -370f), new Vector2(1000f, 140f), new Color(0.07f, 0.075f, 0.09f, 0.96f), 0.16f); card.rectTransform.pivot = new Vector2(0.5f, 1f); var t = card.transform;
                var tl = MakeText(t, "TierLabel", new Vector2(0f, 1f), new Vector2(40, -20), TextAnchor.UpperLeft, 20, OpDim); tl.text = Spaced("TIER"); tl.font = BoldFont(); tl.rectTransform.sizeDelta = new Vector2(160, 30);
                var tn = MakeText(t, "Tier", new Vector2(0f, 1f), new Vector2(38, -44), TextAnchor.UpperLeft, 80, GoldInk); tn.text = tier.ToString(); tn.font = DisplayFont(); tn.rectTransform.sizeDelta = new Vector2(160, 90); tn.verticalOverflow = VerticalWrapMode.Overflow;
                int into = tier >= Bonds.Tiers ? Bonds.StarsPerTier : stars % Bonds.StarsPerTier;
                var bl = MakeText(t, "BarLine", new Vector2(0f, 1f), new Vector2(200, -34), TextAnchor.UpperLeft, 24, OpInk); bl.rectTransform.sizeDelta = new Vector2(500, 36);
                bl.text = tier >= Bonds.Tiers ? "Every tier reached" : into + " / " + Bonds.StarsPerTier + " stars to tier " + (tier + 1);
                var track = MakeImage(t, "Track", new Vector2(0f, 1f), new Vector2(200, -86), new Vector2(480, 14), new Color(1f, 1f, 1f, 0.1f)); track.sprite = Rounded(); track.type = Image.Type.Sliced;
                var fill = MakeImage(track.transform, "Fill", new Vector2(0f, 0.5f), Vector2.zero, new Vector2(Mathf.Max(14f, 480f * into / Bonds.StarsPerTier), 14), GoldInk); fill.sprite = Rounded(); fill.type = Image.Type.Sliced;
                if (Bonds.Waiting > 0) MakePrimary(t, "Claim all", new Vector2(1f, 0.5f), new Vector2(-150f, 0f), new Vector2(250f, 90f), 30, () => { int n = Bonds.ClaimAll(); if (n > 0) Sfx.Pickup(); RebuildBonds(n > 0 ? n + (n == 1 ? " reward taken" : " rewards taken") : null); });
                else if (tier < Bonds.Tiers) GoldButtonAt(t, new Vector2(1f, 0.5f), new Vector2(-150f, 0f), new Vector2(250f, 100f), "+1 TIER", Bonds.TierGold, () => BondsConfirm("tier", Bonds.TierGold, "Tap again: tier " + (tier + 1) + " for " + Bonds.TierGold + " gold", Bonds.BuyTier, "Tier " + (tier + 1) + " reached"));
            }

            // the forty tiers: free above, gold below
            {
                var vp = new GameObject("Tiers", typeof(RectTransform), typeof(RectMask2D), typeof(Image), typeof(ScrollRect)); vp.transform.SetParent(b, false);
                var vr = vp.GetComponent<RectTransform>(); vr.anchorMin = vr.anchorMax = vr.pivot = new Vector2(0.5f, 1f); vr.anchoredPosition = new Vector2(0f, -530f); vr.sizeDelta = new Vector2(1040f, 640f);
                vp.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);   // something to drag on
                var content = new GameObject("Row", typeof(RectTransform)); content.transform.SetParent(vp.transform, false); var cr = content.GetComponent<RectTransform>();
                cr.anchorMin = cr.anchorMax = cr.pivot = new Vector2(0f, 1f); float width = 150f + Bonds.Tiers * TierW + 20f; cr.sizeDelta = new Vector2(width, 640f);
                var sr = vp.GetComponent<ScrollRect>(); sr.content = cr; sr.viewport = vr; sr.horizontal = true; sr.vertical = false; sr.movementType = ScrollRect.MovementType.Clamped; sr.inertia = true; sr.decelerationRate = 0.1f; sr.scrollSensitivity = 60f; bondsScroll = sr;
                var ct = content.transform;
                var fl = MakeText(ct, "Free", new Vector2(0f, 1f), new Vector2(20, -190), TextAnchor.MiddleLeft, 22, OpDim); fl.text = Spaced("FREE"); fl.font = BoldFont(); fl.rectTransform.sizeDelta = new Vector2(130, 40);
                var gl = MakeText(ct, "Gold", new Vector2(0f, 1f), new Vector2(20, -455), TextAnchor.MiddleLeft, 22, GoldInk); gl.text = Spaced("GOLD") + "\n" + Spaced("BOND"); gl.font = BoldFont(); gl.rectTransform.sizeDelta = new Vector2(130, 70);
                var line0 = MakeImage(ct, "Line", new Vector2(0f, 1f), new Vector2(150f + TierW * 0.5f, -31f), new Vector2((Bonds.Tiers - 1) * TierW, 3f), new Color(1f, 1f, 1f, 0.12f)); line0.rectTransform.pivot = new Vector2(0f, 0.5f);
                if (tier > 1) { var line1 = MakeImage(ct, "Reached", new Vector2(0f, 1f), new Vector2(150f + TierW * 0.5f, -31f), new Vector2((tier - 1) * TierW, 3f), GoldInk); line1.rectTransform.pivot = new Vector2(0f, 0.5f); }
                for (int n = 1; n <= Bonds.Tiers; n++) TierColumn(ct, n, tier);
                float want = float.IsNaN(scrollX) ? -(150f + (Mathf.Max(1, tier) - 1) * TierW - 240f) : scrollX;
                cr.anchoredPosition = new Vector2(Mathf.Clamp(want, -(width - 1040f), 0f), 0f);
            }

            // the gold bond
            {
                var card = MakeCard(b, "Bond", new Vector2(0.5f, 1f), new Vector2(0f, -1195f), new Vector2(1000f, 140f), new Color(0.1f, 0.08f, 0.05f, 0.96f), 0.2f); card.rectTransform.pivot = new Vector2(0.5f, 1f); var t = card.transform;
                card.transform.Find("Edge").GetComponent<Image>().color = new Color(1f, 0.8f, 0.35f, Bonds.GoldBond ? 0.9f : 0.5f);
                var bt = MakeText(t, "Name", new Vector2(0f, 1f), new Vector2(34, -18), TextAnchor.UpperLeft, 44, GoldInk); bt.text = Bonds.GoldBond ? "GOLD BOND · ACTIVE" : "GOLD BOND"; bt.font = DisplayFont(); bt.rectTransform.sizeDelta = new Vector2(640, 56);
                var bd = MakeText(t, "Line", new Vector2(0f, 1f), new Vector2(34, -76), TextAnchor.UpperLeft, 22, OpDim); bd.rectTransform.sizeDelta = new Vector2(640, 56);
                bd.text = Bonds.GoldBond ? "Every gold reward of the season is yours, and a star more every night." : "Every gold reward of the season, and a star more every night.";
                if (!Bonds.GoldBond) GoldButtonAt(t, new Vector2(1f, 0.5f), new Vector2(-150f, 0f), new Vector2(250f, 100f), "UNLOCK", Bonds.BondGold, () => BondsConfirm("bond", Bonds.BondGold, "Tap again: the gold bond for " + Bonds.BondGold + " gold", Bonds.BuyBond, "Gold bond · the gold rewards are yours"));
                else if (tier < Bonds.Tiers && Bonds.Waiting > 0) GoldButtonAt(t, new Vector2(1f, 0.5f), new Vector2(-150f, 0f), new Vector2(250f, 100f), "+1 TIER", Bonds.TierGold, () => BondsConfirm("tier", Bonds.TierGold, "Tap again: tier " + (tier + 1) + " for " + Bonds.TierGold + " gold", Bonds.BuyTier, "Tier " + (tier + 1) + " reached"));
            }

            // the week's orders
            {
                var wh = MakeText(b, "Weekly", new Vector2(0.5f, 1f), new Vector2(0, -1360), TextAnchor.MiddleCenter, 24, OpDim); wh.text = Spaced("WEEKLY ORDERS") + "  ·  new orders in " + Bonds.WeekLeft; wh.font = BoldFont(); wh.rectTransform.sizeDelta = new Vector2(1000, 40);
                var orders = Bonds.WeekOrders;
                for (int i = 0; i < orders.Length; i++)
                {
                    var o = orders[i]; int p = Bonds.Progress(o); bool done = p >= o.need, taken = Bonds.OrderClaimed(i);
                    var row = MakeCard(b, "Order", new Vector2(0.5f, 1f), new Vector2(0f, -1412f - i * 74f), new Vector2(1000f, 66f), new Color(0.07f, 0.075f, 0.09f, 0.94f), done ? 0.35f : 0.12f); row.rectTransform.pivot = new Vector2(0.5f, 1f); var t = row.transform;
                    var tx = MakeText(t, "Text", new Vector2(0f, 0.5f), new Vector2(26, 0), TextAnchor.MiddleLeft, 24, taken ? OpDim : OpInk); tx.text = string.Format(o.text, o.need); tx.rectTransform.pivot = new Vector2(0f, 0.5f); tx.rectTransform.sizeDelta = new Vector2(600, 60);
                    var st = MakeImage(t, "Star", new Vector2(1f, 0.5f), new Vector2(-26, 0), new Vector2(30, 30), GoldInk); st.sprite = StarSprite(); st.rectTransform.pivot = new Vector2(1f, 0.5f);
                    var sv = MakeText(t, "Stars", new Vector2(1f, 0.5f), new Vector2(-62, 0), TextAnchor.MiddleRight, 24, GoldInk); sv.text = "+" + o.stars; sv.font = BoldFont(); sv.rectTransform.pivot = new Vector2(1f, 0.5f); sv.rectTransform.sizeDelta = new Vector2(60, 60);
                    if (done && !taken) { MakePrimary(t, "Claim", new Vector2(1f, 0.5f), new Vector2(-210f, 0f), new Vector2(150f, 52f), 22, () => { var got = Bonds.ClaimOrders(); if (got.Count > 0) Sfx.Pickup(); RebuildBonds(got.Count > 0 ? got[0] : null); }); }
                    else { var pv = MakeText(t, "Progress", new Vector2(1f, 0.5f), new Vector2(-140, 0), TextAnchor.MiddleRight, 24, taken ? new Color(0.45f, 0.85f, 0.5f) : OpDim); pv.text = taken ? "DONE" : p + " / " + o.need; pv.font = BoldFont(); pv.rectTransform.pivot = new Vector2(1f, 0.5f); pv.rectTransform.sizeDelta = new Vector2(200, 60); }
                }
            }
            bondsNote = MakeText(b, "Note", new Vector2(0.5f, 1f), new Vector2(0, -1790), TextAnchor.MiddleCenter, 28, OpAmber); bondsNote.rectTransform.sizeDelta = new Vector2(1000, 50); bondsNote.font = BoldFont(); bondsNote.text = line ?? "";
        }

        /// <summary>One tier: its number on the line, the free reward, the gold reward, and what can be done with each.</summary>
        void TierColumn(Transform row, int n, int tier)
        {
            float x = 150f + (n - 1) * TierW; bool reached = n <= tier;
            var dot = MakeImage(row, "Dot", new Vector2(0f, 1f), new Vector2(x + TierW * 0.5f, -31f), new Vector2(54, 54), reached ? GoldInk : new Color(0.24f, 0.24f, 0.27f)); dot.sprite = Lightswarm.ProceduralSprites.Glow(64, 0.92f); dot.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var dn = MakeText(dot.transform, "N", new Vector2(0.5f, 0.5f), new Vector2(0, 1), TextAnchor.MiddleCenter, 24, reached ? new Color(0.12f, 0.09f, 0.04f) : OpDim); dn.text = n.ToString(); dn.font = BoldFont(); dn.rectTransform.sizeDelta = new Vector2(54, 54); dn.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            for (int g = 0; g < 2; g++)
            {
                bool gold = g == 1; var l = gold ? Bonds.GoldReward(n) : Bonds.FreeReward(n); bool taken = Bonds.Claimed(n, gold);
                var card = MakeCard(row, gold ? "GoldTier" : "FreeTier", new Vector2(0f, 1f), new Vector2(x + 10f, gold ? -335f : -70f), new Vector2(TierW - 20f, 250f), gold ? new Color(0.1f, 0.08f, 0.05f, 0.97f) : new Color(0.07f, 0.075f, 0.09f, 0.97f), 0.14f); card.rectTransform.pivot = new Vector2(0f, 1f);
                if (gold) card.transform.Find("Edge").GetComponent<Image>().color = new Color(1f, 0.8f, 0.35f, reached ? 0.8f : 0.35f);
                var t = card.transform;
                RewardIcon(t, new Vector2(0f, 42f), 76f, l.kind, l.id);
                var am = MakeText(t, "Amount", new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), TextAnchor.MiddleCenter, 24, gold ? GoldInk : OpInk); am.font = BoldFont(); am.rectTransform.sizeDelta = new Vector2(TierW - 30f, 56f); am.resizeTextForBestFit = true; am.resizeTextMinSize = 14; am.resizeTextMaxSize = 24;
                am.text = RewardLine(l.kind, l.amount, l.id) + (gold && n == Bonds.Tiers ? " + 100 gold" : "");
                Vector2 btnPos = new Vector2(0f, 30f), btnSize = new Vector2(TierW - 40f, 50f);
                if (taken) { var tk = MakeText(t, "Taken", new Vector2(0.5f, 0f), btnPos, TextAnchor.MiddleCenter, 20, new Color(0.45f, 0.85f, 0.5f)); tk.text = Spaced("TAKEN"); tk.font = BoldFont(); tk.rectTransform.sizeDelta = btnSize; tk.rectTransform.pivot = new Vector2(0.5f, 0.5f); var sh = MakeImage(t, "Shade", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(TierW - 20f, 250f), new Color(0.02f, 0.02f, 0.03f, 0.45f)); sh.sprite = Rounded(); sh.type = Image.Type.Sliced; sh.rectTransform.pivot = new Vector2(0.5f, 0.5f); continue; }
                if (!reached) { var lk = MakeText(t, "Locked", new Vector2(0.5f, 0f), btnPos, TextAnchor.MiddleCenter, 18, OpDim); lk.text = Spaced(gold && !Bonds.GoldBond ? "GOLD BOND" : "TIER " + n); lk.font = BoldFont(); lk.rectTransform.sizeDelta = btnSize; lk.rectTransform.pivot = new Vector2(0.5f, 0.5f); continue; }
                int tn = n;
                if (!gold || Bonds.GoldBond) { MakePrimary(t, "Claim", new Vector2(0.5f, 0f), btnPos, btnSize, 22, () => { if (Bonds.Claim(tn, gold)) Sfx.Pickup(); RebuildBonds(RewardLine(l.kind, l.amount, l.id) + (l.kind == "supply" || l.kind == "officer" ? " · open it on the title" : "")); }); continue; }
                if (Bonds.AdClaimable(l)) { var ad = MakeButton(t, "Watch ad", new Vector2(0.5f, 0f), btnPos, btnSize, 22, () => Ads.Rewarded("bonds", () => { if (Bonds.Claim(tn, true, true)) Sfx.Pickup(); RebuildBonds(RewardLine(l.kind, l.amount, l.id)); })); ad.GetComponent<Image>().color = XpDeep; continue; }
                var need = MakeText(t, "Bond", new Vector2(0.5f, 0f), btnPos, TextAnchor.MiddleCenter, 18, GoldInk); need.text = Spaced("GOLD BOND"); need.font = BoldFont(); need.rectTransform.sizeDelta = btnSize; need.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            }
        }

        /// <summary>A gold-priced button anchored where it is asked (the shop's GoldButton hangs from the top centre).</summary>
        void GoldButtonAt(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, string what, int gold, System.Action onClick)
        {
            var holder = new GameObject("At", typeof(RectTransform)); holder.transform.SetParent(parent, false); var hr = holder.GetComponent<RectTransform>(); hr.anchorMin = hr.anchorMax = anchor; hr.pivot = new Vector2(0.5f, 0.5f); hr.anchoredPosition = pos; hr.sizeDelta = size;
            GoldButton(holder.transform, Vector2.zero, size, what, gold, onClick);
        }

        /// <summary>Gold is only spent on a second tap, here as in the shop.</summary>
        void BondsConfirm(string key, int gold, string ask, System.Func<bool> spend, string done)
        {
            if (Depot.Gold < gold) { bondsArmed = null; bondsNote.text = "Not enough gold · " + gold.ToString("N0", En) + " needed · the shop is on the title"; return; }
            if (bondsArmed != key || Time.unscaledTime - bondsArmedAt > 4f) { bondsArmed = key; bondsArmedAt = Time.unscaledTime; bondsNote.text = ask; return; }
            bondsArmed = null; if (spend()) { Sfx.Pickup(); RefreshGold(); RebuildBonds(done); }
        }
    }
}

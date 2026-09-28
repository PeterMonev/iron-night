using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IronNight
{
    /// <summary>
    /// The rewards on the screen: mail call (the calendar of gifts), the quartermaster's card, the crate being opened on
    /// its stage and the three rewards that come out of it, and the crate button on the title. One sheet serves them
    /// all, rebuilt for each; the first time the title shows in a session it says what came in while the player was away.
    /// </summary>
    public partial class Hud
    {
        GameObject rewardSheet; Transform rewardBody; static bool welcomed;
        // the crate button on the title
        GameObject crateBtn; Image cratePic; Text crateCount, crateLabel; GameObject crateBadge;
        // the crate being opened: its stage, its line, its rewards and when each comes in
        CrateStage stage; string openKind; List<Rewards.Loot> opened; Text crateTap; RectTransform[] lootCards; bool lootDoubled; GameObject lootButtons; Image burstFlash;

        static readonly Color QmGreen = new Color(0.16f, 0.36f, 0.22f, 0.92f);
        static Sprite vignette;
        /// <summary>Clear in the middle, black towards a square's edges and corners: a picture fading into the page.</summary>
        static Sprite Vignette()
        {
            if (vignette != null) return vignette;
            const int N = 128; var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) { float u = Mathf.Abs((x + 0.5f) / N * 2f - 1f), v = Mathf.Abs((y + 0.5f) / N * 2f - 1f), d = Mathf.Pow(Mathf.Pow(u, 4f) + Mathf.Pow(v, 4f), 0.25f); tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, (d - 0.6f) / 0.38f))); }
            tex.Apply(); return vignette = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
        }
        static Color CamoColor(string id) => id == "winter" ? new Color(0.86f, 0.88f, 0.9f) : id == "desert" ? new Color(0.78f, 0.66f, 0.44f) : id == "night" ? new Color(0.22f, 0.26f, 0.35f) : id == "hedgerow" ? new Color(0.3f, 0.4f, 0.18f) : id == "whitewash" ? new Color(0.94f, 0.95f, 0.96f) : id == "steppe" ? new Color(0.72f, 0.58f, 0.32f) : new Color(0.34f, 0.37f, 0.22f);

        /// <summary>The sheet, fresh: a dark backdrop over everything but the curtain, and an empty body.</summary>
        Transform RewardSheet(float dim)
        {
            if (rewardSheet == null)
            {
                rewardSheet = new GameObject("Rewards", typeof(RectTransform), typeof(Image)); rewardSheet.transform.SetParent(canvas.transform, false); Stretch(rewardSheet);
                var body = new GameObject("Body", typeof(RectTransform)); body.transform.SetParent(rewardSheet.transform, false); Stretch(body); rewardBody = body.transform;
            }
            if (curtain != null) rewardSheet.transform.SetSiblingIndex(curtain.transform.GetSiblingIndex());   // under the curtain, over everything else
            rewardSheet.GetComponent<Image>().color = new Color(0.01f, 0.01f, 0.015f, dim);
            foreach (Transform c in rewardBody) Destroy(c.gameObject);
            rewardSheet.SetActive(true); return rewardBody;
        }
        void CloseRewards() { if (stage != null) stage.Stop(); opened = null; if (rewardSheet != null) rewardSheet.SetActive(false); RefreshRewardButtons(); RefreshGold(); if (pointsLine != null) Tick(pointsLine, Depot.Points); if (xpLine != null) Tick(xpLine, Depot.CrewXp, "", " XP"); }

        /// <summary>The first time the title shows in a session: today's mail, then what the quartermaster gathered.</summary>
        public void WelcomeBack()
        {
            if (welcomed) return; welcomed = true;
            CrateStage.Picture("supply"); CrateStage.Picture("officer");   // the pictures first, before a crate is ever on the stage
            RefreshRewardButtons();
            if (Rewards.MailReady) ShowMail(); else if (Rewards.QmHours >= 0.5f) ShowQuartermaster();
        }

        // test switches, each opening its screen straight away: --mail, --qm (five hours gathered), --opencrate (a crate, tapped by itself)
        public void TestMail() { welcomed = true; ShowMail(); }
        public void TestQuartermaster() { welcomed = true; PlayerPrefs.SetString("qm.since", GameClock.UtcNow.AddHours(-5).Ticks.ToString()); ShowQuartermaster(); }
        /// <summary>Test switch --opencrate: the next crate, tapped open by itself; --opencrate=supply or =officer: that
        /// kind, left waiting for the tap.</summary>
        public void TestCrate(string kind = null)
        {
            welcomed = true;
            if (kind != null) { if (Rewards.Crates(kind) == 0) Rewards.AddCrates(kind, 1); ShowCrate(kind); return; }
            if (Rewards.CratesTotal == 0) Rewards.AddCrates("supply", 1); autoTap = true; ShowCrate(Rewards.NextCrate);
        }
        bool autoTap;

        // ---- the title's buttons: the quartermaster and the crate ----
        /// <summary>The quartermaster's line and the crate button, as things stand.</summary>
        void RefreshRewardButtons()
        {
            if (dailyBtn != null)
            {
                int p = Rewards.QmPoints; var lb = dailyBtn.transform.Find("Label").GetComponent<Text>();
                lb.text = p >= 50 ? "Quartermaster · +" + p.ToString("N0", En) + " ready" : "Quartermaster · gathering";
                dailyBtn.GetComponent<Image>().color = p >= 50 ? QmGreen : new Color(0.06f, 0.07f, 0.09f, 0.55f); dailyBtn.SetActive(true);
            }
            RefreshBondsButton(); RefreshWeeklyButton(); RefreshTestDriveButton(); RefreshDispatchButton(); RefreshLetterPill();
            if (crateBtn == null) return;
            int n = Rewards.CratesTotal; bool free = n == 0 && Rewards.FreeCrateReady;
            var sp = CrateStage.Picture(n > 0 ? Rewards.NextCrate : "supply"); if (sp != null) cratePic.sprite = sp;
            cratePic.color = n > 0 || free ? Color.white : new Color(0.45f, 0.45f, 0.47f, 0.8f);
            crateBadge.SetActive(n > 0); crateCount.text = n.ToString();
            if (n > 0) crateLabel.text = n == 1 ? "OPEN CRATE" : "OPEN CRATES";
            else if (free) crateLabel.text = "FREE CRATE";
            else { float h = Rewards.FreeCrateWait; crateLabel.text = h >= 1f ? "free in " + Mathf.FloorToInt(h) + " h " + Mathf.FloorToInt((h % 1f) * 60f) + " min" : "free in " + Mathf.Max(1, Mathf.CeilToInt(h * 60f)) + " min"; }
        }

        /// <summary>The crate button under the rank, top left of the title: the crate, how many are waiting, what a tap does.</summary>
        void BuildCrateButton()
        {
            var b = MakeButton(titleSheet.transform, "", new Vector2(0f, 1f), new Vector2(135, -275), new Vector2(190, 190), 20, OnCrateButton); crateBtn = b;
            b.GetComponent<Image>().color = new Color(0.05f, 0.055f, 0.07f, 0.7f); b.transform.Find("Label").gameObject.SetActive(false);
            var edge = MakeImage(b.transform, "Edge", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(190, 190), new Color(1f, 0.8f, 0.35f, 0.35f)); edge.sprite = Outline(); edge.type = Image.Type.Sliced; edge.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var glow = MakeImage(b.transform, "Glow", new Vector2(0.5f, 0.5f), new Vector2(0, 14), new Vector2(200, 170), new Color(1f, 0.72f, 0.3f, 0.22f)); glow.sprite = Lightswarm.ProceduralSprites.Glow(64, 0.1f); glow.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            cratePic = MakeImage(b.transform, "Crate", new Vector2(0.5f, 0.5f), new Vector2(0, 14), new Vector2(160, 160), Color.white); cratePic.rectTransform.pivot = new Vector2(0.5f, 0.5f); cratePic.preserveAspect = true;
            crateLabel = MakeText(b.transform, "Label2", new Vector2(0.5f, 0f), new Vector2(0, 8), TextAnchor.LowerCenter, 19, OpAmber); crateLabel.font = BoldFont(); crateLabel.rectTransform.sizeDelta = new Vector2(184, 30);
            var badge = MakeImage(b.transform, "Badge", new Vector2(1f, 1f), new Vector2(10, 10), new Vector2(52, 52), new Color(0.86f, 0.22f, 0.16f)); badge.sprite = Lightswarm.ProceduralSprites.Glow(64, 0.9f); badge.rectTransform.pivot = new Vector2(1f, 1f); crateBadge = badge.gameObject;
            crateCount = MakeText(badge.transform, "N", new Vector2(0.5f, 0.5f), new Vector2(0, 1), TextAnchor.MiddleCenter, 28, Color.white); crateCount.font = BoldFont(); crateCount.rectTransform.sizeDelta = new Vector2(52, 52); crateCount.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }
        void OnCrateButton()
        {
            if (Rewards.CratesTotal > 0) { ShowCrate(Rewards.NextCrate); return; }
            if (Rewards.FreeCrateReady) Ads.Rewarded("crate", () => { if (!Rewards.FreeCrateReady) return; Rewards.FreeCrateTaken(); ShowCrate("supply"); });
        }

        /// <summary>A reward's picture: the coin for points, the star for crew experience, a bar of gold, a crate, a swatch.</summary>
        void RewardIcon(Transform parent, Vector2 center, float size, string kind, string id)
        {
            if (kind == "points")
            {
                var ring = MakeImage(parent, "Coin", new Vector2(0.5f, 0.5f), center, new Vector2(size, size), OpAmber); ring.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                if (!Currency(ring, "icon_points", Lightswarm.ProceduralSprites.Ring(64, 0.16f))) { var core = MakeImage(parent, "Core", new Vector2(0.5f, 0.5f), center, new Vector2(size * 0.4f, size * 0.4f), OpAmber); core.sprite = Lightswarm.ProceduralSprites.Glow(32, 0.7f); core.rectTransform.pivot = new Vector2(0.5f, 0.5f); }
            }
            else if (kind == "xp" || kind == "premium") { var st = MakeImage(parent, "Star", new Vector2(0.5f, 0.5f), center, new Vector2(size, size), kind == "xp" ? XpBlue : GoldInk); if (kind != "xp" || !Currency(st, "icon_crewxp", StarSprite())) st.sprite = StarSprite(); st.rectTransform.pivot = new Vector2(0.5f, 0.5f); }
            else if (kind == "gold") { var g = MakeImage(parent, "Gold", new Vector2(0.5f, 0.5f), center, new Vector2(size * 1.3f, size * 0.87f), Color.white); g.sprite = GoldSprite(); g.preserveAspect = true; g.rectTransform.pivot = new Vector2(0.5f, 0.5f); }
            else if (kind == "camo") { var c = MakeImage(parent, "Swatch", new Vector2(0.5f, 0.5f), center, new Vector2(size, size), CamoColor(id)); c.sprite = Rounded(); c.type = Image.Type.Sliced; c.rectTransform.pivot = new Vector2(0.5f, 0.5f); }
            else { var c = MakeImage(parent, "Crate", new Vector2(0.5f, 0.5f), center, new Vector2(size * 1.25f, size * 1.25f), Color.white); c.sprite = CrateStage.Picture(kind); c.preserveAspect = true; c.rectTransform.pivot = new Vector2(0.5f, 0.5f); }
        }
        static string RewardLine(string kind, int amount, string id)
        {
            var en = System.Globalization.CultureInfo.InvariantCulture;
            switch (kind)
            {
                case "points": return "+" + amount.ToString("N0", en);
                case "xp": return "+" + amount.ToString("N0", en) + " XP";
                case "gold": return "+" + amount + " gold";
                case "premium": return amount + (amount == 1 ? " day premium" : " days premium");
                case "camo": { var c = System.Array.Find(Depot.Camos, x => x.id == id); return (c != null ? c.name : "Camouflage") + " camo"; }
                case "officer": return amount > 1 ? amount + " officer's crates" : "Officer's crate";
                default: return amount > 1 ? amount + " supply crates" : "Supply crate";
            }
        }

        // ---- mail call ----
        /// <summary>The week of gifts: the ones taken, today's to take (twice over for an ad), the ones to come.</summary>
        void ShowMail(Rewards.Gift justTaken = null)
        {
            var b = RewardSheet(1f); int done = Rewards.MailDone; bool ready = Rewards.MailReady;
            { var glow = MakeImage(b, "Glow", new Vector2(0.5f, 1f), new Vector2(0f, 200f), new Vector2(1600f, 1100f), new Color(1f, 0.72f, 0.28f, 0.09f)); glow.sprite = Lightswarm.ProceduralSprites.Glow(64, 0.1f); }
            var ey = MakeText(b, "Eyebrow", new Vector2(0.5f, 1f), new Vector2(0, -250), TextAnchor.MiddleCenter, 26, OpAmber); ey.text = Spaced("DAILY REWARDS · DAY " + Mathf.Min(7, done + (ready ? 1 : 0)) + " OF 7"); ey.font = LabelFont(); ey.rectTransform.sizeDelta = new Vector2(800, 40);
            var ti = MakeText(b, "Title", new Vector2(0.5f, 1f), new Vector2(0, -290), TextAnchor.MiddleCenter, 110, OpInk); ti.text = "MAIL CALL"; Engrave(ti); ti.rectTransform.sizeDelta = new Vector2(900, 140); ti.verticalOverflow = VerticalWrapMode.Overflow;
            var ln = MakeText(b, "Line", new Vector2(0.5f, 1f), new Vector2(0, -432), TextAnchor.MiddleCenter, 24, OpDim); ln.text = "A parcel from home every day. Miss a day and the row starts again."; ln.rectTransform.sizeDelta = new Vector2(980, 40);
            for (int i = 0; i < 7; i++)
            {
                var g = Rewards.Week[i]; bool taken = i < done, today = ready ? i == done : i == done - 1, big = i == 6;
                Vector2 size = big ? new Vector2(960f, 250f) : new Vector2(300f, 300f); Vector2 top = big ? new Vector2(0f, -1150f) : new Vector2((i % 3 - 1) * 330f, i < 3 ? -500f : -825f);
                var card = MakeCard(b, "Day", new Vector2(0.5f, 1f), top, size, today ? new Color(0.13f, 0.1f, 0.06f, 0.98f) : new Color(0.07f, 0.075f, 0.09f, 0.96f), 0.16f); card.rectTransform.pivot = new Vector2(0.5f, 1f);
                if (today) card.transform.Find("Edge").GetComponent<Image>().color = new Color(1f, 0.78f, 0.35f, 0.95f);
                var t = card.transform;
                var dl = MakeText(t, "Day", new Vector2(0.5f, 1f), new Vector2(0, -16), TextAnchor.UpperCenter, 22, today ? OpAmber : OpDim); dl.text = Spaced(today && !taken ? "TODAY" : "DAY " + (i + 1)); dl.font = LabelFont(); dl.rectTransform.sizeDelta = new Vector2(size.x, 34);
                RewardIcon(t, big ? new Vector2(-260f, -6f) : new Vector2(0f, 12f), big ? 150f : 110f, g.kind, null);
                var am = MakeText(t, "Amount", new Vector2(0.5f, 0.5f), big ? new Vector2(150f, -6f) : new Vector2(0f, -100f), TextAnchor.MiddleCenter, big ? 44 : 28, today ? OpInk : new Color(0.85f, 0.83f, 0.78f)); am.text = RewardLine(g.kind, g.amount, null); am.font = BoldFont(); am.rectTransform.sizeDelta = new Vector2(big ? 560f : 290f, 60f);
                if (taken && !(today && justTaken != null)) { var shade = MakeImage(t, "Taken", new Vector2(0.5f, 0.5f), Vector2.zero, size, new Color(0.02f, 0.02f, 0.03f, 0.62f)); shade.sprite = Rounded(); shade.type = Image.Type.Sliced; shade.rectTransform.pivot = new Vector2(0.5f, 0.5f); var green = new Color(0.45f, 0.85f, 0.5f, 0.92f);
                    var st = MakeImage(t, "Stamp", new Vector2(0.5f, 0.5f), big ? new Vector2(-260f, -6f) : new Vector2(0f, 12f), new Vector2(236f, 66f), new Color(0.03f, 0.06f, 0.04f, 0.88f)); st.sprite = Rounded(); st.type = Image.Type.Sliced; st.rectTransform.pivot = new Vector2(0.5f, 0.5f); st.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -8f);
                    var se = MakeImage(st.transform, "Edge", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(236f, 66f), green); se.sprite = Outline(); se.type = Image.Type.Sliced; se.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    var tk = MakeText(st.transform, "T", new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleCenter, 30, green); tk.text = Spaced("COLLECTED"); tk.font = LabelBoldFont(); tk.rectTransform.sizeDelta = new Vector2(236f, 50f); }
                if (today && justTaken != null) card.gameObject.AddComponent<Pop>();
            }
            if (ready)
            {
                MakePrimary(b, "Collect", new Vector2(0.5f, 1f), new Vector2(-245f, -1500f), new Vector2(450f, 120f), 38, () => { var g = Rewards.MailCollect(1); if (g != null) { Sfx.Pickup(); ShowMail(g); } });
                var ad = MakeButton(b, "×2 · watch an ad", new Vector2(0.5f, 1f), new Vector2(245f, -1500f), new Vector2(450f, 120f), 34, () => Ads.Rewarded("mail", () => { var g = Rewards.MailCollect(2); if (g != null) { Sfx.Pickup(); ShowMail(g); } }));
                ad.GetComponent<Image>().color = XpDeep;
            }
            else
            {
                bool crate = justTaken != null && (justTaken.kind == "supply" || justTaken.kind == "officer");
                if (crate) MakePrimary(b, "Open it now", new Vector2(0.5f, 1f), new Vector2(-245f, -1500f), new Vector2(450f, 120f), 38, () => ShowCrate(justTaken.kind));
                MakeGhost(b, justTaken != null ? "Done" : "Close", new Vector2(0.5f, 1f), new Vector2(crate ? 245f : 0f, -1500f), new Vector2(450f, 120f), 34, () => { CloseRewards(); if (Rewards.QmHours >= 0.5f && justTaken != null) ShowQuartermaster(); });
                var nx = MakeText(b, "Next", new Vector2(0.5f, 1f), new Vector2(0, -1600), TextAnchor.MiddleCenter, 24, OpDim); nx.text = "The next parcel comes tomorrow."; nx.rectTransform.sizeDelta = new Vector2(900, 40);
            }
        }

        // ---- the quartermaster ----
        /// <summary>What the quartermaster gathered while the player was away: take it, or three times as much for an ad.</summary>
        void ShowQuartermaster()
        {
            var b = RewardSheet(0.93f); float h = Rewards.QmHours; int p = Rewards.QmPoints, x = Rewards.QmXp;
            var card = MakeCard(b, "Card", new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(960f, 1120f), new Color(0.06f, 0.065f, 0.08f, 0.98f), 0.2f); card.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            card.transform.Find("Edge").GetComponent<Image>().color = new Color(1f, 0.78f, 0.35f, 0.5f);
            var t = card.transform;
            { var glow = MakeImage(t, "Glow", new Vector2(0.5f, 1f), new Vector2(0f, 40f), new Vector2(760f, 520f), new Color(1f, 0.72f, 0.3f, 0.18f)); glow.sprite = Lightswarm.ProceduralSprites.Glow(64, 0.1f); }
            var pic = MakeImage(t, "Crate", new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(280f, 280f), Color.white); pic.sprite = CrateStage.Picture("supply"); pic.preserveAspect = true; pic.enabled = pic.sprite != null;
            var ey = MakeText(t, "Eyebrow", new Vector2(0.5f, 1f), new Vector2(0, -320), TextAnchor.MiddleCenter, 26, OpAmber); ey.text = Spaced("QUARTERMASTER"); ey.font = LabelFont(); ey.rectTransform.sizeDelta = new Vector2(800, 40);
            var ti = MakeText(t, "Title", new Vector2(0.5f, 1f), new Vector2(0, -360), TextAnchor.MiddleCenter, 84, OpInk); ti.text = p >= 50 ? "SUPPLIES ARE IN" : "STILL GATHERING"; Engrave(ti); ti.rectTransform.sizeDelta = new Vector2(900, 110); ti.verticalOverflow = VerticalWrapMode.Overflow;
            int hh = Mathf.FloorToInt(h), mm = Mathf.FloorToInt((h - hh) * 60f);
            var ln = MakeText(t, "Line", new Vector2(0.5f, 1f), new Vector2(0, -470), TextAnchor.MiddleCenter, 24, OpDim); ln.rectTransform.sizeDelta = new Vector2(900, 40);
            ln.text = (hh > 0 ? hh + " h " : "") + mm + " min of gathering · he holds up to " + (int)Rewards.QmCapHours + " hours" + (Depot.Premium ? " · premium +50%" : "");
            RewardIcon(t, new Vector2(-250f, -30f), 90f, "points", null);
            var pv = MakeText(t, "Points", new Vector2(0.5f, 0.5f), new Vector2(-80f, -30f), TextAnchor.MiddleLeft, 64, OpAmber); pv.text = "+" + p.ToString("N0", En); Serif(pv); pv.rectTransform.sizeDelta = new Vector2(420f, 90f); pv.rectTransform.pivot = new Vector2(0f, 0.5f);
            RewardIcon(t, new Vector2(-250f, -140f), 90f, "xp", null);
            var xv = MakeText(t, "Xp", new Vector2(0.5f, 0.5f), new Vector2(-80f, -140f), TextAnchor.MiddleLeft, 64, XpBlue); xv.text = "+" + x.ToString("N0", En) + " XP"; Serif(xv); xv.rectTransform.sizeDelta = new Vector2(420f, 90f); xv.rectTransform.pivot = new Vector2(0f, 0.5f);
            if (p >= 50)
            {
                MakePrimary(t, "Collect", new Vector2(0.5f, 0f), new Vector2(-222f, 230f), new Vector2(420f, 116f), 36, () => { Rewards.QmCollect(1); Sfx.Pickup(); CloseRewards(); });
                var ad = MakeButton(t, "×" + Rewards.QmAdMul + " · watch an ad", new Vector2(0.5f, 0f), new Vector2(222f, 230f), new Vector2(420f, 116f), 32, () => Ads.Rewarded("quartermaster", () => { Rewards.QmCollect(Rewards.QmAdMul); Sfx.Pickup(); CloseRewards(); }));
                ad.GetComponent<Image>().color = XpDeep;
            }
            MakeGhost(t, p >= 50 ? "Later" : "Close", new Vector2(0.5f, 0f), new Vector2(0f, 104f), new Vector2(300f, 80f), 28, CloseRewards);
        }

        // ---- the crate ----
        /// <summary>A crate on its stage: tap it, it bursts, three rewards come out one after another.</summary>
        void ShowCrate(string kind)
        {
            if (kind == null || Rewards.Crates(kind) <= 0) { CloseRewards(); return; }
            var b = RewardSheet(1f); openKind = kind; opened = null; lootDoubled = false; lootCards = null;
            if (stage == null) stage = CrateStage.Get();
            { var glow = MakeImage(b, "Glow", new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(1500f, 1300f), new Color(1f, 0.72f, 0.3f, 0.12f)); glow.sprite = Lightswarm.ProceduralSprites.Glow(64, 0.1f); }
            var view = new GameObject("Stage", typeof(RectTransform), typeof(RawImage)); view.transform.SetParent(b, false); var vr = view.GetComponent<RectTransform>(); vr.anchorMin = vr.anchorMax = vr.pivot = new Vector2(0.5f, 1f); vr.anchoredPosition = new Vector2(0f, -250f); vr.sizeDelta = new Vector2(1000f, 1000f);
            var raw = view.GetComponent<RawImage>(); raw.texture = stage.Texture; raw.raycastTarget = true;
            { var vig = MakeImage(view.transform, "Vignette", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1004f, 1004f), new Color(0f, 0f, 0f, 1f)); vig.sprite = Vignette(); vig.rectTransform.pivot = new Vector2(0.5f, 0.5f); }   // the stage fades into the black at its edges
            { var vb = view.AddComponent<Button>(); vb.transition = Selectable.Transition.None; vb.onClick.AddListener(() => stage.Tap()); }   // a tap on the crate opens it
            var ey = MakeText(b, "Eyebrow", new Vector2(0.5f, 1f), new Vector2(0, -120), TextAnchor.MiddleCenter, 26, OpAmber); ey.font = BoldFont(); ey.rectTransform.sizeDelta = new Vector2(900, 40);
            int left = Rewards.CratesTotal; ey.text = Spaced(left > 1 ? left + " CRATES WAITING" : "ONE CRATE WAITING");
            var ti = MakeText(b, "Title", new Vector2(0.5f, 1f), new Vector2(0, -160), TextAnchor.MiddleCenter, 96, OpInk); ti.text = Rewards.CrateName(kind).ToUpperInvariant(); Engrave(ti); ti.rectTransform.sizeDelta = new Vector2(1000, 120); ti.verticalOverflow = VerticalWrapMode.Overflow;
            crateTap = MakeText(b, "Tap", new Vector2(0.5f, 1f), new Vector2(0, -1250), TextAnchor.MiddleCenter, 34, OpAmber); crateTap.text = Spaced("TAP THE CRATE"); crateTap.font = LabelFont(); crateTap.rectTransform.sizeDelta = new Vector2(900, 50);
            var odds = MakeText(b, "Odds", new Vector2(0.5f, 0f), new Vector2(0, 40), TextAnchor.LowerCenter, 19, OpDim); odds.text = "Inside: " + Rewards.Odds(kind) + ". Crates are never sold."; odds.rectTransform.sizeDelta = new Vector2(960, 80);
            burstFlash = MakeImage(b, "Flash", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000f, 4000f), new Color(1f, 0.93f, 0.78f, 0f)); burstFlash.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            stage.Play(kind);
        }

        /// <summary>The three rewards, each on its card; they come in one by one, then the buttons.</summary>
        void BuildLoot()
        {
            lootCards = new RectTransform[opened.Count];
            for (int i = 0; i < opened.Count; i++)
            {
                var l = opened[i];
                var card = MakeCard(rewardBody, "Loot", new Vector2(0.5f, 1f), new Vector2((i - 1) * 330f, -1080f), new Vector2(310f, 380f), new Color(0.08f, 0.075f, 0.07f, 0.97f), 0.2f); card.rectTransform.pivot = new Vector2(0.5f, 1f);
                bool rare = l.kind == "gold" || l.kind == "premium" || l.kind == "camo"; if (rare) card.transform.Find("Edge").GetComponent<Image>().color = new Color(1f, 0.8f, 0.35f, 0.9f);
                var t = card.transform;
                if (rare) { var gl = MakeImage(t, "Glow", new Vector2(0.5f, 0.5f), new Vector2(0f, 50f), new Vector2(300f, 300f), new Color(1f, 0.75f, 0.3f, 0.3f)); gl.sprite = Lightswarm.ProceduralSprites.Glow(64, 0.1f); gl.rectTransform.pivot = new Vector2(0.5f, 0.5f); }
                RewardIcon(t, new Vector2(0f, 50f), 130f, l.kind, l.id);
                var am = MakeText(t, "Amount", new Vector2(0.5f, 0f), new Vector2(0f, 70f), TextAnchor.MiddleCenter, 34, rare ? GoldInk : OpInk); am.text = RewardLine(l.kind, l.amount, l.id); am.font = BoldFont(); am.rectTransform.sizeDelta = new Vector2(300f, 90f); am.resizeTextForBestFit = true; am.resizeTextMinSize = 18; am.resizeTextMaxSize = 34;
                var kd = MakeText(t, "Kind", new Vector2(0.5f, 0f), new Vector2(0f, 24f), TextAnchor.MiddleCenter, 18, OpDim); kd.font = BoldFont(); kd.rectTransform.sizeDelta = new Vector2(300f, 30f);
                kd.text = Spaced(l.kind == "points" ? "POINTS" : l.kind == "xp" ? "CREW EXPERIENCE" : l.kind == "gold" ? "GOLD" : l.kind == "premium" ? "PREMIUM SERVICE" : "CAMOUFLAGE");
                lootCards[i] = card.rectTransform; card.rectTransform.localScale = Vector3.zero;
            }
            lootButtons = new GameObject("Buttons", typeof(RectTransform)); lootButtons.transform.SetParent(rewardBody, false); var br = lootButtons.GetComponent<RectTransform>(); br.anchorMin = br.anchorMax = new Vector2(0.5f, 1f); br.anchoredPosition = new Vector2(0f, -1560f); br.sizeDelta = Vector2.zero;
            int more = Rewards.CratesTotal;
            MakePrimary(lootButtons.transform, more > 0 ? "Next crate · " + more + " left" : "Collect", new Vector2(0.5f, 0.5f), new Vector2(-245f, 0f), new Vector2(450f, 120f), 34, () => { if (Rewards.CratesTotal > 0) ShowCrate(Rewards.NextCrate); else CloseRewards(); });
            var ad = MakeButton(lootButtons.transform, "×2 · watch an ad", new Vector2(0.5f, 0.5f), new Vector2(245f, 0f), new Vector2(450f, 120f), 34, () =>
            {
                if (lootDoubled || opened == null) return;
                Ads.Rewarded("crate2", () => { if (lootDoubled || opened == null) return; lootDoubled = true; foreach (var l in opened) Rewards.GiveLoot(l); Sfx.Pickup(); foreach (var c in lootCards) { var am = c.Find("Amount").GetComponent<Text>(); am.text = "×2 · " + am.text; c.gameObject.AddComponent<Pop>(); } });
            });
            ad.GetComponent<Image>().color = XpDeep;
            lootButtons.SetActive(false);
        }

        /// <summary>The crate's moment, from the Hud's Update: the tap line breathing, the flash, the rewards coming in.</summary>
        void TickRewards(float dt)
        {
            if (crateBtn != null && crateBtn.activeInHierarchy && cratePic != null) cratePic.rectTransform.anchoredPosition = new Vector2(0f, 14f + Mathf.Sin(Time.unscaledTime * 2.4f) * 5f);   // the crate on the title bobs a little
            if (rewardSheet == null || !rewardSheet.activeSelf || stage == null || openKind == null || crateTap == null) return;
            float since = stage.SinceBurst; if (autoTap && stage.Waiting && stage.Age > 2.2f) { autoTap = false; stage.Tap(); }
            if (since < 0f) { var c = crateTap.color; c.a = stage.Waiting ? 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 4f) : 0f; crateTap.color = c; return; }
            if (opened == null) { opened = Rewards.Open(openKind); crateTap.text = ""; if (opened == null) { CloseRewards(); return; } BuildLoot(); }
            if (burstFlash != null) { var fc = burstFlash.color; fc.a = since < 0.08f ? since / 0.08f * 0.45f : Mathf.Max(0f, 0.45f - (since - 0.08f) * 1.4f); burstFlash.color = fc; }
            for (int i = 0; i < lootCards.Length; i++)
            {
                float k = Mathf.Clamp01((since - 0.35f - i * 0.28f) / 0.3f); if (k <= 0f) continue;
                float wasK = Mathf.Clamp01((since - dt - 0.35f - i * 0.28f) / 0.3f); if (wasK <= 0f) Sfx.Pickup(); if (wasK >= 1f) continue;   // in: left alone (a Pop may swell it)
                float s = k < 1f ? Mathf.Sin(k * Mathf.PI * 0.5f) * 1.12f : 1f;
                lootCards[i].localScale = new Vector3(s, s, 1f);
            }
            if (lootButtons != null && !lootButtons.activeSelf && since > 0.35f + lootCards.Length * 0.28f + 0.2f) lootButtons.SetActive(true);
        }
    }

    /// <summary>A quick swell and settle, for a card that has just changed.</summary>
    public class Pop : MonoBehaviour
    {
        float t;
        void Update() { t += Time.unscaledDeltaTime; float s = t < 0.35f ? 1f + Mathf.Sin(t / 0.35f * Mathf.PI) * 0.08f : 1f; transform.localScale = new Vector3(s, s, 1f); if (t >= 0.35f) Destroy(this); }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace IronNight
{
    /// <summary>
    /// Most wanted on the screen: the aces the platoon has met, each on an intelligence file of cream paper (his
    /// photograph, a red WANTED stamp, his rank and the name they have given him, his tank, his record, the bounty on
    /// him, and a rewarded ad to study his tactics), then the trophy wall of the ones who are dead. The title's fourth
    /// tile opens it, with the worst of them on it.
    /// </summary>
    public partial class Hud
    {
        GameObject wantedSheet; Transform wantedBody; Image wantedPic; static Sprite silhouette;
        static readonly Color Paper = new Color(0.84f, 0.79f, 0.66f), PaperInk = new Color(0.14f, 0.11f, 0.08f), StampRed = new Color(0.66f, 0.12f, 0.09f);

        /// <summary>An ace's photograph, or a dark head and shoulders until his portrait is in (Resources/UI/ace_keller ...).</summary>
        static Sprite AcePortrait(Nemesis.Ace a)
        {
            var sp = UiSprite("ace_" + a.name.ToLowerInvariant()); if (sp != null) return sp;
            if (silhouette != null) return silhouette;
            const int W = 128, H = 150; var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
            {
                float u = (x - W * 0.5f) / W, v = y / (float)H; var bg = Color.Lerp(new Color(0.22f, 0.19f, 0.15f), new Color(0.42f, 0.37f, 0.29f), v);
                float head = (u * u) / 0.045f + ((v - 0.62f) * (v - 0.62f)) / 0.03f, shoulders = (u * u) / 0.16f + ((v - 0.05f) * (v - 0.05f)) / 0.09f;
                tex.SetPixel(x, y, head < 1f || (shoulders < 1f && v < 0.38f) ? new Color(0.08f, 0.07f, 0.06f) : bg);
            }
            tex.Apply(); return silhouette = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>The title's fourth tile: the worst of them at large, or how many.</summary>
        void RefreshWantedTile()
        {
            if (ordersCount == null) return;
            var w = Nemesis.Worst; int n = 0; foreach (var a in Nemesis.Living) if (a.met > 0) n++;
            ordersCount.text = w == null ? "none yet" : n == 1 ? w.Title.ToLowerInvariant() + " at large" : n + " aces at large";
            ordersCount.color = w != null && w.leaders > 0 ? MapRed : OpAmber;
            if (wantedPic != null && w != null) { var sp = AcePortrait(w); wantedPic.sprite = sp; float c = Mathf.Max(216f / sp.rect.width, 240f / sp.rect.height); wantedPic.rectTransform.sizeDelta = new Vector2(sp.rect.width * c, sp.rect.height * c); }
        }

        /// <summary>The files, over the title.</summary>
        public void ShowWanted()
        {
            if (wantedSheet == null)
            {
                wantedSheet = new GameObject("Wanted", typeof(RectTransform), typeof(Image)); wantedSheet.transform.SetParent(canvas.transform, false); Stretch(wantedSheet); wantedSheet.GetComponent<Image>().color = new Color(0.035f, 0.03f, 0.025f, 1f);
                var body = new GameObject("Body", typeof(RectTransform)); body.transform.SetParent(wantedSheet.transform, false); Stretch(body); wantedBody = body.transform;
            }
            if (curtain != null) wantedSheet.transform.SetSiblingIndex(curtain.transform.GetSiblingIndex());   // under the curtain, over the title
            wantedSheet.SetActive(true); BuildWanted(null);
        }

        void BuildWanted(string line)
        {
            foreach (Transform c in wantedBody) Destroy(c.gameObject);
            var b = wantedBody;
            { var glow = MakeImage(b, "Glow", new Vector2(0.5f, 1f), new Vector2(0f, 250f), new Vector2(1600f, 1100f), new Color(0.9f, 0.3f, 0.15f, 0.08f)); glow.sprite = Lightswarm.ProceduralSprites.Glow(64, 0.1f); }
            MakeGhost(b, "BACK", new Vector2(0f, 1f), new Vector2(130, -95), new Vector2(200, 80), 26, () => { wantedSheet.SetActive(false); RefreshWantedTile(); });
            var ey = MakeText(b, "Eyebrow", new Vector2(0.5f, 1f), new Vector2(0, -150), TextAnchor.MiddleCenter, 26, MapRed); ey.text = Spaced("INTELLIGENCE · ENEMY ACES"); ey.font = LabelFont(); ey.rectTransform.sizeDelta = new Vector2(900, 40);
            var ti = MakeText(b, "Title", new Vector2(0.5f, 1f), new Vector2(0, -190), TextAnchor.MiddleCenter, 110, OpInk); ti.text = "MOST WANTED"; Engrave(ti); ti.rectTransform.sizeDelta = new Vector2(1000, 140); ti.verticalOverflow = VerticalWrapMode.Overflow;
            var ln = MakeText(b, "Line", new Vector2(0.5f, 1f), new Vector2(0, -318), TextAnchor.MiddleCenter, 24, OpDim); ln.rectTransform.sizeDelta = new Vector2(1000, 40);
            ln.text = line ?? "An ace who gets away comes back stronger. Finish them.";
            if (line != null) ln.color = OpAmber;

            // the list, dragged up and down
            var vp = new GameObject("Files", typeof(RectTransform), typeof(RectMask2D), typeof(Image), typeof(ScrollRect)); vp.transform.SetParent(b, false);
            var vr = vp.GetComponent<RectTransform>(); vr.anchorMin = new Vector2(0.5f, 0f); vr.anchorMax = new Vector2(0.5f, 1f); vr.pivot = new Vector2(0.5f, 1f); vr.offsetMin = new Vector2(-520f, 30f); vr.offsetMax = new Vector2(520f, -370f);
            vp.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
            var content = new GameObject("Stack", typeof(RectTransform)); content.transform.SetParent(vp.transform, false); var cr = content.GetComponent<RectTransform>(); cr.anchorMin = cr.anchorMax = cr.pivot = new Vector2(0.5f, 1f);
            var sr = vp.GetComponent<ScrollRect>(); sr.content = cr; sr.viewport = vr; sr.horizontal = false; sr.vertical = true; sr.movementType = ScrollRect.MovementType.Clamped; sr.inertia = true; sr.decelerationRate = 0.12f; sr.scrollSensitivity = 60f;
            var ct = content.transform; float y = -10f;
            var met = Nemesis.Living.FindAll(a => a.met > 0); met.Sort((p, q) => q.level != p.level ? q.level - p.level : q.leaders - p.leaders);
            if (met.Count == 0) { var none = MakeText(ct, "None", new Vector2(0.5f, 1f), new Vector2(0, y - 40f), TextAnchor.MiddleCenter, 28, OpDim); none.text = "No ace has crossed your path yet. The first comes after 1:40 of a night."; none.rectTransform.sizeDelta = new Vector2(960, 80); y -= 140f; }
            foreach (var a in met) { File(ct, a, y); y -= 400f; }
            var dead = Nemesis.Dead;
            if (dead.Count > 0)
            {
                var th = MakeText(ct, "Wall", new Vector2(0.5f, 1f), new Vector2(0, y - 20f), TextAnchor.MiddleCenter, 26, GoldInk); th.text = Spaced("TROPHY WALL"); th.font = LabelFont(); th.rectTransform.sizeDelta = new Vector2(900, 40); y -= 80f;
                for (int i = 0; i < dead.Count; i++)
                {
                    var d = dead[i]; float x = (i % 3 - 1) * 335f; float top = y - (i / 3) * 215f;
                    var card = MakeCard(ct, "Trophy", new Vector2(0.5f, 1f), new Vector2(x, top), new Vector2(320f, 200f), new Color(0.09f, 0.08f, 0.06f, 0.97f), 0.2f); card.rectTransform.pivot = new Vector2(0.5f, 1f); card.transform.Find("Edge").GetComponent<Image>().color = new Color(1f, 0.8f, 0.35f, 0.55f);
                    var tn = MakeText(card.transform, "Item", new Vector2(0.5f, 1f), new Vector2(0, -26), TextAnchor.UpperCenter, 26, GoldInk); tn.text = d.Title + "'s " + d.trophy; tn.font = BoldFont(); tn.rectTransform.sizeDelta = new Vector2(300, 70); tn.resizeTextForBestFit = true; tn.resizeTextMinSize = 16; tn.resizeTextMaxSize = 26;
                    var tw = MakeText(card.transform, "Where", new Vector2(0.5f, 0f), new Vector2(0, 24), TextAnchor.LowerCenter, 20, OpDim); tw.text = (string.IsNullOrEmpty(d.nick) ? "" : d.nick + "\n") + "killed at " + d.killedAt + " · " + d.killedOn; tw.rectTransform.sizeDelta = new Vector2(300, 90);
                }
                y -= ((dead.Count + 2) / 3) * 215f;
            }
            cr.sizeDelta = new Vector2(1040f, -y + 40f);
        }

        /// <summary>One ace's file: the photograph and the stamp, who he is and what he has done, the bounty, the ad.</summary>
        void File(Transform parent, Nemesis.Ace a, float top)
        {
            var card = MakeImage(parent, "File", new Vector2(0.5f, 1f), new Vector2(0f, top), new Vector2(1000f, 380f), Paper); card.sprite = Rounded(); card.type = Image.Type.Sliced; card.rectTransform.pivot = new Vector2(0.5f, 1f); var t = card.transform;
            var frame = MakeImage(t, "Frame", new Vector2(0f, 1f), new Vector2(26f, -26f), new Vector2(252f, 300f), PaperInk); frame.rectTransform.pivot = new Vector2(0f, 1f);
            var mask = new GameObject("Photo", typeof(RectTransform), typeof(RectMask2D)); mask.transform.SetParent(frame.transform, false); var mr = mask.GetComponent<RectTransform>(); mr.anchorMin = mr.anchorMax = mr.pivot = new Vector2(0.5f, 0.5f); mr.sizeDelta = new Vector2(240f, 288f);
            var sp = AcePortrait(a); var ph = MakeImage(mask.transform, "Pic", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240f, 288f), new Color(1f, 0.93f, 0.8f)); ph.sprite = sp; ph.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            { float c = Mathf.Max(240f / sp.rect.width, 288f / sp.rect.height); ph.rectTransform.sizeDelta = new Vector2(sp.rect.width * c, sp.rect.height * c); }
            var rk = MakeText(t, "Rank", new Vector2(0f, 1f), new Vector2(304, -28), TextAnchor.UpperLeft, 20, new Color(0.35f, 0.29f, 0.22f)); rk.text = Spaced(a.FullRank.ToUpperInvariant() + " · " + Nemesis.Tank(a).name.ToUpperInvariant()); rk.font = LabelFont(); rk.rectTransform.sizeDelta = new Vector2(560, 30);
            var nm = MakeText(t, "Name", new Vector2(0f, 1f), new Vector2(302, -56), TextAnchor.UpperLeft, 66, PaperInk); nm.text = a.name.ToUpperInvariant(); Serif(nm); nm.rectTransform.sizeDelta = new Vector2(560, 80); nm.verticalOverflow = VerticalWrapMode.Overflow;
            var nk = MakeText(t, "Nick", new Vector2(0f, 1f), new Vector2(304, -134), TextAnchor.UpperLeft, 26, StampRed); nk.text = string.IsNullOrEmpty(a.nick) ? "no name for him yet" : a.nick; nk.font = BoldFont(); nk.rectTransform.sizeDelta = new Vector2(660, 36);
            for (int s = 0; s < 5; s++) { var st = MakeImage(t, "Rank" + s, new Vector2(0f, 1f), new Vector2(304f + s * 34f, -180f), new Vector2(28f, 28f), s < a.level ? StampRed : new Color(0.55f, 0.5f, 0.42f, 0.6f)); st.sprite = StarSprite(); st.rectTransform.pivot = new Vector2(0f, 1f); }
            var rec = MakeText(t, "Record", new Vector2(0f, 1f), new Vector2(304, -220), TextAnchor.UpperLeft, 22, PaperInk); rec.text = Nemesis.Record(a) + " · last seen at " + a.place; rec.rectTransform.sizeDelta = new Vector2(660, 60);
            var bt = MakeText(t, "Bounty", new Vector2(0f, 0f), new Vector2(304, 36), TextAnchor.LowerLeft, 26, PaperInk); bt.font = BoldFont(); bt.rectTransform.sizeDelta = new Vector2(420, 40);
            bt.text = "Bounty " + Nemesis.BountyPoints(a).ToString("N0", En) + " points" + (Nemesis.BountyGold(a) > 0 ? " · " + Nemesis.BountyGold(a) + " gold" : "");
            // the stamp
            var stamp = MakeText(t, "Stamp", new Vector2(1f, 1f), new Vector2(-34, -30), TextAnchor.MiddleCenter, 58, new Color(StampRed.r, StampRed.g, StampRed.b, 0.85f)); stamp.text = "WANTED"; stamp.font = LabelBoldFont(); stamp.rectTransform.sizeDelta = new Vector2(250, 76); stamp.rectTransform.pivot = new Vector2(1f, 1f); stamp.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -9f);
            var box = MakeImage(stamp.transform, "Box", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(236f, 72f), new Color(StampRed.r, StampRed.g, StampRed.b, 0.85f)); box.sprite = Outline(); box.type = Image.Type.Sliced; box.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            // the ad: study him
            if (a.studied) { var sd = MakeText(t, "Studied", new Vector2(1f, 0f), new Vector2(-34, 36), TextAnchor.LowerRight, 22, new Color(0.2f, 0.45f, 0.22f)); sd.text = "Tactics studied · +30% against him"; sd.font = BoldFont(); sd.rectTransform.sizeDelta = new Vector2(420, 34); }
            else { var ad = MakeButton(t, "Study his tactics · ad", new Vector2(1f, 0f), new Vector2(-190f, 56f), new Vector2(310f, 64f), 22, () => Ads.Rewarded("nemesis", () => { Nemesis.Study(a); Sfx.Pickup(); BuildWanted(a.Title + "'s tactics studied: a third more damage against him next time"); })); ad.GetComponent<Image>().color = XpDeep; }
        }
    }
}

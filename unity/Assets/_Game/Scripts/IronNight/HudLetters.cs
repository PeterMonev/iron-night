using UnityEngine;
using UnityEngine.UI;

namespace IronNight
{
    /// <summary>
    /// The censor's desk: the crew's letters home wait for the commander to read and pass them, as officers did. A slip
    /// of paper on the title says one is waiting. The letter lies on aged paper (V-mail for the Americans, the field
    /// post for the Soviets), the writer's photograph clipped to its corner, written in a hand. PASS IT brings the
    /// censor's stamp down on it and the letter goes home; then the next, if there is one.
    /// </summary>
    public partial class Hud
    {
        GameObject letterPill, letterSheet; Text letterPillText; Transform letterBody; static Font handFont;
        static readonly Color LetterInk = new Color(0.12f, 0.12f, 0.22f);   // blue-black, from a fountain pen

        /// <summary>A hand to write in: the Caveat font when it is in the game (Resources/Fonts), else the plain one.</summary>
        static Font HandFont() => handFont != null ? handFont : handFont = Resources.Load<Font>("Fonts/Caveat") ?? BoldFont();

        void BuildLetterPill()
        {
            letterPill = MakeButton(titleSheet.transform, "", new Vector2(0.5f, 0f), new Vector2(0, 1140), new Vector2(680, 70), 24, ShowLetters);
            letterPill.GetComponent<Image>().color = new Color(Paper.r, Paper.g, Paper.b, 0.96f); letterPill.transform.Find("Label").gameObject.SetActive(false);
            // an envelope drawn in ink: its body and the flap's V
            var env = MakeImage(letterPill.transform, "Envelope", new Vector2(0f, 0.5f), new Vector2(46, 0), new Vector2(44, 30), PaperInk); env.sprite = Outline(); env.type = Image.Type.Sliced; env.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            foreach (float s in new[] { -1f, 1f })
            {
                var flap = MakeImage(env.transform, "Flap", new Vector2(0.5f, 0.5f), new Vector2(s * 10f, 4f), new Vector2(25f, 2.5f), PaperInk); flap.rectTransform.pivot = new Vector2(0.5f, 0.5f); flap.rectTransform.localRotation = Quaternion.Euler(0f, 0f, s * 32f);
            }
            letterPillText = MakeText(letterPill.transform, "Text", new Vector2(0.5f, 0.5f), new Vector2(26, 0), TextAnchor.MiddleCenter, 24, PaperInk); letterPillText.font = LabelBoldFont(); letterPillText.rectTransform.pivot = new Vector2(0.5f, 0.5f); letterPillText.rectTransform.sizeDelta = new Vector2(580, 60);
            letterPill.SetActive(false);
        }

        /// <summary>The slip on the title: a letter waiting, whose, or how many.</summary>
        void RefreshLetterPill()
        {
            if (letterPill == null) return;
            var w = Letters.Waiting(); letterPill.SetActive(w.Count > 0); if (w.Count == 0) return;
            letterPillText.text = Spaced(w.Count == 1 ? "A LETTER TO PASS · " + Letters.Writer(w[0]).ToUpperInvariant() : w.Count + " LETTERS TO PASS"); Fit(letterPillText, 14);
        }

        void ShowLetters()
        {
            var w = Letters.Waiting();
            if (w.Count == 0) { if (letterSheet != null) letterSheet.SetActive(false); RefreshLetterPill(); return; }
            ShowLetter(w[0], w.Count);
        }

        void ShowLetter(Letters.Letter l, int waiting)
        {
            if (letterSheet == null)
            {
                letterSheet = new GameObject("Letters", typeof(RectTransform), typeof(Image)); letterSheet.transform.SetParent(canvas.transform, false); Stretch(letterSheet); letterSheet.GetComponent<Image>().color = new Color(0.03f, 0.028f, 0.025f, 1f);
                var holder = new GameObject("Body", typeof(RectTransform)); holder.transform.SetParent(letterSheet.transform, false); Stretch(holder); letterBody = holder.transform;
            }
            foreach (Transform c in letterBody) Destroy(c.gameObject);
            var b = letterBody; var m = System.Array.Find(Crew.Men, x => x.id == l.man); bool su = l.man.StartsWith("su_");
            { var glow = MakeImage(b, "Lamp", new Vector2(0.5f, 1f), new Vector2(0f, 120f), new Vector2(1500f, 1500f), new Color(1f, 0.78f, 0.45f, 0.1f)); glow.sprite = Lightswarm.ProceduralSprites.Glow(64, 0.1f); }   // a desk lamp over the paper
            MakeGhost(b, "BACK", new Vector2(0f, 1f), new Vector2(130, -95), new Vector2(200, 80), 26, () => { letterSheet.SetActive(false); RefreshLetterPill(); });
            var ey = MakeText(b, "Eyebrow", new Vector2(0.5f, 1f), new Vector2(0, -170), TextAnchor.MiddleCenter, 26, OpAmber); ey.font = LabelFont(); ey.rectTransform.sizeDelta = new Vector2(1000, 40);
            ey.text = Spaced("THE CENSOR'S DESK" + (waiting > 1 ? " · " + waiting + " WAITING" : ""));
            // the paper, a little crooked on the desk
            var paper = MakeImage(b, "Paper", new Vector2(0.5f, 1f), new Vector2(0f, -230f), new Vector2(980f, 1180f), Paper); paper.sprite = Rounded(); paper.type = Image.Type.Sliced;
            paper.rectTransform.pivot = new Vector2(0.5f, 1f); paper.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -0.8f); var p = paper.transform;
            var head = MakeText(p, "Head", new Vector2(0f, 1f), new Vector2(44, -34), TextAnchor.UpperLeft, 40, StampRed); head.text = su ? "FIELD POST" : "V-MAIL"; head.font = LabelBoldFont(); head.rectTransform.pivot = new Vector2(0f, 1f); head.rectTransform.sizeDelta = new Vector2(560, 50);
            var sub = MakeText(p, "Sub", new Vector2(0f, 1f), new Vector2(44, -84), TextAnchor.UpperLeft, 19, new Color(PaperInk.r, PaperInk.g, PaperInk.b, 0.7f)); sub.font = LabelFont(); sub.rectTransform.pivot = new Vector2(0f, 1f); sub.rectTransform.sizeDelta = new Vector2(640, 28);
            sub.text = Spaced(su ? "MILITARY POST · A LETTER FROM THE FRONT" : "WAR AND NAVY DEPARTMENTS · V-MAIL SERVICE"); Fit(sub, 12);
            var to = MakeText(p, "To", new Vector2(0f, 1f), new Vector2(44, -128), TextAnchor.UpperLeft, 25, PaperInk); to.font = LabelFont(); to.rectTransform.pivot = new Vector2(0f, 1f); to.rectTransform.sizeDelta = new Vector2(640, 34); Fit(to, 16);
            to.text = "To: " + (Letters.To.TryGetValue(l.man, out var whom) ? whom : "home");
            var from = MakeText(p, "From", new Vector2(0f, 1f), new Vector2(44, -164), TextAnchor.UpperLeft, 25, PaperInk); from.font = LabelFont(); from.rectTransform.pivot = new Vector2(0f, 1f); from.rectTransform.sizeDelta = new Vector2(640, 34); Fit(from, 16);
            from.text = "From: " + Letters.Writer(l) + " · " + l.place;
            var rule = MakeImage(p, "Rule", new Vector2(0.5f, 1f), new Vector2(0f, -214f), new Vector2(900f, 2f), new Color(PaperInk.r, PaperInk.g, PaperInk.b, 0.35f)); rule.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            // the writer's photograph clipped to the corner
            if (m != null)
            {
                var frame = MakeImage(p, "Photo", new Vector2(1f, 1f), new Vector2(-40f, -28f), new Vector2(196f, 236f), new Color(0.95f, 0.93f, 0.88f)); frame.rectTransform.pivot = new Vector2(1f, 1f); frame.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 4f);
                var mask = new GameObject("Mask", typeof(RectTransform), typeof(RectMask2D)); mask.transform.SetParent(frame.transform, false); var mr = mask.GetComponent<RectTransform>(); mr.anchorMin = mr.anchorMax = mr.pivot = new Vector2(0.5f, 0.5f); mr.sizeDelta = new Vector2(176f, 200f); mr.anchoredPosition = new Vector2(0f, 8f);
                var sp = UiSprite(Crew.Portrait(m));
                if (sp != null) { var ph = MakeImage(mask.transform, "Face", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(176f, 200f), new Color(1f, 0.95f, 0.85f)); ph.sprite = sp; ph.rectTransform.pivot = new Vector2(0.5f, 0.5f); float c = Mathf.Max(176f / sp.rect.width, 200f / sp.rect.height); ph.rectTransform.sizeDelta = new Vector2(sp.rect.width * c, sp.rect.height * c); }
                var clip = MakeImage(frame.transform, "Clip", new Vector2(0.5f, 1f), new Vector2(-40f, 14f), new Vector2(26f, 64f), new Color(0.55f, 0.56f, 0.58f)); clip.sprite = Outline(); clip.type = Image.Type.Sliced; clip.rectTransform.pivot = new Vector2(0.5f, 1f);
            }
            // the letter, in the writer's hand
            var body = MakeText(p, "Body", new Vector2(0.5f, 1f), new Vector2(0f, -272f), TextAnchor.UpperLeft, 54, LetterInk); body.font = HandFont(); body.rectTransform.pivot = new Vector2(0.5f, 1f); body.rectTransform.sizeDelta = new Vector2(880f, 800f);
            body.text = l.body; body.lineSpacing = 1.08f; Fit(body, 34);
            // the censor's stamp, brought down on PASS IT
            var stamp = MakeText(p, "Stamp", new Vector2(0.5f, 0f), new Vector2(150f, 150f), TextAnchor.MiddleCenter, 44, new Color(StampRed.r, StampRed.g, StampRed.b, 0.85f)); stamp.font = LabelBoldFont(); stamp.rectTransform.pivot = new Vector2(0.5f, 0.5f); stamp.rectTransform.sizeDelta = new Vector2(520f, 120f);
            stamp.text = su ? "CHECKED BY\nMILITARY CENSOR" : "PASSED BY\nARMY EXAMINER"; stamp.lineSpacing = 0.9f; stamp.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -11f);
            var box = MakeImage(stamp.transform, "Box", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500f, 116f), new Color(StampRed.r, StampRed.g, StampRed.b, 0.85f)); box.sprite = Outline(); box.type = Image.Type.Sliced; box.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            stamp.gameObject.SetActive(false);
            GameObject pass = null;
            pass = MakePrimary(b, "Pass it · send it home", new Vector2(0.5f, 1f), new Vector2(0, -1500), new Vector2(780, 130), 40, () =>
            {
                if (!stamp.gameObject.activeSelf) { stamp.gameObject.SetActive(true); stamp.gameObject.AddComponent<Thump>().done = () => { Letters.Pass(l); ShowLetters(); }; Sfx.Rumble(); pass.SetActive(false); }
            });
            letterSheet.transform.SetAsLastSibling(); if (curtain != null) curtain.transform.SetAsLastSibling();
            letterSheet.SetActive(true);
        }
    }

    /// <summary>A stamp brought down: from large and faint to its place with a knock, then a moment on the paper before done.</summary>
    public class Thump : MonoBehaviour
    {
        public System.Action done; float t; Vector3 baseScale; Graphic[] parts;
        void Start() { baseScale = transform.localScale; parts = GetComponentsInChildren<Graphic>(); }
        void Update()
        {
            t += Time.unscaledDeltaTime; float k = Mathf.Clamp01(t / 0.16f), s = Mathf.Lerp(1.9f, 1f, k * k);
            transform.localScale = baseScale * s;
            foreach (var g in parts) { var c = g.color; c.a = Mathf.Lerp(0f, 0.85f, k); g.color = c; }
            if (t >= 1.1f) { enabled = false; done?.Invoke(); }
        }
    }
}

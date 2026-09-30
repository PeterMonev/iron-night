using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IronNight
{
    /// <summary>
    /// The adjutant: the commander's liaison officer, Lt. Vivian Rhodes for the Americans, Leyt. Tatiana Belova for the
    /// Soviets. She meets him on the first title of every session with what waits for him (letters at the censor's
    /// desk, crates, the day's mail, the quartermaster's store, the free dispatch, the week's event), and she comes
    /// after a promotion's salute to congratulate him. By day she is on duty, buttoned up (Resources/UI/adjutant_us,
    /// _su); late at night (22:00 to 4:00) and on a promotion she is off duty, the uniform opened (_offduty). Her
    /// pictures are painted on black and fade out at their edges (art/pipeline/adjutant_fade.py), so she stands in the
    /// dark sheet with no cut-out. She slides in from the left, her words come up in a card beside her, CARRY ON ends it.
    /// </summary>
    public partial class Hud
    {
        GameObject adjutantSheet; Image adjutantDim; RectTransform adjutantFigure, adjutantCard; CanvasGroup adjutantCardGroup;
        float adjutantT = -1f; System.Action adjutantThen; bool welcomeWaits; string adjutantNation;   // welcomeWaits: the session's greeting held back behind a promotion; adjutantNation: a test's nation
        const float FigureX = -190f;

        string AdjutantNation => adjutantNation ?? Depot.Nation;
        string AdjutantName => AdjutantNation == "su" ? "Leyt. Tatiana Belova" : "Lt. Vivian Rhodes";
        static bool OffDutyHour { get { int h = System.DateTime.Now.Hour; return h >= 22 || h < 4; } }

        /// <summary>How she addresses him: his rank, and Comrade before it for the Soviets.</summary>
        string Address() => (AdjutantNation == "su" ? "Comrade " : "") + Depot.Rank;

        /// <summary>The first title of a session: her greeting, by the hour, then the mail and the quartermaster as before.
        /// A promotion waiting over the title goes first; she comes after its salute.</summary>
        void Greet()
        {
            if (promoSheet != null) { welcomeWaits = true; return; }
            bool off = OffDutyHour;
            ShowAdjutant(Hello(off), AdjutantNews(), off, AfterGreeting); Voices.Adjutant(AdjutantNation, HelloKind(off));
        }

        /// <summary>Her greeting by the hour, broken after the comma: good morning, afternoon or evening; late at night she is up too.</summary>
        /// <summary>Which of her recorded greetings goes with the hour: morning, afternoon, evening, or night off duty.</summary>
        static string HelloKind(bool off) { int h = System.DateTime.Now.Hour; return off ? "night" : h >= 5 && h < 12 ? "morning" : h < 18 ? "afternoon" : "evening"; }

        string Hello(bool off)
        {
            int h = System.DateTime.Now.Hour;
            if (off) return (AdjutantNation == "su" ? "Still awake, " : "Still up, ") + Address() + "?\nSo am I.";
            return (h >= 5 && h < 12 ? "Good morning," : h < 18 ? "Good afternoon," : "Good evening,") + "\n" + Address() + ".";
        }

        void AfterGreeting() { if (Rewards.MailReady) ShowMail(); else if (Rewards.QmHours >= 0.5f) ShowQuartermaster(); }

        /// <summary>After a promotion's salute: she congratulates him, off duty; then, if the session's greeting waited, the mail.</summary>
        void Congratulate()
        {
            var lines = new List<string> { AdjutantNation == "su" ? "The whole regiment heard." : "The whole company heard.", "Tonight the drinks are on me." };
            bool waited = welcomeWaits; welcomeWaits = false;
            ShowAdjutant("Congratulations,\n" + Address() + ".", lines, true, waited ? AfterGreeting : (System.Action)null); Voices.Adjutant(AdjutantNation, "congrats");
        }

        /// <summary>What waits for him, most pressing first, three at most.</summary>
        static List<string> AdjutantNews()
        {
            var news = new List<string>(); var inv = System.Globalization.CultureInfo.InvariantCulture;
            var waiting = Letters.Waiting();
            if (waiting.Count == 1) news.Add("A letter from " + Letters.Writer(waiting[0]) + " waits at the censor's desk.");
            else if (waiting.Count > 1) news.Add(waiting.Count + " letters from the crew wait at the censor's desk.");
            int crates = Rewards.CratesTotal;
            if (crates == 1) news.Add((Rewards.NextCrate == "officer" ? "An officer's crate" : "A supply crate") + " waits in the hangar.");
            else if (crates > 1) news.Add(crates + " crates wait in the hangar.");
            else if (Rewards.FreeCrateReady) news.Add("A free supply crate is ready for you.");
            if (Rewards.MailReady) news.Add("Today's mail has come in.");
            if (Rewards.QmPoints >= 100) news.Add("The quartermaster has put by " + Rewards.QmPoints.ToString("N0", inv) + " points for you.");
            if (Dispatch.FreeSpin) news.Add("Your free lucky dispatch is waiting.");
            news.Add(Weekly.Now.name + ": " + Weekly.LeftText + ".");
            if (news.Count > 3) news.RemoveRange(3, news.Count - 3);
            return news;
        }

        /// <summary>Test switch --adjutant[=us|su][-night]: her greeting straight away, in that nation and look.</summary>
        public void TestAdjutant(string spec)
        {
            welcomed = true; adjutantNation = spec.Contains("su") ? "su" : spec.Contains("us") ? "us" : null;
            bool off = spec.Contains("night");
            ShowAdjutant(Hello(off), AdjutantNews(), off, null); Voices.Adjutant(AdjutantNation, HelloKind(off));
        }

        /// <summary>Her sheet: the dark, her picture on the left, her words in a card on the right, CARRY ON under them.</summary>
        void ShowAdjutant(string hello, List<string> news, bool offDuty, System.Action then)
        {
            var amber = new Color(0.95f, 0.66f, 0.23f); var ink = new Color(0.93f, 0.91f, 0.86f);
            if (adjutantSheet != null) Destroy(adjutantSheet);
            adjutantThen = then;
            adjutantSheet = new GameObject("Adjutant", typeof(RectTransform), typeof(Image)); adjutantSheet.transform.SetParent(canvas.transform, false); Stretch(adjutantSheet);
            if (curtain != null) adjutantSheet.transform.SetSiblingIndex(curtain.transform.GetSiblingIndex());   // under the curtain, over everything else
            adjutantDim = adjutantSheet.GetComponent<Image>(); adjutantDim.color = new Color(0.012f, 0.012f, 0.018f, 0f);
            var t = adjutantSheet.transform;

            var fig = MakeImage(t, "Figure", Vector2.zero, new Vector2(FigureX - 180f, 0f), new Vector2(1000f, 1500f), new Color(1f, 1f, 1f, 0f));
            fig.sprite = UiSprite("adjutant_" + AdjutantNation + (offDuty ? "_offduty" : "")); fig.preserveAspect = true; adjutantFigure = fig.rectTransform;

            var card = MakeImage(t, "Card", new Vector2(1f, 0f), new Vector2(-34f, 880f), new Vector2(560f, 640f), new Color(0.05f, 0.055f, 0.07f, 0.94f));
            card.sprite = Rounded(); card.type = Image.Type.Sliced; adjutantCard = card.rectTransform; adjutantCard.pivot = new Vector2(0.5f, 0.5f); adjutantCard.anchoredPosition = new Vector2(-314f, 1200f); adjutantCardGroup = card.gameObject.AddComponent<CanvasGroup>(); adjutantCardGroup.alpha = 0f;
            var edge = MakeImage(card.transform, "Edge", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 640f), new Color(amber.r, amber.g, amber.b, 0.45f)); edge.sprite = Outline(); edge.type = Image.Type.Sliced; edge.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            var c = card.transform;
            var ey = MakeText(c, "Name", new Vector2(0.5f, 1f), new Vector2(0f, -34f), TextAnchor.MiddleCenter, 22, amber); ey.font = LabelFont(); ey.text = Spaced((AdjutantName + " · adjutant").ToUpperInvariant()); ey.rectTransform.sizeDelta = new Vector2(510f, 36f); Fit(ey, 14);
            var hi = MakeText(c, "Hello", new Vector2(0.5f, 1f), new Vector2(0f, -76f), TextAnchor.UpperCenter, 40, ink); hi.font = DisplayFont(); hi.text = hello; hi.rectTransform.sizeDelta = new Vector2(500f, 110f); Fit(hi, 26);
            var rule = MakeImage(c, "Rule", new Vector2(0.5f, 1f), new Vector2(0f, -200f), new Vector2(120f, 2f), new Color(amber.r, amber.g, amber.b, 0.7f)); rule.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var body = MakeText(c, "News", new Vector2(0.5f, 1f), new Vector2(0f, -226f), TextAnchor.UpperLeft, 30, ink); body.font = DefaultFont(); body.lineSpacing = 1.1f;
            body.text = news.Count > 0 ? "•  " + string.Join("\n•  ", news) : ""; body.rectTransform.sizeDelta = new Vector2(480f, 260f); Fit(body, 20);
            MakePrimary(c, "Carry on", new Vector2(0.5f, 0f), new Vector2(0f, 84f), new Vector2(460f, 110f), 34, CloseAdjutant);

            adjutantT = 0f; Sfx.Whoosh();
        }

        void CloseAdjutant()
        {
            if (adjutantSheet != null) Destroy(adjutantSheet);
            adjutantSheet = null; adjutantT = -1f; adjutantNation = null;
            var then = adjutantThen; adjutantThen = null; then?.Invoke();
        }

        /// <summary>The dark comes up, she slides in from the left, the card follows; then she breathes, barely.</summary>
        void TickAdjutant(float dt)
        {
            if (adjutantSheet == null || adjutantT < 0f) return;
            adjutantT += dt;
            float a = Mathf.Clamp01(adjutantT / 0.3f); adjutantDim.color = new Color(0.012f, 0.012f, 0.018f, a);   // whole: in linear light even 1.5% of the bright title shows through
            float k = Mathf.Clamp01((adjutantT - 0.05f) / 0.55f), e = 1f - (1f - k) * (1f - k) * (1f - k);
            adjutantFigure.anchoredPosition = new Vector2(FigureX - 180f * (1f - e), 0f);
            var fc = adjutantFigure.GetComponent<Image>(); fc.color = new Color(1f, 1f, 1f, e);
            float s = 1f + 0.004f * Mathf.Sin(adjutantT * 1.6f); adjutantFigure.localScale = new Vector3(s, s, 1f);
            float cp = Mathf.Clamp01((adjutantT - 0.35f) / 0.3f), ce = 1f - (1f - cp) * (1f - cp);
            adjutantCardGroup.alpha = ce; float cs = Mathf.Lerp(0.92f, 1f, ce); adjutantCard.localScale = new Vector3(cs, cs, 1f);
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace IronNight
{
    /// <summary>
    /// The weekly event's face: a tile under the crate on the title (the event's picture, its name, the time left) and
    /// its sheet: the picture, the name, the rule, the week's goal with its three steps and their rewards, next week's
    /// event, and the way into the night.
    /// </summary>
    public partial class Hud
    {
        public System.Action OnWeekly;
        GameObject weeklyBtn, weeklySheet; Image weeklyPic; Text weeklyName, weeklyLeft; Transform weeklyBody;

        /// <summary>The event's tile under the crate, top left of the title.</summary>
        void BuildWeeklyButton()
        {
            const float W = 190f;
            var b = MakeButton(titleSheet.transform, "", new Vector2(0f, 1f), new Vector2(135, -480), new Vector2(W, W), 20, () => ShowWeekly()); weeklyBtn = b;
            b.GetComponent<Image>().color = new Color(0.05f, 0.055f, 0.07f, 0.85f); b.transform.Find("Label").gameObject.SetActive(false);
            var mask = new GameObject("Mask", typeof(RectTransform), typeof(RectMask2D)); mask.transform.SetParent(b.transform, false);
            var mrt = mask.GetComponent<RectTransform>(); mrt.anchorMin = mrt.anchorMax = new Vector2(0.5f, 0.5f); mrt.sizeDelta = new Vector2(W - 8f, W - 8f); mrt.anchoredPosition = Vector2.zero;
            weeklyPic = MakeImage(mask.transform, "Pic", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(W - 8f, W - 8f), Color.white); weeklyPic.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var fade = MakeImage(mask.transform, "Fade", new Vector2(0.5f, 0f), Vector2.zero, new Vector2(W - 8f, 120f), new Color(0.02f, 0.02f, 0.03f, 0.95f)); fade.sprite = Lightswarm.ProceduralSprites.GradientDown(64, 1.2f); fade.rectTransform.pivot = new Vector2(0.5f, 0f);
            var top = MakeImage(mask.transform, "Top", new Vector2(0.5f, 1f), Vector2.zero, new Vector2(W - 8f, 44f), new Color(0.02f, 0.02f, 0.03f, 0.75f)); top.rectTransform.pivot = new Vector2(0.5f, 1f);
            var edge = MakeImage(b.transform, "Edge", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(W, W), new Color(1f, 0.8f, 0.35f, 0.4f)); edge.sprite = Outline(); edge.type = Image.Type.Sliced; edge.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var ey = MakeText(b.transform, "Eyebrow", new Vector2(0.5f, 1f), new Vector2(0, -8), TextAnchor.UpperCenter, 17, OpAmber); ey.text = Spaced("THIS WEEK"); ey.font = LabelFont(); ey.rectTransform.sizeDelta = new Vector2(W - 10f, 26f);
            weeklyName = MakeText(b.transform, "Name", new Vector2(0.5f, 0f), new Vector2(0, 32), TextAnchor.LowerCenter, 26, OpInk); weeklyName.rectTransform.sizeDelta = new Vector2(W - 14f, 34f);
            weeklyLeft = MakeText(b.transform, "Left", new Vector2(0.5f, 0f), new Vector2(0, 8), TextAnchor.LowerCenter, 18, OpAmber); weeklyLeft.font = BoldFont(); weeklyLeft.rectTransform.sizeDelta = new Vector2(W - 10f, 26f);
        }

        /// <summary>The tile as the week stands: this week's picture and name, the time left, or every step won.</summary>
        void RefreshWeeklyButton()
        {
            if (weeklyBtn == null) return;
            var ev = Weekly.Now; var sp = UiSprite(ev.picture);
            if (sp != null) { weeklyPic.sprite = sp; float cover = Mathf.Max(182f / sp.rect.width, 182f / sp.rect.height); weeklyPic.rectTransform.sizeDelta = new Vector2(sp.rect.width * cover, sp.rect.height * cover); }
            weeklyName.text = ev.name.ToUpperInvariant(); Serif(weeklyName, 1f); weeklyName.color = OpInk;
            weeklyLeft.text = Weekly.AllDone ? "every step won" : Weekly.LeftText;
        }

        /// <summary>The event's sheet: what it is, the week's goal and its rewards, the way in.</summary>
        void ShowWeekly()
        {
            if (weeklySheet == null)
            {
                weeklySheet = new GameObject("Weekly", typeof(RectTransform), typeof(Image)); weeklySheet.transform.SetParent(canvas.transform, false); Stretch(weeklySheet); weeklySheet.GetComponent<Image>().color = new Color(0.02f, 0.02f, 0.03f, 1f);
                var body = new GameObject("Body", typeof(RectTransform)); body.transform.SetParent(weeklySheet.transform, false); Stretch(body); weeklyBody = body.transform;
            }
            foreach (Transform c in weeklyBody) Destroy(c.gameObject);
            var ev = Weekly.Now; int count = Weekly.Count, done = Weekly.StepsDone, last = ev.steps[ev.steps.Length - 1].at;
            Framed(weeklyBody, ev.picture, "campaign_normandy", new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1080, 820), new Color(0.02f, 0.02f, 0.03f, 1f));
            MakeGhost(weeklyBody, "BACK", new Vector2(0f, 1f), new Vector2(130, -95), new Vector2(200, 80), 26, () => weeklySheet.SetActive(false));
            var ey = MakeText(weeklyBody, "Eyebrow", new Vector2(0.5f, 1f), new Vector2(0, -690), TextAnchor.MiddleCenter, 26, OpAmber); ey.text = Spaced("WEEKLY EVENT · " + Weekly.LeftText.ToUpperInvariant()); ey.font = LabelFont(); ey.rectTransform.sizeDelta = new Vector2(1000, 40);
            var ti = MakeText(weeklyBody, "Title", new Vector2(0.5f, 1f), new Vector2(0, -770), TextAnchor.MiddleCenter, 100, OpInk); ti.text = ev.name.ToUpperInvariant(); Engrave(ti); ti.verticalOverflow = VerticalWrapMode.Overflow;
            var chip = MakeChip(weeklyBody, "Place", new Vector2(0.5f, 1f), new Vector2(0, -895), 860f, OpAmber, new Color(0.06f, 0.07f, 0.09f, 0.8f)); chip.text = Weekly.Place(ev);
            var line = MakeText(weeklyBody, "Rule", new Vector2(0.5f, 1f), new Vector2(0, -960), TextAnchor.UpperCenter, 32, new Color(0.86f, 0.84f, 0.8f)); line.text = ev.line; line.rectTransform.sizeDelta = new Vector2(900, 100); Fit(line, 22);
            // the week's goal: the count against the last step, and the three steps with their rewards
            var card = MakeCard(weeklyBody, "Goal", new Vector2(0.5f, 1f), new Vector2(0, -1090), new Vector2(940, 400), new Color(0.06f, 0.07f, 0.09f, 0.9f), 0.16f); card.rectTransform.pivot = new Vector2(0.5f, 1f);
            var t = card.transform;
            var gh = MakeText(t, "Head", new Vector2(0.5f, 1f), new Vector2(0, -26), TextAnchor.UpperCenter, 20, OpDim); gh.text = Spaced("THIS WEEK'S GOAL"); gh.font = LabelFont(); gh.rectTransform.sizeDelta = new Vector2(880, 30);
            var gt = MakeText(t, "Goal", new Vector2(0.5f, 1f), new Vector2(0, -62), TextAnchor.UpperCenter, 34, OpInk); gt.text = ev.goal; gt.font = BoldFont(); gt.rectTransform.sizeDelta = new Vector2(880, 46);
            var nv = MakeText(t, "Count", new Vector2(0.5f, 1f), new Vector2(0, -112), TextAnchor.UpperCenter, 56, OpAmber); nv.text = Mathf.Min(count, last).ToString("N0", En) + " / " + last.ToString("N0", En); Serif(nv); nv.rectTransform.sizeDelta = new Vector2(880, 66);
            var bar = MakeImage(t, "Bar", new Vector2(0.5f, 1f), new Vector2(0, -196), new Vector2(860, 10), new Color(0.18f, 0.18f, 0.2f)); bar.sprite = Rounded(); bar.type = Image.Type.Sliced; bar.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var fill = MakeImage(t, "Fill", new Vector2(0f, 1f), new Vector2(40, -196), new Vector2(Mathf.Max(10f, 860f * Mathf.Clamp01(count / (float)last)), 10), OpAmber); fill.sprite = Rounded(); fill.type = Image.Type.Sliced; fill.rectTransform.pivot = new Vector2(0f, 0.5f);
            for (int i = 0; i < ev.steps.Length; i++)
            {
                var st = ev.steps[i]; bool got = i < done; float x = -300f + i * 300f;
                RewardIcon(t, new Vector2(x, -50f), 74f, st.kind, st.id);   // the steps under the bar (the card is anchored at its middle)
                var at = MakeText(t, "At", new Vector2(0.5f, 0.5f), new Vector2(x, -112f), TextAnchor.MiddleCenter, 22, got ? OpAmber : OpDim); at.text = Spaced((got ? "WON · " : "AT ") + st.at.ToString("N0", En)); at.font = LabelFont(); at.rectTransform.sizeDelta = new Vector2(290, 30);
                var rl = MakeText(t, "Reward", new Vector2(0.5f, 0.5f), new Vector2(x, -150f), TextAnchor.MiddleCenter, 24, got ? OpInk : new Color(0.8f, 0.78f, 0.74f)); rl.text = Weekly.Describe(st); rl.font = BoldFont(); rl.rectTransform.sizeDelta = new Vector2(290, 32); Fit(rl, 16);
            }
            var nx = MakeText(weeklyBody, "Next", new Vector2(0.5f, 1f), new Vector2(0, -1530), TextAnchor.MiddleCenter, 22, OpDim); nx.text = Spaced("NEXT WEEK · " + Weekly.Next.name.ToUpperInvariant()); nx.font = LabelFont(); nx.rectTransform.sizeDelta = new Vector2(900, 32);
            MakePrimary(weeklyBody, "INTO THE NIGHT", new Vector2(0.5f, 0f), new Vector2(0, 170), new Vector2(880, 140), 44, () => { weeklySheet.SetActive(false); OnWeekly?.Invoke(); });
            weeklySheet.transform.SetAsLastSibling(); if (curtain != null) curtain.transform.SetAsLastSibling(); weeklySheet.SetActive(true);
        }
    }
}

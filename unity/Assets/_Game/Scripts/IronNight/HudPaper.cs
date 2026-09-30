using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace IronNight
{
    /// <summary>
    /// The morning paper's page (Battle.MorningPaper writes it): aged newsprint a little askew on the dark, the masthead in
    /// the display face over a double rule and the dateline, the headline in heavy condensed capitals, the sub, the night's
    /// picture printed in black and white through photo mode's look, its caption, the story, and a war bonds advertisement
    /// in a ruled box. THE MORNING PAPER on the end sheet opens it; SAVE (SHARE on a phone) keeps the page like a photo,
    /// CLOSE goes back to the end sheet.
    /// </summary>
    public partial class Hud
    {
        Battle.PaperStory paperStory; GameObject paperBtn, paperSheet, paperButtons; static Material paperPrint;
        static readonly Color Ink = new Color(0.1f, 0.09f, 0.08f), Newsprint = new Color(0.86f, 0.83f, 0.75f);

        /// <summary>The night's paper, ready as the end sheet comes up; its button appears on the end sheet.</summary>
        public void SetPaper(Battle.PaperStory story)
        {
            paperStory = story;
            if (paperBtn == null)
            {
                paperBtn = MakeGhost(endSheet.transform, "The morning paper", new Vector2(0.5f, 0.5f), new Vector2(0, -590), new Vector2(880, 100), 30, ShowPaper);
                paperBtn.transform.Find("Label").GetComponent<Text>().text = Spaced("THE MORNING PAPER");
            }
            paperBtn.SetActive(story != null);
        }

        void ShowPaper()
        {
            var s = paperStory; if (s == null) return;
            if (paperSheet != null) Destroy(paperSheet);
            paperSheet = new GameObject("Paper", typeof(RectTransform), typeof(Image)); paperSheet.transform.SetParent(canvas.transform, false); Stretch(paperSheet);
            if (curtain != null) paperSheet.transform.SetSiblingIndex(curtain.transform.GetSiblingIndex());
            paperSheet.GetComponent<Image>().color = new Color(0.02f, 0.02f, 0.025f, 1f);
            var sh = MakeImage(paperSheet.transform, "Shadow", new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(1040f, 1820f), new Color(0f, 0f, 0f, 0.7f)); sh.sprite = Shadow(); sh.type = Image.Type.Sliced; sh.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -1.2f);
            var page = MakeImage(paperSheet.transform, "Page", new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(980f, 1760f), Newsprint); page.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -1.2f);
            var age = MakeImage(page.transform, "Age", new Vector2(0.5f, 0f), Vector2.zero, new Vector2(980f, 900f), new Color(0.45f, 0.36f, 0.2f, 0.22f)); age.sprite = Lightswarm.ProceduralSprites.GradientDown(128, 1.4f);   // yellowed toward the foot
            var p = page.transform;

            var mast = MakeText(p, "Masthead", new Vector2(0.5f, 1f), new Vector2(0f, -36f), TextAnchor.UpperCenter, 70, Ink); mast.font = DisplayFont(); mast.text = s.masthead; mast.rectTransform.sizeDelta = new Vector2(920f, 90f); Fit(mast, 40);
            Rule(p, -138f, 3f); Rule(p, -146f, 1f);
            var dl = MakeText(p, "Dateline", new Vector2(0.5f, 1f), new Vector2(0f, -156f), TextAnchor.UpperCenter, 20, Ink); dl.font = LabelFont(); dl.text = Spaced(s.dateline); dl.rectTransform.sizeDelta = new Vector2(920f, 32f); Fit(dl, 12);
            Rule(p, -196f, 1f);
            var hl = MakeText(p, "Headline", new Vector2(0.5f, 1f), new Vector2(0f, -212f), TextAnchor.UpperCenter, 92, Ink); hl.font = LabelBoldFont(); hl.text = s.headline; hl.lineSpacing = 0.86f; hl.rectTransform.sizeDelta = new Vector2(920f, 250f); Fit(hl, 44);
            var sub = MakeText(p, "Sub", new Vector2(0.5f, 1f), new Vector2(0f, -470f), TextAnchor.UpperCenter, 32, new Color(0.22f, 0.2f, 0.17f)); sub.font = BoldFont(); sub.text = s.sub; sub.rectTransform.sizeDelta = new Vector2(900f, 90f); Fit(sub, 22);

            // the picture, printed in black and white, and its caption
            var frame = MakeImage(p, "PictureFrame", new Vector2(0.5f, 1f), new Vector2(0f, -570f), new Vector2(904f, 564f), Ink);
            var pic = new GameObject("Picture", typeof(RectTransform), typeof(RawImage)); pic.transform.SetParent(frame.transform, false);
            var pr = (RectTransform)pic.transform; pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f); pr.sizeDelta = new Vector2(896f, 556f);
            var raw = pic.GetComponent<RawImage>(); raw.raycastTarget = false; raw.texture = s.picture;
            if (paperPrint == null) { var shd = Resources.Load<Shader>("Shaders/PhotoLook"); if (shd != null) { paperPrint = new Material(shd); paperPrint.SetFloat("_Look", 2f); paperPrint.SetFloat("_Grain", 2f); } }
            if (paperPrint != null) raw.material = paperPrint;
            if (s.picture != null) { float a = (float)s.picture.width / s.picture.height, want = 896f / 556f; raw.uvRect = a > want ? new Rect((1f - want / a) * 0.5f, 0f, want / a, 1f) : new Rect(0f, (1f - a / want) * 0.5f, 1f, a / want); }
            var cap = MakeText(p, "Caption", new Vector2(0.5f, 1f), new Vector2(0f, -1144f), TextAnchor.UpperLeft, 22, new Color(0.25f, 0.23f, 0.2f)); cap.font = BoldFont(); cap.text = s.caption; cap.rectTransform.sizeDelta = new Vector2(900f, 34f); Fit(cap, 14);
            Rule(p, -1186f, 1f);
            var body = MakeText(p, "Story", new Vector2(0f, 1f), new Vector2(40f, -1204f), TextAnchor.UpperLeft, 27, new Color(0.14f, 0.13f, 0.11f)); body.font = DefaultFont(); body.text = s.body; body.lineSpacing = 1.06f; body.rectTransform.sizeDelta = new Vector2(570f, 520f); Fit(body, 16);
            // a war bonds advertisement in a ruled box, the way the papers carried them
            var box = MakeImage(p, "Ad", new Vector2(1f, 1f), new Vector2(-40f, -1210f), new Vector2(300f, 500f), Ink); box.rectTransform.pivot = new Vector2(1f, 1f);
            var inner = MakeImage(box.transform, "Inside", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(292f, 492f), Newsprint); inner.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var ad1 = MakeText(inner.transform, "Buy", new Vector2(0.5f, 1f), new Vector2(0f, -30f), TextAnchor.UpperCenter, 46, Ink); ad1.font = LabelBoldFont(); ad1.text = "BUY\nWAR\nBONDS"; ad1.lineSpacing = 0.9f; ad1.rectTransform.sizeDelta = new Vector2(270f, 200f);
            var ad2 = MakeText(inner.transform, "Line", new Vector2(0.5f, 1f), new Vector2(0f, -250f), TextAnchor.UpperCenter, 22, Ink); ad2.font = BoldFont(); ad2.text = "Every star you win this season brings the next reward closer."; ad2.rectTransform.sizeDelta = new Vector2(250f, 150f); Fit(ad2, 14);
            var ad3 = MakeText(inner.transform, "Foot", new Vector2(0.5f, 0f), new Vector2(0f, 26f), TextAnchor.LowerCenter, 20, Ink); ad3.font = LabelFont(); ad3.text = Spaced("AT YOUR DEPOT"); ad3.rectTransform.sizeDelta = new Vector2(260f, 30f);

            paperButtons = new GameObject("Buttons", typeof(RectTransform)); paperButtons.transform.SetParent(paperSheet.transform, false); Stretch(paperButtons);
            MakeGhost(paperButtons.transform, "Close", new Vector2(0.5f, 0f), new Vector2(-230f, 90f), new Vector2(400f, 100f), 30, () => { Destroy(paperSheet); paperSheet = null; });
            MakePrimary(paperButtons.transform, Application.isMobilePlatform ? "Share" : "Save", new Vector2(0.5f, 0f), new Vector2(230f, 90f), new Vector2(400f, 100f), 32, () => StartCoroutine(KeepPaper()));
            Sfx.Whoosh();
        }

        void Rule(Transform page, float y, float h) => MakeImage(page, "Rule", new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(920f, h), Ink);

        /// <summary>The page kept like a photo: the buttons hidden for a frame, the screen taken, saved (and on a phone offered to share).</summary>
        IEnumerator KeepPaper()
        {
            if (paperButtons == null) yield break;
            paperButtons.SetActive(false);
            yield return new WaitForEndOfFrame();
            var shot = ScreenCapture.CaptureScreenshotAsTexture(); var jpg = shot.EncodeToJPG(90); Destroy(shot);
            if (paperButtons != null) paperButtons.SetActive(true);
            string note = Battle.PhotoKeep(jpg, "IronNight_paper_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".jpg", out bool share);
            Ads.Say(note); Sfx.Shutter();
#if UNITY_ANDROID && !UNITY_EDITOR
            if (share) { try { PhotoGallery.Share(); } catch (System.Exception e) { Debug.LogWarning("Paper not shared: " + e.Message); } }
#endif
        }
    }
}

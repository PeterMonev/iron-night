using UnityEngine;
using UnityEngine.UI;

namespace IronNight
{
    /// <summary>
    /// The moment of the night: the best killcam's pictures shown as a newsreel over the end sheet. They dissolve one into
    /// the next under a slow push in, toned like wartime film, THE MOMENT OF THE NIGHT above and the killcam's own words
    /// below; a tap, or the end of the reel, lets the end sheet through.
    /// </summary>
    public partial class Hud
    {
        GameObject momentSheet; MomentReel momentReel; Text momentEyebrowT, momentTitleT, momentSubT; static Material sepiaMaterial;

        /// <summary>Shows the night's moment: n pictures, and the killcam's eyebrow, title and line.</summary>
        public void ShowMoment(RenderTexture[] frames, int n, string eyebrow, string title, string sub)
        {
            if (frames == null || n < 2) return;
            if (momentSheet == null) BuildMoment();
            momentEyebrowT.text = Spaced(eyebrow); momentTitleT.text = title; momentSubT.text = sub;
            momentSheet.SetActive(true); momentSheet.transform.SetAsLastSibling(); if (curtain != null) curtain.transform.SetAsLastSibling();
            momentReel.Play(frames, n); Sfx.Click();
        }

        void BuildMoment()
        {
            momentSheet = new GameObject("Moment", typeof(RectTransform), typeof(Image), typeof(Button)); momentSheet.transform.SetParent(canvas.transform, false); Stretch(momentSheet);
            momentSheet.GetComponent<Image>().color = new Color(0.01f, 0.01f, 0.012f, 1f);
            var sh = Resources.Load<Shader>("Shaders/UISepia"); if (sepiaMaterial == null && sh != null) sepiaMaterial = new Material(sh);
            var shots = new RawImage[2];
            for (int i = 0; i < 2; i++)
            {
                var g = new GameObject(i == 0 ? "Shot A" : "Shot B", typeof(RectTransform), typeof(RawImage)); g.transform.SetParent(momentSheet.transform, false); Stretch(g);
                shots[i] = g.GetComponent<RawImage>(); shots[i].raycastTarget = false; if (sepiaMaterial != null) shots[i].material = sepiaMaterial;
            }
            // the words: a dark fade under them, THE MOMENT OF THE NIGHT at the head, the killcam's lines at the foot
            var top = MakeImage(momentSheet.transform, "Head fade", new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1400f, 420f), new Color(0f, 0f, 0f, 0.75f)); top.sprite = Lightswarm.ProceduralSprites.GradientDown(64, 1.1f); top.rectTransform.pivot = new Vector2(0.5f, 1f); top.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 180f); top.rectTransform.anchoredPosition = new Vector2(0f, -420f);
            var foot = MakeImage(momentSheet.transform, "Foot fade", new Vector2(0.5f, 0f), Vector2.zero, new Vector2(1400f, 820f), new Color(0f, 0f, 0f, 0.9f)); foot.sprite = Lightswarm.ProceduralSprites.GradientDown(64, 1.1f); foot.rectTransform.pivot = new Vector2(0.5f, 0f);
            var head = MakeText(momentSheet.transform, "Head", new Vector2(0.5f, 1f), new Vector2(0, -150), TextAnchor.MiddleCenter, 30, new Color(0.96f, 0.68f, 0.24f)); head.text = Spaced("THE MOMENT OF THE NIGHT"); head.font = LabelFont(); head.rectTransform.sizeDelta = new Vector2(900, 44);
            var rule = MakeImage(momentSheet.transform, "Rule", new Vector2(0.5f, 1f), new Vector2(0, -196), new Vector2(110, 4), new Color(0.96f, 0.68f, 0.24f, 0.9f)); rule.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            momentEyebrowT = MakeText(momentSheet.transform, "Eyebrow", new Vector2(0.5f, 0f), new Vector2(0, 476), TextAnchor.MiddleCenter, 26, new Color(0.96f, 0.68f, 0.24f)); momentEyebrowT.font = LabelFont(); momentEyebrowT.rectTransform.sizeDelta = new Vector2(1000, 40);
            momentTitleT = MakeText(momentSheet.transform, "Title", new Vector2(0.5f, 0f), new Vector2(0, 322), TextAnchor.MiddleCenter, 150, new Color(0.93f, 0.91f, 0.86f)); Engrave(momentTitleT); momentTitleT.rectTransform.sizeDelta = new Vector2(1000, 140);
            momentSubT = MakeText(momentSheet.transform, "Sub", new Vector2(0.5f, 0f), new Vector2(0, 272), TextAnchor.MiddleCenter, 28, new Color(0.8f, 0.78f, 0.74f)); momentSubT.font = LabelFont(); momentSubT.rectTransform.sizeDelta = new Vector2(1000, 40); Fit(momentSubT, 18);
            var tap = MakeText(momentSheet.transform, "Tap", new Vector2(0.5f, 0f), new Vector2(0, 130), TextAnchor.MiddleCenter, 22, new Color(0.62f, 0.6f, 0.56f)); tap.text = Spaced("TAP TO CONTINUE"); tap.font = LabelFont(); tap.rectTransform.sizeDelta = new Vector2(600, 32);
            momentReel = momentSheet.AddComponent<MomentReel>(); momentReel.a = shots[0]; momentReel.b = shots[1];
            momentSheet.GetComponent<Button>().onClick.AddListener(() => momentReel.Close());
            momentSheet.SetActive(false);
        }
    }

    /// <summary>The reel: each picture held a moment, dissolving into the next, the whole pushing in slowly; then away.</summary>
    public class MomentReel : MonoBehaviour
    {
        public RawImage a, b;
        RenderTexture[] frames; int n; float t, closeT = -1f; CanvasGroup group;
        const float Each = 0.95f, Blend = 0.45f, Out = 0.45f;
        static readonly bool Hold = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--momenthold") >= 0;   // test switch: the reel stays until tapped, its last picture held

        public void Play(RenderTexture[] f, int count) { frames = f; n = count; t = 0f; closeT = -1f; Show(0f); }
        public void Close() { if (closeT < 0f) closeT = 0f; }

        void Update()
        {
            if (frames == null) return;
            float dt = Time.unscaledDeltaTime; t += dt;
            if (closeT < 0f && !Hold && t >= n * Each + 0.9f) Close();
            if (closeT >= 0f)
            {
                closeT += dt;
                if (group == null) { group = GetComponent<CanvasGroup>(); if (group == null) group = gameObject.AddComponent<CanvasGroup>(); }
                group.alpha = Mathf.Clamp01(1f - closeT / Out);
                if (closeT >= Out) { gameObject.SetActive(false); group.alpha = 1f; frames = null; return; }
            }
            Show(t);
        }

        void Show(float at)
        {
            int k = Mathf.Min(n - 1, Mathf.FloorToInt(at / Each)); float into = at - k * Each;
            float mix = k < n - 1 ? Mathf.Clamp01((into - (Each - Blend)) / Blend) : 0f;
            a.texture = frames[k]; b.texture = frames[Mathf.Min(n - 1, k + 1)]; b.color = new Color(1f, 1f, 1f, mix * mix * (3f - 2f * mix));
            float s = 1f + 0.09f * Mathf.Clamp01(at / (n * Each + 0.9f)); a.rectTransform.localScale = b.rectTransform.localScale = new Vector3(s, s, 1f);   // a slow push in
        }
    }
}

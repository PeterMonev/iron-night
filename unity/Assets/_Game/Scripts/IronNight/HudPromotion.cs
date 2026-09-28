using UnityEngine;
using UnityEngine.UI;

namespace IronNight
{
    /// <summary>
    /// A promotion's ceremony over the title: the new insignia large under a warm light with a glint running across it,
    /// PROMOTED, the rank in engraved gold, the nights it took, what it pays, the next rank ahead, and the fanfare. SALUTE
    /// closes it and the hangar's theme comes back.
    /// </summary>
    public partial class Hud
    {
        GameObject promoSheet;

        void ShowPromotion(Depot.RankStep r)
        {
            if (promoSheet != null) Destroy(promoSheet);
            promoSheet = new GameObject("Promotion", typeof(RectTransform), typeof(Image)); promoSheet.transform.SetParent(canvas.transform, false); Stretch(promoSheet);
            promoSheet.GetComponent<Image>().color = new Color(0.015f, 0.015f, 0.02f, 1f);   // fully: in linear light even 3% of the bright title shows
            var t = promoSheet.transform;
            var glow = MakeImage(t, "Glow", new Vector2(0.5f, 0.5f), new Vector2(0f, 330f), new Vector2(980f, 980f), new Color(1f, 0.72f, 0.3f, 0.3f)); glow.sprite = Lightswarm.ProceduralSprites.Glow(64, 0.12f); glow.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var beam = MakeImage(t, "Beam", new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(620f, 1100f), new Color(1f, 0.86f, 0.6f, 0.08f)); beam.sprite = Lightswarm.ProceduralSprites.GradientDown(64, 1.4f); beam.rectTransform.pivot = new Vector2(0.5f, 1f); beam.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 180f); beam.rectTransform.anchoredPosition = new Vector2(0f, -1100f);
            // the insignia: its cell of the sheet, large, with a glint crossing it now and then
            var sheet = Resources.Load<Texture2D>("UI/rank_insignia"); int cell = Mathf.Max(0, System.Array.IndexOf(Depot.Ranks, r) - 1);
            var ins = MakeImage(t, "Insignia", new Vector2(0.5f, 0.5f), new Vector2(0f, 330f), new Vector2(460f, 460f), Color.white); ins.rectTransform.pivot = new Vector2(0.5f, 0.5f); ins.preserveAspect = true;
            if (sheet != null) ins.sprite = UiSprite("rank_insignia", new Rect(cell * sheet.height, 0f, sheet.height, sheet.height));
            var mask = new GameObject("GlintMask", typeof(RectTransform), typeof(RectMask2D)); mask.transform.SetParent(ins.transform, false); var mrt = mask.GetComponent<RectTransform>(); mrt.anchorMin = Vector2.zero; mrt.anchorMax = Vector2.one; mrt.offsetMin = mrt.offsetMax = Vector2.zero;
            var glint = MakeImage(mask.transform, "Glint", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90f, 700f), new Color(1f, 1f, 1f, 0.35f)); glint.sprite = Lightswarm.ProceduralSprites.Glow(64, 0.1f); glint.rectTransform.pivot = new Vector2(0.5f, 0.5f); glint.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -28f);
            ins.gameObject.AddComponent<Glint>().band = glint.rectTransform;
            ins.gameObject.AddComponent<Pop>();
            // the words
            var ey = MakeText(t, "Eyebrow", new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), TextAnchor.MiddleCenter, 30, new Color(0.96f, 0.68f, 0.24f)); ey.text = Spaced("PROMOTED"); ey.font = LabelFont(); ey.rectTransform.sizeDelta = new Vector2(900, 44);
            var ti = MakeText(t, "Rank", new Vector2(0.5f, 0.5f), new Vector2(0f, -80f), TextAnchor.MiddleCenter, 130, new Color(0.93f, 0.91f, 0.86f)); ti.text = r.name.ToUpperInvariant(); Engrave(ti); ti.rectTransform.sizeDelta = new Vector2(1000, 150);
            var ln = MakeText(t, "Line", new Vector2(0.5f, 0.5f), new Vector2(0f, -170f), TextAnchor.MiddleCenter, 30, new Color(0.72f, 0.7f, 0.66f)); ln.text = Depot.NightsFought + " nights at the front"; ln.rectTransform.sizeDelta = new Vector2(900, 40);
            if (!string.IsNullOrEmpty(r.kind))
            {
                var card = MakeCard(t, "Reward", new Vector2(0.5f, 0.5f), new Vector2(0f, -300f), new Vector2(620f, 150f), new Color(0.06f, 0.07f, 0.09f, 0.92f), 0.2f); card.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                card.transform.Find("Edge").GetComponent<Image>().color = new Color(1f, 0.8f, 0.35f, 0.45f);
                RewardIcon(card.transform, new Vector2(-200f, 0f), 96f, r.kind, null);
                var rh = MakeText(card.transform, "Head", new Vector2(0.5f, 0.5f), new Vector2(60f, 26f), TextAnchor.MiddleLeft, 20, OpDim); rh.text = Spaced("WITH THE RANK"); rh.font = LabelFont(); rh.rectTransform.sizeDelta = new Vector2(380, 30);
                var rv = MakeText(card.transform, "What", new Vector2(0.5f, 0.5f), new Vector2(60f, -18f), TextAnchor.MiddleLeft, 36, OpInk); rv.text = Weekly.Describe(new Weekly.Step { kind = r.kind, amount = r.amount }); rv.font = BoldFont(); rv.rectTransform.sizeDelta = new Vector2(380, 50); Fit(rv, 22);
            }
            var next = Depot.NextRank;
            var nx = MakeText(t, "Next", new Vector2(0.5f, 0.5f), new Vector2(0f, -430f), TextAnchor.MiddleCenter, 22, OpDim); nx.font = LabelFont(); nx.rectTransform.sizeDelta = new Vector2(900, 34);
            nx.text = next != null ? Spaced("NEXT · " + next.name.ToUpperInvariant() + " AT " + next.nights + " NIGHTS") : Spaced("THE HIGHEST RANK THERE IS");
            MakePrimary(t, "Salute", new Vector2(0.5f, 0f), new Vector2(0, 190), new Vector2(760, 140), 44, () => { Destroy(promoSheet); promoSheet = null; Sfx.Theme("menu"); RefreshRewardButtons(); });
            promoSheet.transform.SetAsLastSibling(); if (curtain != null) curtain.transform.SetAsLastSibling();
            Sfx.Theme("dawn");
        }
    }

    /// <summary>A band of light crossing a picture now and then, like a glint on polished metal.</summary>
    public class Glint : MonoBehaviour
    {
        public RectTransform band; float t = -0.6f;
        void Update()
        {
            if (band == null) return;
            t += Time.unscaledDeltaTime; if (t > 3.2f) t = 0f;
            float k = Mathf.Clamp01(t / 0.9f); band.anchoredPosition = new Vector2(Mathf.Lerp(-420f, 420f, k * k * (3f - 2f * k)), 0f);
        }
    }
}

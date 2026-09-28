using UnityEngine;
using UnityEngine.UI;

namespace IronNight
{
    /// <summary>
    /// The lucky dispatch's tile on the title (at the left under the week's event) and its sheet: a wheel of eight
    /// prizes in olive and gunmetal on a brass rim, a gold pointer at its top, SPIN (free once a day, then twice for an
    /// ad). The wheel runs four turns and slows onto the prize, ticking past each place; the prize is named under it.
    /// </summary>
    public partial class Hud
    {
        GameObject dispatchBtn, dispatchSheet; Text dispatchLine; RectTransform wheel; Text dispatchResult; GameObject spinBtn; Text spinLabel, spinNote;
        static Sprite wheelSprite, pointerSprite;

        void BuildDispatchButton()
        {
            const float W = 190f;
            var b = MakeButton(titleSheet.transform, "", new Vector2(0f, 1f), new Vector2(135, -685), new Vector2(W, W), 20, ShowDispatch); dispatchBtn = b;
            b.GetComponent<Image>().color = new Color(0.05f, 0.055f, 0.07f, 0.85f); b.transform.Find("Label").gameObject.SetActive(false);
            var edge = MakeImage(b.transform, "Edge", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(W, W), new Color(1f, 0.8f, 0.35f, 0.4f)); edge.sprite = Outline(); edge.type = Image.Type.Sliced; edge.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var glow = MakeImage(b.transform, "Glow", new Vector2(0.5f, 0.5f), new Vector2(0, 8), new Vector2(170, 150), new Color(1f, 0.72f, 0.3f, 0.18f)); glow.sprite = Lightswarm.ProceduralSprites.Glow(64, 0.1f); glow.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var wi = MakeImage(b.transform, "Wheel", new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(112, 112), Color.white); wi.sprite = WheelSprite(); wi.rectTransform.pivot = new Vector2(0.5f, 0.5f); wi.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 12f);
            var pin = MakeImage(b.transform, "Pointer", new Vector2(0.5f, 0.5f), new Vector2(0, 66), new Vector2(26, 22), Color.white); pin.sprite = PointerSprite(); pin.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var ey = MakeText(b.transform, "Eyebrow", new Vector2(0.5f, 1f), new Vector2(0, -8), TextAnchor.UpperCenter, 17, OpAmber); ey.text = Spaced("LUCKY DISPATCH"); ey.font = LabelFont(); ey.rectTransform.sizeDelta = new Vector2(W - 10f, 26f); Fit(ey, 12);
            dispatchLine = MakeText(b.transform, "Line", new Vector2(0.5f, 0f), new Vector2(0, 8), TextAnchor.LowerCenter, 18, OpAmber); dispatchLine.font = BoldFont(); dispatchLine.rectTransform.sizeDelta = new Vector2(W - 10f, 26f);
        }

        /// <summary>The tile as the day stands: a free spin, a spin for an ad, or come back tomorrow.</summary>
        void RefreshDispatchButton()
        {
            if (dispatchBtn == null) return;
            dispatchLine.text = Dispatch.FreeSpin ? "free spin" : Dispatch.AdSpin ? "spin · ad" : "tomorrow";
            dispatchLine.color = Dispatch.FreeSpin ? new Color(0.55f, 0.85f, 0.5f) : OpAmber;
        }

        /// <summary>The wheel's sheet.</summary>
        void ShowDispatch()
        {
            if (dispatchSheet == null) BuildDispatchSheet();
            dispatchResult.text = ""; RefreshSpin();
            dispatchSheet.transform.SetAsLastSibling(); if (curtain != null) curtain.transform.SetAsLastSibling();
            dispatchSheet.SetActive(true);
        }

        void BuildDispatchSheet()
        {
            dispatchSheet = new GameObject("Dispatch", typeof(RectTransform), typeof(Image)); dispatchSheet.transform.SetParent(canvas.transform, false); Stretch(dispatchSheet);
            dispatchSheet.GetComponent<Image>().color = new Color(0.02f, 0.02f, 0.03f, 1f);
            var t = dispatchSheet.transform;
            MakeGhost(t, "BACK", new Vector2(0f, 1f), new Vector2(130, -95), new Vector2(200, 80), 26, () => { if (!WheelTurning) { dispatchSheet.SetActive(false); RefreshDispatchButton(); } });
            var ey = MakeText(t, "Eyebrow", new Vector2(0.5f, 1f), new Vector2(0, -190), TextAnchor.MiddleCenter, 26, OpAmber); ey.text = Spaced("FROM THE QUARTERMASTER · EVERY DAY"); ey.font = LabelFont(); ey.rectTransform.sizeDelta = new Vector2(1000, 40);
            var ti = MakeText(t, "Title", new Vector2(0.5f, 1f), new Vector2(0, -270), TextAnchor.MiddleCenter, 100, OpInk); ti.text = "LUCKY DISPATCH"; Engrave(ti); ti.rectTransform.sizeDelta = new Vector2(1040, 120); Fit(ti, 60);
            // the wheel under a warm light, its eight prizes turning with it, the pointer fixed over its top
            var glow = MakeImage(t, "Glow", new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(1100, 1100), new Color(1f, 0.72f, 0.3f, 0.16f)); glow.sprite = Lightswarm.ProceduralSprites.Glow(64, 0.12f); glow.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var wi = MakeImage(t, "Wheel", new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(820, 820), Color.white); wi.sprite = WheelSprite(); wi.rectTransform.pivot = new Vector2(0.5f, 0.5f); wheel = wi.rectTransform;
            for (int i = 0; i < Dispatch.Prizes.Length; i++)
            {
                var p = Dispatch.Prizes[i];
                var place = new GameObject("Prize" + i, typeof(RectTransform)); place.transform.SetParent(wheel, false);
                var prt = place.GetComponent<RectTransform>(); prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 0.5f); prt.sizeDelta = Vector2.zero; prt.localRotation = Quaternion.Euler(0f, 0f, -(i + 0.5f) * 45f);   // its place's middle, clockwise from the top
                RewardIcon(place.transform, new Vector2(0f, 262f), 74f, p.kind, null);
                var lb = MakeText(place.transform, "Label", new Vector2(0.5f, 0.5f), new Vector2(0f, 176f), TextAnchor.MiddleCenter, 26, OpInk); lb.text = PrizeShort(p); lb.font = LabelBoldFont(); lb.rectTransform.pivot = new Vector2(0.5f, 0.5f); lb.rectTransform.sizeDelta = new Vector2(170f, 34f); Fit(lb, 16);
            }
            var hub = MakeImage(t, "Hub", new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(150, 150), GoldInk); hub.sprite = Lightswarm.ProceduralSprites.Glow(64, 0.85f); hub.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var star = MakeImage(t, "Star", new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(84, 84), new Color(0.12f, 0.1f, 0.06f)); star.sprite = StarSprite(); star.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var pin = MakeImage(t, "Pointer", new Vector2(0.5f, 0.5f), new Vector2(0, 575), new Vector2(84, 70), Color.white); pin.sprite = PointerSprite(); pin.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            dispatchResult = MakeText(t, "Result", new Vector2(0.5f, 0.5f), new Vector2(0, -340), TextAnchor.MiddleCenter, 54, GoldInk); dispatchResult.font = BoldFont(); dispatchResult.rectTransform.sizeDelta = new Vector2(1000, 70);
            spinBtn = MakePrimary(t, "Spin", new Vector2(0.5f, 0.5f), new Vector2(0, -490), new Vector2(760, 140), 44, Spin);
            spinLabel = spinBtn.transform.Find("Label").GetComponent<Text>();
            spinNote = MakeText(t, "Note", new Vector2(0.5f, 0.5f), new Vector2(0, -610), TextAnchor.MiddleCenter, 26, OpDim); spinNote.rectTransform.sizeDelta = new Vector2(900, 40); Fit(spinNote, 18);
            dispatchSheet.SetActive(false);
        }

        static void Buzz()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (PlayerPrefs.GetInt("vibe", 1) == 1) Handheld.Vibrate();
#endif
        }

        static string PrizeShort(Dispatch.Prize p) =>
            p.kind == "points" ? p.amount + " PTS" : p.kind == "xp" ? p.amount + " XP" : p.kind == "gold" ? p.amount + " GOLD" : p.kind == "officer" ? "OFFICER'S" : "SUPPLY";

        bool WheelTurning => wheel != null && wheel.GetComponent<WheelTurn>() != null;

        /// <summary>The button as the day stands: free, for an ad (and how many are left), or nothing more today.</summary>
        void RefreshSpin()
        {
            bool free = Dispatch.FreeSpin, ad = Dispatch.AdSpin;
            spinBtn.SetActive(free || ad);
            spinLabel.text = Spaced(free ? "SPIN · FREE" : "SPIN AGAIN · WATCH AN AD");
            spinNote.text = free ? "One spin a day for nothing, two more for a short video each." : ad ? Dispatch.AdSpinsLeft + (Dispatch.AdSpinsLeft == 1 ? " more spin" : " more spins") + " today for a short video." : "That is all for today · the quartermaster has more tomorrow.";
        }

        void Spin()
        {
            if (WheelTurning) return;
            if (Dispatch.FreeSpin) TurnTo(Dispatch.Draw());
            else if (Dispatch.AdSpin) Ads.Rewarded("dispatch", () => TurnTo(Dispatch.Draw()));
        }

        /// <summary>Four turns and a slow stop on the prize's place (a little off its middle, as a real wheel stops); then it is given.</summary>
        void TurnTo(int prize)
        {
            dispatchResult.text = ""; spinBtn.SetActive(false);
            float from = wheel.localEulerAngles.z % 360f, target = (prize + 0.5f) * 45f + Random.Range(-15f, 15f);
            var turn = wheel.gameObject.AddComponent<WheelTurn>(); turn.from = from; turn.to = target + 360f * 4f + (from > target ? 360f : 0f);
            turn.done = () =>
            {
                Dispatch.Spun(prize); var p = Dispatch.Prizes[prize]; Sfx.Pickup(); Buzz();
                dispatchResult.text = RewardLine(p.kind, p.amount, null); dispatchResult.gameObject.AddComponent<Pop>();
                RefreshSpin(); RefreshDispatchButton(); RefreshRewardButtons();
            };
        }

        /// <summary>The wheel's face: eight places in olive and gunmetal, brass lines between them, a brass rim with rivets.</summary>
        static Sprite WheelSprite()
        {
            if (wheelSprite != null) return wheelSprite;
            const int S = 512; float c = (S - 1) / 2f, R = S / 2f - 2f; var tex = new Texture2D(S, S, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
            var olive = new Color(0.2f, 0.22f, 0.14f); var steel = new Color(0.1f, 0.11f, 0.12f); var brass = new Color(0.78f, 0.6f, 0.3f); var dark = new Color(0.36f, 0.26f, 0.12f);
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float dx = x - c, dy = y - c, r = Mathf.Sqrt(dx * dx + dy * dy); if (r > R + 1f) { tex.SetPixel(x, y, Color.clear); continue; }
                float a = Mathf.Repeat(Mathf.Atan2(dx, dy) * Mathf.Rad2Deg, 360f); int seg = Mathf.FloorToInt(a / 45f);   // clockwise from the top
                var col = seg % 2 == 0 ? olive : steel;
                col *= 0.82f + 0.3f * (1f - r / R);                                                                     // lit from the middle
                float line = Mathf.Abs(Mathf.DeltaAngle(a, Mathf.Round(a / 45f) * 45f)) * Mathf.Deg2Rad * r;           // distance to the nearest divider, in pixels
                if (line < 2.2f && r > 40f) col = Color.Lerp(brass, col, line / 2.2f);
                if (r > R * 0.9f) { float k = Mathf.InverseLerp(R * 0.9f, R, r); col = Color.Lerp(brass, dark, Mathf.Abs(k - 0.45f) * 1.6f); }   // the rim, rounded
                for (int n = 0; n < 32; n++) { float ra = n * 11.25f * Mathf.Deg2Rad; float rx = c + Mathf.Sin(ra) * R * 0.95f, ry = c + Mathf.Cos(ra) * R * 0.95f; float d = (x - rx) * (x - rx) + (y - ry) * (y - ry); if (d < 30f) col = Color.Lerp(new Color(1f, 0.9f, 0.62f), brass, d / 30f); }   // its rivets
                col.a = Mathf.Clamp01(R + 1f - r);
                tex.SetPixel(x, y, col);
            }
            tex.Apply(true);
            return wheelSprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>A gold pointer, point down.</summary>
        static Sprite PointerSprite()
        {
            if (pointerSprite != null) return pointerSprite;
            const int W = 96, H = 80; var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
            {
                float half = (W / 2f - 4f) * (y / (float)(H - 1)), d = Mathf.Abs(x - W / 2f + 0.5f) - half;   // wide at the top, a point at the bottom
                float a = Mathf.Clamp01(0.5f - d); var col = Color.Lerp(new Color(1f, 0.88f, 0.55f), new Color(0.72f, 0.5f, 0.2f), y / (float)H * 0.6f + (x > W / 2 ? 0.25f : 0f));
                col.a = a; tex.SetPixel(x, y, col);
            }
            tex.Apply();
            return pointerSprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 100f);
        }
    }

    /// <summary>The wheel turning: fast, then slowing onto its place (a cubic ease), ticking as each place goes past the pointer.</summary>
    public class WheelTurn : MonoBehaviour
    {
        public float from, to, secs = 4.2f; public System.Action done; float t; int lastTick = -1;
        void Update()
        {
            t += Time.unscaledDeltaTime; float k = Mathf.Clamp01(t / secs), e = 1f - Mathf.Pow(1f - k, 3f), z = Mathf.Lerp(from, to, e);
            transform.localRotation = Quaternion.Euler(0f, 0f, z);
            int tick = Mathf.FloorToInt(z / 45f); if (tick != lastTick) { if (lastTick >= 0) Sfx.Click(); lastTick = tick; }
            if (k >= 1f) { Destroy(this); done?.Invoke(); }
        }
    }
}

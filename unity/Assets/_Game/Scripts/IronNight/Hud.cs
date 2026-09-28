using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IronNight
{
    /// <summary>
    /// The overlay: night clock, platoon count, level bar, formation buttons, pause button, toast line, the three-card
    /// level-up sheet, the pause sheet and the end-of-assault sheet with the (mock) rewarded-ad button. Built in code with the legacy UI, English only.
    /// </summary>
    /// <summary>A button that gives a little under the finger and springs back.</summary>
    public class PressFeel : MonoBehaviour, UnityEngine.EventSystems.IPointerDownHandler, UnityEngine.EventSystems.IPointerUpHandler, UnityEngine.EventSystems.IPointerExitHandler
    {
        float want = 1f, at = 1f;
        public void OnPointerDown(UnityEngine.EventSystems.PointerEventData e) { want = 0.965f; }
        public void OnPointerUp(UnityEngine.EventSystems.PointerEventData e) { want = 1f; }
        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData e) { want = 1f; }
        void Update() { if (Mathf.Approximately(at, want)) return; at = Mathf.MoveTowards(at, want, Time.unscaledDeltaTime * 3.5f); transform.localScale = new Vector3(at, at, 1f); }
    }

    /// <summary>A title's face: each letter lit like struck brass (or pale stone) from its top down, and the letters set a
    /// little apart on a single line. Only the text's own glyphs are touched; a Shadow added after it copies them as they are.</summary>
    public class EngravedFace : BaseMeshEffect
    {
        public bool gold = true; public float tracking = 0.045f;   // the spacing, in ems
        static readonly Color GoldTop = new Color(1f, 0.93f, 0.74f), GoldFoot = new Color(0.8f, 0.55f, 0.21f), StoneTop = new Color(1f, 0.98f, 0.94f), StoneFoot = new Color(0.72f, 0.7f, 0.66f);
        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive()) return; int n = vh.currentVertCount / 4; if (n == 0) return;
            var t = GetComponent<Text>(); float em = t == null ? 40f : t.resizeTextForBestFit && t.cachedTextGenerator.fontSizeUsedForBestFit > 0 ? t.cachedTextGenerator.fontSizeUsedForBestFit : t.fontSize;
            Color top = gold ? GoldTop : StoneTop, foot = gold ? GoldFoot : StoneFoot; var v = new UIVertex();
            float lo = float.MaxValue, hi = float.MinValue;
            for (int i = 0; i < n * 4; i++) { vh.PopulateUIVertex(ref v, i); lo = Mathf.Min(lo, v.position.y); hi = Mathf.Max(hi, v.position.y); }
            float step = hi - lo < em * 1.4f ? tracking * em : 0f, anchor = t == null ? 0.5f : ((int)t.alignment % 3) * 0.5f;   // one line only, left, centre or right as the text is set
            for (int k = 0; k < n; k++)
            {
                float qLo = float.MaxValue, qHi = float.MinValue;
                for (int j = 0; j < 4; j++) { vh.PopulateUIVertex(ref v, k * 4 + j); qLo = Mathf.Min(qLo, v.position.y); qHi = Mathf.Max(qHi, v.position.y); }
                float dx = step * (k - (n - 1) * anchor);
                for (int j = 0; j < 4; j++)
                {
                    vh.PopulateUIVertex(ref v, k * 4 + j); byte a = v.color.a;
                    v.color = Color.Lerp(foot, top, (v.position.y - qLo) / Mathf.Max(0.01f, qHi - qLo)); v.color.a = a; v.position.x += dx;
                    vh.SetUIVertex(v, k * 4 + j);
                }
            }
        }
    }

    /// <summary>The menus' sheets come in rather than appear: each full-screen sheet the canvas shows rises a little and
    /// fades up (SheetRise); the play HUD, the cinema and the grain itself are left alone. While any sheet is up, a faint
    /// film grain moves over the screen, just under the curtain.</summary>
    public class SheetMotion : MonoBehaviour
    {
        public RawImage grain;
        readonly Dictionary<Transform, bool> seen = new Dictionary<Transform, bool>();
        static readonly HashSet<string> Still = new HashSet<string> { "PlayHud", "Cinema", "Grain", "HitFlash" };
        float grainTick; Transform curtain;
        void LateUpdate()
        {
            bool any = false;
            foreach (Transform c in transform)
            {
                bool on = c.gameObject.activeSelf; seen.TryGetValue(c, out bool was); seen[c] = on;
                var rt = c as RectTransform; if (!on || rt == null || rt.anchorMin != Vector2.zero || rt.anchorMax != Vector2.one || Still.Contains(c.name)) continue;
                any = true;
                if (!was) { var rise = c.GetComponent<SheetRise>(); if (rise == null) rise = c.gameObject.AddComponent<SheetRise>(); rise.Play(); }
            }
            if (grain == null) return;
            grain.enabled = any;
            if (curtain == null) curtain = transform.Find("Curtain");
            if (curtain != null && grain.transform.GetSiblingIndex() != curtain.GetSiblingIndex() - 1) grain.transform.SetSiblingIndex(Mathf.Max(0, curtain.GetSiblingIndex() - 1));
            if (any && (grainTick -= Time.unscaledDeltaTime) <= 0f) { grainTick = 0.07f; var uv = grain.uvRect; uv.x = Random.value; uv.y = Random.value; grain.uvRect = uv; }
        }
    }

    /// <summary>One sheet's entrance: up from 36 below and in from nothing, easing out, with a soft whoosh; a vignette laid
    /// under its content (over its backdrop, where it has one) the first time.</summary>
    public class SheetRise : MonoBehaviour
    {
        CanvasGroup group; RectTransform rt; Vector2 home; float t = 1f; bool dressed;
        static Sprite vignette;
        public void Play()
        {
            rt = (RectTransform)transform; group = GetComponent<CanvasGroup>(); if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            if (t >= 1f) home = rt.anchoredPosition;
            if (!dressed) { dressed = true; Dress(); }
            t = 0f; Apply(); Sfx.Whoosh();
        }
        void Dress()
        {
            var v = new GameObject("Vignette", typeof(RectTransform), typeof(Image)); v.transform.SetParent(transform, false);
            var vr = v.GetComponent<RectTransform>(); vr.anchorMin = Vector2.zero; vr.anchorMax = Vector2.one; vr.offsetMin = vr.offsetMax = Vector2.zero;
            var im = v.GetComponent<Image>(); im.sprite = vignette ??= Lightswarm.ProceduralSprites.Vignette(64); im.raycastTarget = false;
            var backdrop = transform.Find("Backdrop"); v.transform.SetSiblingIndex(backdrop != null ? backdrop.GetSiblingIndex() + 1 : 0);
        }
        void Update() { if (t >= 1f) return; t = Mathf.Min(1f, t + Time.unscaledDeltaTime / 0.34f); Apply(); }
        void Apply()
        {
            float e = 1f - (1f - t) * (1f - t) * (1f - t);
            group.alpha = Mathf.Clamp01(t / 0.6f); group.interactable = t >= 0.6f; rt.anchoredPosition = home + new Vector2(0f, -36f * (1f - e));
            float s = 0.985f + 0.015f * e; rt.localScale = new Vector3(s, s, 1f);
        }
    }

    /// <summary>A band of light that sweeps across the gold button every few seconds.</summary>
    public class Sheen : MonoBehaviour
    {
        public RectTransform band; public float width = 900f;
        float t = -2f;
        void Update()
        {
            if (band == null) return; t += Time.unscaledDeltaTime;
            if (t > 4.5f) t = -1.2f;
            float k = Mathf.Clamp01(t / 1.1f); band.anchoredPosition = new Vector2(Mathf.Lerp(-width * 0.75f, width * 0.75f, k), 0f);
            var img = band.GetComponent<Image>(); var c = img.color; c.a = t < 0f || t > 1.1f ? 0f : 0.16f * Mathf.Sin(k * Mathf.PI); img.color = c;
        }
    }

    /// <summary>A drag across the showroom picture turns the turntable.</summary>
    public class GarageDrag : MonoBehaviour, UnityEngine.EventSystems.IDragHandler, UnityEngine.EventSystems.IPointerDownHandler
    {
        public Garage garage;
        public void OnDrag(UnityEngine.EventSystems.PointerEventData e) { if (garage != null) garage.Drag(-e.delta.x); }
        public void OnPointerDown(UnityEngine.EventSystems.PointerEventData e) { if (garage != null) garage.Drag(0f); }
    }

    public partial class Hud : MonoBehaviour
    {
        public class Card { public string id, title, desc; public bool rare; }

        public System.Action<Formation> OnFormation;
        public System.Action OnAd, OnAgain, OnStart, OnRestart, OnDepot, OnBack, OnReserveAd, OnPause, OnResume, OnSound, OnQuit, OnDaily, OnQuality, OnHold, OnAmmo, OnAbility, OnOrder, OnAir, OnDailyChallenge, OnStandReady; public System.Action<string> OnRoute, OnTheatre, OnStand, OnStandItem, OnConvoy, OnSneak; public System.Action<string, int> OnOperation; string sheetTheatre = "normandy"; readonly Image[] routePics = new Image[3], theatreTabs = new Image[3]; readonly Text[] routeNames = new Text[3], routeLines = new Text[3], routePays = new Text[3], theatreLabels = new Text[3]; GameObject holdBtn, ammoBtn, abilityBtn, orderBtn, airBtn, routeSheet, trainBtn; Text xpLine, depotXp; static readonly Color XpBlue = new Color(0.56f, 0.76f, 0.98f), XpDeep = new Color(0.22f, 0.43f, 0.74f, 0.96f); Text airLabel; Image airFill; Text orderLabel; System.Action routeGo; RectTransform pauseBtnRect; Text ammoLabel, ammoKind; Image abilityFill, abilityPic;

        Text clock, count, fps, tally, levelText, toast, endTitle, endEyebrow, adLabel, adNote, leaderHp, assaultLabel, conditions, qualityLabel, setSound, setQuality, setVibe, setMusic, rankLine, pointsLine, ordersCount; GameObject settingsSheet, ordersSheet; RawImage titleBackdrop, depotBackdrop; GarageDrag depotDrag; readonly List<RectTransform> embers = new List<RectTransform>(); readonly List<float> emberPhase = new List<float>(); GameObject dailyBtn, helpSheet, medalsSheet, recordsSheet; Transform medalRows, recordRows;
        public System.Action OnSuppliesAd; public bool SuppliesGranted; bool suppliesOffer; Text cardsEyebrow;
        Image levelFill, flash; GameObject sheet, endSheet, adBtn, titleSheet, depotSheet, hudGroup, reserveBtn, pauseSheet; Text soundLabel; Transform missionRoot;
        class Rising { public Text t; public float life; public Vector3 world; }
        readonly List<Rising> popups = new List<Rising>(); readonly Stack<Text> popupPool = new Stack<Text>(); RectTransform canvasRect;
        Image radar, objectiveArrow; Text objectiveLabel; readonly List<Image> radarDots = new List<Image>(); readonly List<Image> hpBars = new List<Image>(); readonly List<Image> hpFills = new List<Image>(); Transform cardRoot, depotRows; Image rankBadge; GameObject opsTile, againBtn; public Garage garage; int depotTab; readonly Button[] depotTabs = new Button[3]; Image opsPic, briefingPic; GameObject briefing; Text briefingTitle, briefingText; float briefingLeft; ScrollRect depotScroll; Canvas canvas; Text depotPoints, titleStats, endPoints, reserveNote, bossName; Image bossFill; GameObject bossBar;
        readonly Button[] formButtons = new Button[4];
        readonly List<Image> arrows = new List<Image>(); Sprite arrowSprite;
        float fpsAccum, fpsTimer, toastLeft; int fpsFrames;

        public Canvas Canvas => canvas;

        public void Build()
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1080, 2340); scaler.matchWidthOrHeight = 0.5f;
            if (!FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>())
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));

            var hg = new GameObject("PlayHud", typeof(RectTransform)); hg.transform.SetParent(canvasGo.transform, false); Stretch(hg); hudGroup = hg; hudFade = hg.AddComponent<CanvasGroup>();
            var t = hg.transform; canvasRect = canvasGo.GetComponent<RectTransform>();
            var fl = new GameObject("HitFlash", typeof(RectTransform), typeof(Image)); fl.transform.SetParent(t, false); Stretch(fl); flash = fl.GetComponent<Image>(); flash.color = new Color(0.8f, 0.1f, 0.05f, 0f); flash.raycastTarget = false;
            var ink = new Color(0.93f, 0.91f, 0.86f); var amber = new Color(0.95f, 0.66f, 0.23f); var dim = new Color(0.66f, 0.64f, 0.59f);
            assaultLabel = MakeText(t, "ClockLabel", new Vector2(0, 1), new Vector2(60, -70), TextAnchor.UpperLeft, 30, dim); assaultLabel.text = "NIGHT ASSAULT"; assaultLabel.rectTransform.sizeDelta = new Vector2(760, 40); Fit(assaultLabel, 20);   // a long night's name shrinks, it never runs under PLATOON
            clock = MakeText(t, "Clock", new Vector2(0, 1), new Vector2(60, -108), TextAnchor.UpperLeft, 82, ink);
            tally = MakeText(t, "Tally", new Vector2(0.5f, 1), new Vector2(0, -150), TextAnchor.UpperCenter, 26, dim); tally.rectTransform.sizeDelta = new Vector2(500, 40);
            MakeText(t, "CountLabel", new Vector2(1, 1), new Vector2(-60, -70), TextAnchor.UpperRight, 30, dim).text = "PLATOON";
            count = MakeText(t, "Count", new Vector2(1, 1), new Vector2(-60, -108), TextAnchor.UpperRight, 82, amber);
            fps = MakeText(t, "Fps", new Vector2(0.5f, 1), new Vector2(0, -70), TextAnchor.UpperCenter, 28, dim);
            leaderHp = MakeText(t, "LeaderHp", new Vector2(0, 1), new Vector2(60, -186), TextAnchor.UpperLeft, 30, amber);
            levelText = MakeText(t, "Level", new Vector2(0.5f, 1), new Vector2(0, -262), TextAnchor.UpperCenter, 30, dim);
            var barBg = MakeImage(t, "LevelBar", new Vector2(0.5f, 1), new Vector2(0, -240), new Vector2(960, 8), new Color(1f, 1f, 1f, 0.12f));
            levelFill = MakeImage(barBg.transform, "Fill", new Vector2(0, 0.5f), Vector2.zero, new Vector2(0, 8), amber);
            bossBar = new GameObject("BossBar", typeof(RectTransform)); bossBar.transform.SetParent(t, false);
            var brt = bossBar.GetComponent<RectTransform>(); brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 1f); brt.pivot = new Vector2(0.5f, 1f); brt.anchoredPosition = new Vector2(0, -300); brt.sizeDelta = new Vector2(960, 60);
            bossName = MakeText(bossBar.transform, "Name", new Vector2(0.5f, 1f), new Vector2(0, 0), TextAnchor.UpperCenter, 30, new Color(0.95f, 0.35f, 0.3f));
            var bbg = MakeImage(bossBar.transform, "Bg", new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(960, 10), new Color(1f, 1f, 1f, 0.12f));
            bossFill = MakeImage(bbg.transform, "Fill", new Vector2(0, 0.5f), Vector2.zero, new Vector2(960, 10), new Color(0.95f, 0.35f, 0.3f));
            bossBar.SetActive(false);
            toast = MakeText(t, "Toast", new Vector2(0.5f, 0.5f), new Vector2(0, 420), TextAnchor.MiddleCenter, 64, ink); toast.font = LabelFont(); toast.text = ""; toast.rectTransform.sizeDelta = new Vector2(1000, 240); Fit(toast, 38);   // a long one wraps and shrinks, it is never cut
            { var ts = toast.gameObject.AddComponent<UnityEngine.UI.Shadow>(); ts.effectColor = new Color(0f, 0f, 0f, 0.8f); ts.effectDistance = new Vector2(2f, -3f); }
            var pauseBtn = MakeButton(t, "II", new Vector2(0.5f, 1f), new Vector2(0, -150), new Vector2(120, 76), 40, () => OnPause?.Invoke()); pauseBtn.name = "Pause"; pauseBtnRect = pauseBtn.GetComponent<RectTransform>();
            radar = MakeImage(t, "Radar", new Vector2(1f, 0f), new Vector2(-150, 330), new Vector2(240, 240), new Color(0.02f, 0.03f, 0.04f, 0.55f)); radar.sprite = Lightswarm.ProceduralSprites.Glow(64, 0.98f); radar.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var ring = MakeImage(radar.transform, "Ring", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240, 240), new Color(0.93f, 0.91f, 0.86f, 0.35f)); ring.sprite = Lightswarm.ProceduralSprites.Ring(128, 0.03f); ring.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var me = MakeImage(radar.transform, "Me", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14, 14), new Color(0.95f, 0.66f, 0.23f, 1f)); me.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            objectiveArrow = MakeImage(t, "ObjectiveArrow", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64, 64), new Color(0.35f, 0.95f, 0.45f, 0.95f)); objectiveArrow.rectTransform.pivot = new Vector2(0.5f, 0.5f); objectiveArrow.enabled = false;
            objectiveLabel = MakeText(t, "ObjectiveLabel", new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleCenter, 30, new Color(0.35f, 0.95f, 0.45f)); objectiveLabel.rectTransform.sizeDelta = new Vector2(240, 50); objectiveLabel.text = "";

            var names = new[] { "WEDGE", "COLUMN", "LINE", "ECHELON" };
            var forms = new[] { Formation.Wedge, Formation.Column, Formation.Line, Formation.Echelon };
            for (int i = 0; i < 4; i++)
            {
                var f = forms[i]; int idx = i;
                var b = MakeButton(t, names[i], new Vector2(0.5f, 0f), new Vector2(-390 + i * 260, 120), new Vector2(244, 92), 30, () => { OnFormation?.Invoke(f); Highlight(idx); });
                formButtons[i] = b.GetComponent<Button>();
                var icon = MakeImage(b.transform, "Icon", new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(64f, 64f), new Color(0.93f, 0.91f, 0.86f, 0.9f)); icon.sprite = UiSprite("formation_icons", new Rect(i * 256, 0, 256, 256));
                var lbl = b.transform.Find("Label").GetComponent<Text>(); lbl.alignment = TextAnchor.MiddleLeft; lbl.rectTransform.anchorMin = new Vector2(0f, 0f); lbl.rectTransform.anchorMax = new Vector2(1f, 1f); lbl.rectTransform.offsetMin = new Vector2(82f, 0f); lbl.rectTransform.offsetMax = Vector2.zero;
            }
            Highlight(0);

            // level-up sheet
            sheet = new GameObject("LevelUp", typeof(RectTransform), typeof(Image)); sheet.transform.SetParent(t, false);
            Stretch(sheet); sheet.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.04f, 0.8f);
            { var ey = MakeText(sheet.transform, "Eyebrow", new Vector2(0.5f, 0.5f), new Vector2(0, 600), TextAnchor.MiddleCenter, 28, new Color(0.96f, 0.68f, 0.24f)); ey.text = Spaced("LEVEL UP"); ey.font = LabelFont(); cardsEyebrow = ey; }
            { var ch = MakeText(sheet.transform, "Title", new Vector2(0.5f, 0.5f), new Vector2(0, 530), TextAnchor.MiddleCenter, 96, ink); ch.text = "Choose"; Engrave(ch, true, 0.8f); }
            var cards = new GameObject("Cards", typeof(RectTransform)); cards.transform.SetParent(sheet.transform, false);
            var crt = cards.GetComponent<RectTransform>(); crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f); crt.sizeDelta = Vector2.zero;
            cardRoot = cards.transform; sheet.SetActive(false);

            // end sheet
            endSheet = new GameObject("End", typeof(RectTransform), typeof(Image)); endSheet.transform.SetParent(canvasGo.transform, false);   // over the HUD, not in it: the HUD steps aside behind the end
            Stretch(endSheet); { var es = endSheet.GetComponent<Image>(); es.sprite = Scrim(); es.color = new Color(0.02f, 0.03f, 0.04f, 0.88f); }   // clear at the top, where the camera circles the leader
            endEyebrow = MakeText(endSheet.transform, "Eyebrow", new Vector2(0.5f, 0.5f), new Vector2(0, 596), TextAnchor.MiddleCenter, 30, dim);
            endTitle = MakeText(endSheet.transform, "Title", new Vector2(0.5f, 0.5f), new Vector2(0, 506), TextAnchor.MiddleCenter, 110, ink); Engrave(endTitle, true, 0.62f, 4f);
            { var lg = new GameObject("Ledger", typeof(RectTransform)); lg.transform.SetParent(endSheet.transform, false); endLedger = lg.GetComponent<RectTransform>(); endLedger.anchorMin = endLedger.anchorMax = endLedger.pivot = new Vector2(0.5f, 0.5f); endLedger.sizeDelta = Vector2.zero; }   // the night's account, laid out by ShowEnd
            adBtn = MakeButton(endSheet.transform, "Field repair · watch an ad", new Vector2(0.5f, 0.5f), new Vector2(-130, -226), new Vector2(620, 130), 40, () => OnAd?.Invoke());
            adBtn.GetComponent<Image>().color = amber; adLabel = adBtn.transform.Find("Label").GetComponent<Text>(); adLabel.color = new Color(0.1f, 0.08f, 0.05f);
            adLabel.resizeTextForBestFit = true; adLabel.resizeTextMinSize = 22; adLabel.resizeTextMaxSize = 40; adLabel.rectTransform.sizeDelta = new Vector2(580, 120); Gloss(adBtn.transform);
            {   // or pay for it in gold
                goldBtn = MakeButton(endSheet.transform, "", new Vector2(0.5f, 0.5f), new Vector2(320, -226), new Vector2(240, 130), 40, () => OnGoldAd?.Invoke());
                goldBtn.GetComponent<Image>().color = new Color(0.08f, 0.085f, 0.1f, 0.96f); goldBtn.transform.Find("Label").gameObject.SetActive(false);
                var ge = MakeImage(goldBtn.transform, "Edge", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240, 130), new Color(1f, 0.8f, 0.35f, 0.6f)); ge.sprite = Outline(); ge.type = Image.Type.Sliced; ge.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                var gb = MakeImage(goldBtn.transform, "Bar", new Vector2(0.5f, 0.5f), new Vector2(-46, 0), new Vector2(66, 44), Color.white); gb.sprite = GoldSprite(); gb.preserveAspect = true;
                var gt = MakeText(goldBtn.transform, "Gold", new Vector2(0.5f, 0.5f), new Vector2(34, 0), TextAnchor.MiddleCenter, 44, GoldInk); gt.text = Depot.GoldRepair.ToString(); gt.font = BoldFont(); gt.rectTransform.sizeDelta = new Vector2(110, 60);
            }
            adNote = MakeText(endSheet.transform, "AdNote", new Vector2(0.5f, 0.5f), new Vector2(0, -336), TextAnchor.MiddleCenter, 28, dim); adNote.rectTransform.sizeDelta = new Vector2(900, 100);
            againBtn = MakeButton(endSheet.transform, "New assault", new Vector2(0.5f, 0.5f), new Vector2(0, -450), new Vector2(880, 130), 40, () => OnAgain?.Invoke()); RowLook(againBtn);
            endSheet.SetActive(false);
            // the two things the thumb can press in the night: what is loaded, and the commander's order
            {
                ammoBtn = MakeButton(hudGroup.transform, "", new Vector2(0f, 0f), new Vector2(330, 268), new Vector2(190, 120), 22, () => OnAmmo?.Invoke());
                ammoBtn.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.11f, 0.85f);
                var ae = MakeImage(ammoBtn.transform, "Edge", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(190, 120), new Color(1f, 1f, 1f, 0.2f)); ae.sprite = Outline(); ae.type = Image.Type.Sliced; ae.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                ammoKind = MakeText(ammoBtn.transform, "Kind", new Vector2(0.5f, 1f), new Vector2(0, -12), TextAnchor.UpperCenter, 30, new Color(0.96f, 0.68f, 0.24f)); ammoKind.font = BoldFont(); ammoKind.rectTransform.sizeDelta = new Vector2(190, 40);
                ammoLabel = MakeText(ammoBtn.transform, "Count", new Vector2(0.5f, 0f), new Vector2(0, 12), TextAnchor.LowerCenter, 26, new Color(0.85f, 0.83f, 0.78f)); ammoLabel.rectTransform.sizeDelta = new Vector2(190, 60);
                ammoBtn.transform.Find("Label").GetComponent<Text>().text = "";
                abilityBtn = MakeButton(hudGroup.transform, "", new Vector2(0f, 0f), new Vector2(130, 298), new Vector2(180, 180), 20, () => OnAbility?.Invoke());
                abilityBtn.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.11f, 0.9f);
                var mask = new GameObject("Mask", typeof(RectTransform), typeof(RectMask2D)); mask.transform.SetParent(abilityBtn.transform, false); var mkrt = mask.GetComponent<RectTransform>(); mkrt.anchorMin = mkrt.anchorMax = new Vector2(0.5f, 0.5f); mkrt.sizeDelta = new Vector2(168, 168);
                abilityPic = MakeImage(mask.transform, "Pic", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(168, 168), Color.white); abilityPic.rectTransform.pivot = new Vector2(0.5f, 0.5f); abilityPic.preserveAspect = true;
                abilityFill = MakeImage(abilityBtn.transform, "Cool", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(180, 180), new Color(0.02f, 0.02f, 0.03f, 0.78f)); abilityFill.rectTransform.pivot = new Vector2(0.5f, 0.5f); abilityFill.sprite = Rounded(); abilityFill.type = Image.Type.Filled; abilityFill.fillMethod = Image.FillMethod.Vertical; abilityFill.fillOrigin = 0; abilityFill.fillAmount = 1f;
                var be = MakeImage(abilityBtn.transform, "Edge", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(180, 180), new Color(0.96f, 0.68f, 0.24f, 0.5f)); be.sprite = Outline(); be.type = Image.Type.Sliced; be.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                abilityBtn.transform.Find("Label").GetComponent<Text>().text = "";
                abilityBtn.SetActive(false);
                orderBtn = MakeButton(hudGroup.transform, "FOLLOW", new Vector2(0f, 0f), new Vector2(530, 268), new Vector2(190, 120), 26, () => OnOrder?.Invoke());
                orderBtn.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.11f, 0.85f);
                var oe = MakeImage(orderBtn.transform, "Edge", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(190, 120), new Color(1f, 1f, 1f, 0.2f)); oe.sprite = Outline(); oe.type = Image.Type.Sliced; oe.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                var ot = MakeText(orderBtn.transform, "Top", new Vector2(0.5f, 1f), new Vector2(0, -12), TextAnchor.UpperCenter, 20, new Color(0.66f, 0.64f, 0.59f)); ot.text = Spaced("WINGMEN"); ot.rectTransform.sizeDelta = new Vector2(190, 30);
                orderLabel = orderBtn.transform.Find("Label").GetComponent<Text>(); orderLabel.alignment = TextAnchor.MiddleCenter; orderLabel.rectTransform.anchoredPosition = new Vector2(0, -12); orderLabel.fontSize = 30; orderLabel.color = new Color(0.96f, 0.68f, 0.24f);
                // air support: fills while the next strike is being readied
                airBtn = MakeButton(hudGroup.transform, "", new Vector2(0f, 0f), new Vector2(730, 268), new Vector2(190, 120), 26, () => OnAir?.Invoke());
                airBtn.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.11f, 0.85f);
                airFill = MakeImage(airBtn.transform, "Cool", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(190, 120), new Color(0.02f, 0.02f, 0.03f, 0.78f)); airFill.rectTransform.pivot = new Vector2(0.5f, 0.5f); airFill.sprite = Rounded(); airFill.type = Image.Type.Filled; airFill.fillMethod = Image.FillMethod.Vertical; airFill.fillOrigin = (int)Image.OriginVertical.Top;
                var airEdge = MakeImage(airBtn.transform, "Edge", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(190, 120), new Color(0.96f, 0.68f, 0.24f, 0.5f)); airEdge.sprite = Outline(); airEdge.type = Image.Type.Sliced; airEdge.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                var atop = MakeText(airBtn.transform, "Top", new Vector2(0.5f, 1f), new Vector2(0, -12), TextAnchor.UpperCenter, 20, new Color(0.66f, 0.64f, 0.59f)); atop.text = Spaced("AIR"); atop.rectTransform.sizeDelta = new Vector2(190, 30);
                airLabel = airBtn.transform.Find("Label").GetComponent<Text>(); airLabel.alignment = TextAnchor.MiddleCenter; airLabel.rectTransform.anchoredPosition = new Vector2(0, -12); airLabel.fontSize = 28; airLabel.color = new Color(0.96f, 0.68f, 0.24f);
                airLabel.transform.SetAsLastSibling(); atop.transform.SetAsLastSibling(); airBtn.SetActive(false);
            }
            arrowSprite = ArrowSprite(); objectiveArrow.sprite = arrowSprite;
            var root = canvasGo.transform;
            endPoints = MakeText(endSheet.transform, "Points", new Vector2(0.5f, 0.5f), new Vector2(0, 120), TextAnchor.MiddleCenter, 44, amber);
            endDepotBtn = MakeButton(endSheet.transform, "Depot", new Vector2(0.5f, 0.5f), new Vector2(0, -586), new Vector2(880, 130), 40, () => OnDepot?.Invoke()); RowLook(endDepotBtn);
            holdBtn = MakeButton(endSheet.transform, "Hold till morning · points ×1.5", new Vector2(0.5f, 0.5f), new Vector2(0, -722), new Vector2(880, 130), 40, () => OnHold?.Invoke()); holdBtn.GetComponent<Image>().color = new Color(0.55f, 0.36f, 0.14f, 0.95f); Gloss(holdBtn.transform); holdBtn.SetActive(false);

            // pause sheet
            pauseSheet = new GameObject("Pause", typeof(RectTransform), typeof(Image)); pauseSheet.transform.SetParent(root, false);
            Stretch(pauseSheet); pauseSheet.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.04f, 0.8f);
            { var ey = MakeText(pauseSheet.transform, "Eyebrow", new Vector2(0.5f, 0.5f), new Vector2(0, 540), TextAnchor.MiddleCenter, 26, amber); ey.text = Spaced("IRON NIGHT"); ey.font = LabelFont(); ey.rectTransform.sizeDelta = new Vector2(600, 40); }
            { var ti = MakeText(pauseSheet.transform, "Title", new Vector2(0.5f, 0.5f), new Vector2(0, 440), TextAnchor.MiddleCenter, 120, ink); ti.text = "PAUSED"; Engrave(ti); ti.rectTransform.sizeDelta = new Vector2(900, 150); ti.verticalOverflow = VerticalWrapMode.Overflow; }
            MakePrimary(pauseSheet.transform, "Resume", new Vector2(0.5f, 0.5f), new Vector2(0, 210), new Vector2(880, 150), 46, () => OnResume?.Invoke());
            var snd = MakeButton(pauseSheet.transform, "Sound: on", new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(880, 110), 36, () => OnSound?.Invoke()); soundLabel = snd.transform.Find("Label").GetComponent<Text>(); RowLook(snd);
            var qb = MakeButton(pauseSheet.transform, "Quality: high", new Vector2(0.5f, 0.5f), new Vector2(0, -110), new Vector2(880, 110), 36, () => OnQuality?.Invoke()); qualityLabel = qb.transform.Find("Label").GetComponent<Text>(); RowLook(qb);
            { var qn = MakeText(pauseSheet.transform, "QualityNote", new Vector2(0.5f, 0.5f), new Vector2(0, -196), TextAnchor.MiddleCenter, 24, dim); qn.text = "Low: no shadows, no glow, no rain. For phones that stutter."; qn.rectTransform.sizeDelta = new Vector2(900, 40); Fit(qn, 18); }
            var rs = MakeGhost(pauseSheet.transform, "Restart the night", new Vector2(0.5f, 0.5f), new Vector2(0, -340), new Vector2(880, 110), 34, () => OnRestart?.Invoke()); rs.transform.Find("Label").GetComponent<Text>().text = Spaced("RESTART THE NIGHT");
            var ab = MakeGhost(pauseSheet.transform, "Abandon the assault", new Vector2(0.5f, 0.5f), new Vector2(0, -470), new Vector2(880, 110), 34, () => OnQuit?.Invoke()); var abl = ab.transform.Find("Label").GetComponent<Text>(); abl.text = Spaced("ABANDON THE ASSAULT"); abl.color = new Color(0.94f, 0.6f, 0.54f);
            pauseSheet.SetActive(false);

            // title sheet: the hangar with the leader's tank behind everything
            titleSheet = new GameObject("Title", typeof(RectTransform), typeof(Image)); titleSheet.transform.SetParent(root, false);
            Stretch(titleSheet); titleSheet.GetComponent<Image>().color = new Color(0.02f, 0.02f, 0.03f, 1f);
            { var bd = new GameObject("Backdrop", typeof(RectTransform), typeof(RawImage)); bd.transform.SetParent(titleSheet.transform, false); Stretch(bd); titleBackdrop = bd.GetComponent<RawImage>(); titleBackdrop.color = Color.white; titleBackdrop.raycastTarget = false; }
            { var vig = MakeImage(titleSheet.transform, "Vignette", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400f, 2600f), new Color(0f, 0f, 0f, 0.55f)); vig.sprite = Lightswarm.ProceduralSprites.Glow(128, 0.35f); vig.rectTransform.pivot = new Vector2(0.5f, 0.5f); vig.color = new Color(0f, 0f, 0f, 0f); }
            for (int i = 0; i < 26; i++) { var e = MakeImage(titleSheet.transform, "Ember", new Vector2(0.5f, 0.5f), new Vector2(Random.Range(-520f, 520f), Random.Range(-900f, 900f)), new Vector2(Random.Range(6f, 14f), Random.Range(6f, 14f)), new Color(1f, 0.6f, 0.25f, 0.8f)); e.sprite = Lightswarm.ProceduralSprites.Glow(32, 0.3f); e.rectTransform.pivot = new Vector2(0.5f, 0.5f); embers.Add(e.rectTransform); emberPhase.Add(Random.value * 6.28f); }
            { var shade = MakeImage(titleSheet.transform, "Shade", new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(1400f, 1250f), new Color(0.02f, 0.02f, 0.03f, 0.96f)); shade.sprite = Lightswarm.ProceduralSprites.GradientDown(128, 1.6f);
                var top = MakeImage(titleSheet.transform, "TopShade", new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(1400f, 900f), new Color(0.02f, 0.02f, 0.03f, 0.85f)); top.sprite = Lightswarm.ProceduralSprites.GradientDown(128, 1.6f); top.rectTransform.localScale = new Vector3(1f, -1f, 1f); }
            // top bar: the rank and the points
            rankBadge = MakeImage(titleSheet.transform, "Rank", new Vector2(0f, 1f), new Vector2(50, -70), new Vector2(84, 84), Color.white); rankBadge.preserveAspect = true; rankBadge.rectTransform.pivot = new Vector2(0f, 1f);
            rankLine = MakeText(titleSheet.transform, "RankLine", new Vector2(0f, 1f), new Vector2(150, -72), TextAnchor.UpperLeft, 26, new Color(0.9f, 0.88f, 0.84f)); rankLine.rectTransform.pivot = new Vector2(0f, 1f); rankLine.rectTransform.sizeDelta = new Vector2(520, 90); rankLine.font = BoldFont();
            { var pts = MakeCard(titleSheet.transform, "Points", new Vector2(1f, 1f), new Vector2(-50, -70), new Vector2(330, 72), new Color(0.06f, 0.07f, 0.09f, 0.75f), 0.2f); pts.rectTransform.pivot = new Vector2(1f, 1f);
              var coin = MakeImage(pts.transform, "Coin", new Vector2(0f, 0.5f), new Vector2(18, 0), new Vector2(30, 30), new Color(0.96f, 0.68f, 0.24f)); Currency(coin, "icon_points", Lightswarm.ProceduralSprites.Ring(64, 0.16f)); coin.rectTransform.pivot = new Vector2(0f, 0.5f);
              var coinCore = MakeImage(pts.transform, "CoinCore", new Vector2(0f, 0.5f), new Vector2(27, 0), new Vector2(12, 12), new Color(0.96f, 0.68f, 0.24f)); coinCore.sprite = Lightswarm.ProceduralSprites.Glow(32, 0.7f); coinCore.rectTransform.pivot = new Vector2(0f, 0.5f);
              pointsLine = MakeText(pts.transform, "Text", new Vector2(0f, 0.5f), new Vector2(64, 0), TextAnchor.MiddleLeft, 30, new Color(0.96f, 0.68f, 0.24f)); pointsLine.rectTransform.pivot = new Vector2(0f, 0.5f); pointsLine.rectTransform.sizeDelta = new Vector2(260, 72); pointsLine.font = BoldFont(); }
            { var xpc = MakeCard(titleSheet.transform, "Xp", new Vector2(1f, 1f), new Vector2(-50, -150), new Vector2(330, 60), new Color(0.06f, 0.07f, 0.09f, 0.75f), 0.2f); xpc.rectTransform.pivot = new Vector2(1f, 1f);
              var st = MakeImage(xpc.transform, "Star", new Vector2(0f, 0.5f), new Vector2(18, 0), new Vector2(30, 30), XpBlue); Currency(st, "icon_crewxp", StarSprite()); st.rectTransform.pivot = new Vector2(0f, 0.5f);
              xpLine = MakeText(xpc.transform, "Text", new Vector2(0f, 0.5f), new Vector2(60, 0), TextAnchor.MiddleLeft, 28, XpBlue); xpLine.rectTransform.pivot = new Vector2(0f, 0.5f); xpLine.rectTransform.sizeDelta = new Vector2(200, 60); xpLine.font = BoldFont(); }
            goldLine = GoldPill(titleSheet.transform, new Vector2(-50, -218), 330f, true);
            BuildCrateButton(); BuildBondsButton();
            // the name
            var eyebrow = MakeText(titleSheet.transform, "Eyebrow", new Vector2(0.5f, 1f), new Vector2(0, -190), TextAnchor.MiddleCenter, 26, new Color(0.96f, 0.68f, 0.24f)); eyebrow.text = Spaced("WWII · NIGHT ASSAULT"); eyebrow.font = LabelFont();
            var big = MakeText(titleSheet.transform, "Name", new Vector2(0.5f, 1f), new Vector2(0, -300), TextAnchor.MiddleCenter, 170, ink); big.text = "IRON NIGHT"; big.rectTransform.sizeDelta = new Vector2(1040, 240); big.verticalOverflow = VerticalWrapMode.Overflow;
            Engrave(big, true, 0.58f, 5f); big.GetComponent<EngravedFace>().tracking = 0.07f;   // between the crate on the left and the war bonds on the right
            var rule = MakeImage(titleSheet.transform, "Rule", new Vector2(0.5f, 1f), new Vector2(0, -502), new Vector2(110, 4), new Color(0.96f, 0.68f, 0.24f, 0.9f)); rule.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            // tonight
            conditions = MakeChip(titleSheet.transform, "Conditions", new Vector2(0.5f, 0f), new Vector2(0, 1060), 640f, new Color(0.96f, 0.68f, 0.24f), new Color(0.06f, 0.07f, 0.09f, 0.7f));
            // four tiles with pictures: the operations, the daily challenge, the depot, the standing orders
            string[] tileNames = { "WAR MAP", "DAILY", "DEPOT", "WANTED" }; string[] tilePics = { "campaign_normandy", Daily.Picture(Daily.Today), "card_crews", "card_reinf" };
            System.Action[] tileActs = { () => ShowMap(), () => ShowDaily(), () => OnDepot?.Invoke(), () => ShowWanted() };
            const float TW = 226f, TH = 250f;
            for (int i = 0; i < 4; i++)
            {
                float x = (i - 1.5f) * (TW + 12f); var tile = MakeButton(titleSheet.transform, tileNames[i], new Vector2(0.5f, 0f), new Vector2(x, 860), new Vector2(TW, TH), 26, tileActs[i]); tile.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.11f, 1f);
                var mask = new GameObject("Mask", typeof(RectTransform), typeof(RectMask2D)); mask.transform.SetParent(tile.transform, false); var mkrt = mask.GetComponent<RectTransform>(); mkrt.anchorMin = mkrt.anchorMax = new Vector2(0.5f, 0.5f); mkrt.sizeDelta = new Vector2(TW - 10f, TH - 10f); mkrt.anchoredPosition = Vector2.zero;
                var pic = MakeImage(mask.transform, "Pic", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(TW - 10f, TH - 10f), new Color(0.85f, 0.85f, 0.85f, 1f)); pic.rectTransform.pivot = new Vector2(0.5f, 0.5f); var sp = UiSprite(tilePics[i]);
                if (sp != null) { pic.sprite = sp; float cover = Mathf.Max((TW - 10f) / sp.rect.width, (TH - 10f) / sp.rect.height); pic.rectTransform.sizeDelta = new Vector2(sp.rect.width * cover, sp.rect.height * cover); }
                if (i == 0) opsPic = pic; else if (i == 1) dailyPic = pic; else if (i == 3) wantedPic = pic;
                var fade = MakeImage(mask.transform, "Fade", new Vector2(0.5f, 0f), Vector2.zero, new Vector2(TW - 10f, 150), new Color(0.02f, 0.02f, 0.03f, 0.95f)); fade.sprite = Lightswarm.ProceduralSprites.GradientDown(64, 1.2f); fade.rectTransform.pivot = new Vector2(0.5f, 0f);
                var edge = MakeImage(tile.transform, "Edge", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(TW, TH), new Color(1f, 1f, 1f, 0.16f)); edge.sprite = Outline(); edge.type = Image.Type.Sliced; edge.rectTransform.pivot = new Vector2(0.5f, 0.5f); if (i == 1) dailyEdge = edge;
                var lt = tile.transform.Find("Label").GetComponent<Text>(); lt.text = Spaced(tileNames[i]); lt.alignment = TextAnchor.LowerCenter; lt.rectTransform.sizeDelta = new Vector2(TW, 230); lt.transform.SetAsLastSibling(); lt.font = LabelFont();
                var cnt = MakeText(tile.transform, "Count", new Vector2(0.5f, 0f), new Vector2(0, 50), TextAnchor.LowerCenter, 20, OpAmber); cnt.rectTransform.sizeDelta = new Vector2(TW - 10f, 30); cnt.transform.SetAsLastSibling();
                if (i == 0) { opsTile = tile; opsCount = cnt; } else if (i == 1) dailyCount = cnt; else if (i == 2) depotCount = cnt; else ordersCount = cnt;
            }
            // to battle: gold, a highlight along the top, a shadow under
            var start = MakePrimary(titleSheet.transform, "To battle", new Vector2(0.5f, 0f), new Vector2(0, 610), new Vector2(920, 160), 62, () => OnStart?.Invoke());
            { var g1 = MakeImage(start.transform, "Depth", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(920, 160), new Color(0.45f, 0.22f, 0.02f, 0.55f)); g1.sprite = Lightswarm.ProceduralSprites.GradientDown(64, 1.0f); g1.rectTransform.pivot = new Vector2(0.5f, 0.5f); g1.transform.SetSiblingIndex(0);
              var sheenMask = new GameObject("SheenMask", typeof(RectTransform), typeof(RectMask2D)); sheenMask.transform.SetParent(start.transform, false); var smrt = sheenMask.GetComponent<RectTransform>(); smrt.anchorMin = smrt.anchorMax = new Vector2(0.5f, 0.5f); smrt.sizeDelta = new Vector2(912, 152);
              var band = MakeImage(sheenMask.transform, "Sheen", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160, 260), new Color(1f, 0.98f, 0.9f, 0f)); band.rectTransform.pivot = new Vector2(0.5f, 0.5f); band.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 18f); band.sprite = Lightswarm.ProceduralSprites.Glow(64, 0.1f);
              var sh = start.AddComponent<Sheen>(); sh.band = band.rectTransform; sh.width = 920f;
              var hi = MakeImage(start.transform, "Highlight", new Vector2(0.5f, 1f), new Vector2(0, -6), new Vector2(860, 3), new Color(1f, 0.93f, 0.7f, 0.8f)); hi.rectTransform.pivot = new Vector2(0.5f, 1f);
              var lbl = start.transform.Find("Label").GetComponent<Text>(); lbl.font = LabelBoldFont(); lbl.fontSize = 64; lbl.text = Spaced("TO BATTLE"); lbl.transform.SetAsLastSibling(); }
            dailyBtn = MakeGhost(titleSheet.transform, "Quartermaster", new Vector2(0.5f, 0f), new Vector2(-235, 470), new Vector2(450, 70), 24, () => ShowQuartermaster(), 0.1f);   // the daily gift is mail call now
            dailyBtn.GetComponent<Image>().color = new Color(0.16f, 0.36f, 0.22f, 0.92f);
            reserveBtn = MakeGhost(titleSheet.transform, "4th tank tonight · watch an ad", new Vector2(0.5f, 0f), new Vector2(235, 470), new Vector2(450, 70), 24, () => { if (suppliesOffer) OnSuppliesAd?.Invoke(); else OnReserveAd?.Invoke(); }, 0.12f); Fit(reserveBtn.transform.Find("Label").GetComponent<Text>(), 18);
            trainBtn = MakeGhost(titleSheet.transform, "Daily training · watch an ad · +" + Depot.DailyTrainXp + " crew XP", new Vector2(0.5f, 0f), new Vector2(0, 392), new Vector2(920, 64), 24, () => { if (Depot.DailyTrainReady) Ads.Rewarded("train", () => { if (Depot.ClaimDailyTrain()) { Sfx.Pickup(); trainBtn.SetActive(false); titleStats.rectTransform.anchoredPosition = new Vector2(0, 350); Tick(xpLine, Depot.CrewXp, "", " XP"); } }); }, 0.12f);
            trainBtn.GetComponent<Image>().color = new Color(0.12f, 0.2f, 0.32f, 0.92f);
            { var tl = trainBtn.transform.Find("Label").GetComponent<Text>(); tl.rectTransform.anchoredPosition = new Vector2(20, 0);
              var ts = MakeImage(trainBtn.transform, "Star", new Vector2(0.5f, 0.5f), new Vector2(20f - tl.preferredWidth / 2f - 24f, 0), new Vector2(30, 30), XpBlue); Currency(ts, "icon_crewxp", StarSprite()); ts.rectTransform.pivot = new Vector2(0.5f, 0.5f); }
            reserveNote = MakeText(titleSheet.transform, "ReserveNote", new Vector2(0.5f, 0f), new Vector2(235, 470), TextAnchor.MiddleCenter, 22, dim); reserveNote.text = ""; reserveNote.rectTransform.pivot = new Vector2(0.5f, 0.5f); reserveNote.rectTransform.sizeDelta = new Vector2(450, 70);
            titleStats = MakeText(titleSheet.transform, "Stats", new Vector2(0.5f, 0f), new Vector2(0, 300), TextAnchor.MiddleCenter, 24, new Color(0.72f, 0.7f, 0.66f)); titleStats.rectTransform.sizeDelta = new Vector2(940, 80);
            // the standing orders on their own sheet
            ordersSheet = new GameObject("Orders", typeof(RectTransform), typeof(Image)); ordersSheet.transform.SetParent(root, false);
            Stretch(ordersSheet); ordersSheet.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.05f, 1f);
            SheetHead(ordersSheet.transform, "FROM HEADQUARTERS", "STANDING ORDERS");
            { var nt = MakeText(ordersSheet.transform, "Note", new Vector2(0.5f, 1f), new Vector2(0, -340), TextAnchor.MiddleCenter, 26, dim); nt.text = "Carry them out over the nights; the reward goes to the depot."; nt.rectTransform.sizeDelta = new Vector2(900, 40); Fit(nt, 18); }
            var orders = new GameObject("Rows", typeof(RectTransform)); orders.transform.SetParent(ordersSheet.transform, false);
            var ort = orders.GetComponent<RectTransform>(); ort.anchorMin = ort.anchorMax = new Vector2(0.5f, 1f); ort.anchoredPosition = new Vector2(0, -420); ort.sizeDelta = Vector2.zero; missionRoot = orders.transform;
            MakeGhost(ordersSheet.transform, "BACK", new Vector2(0f, 1f), new Vector2(130, -95), new Vector2(200, 80), 26, () => ordersSheet.SetActive(false));
            ordersSheet.SetActive(false);
            MakeText(titleSheet.transform, "Credits", new Vector2(0.5f, 0f), new Vector2(0, 70), TextAnchor.MiddleCenter, 18, new Color(0.45f, 0.44f, 0.4f)).text = "Tank models by mamont nikita, buffinbag, Hxhdjdjdk, Julian, Artem Goyko, XxRxX, Mr_Chiko, Joanthan To · Sketchfab, CC BY 4.0 · Built with DINOv3 · TRELLIS 2 · Unity";
            var bar = MakeCard(titleSheet.transform, "Bar", new Vector2(0.5f, 0f), new Vector2(0, 170), new Vector2(920, 90), new Color(0.05f, 0.06f, 0.08f, 0.7f)); bar.rectTransform.pivot = new Vector2(0.5f, 0f);
            string[] barLabels = { "How to play", "Medals", "Records", "Settings" }; System.Action[] barActs = { () => { helpSheet.SetActive(true); }, () => { ShowMedals(); }, () => { ShowRecords(); }, () => { ShowSettings(); } };
            for (int i = 0; i < 4; i++) { var bb = MakeButton(bar.transform, barLabels[i], new Vector2(0.5f, 0.5f), new Vector2(-345 + i * 230, 0), new Vector2(222, 74), 24, barActs[i]); bb.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f); var lt = bb.transform.Find("Label").GetComponent<Text>(); lt.text = Spaced(barLabels[i].ToUpperInvariant()); lt.color = new Color(0.82f, 0.8f, 0.76f); if (i > 0) { var div = MakeImage(bar.transform, "Div", new Vector2(0.5f, 0.5f), new Vector2(-345 + i * 230 - 115, 0), new Vector2(2, 40), new Color(1f, 1f, 1f, 0.12f)); div.rectTransform.pivot = new Vector2(0.5f, 0.5f); } }
            titleSheet.SetActive(false);

            // settings and credits
            settingsSheet = new GameObject("Settings", typeof(RectTransform), typeof(Image)); settingsSheet.transform.SetParent(root, false);
            Stretch(settingsSheet); settingsSheet.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.05f, 1f);
            { var ey = MakeText(settingsSheet.transform, "Eyebrow", new Vector2(0.5f, 1f), new Vector2(0, -150), TextAnchor.MiddleCenter, 26, amber); ey.text = Spaced("IRON NIGHT"); ey.font = LabelFont(); ey.rectTransform.sizeDelta = new Vector2(600, 40); }
            { var ti = MakeText(settingsSheet.transform, "Title", new Vector2(0.5f, 1f), new Vector2(0, -190), TextAnchor.MiddleCenter, 120, ink); ti.text = "SETTINGS"; Engrave(ti); ti.rectTransform.sizeDelta = new Vector2(900, 150); ti.verticalOverflow = VerticalWrapMode.Overflow; }
            setSound = MakeButton(settingsSheet.transform, "Sound", new Vector2(0.5f, 1f), new Vector2(0, -390), new Vector2(880, 104), 36, () => { OnSound?.Invoke(); RefreshSettings(); }).transform.Find("Label").GetComponent<Text>();
            setQuality = MakeButton(settingsSheet.transform, "Quality", new Vector2(0.5f, 1f), new Vector2(0, -634), new Vector2(880, 104), 36, () => { OnQuality?.Invoke(); RefreshSettings(); }).transform.Find("Label").GetComponent<Text>();
            setMusic = MakeButton(settingsSheet.transform, "Music", new Vector2(0.5f, 1f), new Vector2(0, -512), new Vector2(880, 104), 36, () => { Sfx.MusicOff = !Sfx.MusicOff; RefreshSettings(); }).transform.Find("Label").GetComponent<Text>();
            setVibe = MakeButton(settingsSheet.transform, "Vibration", new Vector2(0.5f, 1f), new Vector2(0, -756), new Vector2(880, 104), 36, () => { PlayerPrefs.SetInt("vibe", PlayerPrefs.GetInt("vibe", 1) == 1 ? 0 : 1); PlayerPrefs.Save(); RefreshSettings(); }).transform.Find("Label").GetComponent<Text>();
            { var qn = MakeText(settingsSheet.transform, "QualityNote", new Vector2(0.5f, 1f), new Vector2(0, -846), TextAnchor.MiddleCenter, 24, dim); qn.text = "Low quality: no shadows, no rain, simpler hedges, for phones that stutter."; qn.rectTransform.sizeDelta = new Vector2(900, 40); Fit(qn, 18); }
            ShopHeading(settingsSheet.transform, -930f, "CREDITS");
            var cr = MakeText(settingsSheet.transform, "Credits", new Vector2(0.5f, 1f), new Vector2(0, -972), TextAnchor.UpperLeft, 23, new Color(0.8f, 0.78f, 0.73f)); cr.rectTransform.sizeDelta = new Vector2(880, 560); cr.rectTransform.pivot = new Vector2(0.5f, 1f); cr.lineSpacing = 1.12f; Fit(cr, 17);
            cr.text = "Tank models (Sketchfab, CC BY 4.0, modified):\n" +
                "  Sherman M4A3 Green Set E - mamont nikita\n  M24 Chaffee - buffinbag\n  M26 Pershing Eagle 7 - Hxhdjdjdk\n  T-34 85 Tank - Julian\n  Kv-1 - Artem Goyko\n  SU-100 - XxRxX\n  IS-2M - Mr_Chiko\n  Panzer IV Medium Tank - Joanthan To (Toshueyi)\n" +
                "  creativecommons.org/licenses/by/4.0\n\n" +
                "Other vehicles, props and pictures: generated for this game (Microsoft TRELLIS 2, MIT; built with DINOv3).\n" +
                "Sound effects: Pixabay (Pixabay Content License) and Mixkit (Mixkit License), authors listed in the game's SOURCES.\n" +
                "Music: Pixabay (Pixabay Content License) - 'Battlefield Borders' by AberrantRealities, 'Echoes of the Battlefield' by DesiFreeMusic, 'Cinematic Drums War' by Alec_Koff, 'Majestic Brass Fanfare' by Luis_Humanoide.\n" +
                "Made with Unity.\n\nProgress is kept on this device only, with no account. The ads you choose to watch come from Google AdMob, which may use your device's advertising ID: see the privacy policy.";
            adChoices = MakeGhost(settingsSheet.transform, "Ad privacy choices", new Vector2(0.5f, 0f), new Vector2(0, 290), new Vector2(880, 100), 32, () => Ads.ShowPrivacyChoices()); adChoices.SetActive(false);   // Europe: the ad consent can be changed at any time
            MakeGhost(settingsSheet.transform, "Privacy policy", new Vector2(0.5f, 0f), new Vector2(0, 150), new Vector2(560, 100), 30, () => { if (!Battle.QaOn) Application.OpenURL("https://petermonev.github.io/iron-night/privacy.html"); });
            MakeGhost(settingsSheet.transform, "BACK", new Vector2(0f, 1f), new Vector2(130, -95), new Vector2(200, 80), 26, () => settingsSheet.SetActive(false));
            foreach (var sw in new[] { setSound, setMusic, setQuality, setVibe }) RowLook(sw.transform.parent.gameObject);
            settingsSheet.SetActive(false);

            // how to play
            helpSheet = new GameObject("Help", typeof(RectTransform), typeof(Image)); helpSheet.transform.SetParent(root, false);
            Stretch(helpSheet); helpSheet.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.05f, 1f);
            SheetHead(helpSheet.transform, "THE FIELD MANUAL", "HOW TO PLAY"); var helpList = ScrollList(helpSheet.transform, 350f, 60f, out _);
            var help = MakeText(helpList, "Text", new Vector2(0.5f, 1f), new Vector2(0, -10), TextAnchor.UpperLeft, 32, new Color(0.85f, 0.83f, 0.78f)); help.rectTransform.sizeDelta = new Vector2(900, 1600); help.lineSpacing = 1.06f;
            help.text = "Drag anywhere to drive the leader. The turrets aim and fire on their own.\n\n" +
                "The leader carries a limited rack of AP and HE. Every enemy you destroy leaves an ammunition crate by its wreck: drive over it to rearm. Now and then, while the platoon is short, a new tank comes up with a crate, up to three (a fourth with the ad).\n\n" +
                "Pick a formation at the bottom. Wedge for the open field, column for the lanes, line to bring every gun to bear.\n\n" +
                "Hedges slow a tank but do not stop it: shoulder through and the bushes go down under the hull. Farm buildings stop shells: use them as cover, or deny them to the enemy.\n\n" +
                "Anti-tank guns dig in behind sandbags and the 88s stand with the searchlights. Hit them from the side.\n\n" +
                "Green smoke marks the objective. Supply crates come down by parachute. Level up and choose a card.\n\n" +
                "From 1:00 the AIR button calls in fighter-bombers: press it, then tap where they should hit.\n\n" +
                "DAILY is one night a day, the same for everyone, with its own rule. The first run of each day pays more for every day in a row.\n\n" +
                "Hold until 5:00. Dawn is a win.";
            help.rectTransform.sizeDelta = new Vector2(900, help.preferredHeight + 20f); helpList.sizeDelta = new Vector2(940f, help.preferredHeight + 40f);
            MakeGhost(helpSheet.transform, "BACK", new Vector2(0f, 1f), new Vector2(130, -95), new Vector2(200, 80), 26, () => helpSheet.SetActive(false));
            helpSheet.SetActive(false);

            // medals
            medalsSheet = new GameObject("Medals", typeof(RectTransform), typeof(Image)); medalsSheet.transform.SetParent(root, false);
            Stretch(medalsSheet); medalsSheet.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.05f, 1f);
            SheetHead(medalsSheet.transform, "DECORATIONS", "MEDALS");
            medalRows = ScrollList(medalsSheet.transform, 350f, 60f, out medalScroll);
            MakeGhost(medalsSheet.transform, "BACK", new Vector2(0f, 1f), new Vector2(130, -95), new Vector2(200, 80), 26, () => medalsSheet.SetActive(false));
            medalsSheet.SetActive(false);

            // records
            recordsSheet = new GameObject("Records", typeof(RectTransform), typeof(Image)); recordsSheet.transform.SetParent(root, false);
            Stretch(recordsSheet); recordsSheet.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.05f, 1f);
            SheetHead(recordsSheet.transform, "SERVICE RECORD", "RECORDS");
            recordRows = ScrollList(recordsSheet.transform, 350f, 60f, out recordScroll);
            MakeGhost(recordsSheet.transform, "BACK", new Vector2(0f, 1f), new Vector2(130, -95), new Vector2(200, 80), 26, () => recordsSheet.SetActive(false));
            recordsSheet.SetActive(false);

            // depot sheet
            depotSheet = new GameObject("Depot", typeof(RectTransform), typeof(Image)); depotSheet.transform.SetParent(root, false);
            Stretch(depotSheet); depotSheet.GetComponent<Image>().color = new Color(0.02f, 0.02f, 0.03f, 1f);
            { var bd = new GameObject("Backdrop", typeof(RectTransform), typeof(RawImage), typeof(GarageDrag)); bd.transform.SetParent(depotSheet.transform, false); Stretch(bd); depotBackdrop = bd.GetComponent<RawImage>(); depotBackdrop.color = Color.white; depotDrag = bd.GetComponent<GarageDrag>();
              var top = MakeImage(depotSheet.transform, "TopShade", new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1400f, 700f), new Color(0.02f, 0.02f, 0.03f, 0.85f)); top.sprite = Lightswarm.ProceduralSprites.GradientDown(128, 1.6f); top.rectTransform.localScale = new Vector3(1f, -1f, 1f);
              var low = MakeImage(depotSheet.transform, "LowShade", new Vector2(0.5f, 0f), Vector2.zero, new Vector2(1400f, 1500f), new Color(0.02f, 0.02f, 0.03f, 0.97f)); low.sprite = Lightswarm.ProceduralSprites.GradientDown(128, 2.2f); }
            { var back = MakeGhost(depotSheet.transform, "Back", new Vector2(0f, 1f), new Vector2(120, -105), new Vector2(180, 70), 26, () => OnBack?.Invoke(), 0.25f); back.transform.Find("Label").GetComponent<Text>().text = Spaced("BACK"); }
            { var dt = MakeText(depotSheet.transform, "Title", new Vector2(0.5f, 1f), new Vector2(0, -110), TextAnchor.MiddleCenter, 84, ink); dt.text = "DEPOT"; Engrave(dt); dt.verticalOverflow = VerticalWrapMode.Overflow; var dsh = dt.gameObject.AddComponent<UnityEngine.UI.Shadow>(); dsh.effectColor = new Color(0f, 0f, 0f, 0.8f); dsh.effectDistance = new Vector2(0f, -5f); }
            { var pts = MakeCard(depotSheet.transform, "Points", new Vector2(1f, 1f), new Vector2(-50, -70), new Vector2(300, 72), new Color(0.06f, 0.07f, 0.09f, 0.75f), 0.2f); pts.rectTransform.pivot = new Vector2(1f, 1f);
              var coin = MakeImage(pts.transform, "Coin", new Vector2(0f, 0.5f), new Vector2(18, 0), new Vector2(30, 30), new Color(0.96f, 0.68f, 0.24f)); Currency(coin, "icon_points", Lightswarm.ProceduralSprites.Ring(64, 0.16f)); coin.rectTransform.pivot = new Vector2(0f, 0.5f);
              depotPoints = MakeText(pts.transform, "Text", new Vector2(0f, 0.5f), new Vector2(64, 0), TextAnchor.MiddleLeft, 30, new Color(0.96f, 0.68f, 0.24f)); depotPoints.rectTransform.pivot = new Vector2(0f, 0.5f); depotPoints.rectTransform.sizeDelta = new Vector2(230, 72); depotPoints.font = BoldFont(); }
            { var xpc = MakeCard(depotSheet.transform, "Xp", new Vector2(1f, 1f), new Vector2(-50, -150), new Vector2(300, 60), new Color(0.06f, 0.07f, 0.09f, 0.75f), 0.2f); xpc.rectTransform.pivot = new Vector2(1f, 1f);
              var st = MakeImage(xpc.transform, "Star", new Vector2(0f, 0.5f), new Vector2(18, 0), new Vector2(30, 30), XpBlue); Currency(st, "icon_crewxp", StarSprite()); st.rectTransform.pivot = new Vector2(0f, 0.5f);
              depotXp = MakeText(xpc.transform, "Text", new Vector2(0f, 0.5f), new Vector2(60, 0), TextAnchor.MiddleLeft, 28, XpBlue); depotXp.rectTransform.pivot = new Vector2(0f, 0.5f); depotXp.rectTransform.sizeDelta = new Vector2(200, 60); depotXp.font = BoldFont(); }
            depotGold = GoldPill(depotSheet.transform, new Vector2(-50, -218), 300f, true);
            MakeText(depotSheet.transform, "Hint", new Vector2(0.5f, 0f), new Vector2(0, 1062), TextAnchor.MiddleCenter, 22, dim).text = "drag the tank to turn it · tap a tank in the list to see it";
            var panel = MakeCard(depotSheet.transform, "Panel", new Vector2(0.5f, 0f), new Vector2(0, 40), new Vector2(1000, 1000), new Color(0.04f, 0.05f, 0.07f, 0.82f), 0.14f); panel.rectTransform.pivot = new Vector2(0.5f, 0f);
            { var seg = MakeCard(panel.transform, "Tabs", new Vector2(0.5f, 1f), new Vector2(0, -22), new Vector2(920, 84), new Color(0.03f, 0.04f, 0.05f, 0.9f), 0.1f); seg.rectTransform.pivot = new Vector2(0.5f, 1f);
              depotTabs[0] = MakeButton(seg.transform, "Upgrades", new Vector2(0.5f, 0.5f), new Vector2(-302, 0), new Vector2(296, 70), 28, () => { depotTab = 0; dossierId = null; crewRole = null; RefreshDepot(); }).GetComponent<Button>();
              depotTabs[1] = MakeButton(seg.transform, "Garage", new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(296, 70), 28, () => { depotTab = 1; dossierId = null; crewRole = null; RefreshDepot(); }).GetComponent<Button>();
              depotTabs[2] = MakeButton(seg.transform, "Crew", new Vector2(0.5f, 0.5f), new Vector2(302, 0), new Vector2(296, 70), 28, () => { depotTab = 2; dossierId = null; crewRole = null; RefreshDepot(); }).GetComponent<Button>();
              foreach (var tb in depotTabs) { var lt = tb.transform.Find("Label").GetComponent<Text>(); lt.text = Spaced(lt.text.ToUpperInvariant()); } }
            // the rows scroll in a masked window inside the panel, under the tabs
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect)); viewport.transform.SetParent(panel.transform, false);
            var vrt = viewport.GetComponent<RectTransform>(); vrt.anchorMin = new Vector2(0f, 0f); vrt.anchorMax = new Vector2(1f, 1f); vrt.offsetMin = new Vector2(20f, 24f); vrt.offsetMax = new Vector2(-20f, -125f);
            viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);   // something to drag on
            var rows = new GameObject("Rows", typeof(RectTransform)); rows.transform.SetParent(viewport.transform, false);
            var rrt = rows.GetComponent<RectTransform>(); rrt.anchorMin = rrt.anchorMax = new Vector2(0.5f, 1f); rrt.pivot = new Vector2(0.5f, 1f); rrt.anchoredPosition = Vector2.zero; rrt.sizeDelta = new Vector2(940f, 2000f);
            depotRows = rows.transform; depotScroll = viewport.GetComponent<ScrollRect>(); depotScroll.content = rrt; depotScroll.viewport = vrt; depotScroll.horizontal = false; depotScroll.vertical = true;
            depotScroll.movementType = ScrollRect.MovementType.Clamped; depotScroll.scrollSensitivity = 40f; depotScroll.inertia = true; depotScroll.decelerationRate = 0.12f;
            depotSheet.SetActive(false);
            // the cinema: black bars that slide over the top and bottom, and a card of words, while the camera has a shot
            {
                var ci = new GameObject("Cinema", typeof(RectTransform)); ci.transform.SetParent(canvasGo.transform, false); Stretch(ci); ci.transform.SetSiblingIndex(hudGroup.transform.GetSiblingIndex() + 1);
                barTop = MakeImage(ci.transform, "BarTop", new Vector2(0.5f, 1f), new Vector2(0f, BarH), new Vector2(4000f, BarH), Color.black).rectTransform;
                barBottom = MakeImage(ci.transform, "BarBottom", new Vector2(0.5f, 0f), new Vector2(0f, -BarH), new Vector2(4000f, BarH), Color.black).rectTransform;
                var cc = new GameObject("Card", typeof(RectTransform), typeof(CanvasGroup)); cc.transform.SetParent(ci.transform, false);
                cineCard = cc.GetComponent<RectTransform>(); cineCard.anchorMin = cineCard.anchorMax = new Vector2(0.5f, 0.5f); cineCard.sizeDelta = new Vector2(1000f, 420f);
                cineFade = cc.GetComponent<CanvasGroup>(); cineFade.alpha = 0f; cineFade.blocksRaycasts = cineFade.interactable = false;
                var shade = MakeImage(cc.transform, "Shade", new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(1500f, 560f), new Color(0f, 0f, 0f, 0.6f)); shade.sprite = Lightswarm.ProceduralSprites.Glow(64, 0.3f);   // a soft dark behind the words, for a bright fire under them
                cineEyebrow = MakeText(cc.transform, "Eyebrow", new Vector2(0.5f, 0.5f), new Vector2(0f, 128f), TextAnchor.MiddleCenter, 30, amber); cineEyebrow.font = BoldFont(); cineEyebrow.rectTransform.sizeDelta = new Vector2(1060f, 50f); cineEyebrow.horizontalOverflow = HorizontalWrapMode.Overflow;
                cineTitle = MakeText(cc.transform, "Title", new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleCenter, 210, ink); Engrave(cineTitle); cineTitle.rectTransform.sizeDelta = new Vector2(1000f, 250f);
                cineTitle.resizeTextForBestFit = true; cineTitle.resizeTextMinSize = 70; cineTitle.resizeTextMaxSize = 210;
                cineRule = MakeImage(cc.transform, "Rule", new Vector2(0.5f, 0.5f), new Vector2(0f, -112f), new Vector2(0f, 3f), amber);
                cineSub = MakeText(cc.transform, "Sub", new Vector2(0.5f, 0.5f), new Vector2(0f, -156f), TextAnchor.MiddleCenter, 32, new Color(0.85f, 0.83f, 0.78f)); cineSub.rectTransform.sizeDelta = new Vector2(1060f, 56f); cineSub.horizontalOverflow = HorizontalWrapMode.Overflow;
                cineSkip = MakeText(barBottom, "Skip", new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleCenter, 24, dim); cineSkip.text = Spaced("TAP TO SKIP"); cineSkip.rectTransform.sizeDelta = new Vector2(600f, 40f); cineSkip.gameObject.SetActive(false);
            }
            { curtain = MakeImage(canvasGo.transform, "Curtain", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000, 4000), new Color(0.01f, 0.01f, 0.015f, 1f)); curtain.transform.SetAsLastSibling(); Curtain(0f); }
            {   // film grain over the menus, very faint and moving; SheetMotion shows it while a sheet is up and brings the sheets in
                var gr = new GameObject("Grain", typeof(RectTransform), typeof(RawImage)); gr.transform.SetParent(canvasGo.transform, false); Stretch(gr);
                var raw = gr.GetComponent<RawImage>(); raw.texture = Lightswarm.ProceduralSprites.Grain(128); raw.color = new Color(1f, 1f, 1f, 0.08f); raw.raycastTarget = false; raw.uvRect = new Rect(0f, 0f, 1080f / 512f, 2340f / 512f); raw.enabled = false;
                canvasGo.AddComponent<SheetMotion>().grain = raw;
            }
        }

        public void ShowTitle(bool reserveGranted)
        {
            if (garage != null) { garage.SetActive(false); garage.Show(VehicleSpec.ById(Depot.LeaderId)); garage.SetTitle(true); garage.Frame(false); titleBackdrop.texture = garage.TitleTexture; }
            Sfx.Music(true);
            Depot.Load(); Medals.Check(); hudGroup.SetActive(false); endSheet.SetActive(false); depotSheet.SetActive(false); RefreshRewardButtons();
            rankLine.text = Depot.Rank.ToUpperInvariant() + "\n" + (Depot.NightsFought == 0 ? "first night" : Depot.NightsFought + " nights · " + Depot.CrewName.ToLowerInvariant() + " · " + Depot.CrewNights + " together"); Tick(pointsLine, Depot.Points); Tick(xpLine, Depot.CrewXp, "", " XP"); Tick(goldLine, Depot.Gold); trainBtn.SetActive(Depot.DailyTrainReady); titleStats.rectTransform.anchoredPosition = new Vector2(0, Depot.DailyTrainReady ? 300 : 350);
            int m = Mathf.FloorToInt(Depot.BestTime / 60f), s = Mathf.FloorToInt(Depot.BestTime % 60f);
            { var sheet = Resources.Load<Texture2D>("UI/rank_insignia"); if (sheet != null && rankBadge != null) { int cell = Depot.RankIndex; rankBadge.sprite = UiSprite("rank_insignia", new Rect(cell * sheet.width / 6f, 0f, sheet.width / 6f, sheet.height)); rankBadge.enabled = Depot.NightsFought > 0; } }
            { var today = Daily.Today; bool played = Daily.Played(today);   // the daily tile: the rule to come, or the day's best
              dailyCount.text = played ? "best " + Daily.Best(today).ToString("N0", En) : Daily.RuleOf(today).name.ToLowerInvariant();
              dailyEdge.color = played ? new Color(1f, 1f, 1f, 0.16f) : new Color(0.96f, 0.68f, 0.24f, 0.75f);
              var dp = UiSprite(Daily.Picture(today)); if (dp != null && dailyPic != null) { dailyPic.sprite = dp; float dc = Mathf.Max(216f / dp.rect.width, 240f / dp.rect.height); dailyPic.rectTransform.sizeDelta = new Vector2(dp.rect.width * dc, dp.rect.height * dc); } }
            { bool ready = false; foreach (var lc in Depot.Leaders) if (Depot.OwnsLeader(lc) && Career.AnyUpgrade(lc.id)) ready = true; depotCount.text = ready ? "upgrade ready" : ""; }
            { RefreshMapTile();   // the first tile is the war map now, the operations are on it
              var cs = UiSprite("map_europe") ?? UiSprite(Operations.Current.cover) ?? UiSprite("campaign_normandy"); if (cs != null && opsPic != null) { opsPic.sprite = cs; float cover = Mathf.Max(216f / cs.rect.width, 240f / cs.rect.height); opsPic.rectTransform.sizeDelta = new Vector2(cs.rect.width * cover, cs.rect.height * cover); } }
            titleStats.text = Depot.NightsFought == 0 ? "First night. Drag anywhere to drive; the turrets fire on their own." : $"Best {Depot.BestKills} kills · longest {m}:{s:00} · {Depot.CrewName}: {Depot.CrewBonusText}";
            {   // the slot beside the quartermaster: the 4th tank for an ad, then the supplies for another, then what is ready
                suppliesOffer = reserveGranted && !SuppliesGranted; reserveBtn.SetActive(!reserveGranted || suppliesOffer);
                reserveBtn.transform.Find("Label").GetComponent<Text>().text = suppliesOffer ? "4th tank ready · supplies: watch an ad" : "4th tank tonight · watch an ad";
                reserveNote.text = reserveGranted && SuppliesGranted ? "A 4th tank, and a card to choose\nas the night begins." : "";
            }
            Missions.Load(); foreach (Transform c in missionRoot) Destroy(c.gameObject); RefreshWantedTile();   // the fourth tile is the most wanted now, the standing orders are beside the weekly ones
            for (int i = 0; i < Missions.Active.Count; i++)
            {
                var o = Missions.Active[i]; float y = -i * 170f;
                var row = MakeCard(missionRoot, "Order", new Vector2(0.5f, 0.5f), new Vector2(0, y), new Vector2(940, 150), new Color(0.07f, 0.08f, 0.1f, 0.96f), 0.14f); row.rectTransform.pivot = new Vector2(0.5f, 1f);
                var txt = MakeText(row.transform, "Text", new Vector2(0f, 1f), new Vector2(32, -26), TextAnchor.UpperLeft, 32, new Color(0.93f, 0.91f, 0.86f)); txt.text = o.Title; txt.font = BoldFont(); txt.rectTransform.sizeDelta = new Vector2(640, 44); Fit(txt, 20);
                var prog = MakeText(row.transform, "Progress", new Vector2(0f, 1f), new Vector2(32, -76), TextAnchor.UpperLeft, 24, new Color(0.66f, 0.64f, 0.59f)); prog.text = Missions.Progress(o).Replace("/", " of "); prog.rectTransform.sizeDelta = new Vector2(500, 34);
                var rew = MakeText(row.transform, "Reward", new Vector2(1f, 1f), new Vector2(-32, -20), TextAnchor.UpperRight, 56, new Color(0.96f, 0.68f, 0.24f)); rew.text = "+" + o.reward.ToString("N0", En); Serif(rew); rew.rectTransform.sizeDelta = new Vector2(240, 64);
                var unit = MakeText(row.transform, "Unit", new Vector2(1f, 1f), new Vector2(-32, -86), TextAnchor.UpperRight, 18, new Color(0.66f, 0.64f, 0.59f)); unit.text = Spaced("POINTS"); unit.font = LabelFont(); unit.rectTransform.sizeDelta = new Vector2(240, 26);
                MakeImage(row.transform, "Track", new Vector2(0f, 0f), new Vector2(32, 22), new Vector2(876, 6), new Color(1f, 1f, 1f, 0.1f));
                MakeImage(row.transform, "Fill", new Vector2(0f, 0f), new Vector2(32, 22), new Vector2(876 * Mathf.Clamp01(Missions.Fraction(o)), 6), new Color(0.96f, 0.68f, 0.24f, 0.95f));
            }
            titleSheet.SetActive(true);
        }
        public void HideTitle() { titleSheet.SetActive(false); if (rewardSheet != null && rewardSheet.activeSelf) CloseRewards(); depotSheet.SetActive(false); hudGroup.SetActive(true); Sfx.Music(false); if (garage != null) { garage.SetTitle(false); garage.SetActive(false); } }
        void ShowOrders() { ordersSheet.transform.SetAsLastSibling(); if (curtain != null) curtain.transform.SetAsLastSibling(); ordersSheet.SetActive(true); }   // over whatever sheet asked for it

        /// <summary>Test switch --garage=id: straight into the garage tab with that tank on the turntable.</summary>
        public void ShowGarage(VehicleSpec spec) { depotTab = 1; ShowDepot(); if (garage != null) garage.Show(spec); }
        /// <summary>Test switch --sheet=settings, medals, records, help, orders or pause: that sheet, open over the title.</summary>
        public void TestSheet(string name) { if (name == "settings") ShowSettings(); else if (name == "medals") ShowMedals(); else if (name == "records") ShowRecords(); else if (name == "help") helpSheet.SetActive(true); else if (name == "orders") ShowOrders(); else if (name == "names") { var lc = System.Array.Find(Depot.Leaders, x => x.id == Depot.LeaderId); if (lc != null) ShowNames(lc.id, lc.nation); } else if (name == "pause") { ShowPause(true, true); pauseSheet.transform.SetAsLastSibling(); } }
        /// <summary>Test switch --crewtab: straight into the depot's crew tab.</summary>
        public void ShowCrewTab() { depotTab = 2; ShowDepot(); }
        /// <summary>Test switch --dossier=id: a tank's service record, the tank in the hangar.</summary>
        public void ShowDossier(string id) { depotTab = 1; ShowDepot(); dossierId = id; if (garage != null) garage.Show(VehicleSpec.ById(id)); RefreshDepot(); }

        public void ShowDepot()
        {
            Depot.Load(); dossierId = null; crewRole = null; titleSheet.SetActive(false); endSheet.SetActive(false); depotSheet.SetActive(true);
            if (garage != null) { garage.Show(VehicleSpec.ById(Depot.LeaderId)); garage.SetTitle(true); garage.Frame(true); depotBackdrop.texture = garage.TitleTexture; depotDrag.garage = garage; }
            RefreshDepot(); if (depotScroll != null) depotScroll.verticalNormalizedPosition = 1f;
        }

        void RefreshDepot()
        {
            Tick(depotPoints, Depot.Points); Tick(depotXp, Depot.CrewXp, "", " XP"); Tick(depotGold, Depot.Gold);
            foreach (Transform c in depotRows) Destroy(c.gameObject);
            for (int k = 0; k < 3; k++) { bool on = depotTab == k; depotTabs[k].GetComponent<Image>().color = on ? new Color(0.96f, 0.68f, 0.24f, 1f) : new Color(0f, 0f, 0f, 0f); depotTabs[k].transform.Find("Label").GetComponent<Text>().color = on ? new Color(0.12f, 0.09f, 0.04f) : new Color(0.82f, 0.8f, 0.76f); }
            if (depotTab == 1) { RefreshGarage(); FitDepotRows(); return; }
            if (depotTab == 2) { RefreshCrew(); FitDepotRows(); return; }
            for (int i = 0; i < Depot.Upgrades.Count; i++)
            {
                var u = Depot.Upgrades[i]; float y = -i * 250f;
                var row = MakeImage(depotRows, "Row " + u.id, new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(940, 230), new Color(0.08f, 0.09f, 0.1f, 0.96f)); row.rectTransform.pivot = new Vector2(0.5f, 1f);
                var pic = UiSprite(CardPicture(u.id)); float left = pic != null ? 200f : 30f;
                if (pic != null) { var art = MakeImage(row.transform, "Art", new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(150f, 150f), Color.white); art.sprite = pic; }
                var title = MakeText(row.transform, "Title", new Vector2(0f, 1f), new Vector2(left, -20), TextAnchor.UpperLeft, 44, new Color(0.93f, 0.91f, 0.86f)); title.text = u.title; title.rectTransform.sizeDelta = new Vector2(600, 60);
                var pips = new System.Text.StringBuilder(); for (int k = 0; k < u.MaxLevel; k++) pips.Append(k < u.level ? "■" : "□");
                var lvl = MakeText(row.transform, "Level", new Vector2(1f, 1f), new Vector2(-30, -26), TextAnchor.UpperRight, 40, new Color(0.95f, 0.66f, 0.23f)); lvl.text = pips.ToString(); lvl.rectTransform.sizeDelta = new Vector2(300, 60);
                var desc = MakeText(row.transform, "Desc", new Vector2(0f, 1f), new Vector2(left, -80), TextAnchor.UpperLeft, 28, new Color(0.66f, 0.64f, 0.59f)); desc.text = u.desc; desc.rectTransform.sizeDelta = new Vector2(910 - left, 80);
                bool maxed = u.level >= u.MaxLevel, can = !maxed && Depot.Points >= u.Cost;
                var b = MakeButton(row.transform, maxed ? "Maxed" : $"Upgrade · {u.Cost}", new Vector2(1f, 0f), new Vector2(-190, 50), new Vector2(340, 80), 32, () => { if (Depot.Buy(u)) RefreshDepot(); });
                b.GetComponent<Image>().color = can ? new Color(0.95f, 0.66f, 0.23f, 0.95f) : new Color(0.2f, 0.2f, 0.22f, 0.9f);
                b.transform.Find("Label").GetComponent<Text>().color = can ? new Color(0.1f, 0.08f, 0.05f) : new Color(0.55f, 0.53f, 0.5f);
                b.GetComponent<Button>().interactable = can;
            }
            // camouflage: four swatches under the upgrades
            {
                float y = -Depot.Upgrades.Count * 250f;
                var row = MakeImage(depotRows, "Row camo", new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(940, 230), new Color(0.08f, 0.09f, 0.1f, 0.96f)); row.rectTransform.pivot = new Vector2(0.5f, 1f);
                var title = MakeText(row.transform, "Title", new Vector2(0f, 1f), new Vector2(30, -20), TextAnchor.UpperLeft, 44, new Color(0.93f, 0.91f, 0.86f)); title.text = "Camouflage"; title.rectTransform.sizeDelta = new Vector2(600, 60);
                var desc = MakeText(row.transform, "Desc", new Vector2(0f, 1f), new Vector2(30, -80), TextAnchor.UpperLeft, 28, new Color(0.66f, 0.64f, 0.59f)); desc.text = "The platoon's paint. Bought once, kept for good."; desc.rectTransform.sizeDelta = new Vector2(880, 80);
                var shown = new List<Depot.Camo>(); foreach (var cc in Depot.Camos) if (cc.cost >= 0 || Depot.OwnsCamo(cc)) shown.Add(cc);   // a season's camouflage shows once it is won
                float cw = Mathf.Min(210f, 900f / shown.Count - 10f);
                for (int k = 0; k < shown.Count; k++)
                {
                    var c = shown[k]; bool owned = Depot.OwnsCamo(c), chosen = Depot.CamoId == c.id, can = owned || Depot.Points >= c.cost;
                    var b = MakeButton(row.transform, owned ? c.name : $"{c.name} · {c.cost}", new Vector2(0f, 0f), new Vector2(20f + cw * 0.5f + k * (cw + 10f), 50), new Vector2(cw, 80), cw < 180f ? 21 : 26, () => { if (Depot.PickCamo(c)) RefreshDepot(); });
                    b.GetComponent<Image>().color = chosen ? new Color(0.95f, 0.66f, 0.23f, 0.95f) : can ? new Color(0.2f, 0.22f, 0.24f, 0.95f) : new Color(0.12f, 0.12f, 0.13f, 0.9f);
                    b.transform.Find("Label").GetComponent<Text>().color = chosen ? new Color(0.1f, 0.08f, 0.05f) : can ? new Color(0.93f, 0.91f, 0.86f) : new Color(0.5f, 0.48f, 0.45f);
                    b.GetComponent<Button>().interactable = can && !chosen;
                }
            }
            // veteran nights
            {
                float y = -(Depot.Upgrades.Count + 1) * 250f;
                var row = MakeImage(depotRows, "Row veteran", new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(940, 230), new Color(0.08f, 0.09f, 0.1f, 0.96f)); row.rectTransform.pivot = new Vector2(0.5f, 1f);
                var title = MakeText(row.transform, "Title", new Vector2(0f, 1f), new Vector2(30, -20), TextAnchor.UpperLeft, 44, new Color(0.93f, 0.91f, 0.86f)); title.text = "Veteran nights"; title.rectTransform.sizeDelta = new Vector2(600, 60);
                var desc = MakeText(row.transform, "Desc", new Vector2(0f, 1f), new Vector2(30, -80), TextAnchor.UpperLeft, 28, new Color(0.66f, 0.64f, 0.59f)); desc.text = "The enemy takes half again as many hits; the night pays half again as many points."; desc.rectTransform.sizeDelta = new Vector2(880, 80);
                bool on = Depot.Veteran;
                var b = MakeButton(row.transform, on ? "Veteran: on" : "Veteran: off", new Vector2(1f, 0f), new Vector2(-190, 50), new Vector2(340, 80), 32, () => { Depot.Veteran = !Depot.Veteran; RefreshDepot(); });
                b.GetComponent<Image>().color = on ? new Color(0.95f, 0.66f, 0.23f, 0.95f) : new Color(0.2f, 0.22f, 0.24f, 0.95f);
                b.transform.Find("Label").GetComponent<Text>().color = on ? new Color(0.1f, 0.08f, 0.05f) : new Color(0.93f, 0.91f, 0.86f);
            }
            FitDepotRows();
            // the leader's tank is chosen in the garage now
            if (false)
            {
                float y = -(Depot.Upgrades.Count + 1) * 250f;
                var row = MakeImage(depotRows, "Row leader", new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(940, 230), new Color(0.08f, 0.09f, 0.1f, 0.96f)); row.rectTransform.pivot = new Vector2(0.5f, 1f);
                var title = MakeText(row.transform, "Title", new Vector2(0f, 1f), new Vector2(30, -20), TextAnchor.UpperLeft, 44, new Color(0.93f, 0.91f, 0.86f)); title.text = "Leader's tank"; title.rectTransform.sizeDelta = new Vector2(600, 60);
                var desc = MakeText(row.transform, "Desc", new Vector2(0f, 1f), new Vector2(30, -80), TextAnchor.UpperLeft, 28, new Color(0.66f, 0.64f, 0.59f)); desc.text = "The Firefly's 17-pounder hits twice as hard and reaches farther; the turret is slower."; desc.rectTransform.sizeDelta = new Vector2(880, 80);
                for (int k = 0; k < Depot.Leaders.Length; k++)
                {
                    var c = Depot.Leaders[k]; bool owned = Depot.OwnsLeader(c), chosen = Depot.LeaderId == c.id, can = owned || Depot.Points >= c.cost;
                    var b = MakeButton(row.transform, owned ? c.name : $"{c.name} · {c.cost}", new Vector2(0f, 0f), new Vector2(240 + k * 440, 50), new Vector2(420, 80), 28, () => { if (Depot.PickLeader(c)) { if (garage != null) garage.Show(VehicleSpec.ById(c.id)); RefreshDepot(); } });
                    b.GetComponent<Image>().color = chosen ? new Color(0.95f, 0.66f, 0.23f, 0.95f) : can ? new Color(0.2f, 0.22f, 0.24f, 0.95f) : new Color(0.12f, 0.12f, 0.13f, 0.9f);
                    b.transform.Find("Label").GetComponent<Text>().color = chosen ? new Color(0.1f, 0.08f, 0.05f) : can ? new Color(0.93f, 0.91f, 0.86f) : new Color(0.5f, 0.48f, 0.45f);
                    b.GetComponent<Button>().interactable = can && !chosen;
                }
            }
        }

        /// <summary>The garage: which nation the platoon is, which tank the leader drives, who rides in the hatch.</summary>
        void RefreshGarage()
        {
            var ink = new Color(0.93f, 0.91f, 0.86f); var dim = new Color(0.66f, 0.64f, 0.59f); var amber = new Color(0.95f, 0.66f, 0.23f); string nation = Depot.Nation; float y = 0f;
            if (dossierId != null) { RefreshDossier(); return; }
            // the nation: two flags
            {
                var row = MakeImage(depotRows, "Row nation", new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(940, 210), new Color(0.08f, 0.09f, 0.1f, 0.96f)); row.rectTransform.pivot = new Vector2(0.5f, 1f);
                var title = MakeText(row.transform, "Title", new Vector2(0f, 1f), new Vector2(30, -18), TextAnchor.UpperLeft, 40, ink); title.text = "Nation"; title.rectTransform.sizeDelta = new Vector2(600, 50);
                var flags = Resources.Load<Texture2D>("UI/flags");
                for (int k = 0; k < 2; k++)
                {
                    string id = k == 0 ? "us" : "su"; bool on = nation == id;
                    var b = MakeButton(row.transform, k == 0 ? "United States" : "Soviet Union", new Vector2(0f, 0f), new Vector2(240 + k * 460, 20), new Vector2(440, 120), 30, () => { if (Depot.Nation != id) PlayerPrefs.SetString("theatre", id == "su" ? "kursk" : "normandy"); Depot.Nation = id; RefreshDepot(); });
                    b.GetComponent<Image>().color = on ? new Color(0.95f, 0.66f, 0.23f, 0.95f) : new Color(0.2f, 0.22f, 0.24f, 0.95f);
                    var lbl = b.transform.Find("Label").GetComponent<Text>(); lbl.color = on ? new Color(0.1f, 0.08f, 0.05f) : ink; lbl.alignment = TextAnchor.MiddleLeft; lbl.rectTransform.anchorMin = Vector2.zero; lbl.rectTransform.anchorMax = Vector2.one; lbl.rectTransform.offsetMin = new Vector2(150f, 0f); lbl.rectTransform.offsetMax = Vector2.zero;
                    if (flags != null) { var f = MakeImage(b.transform, "Flag", new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(120f, 90f), Color.white); f.sprite = UiSprite("flags", new Rect(k * flags.width / 2f, 0f, flags.width / 2f, flags.height)); f.preserveAspect = true; }
                }
                y -= 272f;
            }
            // the tanks of the tree
            foreach (var c in Depot.Leaders)
            {
                if (c.nation != nation) continue; var spec = VehicleSpec.ById(c.id); bool here = VehicleSpec.Available(spec);
                bool owned = Depot.OwnsLeader(c), chosen = Depot.LeaderId == c.id, can = here && (owned || Depot.Points >= c.cost);
                var row = MakeImage(depotRows, "Row " + c.id, new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(940, 190), new Color(0.08f, 0.09f, 0.1f, here ? 0.96f : 0.6f)); row.rectTransform.pivot = new Vector2(0.5f, 1f);
                float left = 30f;
                if (garage != null && here)
                {
                    // the tank's own picture from the hangar
                    // a wartime photograph of the type where there is one, else the tank photographed in the hangar
                    var photo = UiSprite("photo_" + (c.id == "kv85" ? "kv1" : c.id)); var pic = MakeImage(row.transform, "Pic", new Vector2(0f, 0.5f), new Vector2(14, 0), new Vector2(255, 170), chosen ? Color.white : new Color(0.85f, 0.85f, 0.85f)); pic.rectTransform.pivot = new Vector2(0f, 0.5f); pic.sprite = Rounded(); pic.type = Image.Type.Sliced; pic.color = new Color(0.03f, 0.03f, 0.04f, 1f);
                    var pmask = new GameObject("Mask", typeof(RectTransform), typeof(RectMask2D)); pmask.transform.SetParent(pic.transform, false); var pmrt = pmask.GetComponent<RectTransform>(); pmrt.anchorMin = pmrt.anchorMax = new Vector2(0.5f, 0.5f); pmrt.sizeDelta = new Vector2(247, 162); pmrt.anchoredPosition = Vector2.zero;
                    var inner = MakeImage(pmask.transform, "Photo", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(255, 170), chosen ? Color.white : new Color(0.85f, 0.85f, 0.85f)); inner.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    if (photo != null) inner.sprite = photo; else { var pt = garage.Portrait(spec); if (pt != null) inner.sprite = Sprite.Create(pt, new Rect(0, 0, pt.width, pt.height), new Vector2(0.5f, 0.5f), 100f); }
                    left = 290f;
                }
                var name = MakeText(row.transform, "Name", new Vector2(0f, 1f), new Vector2(left, -16), TextAnchor.UpperLeft, 36, here ? ink : dim); name.text = c.name + (here ? "" : "  · coming"); name.font = BoldFont(); name.rectTransform.sizeDelta = new Vector2(560, 44);
                var stats = MakeText(row.transform, "Stats", new Vector2(0f, 1f), new Vector2(left, -62), TextAnchor.UpperLeft, 22, amber); stats.text = $"speed {spec.speed:0}  ·  gun {spec.damage:0.#}  ·  reload {spec.reload:0.#} s\nrange {spec.range:0} m  ·  hits {spec.hp:0.#}"; stats.rectTransform.sizeDelta = new Vector2(450, 60);
                var desc = MakeText(row.transform, "Desc", new Vector2(0f, 1f), new Vector2(left, -124), TextAnchor.UpperLeft, 22, dim); desc.rectTransform.sizeDelta = new Vector2(440, 60);
                desc.text = owned && here ? Career.Rank(c.id) + " · " + Career.Xp(c.id).ToString("N0", En) + " XP to spend\n" + StepLine(c.id) : c.desc;   // an owned tank's career, a new one's promise
                if (garage != null && here) { var look = MakeButton(row.transform, "", new Vector2(0f, 0.5f), new Vector2(330, 0), new Vector2(660, 190), 10, () => garage.Show(spec)); look.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f); look.transform.SetAsFirstSibling(); }
                var b = MakeButton(row.transform, chosen ? "Leading" : owned ? "Lead" : $"{c.cost} pts", new Vector2(1f, 0f), new Vector2(-120, 44), new Vector2(200, 64), 26, () => { if (Depot.PickLeader(c)) { if (garage != null) garage.Show(spec); RefreshDepot(); } });
                b.GetComponent<Image>().color = chosen ? new Color(0.95f, 0.66f, 0.23f, 0.95f) : can ? new Color(0.2f, 0.22f, 0.24f, 0.95f) : new Color(0.12f, 0.12f, 0.13f, 0.9f);
                b.transform.Find("Label").GetComponent<Text>().color = chosen ? new Color(0.1f, 0.08f, 0.05f) : can ? ink : new Color(0.5f, 0.48f, 0.45f);
                b.GetComponent<Button>().interactable = can && !chosen;
                if (owned && here)
                {
                    // its service record: lit when a step can be bought; its stars in the corner
                    bool ready = Career.AnyUpgrade(c.id);
                    var up = MakeButton(row.transform, ready ? "Upgrade" : "Service", new Vector2(1f, 0f), new Vector2(-120, 118), new Vector2(200, 64), 26, () => { dossierId = c.id; if (garage != null) garage.Show(spec); RefreshDepot(); if (depotScroll != null) depotScroll.verticalNormalizedPosition = 1f; });
                    up.GetComponent<Image>().color = ready ? new Color(0.95f, 0.66f, 0.23f, 0.95f) : new Color(0.2f, 0.22f, 0.24f, 0.95f);
                    up.transform.Find("Label").GetComponent<Text>().color = ready ? new Color(0.1f, 0.08f, 0.05f) : ink;
                    int stars = Career.Stars(c.id);
                    for (int s = 0; s < 4; s++) { var st = MakeImage(row.transform, "Star", new Vector2(1f, 1f), new Vector2(-118 - 26 * 1.5f + s * 26, -24), new Vector2(22, 22), s < stars ? amber : new Color(0.26f, 0.26f, 0.28f)); st.sprite = StarSprite(); st.rectTransform.pivot = new Vector2(0.5f, 0.5f); }
                }
                y -= 202f;
            }
        }

        string dossierId;   // a tank's service record open in the garage tab

        /// <summary>The three steps of a tank as pips: Gun ■■□ · Engine ■□□ · Armour □□□.</summary>
        static string StepLine(string id)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var m in Career.Modules) { if (sb.Length > 0) sb.Append("  ·  "); sb.Append(m.name).Append(' '); int s = Career.Step(id, m.id); for (int k = 0; k < 3; k++) sb.Append(k < s ? "■" : "□"); }
            return sb.ToString();
        }

        /// <summary>A tank's service record in the garage tab: its rank and the experience it has to spend, its gun, engine
        /// and armour with what the next step does and costs, and its record. The tank itself stands in the hangar above,
        /// with its kill rings on the barrel.</summary>
        void RefreshDossier()
        {
            var ink = new Color(0.93f, 0.91f, 0.86f); var dim = new Color(0.66f, 0.64f, 0.59f); var amber = new Color(0.95f, 0.66f, 0.23f); var card = new Color(0.08f, 0.09f, 0.1f, 0.96f);
            var c = System.Array.Find(Depot.Leaders, x => x.id == dossierId); if (c == null) { dossierId = null; RefreshGarage(); return; }
            string id = c.id; float y = 0f;
            // the head: back, the name, its stars and rank, the experience to spend and the way to the next rank
            {
                var row = MakeImage(depotRows, "Row head", new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(940, 270), card); row.rectTransform.pivot = new Vector2(0.5f, 1f);
                MakeGhost(row.transform, "BACK", new Vector2(0f, 1f), new Vector2(110, -48), new Vector2(170, 60), 22, () => { dossierId = null; RefreshDepot(); });
                var nm = MakeText(row.transform, "Name", new Vector2(0f, 1f), new Vector2(30, -94), TextAnchor.UpperLeft, 66, ink); nm.text = c.name.ToUpperInvariant(); Serif(nm); nm.rectTransform.sizeDelta = new Vector2(600, 80); nm.horizontalOverflow = HorizontalWrapMode.Overflow;
                int stars = Career.Stars(id);
                for (int s = 0; s < 4; s++) { var st = MakeImage(row.transform, "Star", new Vector2(0f, 1f), new Vector2(46 + s * 36, -198), new Vector2(30, 30), s < stars ? amber : new Color(0.26f, 0.26f, 0.28f)); st.sprite = StarSprite(); st.rectTransform.pivot = new Vector2(0.5f, 0.5f); }
                var rk = MakeText(row.transform, "Rank", new Vector2(0f, 1f), new Vector2(190, -184), TextAnchor.UpperLeft, 26, amber); rk.text = Spaced(Career.Rank(id).ToUpperInvariant()); rk.font = LabelFont(); rk.rectTransform.sizeDelta = new Vector2(300, 32);
                var xp = MakeText(row.transform, "Xp", new Vector2(1f, 1f), new Vector2(-30, -60), TextAnchor.UpperRight, 84, amber); xp.text = Career.Xp(id).ToString("N0", En); Serif(xp); xp.rectTransform.sizeDelta = new Vector2(360, 96); xp.verticalOverflow = VerticalWrapMode.Overflow; xp.rectTransform.pivot = new Vector2(1f, 1f);
                var xl = MakeText(row.transform, "XpLabel", new Vector2(1f, 1f), new Vector2(-30, -154), TextAnchor.UpperRight, 20, dim); xl.text = Spaced("XP TO SPEND"); xl.font = LabelFont(); xl.rectTransform.sizeDelta = new Vector2(360, 30); xl.rectTransform.pivot = new Vector2(1f, 1f);
                int next = Career.NextRank(id), floor = Career.RankFloor(id), total = Career.TotalXp(id);
                var bar = MakeImage(row.transform, "Bar", new Vector2(0f, 1f), new Vector2(30, -238), new Vector2(880, 8), new Color(0.18f, 0.18f, 0.2f)); bar.sprite = Rounded(); bar.type = Image.Type.Sliced; bar.rectTransform.pivot = new Vector2(0f, 0.5f);
                float k = next > 0 ? Mathf.Clamp01((total - floor) / (float)(next - floor)) : 1f;
                var fill = MakeImage(row.transform, "Fill", new Vector2(0f, 1f), new Vector2(30, -238), new Vector2(Mathf.Max(8f, 880f * k), 8), amber); fill.sprite = Rounded(); fill.type = Image.Type.Sliced; fill.rectTransform.pivot = new Vector2(0f, 0.5f);
                var nx = MakeText(row.transform, "Next", new Vector2(1f, 1f), new Vector2(-30, -206), TextAnchor.UpperRight, 20, dim); nx.text = next > 0 ? total.ToString("N0", En) + " / " + next.ToString("N0", En) + " XP to the next star" : total.ToString("N0", En) + " XP · every star won"; nx.rectTransform.sizeDelta = new Vector2(560, 28); nx.rectTransform.pivot = new Vector2(1f, 1f);
                y -= 282f;
            }
            // the name on its turret (the paint shop): the first coat free, a new one for gold
            {
                string painted = Career.Name(id);
                var row = MakeImage(depotRows, "Row name", new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(940, 150), card); row.rectTransform.pivot = new Vector2(0.5f, 1f);
                var ey = MakeText(row.transform, "Eyebrow", new Vector2(0f, 1f), new Vector2(30, -24), TextAnchor.UpperLeft, 22, amber); ey.text = Spaced("PAINTED ON THE TURRET"); ey.font = LabelFont(); ey.rectTransform.sizeDelta = new Vector2(540, 30);
                var pn = MakeText(row.transform, "Painted", new Vector2(0f, 1f), new Vector2(30, -62), TextAnchor.UpperLeft, painted != null ? 52 : 34, painted != null ? ink : dim); pn.rectTransform.sizeDelta = new Vector2(540, 64);
                if (painted != null) { pn.text = painted.ToUpperInvariant(); Serif(pn); } else pn.text = "No name yet";
                var pb = MakeButton(row.transform, painted == null ? "Paint a name" : "Repaint · " + Career.RepaintGold + " gold", new Vector2(1f, 0.5f), new Vector2(-190, 0), new Vector2(340, 80), 30, () => ShowNames(id, c.nation));
                pb.GetComponent<Image>().color = painted == null ? new Color(0.95f, 0.66f, 0.23f, 0.95f) : new Color(0.2f, 0.22f, 0.24f, 0.95f);
                pb.transform.Find("Label").GetComponent<Text>().color = painted == null ? new Color(0.1f, 0.08f, 0.05f) : ink;
                y -= 162f;
            }
            // the three steps: gun, engine, armour
            foreach (var m in Career.Modules)
            {
                var mod = m; int step = Career.Step(id, m.id), cost = Career.NextCost(id, m.id); bool can = Career.CanUpgrade(id, m.id), done = cost == 0;
                var row = MakeImage(depotRows, "Row " + m.id, new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(940, 244), card); row.rectTransform.pivot = new Vector2(0.5f, 1f);
                var pic = UiSprite(m.picture); if (pic != null) { var art = MakeImage(row.transform, "Art", new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(150f, 150f), Color.white); art.sprite = pic; }
                var ti = MakeText(row.transform, "Title", new Vector2(0f, 1f), new Vector2(200, -18), TextAnchor.UpperLeft, 44, ink); ti.text = m.name; ti.font = BoldFont(); ti.rectTransform.sizeDelta = new Vector2(300, 56);
                string[] roman = { "I", "II", "III" };
                for (int s = 0; s < 3; s++) { var pip = MakeText(row.transform, "Step", new Vector2(1f, 1f), new Vector2(-210 + s * 80, -24), TextAnchor.UpperCenter, 34, s < step ? amber : new Color(0.3f, 0.3f, 0.32f)); pip.text = roman[s]; Serif(pip); pip.rectTransform.sizeDelta = new Vector2(70, 44); }
                var now = MakeText(row.transform, "Now", new Vector2(0f, 1f), new Vector2(200, -80), TextAnchor.UpperLeft, 26, dim); now.text = step == 0 ? "Standard, as it left the factory" : "Now: " + m.steps[step - 1]; now.rectTransform.sizeDelta = new Vector2(710, 34);
                var nxt = MakeText(row.transform, "Next", new Vector2(0f, 1f), new Vector2(200, -118), TextAnchor.UpperLeft, 26, done ? dim : amber); nxt.text = done ? "Every step fitted" : "Step " + roman[step] + ": " + m.steps[step]; nxt.rectTransform.sizeDelta = new Vector2(710, 34);
                var b = MakeButton(row.transform, done ? "Complete" : "Upgrade · " + cost.ToString("N0", En) + " XP", new Vector2(1f, 0f), new Vector2(-190, 44), new Vector2(340, 72), 30, () => { if (Career.Upgrade(id, mod.id)) { Sfx.LevelUp(); RefreshDepot(); } });
                b.GetComponent<Image>().color = can ? new Color(0.95f, 0.66f, 0.23f, 0.95f) : new Color(0.2f, 0.2f, 0.22f, 0.9f);
                b.transform.Find("Label").GetComponent<Text>().color = can ? new Color(0.1f, 0.08f, 0.05f) : new Color(0.55f, 0.53f, 0.5f);
                b.GetComponent<Button>().interactable = can;
                y -= 256f;
            }
            // its record, and the rings on its barrel
            {
                var row = MakeImage(depotRows, "Row record", new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(940, 214), card); row.rectTransform.pivot = new Vector2(0.5f, 1f);
                var ti = MakeText(row.transform, "Title", new Vector2(0f, 1f), new Vector2(30, -18), TextAnchor.UpperLeft, 44, ink); ti.text = "Service record"; ti.font = BoldFont(); ti.rectTransform.sizeDelta = new Vector2(600, 56);
                var rec = MakeText(row.transform, "Record", new Vector2(0f, 1f), new Vector2(30, -80), TextAnchor.UpperLeft, 26, dim);
                rec.text = Career.Nights(id) + (Career.Nights(id) == 1 ? " night" : " nights") + " · " + Career.Kills(id).ToString("N0", En) + " kills · " + Career.Dawns(id) + (Career.Dawns(id) == 1 ? " dawn" : " dawns") + " · best night " + Career.Best(id).ToString("N0", En);
                rec.rectTransform.sizeDelta = new Vector2(880, 34);
                int rings = Career.Rings(id), toNext = Career.KillsPerRing - Career.Kills(id) % Career.KillsPerRing;
                var rg = MakeText(row.transform, "Rings", new Vector2(0f, 1f), new Vector2(30, -118), TextAnchor.UpperLeft, 26, amber); rg.text = (rings == 0 ? "No kill rings yet" : rings + (rings == 1 ? " kill ring" : " kill rings") + " on the barrel") + (rings < Career.MaxRings ? " · the next in " + toNext + " kills" : ""); rg.rectTransform.sizeDelta = new Vector2(560, 34);
                bool chosen = Depot.LeaderId == id;
                var lead = MakeButton(row.transform, chosen ? "Leading" : "Lead", new Vector2(1f, 0f), new Vector2(-190, 48), new Vector2(340, 80), 30, () => { if (Depot.PickLeader(c)) RefreshDepot(); });
                lead.GetComponent<Image>().color = chosen ? new Color(0.95f, 0.66f, 0.23f, 0.95f) : new Color(0.2f, 0.22f, 0.24f, 0.95f);
                lead.transform.Find("Label").GetComponent<Text>().color = chosen ? new Color(0.1f, 0.08f, 0.05f) : ink; lead.GetComponent<Button>().interactable = !chosen;
            }
        }

        /// <summary>The tank on the turntable takes the name it has now.</summary>
        public void Repainted() { if (garage != null) garage.Repaint(); }
        GameObject namesSheet;
        /// <summary>The paint shop's names for a tank: the ones its nation's crews gave theirs. A tap paints one on the
        /// turret (the first coat free, a new one for gold); the name it has is marked.</summary>
        void ShowNames(string tank, string nation)
        {
            var dim = new Color(0.66f, 0.64f, 0.59f); var amber = new Color(0.95f, 0.66f, 0.23f);
            if (namesSheet != null) Destroy(namesSheet);
            namesSheet = new GameObject("Names", typeof(RectTransform), typeof(Image)); namesSheet.transform.SetParent(canvas.transform, false); Stretch(namesSheet); namesSheet.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.05f, 1f);
            SheetHead(namesSheet.transform, "PAINT SHOP", "NAME HER");
            string now = Career.Name(tank);
            var note = MakeText(namesSheet.transform, "Note", new Vector2(0.5f, 1f), new Vector2(0, -340), TextAnchor.MiddleCenter, 26, dim); note.rectTransform.sizeDelta = new Vector2(900, 40);
            note.text = now == null ? "The first coat is on the house. Crews named their tanks for luck, for home, for a girl." : "A new coat costs " + Career.RepaintGold + " gold · you have " + Depot.Gold.ToString("N0", En); Fit(note, 18);
            var names = Career.Names(nation);
            for (int i = 0; i < names.Length; i++)
            {
                string nm = names[i]; bool current = nm == now;
                var b = MakeGhost(namesSheet.transform, nm, new Vector2(0.5f, 1f), new Vector2(i % 2 == 0 ? -232f : 232f, -440f - (i / 2) * 124f), new Vector2(440f, 108f), 30, () =>
                {
                    if (nm == Career.Name(tank)) return;
                    if (!Career.PaintName(tank, nm)) { Ads.Say("Not enough gold · the shop has more"); return; }
                    Sfx.Pickup(); Destroy(namesSheet); namesSheet = null; if (garage != null) garage.Repaint(); RefreshDepot();
                }, current ? 0.7f : 0.22f);
                var lb = b.transform.Find("Label").GetComponent<Text>(); lb.text = nm.ToUpperInvariant(); Serif(lb, 0.8f); if (current) lb.color = amber;
            }
            MakeGhost(namesSheet.transform, "BACK", new Vector2(0f, 1f), new Vector2(130, -95), new Vector2(200, 80), 26, () => { Destroy(namesSheet); namesSheet = null; });
            namesSheet.transform.SetAsLastSibling(); if (curtain != null) curtain.transform.SetAsLastSibling();
        }

        /// <summary>The crew's experience, and a rewarded ad for more (a few a day), at the head of the crew tab.</summary>
        void XpStrip(ref float y)
        {
            var dim = new Color(0.66f, 0.64f, 0.59f); int left = Depot.AdsLeftToday;
            var row = MakeImage(depotRows, "Row xp", new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(940, 112), new Color(0.07f, 0.1f, 0.14f, 0.96f)); row.rectTransform.pivot = new Vector2(0.5f, 1f);
            var edge = MakeImage(row.transform, "Edge", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(940, 112), new Color(XpBlue.r, XpBlue.g, XpBlue.b, 0.35f)); edge.sprite = Outline(); edge.type = Image.Type.Sliced; edge.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var st = MakeImage(row.transform, "Star", new Vector2(0f, 0.5f), new Vector2(30, 0), new Vector2(46, 46), XpBlue); Currency(st, "icon_crewxp", StarSprite()); st.rectTransform.pivot = new Vector2(0f, 0.5f);
            var lb = MakeText(row.transform, "Label", new Vector2(0f, 1f), new Vector2(96, -18), TextAnchor.UpperLeft, 22, dim); lb.text = "Crew experience · trains the men"; lb.rectTransform.sizeDelta = new Vector2(440, 30);
            var val = MakeText(row.transform, "Value", new Vector2(0f, 1f), new Vector2(96, -48), TextAnchor.UpperLeft, 40, XpBlue); val.text = Depot.CrewXp.ToString("N0", En) + " XP"; val.font = BoldFont(); val.rectTransform.sizeDelta = new Vector2(440, 52); val.verticalOverflow = VerticalWrapMode.Overflow;
            var ad = MakeButton(row.transform, left > 0 ? "Watch an ad · +" + Depot.AdCrewXp + " XP" : "No more ads today", new Vector2(1f, 0f), new Vector2(-195, 66), new Vector2(350, 62), 24, () => { if (Depot.AdsLeftToday > 0) Ads.Rewarded("crewxp", () => { if (Depot.WatchXpAd()) { Sfx.Pickup(); RefreshDepot(); } }); });
            ad.GetComponent<Image>().color = left > 0 ? XpDeep : new Color(0.12f, 0.12f, 0.13f, 0.9f); ad.GetComponent<Button>().interactable = left > 0;
            ad.transform.Find("Label").GetComponent<Text>().color = left > 0 ? new Color(0.96f, 0.97f, 1f) : new Color(0.5f, 0.48f, 0.45f);
            var note = MakeText(row.transform, "Left", new Vector2(1f, 0f), new Vector2(-195, 8), TextAnchor.LowerCenter, 19, dim); note.rectTransform.pivot = new Vector2(0.5f, 0f); note.rectTransform.sizeDelta = new Vector2(350, 26); note.text = left > 0 ? left + " of " + Depot.AdsPerDay + " left today" : "more tomorrow";
            y -= 124f;
        }

        /// <summary>The amber ACE tag on an ace's picture, its top left corner at the given place.</summary>
        void AceTag(Transform parent, Vector2 at)
        {
            var tag = MakeImage(parent, "Ace", new Vector2(0f, 1f), at, new Vector2(92, 34), new Color(0.95f, 0.66f, 0.23f, 0.96f)); tag.sprite = Rounded(); tag.type = Image.Type.Sliced; tag.rectTransform.pivot = new Vector2(0f, 1f);
            var tt = MakeText(tag.transform, "Text", new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleCenter, 20, new Color(0.1f, 0.08f, 0.05f)); tt.text = Spaced("ACE"); tt.font = LabelFont(); tt.rectTransform.pivot = new Vector2(0.5f, 0.5f); tt.rectTransform.sizeDelta = new Vector2(92, 34);
        }

        string crewRole;   // a seat's roster open in the crew tab
        static string Pips(int n, int max) { var sb = new System.Text.StringBuilder(); for (int k = 0; k < max; k++) sb.Append(k < n ? "■" : "□"); return sb.ToString(); }
        static string SpacedPips(int n, int max) => string.Join(" ", Pips(n, max).ToCharArray());
        /// <summary>A button made a quiet row: a dark card with a faint edge (the settings' switches).</summary>
        static void RowLook(GameObject btn)
        {
            btn.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.1f, 0.96f);
            var edge = MakeImage(btn.transform, "Edge", new Vector2(0.5f, 0.5f), Vector2.zero, ((RectTransform)btn.transform).sizeDelta, new Color(1f, 1f, 1f, 0.14f)); edge.sprite = Outline(); edge.type = Image.Type.Sliced; edge.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            edge.transform.SetAsFirstSibling();   // under the label
        }

        /// <summary>The head every sheet has: an amber eyebrow over the name in the display face.</summary>
        static void SheetHead(Transform sheet, string eyebrow, string title)
        {
            var ey = MakeText(sheet, "Eyebrow", new Vector2(0.5f, 1f), new Vector2(0, -150), TextAnchor.MiddleCenter, 26, new Color(0.95f, 0.66f, 0.23f)); ey.text = Spaced(eyebrow); ey.font = LabelFont(); ey.rectTransform.sizeDelta = new Vector2(700, 40);
            var ti = MakeText(sheet, "Title", new Vector2(0.5f, 1f), new Vector2(0, -190), TextAnchor.MiddleCenter, 120, new Color(0.93f, 0.91f, 0.86f)); ti.text = title; Engrave(ti); ti.rectTransform.sizeDelta = new Vector2(900, 150); ti.verticalOverflow = VerticalWrapMode.Overflow;
        }

        /// <summary>A list that scrolls in a masked window of a sheet, from top down to bottom (each measured in from its
        /// edge of the sheet); returns where its rows go, 940 wide, their height set by whoever fills it.</summary>
        static RectTransform ScrollList(Transform sheet, float top, float bottom, out ScrollRect sr)
        {
            var vp = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect)); vp.transform.SetParent(sheet, false);
            var vrt = vp.GetComponent<RectTransform>(); vrt.anchorMin = new Vector2(0.5f, 0f); vrt.anchorMax = new Vector2(0.5f, 1f); vrt.pivot = new Vector2(0.5f, 1f); vrt.offsetMin = new Vector2(-480f, bottom); vrt.offsetMax = new Vector2(480f, -top);
            vp.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);   // something to drag on
            var rows = new GameObject("Rows", typeof(RectTransform)); rows.transform.SetParent(vp.transform, false);
            var rrt = rows.GetComponent<RectTransform>(); rrt.anchorMin = rrt.anchorMax = new Vector2(0.5f, 1f); rrt.pivot = new Vector2(0.5f, 1f); rrt.anchoredPosition = Vector2.zero; rrt.sizeDelta = new Vector2(940f, 0f);
            sr = vp.GetComponent<ScrollRect>(); sr.content = rrt; sr.viewport = vrt; sr.horizontal = false; sr.vertical = true; sr.movementType = ScrollRect.MovementType.Clamped; sr.scrollSensitivity = 40f; sr.inertia = true; sr.decelerationRate = 0.12f;
            return rrt;
        }
        ScrollRect medalScroll, recordScroll;

        /// <summary>Shrinks a line that would not fit its box, down to the given size, instead of wrapping it away.</summary>
        static void Fit(Text t, int min) { t.resizeTextForBestFit = true; t.resizeTextMinSize = min; t.resizeTextMaxSize = t.fontSize; }

        /// <summary>A picture shown whole and square in a thin gold keyline: the crew's portraits.</summary>
        static void Portrait(Transform parent, Vector2 topLeft, float size, Sprite sprite, Color tint)
        {
            var line = MakeImage(parent, "Keyline", new Vector2(0f, 1f), topLeft + new Vector2(-2f, 2f), new Vector2(size + 4f, size + 4f), new Color(0.86f, 0.68f, 0.38f, 0.55f)); line.rectTransform.pivot = new Vector2(0f, 1f);
            var pic = MakeImage(parent, "Pic", new Vector2(0f, 1f), topLeft, new Vector2(size, size), tint); pic.rectTransform.pivot = new Vector2(0f, 1f); pic.sprite = sprite; pic.preserveAspect = true;
        }

        /// <summary>A seat's roster: the nation's three men for it, each with his gift now and at the next level. The one in
        /// the seat rides every night; another is assigned (hired first, for points), and any of them trained with crew experience.</summary>
        void RefreshCrewRole()
        {
            var ink = new Color(0.93f, 0.91f, 0.86f); var dim = new Color(0.66f, 0.64f, 0.59f); var amber = new Color(0.95f, 0.66f, 0.23f); var card = new Color(0.08f, 0.09f, 0.1f, 0.96f);
            string nation = Depot.Nation, role = crewRole; float y = 0f;
            {
                var row = MakeImage(depotRows, "Row seat", new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(940, 170), card); row.rectTransform.pivot = new Vector2(0.5f, 1f);
                MakeGhost(row.transform, "BACK", new Vector2(0f, 1f), new Vector2(110, -48), new Vector2(170, 60), 22, () => { crewRole = null; RefreshDepot(); });
                var ti = MakeText(row.transform, "Title", new Vector2(0f, 1f), new Vector2(30, -92), TextAnchor.UpperLeft, 60, ink); ti.text = Crew.RolePlural(role).ToUpperInvariant(); Engrave(ti); ti.rectTransform.sizeDelta = new Vector2(600, 70); ti.verticalOverflow = VerticalWrapMode.Overflow;
                var hint = MakeText(row.transform, "Hint", new Vector2(1f, 1f), new Vector2(-30, -110), TextAnchor.UpperRight, 22, dim); hint.text = "the one in the seat rides every night"; hint.rectTransform.sizeDelta = new Vector2(420, 32); hint.rectTransform.pivot = new Vector2(1f, 1f);
                y -= 182f;
            }
            XpStrip(ref y);
            const float RowH = 320f, RowPic = 280f, TextX = 332f;   // the whole portrait at the left, his lines beside it
            foreach (var m in Crew.Men)
            {
                if (m.nation != nation || m.role != role || !Crew.Seen(m)) continue; var man = m;
                bool owned = Crew.Owns(m), seated = Crew.InSeat(m); int lvl = Crew.Level(m), train = Crew.NextTrain(m);
                var row = MakeImage(depotRows, "Row " + m.id, new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(940, RowH), card); row.rectTransform.pivot = new Vector2(0.5f, 1f);
                if (seated) { var frame = MakeImage(row.transform, "Seat", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(940, RowH), new Color(0.95f, 0.66f, 0.23f, 0.55f)); frame.sprite = Outline(); frame.type = Image.Type.Sliced; frame.rectTransform.pivot = new Vector2(0.5f, 0.5f); }
                Portrait(row.transform, new Vector2(20, -20), RowPic, UiSprite(Crew.Portrait(m)), owned ? Color.white : new Color(0.55f, 0.55f, 0.58f));
                if (Crew.Ace(m)) AceTag(row.transform, new Vector2(34, -34));
                var nm = MakeText(row.transform, "Name", new Vector2(0f, 1f), new Vector2(TextX, -26), TextAnchor.UpperLeft, 36, ink); nm.text = m.name; nm.font = BoldFont(); nm.rectTransform.sizeDelta = new Vector2(580, 46); Fit(nm, 26);
                var gf = MakeText(row.transform, "Gift", new Vector2(0f, 1f), new Vector2(TextX, -84), TextAnchor.UpperLeft, 26, amber); gf.text = Crew.GiftName(m) + "    " + SpacedPips(lvl, Crew.MaxLevel); gf.rectTransform.sizeDelta = new Vector2(580, 34); Fit(gf, 20);
                var now = MakeText(row.transform, "Now", new Vector2(0f, 1f), new Vector2(TextX, -132), TextAnchor.UpperLeft, 23, dim); now.text = "Level " + lvl + ": " + Crew.EffectOf(m, lvl); now.rectTransform.sizeDelta = new Vector2(580, 30); Fit(now, 18);
                var nx = MakeText(row.transform, "Next", new Vector2(0f, 1f), new Vector2(TextX, -168), TextAnchor.UpperLeft, 23, lvl < Crew.MaxLevel ? ink : dim); nx.text = lvl < Crew.MaxLevel ? "Level " + (lvl + 1) + ": " + Crew.EffectOf(m, lvl + 1) : "Fully trained"; nx.rectTransform.sizeDelta = new Vector2(580, 30); Fit(nx, 18);
                // the seat: assign, or hire first
                bool canSeat = !seated && (owned || Depot.Points >= m.cost);
                var sb = MakeButton(row.transform, seated ? "In the seat" : owned ? "Assign" : "Hire · " + m.cost.ToString("N0", En), new Vector2(1f, 0f), new Vector2(-165, 50), new Vector2(290, 70), 27, () => { if (Crew.Pick(man)) { Sfx.Pickup(); RefreshDepot(); if (garage != null) garage.RefreshCrew(); } });
                sb.GetComponent<Image>().color = seated ? new Color(0.95f, 0.66f, 0.23f, 0.95f) : canSeat ? new Color(0.2f, 0.22f, 0.24f, 0.95f) : new Color(0.12f, 0.12f, 0.13f, 0.9f);
                sb.transform.Find("Label").GetComponent<Text>().color = seated ? new Color(0.1f, 0.08f, 0.05f) : canSeat ? ink : new Color(0.5f, 0.48f, 0.45f); sb.GetComponent<Button>().interactable = canSeat;
                // training: only for a man on the books
                if (owned)
                {
                    bool canTrain = train > 0 && Depot.CrewXp >= train;
                    var tr = MakeButton(row.transform, train > 0 ? "Train · " + train.ToString("N0", En) + " XP" : "Trained", new Vector2(1f, 0f), new Vector2(-475, 50), new Vector2(290, 70), 27, () => { if (Crew.Train(man)) { Sfx.LevelUp(); RefreshDepot(); } });
                    tr.GetComponent<Image>().color = canTrain ? XpDeep : new Color(0.2f, 0.2f, 0.22f, 0.9f);
                    tr.transform.Find("Label").GetComponent<Text>().color = canTrain ? new Color(0.96f, 0.97f, 1f) : new Color(0.55f, 0.53f, 0.5f); tr.GetComponent<Button>().interactable = canTrain;
                }
                y -= RowH + 14f;
            }
        }

        /// <summary>The crew tab: the four men in the leader's tank, their nights together and what they bring; the commander who rides with them.</summary>
        void RefreshCrew()
        {
            var ink = new Color(0.93f, 0.91f, 0.86f); var dim = new Color(0.66f, 0.64f, 0.59f); var amber = new Color(0.95f, 0.66f, 0.23f); string nation = Depot.Nation; float y = 0f;
            if (crewRole != null) { RefreshCrewRole(); return; }
            {
                var row = MakeImage(depotRows, "Row crew", new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(940, 120), new Color(0.08f, 0.09f, 0.1f, 0.96f)); row.rectTransform.pivot = new Vector2(0.5f, 1f);
                var title = MakeText(row.transform, "Title", new Vector2(0f, 1f), new Vector2(30, -16), TextAnchor.UpperLeft, 40, ink); title.text = Depot.CrewName; title.font = BoldFont(); title.rectTransform.sizeDelta = new Vector2(600, 50);
                var sub = MakeText(row.transform, "Sub", new Vector2(0f, 1f), new Vector2(30, -66), TextAnchor.UpperLeft, 24, amber); sub.text = Depot.CrewNights + (Depot.CrewNights == 1 ? " night together · " : " nights together · ") + Depot.CrewBonusText; sub.rectTransform.sizeDelta = new Vector2(880, 40);
                y -= 132f;
            }
            XpStrip(ref y);
            const float TileW = 456f, TilePic = 424f, TileH = 672f;   // the whole portrait, and his three lines under it with room to breathe
            for (int r = 0; r < 4; r++)
            {
                // a portrait tile per man: two per row
                float x = (r % 2 == 0) ? -238f : 238f; float ry = y - (r / 2) * (TileH + 20f);
                var tile = MakeImage(depotRows, "Row man" + r, new Vector2(0.5f, 1f), new Vector2(x, ry), new Vector2(TileW, TileH), new Color(0.08f, 0.09f, 0.1f, 0.96f)); tile.rectTransform.pivot = new Vector2(0.5f, 1f);
                var man = Crew.Chosen(nation, Crew.Roles[r]); string seat = man.role;
                tile.raycastTarget = true; var tb = tile.gameObject.AddComponent<Button>(); tb.onClick.AddListener(() => { UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null); crewRole = seat; Sfx.Click(); RefreshDepot(); if (depotScroll != null) depotScroll.verticalNormalizedPosition = 1f; });
                Portrait(tile.transform, new Vector2((TileW - TilePic) * 0.5f, -16f), TilePic, UiSprite(Crew.Portrait(man)), Color.white);
                if (Crew.Ace(man)) AceTag(tile.transform, new Vector2(30, -30));
                float cy = -16f - TilePic - 30f;   // the lines under the picture
                var role = MakeText(tile.transform, "Role", new Vector2(0.5f, 1f), new Vector2(0, cy), TextAnchor.UpperCenter, 21, amber); role.text = Spaced(Crew.RoleName(man.role).ToUpperInvariant()); role.font = LabelFont(); role.rectTransform.sizeDelta = new Vector2(424, 30);
                var name = MakeText(tile.transform, "Name", new Vector2(0.5f, 1f), new Vector2(0, cy - 44f), TextAnchor.UpperCenter, 32, ink); name.text = man.name; name.font = BoldFont(); name.rectTransform.sizeDelta = new Vector2(424, 42); Fit(name, 24);
                var gift = MakeText(tile.transform, "Gift", new Vector2(0.5f, 1f), new Vector2(0, cy - 104f), TextAnchor.UpperCenter, 22, dim); gift.text = Crew.GiftName(man); gift.rectTransform.sizeDelta = new Vector2(424, 30); Fit(gift, 17);
                var pips = MakeText(tile.transform, "Pips", new Vector2(0.5f, 1f), new Vector2(0, cy - 146f), TextAnchor.UpperCenter, 20, amber); pips.text = SpacedPips(Crew.Level(man), Crew.MaxLevel); pips.rectTransform.sizeDelta = new Vector2(424, 26);
            }
            y -= 2f * TileH + 20f + 12f;
            // the commanders: three portraits
            {
                var row = MakeImage(depotRows, "Row commanders", new Vector2(0.5f, 1f), new Vector2(0, y - 10f), new Vector2(940, 400), new Color(0.08f, 0.09f, 0.1f, 0.96f)); row.rectTransform.pivot = new Vector2(0.5f, 1f);
                var title = MakeText(row.transform, "Title", new Vector2(0f, 1f), new Vector2(30, -18), TextAnchor.UpperLeft, 40, ink); title.text = "Commander"; title.rectTransform.sizeDelta = new Vector2(600, 50);
                var hint = MakeText(row.transform, "Hint", new Vector2(1f, 1f), new Vector2(-30, -26), TextAnchor.UpperRight, 24, dim); hint.text = "one rides with the leader"; hint.rectTransform.sizeDelta = new Vector2(400, 40);
                int k = 0;
                foreach (var c in Depot.Commanders)
                {
                    if (c.nation != nation) continue; bool owned = Depot.OwnsCommander(c), chosen = Depot.CommanderId == c.id, can = owned || Depot.Points >= c.cost; float x = 160f + k * 310f;
                    var pic = MakeImage(row.transform, "Pic", new Vector2(0f, 1f), new Vector2(x, -70f), new Vector2(200f, 200f), chosen ? Color.white : new Color(0.7f, 0.7f, 0.72f)); pic.rectTransform.pivot = new Vector2(0.5f, 1f); pic.sprite = UiSprite(c.picture);
                    if (chosen) { var frame = MakeImage(row.transform, "Frame", new Vector2(0f, 1f), new Vector2(x, -64f), new Vector2(212f, 212f), amber); frame.rectTransform.pivot = new Vector2(0.5f, 1f); frame.transform.SetSiblingIndex(pic.transform.GetSiblingIndex()); }
                    var name = MakeText(row.transform, "Name", new Vector2(0f, 1f), new Vector2(x, -278f), TextAnchor.UpperCenter, 26, ink); name.rectTransform.pivot = new Vector2(0.5f, 1f); name.text = c.name; name.rectTransform.sizeDelta = new Vector2(300, 34);
                    var desc = MakeText(row.transform, "Desc", new Vector2(0f, 1f), new Vector2(x, -308f), TextAnchor.UpperCenter, 20, dim); desc.rectTransform.pivot = new Vector2(0.5f, 1f); desc.text = c.desc; desc.rectTransform.sizeDelta = new Vector2(290, 50);
                    var b = MakeButton(row.transform, chosen ? "Riding" : owned ? "Pick" : $"{c.cost} pts", new Vector2(0f, 0f), new Vector2(x, 34f), new Vector2(220, 56), 24, () => { if (Depot.PickCommander(c)) RefreshDepot(); });
                    b.GetComponent<Image>().color = chosen ? new Color(0.95f, 0.66f, 0.23f, 0.95f) : can ? new Color(0.2f, 0.22f, 0.24f, 0.95f) : new Color(0.12f, 0.12f, 0.13f, 0.9f);
                    b.transform.Find("Label").GetComponent<Text>().color = chosen ? new Color(0.1f, 0.08f, 0.05f) : can ? ink : new Color(0.5f, 0.48f, 0.45f);
                    b.GetComponent<Button>().interactable = can;
                    k++;
                }
            }
        }

        /// <summary>The scroll window's content is as tall as the lowest row.</summary>
        void FitDepotRows()
        {
            float bottom = 0f;
            foreach (Transform c in depotRows) { var r = c as RectTransform; if (r == null) continue; bottom = Mathf.Max(bottom, -r.anchoredPosition.y + r.sizeDelta.y); }
            depotRows.GetComponent<RectTransform>().sizeDelta = new Vector2(940f, bottom + 40f);
            if (depotScroll != null && depotScroll.verticalNormalizedPosition > 1f) depotScroll.verticalNormalizedPosition = 1f;
        }

        public void SetEndPoints(int points) { if (points > 0) Tick(endPoints, points, "+", " depot points"); else endPoints.text = ""; }

        /// <summary>Red chevrons on the screen edge pointing at enemies that are outside the view.</summary>
        public void Indicators(List<Vehicle> foes, Camera cam)
        {
            int used = 0; var rect = canvas.GetComponent<RectTransform>().rect; float hw = rect.width * 0.5f - 40f, hh = rect.height * 0.5f - 150f;
            foreach (var e in foes)
            {
                if (e.dead) continue; var vp = cam.WorldToViewportPoint(e.transform.position);
                if (vp.z > 0f && vp.x > 0.02f && vp.x < 0.98f && vp.y > 0.06f && vp.y < 0.9f) continue;
                var d = new Vector2(vp.x - 0.5f, vp.y - 0.5f); if (vp.z < 0f) d = -d; if (d.sqrMagnitude < 1e-6f) continue;
                d.Normalize(); float k = Mathf.Min(hw / Mathf.Max(0.001f, Mathf.Abs(d.x)), hh / Mathf.Max(0.001f, Mathf.Abs(d.y)));
                Image a;
                if (used < arrows.Count) a = arrows[used];
                else { a = MakeImage(hudGroup.transform, "Arrow", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(54, 54), new Color(0.95f, 0.3f, 0.25f, 0.9f)); a.sprite = arrowSprite; a.rectTransform.pivot = new Vector2(0.5f, 0.5f); arrows.Add(a); }
                a.enabled = true; a.rectTransform.anchoredPosition = d * k; a.rectTransform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f); used++;
            }
            for (int i = used; i < arrows.Count; i++) arrows[i].enabled = false;
        }

        public float radarMul = 1f;   // the spotter's reach
        /// <summary>The radar in the corner: enemies red, guns orange, wingmen and the objective green, ammunition crates
        /// small and gold, ninety metres to the rim, north up.</summary>
        public void Radar(List<Vehicle> foes, List<Vehicle> platoon, Vector3 leader, Vector3 objective, bool hasObjective, List<Vector3> crates)
        {
            int used = 0; float range = (Depot.CommanderBonus == "radar" ? 120f : 90f) * radarMul; const float rim = 110f;
            Image Dot(Vector3 world, Color c, float size)
            {
                Image d; if (used < radarDots.Count) d = radarDots[used]; else { d = MakeImage(radar.transform, "Dot", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10, 10), Color.white); d.sprite = Lightswarm.ProceduralSprites.Glow(16, 0.9f); d.rectTransform.pivot = new Vector2(0.5f, 0.5f); radarDots.Add(d); }
                used++; var o = new Vector2(world.x - leader.x, world.z - leader.z) * (rim / range); if (o.magnitude > rim) o = o.normalized * rim;
                d.enabled = true; d.color = c; d.rectTransform.sizeDelta = new Vector2(size, size); d.rectTransform.anchoredPosition = o; return d;
            }
            foreach (var c in crates) Dot(c, new Color(1f, 0.86f, 0.5f), 7f);
            foreach (var e in foes) if (!e.dead) Dot(e.transform.position, e.spec.isGun ? new Color(1f, 0.55f, 0.3f) : new Color(0.95f, 0.3f, 0.25f), e.spec == VehicleSpec.TigerAce ? 16f : 11f);
            for (int i = 1; i < platoon.Count; i++) if (!platoon[i].dead) Dot(platoon[i].transform.position, new Color(0.45f, 0.9f, 0.5f), 10f);
            if (hasObjective) Dot(objective, new Color(0.35f, 0.95f, 0.45f), 14f);
            for (int i = used; i < radarDots.Count; i++) radarDots[i].enabled = false;
        }

        /// <summary>Small bars over enemies that were hit in the last three seconds.</summary>
        public void HpBars(List<Vehicle> foes, Camera cam, Vehicle boss)
        {
            int used = 0;
            foreach (var e in foes)
            {
                if (e.dead || e == boss || e.hp >= e.spec.hp || Time.time - e.lastHit > 3f) continue;
                var sp = cam.WorldToScreenPoint(e.transform.position + Vector3.up * 3.2f); if (sp.z < 0f) continue;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, sp, null, out var local);
                Image bg, fill;
                if (used < hpBars.Count) { bg = hpBars[used]; fill = hpFills[used]; }
                else { bg = MakeImage(hudGroup.transform, "HpBar", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90, 10), new Color(0f, 0f, 0f, 0.6f)); bg.rectTransform.pivot = new Vector2(0.5f, 0.5f); fill = MakeImage(bg.transform, "Fill", new Vector2(0f, 0.5f), new Vector2(2, 0), new Vector2(86, 6), new Color(0.95f, 0.3f, 0.25f)); fill.rectTransform.pivot = new Vector2(0f, 0.5f); hpBars.Add(bg); hpFills.Add(fill); }
                bg.enabled = fill.enabled = true; bg.rectTransform.anchoredPosition = local; fill.rectTransform.sizeDelta = new Vector2(86f * Mathf.Clamp01(e.hp / e.spec.hp), 6f); used++;
            }
            for (int i = used; i < hpBars.Count; i++) { hpBars[i].enabled = false; hpFills[i].enabled = false; }
        }

        /// <summary>A green chevron on the screen edge toward the objective, with the distance; hidden while it is in view.</summary>
        public void Objective(Vector3 pos, float dist, Camera cam, bool show, string label = null)
        {
            if (!show) { objectiveArrow.enabled = false; objectiveLabel.text = ""; return; }
            var rect = canvas.GetComponent<RectTransform>().rect; float hw = rect.width * 0.5f - 60f, hh = rect.height * 0.5f - 340f;
            var vp = cam.WorldToViewportPoint(pos);
            if (vp.z > 0f && vp.x > 0.05f && vp.x < 0.95f && vp.y > 0.1f && vp.y < 0.85f) { objectiveArrow.enabled = false; objectiveLabel.text = ""; return; }
            var d = new Vector2(vp.x - 0.5f, vp.y - 0.5f); if (vp.z < 0f) d = -d; if (d.sqrMagnitude < 1e-6f) d = Vector2.up; d.Normalize();
            float k = Mathf.Min(hw / Mathf.Max(0.001f, Mathf.Abs(d.x)), hh / Mathf.Max(0.001f, Mathf.Abs(d.y)));
            objectiveArrow.enabled = true; objectiveArrow.rectTransform.anchoredPosition = d * k; objectiveArrow.rectTransform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f);
            objectiveLabel.rectTransform.anchoredPosition = d * k - d * 70f; objectiveLabel.text = label ?? (Mathf.RoundToInt(dist) + " m");
        }

        static Sprite ArrowSprite()
        {
            const int S = 64; var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++) { float u = (x + 0.5f) / S - 0.5f, v = (y + 0.5f) / S; bool inside = v > 0.15f && Mathf.Abs(u) < (v - 0.15f) * 0.55f && v < 0.95f; tex.SetPixel(x, y, new Color(1f, 1f, 1f, inside ? 1f : 0f)); }
            tex.Apply(); return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), S);
        }

        void Highlight(int idx) { for (int i = 0; i < 4; i++) formButtons[i].GetComponent<Image>().color = i == idx ? new Color(0.95f, 0.66f, 0.23f, 0.9f) : new Color(0.03f, 0.04f, 0.06f, 0.6f); for (int i = 0; i < 4; i++) formButtons[i].transform.Find("Label").GetComponent<Text>().color = i == idx ? new Color(0.1f, 0.08f, 0.05f) : new Color(0.93f, 0.91f, 0.86f); }
        static void Stretch(GameObject go) { var rt = go.GetComponent<RectTransform>(); rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; }

        public void Set(float seconds, int platoon)
        {
            int m = Mathf.FloorToInt(seconds / 60f), s = Mathf.FloorToInt(seconds % 60f);
            clock.text = $"{m}:{s:00}"; count.text = platoon.ToString();
        }
        /// <summary>What is in the racks, and which round is loaded.</summary>
        /// <summary>The stick must let these buttons have their own presses.</summary>
        TouchStick handStick;   // the stick, for the buttons made later to be kept out of its taps
        public void HandTo(TouchStick stick) { if (stick == null) return; handStick = stick; stick.Blockers.Add(ammoBtn.GetComponent<RectTransform>()); stick.Blockers.Add(abilityBtn.GetComponent<RectTransform>()); stick.Blockers.Add(orderBtn.GetComponent<RectTransform>()); stick.Blockers.Add(pauseBtnRect); stick.Blockers.Add(airBtn.GetComponent<RectTransform>()); }

        public void SetOrder(string text) { if (orderLabel != null) orderLabel.text = text; }

        // ---- the daily challenge: one night a day, the same for every commander
        Text dailyCount, dailyClock, depotCount; Image dailyPic, dailyEdge; GameObject dailySheet; Transform dailyBody;
        static readonly System.Globalization.CultureInfo En = System.Globalization.CultureInfo.InvariantCulture;

        /// <summary>The day's challenge: its picture, its rule and place, the time to the next one, the record so far and
        /// the last seven days, and the way in.</summary>
        void ShowDaily()
        {
            if (dailySheet == null)
            {
                dailySheet = new GameObject("Daily", typeof(RectTransform), typeof(Image)); dailySheet.transform.SetParent(canvas.transform, false); Stretch(dailySheet); dailySheet.GetComponent<Image>().color = new Color(0.02f, 0.02f, 0.03f, 1f);
                var body = new GameObject("Body", typeof(RectTransform)); body.transform.SetParent(dailySheet.transform, false); Stretch(body); dailyBody = body.transform;
            }
            foreach (Transform c in dailyBody) Destroy(c.gameObject);
            var day = Daily.Today; var rule = Daily.RuleOf(day); bool played = Daily.Played(day);
            Framed(dailyBody, Daily.Picture(day), "campaign_normandy", new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1080, 820), new Color(0.02f, 0.02f, 0.03f, 1f));
            MakeGhost(dailyBody, "BACK", new Vector2(0f, 1f), new Vector2(130, -95), new Vector2(200, 80), 26, () => dailySheet.SetActive(false));
            var ey = MakeText(dailyBody, "Eyebrow", new Vector2(0.5f, 1f), new Vector2(0, -690), TextAnchor.MiddleCenter, 26, OpAmber); ey.text = Spaced("DAILY CHALLENGE · " + Daily.Heading(day)); ey.font = LabelFont(); ey.rectTransform.sizeDelta = new Vector2(1040, 40);
            var ti = MakeText(dailyBody, "Title", new Vector2(0.5f, 1f), new Vector2(0, -770), TextAnchor.MiddleCenter, 100, OpInk); ti.text = rule.name.ToUpperInvariant(); Engrave(ti); ti.horizontalOverflow = HorizontalWrapMode.Overflow; ti.verticalOverflow = VerticalWrapMode.Overflow;
            var chip = MakeChip(dailyBody, "Place", new Vector2(0.5f, 1f), new Vector2(0, -895), 860f, OpAmber, new Color(0.06f, 0.07f, 0.09f, 0.8f)); chip.text = Daily.Place(day);
            var line = MakeText(dailyBody, "Rule", new Vector2(0.5f, 1f), new Vector2(0, -960), TextAnchor.UpperCenter, 32, new Color(0.86f, 0.84f, 0.8f)); line.text = rule.line; line.rectTransform.sizeDelta = new Vector2(900, 100);
            dailyClock = MakeText(dailyBody, "Clock", new Vector2(0.5f, 1f), new Vector2(0, -1085), TextAnchor.MiddleCenter, 22, OpDim); dailyClock.rectTransform.sizeDelta = new Vector2(1040, 36); dailyClock.font = BoldFont();
            // the record: today's best, the days in a row, what the first run of the day pays
            var card = MakeCard(dailyBody, "Record", new Vector2(0.5f, 1f), new Vector2(0, -1130), new Vector2(940, 230), new Color(0.06f, 0.07f, 0.09f, 0.9f), 0.16f); card.rectTransform.pivot = new Vector2(0.5f, 1f);
            string[] heads = { "TODAY'S BEST", "DAYS IN A ROW", played ? "PAID TODAY" : "FIRST RUN PAYS" };
            string[] values = { played ? Daily.Best(day).ToString("N0", En) : "-", Daily.Streak.ToString(En), "+" + (played ? Daily.Reward(Daily.Streak) : Daily.NextReward).ToString("N0", En) };
            for (int i = 0; i < 3; i++)
            {
                float x = -313f + i * 313f;
                var h = MakeText(card.transform, "Head", new Vector2(0.5f, 1f), new Vector2(x, -50), TextAnchor.MiddleCenter, 20, OpDim); h.text = Spaced(heads[i]); h.font = LabelFont(); h.rectTransform.sizeDelta = new Vector2(300, 30);
                var v = MakeText(card.transform, "Value", new Vector2(0.5f, 1f), new Vector2(x, -132), TextAnchor.MiddleCenter, 72, i == 2 ? OpAmber : OpInk); v.text = values[i]; Serif(v); v.rectTransform.sizeDelta = new Vector2(300, 100);
            }
            // the last seven days, today on the right: gold where one was fought
            var week = MakeText(dailyBody, "Week", new Vector2(0.5f, 1f), new Vector2(0, -1420), TextAnchor.MiddleCenter, 20, OpDim); week.text = Spaced("THE LAST SEVEN DAYS"); week.font = LabelFont(); week.rectTransform.sizeDelta = new Vector2(900, 30);
            for (int i = 0; i < 7; i++)
            {
                var d = Daily.DaysAgo(6 - i); bool got = Daily.Played(d), now = i == 6; float x = -330f + i * 110f, sz = now ? 64f : 52f;
                var dot = MakeImage(dailyBody, "Day", new Vector2(0.5f, 1f), new Vector2(x, -1490), new Vector2(sz, sz), got ? OpAmber : now ? OpInk : OpOff); dot.sprite = got ? StarSprite() : Lightswarm.ProceduralSprites.Ring(64, 0.1f); dot.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                var dl = MakeText(dailyBody, "Letter", new Vector2(0.5f, 1f), new Vector2(x, -1548), TextAnchor.MiddleCenter, 20, now ? OpInk : OpDim); dl.text = System.DateTime.ParseExact(d, "yyyyMMdd", En).ToString("ddd", En).ToUpperInvariant(); dl.font = BoldFont(); dl.rectTransform.sizeDelta = new Vector2(100, 30);
            }
            MakePrimary(dailyBody, "INTO THE NIGHT", new Vector2(0.5f, 0f), new Vector2(0, 170), new Vector2(880, 140), 44, () => { dailySheet.SetActive(false); OnDailyChallenge?.Invoke(); });
            dailySheet.transform.SetAsLastSibling(); if (curtain != null) curtain.transform.SetAsLastSibling(); dailySheet.SetActive(true);
            StartCoroutine(DailyClock());
        }

        System.Collections.IEnumerator DailyClock()
        {
            while (dailySheet != null && dailySheet.activeSelf && dailyClock != null)
            {
                var left = Daily.Left; dailyClock.text = Spaced("THE SAME NIGHT FOR EVERY COMMANDER · NEXT IN " + ((int)left.TotalHours).ToString("00", En) + ":" + left.Minutes.ToString("00", En) + ":" + left.Seconds.ToString("00", En));
                yield return new WaitForSecondsRealtime(0.5f);
            }
        }

        // ---- the operations: three fronts, five nights each, up to three stars a night
        GameObject opsSheet; Transform opsBody; Text opsCount; static Sprite starSprite;
        static readonly Color OpAmber = new Color(0.96f, 0.68f, 0.24f), OpInk = new Color(0.93f, 0.91f, 0.86f), OpDim = new Color(0.66f, 0.64f, 0.59f), OpOff = new Color(0.22f, 0.22f, 0.24f);
        static Sprite StarSprite() => starSprite ??= Lightswarm.ProceduralSprites.Star(128);
        /// <summary>A currency's icon: the painted picture (Resources/UI/icon_points, icon_crewxp) in its own colours and a
        /// fifth larger than the drawn shape it replaces; false, with the drawn shape in its tint, when the picture is
        /// missing.</summary>
        static bool Currency(Image img, string painted, Sprite drawn)
        {
            var s = UiSprite(painted);
            if (s == null) { img.sprite = drawn; return false; }
            img.sprite = s; img.color = Color.white; img.preserveAspect = true; img.rectTransform.sizeDelta *= 1.2f; return true;
        }

        /// <summary>The operations sheet from the title: the three operations, then an operation's nights, then a night's briefing.</summary>
        void ShowOperations()
        {
            if (opsSheet == null)
            {
                opsSheet = new GameObject("Operations", typeof(RectTransform), typeof(Image)); opsSheet.transform.SetParent(canvas.transform, false); Stretch(opsSheet); opsSheet.GetComponent<Image>().color = new Color(0.02f, 0.02f, 0.03f, 1f);
                var body = new GameObject("Body", typeof(RectTransform)); body.transform.SetParent(opsSheet.transform, false); Stretch(body); opsBody = body.transform;
            }
            OpsList(); opsSheet.transform.SetAsLastSibling(); if (curtain != null) curtain.transform.SetAsLastSibling(); opsSheet.SetActive(true);
        }

        /// <summary>Clears the sheet for a page: the back button, and the eyebrow and title when there are any.</summary>
        GameObject OpsPage(string eyebrow, string title, System.Action back)
        {
            foreach (Transform c in opsBody) Destroy(c.gameObject);
            var b = MakeGhost(opsBody, "BACK", new Vector2(0f, 1f), new Vector2(130, -95), new Vector2(200, 80), 26, back);
            if (title == null) return b;
            var ey = MakeText(opsBody, "Eyebrow", new Vector2(0.5f, 1f), new Vector2(0, -160), TextAnchor.MiddleCenter, 26, OpAmber); ey.text = Spaced(eyebrow); ey.font = LabelFont(); ey.rectTransform.sizeDelta = new Vector2(1040, 40);
            var ti = MakeText(opsBody, "Title", new Vector2(0.5f, 1f), new Vector2(0, -240), TextAnchor.MiddleCenter, 96, OpInk); ti.text = title; Engrave(ti); ti.horizontalOverflow = HorizontalWrapMode.Overflow; ti.verticalOverflow = VerticalWrapMode.Overflow;
            return b;
        }

        /// <summary>A picture filling a box, cropped to cover it, fading into the colour below at the bottom.</summary>
        void Framed(Transform parent, string picture, string fallback, Vector2 anchor, Vector2 pos, Vector2 size, Color fadeTo)
        {
            var frame = new GameObject("Frame", typeof(RectTransform), typeof(RectMask2D)); frame.transform.SetParent(parent, false);
            var rt = frame.GetComponent<RectTransform>(); rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 1f); rt.sizeDelta = size; rt.anchoredPosition = pos;
            var sp = UiSprite(picture) ?? UiSprite(fallback) ?? UiSprite("campaign_normandy");
            var img = MakeImage(frame.transform, "Pic", new Vector2(0.5f, 0.5f), Vector2.zero, size, Color.white); img.rectTransform.pivot = new Vector2(0.5f, 0.5f); img.raycastTarget = false;
            if (sp != null) { img.sprite = sp; float k = Mathf.Max(size.x / sp.rect.width, size.y / sp.rect.height); img.rectTransform.sizeDelta = new Vector2(sp.rect.width * k, sp.rect.height * k); }
            if (fadeTo.a <= 0f) return;
            var fade = MakeImage(frame.transform, "Fade", new Vector2(0.5f, 0f), Vector2.zero, new Vector2(size.x, size.y * 0.5f), fadeTo); fade.sprite = Lightswarm.ProceduralSprites.GradientDown(64, 1.1f); fade.rectTransform.pivot = new Vector2(0.5f, 0f); fade.raycastTarget = false;
        }

        void StarRow(Transform parent, Vector2 anchor, Vector2 pos, float size, int mask)
        {
            for (int i = 0; i < 3; i++) { var s = MakeImage(parent, "Star", anchor, pos + new Vector2(i * size * 1.15f, 0f), new Vector2(size, size), (mask & (1 << i)) != 0 ? OpAmber : OpOff); s.sprite = StarSprite(); s.rectTransform.pivot = new Vector2(0.5f, 0.5f); s.raycastTarget = false; }
        }

        void OpsList()
        {
            OpsPage("HISTORICAL OPERATIONS", "OPERATIONS", () => opsSheet.SetActive(false));
            var total = MakeText(opsBody, "Total", new Vector2(0.5f, 1f), new Vector2(0, -365), TextAnchor.MiddleCenter, 28, OpDim); total.text = Operations.TotalStars + " of " + Operations.All.Length * 15 + " stars won"; total.rectTransform.sizeDelta = new Vector2(900, 40);
            for (int i = 0; i < Operations.All.Length; i++)
            {
                var op = Operations.All[i]; bool open = Depot.TheatreOpen(op.theatre); float y = -410 - i * 488;
                var card = MakeButton(opsBody, "", new Vector2(0.5f, 1f), new Vector2(0, y - 225), new Vector2(940, 450), 20, () => { if (open) OpNights(op); });
                card.GetComponent<Image>().color = new Color(0.07f, 0.08f, 0.1f, 1f);
                Framed(card.transform, op.cover, op.nights[0].picture, new Vector2(0.5f, 1f), new Vector2(0, -8), new Vector2(924, 270), new Color(0.07f, 0.08f, 0.1f, 1f));
                var edge = MakeImage(card.transform, "Edge", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(940, 450), new Color(1f, 1f, 1f, 0.16f)); edge.sprite = Outline(); edge.type = Image.Type.Sliced; edge.rectTransform.pivot = new Vector2(0.5f, 0.5f); edge.raycastTarget = false;
                var nm = MakeText(card.transform, "Name", new Vector2(0f, 0f), new Vector2(30, 112), TextAnchor.LowerLeft, 50, OpInk); nm.text = op.name.ToUpperInvariant(); Engrave(nm, true, 0.86f, 2f); nm.rectTransform.pivot = new Vector2(0f, 0f); nm.rectTransform.sizeDelta = new Vector2(620, 64);
                var ln = MakeText(card.transform, "Front", new Vector2(0f, 0f), new Vector2(30, 66), TextAnchor.LowerLeft, 26, OpDim); ln.text = op.front; ln.rectTransform.pivot = new Vector2(0f, 0f); ln.rectTransform.sizeDelta = new Vector2(880, 40);
                var got = MakeText(card.transform, "Stars", new Vector2(1f, 0f), new Vector2(-30, 118), TextAnchor.LowerRight, 36, OpAmber); got.text = Operations.Stars(op) + " / " + op.nights.Length * 3; Serif(got, 0.9f); got.rectTransform.pivot = new Vector2(1f, 0f); got.rectTransform.sizeDelta = new Vector2(160, 50);
                var st = MakeImage(card.transform, "Star", new Vector2(1f, 0f), new Vector2(-215, 143), new Vector2(40, 40), OpAmber); st.sprite = StarSprite(); st.rectTransform.pivot = new Vector2(0.5f, 0.5f); st.raycastTarget = false;
                if (!open)
                {
                    var shut = MakeImage(card.transform, "Shut", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(940, 450), new Color(0.01f, 0.01f, 0.02f, 0.72f)); shut.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    var why = MakeText(card.transform, "Why", new Vector2(0.5f, 0.5f), new Vector2(0, 20), TextAnchor.MiddleCenter, 34, OpInk); why.text = op.theatre == "kursk" ? "Opens after your first dawn" : "Opens after your first night"; why.font = BoldFont(); why.rectTransform.sizeDelta = new Vector2(900, 60);
                }
            }
        }

        void OpNights(Operations.Op op)
        {
            OpsPage(op.front.ToUpperInvariant(), op.name.ToUpperInvariant(), OpsList);
            var blurb = MakeText(opsBody, "Blurb", new Vector2(0.5f, 1f), new Vector2(0, -368), TextAnchor.UpperCenter, 28, OpDim); blurb.text = op.blurb; blurb.rectTransform.sizeDelta = new Vector2(920, 110);
            for (int n = 1; n <= op.nights.Length; n++)
            {
                var night = op.nights[n - 1]; bool open = Operations.Open(op, n); int nn = n; float y = -500 - (n - 1) * 258;
                var row = MakeButton(opsBody, "", new Vector2(0.5f, 1f), new Vector2(0, y - 120), new Vector2(940, 240), 20, () => { if (open) OpBrief(op, nn); });
                row.GetComponent<Image>().color = open ? new Color(0.07f, 0.08f, 0.1f, 1f) : new Color(0.04f, 0.045f, 0.055f, 1f);
                Framed(row.transform, night.picture, op.cover, new Vector2(0f, 1f), new Vector2(128, -10), new Vector2(236, 220), new Color(0f, 0f, 0f, 0f));
                if (!open) { var dim = MakeImage(row.transform, "Shut", new Vector2(0f, 1f), new Vector2(128, -120), new Vector2(236, 220), new Color(0.02f, 0.02f, 0.03f, 0.78f)); dim.rectTransform.pivot = new Vector2(0.5f, 0.5f); }
                var edge = MakeImage(row.transform, "Edge", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(940, 240), new Color(1f, 1f, 1f, open ? 0.16f : 0.07f)); edge.sprite = Outline(); edge.type = Image.Type.Sliced; edge.rectTransform.pivot = new Vector2(0.5f, 0.5f); edge.raycastTarget = false;
                var num = MakeText(row.transform, "Night", new Vector2(0f, 1f), new Vector2(270, -26), TextAnchor.UpperLeft, 22, OpAmber); num.text = Spaced("NIGHT " + n); num.font = LabelFont(); num.rectTransform.pivot = new Vector2(0f, 1f); num.rectTransform.sizeDelta = new Vector2(400, 30);
                var nm = MakeText(row.transform, "Name", new Vector2(0f, 1f), new Vector2(270, -58), TextAnchor.UpperLeft, 40, open ? OpInk : OpDim); nm.text = night.name.ToUpperInvariant(); if (open) Engrave(nm, true, 0.8f, 2f); else Serif(nm, 0.8f); nm.rectTransform.pivot = new Vector2(0f, 1f); nm.rectTransform.sizeDelta = new Vector2(640, 52);
                var ln = MakeText(row.transform, "Line", new Vector2(0f, 1f), new Vector2(270, -116), TextAnchor.UpperLeft, 24, OpDim); ln.text = open ? Operations.Conditions(op, night) : "Opens when night " + (n - 1) + " is held until dawn"; ln.rectTransform.pivot = new Vector2(0f, 1f); ln.rectTransform.sizeDelta = new Vector2(640, 70);
                StarRow(row.transform, new Vector2(1f, 1f), new Vector2(-150, -46), 40, Operations.Mask(op.id, n));
            }
        }

        void OpBrief(Operations.Op op, int n)
        {
            var night = op.nights[n - 1]; int mask = Operations.Mask(op.id, n);
            var back = OpsPage(null, null, () => OpNights(op));
            Framed(opsBody, night.picture, op.cover, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1080, 860), new Color(0.02f, 0.02f, 0.03f, 1f));
            back.transform.SetAsLastSibling();
            var ey = MakeText(opsBody, "Eyebrow", new Vector2(0.5f, 1f), new Vector2(0, -700), TextAnchor.MiddleCenter, 26, OpAmber); ey.text = Spaced(op.name.ToUpperInvariant() + " · NIGHT " + n + " OF " + op.nights.Length); ey.font = LabelFont(); ey.rectTransform.sizeDelta = new Vector2(1040, 40);
            var ti = MakeText(opsBody, "Title", new Vector2(0.5f, 1f), new Vector2(0, -770), TextAnchor.MiddleCenter, 96, OpInk); ti.text = night.name.ToUpperInvariant(); Engrave(ti); ti.verticalOverflow = VerticalWrapMode.Overflow;
            var chip = MakeChip(opsBody, "Conditions", new Vector2(0.5f, 1f), new Vector2(0, -905), 720f, OpAmber, new Color(0.06f, 0.07f, 0.09f, 0.8f)); chip.text = Operations.Conditions(op, night);
            var brief = MakeText(opsBody, "Brief", new Vector2(0.5f, 1f), new Vector2(0, -975), TextAnchor.UpperCenter, 32, new Color(0.86f, 0.84f, 0.8f)); brief.text = night.brief; brief.rectTransform.sizeDelta = new Vector2(900, 170);
            var head = MakeText(opsBody, "Goals", new Vector2(0.5f, 1f), new Vector2(0, -1190), TextAnchor.MiddleCenter, 24, OpAmber); head.text = Spaced("TONIGHT'S STARS"); head.font = LabelFont(); head.rectTransform.sizeDelta = new Vector2(900, 36);
            string[] goals = { "Hold until dawn", night.second.text, night.third.text };
            for (int i = 0; i < 3; i++)
            {
                float y = -1255 - i * 82;
                var st = MakeImage(opsBody, "Star", new Vector2(0.5f, 1f), new Vector2(-330, y), new Vector2(50, 50), (mask & (1 << i)) != 0 ? OpAmber : OpOff); st.sprite = StarSprite(); st.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                var g = MakeText(opsBody, "Goal", new Vector2(0.5f, 1f), new Vector2(70, y), TextAnchor.MiddleLeft, 34, OpInk); g.text = goals[i]; g.rectTransform.sizeDelta = new Vector2(740, 50);
            }
            MakePrimary(opsBody, "INTO THE NIGHT", new Vector2(0.5f, 0f), new Vector2(0, 170), new Vector2(880, 140), 44, () => { opsSheet.SetActive(false); OnOperation?.Invoke(op.id, n); });
        }

        /// <summary>The way in, chosen before the night: three cards with the country, what waits there and what it pays.</summary>
        public void ShowRoutes(System.Action go)
        {
            routeGo = go;
            if (routeSheet == null)
            {
                routeSheet = new GameObject("Routes", typeof(RectTransform), typeof(Image)); routeSheet.transform.SetParent(canvas.transform, false); Stretch(routeSheet); routeSheet.GetComponent<Image>().color = new Color(0.02f, 0.02f, 0.03f, 1f);
                var ey = MakeText(routeSheet.transform, "Eyebrow", new Vector2(0.5f, 1f), new Vector2(0, -80), TextAnchor.MiddleCenter, 26, new Color(0.96f, 0.68f, 0.24f)); ey.text = Spaced("TONIGHT'S ORDERS"); ey.font = LabelFont();
                var ti = MakeText(routeSheet.transform, "Title", new Vector2(0.5f, 1f), new Vector2(0, -275), TextAnchor.MiddleCenter, 100, new Color(0.93f, 0.91f, 0.86f)); ti.text = "CHOOSE THE WAY IN"; Engrave(ti); ti.verticalOverflow = VerticalWrapMode.Overflow; ti.horizontalOverflow = HorizontalWrapMode.Overflow;
                // the three fronts across the top; a closed one says what opens it
                string[] th = { "normandy", "ardennes", "kursk" };
                for (int k = 0; k < 3; k++)
                {
                    string tid = th[k];
                    var tab = MakeButton(routeSheet.transform, "", new Vector2(0.5f, 1f), new Vector2(-310 + k * 310, -165), new Vector2(290, 80), 28, () => { if (!Depot.TheatreOpen(tid)) return; sheetTheatre = tid; PlayerPrefs.SetString("theatre", tid); PlayerPrefs.Save(); OnTheatre?.Invoke(tid); FillRoutes(); });
                    theatreTabs[k] = tab.GetComponent<Image>(); theatreLabels[k] = tab.GetComponentInChildren<Text>();
                }
                string[] ids = { "village", "open", "bocage" };
                for (int i = 0; i < 3; i++)
                {
                    string id = ids[i]; float y = -400 - i * 470;
                    var card = MakeButton(routeSheet.transform, "", new Vector2(0.5f, 1f), new Vector2(0, y - 215), new Vector2(940, 430), 20, () => { OnRoute?.Invoke(id); routeSheet.SetActive(false); routeGo?.Invoke(); });
                    card.GetComponent<Image>().color = new Color(0.07f, 0.08f, 0.1f, 1f);
                    var mask = new GameObject("Mask", typeof(RectTransform), typeof(RectMask2D)); mask.transform.SetParent(card.transform, false); var mkrt = mask.GetComponent<RectTransform>(); mkrt.anchorMin = mkrt.anchorMax = new Vector2(0.5f, 1f); mkrt.pivot = new Vector2(0.5f, 1f); mkrt.sizeDelta = new Vector2(924, 250); mkrt.anchoredPosition = new Vector2(0, -8);
                    var pic = MakeImage(mask.transform, "Pic", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(924, 924), Color.white); pic.rectTransform.pivot = new Vector2(0.5f, 0.5f); routePics[i] = pic;
                    var fade = MakeImage(mask.transform, "Fade", new Vector2(0.5f, 0f), Vector2.zero, new Vector2(924, 140), new Color(0.07f, 0.08f, 0.1f, 1f)); fade.sprite = Lightswarm.ProceduralSprites.GradientDown(64, 1.1f); fade.rectTransform.pivot = new Vector2(0.5f, 0f);
                    var edge = MakeImage(card.transform, "Edge", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(940, 430), new Color(1f, 1f, 1f, 0.16f)); edge.sprite = Outline(); edge.type = Image.Type.Sliced; edge.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    var nm = MakeText(card.transform, "Name", new Vector2(0f, 0f), new Vector2(30, 150), TextAnchor.LowerLeft, 44, new Color(0.96f, 0.94f, 0.9f)); routeNames[i] = nm; nm.font = BoldFont(); nm.rectTransform.pivot = new Vector2(0f, 0f); nm.rectTransform.sizeDelta = new Vector2(640, 56);
                    var py = MakeText(card.transform, "Pay", new Vector2(1f, 0f), new Vector2(-30, 154), TextAnchor.LowerRight, 30, new Color(0.96f, 0.68f, 0.24f)); routePays[i] = py; py.font = BoldFont(); py.rectTransform.pivot = new Vector2(1f, 0f); py.rectTransform.sizeDelta = new Vector2(260, 44);
                    var ln = MakeText(card.transform, "Line", new Vector2(0f, 0f), new Vector2(30, 24), TextAnchor.UpperLeft, 26, new Color(0.78f, 0.76f, 0.72f)); routeLines[i] = ln; ln.rectTransform.pivot = new Vector2(0f, 0f); ln.rectTransform.sizeDelta = new Vector2(880, 118);
                }
                // the three modes under the three ways in, side by side: the last stand, the convoy, the night raid
                ModeCard(-317f, "LAST STAND", "Hold the crossroads", "Ten waves; dig in between them.", () => { routeSheet.SetActive(false); OnStand?.Invoke(sheetTheatre); }, out standPic, out standBestText);
                ModeCard(0f, "CONVOY", "Bring the trucks through", "Five trucks, 900 m of ambushes.", () => { if (!Battle.ConvoyReady(sheetTheatre)) return; routeSheet.SetActive(false); OnConvoy?.Invoke(sheetTheatre); }, out convoyPic, out convoyBestText);
                ModeCard(317f, "NIGHT RAID", "Blow the depot", "No lights; mind the flares.", () => { routeSheet.SetActive(false); OnSneak?.Invoke(sheetTheatre); }, out sneakPic, out sneakBestText);
            }
            sheetTheatre = PlayerPrefs.GetString("theatre", "normandy"); if (!Depot.TheatreOpen(sheetTheatre)) sheetTheatre = "normandy"; FillRoutes();
            routeSheet.transform.SetAsLastSibling(); if (curtain != null) curtain.transform.SetAsLastSibling(); routeSheet.SetActive(true);
        }

        /// <summary>A half-width card for a mode under the three ways in: the picture behind the words, an amber edge,
        /// the best on this front at the bottom right.</summary>
        void ModeCard(float x, string eyebrow, string name, string line, System.Action go, out Image pic, out Text best)
        {
            var card = MakeButton(routeSheet.transform, "", new Vector2(0.5f, 1f), new Vector2(x, -1890), new Vector2(306, 200), 20, go);
            card.GetComponent<Image>().color = new Color(0.08f, 0.06f, 0.04f, 1f);
            var mask = new GameObject("Mask", typeof(RectTransform), typeof(RectMask2D)); mask.transform.SetParent(card.transform, false); var mkrt = mask.GetComponent<RectTransform>(); mkrt.anchorMin = mkrt.anchorMax = new Vector2(0.5f, 0.5f); mkrt.pivot = new Vector2(0.5f, 0.5f); mkrt.sizeDelta = new Vector2(294, 188);
            pic = MakeImage(mask.transform, "Pic", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(294, 294), new Color(0.8f, 0.8f, 0.8f)); pic.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var shade = MakeImage(mask.transform, "Shade", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(294, 188), new Color(0.03f, 0.03f, 0.04f, 0.6f)); shade.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var edge = MakeImage(card.transform, "Edge", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(306, 200), new Color(0.96f, 0.68f, 0.24f, 0.6f)); edge.sprite = Outline(); edge.type = Image.Type.Sliced; edge.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var ey = MakeText(card.transform, "Eyebrow", new Vector2(0f, 1f), new Vector2(16, -14), TextAnchor.UpperLeft, 18, new Color(0.96f, 0.68f, 0.24f)); ey.text = Spaced(eyebrow); ey.font = LabelFont(); ey.rectTransform.sizeDelta = new Vector2(280, 28);
            var nm = MakeText(card.transform, "Name", new Vector2(0f, 1f), new Vector2(16, -40), TextAnchor.UpperLeft, 26, new Color(0.96f, 0.94f, 0.9f)); nm.text = name; nm.font = BoldFont(); nm.rectTransform.sizeDelta = new Vector2(280, 66);
            var ln = MakeText(card.transform, "Line", new Vector2(0f, 1f), new Vector2(16, -108), TextAnchor.UpperLeft, 18, new Color(0.8f, 0.78f, 0.74f)); ln.text = line; ln.rectTransform.sizeDelta = new Vector2(280, 48);
            best = MakeText(card.transform, "Best", new Vector2(1f, 0f), new Vector2(-14, 10), TextAnchor.LowerRight, 19, new Color(0.96f, 0.68f, 0.24f)); best.font = BoldFont(); best.rectTransform.sizeDelta = new Vector2(290, 30);
        }

        /// <summary>A picture filling a mode card's window, cut at the edges.</summary>
        static void Cover(Image pic, Sprite sp) { if (sp == null) return; pic.sprite = sp; float c = Mathf.Max(294f / sp.rect.width, 188f / sp.rect.height); pic.rectTransform.sizeDelta = new Vector2(sp.rect.width * c, sp.rect.height * c); }

        /// <summary>The three ways in of the chosen front, and the front tabs lit, dimmed or closed.</summary>
        void FillRoutes()
        {
            bool k = sheetTheatre == "kursk";
            string[] pics = k ? new[] { "route_kursk_village", "route_kursk_steppe", "route_kursk_belts" } : new[] { "route_village", "route_open", "route_bocage" };
            string[] names = k ? new[] { "Through the village", "Across the steppe", "Along the tree belts" } : new[] { "Through the village", "Across the open fields", "Through the bocage" };
            string[] lines = k
                ? new[] { "Whitewashed huts and a church on every other field. Panzergrenadiers among the houses, Tigers in the lanes.", "Wheat to the horizon, some of it burning. The Panzerkeils come straight at you, Tigers at the tip.", "Wattle fences and birch belts. Short sight, hedgehogs by the roads, tank hunters close." }
                : new[] { "Farms and houses on every other field. Anti-tank guns in the gardens, infantry in the lanes. Cover for you and for them.", "Few hedges, long sight lines. The tanks come at you in the open - and you see them coming.", "Hedges on every side and trees along them. Short sight, tank hunters close. Slow, dark and dangerous." };
            string[] pays = k ? new[] { "points +38%", "points +27%", "points +32%" } : new[] { "points +20%", "points +10%", "points +15%" };   // Kursk pays 15% more on top
            if (standBestText != null)
            {
                int best = Battle.StandBest(sheetTheatre); standBestText.text = best > 0 ? "best: wave " + best : "not held yet";
                Cover(standPic, UiSprite("stand_" + sheetTheatre) ?? UiSprite(sheetTheatre == "kursk" ? "route_kursk_village" : sheetTheatre == "ardennes" ? "campaign_ardennes" : "campaign_lastpush"));
                bool ready = Battle.ConvoyReady(sheetTheatre); int cb = Battle.ConvoyBest(sheetTheatre);
                convoyBestText.text = !ready ? "trucks coming" : cb > 0 ? "best: " + cb + " of " + Battle.ConvoyTrucks + " through" : "not run yet";
                Cover(convoyPic, UiSprite("convoy_" + sheetTheatre) ?? UiSprite(sheetTheatre == "kursk" ? "route_kursk_steppe" : "route_open")); convoyPic.color = ready ? new Color(0.8f, 0.8f, 0.8f) : new Color(0.3f, 0.3f, 0.32f);
                int nb = Battle.SneakBest(sheetTheatre); sneakBestText.text = nb == 2 ? "best: unseen" : nb == 1 ? "best: destroyed" : "not done yet";
                Cover(sneakPic, UiSprite("sneak_" + sheetTheatre) ?? UiSprite(sheetTheatre == "kursk" ? "route_kursk_belts" : "route_bocage"));
            }
            for (int i = 0; i < 3; i++) { routePics[i].sprite = UiSprite(pics[i]); routeNames[i].text = names[i]; routeLines[i].text = lines[i]; routePays[i].text = pays[i]; }
            string[] th = { "normandy", "ardennes", "kursk" }; string[] label = { "NORMANDY", "ARDENNES", "KURSK" }; string[] shut = { "", "after 1 night", "after 1 dawn" };
            for (int t = 0; t < 3; t++)
            {
                bool open = Depot.TheatreOpen(th[t]), on = th[t] == sheetTheatre;
                theatreTabs[t].color = on ? new Color(0.96f, 0.68f, 0.24f) : open ? new Color(0.1f, 0.11f, 0.13f) : new Color(0.05f, 0.05f, 0.06f);
                theatreLabels[t].text = open ? label[t] : label[t] + "\n<size=18>" + shut[t] + "</size>"; theatreLabels[t].supportRichText = true;
                theatreLabels[t].color = on ? new Color(0.08f, 0.07f, 0.05f) : open ? new Color(0.93f, 0.91f, 0.86f) : new Color(0.45f, 0.44f, 0.42f);
            }
        }

        public void SetAmmo(int ap, int he, bool loadHe)
        {
            if (ammoKind == null) return;
            ammoKind.text = loadHe ? "HE" : "AP"; ammoKind.color = loadHe ? new Color(1f, 0.55f, 0.3f) : new Color(0.96f, 0.68f, 0.24f);
            ammoLabel.text = (loadHe ? he : ap) + "\n" + (loadHe ? ap + " AP" : he + " HE");
            ammoBtn.GetComponent<Image>().color = (loadHe ? he : ap) > 0 ? new Color(0.08f, 0.09f, 0.11f, 0.85f) : new Color(0.3f, 0.08f, 0.06f, 0.9f);
        }

        /// <summary>The commander's button: his portrait, the cooldown draining down it, a glow while his order lasts.</summary>
        /// <summary>The AIR button: hidden until the planes are on station, filling while the next strike is readied,
        /// lit while it waits for a tap on the target.</summary>
        public void SetAir(bool has, float charged, string label, bool armed)
        {
            if (airBtn == null) return;
            if (airBtn.activeSelf != has) airBtn.SetActive(has);
            if (!has) return;
            airFill.fillAmount = 1f - Mathf.Clamp01(charged); if (airLabel.text != label) airLabel.text = label;
            airBtn.transform.Find("Edge").GetComponent<Image>().color = armed ? new Color(1f, 0.85f, 0.4f, 0.95f) : charged >= 1f ? new Color(0.96f, 0.68f, 0.24f, 0.85f) : new Color(0.96f, 0.68f, 0.24f, 0.3f);
        }

        public void SetAbility(bool has, float charged, bool running)
        {
            if (abilityBtn == null) return;
            if (abilityBtn.activeSelf != has) abilityBtn.SetActive(has);
            if (!has) return;
            if (abilityPic.sprite == null) { var c = System.Array.Find(Depot.Commanders, x => x.id == Depot.CommanderId && x.nation == Depot.Nation); if (c != null) abilityPic.sprite = UiSprite(c.picture); }
            abilityFill.fillAmount = 1f - Mathf.Clamp01(charged);
            var edge = abilityBtn.transform.Find("Edge").GetComponent<Image>();
            edge.color = running ? new Color(1f, 0.85f, 0.4f, 0.95f) : charged >= 1f ? new Color(0.96f, 0.68f, 0.24f, 0.85f) : new Color(0.96f, 0.68f, 0.24f, 0.35f);
            abilityPic.color = charged >= 1f ? Color.white : new Color(0.55f, 0.55f, 0.58f);
        }

        /// <summary>The ace's name over his turret, with what is left of him under it; null hides it.</summary>
        Text nameText; Image nameBar, nameFill;
        public void NamePlate(string name, Vector3 world, Camera cam, float fraction)
        {
            if (nameText == null)
            {
                nameText = MakeText(hudGroup.transform, "AceName", new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.LowerCenter, 26, new Color(1f, 0.55f, 0.45f)); nameText.font = BoldFont(); nameText.rectTransform.sizeDelta = new Vector2(500, 40);
                nameBar = MakeImage(hudGroup.transform, "AceBar", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220, 8), new Color(0.1f, 0.05f, 0.05f, 0.85f)); nameBar.sprite = Rounded(); nameBar.type = Image.Type.Sliced;
                nameFill = MakeImage(hudGroup.transform, "AceFill", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220, 8), new Color(0.95f, 0.35f, 0.3f, 0.95f)); nameFill.sprite = Rounded(); nameFill.type = Image.Type.Sliced;
            }
            bool on = name != null; if (nameText.enabled != on) { nameText.enabled = on; nameBar.enabled = on; nameFill.enabled = on; }
            if (!on) return;
            var vp = cam.WorldToViewportPoint(world); if (vp.z < 0f) { nameText.enabled = nameBar.enabled = nameFill.enabled = false; return; }
            var rect = canvas.GetComponent<RectTransform>().rect; var at = new Vector2((vp.x - 0.5f) * rect.width, (vp.y - 0.5f) * rect.height);
            nameText.text = name; nameText.rectTransform.anchoredPosition = at + new Vector2(0f, 14f);
            nameBar.rectTransform.anchoredPosition = at; nameFill.rectTransform.anchoredPosition = at - new Vector2(110f * (1f - Mathf.Clamp01(fraction)), 0f);
            nameFill.rectTransform.sizeDelta = new Vector2(220f * Mathf.Clamp01(fraction), 8f);
        }

        public void SetTally(int kills, int score) { tally.text = kills == 0 && score == 0 ? "" : $"{kills} kills · {score}"; }

        // the reload arc: a ring under the leader that fills while the gun reloads, gone when it is ready
        Transform reloadRing; Image reloadImg; float slowFor; bool slowOffered;
        public void ReloadArc(Vector3 at, float fraction, Camera cam)
        {
            if (reloadImg == null) { reloadImg = MakeImage(hudGroup.transform, "Reload", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(96, 96), new Color(1f, 0.75f, 0.35f, 0.7f)); reloadImg.sprite = Lightswarm.ProceduralSprites.Ring(96, 0.1f); reloadImg.type = Image.Type.Filled; reloadImg.fillMethod = Image.FillMethod.Radial360; reloadImg.fillOrigin = 2; reloadImg.fillClockwise = true; }
            if (fraction >= 1f) { reloadImg.enabled = false; return; }
            var vp = cam.WorldToViewportPoint(at); if (vp.z < 0f) { reloadImg.enabled = false; return; }
            var rect = canvas.GetComponent<RectTransform>().rect; reloadImg.enabled = true; reloadImg.fillAmount = fraction; reloadImg.rectTransform.anchoredPosition = new Vector2((vp.x - 0.5f) * rect.width, (vp.y - 0.5f) * rect.height);
        }

        public void SetLeader(int hp, int max) { var s = new System.Text.StringBuilder("LEADER "); for (int i = 0; i < max; i++) s.Append(i < hp ? "■" : "□"); leaderHp.text = s.ToString(); }
        public void SetLevel(int level, float progress) { levelText.text = $"Level {level}"; levelFill.rectTransform.sizeDelta = new Vector2(960f * Mathf.Clamp01(progress), 8f); }
        public void Toast(string text, float seconds = 2.2f) { toast.text = text; toastLeft = seconds; }
        /// <summary>A red blink over the screen: the leader was hit.</summary>
        public void Flash() { flash.color = new Color(0.8f, 0.1f, 0.05f, 0.38f); }

        /// <summary>A little number rising over a point of the field: score, points, a kill.</summary>
        /// <summary>A briefing card at the top when an objective is set: its picture, its name, one line; gone after a few seconds.</summary>
        public void Briefing(string picture, string title, string text)
        {
            if (briefing == null)
            {
                var card = MakeCard(hudGroup.transform, "Briefing", new Vector2(0.5f, 1f), new Vector2(0, -250), new Vector2(940, 200), new Color(0.05f, 0.06f, 0.08f, 0.9f), 0.2f); card.rectTransform.pivot = new Vector2(0.5f, 1f); briefing = card.gameObject;
                var mask = new GameObject("Mask", typeof(RectTransform), typeof(RectMask2D)); mask.transform.SetParent(card.transform, false); var mkrt = mask.GetComponent<RectTransform>(); mkrt.anchorMin = mkrt.anchorMax = new Vector2(0f, 0.5f); mkrt.pivot = new Vector2(0f, 0.5f); mkrt.sizeDelta = new Vector2(200, 180); mkrt.anchoredPosition = new Vector2(10, 0);
                briefingPic = MakeImage(mask.transform, "Pic", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200, 200), Color.white); briefingPic.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                briefingTitle = MakeText(card.transform, "Title", new Vector2(0f, 1f), new Vector2(230, -22), TextAnchor.UpperLeft, 40, new Color(0.96f, 0.68f, 0.24f)); briefingTitle.font = BoldFont(); briefingTitle.rectTransform.sizeDelta = new Vector2(690, 50);
                briefingText = MakeText(card.transform, "Text", new Vector2(0f, 1f), new Vector2(230, -76), TextAnchor.UpperLeft, 27, new Color(0.9f, 0.88f, 0.84f)); briefingText.rectTransform.sizeDelta = new Vector2(690, 110);
            }
            var sp = UiSprite(picture); briefingPic.sprite = sp; briefingPic.enabled = sp != null; if (sp != null) { float cover = Mathf.Max(200f / sp.rect.width, 200f / sp.rect.height); briefingPic.rectTransform.sizeDelta = new Vector2(sp.rect.width * cover, sp.rect.height * cover); }
            briefingTitle.text = title; briefingText.text = text; briefing.SetActive(true); briefingLeft = 4.5f;   // a wide picture fills the square above, cut at the sides
        }

        public void Popup(Vector3 world, string text, Color color)
        {
            Text t = popupPool.Count > 0 ? popupPool.Pop() : MakeText(hudGroup.transform, "Popup", new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleCenter, 44, color);
            t.gameObject.SetActive(true); t.text = text; t.color = color; t.fontStyle = FontStyle.Bold;
            popups.Add(new Rising { t = t, life = 1.1f, world = world });
        }
        public void ShowBoss(string name) { bossName.text = name.ToUpperInvariant(); bossBar.SetActive(true); }
        public void SetBoss(float frac) { bossFill.rectTransform.sizeDelta = new Vector2(960f * Mathf.Clamp01(frac), 10f); }
        public void HideBoss() { bossBar.SetActive(false); }

        /// <summary>The cards to choose from, under an eyebrow (LEVEL UP, SUPPLIES FOR THE NIGHT); under them the ads the
        /// player may ask for, where offered: three other cards, all three taken.</summary>
        public void ShowCards(List<Card> cards, System.Action<string> onPick, string eyebrow = "LEVEL UP", System.Action again = null, System.Action all = null)
        {
            foreach (Transform c in cardRoot) Destroy(c.gameObject);
            if (cardsEyebrow != null) cardsEyebrow.text = Spaced(eyebrow);
            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                var b = MakeButton(cardRoot, card.title, new Vector2(0.5f, 0.5f), new Vector2(0, 300 - i * 290), new Vector2(900, 250), 56, () => { sheet.SetActive(false); onPick(card.id); });
                b.GetComponent<Image>().color = card.rare ? new Color(0.16f, 0.12f, 0.05f, 0.97f) : new Color(0.08f, 0.09f, 0.1f, 0.96f);
                var title = b.transform.Find("Label").GetComponent<Text>(); if (card.rare) title.color = new Color(0.95f, 0.66f, 0.23f);
                title.alignment = TextAnchor.UpperLeft; title.rectTransform.anchorMin = title.rectTransform.anchorMax = title.rectTransform.pivot = new Vector2(0f, 1f);
                var pic = UiSprite(CardPicture(card.id)); float left = pic != null ? 262f : 30f;
                if (pic != null) { var art = MakeImage(b.transform, "Art", new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(214f, 214f), Color.white); art.sprite = pic; }
                title.rectTransform.anchoredPosition = new Vector2(left, -22f); title.rectTransform.sizeDelta = new Vector2(900f - left - 20f, 70f); title.text = card.title.ToUpperInvariant(); Serif(title, 0.66f);
                var desc = MakeText(b.transform, "Desc", new Vector2(0f, 1f), new Vector2(left, -100f), TextAnchor.UpperLeft, 34, new Color(0.66f, 0.64f, 0.59f));
                desc.rectTransform.sizeDelta = new Vector2(900f - left - 20f, 130f); desc.text = card.desc;
            }
            if (again != null) MakeGhost(cardRoot, "Other cards · watch an ad", new Vector2(0.5f, 0.5f), new Vector2(all != null ? -232f : 0f, -520f), new Vector2(440f, 96f), 26, again, 0.18f);
            if (all != null) MakeGhost(cardRoot, "Take all three · watch an ad", new Vector2(0.5f, 0.5f), new Vector2(again != null ? 232f : 0f, -520f), new Vector2(440f, 96f), 26, all, 0.18f);
            sheet.transform.SetAsLastSibling();   // over the rings and markers the night has made since the HUD was built
            sheet.SetActive(true);
        }
        public void HideCards() { sheet.SetActive(false); }

        RectTransform endLedger; int endKills = -1, endGunnery, endScore; string endHeld, endSub, lastLedger = ""; GameObject endDepotBtn;
        static readonly string[] EndLabels = { "KNOCKED OUT", "HELD", "GUNNERY", "SCORE" };

        /// <summary>The night's numbers for the head of the end sheet, and the line under them.</summary>
        public void SetEndNumbers(int kills, string held, int gunnery, int score, string sub) { endKills = kills; endHeld = held; endGunnery = gunnery; endScore = score; endSub = sub; }

        /// <summary>The ad watched or the gold paid at dawn: the sheet stays as it is, its score doubled, the offer gone.</summary>
        public void EndDoubled(int score) { endScore = score; lastLedger += "\nScore doubled · for the depot"; BuildLedger(lastLedger); EndOffer(false); }

        /// <summary>The offer's row (the ad and the gold) shown or not; without it, its note goes too and the buttons
        /// under it close up.</summary>
        void EndOffer(bool on)
        {
            adBtn.SetActive(on); goldBtn.SetActive(on); if (!on) adNote.text = "";
            float lift = on ? 0f : 214f;
            ((RectTransform)againBtn.transform).anchoredPosition = new Vector2(0f, -450f + lift);
            if (endDepotBtn != null) ((RectTransform)endDepotBtn.transform).anchoredPosition = new Vector2(0f, -586f + lift);
            if (holdBtn != null) ((RectTransform)holdBtn.transform).anchoredPosition = new Vector2(0f, -722f + lift);
        }

        /// <summary>The night's account on the end sheet: its four numbers large across the head and the line under them,
        /// then everything else it brought a line each with room between, and the points under the last.</summary>
        void BuildLedger(string statLine)
        {
            foreach (Transform c in endLedger) Destroy(c.gameObject);
            var ink = new Color(0.93f, 0.91f, 0.86f); var dim = new Color(0.66f, 0.64f, 0.59f); float top = 424f;
            if (endKills >= 0)
            {
                string[] values = { endKills.ToString("N0", En), endHeld, endGunnery + "%", endScore.ToString("N0", En) };
                for (int i = 0; i < 4; i++)
                {
                    var tile = MakeImage(endLedger, "Tile", new Vector2(0.5f, 0.5f), new Vector2(-345f + i * 230f, top), new Vector2(214f, 132f), new Color(1f, 1f, 1f, 0.05f)); tile.sprite = Rounded(); tile.type = Image.Type.Sliced; tile.rectTransform.pivot = new Vector2(0.5f, 1f);
                    var v = MakeText(tile.transform, "Value", new Vector2(0.5f, 1f), new Vector2(0f, -12f), TextAnchor.UpperCenter, 58, ink); v.text = values[i]; Serif(v); v.rectTransform.sizeDelta = new Vector2(200f, 70f); Fit(v, 32);
                    var l = MakeText(tile.transform, "Label", new Vector2(0.5f, 0f), new Vector2(0f, 14f), TextAnchor.LowerCenter, 19, dim); l.text = Spaced(EndLabels[i]); l.rectTransform.sizeDelta = new Vector2(206f, 28f); Fit(l, 13);
                }
                top -= 156f;
                if (!string.IsNullOrEmpty(endSub)) { var sub = MakeText(endLedger, "Sub", new Vector2(0.5f, 0.5f), new Vector2(0f, top), TextAnchor.UpperCenter, 24, dim); sub.rectTransform.pivot = new Vector2(0.5f, 1f); sub.rectTransform.sizeDelta = new Vector2(900f, 32f); sub.text = endSub; Fit(sub, 18); top -= 32f; }
                top -= 22f;
            }
            var lines = new List<string>(); foreach (var ln in statLine.Split('\n')) if (!string.IsNullOrWhiteSpace(ln)) lines.Add(ln.Trim());
            for (int i = 0; i < lines.Count; i++)   // lines of the same kind (two medals, two orders) share one
            {
                int ci = lines[i].IndexOf(" · "); if (ci <= 0) continue; string head = lines[i].Substring(0, ci + 3); var more = new List<string>();
                for (int j = i + 1; j < lines.Count;) { if (lines[j].StartsWith(head)) { more.Add(lines[j].Substring(head.Length)); lines.RemoveAt(j); } else j++; }
                if (more.Count > 0) lines[i] += "   ·   " + string.Join("   ·   ", more);
            }
            float pitch = Mathf.Clamp((top + 123f) / (lines.Count + 1.5f), 30f, 50f); int size = Mathf.RoundToInt(Mathf.Clamp(pitch * 0.58f, 19f, 29f));   // more room between the lines when there are few
            foreach (var ln in lines)
            {
                int cut = ln.IndexOf(" · "); string text = cut > 0 ? "<color=#E3A64A>" + ln.Substring(0, cut) + "</color>   " + ln.Substring(cut + 3) : ln;   // what it is in amber, then what it brought
                var row = MakeText(endLedger, "Line", new Vector2(0.5f, 0.5f), new Vector2(0f, top), TextAnchor.UpperCenter, size, ink); row.rectTransform.pivot = new Vector2(0.5f, 1f); row.rectTransform.sizeDelta = new Vector2(920f, pitch); row.supportRichText = true; row.text = text; Fit(row, 16);
                top -= pitch;
            }
            endPoints.rectTransform.anchoredPosition = new Vector2(0f, top - pitch * 0.5f - 4f);
        }

        public void ShowEnd(bool dawn, string statLine, bool adAvailable, string eyebrow = null, string title = null, string again = null)
        {
            if (endStars != null) foreach (var st in endStars) st.gameObject.SetActive(false);
            endEyebrow.text = eyebrow ?? (dawn ? "05:00 · DAWN" : "PLATOON LEADER KNOCKED OUT");
            endTitle.text = title ?? (dawn ? "You held the line" : "Assault over");
            againBtn.transform.Find("Label").GetComponent<Text>().text = again ?? "New assault";
            lastLedger = statLine; BuildLedger(statLine);
            adLabel.text = dawn ? "Double score · watch an ad" : Depot.CrewNights == 0 && statLine.Contains("nights together are lost") ? "Field repair · keep the crew together · watch an ad" : "Field repair · watch an ad";
            adNote.text = dawn ? "A short video, or " + Depot.GoldRepair + " gold: ×2 score for the depot." : "A short video, or " + Depot.GoldRepair + " gold: the leader is repaired once and the assault goes on.";
            EndOffer(adAvailable);
            endSheet.SetActive(true);
        }
        public void HideEnd() { endSheet.SetActive(false); }

        /// <summary>A shot of the camera's own: the black bars in or out, the play HUD shown or stepped aside (now: at
        /// once, for behind the curtain).</summary>
        public void Cinema(bool bars, bool hudOn, bool now = false) { barsWant = bars ? 1f : 0f; hudWant = hudOn ? 1f : 0f; if (now) { barsAt = barsWant; hudFade.alpha = hudWant; } }
        /// <summary>Words over a shot: an eyebrow, a big title, a line under it. The opening's sit high and drift; the
        /// killcam's sit low and punch in.</summary>
        public void CineCard(string eyebrow, string title, string sub, float delay, float life, bool low)
        {
            cineEyebrow.text = Spaced(eyebrow); cineTitle.text = title; cineSub.text = low ? sub : Spaced(sub);
            cineCard.anchoredPosition = new Vector2(0f, low ? -540f : 470f); cineTitle.resizeTextMaxSize = low ? 150 : 210;
            cineSub.font = low ? BoldFont() : DefaultFont(); cineSub.fontSize = low ? 44 : 32; cineSub.color = low ? new Color(0.96f, 0.68f, 0.24f) : new Color(0.85f, 0.83f, 0.78f);
            cardLow = low; cardDelay = delay; cardLife = life; cardAge = 0f; cineFade.alpha = 0f;
        }
        /// <summary>The words cut short: they fade out from where they are.</summary>
        public void CineCardOut() { float a = cardAge - cardDelay; if (a <= 0f) cardAge = cardDelay + cardLife; else cardLife = Mathf.Min(cardLife, a + 0.5f * cineFade.alpha); }
        public void CineSkip(bool on) { cineSkip.gameObject.SetActive(on); }
        void TickCinema(float dt)
        {
            barsAt = Mathf.MoveTowards(barsAt, barsWant, dt * 2.2f); float bk = barsAt * barsAt * (3f - 2f * barsAt);
            barTop.anchoredPosition = new Vector2(0f, BarH * (1f - bk)); barBottom.anchoredPosition = new Vector2(0f, -BarH * (1f - bk));
            float hw = sheet.activeSelf ? 1f : hudWant; hudFade.alpha = Mathf.MoveTowards(hudFade.alpha, hw, dt * 3f); hudFade.blocksRaycasts = hw > 0.5f;   // the level-up sheet lives in the HUD: never hidden
            if (cineSkip.gameObject.activeSelf) { var sc = cineSkip.color; sc.a = bk * (0.5f + 0.2f * Mathf.Sin(Time.unscaledTime * 3f)); cineSkip.color = sc; }
            if (cardAge < cardDelay + cardLife)
            {
                cardAge += dt; float a = cardAge - cardDelay;
                cineFade.alpha = a <= 0f ? 0f : Mathf.Clamp01(Mathf.Min(a / 0.6f, (cardLife - a) / 0.5f));
                float sc = cardLow ? (a < 0.25f ? Mathf.Lerp(1.12f, 1f, Mathf.Clamp01(a / 0.25f)) : 1f + (a - 0.25f) * 0.01f) : 0.985f + 0.012f * Mathf.Max(0f, a);
                cineCard.localScale = new Vector3(sc, sc, 1f); cineRule.rectTransform.sizeDelta = new Vector2(180f * Mathf.SmoothStep(0f, 1f, (a - 0.15f) / 0.8f), 3f);
            }
            else if (cineFade.alpha > 0f) cineFade.alpha = 0f;
        }

        Image[] endStars;
        /// <summary>An operation night's three stars over the end sheet, the ones won lighting up one after another.</summary>
        public void ShowStars(bool[] got)
        {
            if (endStars == null)
            {
                endStars = new Image[3];
                for (int i = 0; i < 3; i++) { float sz = i == 1 ? 170f : 140f; var s = MakeImage(endSheet.transform, "Star" + i, new Vector2(0.5f, 0.5f), new Vector2(-175 + i * 175, i == 1 ? 766 : 736), new Vector2(sz, sz), OpOff); s.sprite = StarSprite(); s.rectTransform.pivot = new Vector2(0.5f, 0.5f); endStars[i] = s; }
            }
            foreach (var s in endStars) { s.gameObject.SetActive(true); s.color = OpOff; s.rectTransform.localScale = Vector3.one; }
            StartCoroutine(PopStars(got));
        }
        System.Collections.IEnumerator PopStars(bool[] got)
        {
            yield return new WaitForSecondsRealtime(0.4f);
            for (int i = 0; i < 3; i++)
            {
                if (!got[i]) continue;
                var s = endStars[i]; s.color = OpAmber; Sfx.Pickup();
                for (float a = 0f; a < 1f; a += Time.unscaledDeltaTime / 0.3f) { s.rectTransform.localScale = Vector3.one * (a < 0.6f ? Mathf.Lerp(0.3f, 1.3f, a / 0.6f) : Mathf.Lerp(1.3f, 1f, (a - 0.6f) / 0.4f)); yield return null; }
                s.rectTransform.localScale = Vector3.one; yield return new WaitForSecondsRealtime(0.2f);
            }
        }

        Text standBestText, convoyBestText, sneakBestText; Image standPic, convoyPic, sneakPic;   // the mode cards on the way-in sheet
        GameObject standKit, standReady; Text standSupplyText; readonly Dictionary<string, Image> standItems = new Dictionary<string, Image>(); readonly Dictionary<string, Text> standCosts = new Dictionary<string, Text>();
        /// <summary>The last stand's kit in the fight: shown for a stand, gone at its end.</summary>
        public void ShowStandKit(bool on)
        {
            if (standKit == null && on)
            {
                var dim = new Color(0.66f, 0.64f, 0.59f); var amber = new Color(0.96f, 0.68f, 0.24f);
                standKit = new GameObject("StandKit", typeof(RectTransform)); standKit.transform.SetParent(hudGroup.transform, false); Stretch(standKit);
                string[] ids = { "bags", "hogs", "mines" }; string[] names = { "SANDBAGS", "HEDGEHOGS", "MINES" };
                for (int i = 0; i < 3; i++)
                {
                    string id = ids[i];
                    var b = MakeButton(standKit.transform, "", new Vector2(0f, 0f), new Vector2(330 + i * 200, 400), new Vector2(190, 110), 26, () => OnStandItem?.Invoke(id));
                    var bi = b.GetComponent<Image>(); bi.color = new Color(0.08f, 0.09f, 0.11f, 0.88f); standItems[id] = bi;
                    var be = MakeImage(b.transform, "Edge", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(190, 110), new Color(0.96f, 0.68f, 0.24f, 0.45f)); be.sprite = Outline(); be.type = Image.Type.Sliced; be.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    var top = MakeText(b.transform, "Top", new Vector2(0.5f, 1f), new Vector2(0, -10), TextAnchor.UpperCenter, 19, dim); top.text = Spaced(names[i]); top.rectTransform.sizeDelta = new Vector2(190, 28);
                    var lbl = b.transform.Find("Label").GetComponent<Text>(); lbl.alignment = TextAnchor.MiddleCenter; lbl.rectTransform.anchoredPosition = new Vector2(0, -12); lbl.fontSize = 32; lbl.color = amber; lbl.text = Battle.StandCost(id).ToString(); standCosts[id] = lbl;
                    lbl.transform.SetAsLastSibling(); top.transform.SetAsLastSibling();
                }
                var sup = MakeImage(standKit.transform, "Supply", new Vector2(0f, 0f), new Vector2(130, 440), new Vector2(180, 100), new Color(0.06f, 0.07f, 0.09f, 0.85f)); sup.sprite = Rounded(); sup.type = Image.Type.Sliced; sup.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                var st = MakeText(sup.transform, "Top", new Vector2(0.5f, 1f), new Vector2(0, -8), TextAnchor.UpperCenter, 19, dim); st.text = Spaced("SUPPLY"); st.rectTransform.sizeDelta = new Vector2(180, 28);
                standSupplyText = MakeText(sup.transform, "Value", new Vector2(0.5f, 0.5f), new Vector2(0, -12), TextAnchor.MiddleCenter, 40, amber); standSupplyText.font = BoldFont(); standSupplyText.rectTransform.pivot = new Vector2(0.5f, 0.5f); standSupplyText.rectTransform.sizeDelta = new Vector2(180, 56);
                standReady = MakeGhost(standKit.transform, "Next wave now", new Vector2(0.5f, 1f), new Vector2(0, -372), new Vector2(380, 70), 26, () => OnStandReady?.Invoke(), 0.5f);
                standReady.transform.Find("Label").GetComponent<Text>().color = amber;
                if (handStick != null) { foreach (var kv in standItems) handStick.Blockers.Add(kv.Value.rectTransform); handStick.Blockers.Add(sup.rectTransform); handStick.Blockers.Add(standReady.GetComponent<RectTransform>()); }
            }
            if (standKit != null) standKit.SetActive(on);
        }
        /// <summary>The kit's state: the supplies, the item taken up lit amber, the ones out of reach dimmed; "Next wave now" in a break.</summary>
        public void SetStandKit(int supply, string armed, bool inBreak)
        {
            if (standKit == null) return;
            standSupplyText.text = supply.ToString();
            foreach (var kv in standItems)
            {
                bool on = kv.Key == armed, can = supply >= Battle.StandCost(kv.Key);
                kv.Value.color = on ? new Color(0.95f, 0.66f, 0.23f, 0.96f) : new Color(0.08f, 0.09f, 0.11f, can ? 0.88f : 0.5f);
                standCosts[kv.Key].color = on ? new Color(0.12f, 0.09f, 0.04f) : can ? new Color(0.96f, 0.68f, 0.24f) : new Color(0.45f, 0.43f, 0.4f);
            }
            if (standReady.activeSelf != inBreak) standReady.SetActive(inBreak);
        }

        Text goalsText;
        /// <summary>The operation night's two goals under the level bar, each lit amber once it is met.</summary>
        public void SetGoals(string line)
        {
            if (goalsText == null)
            {
                goalsText = MakeText(hudGroup.transform, "Goals", new Vector2(0.5f, 1f), new Vector2(0, -300), TextAnchor.MiddleCenter, 26, new Color(0.86f, 0.84f, 0.78f)); goalsText.font = BoldFont(); goalsText.supportRichText = true; goalsText.rectTransform.sizeDelta = new Vector2(1000, 40);
                var sh = goalsText.gameObject.AddComponent<UnityEngine.UI.Shadow>(); sh.effectColor = new Color(0f, 0f, 0f, 0.8f); sh.effectDistance = new Vector2(0f, -2f);
            }
            goalsText.text = line; goalsText.enabled = briefing == null || !briefing.activeSelf;   // under the briefing card while it is up
        }
        public void ShowHold(bool on) { if (holdBtn != null) holdBtn.SetActive(on); }

        void ShowMedals()
        {
            foreach (Transform c in medalRows) Destroy(c.gameObject);
            var amber = new Color(0.95f, 0.66f, 0.23f); var dim = new Color(0.5f, 0.48f, 0.45f);
            for (int i = 0; i < Medals.All.Length; i++)
            {
                var m = Medals.All[i]; bool got = Medals.Earned(m); float y = -i * 130f;
                var row = MakeImage(medalRows, "Medal", new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(940, 118), new Color(0.08f, 0.09f, 0.1f, got ? 0.96f : 0.6f)); row.rectTransform.pivot = new Vector2(0.5f, 1f);
                var disc = MakeImage(row.transform, "Disc", new Vector2(0f, 0.5f), new Vector2(60, 0), new Vector2(104, 104), got ? Color.white : new Color(0.3f, 0.3f, 0.32f)); disc.sprite = i < MedalPictures.Length ? UiSprite(MedalPictures[i]) : Lightswarm.ProceduralSprites.Glow(32, 0.95f); disc.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                var name = MakeText(row.transform, "Name", new Vector2(0f, 1f), new Vector2(120, -14), TextAnchor.UpperLeft, 38, got ? new Color(0.93f, 0.91f, 0.86f) : dim); name.text = m.name; name.rectTransform.sizeDelta = new Vector2(700, 50);
                var desc = MakeText(row.transform, "Desc", new Vector2(0f, 1f), new Vector2(120, -62), TextAnchor.UpperLeft, 26, got ? new Color(0.66f, 0.64f, 0.59f) : dim); desc.text = m.desc + (got ? "" : "  · +" + Medals.Reward); desc.rectTransform.sizeDelta = new Vector2(780, 50);
            }
            ((RectTransform)medalRows).sizeDelta = new Vector2(940f, Medals.All.Length * 130f + 20f); medalScroll.verticalNormalizedPosition = 1f;
            medalsSheet.SetActive(true);
        }

        void ShowRecords()
        {
            foreach (Transform c in recordRows) Destroy(c.gameObject);
            var ink = new Color(0.93f, 0.91f, 0.86f); var dim = new Color(0.66f, 0.64f, 0.59f); var amber = new Color(0.95f, 0.66f, 0.23f);
            var totals = MakeText(recordRows, "Totals", new Vector2(0.5f, 1f), new Vector2(0, 0), TextAnchor.UpperCenter, 30, dim); totals.rectTransform.sizeDelta = new Vector2(940, 120);
            totals.text = (Operations.TotalStars > 0 ? $"{Operations.TotalStars} of {Operations.All.Length * 15} operation stars\n" : "") + $"{Depot.NightsFought} nights · {Depot.Total("kills")} vehicles · {Depot.Total("tigers")} Tigers · {Depot.Total("guns")} guns · {Depot.Total("infantry")} infantry\n{Depot.Total("dawns")} dawns · {Depot.Total("objectives")} objectives · {Medals.Count}/{Medals.All.Length} medals";
            var log = Depot.NightLog();
            if (log.Count == 0) { var none = MakeText(recordRows, "None", new Vector2(0.5f, 1f), new Vector2(0, -160), TextAnchor.UpperCenter, 30, dim); none.text = "No nights fought yet."; }
            for (int i = 0; i < log.Count; i++)
            {
                var e = log[i]; if (e.Length < 6) continue; float y = -150f - i * 90f;
                var row = MakeImage(recordRows, "Night", new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(940, 80), new Color(0.08f, 0.09f, 0.1f, 0.9f)); row.rectTransform.pivot = new Vector2(0.5f, 1f);
                var left = MakeText(row.transform, "Left", new Vector2(0f, 0.5f), new Vector2(24, 0), TextAnchor.MiddleLeft, 28, ink); left.text = e[0] + " · " + e[1] + (e[5] == "1" ? " · dawn" : ""); left.rectTransform.sizeDelta = new Vector2(500, 80);
                var right = MakeText(row.transform, "Right", new Vector2(1f, 0.5f), new Vector2(-24, 0), TextAnchor.MiddleRight, 28, amber); right.text = e[2] + " kills · " + e[3] + " · " + e[4]; right.rectTransform.sizeDelta = new Vector2(520, 80);
            }
            ((RectTransform)recordRows).sizeDelta = new Vector2(940f, 170f + log.Count * 90f); recordScroll.verticalNormalizedPosition = 1f;
            recordsSheet.SetActive(true);
        }
        static readonly bool ShowFps = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--fps") >= 0;   // test switch: the frame rate over the HUD
        /// <summary>Tonight's weather on the title and in the corner of the HUD.</summary>
        public void SetConditions(string sector, string name, string note) { conditions.text = "Tonight: " + sector + " · " + (name == "Clear" ? "clear skies, full moon" : name.ToLowerInvariant() + " · " + note); assaultLabel.text = sector.ToUpperInvariant() + " · " + name.ToUpperInvariant();
            var pill = conditions.transform.parent as RectTransform; conditions.resizeTextForBestFit = true; conditions.resizeTextMinSize = 18; conditions.resizeTextMaxSize = conditions.fontSize;   // the pill fits the line
            if (pill != null) { float w = Mathf.Clamp(conditions.preferredWidth + 76f, 420f, 980f); pill.sizeDelta = new Vector2(w, pill.sizeDelta.y); conditions.rectTransform.sizeDelta = new Vector2(w - 60f, conditions.rectTransform.sizeDelta.y); } }
        public void ShowPause(bool soundOn, bool highQuality) { soundLabel.text = soundOn ? "Sound: on" : "Sound: off"; qualityLabel.text = highQuality ? "Quality: high" : "Quality: low"; pauseSheet.SetActive(true); }
        public void HidePause() { pauseSheet.SetActive(false); }
        public void SetQualityLabel(bool high) { qualityLabel.text = high ? "Quality: high" : "Quality: low"; }
        void ShowSettings() { RefreshSettings(); if (adChoices != null) adChoices.SetActive(Ads.PrivacyChoices); settingsSheet.SetActive(true); }
        GameObject adChoices;
        void RefreshSettings()
        {
            setSound.text = PlayerPrefs.GetInt("sound", 1) == 1 ? "Sound: on" : "Sound: off"; setMusic.text = Sfx.MusicOff ? "Music: off" : "Music: on"; setQuality.text = PlayerPrefs.GetInt("quality", 1) == 1 ? "Quality: high" : "Quality: low"; setVibe.text = PlayerPrefs.GetInt("vibe", 1) == 1 ? "Vibration: on" : "Vibration: off";
        }
        public void SetAdNote(string s) { adNote.text = s; }

        void Update()
        {
            fpsAccum += Time.unscaledDeltaTime; fpsFrames++; fpsTimer += Time.unscaledDeltaTime;
            if (fpsTimer >= 0.5f) { float f = fpsFrames / fpsAccum; fps.text = ShowFps ? $"{f:0} fps" : ""; fpsAccum = 0; fpsFrames = 0; fpsTimer = 0; if (f < 38f && hudGroup.activeSelf) slowFor += 0.5f; else slowFor = 0f; if (slowFor >= 5f && !slowOffered && PlayerPrefs.GetInt("quality", 1) != 0) { slowOffered = true; Toast("Stuttering? Pause · Quality: low turns off shadows and rain", 5f); } }
            if (toastLeft > 0f && hudFade.alpha > 0.95f) { toastLeft -= Time.unscaledDeltaTime; if (toastLeft <= 0f) toast.text = ""; }
            toast.enabled = sheet == null || !sheet.activeSelf;   // hidden behind the level-up cards, back when they go
            bool choosing = sheet != null && sheet.activeSelf;
            if (briefing != null && briefingLeft > 0f && briefing.activeSelf == choosing) briefing.SetActive(!choosing);   // out of the way of the cards, back after them
            if (briefingLeft > 0f && hudFade.alpha > 0.95f && !choosing) { briefingLeft -= Time.unscaledDeltaTime; if (briefingLeft <= 0f && briefing != null) briefing.SetActive(false); }
            TickNumbers(Time.unscaledDeltaTime); CurtainTick(Time.unscaledDeltaTime); TickCinema(Mathf.Min(Time.unscaledDeltaTime, 0.05f)); TickRewards(Mathf.Min(Time.unscaledDeltaTime, 0.05f)); TickMap();
            if (titleSheet != null && titleSheet.activeSelf) for (int i = 0; i < embers.Count; i++)
            {
                var e = embers[i]; float ph = emberPhase[i]; var p = e.anchoredPosition; p.y += (18f + 10f * Mathf.Sin(ph)) * Time.unscaledDeltaTime; p.x += Mathf.Sin(Time.unscaledTime * 0.7f + ph) * 12f * Time.unscaledDeltaTime;
                if (p.y > 1000f) { p.y = -1000f; p.x = Random.Range(-520f, 520f); } e.anchoredPosition = p;
                var img = e.GetComponent<Image>(); var c = img.color; c.a = 0.35f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 1.3f + ph)); img.color = c;
            }
            if (flash.color.a > 0f) { var c = flash.color; c.a = Mathf.Max(0f, c.a - Time.unscaledDeltaTime * 0.9f); flash.color = c; }
            var cam = Camera.main;
            for (int i = popups.Count - 1; i >= 0; i--)
            {
                var p = popups[i]; p.life -= Time.deltaTime;
                if (p.life <= 0f || cam == null) { p.t.gameObject.SetActive(false); popupPool.Push(p.t); popups.RemoveAt(i); continue; }
                var sp = cam.WorldToScreenPoint(p.world + Vector3.up * (2f + (1.1f - p.life) * 4f));
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, sp, null, out var local); p.t.rectTransform.anchoredPosition = local;
                var c = p.t.color; c.a = Mathf.Clamp01(p.life * 2f); p.t.color = c;
            }
        }

        static Font uiFont, uiBold, displayFont, labelFont, labelBold;
        static Font DefaultFont() { if (uiFont == null) uiFont = Resources.Load<Font>("Fonts/Barlow-Regular") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); return uiFont; }
        static Font BoldFont() { if (uiBold == null) uiBold = Resources.Load<Font>("Fonts/Barlow-SemiBold") ?? DefaultFont(); return uiBold; }
        static Font DisplayFont() { if (displayFont == null) displayFont = Resources.Load<Font>("Fonts/Cinzel-Bold") ?? BoldFont(); return displayFont; }
        /// <summary>The labels' face: Barlow Condensed, for eyebrows, tabs and buttons in spaced capitals.</summary>
        static Font LabelFont() { if (labelFont == null) labelFont = Resources.Load<Font>("Fonts/BarlowCondensed-SemiBold") ?? BoldFont(); return labelFont; }
        static Font LabelBoldFont() { if (labelBold == null) labelBold = Resources.Load<Font>("Fonts/BarlowCondensed-Bold") ?? LabelFont(); return labelBold; }

        /// <summary>A title engraved: the serif capitals a third smaller than the old display face gave (they run nearly twice
        /// as wide), shrunk further if still too long, set a little apart, in struck gold (or pale stone) with a shadow.</summary>
        static void Engrave(Text t, bool gold = true, float scale = 0.66f, float shadow = 3f)
        {
            t.font = DisplayFont(); t.fontSize = Mathf.RoundToInt(t.fontSize * scale); t.horizontalOverflow = HorizontalWrapMode.Wrap; Fit(t, Mathf.Max(18, t.fontSize / 2));
            foreach (var old in t.GetComponents<UnityEngine.UI.Shadow>()) UnityEngine.Object.DestroyImmediate(old);   // the shadow goes on after the face, so it copies gold letters as a shadow
            var face = t.GetComponent<EngravedFace>(); if (face == null) face = t.gameObject.AddComponent<EngravedFace>(); face.gold = gold;
            var sh = t.gameObject.AddComponent<UnityEngine.UI.Shadow>(); sh.effectColor = new Color(0f, 0f, 0f, 0.65f); sh.effectDistance = new Vector2(shadow * 0.5f, -shadow);
        }
        /// <summary>A name or a number in the serif capitals, without the gold: the old sizes a fifth smaller, shrunk further if
        /// it still does not fit.</summary>
        static void Serif(Text t, float scale = 0.82f) { t.font = DisplayFont(); t.fontSize = Mathf.RoundToInt(t.fontSize * scale); t.horizontalOverflow = HorizontalWrapMode.Wrap; Fit(t, Mathf.Max(14, t.fontSize / 2)); }
        static Sprite outlineSprite, shadowSprite, scrimSprite; static Sprite[] glass;
        Image curtain; float curtainWant, curtainAt = 1f;   // the black that screens change behind
        // the cinema: its bars and card of words, the play HUD's fade, and where each of them is going
        RectTransform barTop, barBottom, cineCard; CanvasGroup hudFade, cineFade; Text cineEyebrow, cineTitle, cineSub, cineSkip; Image cineRule;
        float barsWant, barsAt, hudWant = 1f, cardAge = 99f, cardLife, cardDelay; bool cardLow; const float BarH = 230f;
        readonly List<Ticker> tickers = new List<Ticker>();
        class Ticker { public Text text; public string prefix, suffix; public float shown, want; }

        /// <summary>A number that runs up to its new value instead of jumping.</summary>
        void Tick(Text t, float value, string prefix = "", string suffix = "")
        {
            var k = tickers.Find(x => x.text == t); if (k == null) { k = new Ticker { text = t, shown = value }; tickers.Add(k); }
            k.prefix = prefix; k.suffix = suffix; k.want = value; if (Mathf.Abs(k.shown - value) > value * 0.5f + 200f) k.shown = value;   // a fresh screen starts on the number, only changes run up
            t.text = prefix + Mathf.RoundToInt(k.shown).ToString("N0") + suffix;
        }
        void TickNumbers(float dt)
        {
            foreach (var k in tickers)
            {
                if (k.text == null || Mathf.Approximately(k.shown, k.want)) continue;
                k.shown = Mathf.MoveTowards(k.shown, k.want, Mathf.Max(60f, Mathf.Abs(k.want - k.shown) * 3.2f) * dt);
                k.text.text = k.prefix + Mathf.RoundToInt(k.shown).ToString("N0") + k.suffix;
            }
        }

        /// <summary>Black over everything: 1 hides the screen, 0 shows it. Screens change behind it.</summary>
        public void Curtain(float to) { curtainWant = to; }
        void CurtainTick(float dt)
        {
            if (curtain == null) return;
            curtainAt = Mathf.MoveTowards(curtainAt, curtainWant, dt * 2.6f);
            var c = curtain.color; c.a = curtainAt; curtain.color = c; curtain.raycastTarget = curtainAt > 0.9f; curtain.enabled = curtainAt > 0.001f;
        }
        static Sprite Rounded() => (glass ??= Lightswarm.ProceduralSprites.RoundedGlass(24, 96))[0];
        /// <summary>The sheen laid over a solid card or button, under its content: a wash from the top, a hairline on the rim.</summary>
        static void Gloss(Transform on)
        {
            if (on.Find("Gloss") != null) return;
            var g = new GameObject("Gloss", typeof(RectTransform), typeof(Image)); g.transform.SetParent(on, false);
            var rt = g.GetComponent<RectTransform>(); rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            var im = g.GetComponent<Image>(); im.sprite = (glass ??= Lightswarm.ProceduralSprites.RoundedGlass(24, 96))[1]; im.type = Image.Type.Sliced; im.raycastTarget = false; g.transform.SetAsFirstSibling();
        }
        static Sprite Outline() => outlineSprite ??= Lightswarm.ProceduralSprites.RoundedOutline(24, 96, 2f);
        static Sprite Shadow() => shadowSprite ??= Lightswarm.ProceduralSprites.SoftShadow(24, 28, 160);
        /// <summary>A tall fade for the end sheet: solid below, clearing over the top quarter where the camera circles the leader.</summary>
        static Sprite Scrim()
        {
            if (scrimSprite != null) return scrimSprite;
            var tex = new Texture2D(2, 256, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            for (int y = 0; y < 256; y++) { float a = Mathf.Lerp(1f, 0.15f, Mathf.SmoothStep(0f, 1f, (y / 255f - 0.7f) / 0.2f)); tex.SetPixel(0, y, new Color(1f, 1f, 1f, a)); tex.SetPixel(1, y, new Color(1f, 1f, 1f, a)); }
            tex.Apply(); return scrimSprite = Sprite.Create(tex, new Rect(0f, 0f, 2f, 256f), new Vector2(0.5f, 0.5f));
        }
        /// <summary>Letters spaced out with hair spaces: the small caps eyebrows and button labels.</summary>
        static string Spaced(string s) { var sb = new System.Text.StringBuilder(); foreach (var ch in s) { sb.Append(ch); if (ch != ' ') sb.Append('\u200A'); } return sb.ToString(); }
        /// <summary>A rounded translucent panel with a faint edge.</summary>
        static Image MakeCard(Transform parent, string name, Vector2 anchor, Vector2 offset, Vector2 size, Color fill, float edgeAlpha = 0.12f)
        {
            var card = MakeImage(parent, name, anchor, offset, size, fill); card.sprite = Rounded(); card.type = Image.Type.Sliced; Gloss(card.transform);
            var edge = MakeImage(card.transform, "Edge", new Vector2(0.5f, 0.5f), Vector2.zero, size, new Color(1f, 1f, 1f, edgeAlpha)); edge.sprite = Outline(); edge.type = Image.Type.Sliced; edge.rectTransform.pivot = new Vector2(0.5f, 0.5f); edge.rectTransform.anchoredPosition = Vector2.zero;
            return card;
        }
        /// <summary>The call to action: amber, rounded, a soft shadow under it, dark bold label.</summary>
        static GameObject MakePrimary(Transform parent, string label, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize, System.Action onClick)
        {
            var sh = MakeImage(parent, "Shadow", anchor, pos + new Vector2(0f, -14f), size + new Vector2(40f, 40f), new Color(0f, 0f, 0f, 0.55f)); sh.sprite = Shadow(); sh.type = Image.Type.Sliced; sh.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var b = MakeButton(parent, label, anchor, pos, size, fontSize, onClick); b.GetComponent<Image>().color = new Color(0.96f, 0.68f, 0.24f, 1f); Gloss(b.transform);
            var t = b.transform.Find("Label").GetComponent<Text>(); t.color = new Color(0.12f, 0.09f, 0.04f); t.font = LabelFont(); t.text = Spaced(label.ToUpperInvariant());
            return b;
        }
        /// <summary>A quiet button: dark glass with a thin light edge.</summary>
        static GameObject MakeGhost(Transform parent, string label, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize, System.Action onClick, float edgeAlpha = 0.22f)
        {
            var b = MakeButton(parent, label, anchor, pos, size, fontSize, onClick); b.GetComponent<Image>().color = new Color(0.06f, 0.07f, 0.09f, 0.55f);
            var edge = MakeImage(b.transform, "Edge", new Vector2(0.5f, 0.5f), Vector2.zero, size, new Color(1f, 1f, 1f, edgeAlpha)); edge.sprite = Outline(); edge.type = Image.Type.Sliced; edge.rectTransform.pivot = new Vector2(0.5f, 0.5f); edge.rectTransform.anchoredPosition = Vector2.zero; edge.transform.SetSiblingIndex(0); Gloss(b.transform);
            var lbl = b.transform.Find("Label").GetComponent<Text>(); if (label.Length <= 16 && label.IndexOf('·') < 0) lbl.text = Spaced(label.ToUpperInvariant());   // a short one in spaced capitals, like the tabs
            return b;
        }
        /// <summary>A small pill with a coloured dot: the weather, the daily drop.</summary>
        static Text MakeChip(Transform parent, string name, Vector2 anchor, Vector2 pos, float width, Color dot, Color fill)
        {
            var pill = MakeImage(parent, name, anchor, pos, new Vector2(width, 56f), fill); pill.sprite = Rounded(); pill.type = Image.Type.Sliced; pill.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var d = MakeImage(pill.transform, "Dot", new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(14f, 14f), dot); d.sprite = Lightswarm.ProceduralSprites.Glow(32, 0.55f); d.rectTransform.pivot = new Vector2(0f, 0.5f);
            var t = MakeText(pill.transform, "Text", new Vector2(0f, 0.5f), new Vector2(48f, 0f), TextAnchor.MiddleLeft, 26, new Color(0.93f, 0.91f, 0.86f)); t.rectTransform.pivot = new Vector2(0f, 0.5f); t.rectTransform.sizeDelta = new Vector2(width - 60f, 56f); return t;
        }

        public static Text MakeText(Transform parent, string name, Vector2 anchor, Vector2 offset, TextAnchor align, int size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>(); rt.anchorMin = rt.anchorMax = anchor; rt.pivot = anchor; rt.anchoredPosition = offset; rt.sizeDelta = new Vector2(900, 140);
            var t = go.GetComponent<Text>(); t.font = DefaultFont(); t.fontSize = size; t.alignment = align; t.color = color; t.raycastTarget = false;
            return t;
        }

        static readonly Dictionary<string, Sprite> uiSprites = new Dictionary<string, Sprite>();
        /// <summary>A painted picture from Resources/UI as a sprite; a rect (in pixels) picks one cell of a sheet.</summary>
        public static Sprite UiSprite(string name, Rect? cell = null)
        {
            string key = name + (cell.HasValue ? cell.Value.ToString() : ""); if (uiSprites.TryGetValue(key, out var s)) return s;
            var tex = Resources.Load<Texture2D>("UI/" + name); if (tex == null) return null;
            s = Sprite.Create(tex, cell ?? new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f); uiSprites[key] = s; return s;
        }
        static string CardPicture(string id)
        {
            switch (id)
            {
                case "he": return "card_he"; case "apcr": return "card_apcr"; case "rapid": case "loaders": return "card_loader"; case "radar": case "optics": return "card_optics";
                case "engine": case "engines": return "card_engines"; case "repair": return "card_repair"; case "reinf": return "card_reinf"; case "arty": return "card_artillery";
                case "smoke": return "card_smoke"; case "firefly": return "card_firefly"; case "gunners": return "card_gunner"; case "plates": case "armor": return "card_armour";
                // the new cards borrow the nearest picture until they have their own
                case "wet": case "sandbags": return "card_armour"; case "dozer": case "widetracks": return "card_engines"; case "canister": case "phos": return "card_he";
                case "radionet": case "plane": return "card_optics"; case "ace": return "card_gunner"; case "recovery": return "card_repair";
                case "reserve": return "card_crews"; case "veteran": return "card_veteran"; default: return null;
            }
        }
        static readonly string[] MedalPictures = { "medal_recruit", "medal_nightfighter", "medal_bocage", "medal_sharpshooter", "medal_tankace", "medal_tigerslayer", "medal_gunbuster", "medal_trenchbroom", "medal_pathfinder", "medal_aceofaces", "medal_oldguard", "medal_ironnight" };

        public static Image MakeImage(Transform parent, string name, Vector2 anchor, Vector2 offset, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>(); rt.anchorMin = rt.anchorMax = anchor; rt.pivot = anchor; rt.anchoredPosition = offset; rt.sizeDelta = size;
            var img = go.GetComponent<Image>(); img.color = color; img.raycastTarget = false;
            if (name.StartsWith("Row") || name == "Order" || name == "Showroom") { img.sprite = Rounded(); img.type = Image.Type.Sliced; Gloss(img.transform); }
            return img;
        }

        static GameObject MakeButton(Transform parent, string label, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize, System.Action onClick)
        {
            var go = new GameObject("Btn " + label, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>(); rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f); rt.anchoredPosition = pos; rt.sizeDelta = size;
            var bi = go.GetComponent<Image>(); bi.color = new Color(0.03f, 0.04f, 0.06f, 0.6f); bi.sprite = Rounded(); bi.type = Image.Type.Sliced;
            go.AddComponent<PressFeel>();
            var btn = go.GetComponent<Button>(); btn.onClick.AddListener(() => { if (UnityEngine.EventSystems.EventSystem.current != null) UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null); Sfx.Click(); onClick(); });   // nothing stays selected: a stray key never presses a button again
            var cb = btn.colors; cb.highlightedColor = new Color(1f, 1f, 1f, 0.92f); cb.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f); cb.fadeDuration = 0.06f; btn.colors = cb;
            var t = MakeText(go.transform, "Label", new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleCenter, Mathf.RoundToInt(fontSize * 1.1f), new Color(0.93f, 0.91f, 0.86f)); t.font = LabelFont();   // condensed: a tenth larger
            t.GetComponent<RectTransform>().sizeDelta = size; t.text = label;
            return go;
        }
    }
}

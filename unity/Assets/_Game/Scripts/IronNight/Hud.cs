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

    public class Hud : MonoBehaviour
    {
        public class Card { public string id, title, desc; public bool rare; }

        public System.Action<Formation> OnFormation;
        public System.Action OnAd, OnAgain, OnStart, OnCampaign, OnRestart, OnDepot, OnBack, OnReserveAd, OnPause, OnResume, OnSound, OnQuit, OnDaily, OnQuality, OnHold, OnAmmo, OnAbility; GameObject holdBtn, ammoBtn, abilityBtn; RectTransform pauseBtnRect; Text ammoLabel, ammoKind; Image abilityFill, abilityPic;

        Text clock, count, fps, tally, levelText, toast, endTitle, endEyebrow, stats, adLabel, adNote, leaderHp, assaultLabel, conditions, qualityLabel, setSound, setQuality, setVibe, setMusic, rankLine, pointsLine, ordersCount; GameObject settingsSheet, ordersSheet; RawImage titleBackdrop, depotBackdrop; GarageDrag depotDrag; readonly List<RectTransform> embers = new List<RectTransform>(); readonly List<float> emberPhase = new List<float>(); GameObject dailyBtn, helpSheet, medalsSheet, recordsSheet; Transform medalRows, recordRows;
        Image levelFill, flash; GameObject sheet, endSheet, adBtn, titleSheet, depotSheet, hudGroup, reserveBtn, pauseSheet; Text soundLabel; Transform missionRoot;
        class Rising { public Text t; public float life; public Vector3 world; }
        readonly List<Rising> popups = new List<Rising>(); readonly Stack<Text> popupPool = new Stack<Text>(); RectTransform canvasRect;
        Image radar, objectiveArrow; Text objectiveLabel; readonly List<Image> radarDots = new List<Image>(); readonly List<Image> hpBars = new List<Image>(); readonly List<Image> hpFills = new List<Image>(); Transform cardRoot, depotRows; Image rankBadge; GameObject campaignBtn, againBtn; public Garage garage; int depotTab; readonly Button[] depotTabs = new Button[3]; Image campaignPic, briefingPic; GameObject briefing; Text briefingTitle, briefingText; float briefingLeft; ScrollRect depotScroll; Canvas canvas; Text depotPoints, titleStats, endPoints, reserveNote, bossName; Image bossFill; GameObject bossBar;
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

            var hg = new GameObject("PlayHud", typeof(RectTransform)); hg.transform.SetParent(canvasGo.transform, false); Stretch(hg); hudGroup = hg;
            var t = hg.transform; canvasRect = canvasGo.GetComponent<RectTransform>();
            var fl = new GameObject("HitFlash", typeof(RectTransform), typeof(Image)); fl.transform.SetParent(t, false); Stretch(fl); flash = fl.GetComponent<Image>(); flash.color = new Color(0.8f, 0.1f, 0.05f, 0f); flash.raycastTarget = false;
            var ink = new Color(0.93f, 0.91f, 0.86f); var amber = new Color(0.95f, 0.66f, 0.23f); var dim = new Color(0.66f, 0.64f, 0.59f);
            assaultLabel = MakeText(t, "ClockLabel", new Vector2(0, 1), new Vector2(60, -70), TextAnchor.UpperLeft, 30, dim); assaultLabel.text = "NIGHT ASSAULT";
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
            toast = MakeText(t, "Toast", new Vector2(0.5f, 0.5f), new Vector2(0, 420), TextAnchor.MiddleCenter, 60, ink); toast.text = "";
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
            MakeText(sheet.transform, "Eyebrow", new Vector2(0.5f, 0.5f), new Vector2(0, 600), TextAnchor.MiddleCenter, 30, dim).text = "LEVEL UP";
            MakeText(sheet.transform, "Title", new Vector2(0.5f, 0.5f), new Vector2(0, 530), TextAnchor.MiddleCenter, 96, ink).text = "Choose";
            var cards = new GameObject("Cards", typeof(RectTransform)); cards.transform.SetParent(sheet.transform, false);
            var crt = cards.GetComponent<RectTransform>(); crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f); crt.sizeDelta = Vector2.zero;
            cardRoot = cards.transform; sheet.SetActive(false);

            // end sheet
            endSheet = new GameObject("End", typeof(RectTransform), typeof(Image)); endSheet.transform.SetParent(t, false);
            Stretch(endSheet); endSheet.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.04f, 0.84f);
            endEyebrow = MakeText(endSheet.transform, "Eyebrow", new Vector2(0.5f, 0.5f), new Vector2(0, 520), TextAnchor.MiddleCenter, 30, dim);
            endTitle = MakeText(endSheet.transform, "Title", new Vector2(0.5f, 0.5f), new Vector2(0, 430), TextAnchor.MiddleCenter, 110, ink);
            stats = MakeText(endSheet.transform, "Stats", new Vector2(0.5f, 0.5f), new Vector2(0, 250), TextAnchor.MiddleCenter, 40, dim); stats.rectTransform.sizeDelta = new Vector2(900, 300);
            adBtn = MakeButton(endSheet.transform, "Field repair · watch an ad", new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(880, 130), 40, () => OnAd?.Invoke());
            adBtn.GetComponent<Image>().color = amber; adLabel = adBtn.transform.Find("Label").GetComponent<Text>(); adLabel.color = new Color(0.1f, 0.08f, 0.05f);
            adNote = MakeText(endSheet.transform, "AdNote", new Vector2(0.5f, 0.5f), new Vector2(0, -130), TextAnchor.MiddleCenter, 28, dim); adNote.rectTransform.sizeDelta = new Vector2(900, 100);
            againBtn = MakeButton(endSheet.transform, "New assault", new Vector2(0.5f, 0.5f), new Vector2(0, -260), new Vector2(880, 130), 40, () => OnAgain?.Invoke());
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
            }
            arrowSprite = ArrowSprite(); objectiveArrow.sprite = arrowSprite;
            var root = canvasGo.transform;
            endPoints = MakeText(endSheet.transform, "Points", new Vector2(0.5f, 0.5f), new Vector2(0, 120), TextAnchor.MiddleCenter, 44, amber);
            MakeButton(endSheet.transform, "Depot", new Vector2(0.5f, 0.5f), new Vector2(0, -400), new Vector2(880, 130), 40, () => OnDepot?.Invoke());
            holdBtn = MakeButton(endSheet.transform, "Hold till morning · points ×1.5", new Vector2(0.5f, 0.5f), new Vector2(0, -540), new Vector2(880, 130), 40, () => OnHold?.Invoke()); holdBtn.GetComponent<Image>().color = new Color(0.55f, 0.36f, 0.14f, 0.95f); holdBtn.SetActive(false);

            // pause sheet
            pauseSheet = new GameObject("Pause", typeof(RectTransform), typeof(Image)); pauseSheet.transform.SetParent(root, false);
            Stretch(pauseSheet); pauseSheet.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.04f, 0.8f);
            MakeText(pauseSheet.transform, "Title", new Vector2(0.5f, 0.5f), new Vector2(0, 430), TextAnchor.MiddleCenter, 96, ink).text = "Paused";
            var resume = MakeButton(pauseSheet.transform, "Resume", new Vector2(0.5f, 0.5f), new Vector2(0, 160), new Vector2(880, 150), 52, () => OnResume?.Invoke());
            resume.GetComponent<Image>().color = amber; resume.transform.Find("Label").GetComponent<Text>().color = new Color(0.1f, 0.08f, 0.05f);
            var snd = MakeButton(pauseSheet.transform, "Sound: on", new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(880, 130), 40, () => OnSound?.Invoke()); soundLabel = snd.transform.Find("Label").GetComponent<Text>();
            var qb = MakeButton(pauseSheet.transform, "Quality: high", new Vector2(0.5f, 0.5f), new Vector2(0, -180), new Vector2(880, 130), 40, () => OnQuality?.Invoke()); qualityLabel = qb.transform.Find("Label").GetComponent<Text>();
            MakeText(pauseSheet.transform, "QualityNote", new Vector2(0.5f, 0.5f), new Vector2(0, -270), TextAnchor.MiddleCenter, 26, dim).text = "Low: no shadows, no glow, no rain. For phones that stutter.";
            MakeButton(pauseSheet.transform, "Restart the night", new Vector2(0.5f, 0.5f), new Vector2(0, -400), new Vector2(880, 110), 38, () => OnRestart?.Invoke());
            MakeButton(pauseSheet.transform, "Abandon the assault", new Vector2(0.5f, 0.5f), new Vector2(0, -530), new Vector2(880, 110), 38, () => OnQuit?.Invoke());
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
              var coin = MakeImage(pts.transform, "Coin", new Vector2(0f, 0.5f), new Vector2(18, 0), new Vector2(30, 30), new Color(0.96f, 0.68f, 0.24f)); coin.sprite = Lightswarm.ProceduralSprites.Ring(64, 0.16f); coin.rectTransform.pivot = new Vector2(0f, 0.5f);
              var coinCore = MakeImage(pts.transform, "CoinCore", new Vector2(0f, 0.5f), new Vector2(27, 0), new Vector2(12, 12), new Color(0.96f, 0.68f, 0.24f)); coinCore.sprite = Lightswarm.ProceduralSprites.Glow(32, 0.7f); coinCore.rectTransform.pivot = new Vector2(0f, 0.5f);
              pointsLine = MakeText(pts.transform, "Text", new Vector2(0f, 0.5f), new Vector2(64, 0), TextAnchor.MiddleLeft, 30, new Color(0.96f, 0.68f, 0.24f)); pointsLine.rectTransform.pivot = new Vector2(0f, 0.5f); pointsLine.rectTransform.sizeDelta = new Vector2(260, 72); pointsLine.font = BoldFont(); }
            // the name
            var eyebrow = MakeText(titleSheet.transform, "Eyebrow", new Vector2(0.5f, 1f), new Vector2(0, -190), TextAnchor.MiddleCenter, 26, new Color(0.96f, 0.68f, 0.24f)); eyebrow.text = Spaced("WWII · NIGHT ASSAULT"); eyebrow.font = BoldFont();
            var big = MakeText(titleSheet.transform, "Name", new Vector2(0.5f, 1f), new Vector2(0, -300), TextAnchor.MiddleCenter, 170, ink); big.text = "IRON NIGHT"; big.font = DisplayFont(); big.rectTransform.sizeDelta = new Vector2(1040, 240); big.verticalOverflow = VerticalWrapMode.Overflow; big.horizontalOverflow = HorizontalWrapMode.Overflow;
            var bigShadow = big.gameObject.AddComponent<UnityEngine.UI.Shadow>(); bigShadow.effectColor = new Color(0f, 0f, 0f, 0.8f); bigShadow.effectDistance = new Vector2(0f, -7f);
            var rule = MakeImage(titleSheet.transform, "Rule", new Vector2(0.5f, 1f), new Vector2(0, -400), new Vector2(110, 4), new Color(0.96f, 0.68f, 0.24f, 0.9f)); rule.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            // tonight
            conditions = MakeChip(titleSheet.transform, "Conditions", new Vector2(0.5f, 0f), new Vector2(0, 1060), 640f, new Color(0.96f, 0.68f, 0.24f), new Color(0.06f, 0.07f, 0.09f, 0.7f));
            // three tiles with pictures: the campaign, the depot, the standing orders
            string[] tileNames = { "CAMPAIGN", "DEPOT", "ORDERS" }; string[] tilePics = { "campaign_normandy", "card_crews", "card_reinf" }; System.Action[] tileActs = { () => OnCampaign?.Invoke(), () => OnDepot?.Invoke(), () => ShowOrders() };
            for (int i = 0; i < 3; i++)
            {
                float x = -305f + i * 305f; var tile = MakeButton(titleSheet.transform, tileNames[i], new Vector2(0.5f, 0f), new Vector2(x, 860), new Vector2(290, 250), 26, tileActs[i]); tile.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.11f, 1f);
                var mask = new GameObject("Mask", typeof(RectTransform), typeof(RectMask2D)); mask.transform.SetParent(tile.transform, false); var mkrt = mask.GetComponent<RectTransform>(); mkrt.anchorMin = mkrt.anchorMax = new Vector2(0.5f, 0.5f); mkrt.sizeDelta = new Vector2(280, 240); mkrt.anchoredPosition = Vector2.zero;
                var pic = MakeImage(mask.transform, "Pic", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280, 240), new Color(0.85f, 0.85f, 0.85f, 1f)); pic.rectTransform.pivot = new Vector2(0.5f, 0.5f); var sp = UiSprite(tilePics[i]);
                if (sp != null) { pic.sprite = sp; float cover = Mathf.Max(280f / sp.rect.width, 240f / sp.rect.height); pic.rectTransform.sizeDelta = new Vector2(sp.rect.width * cover, sp.rect.height * cover); }
                if (i == 0) campaignPic = pic;
                var fade = MakeImage(mask.transform, "Fade", new Vector2(0.5f, 0f), Vector2.zero, new Vector2(280, 150), new Color(0.02f, 0.02f, 0.03f, 0.95f)); fade.sprite = Lightswarm.ProceduralSprites.GradientDown(64, 1.2f); fade.rectTransform.pivot = new Vector2(0.5f, 0f);
                var edge = MakeImage(tile.transform, "Edge", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(290, 250), new Color(1f, 1f, 1f, 0.16f)); edge.sprite = Outline(); edge.type = Image.Type.Sliced; edge.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                var lt = tile.transform.Find("Label").GetComponent<Text>(); lt.text = Spaced(tileNames[i]); lt.alignment = TextAnchor.LowerCenter; lt.rectTransform.sizeDelta = new Vector2(290, 230); lt.transform.SetAsLastSibling(); lt.font = BoldFont();
                if (i == 0) campaignBtn = tile; if (i == 2) { ordersCount = MakeText(tile.transform, "Count", new Vector2(0.5f, 0f), new Vector2(0, 50), TextAnchor.LowerCenter, 20, new Color(0.96f, 0.68f, 0.24f)); ordersCount.rectTransform.sizeDelta = new Vector2(280, 30); ordersCount.transform.SetAsLastSibling(); }
            }
            // to battle: gold, a highlight along the top, a shadow under
            var start = MakePrimary(titleSheet.transform, "To battle", new Vector2(0.5f, 0f), new Vector2(0, 610), new Vector2(920, 160), 62, () => OnStart?.Invoke());
            { var g1 = MakeImage(start.transform, "Depth", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(920, 160), new Color(0.45f, 0.22f, 0.02f, 0.55f)); g1.sprite = Lightswarm.ProceduralSprites.GradientDown(64, 1.0f); g1.rectTransform.pivot = new Vector2(0.5f, 0.5f); g1.transform.SetSiblingIndex(0);
              var sheenMask = new GameObject("SheenMask", typeof(RectTransform), typeof(RectMask2D)); sheenMask.transform.SetParent(start.transform, false); var smrt = sheenMask.GetComponent<RectTransform>(); smrt.anchorMin = smrt.anchorMax = new Vector2(0.5f, 0.5f); smrt.sizeDelta = new Vector2(912, 152);
              var band = MakeImage(sheenMask.transform, "Sheen", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160, 260), new Color(1f, 0.98f, 0.9f, 0f)); band.rectTransform.pivot = new Vector2(0.5f, 0.5f); band.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 18f); band.sprite = Lightswarm.ProceduralSprites.Glow(64, 0.1f);
              var sh = start.AddComponent<Sheen>(); sh.band = band.rectTransform; sh.width = 920f;
              var hi = MakeImage(start.transform, "Highlight", new Vector2(0.5f, 1f), new Vector2(0, -6), new Vector2(860, 3), new Color(1f, 0.93f, 0.7f, 0.8f)); hi.rectTransform.pivot = new Vector2(0.5f, 1f);
              var lbl = start.transform.Find("Label").GetComponent<Text>(); lbl.font = DisplayFont(); lbl.fontSize = 72; lbl.text = "TO  BATTLE"; lbl.transform.SetAsLastSibling(); }
            dailyBtn = MakeGhost(titleSheet.transform, "Daily supply drop · +" + Depot.DailyPoints + " points", new Vector2(0.5f, 0f), new Vector2(-235, 470), new Vector2(450, 70), 24, () => OnDaily?.Invoke(), 0.1f);
            dailyBtn.GetComponent<Image>().color = new Color(0.16f, 0.36f, 0.22f, 0.92f);
            reserveBtn = MakeGhost(titleSheet.transform, "4th tank tonight · watch an ad", new Vector2(0.5f, 0f), new Vector2(235, 470), new Vector2(450, 70), 24, () => OnReserveAd?.Invoke(), 0.12f);
            reserveNote = MakeText(titleSheet.transform, "ReserveNote", new Vector2(0.5f, 0f), new Vector2(0, 405), TextAnchor.MiddleCenter, 22, dim); reserveNote.text = "";
            titleStats = MakeText(titleSheet.transform, "Stats", new Vector2(0.5f, 0f), new Vector2(0, 345), TextAnchor.MiddleCenter, 24, new Color(0.72f, 0.7f, 0.66f)); titleStats.rectTransform.sizeDelta = new Vector2(940, 80);
            // the standing orders on their own sheet
            ordersSheet = new GameObject("Orders", typeof(RectTransform), typeof(Image)); ordersSheet.transform.SetParent(root, false);
            Stretch(ordersSheet); ordersSheet.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.05f, 1f);
            MakeText(ordersSheet.transform, "Title", new Vector2(0.5f, 1f), new Vector2(0, -170), TextAnchor.MiddleCenter, 96, ink).text = "Standing orders";
            MakeText(ordersSheet.transform, "Note", new Vector2(0.5f, 1f), new Vector2(0, -270), TextAnchor.MiddleCenter, 28, dim).text = "Carry them out over the nights; the reward goes to the depot.";
            var orders = new GameObject("Rows", typeof(RectTransform)); orders.transform.SetParent(ordersSheet.transform, false);
            var ort = orders.GetComponent<RectTransform>(); ort.anchorMin = ort.anchorMax = new Vector2(0.5f, 1f); ort.anchoredPosition = new Vector2(0, -400); ort.sizeDelta = Vector2.zero; missionRoot = orders.transform;
            MakeButton(ordersSheet.transform, "Back", new Vector2(0.5f, 0f), new Vector2(0, 150), new Vector2(880, 130), 40, () => ordersSheet.SetActive(false));
            ordersSheet.SetActive(false);
            MakeText(titleSheet.transform, "Credits", new Vector2(0.5f, 0f), new Vector2(0, 70), TextAnchor.MiddleCenter, 18, new Color(0.45f, 0.44f, 0.4f)).text = "Tank models by mamont nikita, buffinbag, Hxhdjdjdk, Julian, Artem Goyko, XxRxX, Mr_Chiko, Joanthan To · Sketchfab, CC BY 4.0 · Built with DINOv3 · TRELLIS 2 · Unity";
            var bar = MakeCard(titleSheet.transform, "Bar", new Vector2(0.5f, 0f), new Vector2(0, 170), new Vector2(920, 90), new Color(0.05f, 0.06f, 0.08f, 0.7f)); bar.rectTransform.pivot = new Vector2(0.5f, 0f);
            string[] barLabels = { "How to play", "Medals", "Records", "Settings" }; System.Action[] barActs = { () => { helpSheet.SetActive(true); }, () => { ShowMedals(); }, () => { ShowRecords(); }, () => { ShowSettings(); } };
            for (int i = 0; i < 4; i++) { var bb = MakeButton(bar.transform, barLabels[i], new Vector2(0.5f, 0.5f), new Vector2(-345 + i * 230, 0), new Vector2(222, 74), 24, barActs[i]); bb.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f); var lt = bb.transform.Find("Label").GetComponent<Text>(); lt.text = Spaced(barLabels[i].ToUpperInvariant()); lt.color = new Color(0.82f, 0.8f, 0.76f); if (i > 0) { var div = MakeImage(bar.transform, "Div", new Vector2(0.5f, 0.5f), new Vector2(-345 + i * 230 - 115, 0), new Vector2(2, 40), new Color(1f, 1f, 1f, 0.12f)); div.rectTransform.pivot = new Vector2(0.5f, 0.5f); } }
            titleSheet.SetActive(false);

            // settings and credits
            settingsSheet = new GameObject("Settings", typeof(RectTransform), typeof(Image)); settingsSheet.transform.SetParent(root, false);
            Stretch(settingsSheet); settingsSheet.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.05f, 1f);
            MakeText(settingsSheet.transform, "Title", new Vector2(0.5f, 1f), new Vector2(0, -170), TextAnchor.MiddleCenter, 96, ink).text = "Settings";
            setSound = MakeButton(settingsSheet.transform, "Sound", new Vector2(0.5f, 1f), new Vector2(0, -320), new Vector2(880, 110), 36, () => { OnSound?.Invoke(); RefreshSettings(); }).transform.Find("Label").GetComponent<Text>();
            setQuality = MakeButton(settingsSheet.transform, "Quality", new Vector2(0.5f, 1f), new Vector2(0, -580), new Vector2(880, 110), 36, () => { OnQuality?.Invoke(); RefreshSettings(); }).transform.Find("Label").GetComponent<Text>();
            setMusic = MakeButton(settingsSheet.transform, "Music", new Vector2(0.5f, 1f), new Vector2(0, -450), new Vector2(880, 110), 36, () => { Sfx.MusicOff = !Sfx.MusicOff; RefreshSettings(); }).transform.Find("Label").GetComponent<Text>();
            setVibe = MakeButton(settingsSheet.transform, "Vibration", new Vector2(0.5f, 1f), new Vector2(0, -710), new Vector2(880, 110), 36, () => { PlayerPrefs.SetInt("vibe", PlayerPrefs.GetInt("vibe", 1) == 1 ? 0 : 1); PlayerPrefs.Save(); RefreshSettings(); }).transform.Find("Label").GetComponent<Text>();
            MakeText(settingsSheet.transform, "QualityNote", new Vector2(0.5f, 1f), new Vector2(0, -790), TextAnchor.MiddleCenter, 26, dim).text = "Low quality: no shadows, no rain, simpler hedges - for phones that stutter.";
            MakeText(settingsSheet.transform, "CreditsTitle", new Vector2(0.5f, 1f), new Vector2(0, -735), TextAnchor.MiddleCenter, 40, ink).text = "Credits";
            var cr = MakeText(settingsSheet.transform, "Credits", new Vector2(0.5f, 1f), new Vector2(0, -790), TextAnchor.UpperLeft, 26, new Color(0.8f, 0.78f, 0.73f)); cr.rectTransform.sizeDelta = new Vector2(920, 900); cr.rectTransform.pivot = new Vector2(0.5f, 1f);
            cr.text = "Tank models (Sketchfab, CC BY 4.0, modified):\n" +
                "  Sherman M4A3 Green Set E - mamont nikita\n  M24 Chaffee - buffinbag\n  M26 Pershing Eagle 7 - Hxhdjdjdk\n  T-34 85 Tank - Julian\n  Kv-1 - Artem Goyko\n  SU-100 - XxRxX\n  IS-2M - Mr_Chiko\n  Panzer IV Medium Tank - Joanthan To (Toshueyi)\n" +
                "  creativecommons.org/licenses/by/4.0\n\n" +
                "Other vehicles, props and pictures: generated for this game (Microsoft TRELLIS 2, MIT; built with DINOv3).\n" +
                "Sound effects: Pixabay (Pixabay Content License) and Mixkit (Mixkit License), authors listed in the game's SOURCES.\n" +
                "Menu march: Sousa's 'The U.S. Field Artillery', United States Marine Band - public domain.\n" +
                "Made with Unity.\n\nProgress is kept on this device only. No account, no personal data.";
            MakeButton(settingsSheet.transform, "Privacy policy", new Vector2(0.5f, 0f), new Vector2(-230, 150), new Vector2(420, 130), 34, () => Application.OpenURL("https://petermonev.github.io/iron-night/privacy.html"));
            MakeButton(settingsSheet.transform, "Back", new Vector2(0.5f, 0f), new Vector2(230, 150), new Vector2(420, 130), 40, () => settingsSheet.SetActive(false));
            settingsSheet.SetActive(false);

            // how to play
            helpSheet = new GameObject("Help", typeof(RectTransform), typeof(Image)); helpSheet.transform.SetParent(root, false);
            Stretch(helpSheet); helpSheet.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.05f, 1f);
            MakeText(helpSheet.transform, "Title", new Vector2(0.5f, 1f), new Vector2(0, -170), TextAnchor.MiddleCenter, 96, ink).text = "How to play";
            var help = MakeText(helpSheet.transform, "Text", new Vector2(0.5f, 1f), new Vector2(0, -320), TextAnchor.UpperLeft, 34, new Color(0.85f, 0.83f, 0.78f)); help.rectTransform.sizeDelta = new Vector2(920, 1400);
            help.text = "Drag anywhere to drive the leader. The turrets aim and fire on their own.\n\n" +
                "Every enemy you destroy leaves a flare. Drive over it: a new tank joins the platoon, up to three (a fourth with the ad).\n\n" +
                "Pick a formation at the bottom. Wedge for the open field, column for the lanes, line to bring every gun to bear.\n\n" +
                "Hedges stop tanks; drive through the gates. Farm buildings stop shells: use them as cover, or deny them to the enemy.\n\n" +
                "Anti-tank guns dig in behind sandbags and the 88s stand with the searchlights. Hit them from the side.\n\n" +
                "Green smoke marks the objective. Supply crates come down by parachute. Level up and choose a card.\n\n" +
                "Hold until 5:00. Dawn is a win.";
            MakeButton(helpSheet.transform, "Back", new Vector2(0.5f, 0f), new Vector2(0, 150), new Vector2(880, 130), 40, () => helpSheet.SetActive(false));
            helpSheet.SetActive(false);

            // medals
            medalsSheet = new GameObject("Medals", typeof(RectTransform), typeof(Image)); medalsSheet.transform.SetParent(root, false);
            Stretch(medalsSheet); medalsSheet.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.05f, 1f);
            MakeText(medalsSheet.transform, "Title", new Vector2(0.5f, 1f), new Vector2(0, -150), TextAnchor.MiddleCenter, 96, ink).text = "Medals";
            var mrows = new GameObject("Rows", typeof(RectTransform)); mrows.transform.SetParent(medalsSheet.transform, false);
            var mrt = mrows.GetComponent<RectTransform>(); mrt.anchorMin = mrt.anchorMax = new Vector2(0.5f, 1f); mrt.pivot = new Vector2(0.5f, 1f); mrt.anchoredPosition = new Vector2(0, -260); mrt.sizeDelta = Vector2.zero; medalRows = mrows.transform;
            MakeButton(medalsSheet.transform, "Back", new Vector2(0.5f, 0f), new Vector2(0, 150), new Vector2(880, 130), 40, () => medalsSheet.SetActive(false));
            medalsSheet.SetActive(false);

            // records
            recordsSheet = new GameObject("Records", typeof(RectTransform), typeof(Image)); recordsSheet.transform.SetParent(root, false);
            Stretch(recordsSheet); recordsSheet.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.05f, 1f);
            MakeText(recordsSheet.transform, "Title", new Vector2(0.5f, 1f), new Vector2(0, -150), TextAnchor.MiddleCenter, 96, ink).text = "Records";
            var rrows2 = new GameObject("Rows", typeof(RectTransform)); rrows2.transform.SetParent(recordsSheet.transform, false);
            var rrt2 = rrows2.GetComponent<RectTransform>(); rrt2.anchorMin = rrt2.anchorMax = new Vector2(0.5f, 1f); rrt2.pivot = new Vector2(0.5f, 1f); rrt2.anchoredPosition = new Vector2(0, -260); rrt2.sizeDelta = Vector2.zero; recordRows = rrows2.transform;
            MakeButton(recordsSheet.transform, "Back", new Vector2(0.5f, 0f), new Vector2(0, 150), new Vector2(880, 130), 40, () => recordsSheet.SetActive(false));
            recordsSheet.SetActive(false);

            // depot sheet
            depotSheet = new GameObject("Depot", typeof(RectTransform), typeof(Image)); depotSheet.transform.SetParent(root, false);
            Stretch(depotSheet); depotSheet.GetComponent<Image>().color = new Color(0.02f, 0.02f, 0.03f, 1f);
            { var bd = new GameObject("Backdrop", typeof(RectTransform), typeof(RawImage), typeof(GarageDrag)); bd.transform.SetParent(depotSheet.transform, false); Stretch(bd); depotBackdrop = bd.GetComponent<RawImage>(); depotBackdrop.color = Color.white; depotDrag = bd.GetComponent<GarageDrag>();
              var top = MakeImage(depotSheet.transform, "TopShade", new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1400f, 700f), new Color(0.02f, 0.02f, 0.03f, 0.85f)); top.sprite = Lightswarm.ProceduralSprites.GradientDown(128, 1.6f); top.rectTransform.localScale = new Vector3(1f, -1f, 1f);
              var low = MakeImage(depotSheet.transform, "LowShade", new Vector2(0.5f, 0f), Vector2.zero, new Vector2(1400f, 1500f), new Color(0.02f, 0.02f, 0.03f, 0.97f)); low.sprite = Lightswarm.ProceduralSprites.GradientDown(128, 2.2f); }
            { var back = MakeGhost(depotSheet.transform, "Back", new Vector2(0f, 1f), new Vector2(120, -105), new Vector2(180, 70), 26, () => OnBack?.Invoke(), 0.25f); back.transform.Find("Label").GetComponent<Text>().text = Spaced("BACK"); }
            { var dt = MakeText(depotSheet.transform, "Title", new Vector2(0.5f, 1f), new Vector2(0, -110), TextAnchor.MiddleCenter, 84, ink); dt.text = "DEPOT"; dt.font = DisplayFont(); dt.verticalOverflow = VerticalWrapMode.Overflow; var dsh = dt.gameObject.AddComponent<UnityEngine.UI.Shadow>(); dsh.effectColor = new Color(0f, 0f, 0f, 0.8f); dsh.effectDistance = new Vector2(0f, -5f); }
            { var pts = MakeCard(depotSheet.transform, "Points", new Vector2(1f, 1f), new Vector2(-50, -70), new Vector2(300, 72), new Color(0.06f, 0.07f, 0.09f, 0.75f), 0.2f); pts.rectTransform.pivot = new Vector2(1f, 1f);
              var coin = MakeImage(pts.transform, "Coin", new Vector2(0f, 0.5f), new Vector2(18, 0), new Vector2(30, 30), new Color(0.96f, 0.68f, 0.24f)); coin.sprite = Lightswarm.ProceduralSprites.Ring(64, 0.16f); coin.rectTransform.pivot = new Vector2(0f, 0.5f);
              depotPoints = MakeText(pts.transform, "Text", new Vector2(0f, 0.5f), new Vector2(64, 0), TextAnchor.MiddleLeft, 30, new Color(0.96f, 0.68f, 0.24f)); depotPoints.rectTransform.pivot = new Vector2(0f, 0.5f); depotPoints.rectTransform.sizeDelta = new Vector2(230, 72); depotPoints.font = BoldFont(); }
            MakeText(depotSheet.transform, "Hint", new Vector2(0.5f, 0f), new Vector2(0, 1062), TextAnchor.MiddleCenter, 22, dim).text = "drag the tank to turn it · tap a tank in the list to see it";
            var panel = MakeCard(depotSheet.transform, "Panel", new Vector2(0.5f, 0f), new Vector2(0, 40), new Vector2(1000, 1000), new Color(0.04f, 0.05f, 0.07f, 0.82f), 0.14f); panel.rectTransform.pivot = new Vector2(0.5f, 0f);
            { var seg = MakeCard(panel.transform, "Tabs", new Vector2(0.5f, 1f), new Vector2(0, -22), new Vector2(920, 84), new Color(0.03f, 0.04f, 0.05f, 0.9f), 0.1f); seg.rectTransform.pivot = new Vector2(0.5f, 1f);
              depotTabs[0] = MakeButton(seg.transform, "Upgrades", new Vector2(0.5f, 0.5f), new Vector2(-302, 0), new Vector2(296, 70), 28, () => { depotTab = 0; RefreshDepot(); }).GetComponent<Button>();
              depotTabs[1] = MakeButton(seg.transform, "Garage", new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(296, 70), 28, () => { depotTab = 1; RefreshDepot(); }).GetComponent<Button>();
              depotTabs[2] = MakeButton(seg.transform, "Crew", new Vector2(0.5f, 0.5f), new Vector2(302, 0), new Vector2(296, 70), 28, () => { depotTab = 2; RefreshDepot(); }).GetComponent<Button>();
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
            { curtain = MakeImage(canvasGo.transform, "Curtain", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000, 4000), new Color(0.01f, 0.01f, 0.015f, 1f)); curtain.transform.SetAsLastSibling(); Curtain(0f); }
        }

        public void ShowTitle(bool reserveGranted)
        {
            if (garage != null) { garage.SetActive(false); garage.Show(VehicleSpec.ById(Depot.LeaderId)); garage.SetTitle(true); garage.Frame(false); titleBackdrop.texture = garage.TitleTexture; }
            Sfx.Music(true);
            Depot.Load(); Medals.Check(); hudGroup.SetActive(false); endSheet.SetActive(false); depotSheet.SetActive(false); dailyBtn.SetActive(Depot.DailyReady);
            rankLine.text = Depot.Rank.ToUpperInvariant() + "\n" + (Depot.NightsFought == 0 ? "first night" : Depot.NightsFought + " nights · " + Depot.CrewName.ToLowerInvariant() + " · " + Depot.CrewNights + " together"); Tick(pointsLine, Depot.Points);
            int m = Mathf.FloorToInt(Depot.BestTime / 60f), s = Mathf.FloorToInt(Depot.BestTime % 60f);
            { var sheet = Resources.Load<Texture2D>("UI/rank_insignia"); if (sheet != null && rankBadge != null) { int cell = Depot.RankIndex; rankBadge.sprite = UiSprite("rank_insignia", new Rect(cell * sheet.width / 6f, 0f, sheet.width / 6f, sheet.height)); rankBadge.enabled = Depot.NightsFought > 0; } }
            { int cn = Depot.CampaignNight; var lbl = campaignBtn.transform.Find("Label").GetComponent<Text>(); lbl.text = cn == 0 ? Spaced("CAMPAIGN") : Spaced("NIGHT " + cn + " OF 3");
              var cs = UiSprite(cn == 2 ? "campaign_ardennes" : cn == 3 ? "campaign_lastpush" : "campaign_normandy"); if (cs != null && campaignPic != null) { campaignPic.sprite = cs; float cover = Mathf.Max(280f / cs.rect.width, 240f / cs.rect.height); campaignPic.rectTransform.sizeDelta = new Vector2(cs.rect.width * cover, cs.rect.height * cover); } }
            titleStats.text = Depot.NightsFought == 0 ? "First night. Drag anywhere to drive; the turrets fire on their own." : $"Best {Depot.BestKills} kills · longest {m}:{s:00} · {Depot.CrewName}: {Depot.CrewBonusText}";
            reserveBtn.SetActive(!reserveGranted); reserveNote.text = reserveGranted ? "Reserve tank granted: the platoon can grow to 4 tonight." : "";
            Missions.Load(); foreach (Transform c in missionRoot) Destroy(c.gameObject); if (ordersCount != null) ordersCount.text = Missions.Active.Count + " standing";
            for (int i = 0; i < Missions.Active.Count; i++)
            {
                var o = Missions.Active[i]; float y = -i * 72f;
                var row = MakeImage(missionRoot, "Order", new Vector2(0.5f, 0.5f), new Vector2(0, y), new Vector2(860, 64), new Color(0.1f, 0.11f, 0.13f, 0.55f));
                var txt = MakeText(row.transform, "Text", new Vector2(0f, 0.5f), new Vector2(24, 4), TextAnchor.MiddleLeft, 27, new Color(0.93f, 0.91f, 0.86f)); txt.text = o.Title; txt.rectTransform.sizeDelta = new Vector2(600, 64);
                var prog = MakeText(row.transform, "Progress", new Vector2(1f, 0.5f), new Vector2(-130, 4), TextAnchor.MiddleRight, 25, new Color(0.66f, 0.64f, 0.59f)); prog.text = Missions.Progress(o); prog.rectTransform.sizeDelta = new Vector2(200, 64);
                var rew = MakeText(row.transform, "Reward", new Vector2(1f, 0.5f), new Vector2(-24, 4), TextAnchor.MiddleRight, 27, new Color(0.96f, 0.68f, 0.24f)); rew.text = "+" + o.reward; rew.font = BoldFont(); rew.rectTransform.sizeDelta = new Vector2(120, 64);
                { var track = MakeImage(row.transform, "Track", new Vector2(0f, 0f), new Vector2(24, 8), new Vector2(812, 4), new Color(1f, 1f, 1f, 0.1f)); track.rectTransform.pivot = new Vector2(0f, 0f); var fill = MakeImage(row.transform, "Fill", new Vector2(0f, 0f), new Vector2(24, 8), new Vector2(812 * Mathf.Clamp01(Missions.Fraction(o)), 4), new Color(0.96f, 0.68f, 0.24f, 0.9f)); fill.rectTransform.pivot = new Vector2(0f, 0f); }
            }
            titleSheet.SetActive(true);
        }
        public void HideTitle() { titleSheet.SetActive(false); depotSheet.SetActive(false); hudGroup.SetActive(true); Sfx.Music(false); if (garage != null) { garage.SetTitle(false); garage.SetActive(false); } }
        void ShowOrders() { ordersSheet.SetActive(true); }

        /// <summary>Test switch --garage=id: straight into the garage tab with that tank on the turntable.</summary>
        public void ShowGarage(VehicleSpec spec) { depotTab = 1; ShowDepot(); if (garage != null) garage.Show(spec); }

        public void ShowDepot()
        {
            Depot.Load(); titleSheet.SetActive(false); endSheet.SetActive(false); depotSheet.SetActive(true);
            if (garage != null) { garage.Show(VehicleSpec.ById(Depot.LeaderId)); garage.SetTitle(true); garage.Frame(true); depotBackdrop.texture = garage.TitleTexture; depotDrag.garage = garage; }
            RefreshDepot(); if (depotScroll != null) depotScroll.verticalNormalizedPosition = 1f;
        }

        void RefreshDepot()
        {
            Tick(depotPoints, Depot.Points);
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
                for (int k = 0; k < Depot.Camos.Length; k++)
                {
                    var c = Depot.Camos[k]; bool owned = Depot.OwnsCamo(c), chosen = Depot.CamoId == c.id, can = owned || Depot.Points >= c.cost;
                    var b = MakeButton(row.transform, owned ? c.name : $"{c.name} · {c.cost}", new Vector2(0f, 0f), new Vector2(130 + k * 225, 50), new Vector2(210, 80), 26, () => { if (Depot.PickCamo(c)) RefreshDepot(); });
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
            // the nation: two flags
            {
                var row = MakeImage(depotRows, "Row nation", new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(940, 210), new Color(0.08f, 0.09f, 0.1f, 0.96f)); row.rectTransform.pivot = new Vector2(0.5f, 1f);
                var title = MakeText(row.transform, "Title", new Vector2(0f, 1f), new Vector2(30, -18), TextAnchor.UpperLeft, 40, ink); title.text = "Nation"; title.rectTransform.sizeDelta = new Vector2(600, 50);
                var flags = Resources.Load<Texture2D>("UI/flags");
                for (int k = 0; k < 2; k++)
                {
                    string id = k == 0 ? "us" : "su"; bool on = nation == id;
                    var b = MakeButton(row.transform, k == 0 ? "United States" : "Soviet Union", new Vector2(0f, 0f), new Vector2(240 + k * 460, 20), new Vector2(440, 120), 30, () => { Depot.Nation = id; RefreshDepot(); });
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
                var desc = MakeText(row.transform, "Desc", new Vector2(0f, 1f), new Vector2(left, -124), TextAnchor.UpperLeft, 22, dim); desc.text = c.desc; desc.rectTransform.sizeDelta = new Vector2(440, 60);
                if (garage != null && here) { var look = MakeButton(row.transform, "", new Vector2(0f, 0.5f), new Vector2(330, 0), new Vector2(660, 190), 10, () => garage.Show(spec)); look.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f); look.transform.SetAsFirstSibling(); }
                var b = MakeButton(row.transform, chosen ? "Leading" : owned ? "Lead" : $"{c.cost} pts", new Vector2(1f, 0f), new Vector2(-120, 44), new Vector2(200, 64), 26, () => { if (Depot.PickLeader(c)) { if (garage != null) garage.Show(spec); RefreshDepot(); } });
                b.GetComponent<Image>().color = chosen ? new Color(0.95f, 0.66f, 0.23f, 0.95f) : can ? new Color(0.2f, 0.22f, 0.24f, 0.95f) : new Color(0.12f, 0.12f, 0.13f, 0.9f);
                b.transform.Find("Label").GetComponent<Text>().color = chosen ? new Color(0.1f, 0.08f, 0.05f) : can ? ink : new Color(0.5f, 0.48f, 0.45f);
                b.GetComponent<Button>().interactable = can && !chosen;
                y -= 202f;
            }
        }

        /// <summary>The crew tab: the four men in the leader's tank, their nights together and what they bring; the commander who rides with them.</summary>
        void RefreshCrew()
        {
            var ink = new Color(0.93f, 0.91f, 0.86f); var dim = new Color(0.66f, 0.64f, 0.59f); var amber = new Color(0.95f, 0.66f, 0.23f); string nation = Depot.Nation; float y = 0f;
            {
                var row = MakeImage(depotRows, "Row crew", new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(940, 120), new Color(0.08f, 0.09f, 0.1f, 0.96f)); row.rectTransform.pivot = new Vector2(0.5f, 1f);
                var title = MakeText(row.transform, "Title", new Vector2(0f, 1f), new Vector2(30, -16), TextAnchor.UpperLeft, 40, ink); title.text = Depot.CrewName; title.font = BoldFont(); title.rectTransform.sizeDelta = new Vector2(600, 50);
                var sub = MakeText(row.transform, "Sub", new Vector2(0f, 1f), new Vector2(30, -66), TextAnchor.UpperLeft, 24, amber); sub.text = Depot.CrewNights + (Depot.CrewNights == 1 ? " night together · " : " nights together · ") + Depot.CrewBonusText; sub.rectTransform.sizeDelta = new Vector2(880, 40);
                y -= 132f;
            }
            for (int r = 0; r < 4; r++)
            {
                // a portrait tile per man: two per row
                float x = (r % 2 == 0) ? -235f : 235f; float ry = y - (r / 2) * 400f;
                var tile = MakeImage(depotRows, "Row man" + r, new Vector2(0.5f, 1f), new Vector2(x, ry), new Vector2(456, 388), new Color(0.08f, 0.09f, 0.1f, 0.96f)); tile.rectTransform.pivot = new Vector2(0.5f, 1f);
                var mask = new GameObject("Mask", typeof(RectTransform), typeof(RectMask2D)); mask.transform.SetParent(tile.transform, false); var mkrt = mask.GetComponent<RectTransform>(); mkrt.anchorMin = mkrt.anchorMax = new Vector2(0.5f, 1f); mkrt.pivot = new Vector2(0.5f, 1f); mkrt.sizeDelta = new Vector2(440, 280); mkrt.anchoredPosition = new Vector2(0, -8);
                var pic = MakeImage(mask.transform, "Pic", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440, 440), Color.white); pic.rectTransform.pivot = new Vector2(0.5f, 0.5f); pic.sprite = UiSprite("crew_" + nation + "_" + Depot.CrewRoles[r]); pic.rectTransform.anchoredPosition = new Vector2(0, -30);
                var fade = MakeImage(mask.transform, "Fade", new Vector2(0.5f, 0f), Vector2.zero, new Vector2(440, 120), new Color(0.06f, 0.07f, 0.08f, 0.95f)); fade.sprite = Lightswarm.ProceduralSprites.GradientDown(64, 1.2f); fade.rectTransform.pivot = new Vector2(0.5f, 0f);
                var role = MakeText(tile.transform, "Role", new Vector2(0.5f, 0f), new Vector2(0, 64), TextAnchor.LowerCenter, 22, amber); role.text = Spaced(Depot.CrewRoleText(r).ToUpperInvariant()); role.font = BoldFont(); role.rectTransform.sizeDelta = new Vector2(440, 30);
                var name = MakeText(tile.transform, "Name", new Vector2(0.5f, 0f), new Vector2(0, 22), TextAnchor.LowerCenter, 28, ink); name.text = Depot.CrewMan(r); name.rectTransform.sizeDelta = new Vector2(440, 40);
            }
            y -= 812f;
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
                else { a = MakeImage(canvas.transform, "Arrow", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(54, 54), new Color(0.95f, 0.3f, 0.25f, 0.9f)); a.sprite = arrowSprite; a.rectTransform.pivot = new Vector2(0.5f, 0.5f); arrows.Add(a); }
                a.enabled = true; a.rectTransform.anchoredPosition = d * k; a.rectTransform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f); used++;
            }
            for (int i = used; i < arrows.Count; i++) arrows[i].enabled = false;
        }

        /// <summary>The radar in the corner: enemies red, wingmen green, the objective amber, ninety metres to the rim, north up.</summary>
        public void Radar(List<Vehicle> foes, List<Vehicle> platoon, Vector3 leader, Vector3 objective, bool hasObjective)
        {
            int used = 0; float range = Depot.CommanderBonus == "radar" ? 120f : 90f; const float rim = 110f;
            Image Dot(Vector3 world, Color c, float size)
            {
                Image d; if (used < radarDots.Count) d = radarDots[used]; else { d = MakeImage(radar.transform, "Dot", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10, 10), Color.white); d.sprite = Lightswarm.ProceduralSprites.Glow(16, 0.9f); d.rectTransform.pivot = new Vector2(0.5f, 0.5f); radarDots.Add(d); }
                used++; var o = new Vector2(world.x - leader.x, world.z - leader.z) * (rim / range); if (o.magnitude > rim) o = o.normalized * rim;
                d.enabled = true; d.color = c; d.rectTransform.sizeDelta = new Vector2(size, size); d.rectTransform.anchoredPosition = o; return d;
            }
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
        public void HandTo(TouchStick stick) { if (stick == null) return; stick.Blockers.Add(ammoBtn.GetComponent<RectTransform>()); stick.Blockers.Add(abilityBtn.GetComponent<RectTransform>()); stick.Blockers.Add(pauseBtnRect); }

        public void SetAmmo(int ap, int he, bool loadHe)
        {
            if (ammoKind == null) return;
            ammoKind.text = loadHe ? "HE" : "AP"; ammoKind.color = loadHe ? new Color(1f, 0.55f, 0.3f) : new Color(0.96f, 0.68f, 0.24f);
            ammoLabel.text = (loadHe ? he : ap) + "\n" + (loadHe ? ap + " AP" : he + " HE");
            ammoBtn.GetComponent<Image>().color = (loadHe ? he : ap) > 0 ? new Color(0.08f, 0.09f, 0.11f, 0.85f) : new Color(0.3f, 0.08f, 0.06f, 0.9f);
        }

        /// <summary>The commander's button: his portrait, the cooldown draining down it, a glow while his order lasts.</summary>
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
            var sp = UiSprite(picture); briefingPic.sprite = sp; briefingPic.enabled = sp != null; briefingTitle.text = title; briefingText.text = text; briefing.SetActive(true); briefingLeft = 4.5f;
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

        public void ShowCards(List<Card> cards, System.Action<string> onPick)
        {
            foreach (Transform c in cardRoot) Destroy(c.gameObject);
            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                var b = MakeButton(cardRoot, card.title, new Vector2(0.5f, 0.5f), new Vector2(0, 300 - i * 290), new Vector2(900, 250), 56, () => { sheet.SetActive(false); onPick(card.id); });
                b.GetComponent<Image>().color = card.rare ? new Color(0.16f, 0.12f, 0.05f, 0.97f) : new Color(0.08f, 0.09f, 0.1f, 0.96f);
                var title = b.transform.Find("Label").GetComponent<Text>(); if (card.rare) title.color = new Color(0.95f, 0.66f, 0.23f);
                title.alignment = TextAnchor.UpperLeft; title.rectTransform.anchorMin = title.rectTransform.anchorMax = title.rectTransform.pivot = new Vector2(0f, 1f);
                var pic = UiSprite(CardPicture(card.id)); float left = pic != null ? 262f : 30f;
                if (pic != null) { var art = MakeImage(b.transform, "Art", new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(214f, 214f), Color.white); art.sprite = pic; }
                title.rectTransform.anchoredPosition = new Vector2(left, -22f); title.rectTransform.sizeDelta = new Vector2(900f - left - 20f, 70f);
                var desc = MakeText(b.transform, "Desc", new Vector2(0f, 1f), new Vector2(left, -100f), TextAnchor.UpperLeft, 34, new Color(0.66f, 0.64f, 0.59f));
                desc.rectTransform.sizeDelta = new Vector2(900f - left - 20f, 130f); desc.text = card.desc;
            }
            sheet.SetActive(true);
        }

        public void ShowEnd(bool dawn, string statLine, bool adAvailable, string eyebrow = null, string title = null, string again = null)
        {
            endEyebrow.text = eyebrow ?? (dawn ? "05:00 · DAWN" : "PLATOON LEADER KNOCKED OUT");
            endTitle.text = title ?? (dawn ? "You held the line" : "Assault over");
            againBtn.transform.Find("Label").GetComponent<Text>().text = again ?? "New assault";
            stats.text = statLine;
            adBtn.SetActive(adAvailable);
            adLabel.text = dawn ? "Double score · watch an ad" : Depot.CrewNights == 0 && statLine.Contains("crew is lost") ? "Field repair · save the crew · watch an ad" : "Field repair · watch an ad";
            adNote.text = dawn ? "Rewarded video (mock): ×2 score for the depot." : "Rewarded video (mock): the leader is repaired once and the assault goes on.";
            endSheet.SetActive(true);
        }
        public void HideEnd() { endSheet.SetActive(false); }
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
            medalsSheet.SetActive(true);
        }

        void ShowRecords()
        {
            foreach (Transform c in recordRows) Destroy(c.gameObject);
            var ink = new Color(0.93f, 0.91f, 0.86f); var dim = new Color(0.66f, 0.64f, 0.59f); var amber = new Color(0.95f, 0.66f, 0.23f);
            var totals = MakeText(recordRows, "Totals", new Vector2(0.5f, 1f), new Vector2(0, 0), TextAnchor.UpperCenter, 30, dim); totals.rectTransform.sizeDelta = new Vector2(940, 120);
            totals.text = (Depot.CampaignsWon > 0 ? $"{Depot.CampaignsWon} campaigns won · best {Depot.CampaignBest}\n" : "") + $"{Depot.NightsFought} nights · {Depot.Total("kills")} vehicles · {Depot.Total("tigers")} Tigers · {Depot.Total("guns")} guns · {Depot.Total("infantry")} infantry\n{Depot.Total("dawns")} dawns · {Depot.Total("objectives")} objectives · {Medals.Count}/{Medals.All.Length} medals";
            var log = Depot.NightLog();
            if (log.Count == 0) { var none = MakeText(recordRows, "None", new Vector2(0.5f, 1f), new Vector2(0, -160), TextAnchor.UpperCenter, 30, dim); none.text = "No nights fought yet."; }
            for (int i = 0; i < log.Count; i++)
            {
                var e = log[i]; if (e.Length < 6) continue; float y = -150f - i * 90f;
                var row = MakeImage(recordRows, "Night", new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(940, 80), new Color(0.08f, 0.09f, 0.1f, 0.9f)); row.rectTransform.pivot = new Vector2(0.5f, 1f);
                var left = MakeText(row.transform, "Left", new Vector2(0f, 0.5f), new Vector2(24, 0), TextAnchor.MiddleLeft, 28, ink); left.text = e[0] + " · " + e[1] + (e[5] == "1" ? " · dawn" : ""); left.rectTransform.sizeDelta = new Vector2(500, 80);
                var right = MakeText(row.transform, "Right", new Vector2(1f, 0.5f), new Vector2(-24, 0), TextAnchor.MiddleRight, 28, amber); right.text = e[2] + " kills · " + e[3] + " · " + e[4]; right.rectTransform.sizeDelta = new Vector2(520, 80);
            }
            recordsSheet.SetActive(true);
        }
        /// <summary>Tonight's weather on the title and in the corner of the HUD.</summary>
        public void SetConditions(string sector, string name, string note) { conditions.text = "Tonight: " + sector + " · " + (name == "Clear" ? "clear skies, full moon" : name.ToLowerInvariant() + " · " + note); assaultLabel.text = sector.ToUpperInvariant() + " · " + name.ToUpperInvariant(); }
        public void ShowPause(bool soundOn, bool highQuality) { soundLabel.text = soundOn ? "Sound: on" : "Sound: off"; qualityLabel.text = highQuality ? "Quality: high" : "Quality: low"; pauseSheet.SetActive(true); }
        public void HidePause() { pauseSheet.SetActive(false); }
        public void SetQualityLabel(bool high) { qualityLabel.text = high ? "Quality: high" : "Quality: low"; }
        void ShowSettings() { RefreshSettings(); settingsSheet.SetActive(true); }
        void RefreshSettings()
        {
            setSound.text = PlayerPrefs.GetInt("sound", 1) == 1 ? "Sound: on" : "Sound: off"; setMusic.text = Sfx.MusicOff ? "Music: off" : "Music: on"; setQuality.text = PlayerPrefs.GetInt("quality", 1) == 1 ? "Quality: high" : "Quality: low"; setVibe.text = PlayerPrefs.GetInt("vibe", 1) == 1 ? "Vibration: on" : "Vibration: off";
        }
        public void SetAdNote(string s) { adNote.text = s; }

        void Update()
        {
            fpsAccum += Time.unscaledDeltaTime; fpsFrames++; fpsTimer += Time.unscaledDeltaTime;
            if (fpsTimer >= 0.5f) { float f = fpsFrames / fpsAccum; fps.text = $"{f:0} fps"; fpsAccum = 0; fpsFrames = 0; fpsTimer = 0; if (f < 38f && hudGroup.activeSelf) slowFor += 0.5f; else slowFor = 0f; if (slowFor >= 5f && !slowOffered && PlayerPrefs.GetInt("quality", 1) != 0) { slowOffered = true; Toast("Stuttering? Pause · Quality: low turns off shadows and rain", 5f); } }
            if (toastLeft > 0f) { toastLeft -= Time.unscaledDeltaTime; if (toastLeft <= 0f) toast.text = ""; }
            if (briefingLeft > 0f) { briefingLeft -= Time.unscaledDeltaTime; if (briefingLeft <= 0f && briefing != null) briefing.SetActive(false); }
            TickNumbers(Time.unscaledDeltaTime); CurtainTick(Time.unscaledDeltaTime);
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

        static Font uiFont, uiBold, displayFont;
        static Font DefaultFont() { if (uiFont == null) uiFont = Resources.Load<Font>("Fonts/Barlow-Regular") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); return uiFont; }
        static Font BoldFont() { if (uiBold == null) uiBold = Resources.Load<Font>("Fonts/Barlow-SemiBold") ?? DefaultFont(); return uiBold; }
        static Font DisplayFont() { if (displayFont == null) displayFont = Resources.Load<Font>("Fonts/BebasNeue-Regular") ?? BoldFont(); return displayFont; }
        static Sprite roundSprite, outlineSprite, shadowSprite, scrimSprite;
        Image curtain; float curtainWant, curtainAt = 1f;   // the black that screens change behind
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
        static Sprite Rounded() => roundSprite ??= Lightswarm.ProceduralSprites.RoundedRect(24, 96);
        static Sprite Outline() => outlineSprite ??= Lightswarm.ProceduralSprites.RoundedOutline(24, 96, 2f);
        static Sprite Shadow() => shadowSprite ??= Lightswarm.ProceduralSprites.SoftShadow(24, 28, 160);
        /// <summary>Letters spaced out with hair spaces: the small caps eyebrows and button labels.</summary>
        static string Spaced(string s) { var sb = new System.Text.StringBuilder(); foreach (var ch in s) { sb.Append(ch); if (ch != ' ') sb.Append('\u200A'); } return sb.ToString(); }
        /// <summary>A rounded translucent panel with a faint edge.</summary>
        static Image MakeCard(Transform parent, string name, Vector2 anchor, Vector2 offset, Vector2 size, Color fill, float edgeAlpha = 0.12f)
        {
            var card = MakeImage(parent, name, anchor, offset, size, fill); card.sprite = Rounded(); card.type = Image.Type.Sliced;
            var edge = MakeImage(card.transform, "Edge", new Vector2(0.5f, 0.5f), Vector2.zero, size, new Color(1f, 1f, 1f, edgeAlpha)); edge.sprite = Outline(); edge.type = Image.Type.Sliced; edge.rectTransform.pivot = new Vector2(0.5f, 0.5f); edge.rectTransform.anchoredPosition = Vector2.zero;
            return card;
        }
        /// <summary>The call to action: amber, rounded, a soft shadow under it, dark bold label.</summary>
        static GameObject MakePrimary(Transform parent, string label, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize, System.Action onClick)
        {
            var sh = MakeImage(parent, "Shadow", anchor, pos + new Vector2(0f, -14f), size + new Vector2(40f, 40f), new Color(0f, 0f, 0f, 0.55f)); sh.sprite = Shadow(); sh.type = Image.Type.Sliced; sh.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var b = MakeButton(parent, label, anchor, pos, size, fontSize, onClick); b.GetComponent<Image>().color = new Color(0.96f, 0.68f, 0.24f, 1f);
            var t = b.transform.Find("Label").GetComponent<Text>(); t.color = new Color(0.12f, 0.09f, 0.04f); t.font = BoldFont(); t.text = Spaced(label.ToUpperInvariant());
            return b;
        }
        /// <summary>A quiet button: dark glass with a thin light edge.</summary>
        static GameObject MakeGhost(Transform parent, string label, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize, System.Action onClick, float edgeAlpha = 0.22f)
        {
            var b = MakeButton(parent, label, anchor, pos, size, fontSize, onClick); b.GetComponent<Image>().color = new Color(0.06f, 0.07f, 0.09f, 0.55f);
            var edge = MakeImage(b.transform, "Edge", new Vector2(0.5f, 0.5f), Vector2.zero, size, new Color(1f, 1f, 1f, edgeAlpha)); edge.sprite = Outline(); edge.type = Image.Type.Sliced; edge.rectTransform.pivot = new Vector2(0.5f, 0.5f); edge.rectTransform.anchoredPosition = Vector2.zero; edge.transform.SetSiblingIndex(0);
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
                case "reserve": return "card_crews"; case "veteran": return "card_veteran"; default: return null;
            }
        }
        static readonly string[] MedalPictures = { "medal_recruit", "medal_nightfighter", "medal_bocage", "medal_sharpshooter", "medal_tankace", "medal_tigerslayer", "medal_gunbuster", "medal_trenchbroom", "medal_pathfinder", "medal_aceofaces", "medal_oldguard", "medal_ironnight" };

        public static Image MakeImage(Transform parent, string name, Vector2 anchor, Vector2 offset, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>(); rt.anchorMin = rt.anchorMax = anchor; rt.pivot = anchor; rt.anchoredPosition = offset; rt.sizeDelta = size;
            var img = go.GetComponent<Image>(); img.color = color; img.raycastTarget = false;
            if (name.StartsWith("Row") || name == "Order" || name == "Showroom") { img.sprite = Rounded(); img.type = Image.Type.Sliced; }
            return img;
        }

        static GameObject MakeButton(Transform parent, string label, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize, System.Action onClick)
        {
            var go = new GameObject("Btn " + label, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>(); rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f); rt.anchoredPosition = pos; rt.sizeDelta = size;
            var bi = go.GetComponent<Image>(); bi.color = new Color(0.03f, 0.04f, 0.06f, 0.6f); bi.sprite = Rounded(); bi.type = Image.Type.Sliced;
            go.AddComponent<PressFeel>();
            var btn = go.GetComponent<Button>(); btn.onClick.AddListener(() => { Sfx.Click(); onClick(); }); var cb = btn.colors; cb.highlightedColor = new Color(1f, 1f, 1f, 0.92f); cb.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f); cb.fadeDuration = 0.06f; btn.colors = cb;
            var t = MakeText(go.transform, "Label", new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleCenter, fontSize, new Color(0.93f, 0.91f, 0.86f)); t.font = BoldFont();
            t.GetComponent<RectTransform>().sizeDelta = size; t.text = label;
            return go;
        }
    }
}

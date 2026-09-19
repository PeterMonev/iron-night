using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IronNight
{
    /// <summary>
    /// The overlay: night clock, platoon count, level bar, formation buttons, pause button, toast line, the three-card
    /// level-up sheet, the pause sheet and the end-of-assault sheet with the (mock) rewarded-ad button. Built in code with the legacy UI, English only.
    /// </summary>
    public class Hud : MonoBehaviour
    {
        public class Card { public string id, title, desc; public bool rare; }

        public System.Action<Formation> OnFormation;
        public System.Action OnAd, OnAgain, OnStart, OnCampaign, OnRestart, OnDepot, OnBack, OnReserveAd, OnPause, OnResume, OnSound, OnQuit, OnDaily, OnQuality;

        Text clock, count, fps, levelText, toast, endTitle, endEyebrow, stats, adLabel, adNote, leaderHp, assaultLabel, conditions, qualityLabel; GameObject dailyBtn, helpSheet, medalsSheet, recordsSheet; Transform medalRows, recordRows;
        Image levelFill, flash; GameObject sheet, endSheet, adBtn, titleSheet, depotSheet, hudGroup, reserveBtn, pauseSheet; Text soundLabel; Transform missionRoot;
        class Rising { public Text t; public float life; public Vector3 world; }
        readonly List<Rising> popups = new List<Rising>(); readonly Stack<Text> popupPool = new Stack<Text>(); RectTransform canvasRect;
        Image radar, objectiveArrow; Text objectiveLabel; readonly List<Image> radarDots = new List<Image>(); readonly List<Image> hpBars = new List<Image>(); readonly List<Image> hpFills = new List<Image>(); Transform cardRoot, depotRows; Image rankBadge; GameObject campaignBtn, againBtn; int depotTab; readonly Button[] depotTabs = new Button[2]; ScrollRect depotScroll; Canvas canvas; Text depotPoints, titleStats, endPoints, reserveNote, bossName; Image bossFill; GameObject bossBar;
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
            var pauseBtn = MakeButton(t, "II", new Vector2(0.5f, 1f), new Vector2(0, -150), new Vector2(120, 76), 40, () => OnPause?.Invoke()); pauseBtn.name = "Pause";
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
            arrowSprite = ArrowSprite(); objectiveArrow.sprite = arrowSprite;
            var root = canvasGo.transform;
            endPoints = MakeText(endSheet.transform, "Points", new Vector2(0.5f, 0.5f), new Vector2(0, 120), TextAnchor.MiddleCenter, 44, amber);
            MakeButton(endSheet.transform, "Depot", new Vector2(0.5f, 0.5f), new Vector2(0, -400), new Vector2(880, 130), 40, () => OnDepot?.Invoke());

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

            // title sheet
            titleSheet = new GameObject("Title", typeof(RectTransform), typeof(Image)); titleSheet.transform.SetParent(root, false);
            Stretch(titleSheet); titleSheet.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.04f, 0.72f);
            { var art = UiSprite("keyart"); if (art != null) { var bg = MakeImage(titleSheet.transform, "KeyArt", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1320f, 2347f), new Color(0.78f, 0.78f, 0.8f, 1f)); bg.sprite = art; bg.transform.SetAsFirstSibling(); var shade = MakeImage(titleSheet.transform, "Shade", new Vector2(0.5f, 0.5f), new Vector2(0f, -130f), new Vector2(1080f, 1400f), new Color(0.02f, 0.03f, 0.04f, 0.55f)); shade.transform.SetSiblingIndex(1); } }
            MakeText(titleSheet.transform, "Eyebrow", new Vector2(0.5f, 0.5f), new Vector2(0, 660), TextAnchor.MiddleCenter, 34, dim).text = "WWII · NIGHT ASSAULT";
            var big = MakeText(titleSheet.transform, "Name", new Vector2(0.5f, 0.5f), new Vector2(0, 540), TextAnchor.MiddleCenter, 150, ink); big.text = "IRON NIGHT"; big.fontStyle = FontStyle.Bold; big.rectTransform.sizeDelta = new Vector2(1000, 200);
            MakeText(titleSheet.transform, "Tag", new Vector2(0.5f, 0.5f), new Vector2(0, 430), TextAnchor.MiddleCenter, 36, dim).text = "Lead a Sherman platoon through five minutes of darkness.";
            conditions = MakeText(titleSheet.transform, "Conditions", new Vector2(0.5f, 0.5f), new Vector2(0, 360), TextAnchor.MiddleCenter, 30, amber);
            dailyBtn = MakeButton(titleSheet.transform, "Daily supply drop · +" + Depot.DailyPoints + " points", new Vector2(0.5f, 0.5f), new Vector2(0, 275), new Vector2(880, 80), 30, () => OnDaily?.Invoke());
            dailyBtn.GetComponent<Image>().color = new Color(0.2f, 0.32f, 0.2f, 0.95f);
            var start = MakeButton(titleSheet.transform, "Night assault", new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(880, 150), 52, () => OnStart?.Invoke());
            start.GetComponent<Image>().color = amber; start.transform.Find("Label").GetComponent<Text>().color = new Color(0.1f, 0.08f, 0.05f);
            campaignBtn = MakeButton(titleSheet.transform, "Campaign · three nights", new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(880, 100), 38, () => OnCampaign?.Invoke());
            campaignBtn.GetComponent<Image>().color = new Color(0.55f, 0.36f, 0.14f, 0.95f);
            MakeButton(titleSheet.transform, "Depot", new Vector2(0.5f, 0.5f), new Vector2(0, -80), new Vector2(880, 90), 36, () => OnDepot?.Invoke());
            reserveBtn = MakeButton(titleSheet.transform, "4th tank tonight · watch an ad", new Vector2(0.5f, 0.5f), new Vector2(0, -180), new Vector2(880, 90), 32, () => OnReserveAd?.Invoke());
            reserveNote = MakeText(titleSheet.transform, "ReserveNote", new Vector2(0.5f, 0.5f), new Vector2(0, -225), TextAnchor.MiddleCenter, 26, dim); reserveNote.text = "The platoon holds 3 tanks. A rewarded video opens a 4th slot for this night (mock).";
            MakeText(titleSheet.transform, "OrdersLabel", new Vector2(0.5f, 0.5f), new Vector2(0, -320), TextAnchor.MiddleCenter, 28, dim).text = "STANDING ORDERS";
            var orders = new GameObject("Orders", typeof(RectTransform)); orders.transform.SetParent(titleSheet.transform, false);
            var ort = orders.GetComponent<RectTransform>(); ort.anchorMin = ort.anchorMax = new Vector2(0.5f, 0.5f); ort.anchoredPosition = new Vector2(0, -370); ort.sizeDelta = Vector2.zero; missionRoot = orders.transform;
            rankBadge = MakeImage(titleSheet.transform, "Rank", new Vector2(0.5f, 0.5f), new Vector2(0, -625), new Vector2(90, 90), Color.white); rankBadge.preserveAspect = true;
            titleStats = MakeText(titleSheet.transform, "Stats", new Vector2(0.5f, 0.5f), new Vector2(0, -700), TextAnchor.MiddleCenter, 32, dim); titleStats.rectTransform.sizeDelta = new Vector2(900, 200);
            MakeText(titleSheet.transform, "Credits", new Vector2(0.5f, 0f), new Vector2(0, 70), TextAnchor.MiddleCenter, 24, new Color(0.45f, 0.44f, 0.4f)).text = "Built with DINOv3 · TRELLIS 2 · Unity";
            MakeButton(titleSheet.transform, "How to play", new Vector2(0.5f, 0f), new Vector2(-310, 215), new Vector2(290, 70), 28, () => { helpSheet.SetActive(true); });
            MakeButton(titleSheet.transform, "Medals", new Vector2(0.5f, 0f), new Vector2(0, 215), new Vector2(290, 70), 28, () => { ShowMedals(); });
            MakeButton(titleSheet.transform, "Records", new Vector2(0.5f, 0f), new Vector2(310, 215), new Vector2(290, 70), 28, () => { ShowRecords(); });
            titleSheet.SetActive(false);

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
            Stretch(depotSheet); depotSheet.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.04f, 0.9f);
            MakeText(depotSheet.transform, "Eyebrow", new Vector2(0.5f, 1f), new Vector2(0, -90), TextAnchor.MiddleCenter, 30, dim).text = "THE DEPOT";
            MakeText(depotSheet.transform, "Title", new Vector2(0.5f, 1f), new Vector2(0, -170), TextAnchor.MiddleCenter, 96, ink).text = "Upgrades";
            depotPoints = MakeText(depotSheet.transform, "Points", new Vector2(0.5f, 1f), new Vector2(0, -270), TextAnchor.MiddleCenter, 40, amber);
            depotTabs[0] = MakeButton(depotSheet.transform, "Upgrades", new Vector2(0.5f, 1f), new Vector2(-240, -400), new Vector2(440, 80), 32, () => { depotTab = 0; RefreshDepot(); }).GetComponent<Button>();
            depotTabs[1] = MakeButton(depotSheet.transform, "Garage", new Vector2(0.5f, 1f), new Vector2(240, -400), new Vector2(440, 80), 32, () => { depotTab = 1; RefreshDepot(); }).GetComponent<Button>();
            // the rows scroll in a masked window between the tabs and the Back button
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect)); viewport.transform.SetParent(depotSheet.transform, false);
            var vrt = viewport.GetComponent<RectTransform>(); vrt.anchorMin = new Vector2(0f, 0f); vrt.anchorMax = new Vector2(1f, 1f); vrt.offsetMin = new Vector2(0f, 300f); vrt.offsetMax = new Vector2(0f, -500f);
            viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);   // something to drag on
            var rows = new GameObject("Rows", typeof(RectTransform)); rows.transform.SetParent(viewport.transform, false);
            var rrt = rows.GetComponent<RectTransform>(); rrt.anchorMin = rrt.anchorMax = new Vector2(0.5f, 1f); rrt.pivot = new Vector2(0.5f, 1f); rrt.anchoredPosition = Vector2.zero; rrt.sizeDelta = new Vector2(940f, 2000f);
            depotRows = rows.transform; depotScroll = viewport.GetComponent<ScrollRect>(); depotScroll.content = rrt; depotScroll.viewport = vrt; depotScroll.horizontal = false; depotScroll.vertical = true;
            depotScroll.movementType = ScrollRect.MovementType.Clamped; depotScroll.scrollSensitivity = 40f; depotScroll.inertia = true; depotScroll.decelerationRate = 0.12f;
            MakeButton(depotSheet.transform, "Back", new Vector2(0.5f, 0f), new Vector2(0, 150), new Vector2(880, 130), 40, () => OnBack?.Invoke());
            depotSheet.SetActive(false);
        }

        public void ShowTitle(bool reserveGranted)
        {
            Depot.Load(); Medals.Check(); hudGroup.SetActive(false); endSheet.SetActive(false); depotSheet.SetActive(false); dailyBtn.SetActive(Depot.DailyReady);
            int m = Mathf.FloorToInt(Depot.BestTime / 60f), s = Mathf.FloorToInt(Depot.BestTime % 60f);
            { var sheet = Resources.Load<Texture2D>("UI/rank_insignia"); if (sheet != null && rankBadge != null) { int cell = Depot.RankIndex; rankBadge.sprite = UiSprite("rank_insignia", new Rect(cell * sheet.width / 6f, 0f, sheet.width / 6f, sheet.height)); rankBadge.enabled = Depot.NightsFought > 0; } }
            { int cn = Depot.CampaignNight; var lbl = campaignBtn.transform.Find("Label").GetComponent<Text>(); lbl.text = cn == 0 ? "Campaign · three nights" : cn == 2 ? "Campaign · night 2 of 3 · the Ardennes" : "Campaign · night 3 of 3 · the last push"; campaignBtn.GetComponent<Image>().color = cn == 0 ? new Color(0.55f, 0.36f, 0.14f, 0.95f) : new Color(0.75f, 0.45f, 0.12f, 0.95f); }
            titleStats.text = Depot.NightsFought == 0 ? "First night. Drag anywhere to drive; the turrets fire on their own." : $"{Depot.Rank} · {Depot.NightsFought} nights fought · best {Depot.BestKills} kills · longest {m}:{s:00}\n{Depot.Points} depot points";
            reserveBtn.SetActive(!reserveGranted); reserveNote.text = reserveGranted ? "Reserve tank granted: the platoon can grow to 4 tonight." : "The platoon holds 3 tanks. A rewarded video opens a 4th slot for this night (mock).";
            Missions.Load(); foreach (Transform c in missionRoot) Destroy(c.gameObject);
            for (int i = 0; i < Missions.Active.Count; i++)
            {
                var o = Missions.Active[i]; float y = -i * 72f;
                var row = MakeImage(missionRoot, "Order", new Vector2(0.5f, 0.5f), new Vector2(0, y), new Vector2(880, 64), new Color(0.08f, 0.09f, 0.1f, 0.7f));
                var txt = MakeText(row.transform, "Text", new Vector2(0f, 0.5f), new Vector2(24, 0), TextAnchor.MiddleLeft, 30, new Color(0.93f, 0.91f, 0.86f)); txt.text = o.Title; txt.rectTransform.sizeDelta = new Vector2(600, 64);
                var prog = MakeText(row.transform, "Progress", new Vector2(1f, 0.5f), new Vector2(-150, 0), TextAnchor.MiddleRight, 28, new Color(0.66f, 0.64f, 0.59f)); prog.text = Missions.Progress(o); prog.rectTransform.sizeDelta = new Vector2(200, 64);
                var rew = MakeText(row.transform, "Reward", new Vector2(1f, 0.5f), new Vector2(-24, 0), TextAnchor.MiddleRight, 30, new Color(0.95f, 0.66f, 0.23f)); rew.text = "+" + o.reward; rew.rectTransform.sizeDelta = new Vector2(120, 64);
            }
            titleSheet.SetActive(true);
        }
        public void HideTitle() { titleSheet.SetActive(false); hudGroup.SetActive(true); }

        public void ShowDepot()
        {
            Depot.Load(); titleSheet.SetActive(false); endSheet.SetActive(false); depotSheet.SetActive(true); RefreshDepot(); if (depotScroll != null) depotScroll.verticalNormalizedPosition = 1f;
        }

        void RefreshDepot()
        {
            depotPoints.text = $"{Depot.Points} points";
            foreach (Transform c in depotRows) Destroy(c.gameObject);
            for (int k = 0; k < 2; k++) { bool on = depotTab == k; depotTabs[k].GetComponent<Image>().color = on ? new Color(0.95f, 0.66f, 0.23f, 0.95f) : new Color(0.2f, 0.22f, 0.24f, 0.95f); depotTabs[k].transform.Find("Label").GetComponent<Text>().color = on ? new Color(0.1f, 0.08f, 0.05f) : new Color(0.93f, 0.91f, 0.86f); }
            if (depotTab == 1) { RefreshGarage(); FitDepotRows(); return; }
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
                    var b = MakeButton(row.transform, owned ? c.name : $"{c.name} · {c.cost}", new Vector2(0f, 0f), new Vector2(240 + k * 440, 50), new Vector2(420, 80), 28, () => { if (Depot.PickLeader(c)) RefreshDepot(); });
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
                y -= 230f;
            }
            // the tanks of the tree
            foreach (var c in Depot.Leaders)
            {
                if (c.nation != nation) continue; var spec = VehicleSpec.ById(c.id); bool here = VehicleSpec.Available(spec);
                bool owned = Depot.OwnsLeader(c), chosen = Depot.LeaderId == c.id, can = here && (owned || Depot.Points >= c.cost);
                var row = MakeImage(depotRows, "Row " + c.id, new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(940, 150), new Color(0.08f, 0.09f, 0.1f, here ? 0.96f : 0.6f)); row.rectTransform.pivot = new Vector2(0.5f, 1f);
                var name = MakeText(row.transform, "Name", new Vector2(0f, 1f), new Vector2(30, -14), TextAnchor.UpperLeft, 38, here ? ink : dim); name.text = c.name + (here ? "" : "  · coming"); name.rectTransform.sizeDelta = new Vector2(560, 46);
                var stats = MakeText(row.transform, "Stats", new Vector2(0f, 1f), new Vector2(30, -58), TextAnchor.UpperLeft, 24, amber); stats.text = $"speed {spec.speed:0}  ·  gun {spec.damage:0.#}  ·  reload {spec.reload:0.#} s  ·  range {spec.range:0} m  ·  hits {spec.hp:0.#}"; stats.rectTransform.sizeDelta = new Vector2(600, 34);
                var desc = MakeText(row.transform, "Desc", new Vector2(0f, 1f), new Vector2(30, -92), TextAnchor.UpperLeft, 24, dim); desc.text = c.desc; desc.rectTransform.sizeDelta = new Vector2(600, 56);
                var b = MakeButton(row.transform, chosen ? "Leading" : owned ? "Lead" : $"{c.cost} pts", new Vector2(1f, 0.5f), new Vector2(-150, 0), new Vector2(260, 80), 28, () => { if (Depot.PickLeader(c)) RefreshDepot(); });
                b.GetComponent<Image>().color = chosen ? new Color(0.95f, 0.66f, 0.23f, 0.95f) : can ? new Color(0.2f, 0.22f, 0.24f, 0.95f) : new Color(0.12f, 0.12f, 0.13f, 0.9f);
                b.transform.Find("Label").GetComponent<Text>().color = chosen ? new Color(0.1f, 0.08f, 0.05f) : can ? ink : new Color(0.5f, 0.48f, 0.45f);
                b.GetComponent<Button>().interactable = can && !chosen;
                y -= 160f;
            }
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

        public void SetEndPoints(int points) { endPoints.text = points > 0 ? $"+{points} depot points" : ""; }

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

        public void SetLeader(int hp, int max) { var s = new System.Text.StringBuilder("LEADER "); for (int i = 0; i < max; i++) s.Append(i < hp ? "■" : "□"); leaderHp.text = s.ToString(); }
        public void SetLevel(int level, float progress) { levelText.text = $"Level {level}"; levelFill.rectTransform.sizeDelta = new Vector2(960f * Mathf.Clamp01(progress), 8f); }
        public void Toast(string text, float seconds = 2.2f) { toast.text = text; toastLeft = seconds; }
        /// <summary>A red blink over the screen: the leader was hit.</summary>
        public void Flash() { flash.color = new Color(0.8f, 0.1f, 0.05f, 0.38f); }

        /// <summary>A little number rising over a point of the field: score, points, a kill.</summary>
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
            adLabel.text = dawn ? "Double score · watch an ad" : "Field repair · watch an ad";
            adNote.text = dawn ? "Rewarded video (mock): ×2 score for the depot." : "Rewarded video (mock): the leader is repaired once and the assault goes on.";
            endSheet.SetActive(true);
        }
        public void HideEnd() { endSheet.SetActive(false); }

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
            totals.text = $"{Depot.NightsFought} nights · {Depot.Total("kills")} vehicles · {Depot.Total("tigers")} Tigers · {Depot.Total("guns")} guns · {Depot.Total("infantry")} infantry\n{Depot.Total("dawns")} dawns · {Depot.Total("objectives")} objectives · {Medals.Count}/{Medals.All.Length} medals";
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
        public void SetAdNote(string s) { adNote.text = s; }

        void Update()
        {
            fpsAccum += Time.unscaledDeltaTime; fpsFrames++; fpsTimer += Time.unscaledDeltaTime;
            if (fpsTimer >= 0.5f) { fps.text = $"{fpsFrames / fpsAccum:0} fps"; fpsAccum = 0; fpsFrames = 0; fpsTimer = 0; }
            if (toastLeft > 0f) { toastLeft -= Time.unscaledDeltaTime; if (toastLeft <= 0f) toast.text = ""; }
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

        static Font DefaultFont() => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

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
            return img;
        }

        static GameObject MakeButton(Transform parent, string label, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize, System.Action onClick)
        {
            var go = new GameObject("Btn " + label, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>(); rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f); rt.anchoredPosition = pos; rt.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.06f, 0.6f);
            go.GetComponent<Button>().onClick.AddListener(() => { Sfx.Click(); onClick(); });
            var t = MakeText(go.transform, "Label", new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleCenter, fontSize, new Color(0.93f, 0.91f, 0.86f));
            t.GetComponent<RectTransform>().sizeDelta = size; t.text = label;
            return go;
        }
    }
}

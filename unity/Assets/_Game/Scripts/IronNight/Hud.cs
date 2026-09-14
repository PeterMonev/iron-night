using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IronNight
{
    /// <summary>
    /// The overlay: night clock, platoon count, level bar, formation buttons, toast line, the three-card level-up sheet
    /// and the end-of-assault sheet with the (mock) rewarded-ad button. Built in code with the legacy UI, English only.
    /// </summary>
    public class Hud : MonoBehaviour
    {
        public class Card { public string id, title, desc; }

        public System.Action<Formation> OnFormation;
        public System.Action OnAd, OnAgain, OnStart, OnDepot, OnBack, OnReserveAd;

        Text clock, count, fps, levelText, toast, endTitle, endEyebrow, stats, adLabel, adNote, leaderHp;
        Image levelFill; GameObject sheet, endSheet, adBtn, titleSheet, depotSheet, hudGroup, reserveBtn; Transform cardRoot, depotRows; Canvas canvas; Text depotPoints, titleStats, endPoints, reserveNote, bossName; Image bossFill; GameObject bossBar;
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
            var t = hg.transform;
            var ink = new Color(0.93f, 0.91f, 0.86f); var amber = new Color(0.95f, 0.66f, 0.23f); var dim = new Color(0.66f, 0.64f, 0.59f);
            MakeText(t, "ClockLabel", new Vector2(0, 1), new Vector2(60, -70), TextAnchor.UpperLeft, 30, dim).text = "NIGHT ASSAULT";
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

            var names = new[] { "WEDGE", "COLUMN", "LINE", "ECHELON" };
            var forms = new[] { Formation.Wedge, Formation.Column, Formation.Line, Formation.Echelon };
            for (int i = 0; i < 4; i++)
            {
                var f = forms[i]; int idx = i;
                var b = MakeButton(t, names[i], new Vector2(0.5f, 0f), new Vector2(-390 + i * 260, 120), new Vector2(244, 92), 34, () => { OnFormation?.Invoke(f); Highlight(idx); });
                formButtons[i] = b.GetComponent<Button>();
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
            MakeButton(endSheet.transform, "New assault", new Vector2(0.5f, 0.5f), new Vector2(0, -260), new Vector2(880, 130), 40, () => OnAgain?.Invoke());
            endSheet.SetActive(false);
            arrowSprite = ArrowSprite();
            var root = canvasGo.transform;
            endPoints = MakeText(endSheet.transform, "Points", new Vector2(0.5f, 0.5f), new Vector2(0, 120), TextAnchor.MiddleCenter, 44, amber);
            MakeButton(endSheet.transform, "Depot", new Vector2(0.5f, 0.5f), new Vector2(0, -400), new Vector2(880, 130), 40, () => OnDepot?.Invoke());

            // title sheet
            titleSheet = new GameObject("Title", typeof(RectTransform), typeof(Image)); titleSheet.transform.SetParent(root, false);
            Stretch(titleSheet); titleSheet.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.04f, 0.72f);
            MakeText(titleSheet.transform, "Eyebrow", new Vector2(0.5f, 0.5f), new Vector2(0, 660), TextAnchor.MiddleCenter, 34, dim).text = "WWII · NIGHT ASSAULT";
            var big = MakeText(titleSheet.transform, "Name", new Vector2(0.5f, 0.5f), new Vector2(0, 540), TextAnchor.MiddleCenter, 150, ink); big.text = "IRON NIGHT"; big.fontStyle = FontStyle.Bold; big.rectTransform.sizeDelta = new Vector2(1000, 200);
            MakeText(titleSheet.transform, "Tag", new Vector2(0.5f, 0.5f), new Vector2(0, 420), TextAnchor.MiddleCenter, 36, dim).text = "Lead a Sherman platoon through five minutes of darkness.";
            var start = MakeButton(titleSheet.transform, "Night assault", new Vector2(0.5f, 0.5f), new Vector2(0, 80), new Vector2(880, 150), 52, () => OnStart?.Invoke());
            start.GetComponent<Image>().color = amber; start.transform.Find("Label").GetComponent<Text>().color = new Color(0.1f, 0.08f, 0.05f);
            MakeButton(titleSheet.transform, "Depot", new Vector2(0.5f, 0.5f), new Vector2(0, -100), new Vector2(880, 130), 40, () => OnDepot?.Invoke());
            reserveBtn = MakeButton(titleSheet.transform, "4th tank tonight · watch an ad", new Vector2(0.5f, 0.5f), new Vector2(0, -260), new Vector2(880, 110), 34, () => OnReserveAd?.Invoke());
            reserveNote = MakeText(titleSheet.transform, "ReserveNote", new Vector2(0.5f, 0.5f), new Vector2(0, -345), TextAnchor.MiddleCenter, 26, dim); reserveNote.text = "The platoon holds 3 tanks. A rewarded video opens a 4th slot for this night (mock).";
            titleStats = MakeText(titleSheet.transform, "Stats", new Vector2(0.5f, 0.5f), new Vector2(0, -480), TextAnchor.MiddleCenter, 32, dim); titleStats.rectTransform.sizeDelta = new Vector2(900, 200);
            MakeText(titleSheet.transform, "Credits", new Vector2(0.5f, 0f), new Vector2(0, 70), TextAnchor.MiddleCenter, 24, new Color(0.45f, 0.44f, 0.4f)).text = "Built with DINOv3 · TRELLIS 2 · Unity";
            titleSheet.SetActive(false);

            // depot sheet
            depotSheet = new GameObject("Depot", typeof(RectTransform), typeof(Image)); depotSheet.transform.SetParent(root, false);
            Stretch(depotSheet); depotSheet.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.04f, 0.9f);
            MakeText(depotSheet.transform, "Eyebrow", new Vector2(0.5f, 1f), new Vector2(0, -90), TextAnchor.MiddleCenter, 30, dim).text = "THE DEPOT";
            MakeText(depotSheet.transform, "Title", new Vector2(0.5f, 1f), new Vector2(0, -170), TextAnchor.MiddleCenter, 96, ink).text = "Upgrades";
            depotPoints = MakeText(depotSheet.transform, "Points", new Vector2(0.5f, 1f), new Vector2(0, -300), TextAnchor.MiddleCenter, 40, amber);
            var rows = new GameObject("Rows", typeof(RectTransform)); rows.transform.SetParent(depotSheet.transform, false);
            var rrt = rows.GetComponent<RectTransform>(); rrt.anchorMin = rrt.anchorMax = new Vector2(0.5f, 1f); rrt.pivot = new Vector2(0.5f, 1f); rrt.anchoredPosition = new Vector2(0, -400); rrt.sizeDelta = Vector2.zero;
            depotRows = rows.transform;
            MakeButton(depotSheet.transform, "Back", new Vector2(0.5f, 0f), new Vector2(0, 150), new Vector2(880, 130), 40, () => OnBack?.Invoke());
            depotSheet.SetActive(false);
        }

        public void ShowTitle(bool reserveGranted)
        {
            Depot.Load(); hudGroup.SetActive(false); endSheet.SetActive(false); depotSheet.SetActive(false);
            int m = Mathf.FloorToInt(Depot.BestTime / 60f), s = Mathf.FloorToInt(Depot.BestTime % 60f);
            titleStats.text = Depot.NightsFought == 0 ? "First night. Drag anywhere to drive; the turrets fire on their own." : $"{Depot.NightsFought} nights fought · best {Depot.BestKills} kills · longest {m}:{s:00}\n{Depot.Points} depot points";
            reserveBtn.SetActive(!reserveGranted); reserveNote.text = reserveGranted ? "Reserve tank granted: the platoon can grow to 4 tonight." : "The platoon holds 3 tanks. A rewarded video opens a 4th slot for this night (mock).";
            titleSheet.SetActive(true);
        }
        public void HideTitle() { titleSheet.SetActive(false); hudGroup.SetActive(true); }

        public void ShowDepot()
        {
            Depot.Load(); titleSheet.SetActive(false); endSheet.SetActive(false); depotSheet.SetActive(true); RefreshDepot();
        }

        void RefreshDepot()
        {
            depotPoints.text = $"{Depot.Points} points";
            foreach (Transform c in depotRows) Destroy(c.gameObject);
            for (int i = 0; i < Depot.Upgrades.Count; i++)
            {
                var u = Depot.Upgrades[i]; float y = -i * 250f;
                var row = MakeImage(depotRows, "Row " + u.id, new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(940, 230), new Color(0.08f, 0.09f, 0.1f, 0.96f)); row.rectTransform.pivot = new Vector2(0.5f, 1f);
                var title = MakeText(row.transform, "Title", new Vector2(0f, 1f), new Vector2(30, -20), TextAnchor.UpperLeft, 44, new Color(0.93f, 0.91f, 0.86f)); title.text = u.title; title.rectTransform.sizeDelta = new Vector2(600, 60);
                var pips = new System.Text.StringBuilder(); for (int k = 0; k < u.MaxLevel; k++) pips.Append(k < u.level ? "■" : "□");
                var lvl = MakeText(row.transform, "Level", new Vector2(1f, 1f), new Vector2(-30, -26), TextAnchor.UpperRight, 40, new Color(0.95f, 0.66f, 0.23f)); lvl.text = pips.ToString(); lvl.rectTransform.sizeDelta = new Vector2(300, 60);
                var desc = MakeText(row.transform, "Desc", new Vector2(0f, 1f), new Vector2(30, -80), TextAnchor.UpperLeft, 28, new Color(0.66f, 0.64f, 0.59f)); desc.text = u.desc; desc.rectTransform.sizeDelta = new Vector2(880, 80);
                bool maxed = u.level >= u.MaxLevel, can = !maxed && Depot.Points >= u.Cost;
                var b = MakeButton(row.transform, maxed ? "Maxed" : $"Upgrade · {u.Cost}", new Vector2(1f, 0f), new Vector2(-190, 50), new Vector2(340, 80), 32, () => { if (Depot.Buy(u)) RefreshDepot(); });
                b.GetComponent<Image>().color = can ? new Color(0.95f, 0.66f, 0.23f, 0.95f) : new Color(0.2f, 0.2f, 0.22f, 0.9f);
                b.transform.Find("Label").GetComponent<Text>().color = can ? new Color(0.1f, 0.08f, 0.05f) : new Color(0.55f, 0.53f, 0.5f);
                b.GetComponent<Button>().interactable = can;
            }
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
        public void Toast(string text) { toast.text = text; toastLeft = 2.2f; }
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
                b.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.1f, 0.96f);
                var title = b.transform.Find("Label").GetComponent<Text>();
                title.alignment = TextAnchor.UpperLeft; title.rectTransform.anchorMin = title.rectTransform.anchorMax = title.rectTransform.pivot = new Vector2(0f, 1f);
                title.rectTransform.anchoredPosition = new Vector2(30f, -22f); title.rectTransform.sizeDelta = new Vector2(840f, 70f);
                var desc = MakeText(b.transform, "Desc", new Vector2(0f, 1f), new Vector2(30f, -100f), TextAnchor.UpperLeft, 34, new Color(0.66f, 0.64f, 0.59f));
                desc.rectTransform.sizeDelta = new Vector2(840f, 130f); desc.text = card.desc;
            }
            sheet.SetActive(true);
        }

        public void ShowEnd(bool dawn, string statLine, bool adAvailable)
        {
            endEyebrow.text = dawn ? "05:00 · DAWN" : "PLATOON LEADER KNOCKED OUT";
            endTitle.text = dawn ? "You held the line" : "Assault over";
            stats.text = statLine;
            adBtn.SetActive(adAvailable);
            adLabel.text = dawn ? "Double score · watch an ad" : "Field repair · watch an ad";
            adNote.text = dawn ? "Rewarded video (mock): ×2 score for the depot." : "Rewarded video (mock): the leader is repaired once and the assault goes on.";
            endSheet.SetActive(true);
        }
        public void HideEnd() { endSheet.SetActive(false); }
        public void SetAdNote(string s) { adNote.text = s; }

        void Update()
        {
            fpsAccum += Time.unscaledDeltaTime; fpsFrames++; fpsTimer += Time.unscaledDeltaTime;
            if (fpsTimer >= 0.5f) { fps.text = $"{fpsFrames / fpsAccum:0} fps"; fpsAccum = 0; fpsFrames = 0; fpsTimer = 0; }
            if (toastLeft > 0f) { toastLeft -= Time.unscaledDeltaTime; if (toastLeft <= 0f) toast.text = ""; }
        }

        static Font DefaultFont() => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static Text MakeText(Transform parent, string name, Vector2 anchor, Vector2 offset, TextAnchor align, int size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>(); rt.anchorMin = rt.anchorMax = anchor; rt.pivot = anchor; rt.anchoredPosition = offset; rt.sizeDelta = new Vector2(900, 140);
            var t = go.GetComponent<Text>(); t.font = DefaultFont(); t.fontSize = size; t.alignment = align; t.color = color; t.raycastTarget = false;
            return t;
        }

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

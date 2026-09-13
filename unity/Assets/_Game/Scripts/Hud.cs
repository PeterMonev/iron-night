using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Lightswarm
{
    /// <summary>
    /// Overlay for the prototype: night clock, firefly count, level bar, FPS (the number the first week is about),
    /// four formation buttons along the bottom, and the three-card level-up sheet. Built in code with the legacy UI
    /// so no assets are needed.
    /// </summary>
    public class Hud : MonoBehaviour
    {
        public class Card { public string id, title, desc; }

        Text clock, count, fps, banner, levelText;
        Image levelFill;
        GameObject sheet; Transform cardRoot;
        float fpsAccum; int fpsFrames; float fpsTimer;

        public System.Action<Swarm.Formation> OnFormation;

        public void Build()
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1080, 2340); scaler.matchWidthOrHeight = 0.5f;

            if (!FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>())
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));

            var t = canvasGo.transform;
            clock = MakeText(t, "Clock", new Vector2(0, 1), new Vector2(60, -80), TextAnchor.UpperLeft, 64, new Color(0.95f, 0.92f, 0.84f));
            count = MakeText(t, "Count", new Vector2(1, 1), new Vector2(-60, -80), TextAnchor.UpperRight, 64, new Color(1f, 0.71f, 0.28f));
            fps = MakeText(t, "Fps", new Vector2(0.5f, 1), new Vector2(0, -80), TextAnchor.UpperCenter, 40, new Color(0.6f, 0.62f, 0.72f));
            levelText = MakeText(t, "Level", new Vector2(0.5f, 1), new Vector2(0, -150), TextAnchor.UpperCenter, 30, new Color(0.6f, 0.62f, 0.72f));
            banner = MakeText(t, "Banner", new Vector2(0.5f, 0.5f), new Vector2(0, 300), TextAnchor.MiddleCenter, 96, new Color(0.95f, 0.92f, 0.84f));
            banner.text = "";

            var barBg = MakeImage(t, "LevelBar", new Vector2(0.5f, 1), new Vector2(0, -196), new Vector2(960, 6), new Color(1f, 1f, 1f, 0.12f));
            levelFill = MakeImage(barBg.transform, "Fill", new Vector2(0, 0.5f), Vector2.zero, new Vector2(0, 6), new Color(1f, 0.71f, 0.28f));

            var names = new[] { "Ring", "Spear", "Cloud", "Spiral" };
            var forms = new[] { Swarm.Formation.Ring, Swarm.Formation.Spear, Swarm.Formation.Cloud, Swarm.Formation.Spiral };
            for (int i = 0; i < 4; i++)
            {
                var f = forms[i];
                MakeButton(t, names[i], new Vector2(0.5f, 0f), new Vector2(-390 + i * 260, 120), new Vector2(240, 96), 40, () => OnFormation?.Invoke(f));
            }

            // level-up sheet: dark veil, title, three cards
            sheet = new GameObject("LevelUp", typeof(RectTransform), typeof(Image));
            sheet.transform.SetParent(t, false);
            var srt = sheet.GetComponent<RectTransform>(); srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one; srt.offsetMin = srt.offsetMax = Vector2.zero;
            sheet.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.06f, 0.78f);
            MakeText(sheet.transform, "Title", new Vector2(0.5f, 0.5f), new Vector2(0, 520), TextAnchor.MiddleCenter, 96, new Color(0.95f, 0.92f, 0.84f)).text = "Choose";
            var cards = new GameObject("Cards", typeof(RectTransform)); cards.transform.SetParent(sheet.transform, false);
            var crt = cards.GetComponent<RectTransform>(); crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f); crt.sizeDelta = Vector2.zero;
            cardRoot = cards.transform;
            sheet.SetActive(false);
        }

        public void Set(float nightSeconds, int fireflies)
        {
            int m = Mathf.FloorToInt(nightSeconds / 60f), s = Mathf.FloorToInt(nightSeconds % 60f);
            clock.text = $"{m}:{s:00}";
            count.text = fireflies.ToString();
        }

        public void SetLevel(int level, float progress)
        {
            levelText.text = $"Level {level}";
            levelFill.rectTransform.sizeDelta = new Vector2(960f * Mathf.Clamp01(progress), 6f);
        }

        public void Banner(string text) { banner.text = text; }

        public void ShowCards(int level, List<Card> cards, System.Action<string> onPick)
        {
            foreach (Transform c in cardRoot) Destroy(c.gameObject);
            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                var b = MakeButton(cardRoot, card.title, new Vector2(0.5f, 0.5f), new Vector2(0, 260 - i * 270), new Vector2(880, 230), 56, () => { sheet.SetActive(false); onPick(card.id); });
                b.GetComponent<Image>().color = new Color(0.07f, 0.1f, 0.19f, 0.95f);
                // title along the top edge, description under it, both left-aligned inside the card
                var title = b.transform.Find("Label").GetComponent<Text>();
                title.alignment = TextAnchor.UpperLeft; title.rectTransform.anchorMin = title.rectTransform.anchorMax = title.rectTransform.pivot = new Vector2(0f, 1f);
                title.rectTransform.anchoredPosition = new Vector2(28f, -20f); title.rectTransform.sizeDelta = new Vector2(820f, 70f);
                var desc = MakeText(b.transform, "Desc", new Vector2(0f, 1f), new Vector2(28f, -96f), TextAnchor.UpperLeft, 34, new Color(0.66f, 0.65f, 0.56f));
                desc.rectTransform.sizeDelta = new Vector2(820f, 120f); desc.text = card.desc;
            }
            sheet.SetActive(true);
        }

        void Update()
        {
            fpsAccum += Time.unscaledDeltaTime; fpsFrames++; fpsTimer += Time.unscaledDeltaTime;
            if (fpsTimer >= 0.5f) { fps.text = $"{fpsFrames / fpsAccum:0} fps"; fpsAccum = 0; fpsFrames = 0; fpsTimer = 0; }
        }

        static Font DefaultFont() => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        static Text MakeText(Transform parent, string name, Vector2 anchor, Vector2 offset, TextAnchor align, int size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = anchor; rt.anchoredPosition = offset; rt.sizeDelta = new Vector2(900, 140);
            var t = go.GetComponent<Text>();
            t.font = DefaultFont(); t.fontSize = size; t.alignment = align; t.color = color; t.raycastTarget = false;
            return t;
        }

        static Image MakeImage(Transform parent, string name, Vector2 anchor, Vector2 offset, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = anchor; rt.anchoredPosition = offset; rt.sizeDelta = size;
            var img = go.GetComponent<Image>(); img.color = color; img.raycastTarget = false;
            return img;
        }

        static GameObject MakeButton(Transform parent, string label, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize, System.Action onClick)
        {
            var go = new GameObject("Btn " + label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f); rt.anchoredPosition = pos; rt.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.09f, 0.6f);
            go.GetComponent<Button>().onClick.AddListener(() => onClick());
            var t = MakeText(go.transform, "Label", new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleCenter, fontSize, new Color(0.95f, 0.92f, 0.84f));
            t.GetComponent<RectTransform>().sizeDelta = size; t.text = label;
            return go;
        }
    }
}

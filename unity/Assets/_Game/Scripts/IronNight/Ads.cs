using UnityEngine;
using UnityEngine.UI;

namespace IronNight
{
    /// <summary>
    /// Rewarded video ads: the player chooses to watch one and gets something for it; nothing is ever forced on them.
    /// On Android they come from AdMob (with the consent form Europe needs first); until AdMob is in, and on the desktop
    /// build, a stand-in plays for a moment and grants the reward, so every place that offers an ad already works. When no
    /// ad can be shown, a line says so and nothing is granted.
    /// </summary>
    public static class Ads
    {
        public static bool Playing { get; private set; }

        /// <summary>Plays a rewarded ad for a placement ("end", "reserve", "train", "crewxp", "gold"); onReward runs only if
        /// it was watched to the end.</summary>
        public static void Rewarded(string placement, System.Action onReward)
        {
            if (Playing) return;
            Playing = true;
            Runner.Get().Play(placement, ok => { Playing = false; if (ok) onReward?.Invoke(); else Runner.Get().Say("No ad to show right now · try again in a minute"); });
        }

        /// <summary>The stand-in's player, and the line for "no ad": its own canvas over everything, kept across scenes.</summary>
        class Runner : MonoBehaviour
        {
            static Runner instance;
            GameObject screen; Text count; Text note; float left, noteLeft; System.Action<bool> done;

            public static Runner Get()
            {
                if (instance != null) return instance;
                var go = new GameObject("Ads", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); DontDestroyOnLoad(go);
                var cv = go.GetComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 200;
                var sc = go.GetComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1080, 2340); sc.matchWidthOrHeight = 0.5f;
                instance = go.AddComponent<Runner>(); instance.Build(); return instance;
            }

            void Build()
            {
                var black = Hud.MakeImage(transform, "Screen", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000f, 4000f), new Color(0.01f, 0.01f, 0.015f, 1f)); black.raycastTarget = true; screen = black.gameObject;   // it takes the taps while it plays
                var ey = Hud.MakeText(screen.transform, "Eyebrow", new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), TextAnchor.MiddleCenter, 28, new Color(0.96f, 0.68f, 0.24f)); ey.text = "REWARDED VIDEO · TEST";
                count = Hud.MakeText(screen.transform, "Count", new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), TextAnchor.MiddleCenter, 90, new Color(0.93f, 0.91f, 0.86f));
                screen.SetActive(false);
                note = Hud.MakeText(transform, "Note", new Vector2(0.5f, 0.5f), new Vector2(0f, -560f), TextAnchor.MiddleCenter, 30, new Color(0.93f, 0.91f, 0.86f)); note.rectTransform.sizeDelta = new Vector2(980f, 60f);
                var shadow = note.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(0f, 0f, 0f, 0.9f); shadow.effectDistance = new Vector2(0f, -3f);
            }

            public void Play(string placement, System.Action<bool> onDone) { done = onDone; left = 1.6f; screen.SetActive(true); }
            public void Say(string line) { note.text = line; noteLeft = 2.6f; }

            void Update()
            {
                if (noteLeft > 0f) { noteLeft -= Time.unscaledDeltaTime; if (noteLeft <= 0f) note.text = ""; }
                if (done == null) return;
                left -= Time.unscaledDeltaTime; count.text = Mathf.CeilToInt(Mathf.Max(0f, left)).ToString();
                if (left > 0f) return;
                screen.SetActive(false); var d = done; done = null; d(true);
            }
        }
    }
}

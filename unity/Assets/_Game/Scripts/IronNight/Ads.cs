using UnityEngine;
using UnityEngine.UI;
#if UNITY_ANDROID && !UNITY_EDITOR
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
#endif

namespace IronNight
{
    /// <summary>
    /// Rewarded video ads: the player chooses to watch one and gets something for it; nothing is ever forced on them.
    /// On Android they come from AdMob, after the consent form Europe needs (Google's UMP); on the desktop build a
    /// stand-in plays for a moment and grants the reward, so every place that offers an ad can be tried. When no ad can
    /// be shown, a line says so and nothing is granted.
    /// </summary>
    public static class Ads
    {
        // Google's test ad unit, until the game's own is made in AdMob: then its id goes here, and the app id into
        // Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings (IronNight.EditorTools.AdsSetup puts the test one there)
        public const string RewardedUnit = "ca-app-pub-3940256099942544/5224354917";
        public static bool Playing { get; private set; }

        /// <summary>Asks for consent where the law wants it, then starts AdMob and loads the first ad. Once, at start.</summary>
        public static void Start() => Runner.Get().Begin();

        /// <summary>Plays a rewarded ad for a placement ("end", "reserve", "train", "crewxp", "gold"); onReward runs only if
        /// it was watched to the end.</summary>
        public static void Rewarded(string placement, System.Action onReward)
        {
            if (Playing) return;
            Playing = true;
            Runner.Get().Play(placement, ok => { Playing = false; if (ok) onReward?.Invoke(); else Runner.Get().Say("No ad to show right now · try again in a minute"); });
        }

        /// <summary>A line over everything for a few seconds ("no ad", a season over).</summary>
        public static void Say(string line) => Runner.Get().Say(line);

        /// <summary>True where the player must be able to change their ad consent later (the EU): the settings show a button.</summary>
        public static bool PrivacyChoices
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            get { return ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required; }
#else
            get { return false; }
#endif
        }
        /// <summary>Google's form for changing the ad consent.</summary>
        public static void ShowPrivacyChoices()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            ConsentForm.ShowPrivacyOptionsForm(e => { if (e != null) Debug.LogWarning("Iron Night: privacy form: " + e.Message); });
#endif
        }

        /// <summary>The player behind Ads: AdMob on the phone, the stand-in elsewhere, and the line for "no ad". Its own
        /// canvas over everything, kept across scenes.</summary>
        class Runner : MonoBehaviour
        {
            static Runner instance;
            GameObject screen; Text count; Text note; float left, noteLeft; System.Action<bool> done;
#if UNITY_ANDROID && !UNITY_EDITOR
            RewardedAd rewarded; bool begun, started, loading; float retryAt;
#endif

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

            public void Begin()
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                if (begun) return; begun = true;
                // consent first: the form shows only where the law wants it, and only until it has been answered
                ConsentInformation.Update(new ConsentRequestParameters(), updateError =>
                {
                    if (updateError != null) Debug.LogWarning("Iron Night: consent: " + updateError.Message);
                    ConsentForm.LoadAndShowConsentFormIfRequired(formError => { if (formError != null) Debug.LogWarning("Iron Night: consent form: " + formError.Message); if (ConsentInformation.CanRequestAds()) StartAdMob(); });
                });
                if (ConsentInformation.CanRequestAds()) StartAdMob();   // answered in an earlier session: no need to wait
#endif
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            void StartAdMob()
            {
                if (started) return; started = true;
                MobileAds.RaiseAdEventsOnUnityMainThread = true;
                MobileAds.Initialize(_ => Load());
            }

            void Load()
            {
                if (!started || loading || rewarded != null) return; loading = true;
                RewardedAd.Load(RewardedUnit, new AdRequest(), (ad, error) =>
                {
                    loading = false;
                    if (error != null || ad == null) { retryAt = Time.unscaledTime + 30f; Debug.LogWarning("Iron Night: no rewarded ad loaded: " + error); return; }
                    rewarded = ad;
                });
            }
#endif

            public void Play(string placement, System.Action<bool> onDone)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                if (rewarded == null || !rewarded.CanShowAd()) { onDone(false); Load(); return; }
                var ad = rewarded; rewarded = null; bool earned = false;
                ad.OnAdFullScreenContentClosed += () => { ad.Destroy(); onDone(earned); Load(); };
                ad.OnAdFullScreenContentFailed += e => { ad.Destroy(); onDone(false); Load(); };
                ad.Show(r => earned = true);
#else
                done = onDone; left = 1.6f; screen.SetActive(true);
#endif
            }
            public void Say(string line) { note.text = line; noteLeft = 2.6f; }

            void Update()
            {
                if (noteLeft > 0f) { noteLeft -= Time.unscaledDeltaTime; if (noteLeft <= 0f) note.text = ""; }
#if UNITY_ANDROID && !UNITY_EDITOR
                if (started && rewarded == null && !loading && Time.unscaledTime > retryAt) Load();   // a failed load is tried again half a minute later
#endif
                if (done == null) return;
                left -= Time.unscaledDeltaTime; count.text = Mathf.CeilToInt(Mathf.Max(0f, left)).ToString();
                if (left > 0f) return;
                screen.SetActive(false); var d = done; done = null; d(true);
            }
        }
    }
}

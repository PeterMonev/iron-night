using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace IronNight.EditorTools
{
    /// <summary>
    /// The AdMob app id in the Google Mobile Ads settings (Assets/GoogleMobileAds/Resources): Google's test app until the
    /// game's own AdMob app exists, then that one. The Android build runs it first; by hand:
    /// Unity.exe -batchmode -projectPath ... -executeMethod IronNight.EditorTools.AdsSetup.Setup -quit
    /// </summary>
    public static class AdsSetup
    {
        public const string AndroidAppId = "ca-app-pub-3940256099942544~3347511713";   // Google's test app: its ads are safe to tap

        [MenuItem("Iron Night/Ads setup")]
        public static void Setup()
        {
            var t = System.Type.GetType("GoogleMobileAds.Editor.GoogleMobileAdsSettings, GoogleMobileAds.Editor");
            if (t == null) { Debug.LogError("Iron Night: the Google Mobile Ads plugin is not in the project"); return; }
            var settings = t.GetMethod("LoadInstance", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public).Invoke(null, null) as ScriptableObject;
            var id = t.GetProperty("GoogleMobileAdsAndroidAppId");
            if ((string)id.GetValue(settings) == AndroidAppId) return;
            id.SetValue(settings, AndroidAppId); EditorUtility.SetDirty(settings); AssetDatabase.SaveAssets();
            Debug.Log("Iron Night: AdMob app id set to " + AndroidAppId);
        }
    }
}

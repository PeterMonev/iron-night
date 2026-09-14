using UnityEditor;
using UnityEngine;

namespace IronNight.EditorTools
{
    /// <summary>
    /// Headless builds: Unity.exe -batchmode -projectPath ... -executeMethod IronNight.EditorTools.BuildTools.Windows -quit
    /// (or .Android). Output goes to ../builds next to the project; "-out <folder>" puts a Windows build elsewhere
    /// (a test copy while the real one is being played).
    /// </summary>
    public static class BuildTools
    {
        static string[] Scenes => new[] { "Assets/_Game/Scenes/Battle.unity" };

        static string Arg(string key, string fallback) { var a = System.Environment.GetCommandLineArgs(); for (int i = 0; i + 1 < a.Length; i++) if (a[i] == key) return a[i + 1]; return fallback; }

        [MenuItem("Iron Night/Build Windows")]
        public static void Windows()
        {
            var report = BuildPipeline.BuildPlayer(Scenes, Arg("-out", "../builds/win") + "/IronNight.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
            Debug.Log($"Iron Night: Windows build {report.summary.result} ({report.summary.totalErrors} errors)");
        }

        [MenuItem("Iron Night/Build Android APK")]
        public static void Android()
        {
            EditorUserBuildSettings.buildAppBundle = false;
            var report = BuildPipeline.BuildPlayer(Scenes, "../builds/android/IronNight.apk", BuildTarget.Android, BuildOptions.None);
            Debug.Log($"Iron Night: Android build {report.summary.result} ({report.summary.totalErrors} errors)");
        }
    }
}

using UnityEditor;
using UnityEngine;

namespace IronNight.EditorTools
{
    /// <summary>
    /// Headless builds: Unity.exe -batchmode -projectPath ... -executeMethod IronNight.EditorTools.BuildTools.Windows -quit
    /// (or .Android). Output goes to ../builds next to the project.
    /// </summary>
    public static class BuildTools
    {
        static string[] Scenes => new[] { "Assets/_Game/Scenes/Battle.unity" };

        [MenuItem("Iron Night/Build Windows")]
        public static void Windows()
        {
            var report = BuildPipeline.BuildPlayer(Scenes, "../builds/win/IronNight.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
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

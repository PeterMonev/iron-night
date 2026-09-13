using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Lightswarm.EditorTools
{
    /// <summary>
    /// One-shot project configuration: phone player settings and the Grove scene with the bootstrap object.
    /// Runs from the menu (Lightswarm > Setup Project) or headless: Unity.exe -batchmode -executeMethod Lightswarm.EditorTools.ProjectSetup.Setup -quit
    /// </summary>
    public static class ProjectSetup
    {
        const string ScenePath = "Assets/_Game/Scenes/Grove.unity";

        [MenuItem("Lightswarm/Setup Project")]
        public static void Setup()
        {
            PlayerSettings.companyName = "Monev";
            PlayerSettings.productName = "Lightswarm";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.monev.lightswarm");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;

            CreateMaterials();
            CreateScene();
            AssetDatabase.SaveAssets();
            Debug.Log("Lightswarm: project setup complete.");
        }

        // Materials created in code at runtime lose their shader in a build (nothing references it, so it is stripped);
        // a material asset under Resources keeps the shader in the build and is loaded by name.
        static void CreateMaterials()
        {
            const string path = "Assets/_Game/Resources/SpriteUnlit.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
            Directory.CreateDirectory("Assets/_Game/Resources");
            AssetDatabase.CreateAsset(new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")), path);
            AssetDatabase.CreateAsset(new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default")), "Assets/_Game/Resources/SpriteLit.mat");
            // the volume profile must be an asset too, or the Bloom shader is stripped from the build
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, "Assets/_Game/Resources/NightProfile.asset");
            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(1.1f); bloom.threshold.Override(0.85f); bloom.scatter.Override(0.65f);
            AssetDatabase.AddObjectToAsset(bloom, profile);
            EditorUtility.SetDirty(profile);
        }

        static void CreateScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera", typeof(Camera)) { tag = "MainCamera" };
            var cam = camGo.GetComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = 8f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.03f, 0.045f, 0.09f);
            camGo.transform.position = new Vector3(0f, 0f, -10f);

            new GameObject("Game", typeof(GameBootstrap));

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }
    }
}

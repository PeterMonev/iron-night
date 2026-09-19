using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace IronNight.EditorTools
{
    /// <summary>
    /// One-shot project configuration for the 3D game: phone player settings, a URP 3D renderer with shadows (the 2D
    /// template only had the 2D renderer), the material and volume assets the code loads from Resources, import settings
    /// for the generated OBJ models, and the Battle scene. Menu: Iron Night > Setup Project, or headless:
    /// Unity.exe -batchmode -projectPath ... -executeMethod IronNight.EditorTools.IronNightSetup.Setup -quit
    /// </summary>
    public static class IronNightSetup
    {
        const string ScenePath = "Assets/_Game/Scenes/Battle.unity";
        const string Res = "Assets/_Game/Resources/";

        [MenuItem("Iron Night/Setup Project")]
        public static void Setup()
        {
            PlayerSettings.companyName = "Monev";
            PlayerSettings.productName = "Iron Night";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.monev.ironnight");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.colorSpace = ColorSpace.Linear;

            // the launcher icon: one 1024 px image, Unity scales it for every density
            var iconImp = AssetImporter.GetAtPath("Assets/_Game/Icon/icon.png") as TextureImporter;
            if (iconImp != null) { iconImp.textureType = TextureImporterType.Default; iconImp.textureCompression = TextureImporterCompression.Uncompressed; iconImp.mipmapEnabled = false; iconImp.SaveAndReimport(); }
            foreach (var n in new[] { "icon_fg", "icon_bg" }) { var imp = AssetImporter.GetAtPath("Assets/_Game/Icon/" + n + ".png") as TextureImporter; if (imp != null) { imp.textureType = TextureImporterType.Default; imp.textureCompression = TextureImporterCompression.Uncompressed; imp.mipmapEnabled = false; imp.alphaIsTransparency = true; imp.SaveAndReimport(); } }
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Icon/icon.png");
            var iconFg = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Icon/icon_fg.png"); var iconBg = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Icon/icon_bg.png");
            if (icon != null)
            {
                PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
                // Android: the adaptive icon (background + foreground), the round and the legacy ones, every size from the same art
                foreach (var kind in PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.Android))
                {
                    var icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
                    foreach (var pi in icons) { if (pi.maxLayerCount >= 2 && iconFg != null && iconBg != null) pi.SetTextures(iconBg, iconFg); else pi.SetTexture(icon); }
                    PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, kind, icons);
                }
            }

            Directory.CreateDirectory(Res);
            SetupPipeline();
            CreateMaterials();
            ImportModels();
            CreateScene();
            AssetDatabase.SaveAssets();
            Debug.Log("Iron Night: project setup complete.");
        }

        // A Universal (3D) renderer with main-light soft shadows; assigned as the default pipeline and to every quality level.
        static void SetupPipeline()
        {
            Directory.CreateDirectory("Assets/_Game/Settings");
            const string rendererPath = "Assets/_Game/Settings/IronNightRenderer.asset", assetPath = "Assets/_Game/Settings/IronNightURP.asset";
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (renderer == null) { renderer = ScriptableObject.CreateInstance<UniversalRendererData>(); AssetDatabase.CreateAsset(renderer, rendererPath); }
            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
            if (asset == null) { asset = UniversalRenderPipelineAsset.Create(renderer); AssetDatabase.CreateAsset(asset, assetPath); }
            var so = new SerializedObject(asset);
            void Set(string field, System.Action<SerializedProperty> apply) { var p = so.FindProperty(field); if (p != null) apply(p); else Debug.LogWarning("URP asset: no field " + field); }
            Set("m_MainLightShadowsSupported", p => p.boolValue = true);
            Set("m_MainLightShadowmapResolution", p => p.intValue = 2048);
            Set("m_ShadowDistance", p => p.floatValue = 115f);
            Set("m_ShadowCascadeCount", p => p.intValue = 2);
            Set("m_SoftShadowsSupported", p => p.boolValue = true);
            Set("m_AdditionalLightsRenderingMode", p => p.intValue = 1);   // per pixel
            Set("m_AdditionalLightsPerObjectLimit", p => p.intValue = 8);
            Set("m_AdditionalLightShadowsSupported", p => p.boolValue = false);
            Set("m_SupportsHDR", p => p.boolValue = true);
            Set("m_MSAA", p => p.intValue = 2);
            Set("m_RenderScale", p => p.floatValue = 1f);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset); EditorUtility.SetDirty(renderer);
            GraphicsSettings.defaultRenderPipeline = asset;
            int level = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++) { QualitySettings.SetQualityLevel(i, false); QualitySettings.renderPipeline = asset; }
            QualitySettings.SetQualityLevel(level, false);
        }

        static Material MakeMaterial(string name, string shader)
        {
            string path = Res + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find(shader)); AssetDatabase.CreateAsset(m, path); }
            else m.shader = Shader.Find(shader);
            return m;
        }

        static void CreateMaterials()
        {
            var vehicle = MakeMaterial("VehicleLit", "Universal Render Pipeline/Lit");
            vehicle.SetFloat("_Smoothness", 0.1f); vehicle.SetFloat("_Metallic", 0f);   // matt paint: the moon put a flat blue sheen on every level deck at 0.28 vehicle.SetFloat("_Cull", (float)CullMode.Off); // generated meshes have holes: render the inside walls too
            // the same with a detail normal map: the keyword lives on the asset so the variant is in the build
            var vehicleN = MakeMaterial("VehicleLitN", "Universal Render Pipeline/Lit");
            vehicleN.SetFloat("_Smoothness", 0.1f); vehicleN.SetFloat("_Metallic", 0f); vehicleN.SetFloat("_Cull", (float)CullMode.Off); vehicleN.EnableKeyword("_NORMALMAP"); vehicleN.SetFloat("_BumpScale", 0.8f);
            vehicleN.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Resources/Models/sherman_n.png"));
            var barrel = MakeMaterial("BarrelLit", "Universal Render Pipeline/Lit");
            barrel.SetColor("_BaseColor", new Color(0.3f, 0.31f, 0.26f)); barrel.SetFloat("_Smoothness", 0.45f); barrel.SetFloat("_Metallic", 0.4f);
            var ground = MakeMaterial("GroundLit", "Universal Render Pipeline/Lit");
            ground.SetFloat("_Smoothness", 0.06f); ground.SetFloat("_Metallic", 0f);
            // additive and alpha-blended unlit quads for flashes, tracers, flares, beams and smoke
            var additive = MakeMaterial("Additive", "Universal Render Pipeline/Unlit"); Transparent(additive, true);
            var smoke = MakeMaterial("Smoke", "Universal Render Pipeline/Unlit"); Transparent(smoke, false);
            // lit, alpha-blended ground decals: lanes, yards, craters
            var decal = MakeMaterial("GroundDecal", "Universal Render Pipeline/Lit"); Transparent(decal, false); decal.SetFloat("_Smoothness", 0.05f); decal.SetFloat("_Metallic", 0f);
            Texture2D Tex(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Resources/Textures/" + name + ".png");
            // the decals and the hedges carry normal maps; the keyword on the asset keeps the shader variant in the build
            decal.EnableKeyword("_NORMALMAP"); decal.SetTexture("_BumpMap", Tex("lane_n"));
            var foliage = MakeMaterial("FoliageLit", "Universal Render Pipeline/Lit");
            foliage.SetFloat("_Smoothness", 0.08f); foliage.SetFloat("_Metallic", 0f); foliage.SetFloat("_Cull", (float)CullMode.Off); foliage.EnableKeyword("_NORMALMAP"); foliage.SetTexture("_BaseMap", Tex("hedge")); foliage.SetTexture("_BumpMap", Tex("hedge_n"));
            // leaf cards: alpha-cut clusters on both sides; the keyword on the asset keeps the cutout variant (and its shadow pass) in the build
            var leaves = MakeMaterial("FoliageCut", "Universal Render Pipeline/Lit");
            leaves.SetFloat("_AlphaClip", 1f); leaves.SetFloat("_Cutoff", 0.45f); leaves.EnableKeyword("_ALPHATEST_ON"); leaves.SetFloat("_Cull", (float)CullMode.Off); leaves.SetFloat("_Smoothness", 0.1f); leaves.SetFloat("_Metallic", 0f);
            leaves.SetTexture("_BaseMap", Tex("leaves")); leaves.SetOverrideTag("RenderType", "TransparentCutout"); leaves.renderQueue = (int)RenderQueue.AlphaTest;
            // the field: four photographic ground textures blended by vertex colour, in world space
            var field = MakeMaterial("Ground", "IronNight/Ground"); string[] set = { "plough", "pasture", "mown", "stubble" };
            for (int i = 0; i < 4; i++) { field.SetTexture("_Tex" + i, Tex("ground_" + set[i])); field.SetTexture("_Nrm" + i, Tex("ground_" + set[i] + "_n")); }
            field.SetTexture("_Variation", Tex("ground_variation")); field.SetFloat("_Tiling", 40f / 3f); field.SetFloat("_VariationTiling", 300f); field.SetFloat("_VariationStrength", 0.5f); field.SetFloat("_BumpScale", 1f); field.SetFloat("_Smoothness", 0.06f);
            foreach (var m in new[] { vehicle, vehicleN, barrel, ground, additive, smoke, decal, foliage, field, leaves }) EditorUtility.SetDirty(m);

            const string profilePath = Res + "BattleProfile.asset";
            var old = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath); if (old != null) AssetDatabase.DeleteAsset(profilePath);
            {
                var profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, profilePath);
                var bloom = profile.Add<Bloom>(true);
                bloom.intensity.Override(0.9f); bloom.threshold.Override(1.1f); bloom.scatter.Override(0.6f);
                AssetDatabase.AddObjectToAsset(bloom, profile);
                var grading = profile.Add<ColorAdjustments>(true);
                grading.postExposure.Override(0.6f); grading.contrast.Override(14f); grading.saturation.Override(10f);
                AssetDatabase.AddObjectToAsset(grading, profile);
                var tone = profile.Add<Tonemapping>(true); tone.mode.Override(TonemappingMode.ACES);
                AssetDatabase.AddObjectToAsset(tone, profile);
                EditorUtility.SetDirty(profile);
            }
        }

        // URP unlit shaders read their blend state from these properties; the editor GUI normally sets them, so it is done here
        static void Transparent(Material m, bool additive)
        {
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", additive ? 2f : 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f); m.SetFloat("_Cull", (float)CullMode.Off);
            m.SetOverrideTag("RenderType", "Transparent"); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            // straight alpha: with premultiply and preserve-specular the Lit shader keeps the specular of fully transparent texels, and a runtime
            // marking texture shows as a faint lit square
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON"); m.DisableKeyword("_ALPHAMODULATE_ON"); m.SetFloat("_BlendModePreserveSpecular", 0f);
            m.renderQueue = (int)RenderQueue.Transparent + (additive ? 10 : 0);
        }

        // the generated vehicles: smooth normals, no importer materials (the code assigns its own, textured per vehicle)
        static void ImportModels()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets/_Game/Resources/Models", "Assets/_Game/Resources/Props" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var imp = AssetImporter.GetAtPath(path) as ModelImporter; if (imp == null) continue;
                imp.materialImportMode = ModelImporterMaterialImportMode.None;
                imp.importNormals = System.Text.RegularExpressions.Regex.IsMatch(path, @"_m\d+\.obj$") ? ModelImporterNormals.Import : ModelImporterNormals.Calculate; imp.normalSmoothingAngle = 60f;   // an artist's model brings its own normals
                imp.meshCompression = ModelImporterMeshCompression.Medium; imp.isReadable = path.Contains("_turret");   // the turrets are read at runtime for the roof height (the commander stands on it)
                imp.SaveAndReimport();
            }
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/_Game/Resources/Models", "Assets/_Game/Resources/Textures", "Assets/_Game/Resources/Props" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var imp = AssetImporter.GetAtPath(path) as TextureImporter; if (imp == null) continue;
                imp.maxTextureSize = 2048; imp.mipmapEnabled = true; imp.wrapMode = TextureWrapMode.Repeat; imp.anisoLevel = 4;
                string file = System.IO.Path.GetFileNameWithoutExtension(path);
                imp.textureType = file.EndsWith("_n") ? TextureImporterType.NormalMap : TextureImporterType.Default;   // *_n.png are normal maps
                imp.sRGBTexture = file != "ground_variation";                                                      // a multiplier, not a colour
                if (file == "leaves") { imp.alphaIsTransparency = true; imp.wrapMode = TextureWrapMode.Clamp; }
                var android = imp.GetPlatformTextureSettings("Android"); android.overridden = true; android.maxTextureSize = 1024; android.format = TextureImporterFormat.ASTC_6x6; imp.SetPlatformTextureSettings(android);
                imp.SaveAndReimport();
            }
        }

        static void CreateScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Main Camera", typeof(Camera)) { tag = "MainCamera" };
            camGo.transform.position = new Vector3(0f, 30f, -27f);
            new GameObject("Game", typeof(IronNightBootstrap));
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }
    }
}

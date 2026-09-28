using UnityEditor;
using UnityEngine;

namespace IronNight.EditorTools
{
    /// <summary>
    /// The game made lighter for phones. The textures change for Android only (the Windows build keeps them as they
    /// were): pictures in the menus as large as they are ever drawn (portraits and cards 512, the rest 1024), the props'
    /// textures and every normal map in ASTC 8x8 (seen small from the battle's camera, the difference does not show).
    /// The sound effects (Vorbis, the battle's noises in mono, the wind, the rain and the far front in stereo) and the
    /// props' meshes (compressed high) change for both builds: an importer keeps those for every platform at once.
    /// Run once, or again after new assets:
    /// Unity.exe -batchmode -projectPath ... -executeMethod IronNight.EditorTools.MobileDiet.Apply -quit
    /// </summary>
    public static class MobileDiet
    {
        const TextureImporterFormat Astc6 = TextureImporterFormat.ASTC_6x6, Astc8 = TextureImporterFormat.ASTC_8x8;

        [MenuItem("Iron Night/Mobile Diet")]
        public static void Apply()
        {
            int n = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var path in Paths("t:Texture2D", "Assets/_Game/Resources/UI"))
                {
                    string file = System.IO.Path.GetFileNameWithoutExtension(path);
                    if (file == "rank_insignia") continue;   // a strip of eleven cells: shrunk, each would blur
                    bool small = file.StartsWith("ace_") || file.StartsWith("crew_") || file.StartsWith("cmd_") || file.StartsWith("card_");   // drawn a few hundred pixels at most
                    n += Android(path, small ? 512 : 1024, Astc6);
                }
                foreach (var path in Paths("t:Texture2D", "Assets/_Game/Resources/Props")) n += Android(path, 1024, Astc8);
                foreach (var path in Paths("t:Texture2D", "Assets/_Game/Resources/Textures", "Assets/_Game/Resources/Models"))
                    if (System.IO.Path.GetFileNameWithoutExtension(path).EndsWith("_n")) n += Android(path, 1024, Astc8);
                foreach (var path in Paths("t:AudioClip", "Assets/_Game/Resources/Audio"))
                {
                    if (path.Contains("/music_")) continue;   // the themes are Vorbis already, kept in memory for the two decks
                    var imp = AssetImporter.GetAtPath(path) as AudioImporter; if (imp == null) continue;
                    string file = System.IO.Path.GetFileNameWithoutExtension(path); bool ambient = file == "wind" || file == "rain" || file == "front";
                    var s = imp.defaultSampleSettings;
                    if (s.compressionFormat == AudioCompressionFormat.Vorbis && imp.forceToMono == !ambient) continue;
                    s.compressionFormat = AudioCompressionFormat.Vorbis; s.quality = 0.5f; s.loadType = AudioClipLoadType.DecompressOnLoad;   // decoded on load: played as quickly as before
                    imp.defaultSampleSettings = s; imp.forceToMono = !ambient; imp.SaveAndReimport(); n++;
                }
                foreach (var path in Paths("t:Model", "Assets/_Game/Resources/Props"))
                {
                    var imp = AssetImporter.GetAtPath(path) as ModelImporter; if (imp == null || imp.meshCompression == ModelImporterMeshCompression.High) continue;
                    imp.meshCompression = ModelImporterMeshCompression.High; imp.SaveAndReimport(); n++;
                }
            }
            finally { AssetDatabase.StopAssetEditing(); }
            AssetDatabase.SaveAssets();
            Debug.Log("Iron Night: mobile diet, " + n + " assets changed");
        }

        static string[] Paths(string filter, params string[] folders)
        {
            var guids = AssetDatabase.FindAssets(filter, folders); var paths = new string[guids.Length];
            for (int i = 0; i < guids.Length; i++) paths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
            return paths;
        }

        /// <summary>A texture's Android override: at most size, in format. 1 when it changed.</summary>
        static int Android(string path, int size, TextureImporterFormat format)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter; if (imp == null) return 0;
            var a = imp.GetPlatformTextureSettings("Android");
            if (a.overridden && a.maxTextureSize == size && a.format == format) return 0;
            a.overridden = true; a.maxTextureSize = size; a.format = format; imp.SetPlatformTextureSettings(a); imp.SaveAndReimport();
            return 1;
        }
    }
}

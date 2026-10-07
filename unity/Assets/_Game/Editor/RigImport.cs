using UnityEditor;

namespace IronNight.EditorTools
{
    /// <summary>
    /// The Mixamo-rigged crews (Resources/Rigs) come in as Humanoids, so any of the Mixamo motions (Resources/Rigs/Anims,
    /// downloaded once, without a skin) plays on any of them; the motions loop and keep their feet where they are, the
    /// materials are left out (the hangar puts each figure's own texture on), and the meshes stay readable for nobody.
    /// </summary>
    public class RigImport : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/Rigs/")) return;
            var mi = (ModelImporter)assetImporter;
            mi.animationType = ModelImporterAnimationType.Human;
            mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            mi.materialImportMode = ModelImporterMaterialImportMode.None;
            mi.importCameras = false; mi.importLights = false; mi.isReadable = false;
            mi.useFileUnits = true;
            mi.importAnimation = true;
        }

        void OnPreprocessAnimation()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/Rigs/Anims/")) return;
            var mi = (ModelImporter)assetImporter;
            var clips = mi.defaultClipAnimations;
            foreach (var c in clips)
            {
                c.loopTime = true; c.loopPose = true;
                c.lockRootRotation = true; c.lockRootHeightY = true; c.lockRootPositionXZ = true;   // in place: the hangar moves the walkers itself
                c.keepOriginalOrientation = true; c.keepOriginalPositionY = true; c.keepOriginalPositionXZ = true;
            }
            mi.clipAnimations = clips;
        }
    }
}

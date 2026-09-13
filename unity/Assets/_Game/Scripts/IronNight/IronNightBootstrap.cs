using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace IronNight
{
    /// <summary>
    /// The only component the Battle scene needs. On Play it configures the phone camera and post-processing, then builds
    /// the HUD, the thumb stick, the effects and the battle itself. No prefabs; the meshes and materials come from Resources.
    /// </summary>
    public class IronNightBootstrap : MonoBehaviour
    {
        void Awake()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            Application.runInBackground = true;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            var cam = Camera.main;
            if (cam == null) cam = new GameObject("Main Camera", typeof(Camera)) { tag = "MainCamera" }.GetComponent<Camera>();
            cam.orthographic = false; cam.fieldOfView = 50f; cam.nearClipPlane = 0.5f; cam.farClipPlane = 320f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.02f, 0.03f, 0.05f);
            var camData = cam.GetUniversalAdditionalCameraData();
            camData.renderPostProcessing = true; camData.renderShadows = true;

            // bloom so muzzle flashes, tracers and fires glow
            var volume = new GameObject("Post Processing").AddComponent<Volume>();
            volume.isGlobal = true; volume.profile = Resources.Load<VolumeProfile>("BattleProfile");

            var hud = new GameObject("Hud").AddComponent<Hud>(); hud.Build();
            var stick = new GameObject("Stick").AddComponent<TouchStick>(); stick.Build(hud.Canvas);
            var fx = new GameObject("Fx").AddComponent<Fx>(); fx.Build(cam);
            var battle = new GameObject("Battle").AddComponent<Battle>(); battle.Build(cam, hud, stick, fx);
        }
    }
}

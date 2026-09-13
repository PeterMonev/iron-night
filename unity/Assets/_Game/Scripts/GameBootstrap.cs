using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Lightswarm
{
    /// <summary>
    /// The only component the scene needs. On Play it configures the camera for a portrait phone, the night
    /// lighting and bloom, then builds the forest, the joystick, the HUD and the swarm. No prefabs, no art files.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        // Command-line switches for isolating problems in builds: --nolight --noforest --nobloom
        static bool Has(string flag) { foreach (var a in System.Environment.GetCommandLineArgs()) if (a == flag) return true; return false; }

        void Awake()
        {
            // 60 fps on phones; uncapped in --stress so the frame rate says how much headroom there is
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = Has("--stress") ? -1 : 60;
            Application.runInBackground = true;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            var cam = Camera.main;
            if (cam == null) cam = new GameObject("Main Camera", typeof(Camera)) { tag = "MainCamera" }.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.015f, 0.02f, 0.05f);
            cam.transform.position = new Vector3(0f, 0f, -10f);
            var camData = cam.GetUniversalAdditionalCameraData();
            camData.renderPostProcessing = !Has("--nobloom");

            // bloom makes the fireflies glow beyond their sprites
            var volume = new GameObject("Post Processing").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.profile = Resources.Load<VolumeProfile>("NightProfile");

            if (!Has("--noforest")) new GameObject("Forest").AddComponent<ForestBackground>().Build(cam);
            var night = new GameObject("Night").AddComponent<DarknessMask>();
            night.Build();

            var joystick = new GameObject("Joystick").AddComponent<FloatingJoystick>();
            joystick.Build(cam);

            var hud = new GameObject("Hud").AddComponent<Hud>();
            hud.Build();

            var swarm = new GameObject("Swarm").AddComponent<Swarm>();
            if (Has("--stress")) { swarm.startFireflies = 500; swarm.stress = true; }
            swarm.Build(cam, joystick, hud);
            hud.OnFormation = f => swarm.formation = f;
            if (!Has("--nolight")) swarm.night = night; else night.gameObject.SetActive(false);
        }
    }
}

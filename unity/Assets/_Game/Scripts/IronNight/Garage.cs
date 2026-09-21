using UnityEngine;

namespace IronNight
{
    /// <summary>
    /// The depot's showroom: the chosen tank on a turntable under a hangar lamp, the commander in the hatch, rendered by
    /// its own camera into a texture the garage tab shows. Lives far below the field so nothing else sees it. A drag on
    /// the picture turns the tank; left alone it turns slowly on its own.
    /// </summary>
    public class Garage : MonoBehaviour
    {
        public RenderTexture Texture { get; private set; }
        public RenderTexture TitleTexture { get; private set; }   // the whole screen behind the menu
        Camera titleCam; bool titleOn;
        Camera cam; Transform stage; Vehicle shown; string shownId; float spin = 35f, spinVel; Light lamp, fill;
        static readonly Vector3 Home = new Vector3(0f, -600f, 0f);

        public static Garage Build()
        {
            var go = new GameObject("Garage"); go.transform.position = Home; var g = go.AddComponent<Garage>();
            g.Texture = new RenderTexture(1024, 768, 24) { antiAliasing = 2 };
            // the hangar: a concrete floor, a back wall, one warm lamp from above and a cold fill from the door
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane); Destroy(floor.GetComponent<Collider>()); floor.transform.SetParent(go.transform, false); floor.transform.localScale = new Vector3(4f, 1f, 4f);
            var fm = new Material(Resources.Load<Material>("GroundLit")); fm.SetTexture("_BaseMap", Resources.Load<Texture2D>("Textures/yard")); fm.SetTextureScale("_BaseMap", new Vector2(3f, 3f)); fm.SetColor("_BaseColor", new Color(0.55f, 0.55f, 0.58f)); fm.SetFloat("_Smoothness", 0.35f);
            floor.GetComponent<Renderer>().sharedMaterial = fm;
            var wall = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(wall.GetComponent<Collider>()); wall.transform.SetParent(go.transform, false); wall.transform.localPosition = new Vector3(0f, 6f, 14f); wall.transform.localScale = new Vector3(60f, 14f, 1f);
            var wm = new Material(Resources.Load<Material>("GroundLit")); wm.SetColor("_BaseColor", new Color(0.16f, 0.17f, 0.19f)); wm.SetFloat("_Smoothness", 0.1f); wall.GetComponent<Renderer>().sharedMaterial = wm;
            g.stage = new GameObject("Turntable").transform; g.stage.SetParent(go.transform, false);
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(disc.GetComponent<Collider>()); disc.transform.SetParent(go.transform, false); disc.transform.localPosition = new Vector3(0f, 0.03f, 0f); disc.transform.localScale = new Vector3(9f, 0.03f, 9f);
            var dm = new Material(Resources.Load<Material>("BarrelLit")); dm.SetColor("_BaseColor", new Color(0.22f, 0.22f, 0.24f)); dm.SetFloat("_Smoothness", 0.5f); dm.SetFloat("_Metallic", 0.6f); disc.GetComponent<Renderer>().sharedMaterial = dm;
            var lampGo = new GameObject("Lamp"); lampGo.transform.SetParent(go.transform, false); lampGo.transform.localPosition = new Vector3(6f, 6f, -7f); lampGo.transform.LookAt(go.transform.position + new Vector3(0f, 1.2f, 0f));
            g.lamp = lampGo.AddComponent<Light>(); g.lamp.type = LightType.Spot; g.lamp.spotAngle = 60f; g.lamp.range = 30f; g.lamp.intensity = 34f; g.lamp.color = new Color(1f, 0.96f, 0.9f); g.lamp.shadows = LightShadows.Soft;
            var fillGo = new GameObject("Fill"); fillGo.transform.SetParent(go.transform, false); fillGo.transform.localPosition = new Vector3(-8f, 4f, -8f);
            g.fill = fillGo.AddComponent<Light>(); g.fill.type = LightType.Point; g.fill.range = 30f; g.fill.intensity = 16f; g.fill.color = new Color(0.8f, 0.85f, 1f); g.fill.shadows = LightShadows.None;
            var backGo = new GameObject("Back"); backGo.transform.SetParent(go.transform, false); backGo.transform.localPosition = new Vector3(-6f, 5f, 9f);
            var back = backGo.AddComponent<Light>(); back.type = LightType.Point; back.range = 25f; back.intensity = 10f; back.color = new Color(1f, 0.95f, 0.85f); back.shadows = LightShadows.None;
            // the menu's camera: low, from the front-left, the tank filling the middle of a portrait screen; light shafts from the roof for it
            g.TitleTexture = new RenderTexture(Mathf.Max(480, Screen.width * 2 / 3), Mathf.Max(800, Screen.height * 2 / 3), 24) { antiAliasing = 2 };
            var tcGo = new GameObject("TitleCamera"); tcGo.transform.SetParent(go.transform, false); tcGo.transform.localPosition = new Vector3(-9.6f, 3.4f, -15.2f); tcGo.transform.LookAt(go.transform.position + new Vector3(0.2f, 1.05f, 0.3f));
            g.titleCam = tcGo.AddComponent<Camera>(); g.titleCam.targetTexture = g.TitleTexture; g.titleCam.fieldOfView = 40f; g.titleCam.nearClipPlane = 0.3f; g.titleCam.farClipPlane = 60f; g.titleCam.clearFlags = CameraClearFlags.SolidColor; g.titleCam.backgroundColor = new Color(0.03f, 0.03f, 0.04f); g.titleCam.enabled = false;
            var tdata = tcGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(); tdata.renderShadows = true; tdata.renderPostProcessing = false;
            for (int i = 0; i < 3; i++)
            {
                var shaft = new GameObject("Shaft").AddComponent<LightShaft>(); shaft.transform.SetParent(go.transform, false); shaft.facing = g.titleCam; shaft.length = 15f; shaft.width0 = 1.2f; shaft.width1 = 6.5f;
                shaft.Set(go.transform.position + new Vector3(-3f + i * 3.2f, 10.5f, 4f + i * 0.5f), new Vector3(0.28f, -1f, -0.35f));
            }
            var camGo = new GameObject("GarageCamera"); camGo.transform.SetParent(go.transform, false); camGo.transform.localPosition = new Vector3(0f, 3.6f, -9f); camGo.transform.LookAt(go.transform.position + new Vector3(0f, 1.2f, 0f));
            g.cam = camGo.AddComponent<Camera>(); g.cam.targetTexture = g.Texture; g.cam.fieldOfView = 34f; g.cam.nearClipPlane = 0.3f; g.cam.farClipPlane = 60f; g.cam.clearFlags = CameraClearFlags.SolidColor; g.cam.backgroundColor = new Color(0.05f, 0.05f, 0.06f); g.cam.enabled = false;
            var data = camGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(); data.renderShadows = true; data.renderPostProcessing = false;
            return g;
        }

        /// <summary>Puts the tank of that spec on the turntable (the last one goes); painted as the platoon is.</summary>
        public void Show(VehicleSpec spec)
        {
            if (spec == null || (shown != null && shownId == spec.id)) return;
            if (shown != null) Destroy(shown.gameObject);
            shown = Vehicle.Create(spec, true, Home, 0f); shown.transform.SetParent(stage, true); shownId = spec.id;
            shown.turretYaw = 0.35f; shown.Apply(); shown.enabled = false;
        }

        public void SetActive(bool on) { cam.enabled = on; gameObject.SetActive(on || titleOn); }
        /// <summary>The menu backdrop: the tank turning slowly under the roof lights.</summary>
        public void SetTitle(bool on) { titleOn = on; titleCam.enabled = on; if (on) gameObject.SetActive(true); else if (!cam.enabled) gameObject.SetActive(false); }
        public void Drag(float dx) { spinVel = dx * 0.35f; }

        void Update()
        {
            if (!cam.enabled && !titleOn) return;
            spinVel = Mathf.MoveTowards(spinVel, titleOn && !cam.enabled ? 5f : 12f, Time.unscaledDeltaTime * 30f); spin += spinVel * Time.unscaledDeltaTime;
            stage.localRotation = Quaternion.Euler(0f, spin, 0f);
            lamp.intensity = 34f + Mathf.Sin(Time.unscaledTime * 9f) * 0.8f;   // the hangar lamp hums
        }
    }
}

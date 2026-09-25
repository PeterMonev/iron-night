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

        Transform parked; Vehicle parkedTank;
        Camera portraitCam; RenderTexture portraitRt; readonly System.Collections.Generic.Dictionary<string, Texture2D> portraits = new System.Collections.Generic.Dictionary<string, Texture2D>();

        /// <summary>A picture of the tank for the garage list: put on the turntable and photographed by the portrait camera; kept for the session.</summary>
        public Texture2D Portrait(VehicleSpec spec)
        {
            if (spec == null) return null; if (portraits.TryGetValue(spec.id, out var have)) return have;
            if (portraitCam == null)
            {
                portraitRt = new RenderTexture(600, 340, 24) { antiAliasing = 2 };
                var pc = new GameObject("PortraitCamera"); pc.transform.SetParent(transform, false); pc.transform.localPosition = new Vector3(-5.6f, 2.1f, -6.9f); pc.transform.LookAt(transform.position + new Vector3(0.2f, 1.1f, 0.2f));
                portraitCam = pc.AddComponent<Camera>(); portraitCam.targetTexture = portraitRt; portraitCam.fieldOfView = 34f; portraitCam.nearClipPlane = 0.3f; portraitCam.farClipPlane = 60f; portraitCam.clearFlags = CameraClearFlags.SolidColor; portraitCam.backgroundColor = new Color(0.03f, 0.03f, 0.04f); portraitCam.enabled = false;
                var d = pc.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(); d.renderShadows = true; d.renderPostProcessing = true; d.volumeLayerMask = 1 << HangarLayer;
            }
            var before = shown != null ? shown.spec : null; bool wasActive = gameObject.activeSelf; gameObject.SetActive(true);
            var spinBefore = stage.localRotation; stage.localRotation = Quaternion.Euler(0f, 28f, 0f);
            Show(spec); if (shown != null) shown.Apply();
            portraitCam.Render();
            var tex = new Texture2D(portraitRt.width, portraitRt.height, TextureFormat.RGB24, false); var prev = RenderTexture.active; RenderTexture.active = portraitRt; tex.ReadPixels(new Rect(0, 0, portraitRt.width, portraitRt.height), 0, 0); RenderTexture.active = prev;
            var px = tex.GetPixels(); for (int i = 0; i < px.Length; i++) px[i] = new Color(Mathf.Min(1f, px[i].r * 1.45f), Mathf.Min(1f, px[i].g * 1.45f), Mathf.Min(1f, px[i].b * 1.45f), 1f); tex.SetPixels(px); tex.Apply();   // a little exposure: the list is smaller and darker than the hangar
            portraits[spec.id] = tex; stage.localRotation = spinBefore;
            if (before != null) Show(before); if (!wasActive) gameObject.SetActive(false);
            return tex;
        }
        static Material Surface(string tex, Vector2 tiling, Color tint, float smooth)
        {
            var m = new Material(Resources.Load<Material>("VehicleLitN")); m.SetTexture("_BaseMap", Resources.Load<Texture2D>("Textures/" + tex)); m.SetTexture("_BumpMap", Resources.Load<Texture2D>("Textures/" + tex + "_n"));
            m.SetTextureScale("_BaseMap", tiling); m.SetColor("_BaseColor", tint); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Cull", 2f); return m;
        }
        static void Wall(Transform parent, Vector3 pos, Quaternion rot, Vector3 scale, Material m)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(q.GetComponent<Collider>()); q.transform.SetParent(parent, false); q.transform.localPosition = pos; q.transform.localRotation = rot; q.transform.localScale = scale; q.GetComponent<Renderer>().sharedMaterial = m;
        }
        static void Prop(Transform parent, string mesh, Vector3 pos, float yaw)
        {
            var pf = Resources.Load<GameObject>("Props/" + mesh); if (pf == null) return;
            var p = Instantiate(pf, parent); p.transform.localPosition = pos; p.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var m = new Material(Resources.Load<Material>("VehicleLit")); m.SetTexture("_BaseMap", Resources.Load<Texture2D>("Props/" + mesh + "_tex")); m.SetFloat("_Smoothness", 0.15f); m.SetFloat("_Cull", 0f);
            foreach (var r in p.GetComponentsInChildren<Renderer>()) { r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
        }

        const int HangarLayer = 30;   // the hangar's own volume layer, so the field camera never sees this grading

        public static Garage Build()
        {
            var go = new GameObject("Garage"); go.transform.position = Home; var g = go.AddComponent<Garage>();
            {
                var vol = new GameObject("HangarVolume"); vol.transform.SetParent(go.transform, false); vol.layer = HangarLayer;
                var v = vol.AddComponent<UnityEngine.Rendering.Volume>(); v.isGlobal = true; v.priority = 10f; v.sharedProfile = Resources.Load<UnityEngine.Rendering.VolumeProfile>("HangarProfile");
            }
            g.Texture = new RenderTexture(1024, 768, 24) { antiAliasing = 2 };
            // the hangar: concrete underfoot, brick at the back, corrugated steel at the sides, steel beams and lamps overhead, the door open on the night
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane); Destroy(floor.GetComponent<Collider>()); floor.transform.SetParent(go.transform, false); floor.transform.localScale = new Vector3(4.4f, 1f, 4.4f);
            floor.GetComponent<Renderer>().sharedMaterial = Surface("hangar_floor", new Vector2(9f, 9f), new Color(0.62f, 0.62f, 0.64f), 0.42f);
            Wall(go.transform, new Vector3(0f, 6f, 20f), Quaternion.identity, new Vector3(46f, 12f, 1f), Surface("hangar_brick", new Vector2(9f, 2.4f), new Color(0.62f, 0.6f, 0.58f), 0.12f));
            Wall(go.transform, new Vector3(-22f, 6f, 0f), Quaternion.Euler(0f, -90f, 0f), new Vector3(44f, 12f, 1f), Surface("hangar_metal", new Vector2(8f, 2.2f), new Color(0.6f, 0.62f, 0.66f), 0.3f));
            Wall(go.transform, new Vector3(22f, 6f, 0f), Quaternion.Euler(0f, 90f, 0f), new Vector3(44f, 12f, 1f), Surface("hangar_metal", new Vector2(8f, 2.2f), new Color(0.6f, 0.62f, 0.66f), 0.3f));
            var roof = GameObject.CreatePrimitive(PrimitiveType.Plane); Destroy(roof.GetComponent<Collider>()); roof.transform.SetParent(go.transform, false); roof.transform.localPosition = new Vector3(0f, 12f, 0f); roof.transform.localRotation = Quaternion.Euler(180f, 0f, 0f); roof.transform.localScale = new Vector3(4.4f, 1f, 4.4f);
            roof.GetComponent<Renderer>().sharedMaterial = Surface("hangar_metal", new Vector2(10f, 10f), new Color(0.22f, 0.23f, 0.25f), 0.1f);
            var steel = new Material(Resources.Load<Material>("BarrelLit")); steel.SetColor("_BaseColor", new Color(0.2f, 0.2f, 0.21f)); steel.SetFloat("_Metallic", 0.5f); steel.SetFloat("_Smoothness", 0.35f);
            for (int i = -2; i <= 2; i++) { var beam = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(beam.GetComponent<Collider>()); beam.transform.SetParent(go.transform, false); beam.transform.localPosition = new Vector3(0f, 11.4f, i * 8f); beam.transform.localScale = new Vector3(44f, 0.7f, 0.35f); beam.GetComponent<Renderer>().sharedMaterial = steel; }
            for (int i = -1; i <= 1; i++) { var post = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(post.GetComponent<Collider>()); post.transform.SetParent(go.transform, false); post.transform.localPosition = new Vector3(-21.6f, 6f, i * 12f); post.transform.localScale = new Vector3(0.5f, 12f, 0.5f); post.GetComponent<Renderer>().sharedMaterial = steel; }
            // the door: a bright opening in the back wall to the right, the night coming in cold
            var doorMat = new Material(Resources.Load<Material>("VehicleLit")); doorMat.SetColor("_BaseColor", new Color(0.05f, 0.07f, 0.14f)); doorMat.SetFloat("_Smoothness", 0f);   // the night outside: a dark plate in the opening
            var door = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(door.GetComponent<Collider>()); door.transform.SetParent(go.transform, false); door.transform.localPosition = new Vector3(18.5f, 4.2f, 19.7f); door.transform.localScale = new Vector3(6f, 8.4f, 1f); door.GetComponent<Renderer>().sharedMaterial = doorMat;
            var hazeMat = new Material(Resources.Load<Material>("Additive")); hazeMat.SetTexture("_BaseMap", Lightswarm.ProceduralSprites.Glow(64, 0.2f).texture); hazeMat.SetColor("_BaseColor", new Color(0.3f, 0.4f, 0.7f, 1f));
            var haze = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(haze.GetComponent<Collider>()); haze.transform.SetParent(go.transform, false); haze.transform.localPosition = new Vector3(18.5f, 4.5f, 19.5f); haze.transform.localScale = new Vector3(9f, 11f, 1f); haze.GetComponent<Renderer>().sharedMaterial = hazeMat;   // moonlight spilling in
            var doorLight = new GameObject("DoorLight").AddComponent<Light>(); doorLight.transform.SetParent(go.transform, false); doorLight.transform.localPosition = new Vector3(17f, 5f, 15f); doorLight.type = LightType.Point; doorLight.range = 24f; doorLight.intensity = 8f; doorLight.color = new Color(0.55f, 0.66f, 1f); doorLight.shadows = LightShadows.None;
            // a wide soft light in the middle so the walls and the roof read as walls, not as a void
            var amb = new GameObject("HangarAmbient").AddComponent<Light>(); amb.transform.SetParent(go.transform, false); amb.transform.localPosition = new Vector3(0f, 7f, 4f); amb.type = LightType.Point; amb.range = 52f; amb.intensity = 30f; amb.color = new Color(0.9f, 0.85f, 0.75f); amb.shadows = LightShadows.None;
            // wall washers: a light falls off with the square of the distance, so the walls need lamps of their own close by
            foreach (var w in new[] { new Vector3(-9f, 9.5f, 15.5f), new Vector3(7f, 9.5f, 15.5f) }) { var ws = new GameObject("Washer").AddComponent<Light>(); ws.transform.SetParent(go.transform, false); ws.transform.localPosition = w; ws.transform.rotation = Quaternion.Euler(62f, 0f, 0f); ws.type = LightType.Spot; ws.spotAngle = 110f; ws.range = 20f; ws.intensity = 34f; ws.color = new Color(1f, 0.88f, 0.7f); ws.shadows = LightShadows.None; }
            foreach (var sx in new[] { -1f, 1f }) { var ws = new GameObject("SideWasher").AddComponent<Light>(); ws.transform.SetParent(go.transform, false); ws.transform.localPosition = new Vector3(sx * 17.5f, 9.5f, 2f); ws.transform.rotation = Quaternion.Euler(60f, sx > 0 ? 90f : -90f, 0f); ws.type = LightType.Spot; ws.spotAngle = 120f; ws.range = 22f; ws.intensity = 26f; ws.color = new Color(0.95f, 0.9f, 0.8f); ws.shadows = LightShadows.None; }
            // three lamps hanging from the beams, with a glow in each shade and warm light under it
            var shadeMat = new Material(Resources.Load<Material>("BarrelLit")); shadeMat.SetColor("_BaseColor", new Color(0.12f, 0.12f, 0.12f)); shadeMat.SetFloat("_Metallic", 0.3f); shadeMat.SetFloat("_Smoothness", 0.5f); shadeMat.SetFloat("_Cull", 0f);
            var glowMat = new Material(Resources.Load<Material>("Additive")); glowMat.SetTexture("_BaseMap", Lightswarm.ProceduralSprites.Glow(64, 0.35f).texture); glowMat.SetColor("_BaseColor", new Color(1f, 0.85f, 0.6f, 1f));
            for (int i = 0; i < 3; i++)
            {
                var at = new Vector3(-3f + i * 3.2f, 10.5f, 4f + i * 0.5f);
                var cord = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(cord.GetComponent<Collider>()); cord.transform.SetParent(go.transform, false); cord.transform.localPosition = at + new Vector3(0f, 0.6f, 0f); cord.transform.localScale = new Vector3(0.04f, 0.6f, 0.04f); cord.GetComponent<Renderer>().sharedMaterial = steel;
                var shade = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(shade.GetComponent<Collider>()); shade.transform.SetParent(go.transform, false); shade.transform.localPosition = at; shade.transform.localScale = new Vector3(1.3f, 0.18f, 1.3f); shade.GetComponent<Renderer>().sharedMaterial = shadeMat;
                var glow = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(glow.GetComponent<Collider>()); glow.transform.SetParent(go.transform, false); glow.transform.localPosition = at + new Vector3(0f, -0.25f, 0f); glow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); glow.transform.localScale = new Vector3(1.6f, 1.6f, 1f); glow.GetComponent<Renderer>().sharedMaterial = glowMat;
            }
            // what stands about in a workshop: drums, crates, sandbags, a truck and a second tank in the shadows at the back
            Prop(go.transform, "barrels", new Vector3(-13f, 0f, 9f), 20f); Prop(go.transform, "barrels", new Vector3(-15.5f, 0f, 6f), 110f); Prop(go.transform, "crate", new Vector3(-12f, 0f, 3f), 15f); Prop(go.transform, "crate", new Vector3(-12.8f, 0f, 4.6f), 40f);
            Prop(go.transform, "sandbags", new Vector3(14f, 0f, 2f), -30f); Prop(go.transform, "crate", new Vector3(15f, 0f, 8f), 70f); Prop(go.transform, "barrels", new Vector3(17f, 0f, 12f), 0f);
            Prop(go.transform, "truck", new Vector3(-13f, 0f, 15f), 160f);
            g.parked = new GameObject("Parked").transform; g.parked.SetParent(go.transform, false); g.parked.localPosition = new Vector3(13.5f, 0f, 14f); g.parked.localRotation = Quaternion.Euler(0f, 205f, 0f);
            g.stage = new GameObject("Turntable").transform; g.stage.SetParent(go.transform, false);
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(disc.GetComponent<Collider>()); disc.transform.SetParent(go.transform, false); disc.transform.localPosition = new Vector3(0f, 0.02f, 0f); disc.transform.localScale = new Vector3(9f, 0.02f, 9f);
            disc.GetComponent<Renderer>().sharedMaterial = Surface("hangar_floor", new Vector2(2f, 2f), new Color(0.3f, 0.3f, 0.31f), 0.5f);   // the painted circle of the turntable
            var lampGo = new GameObject("Lamp"); lampGo.transform.SetParent(go.transform, false); lampGo.transform.localPosition = new Vector3(6f, 6f, -7f); lampGo.transform.LookAt(go.transform.position + new Vector3(0f, 1.2f, 0f));
            g.lamp = lampGo.AddComponent<Light>(); g.lamp.type = LightType.Spot; g.lamp.spotAngle = 60f; g.lamp.range = 30f; g.lamp.intensity = 34f; g.lamp.color = new Color(1f, 0.96f, 0.9f); g.lamp.shadows = LightShadows.Soft;
            var fillGo = new GameObject("Fill"); fillGo.transform.SetParent(go.transform, false); fillGo.transform.localPosition = new Vector3(-8f, 4f, -8f);
            g.fill = fillGo.AddComponent<Light>(); g.fill.type = LightType.Point; g.fill.range = 30f; g.fill.intensity = 16f; g.fill.color = new Color(0.8f, 0.85f, 1f); g.fill.shadows = LightShadows.None;
            // the menu's camera: low, from the front-left, the tank filling the middle of a portrait screen; light shafts from the roof for it
            g.TitleTexture = new RenderTexture(Mathf.Max(480, Screen.width * 2 / 3), Mathf.Max(800, Screen.height * 2 / 3), 24) { antiAliasing = 2 };
            var tcGo = new GameObject("TitleCamera"); tcGo.transform.SetParent(go.transform, false); tcGo.transform.localPosition = new Vector3(-9.6f, 3.4f, -15.2f); tcGo.transform.LookAt(go.transform.position + new Vector3(0.2f, 1.05f, 0.3f));
            g.titleCam = tcGo.AddComponent<Camera>(); g.titleCam.targetTexture = g.TitleTexture; g.titleCam.fieldOfView = 40f; g.titleCam.nearClipPlane = 0.3f; g.titleCam.farClipPlane = 60f; g.titleCam.clearFlags = CameraClearFlags.SolidColor; g.titleCam.backgroundColor = new Color(0.03f, 0.03f, 0.04f); g.titleCam.enabled = false;
            var tdata = tcGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(); tdata.renderShadows = true; tdata.renderPostProcessing = true; tdata.volumeLayerMask = 1 << HangarLayer; tdata.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.FastApproximateAntialiasing;
            for (int i = 0; i < 3; i++)
            {
                var shaft = new GameObject("Shaft").AddComponent<LightShaft>(); shaft.transform.SetParent(go.transform, false); shaft.facing = g.titleCam; shaft.length = 15f; shaft.width0 = 1.2f; shaft.width1 = 6.5f;
                shaft.Set(go.transform.position + new Vector3(-3f + i * 3.2f, 10.5f, 4f + i * 0.5f), new Vector3(0.28f, -1f, -0.35f));
            }
            var camGo = new GameObject("GarageCamera"); camGo.transform.SetParent(go.transform, false); camGo.transform.localPosition = new Vector3(0f, 3.6f, -9f); camGo.transform.LookAt(go.transform.position + new Vector3(0f, 1.2f, 0f));
            g.cam = camGo.AddComponent<Camera>(); g.cam.targetTexture = g.Texture; g.cam.fieldOfView = 34f; g.cam.nearClipPlane = 0.3f; g.cam.farClipPlane = 60f; g.cam.clearFlags = CameraClearFlags.SolidColor; g.cam.backgroundColor = new Color(0.05f, 0.05f, 0.06f); g.cam.enabled = false;
            var data = camGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(); data.renderShadows = true; data.renderPostProcessing = true; data.volumeLayerMask = 1 << HangarLayer;
            return g;
        }

        /// <summary>Puts the tank of that spec on the turntable (the last one goes); painted as the platoon is.</summary>
        public void Show(VehicleSpec spec)
        {
            if (spec == null || (shown != null && shownId == spec.id)) return;
            if (shown != null) Destroy(shown.gameObject);
            shown = Vehicle.Create(spec, true, Home, 0f); shown.transform.SetParent(stage, true); shownId = spec.id;
            shown.turretYaw = 0.35f; shown.Apply(); shown.enabled = false; shown.KillRings(Career.Rings(spec.id));
            if (parkedTank == null && parked != null)
            {
                // one of the wingmen parked at the back, in the shadows
                var wing = VehicleSpec.ById(Depot.WingmanId); if (wing != null && VehicleSpec.Available(wing)) { parkedTank = Vehicle.Create(wing, false, parked.position, parked.eulerAngles.y * Mathf.Deg2Rad); parkedTank.transform.SetParent(parked, true); parkedTank.turretYaw = parkedTank.yaw + 0.5f; parkedTank.Apply(); parkedTank.enabled = false; }
            }
        }

        public void SetActive(bool on) { cam.enabled = on; gameObject.SetActive(on || titleOn); }
        /// <summary>The menu camera's framing: the tank in the middle of the screen for the title, high in the frame for the depot (a card fills the lower half).</summary>
        public void Frame(bool depot) { titleCam.transform.LookAt(transform.position + (depot ? new Vector3(0.2f, -1.7f, 0.3f) : new Vector3(0.2f, 1.05f, 0.3f))); }
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

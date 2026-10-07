using UnityEngine;

namespace IronNight
{
    /// <summary>
    /// The depot's showroom: the chosen tank on a turntable under a hangar lamp, the commander in the hatch, rendered by
    /// its own camera into a texture the garage tab shows. Lives far below the field so nothing else sees it. A drag on
    /// the picture turns the tank; left alone it turns slowly on its own.
    /// </summary>
    public partial class Garage : MonoBehaviour
    {
        public RenderTexture Texture { get; private set; }
        public RenderTexture TitleTexture { get; private set; }   // the whole screen behind the menu
        Camera titleCam; bool titleOn; Transform floor;
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
            var spinBefore = stage.localRotation; stage.localRotation = Quaternion.Euler(0f, 208f, 0f);   // her front three-quarters to this camera, the gun toward it
            Show(spec); if (shown != null) shown.Apply();
            foreach (var f in crewFigures) if (f != null) f.SetActive(false);   // the tank alone: the crew stand between her and this camera
            portraitCam.Render();
            foreach (var f in crewFigures) if (f != null) f.SetActive(true);
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
        /// <summary>A prop from Resources/Props (its mesh and its _tex picture) placed and turned; null when it is not there.</summary>
        internal static GameObject Prop(Transform parent, string mesh, Vector3 pos, float yaw)
        {
            var pf = Resources.Load<GameObject>("Props/" + mesh); if (pf == null) return null;
            var p = Instantiate(pf, parent); p.transform.localPosition = pos; p.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var m = new Material(Resources.Load<Material>("VehicleLit")); m.SetTexture("_BaseMap", Resources.Load<Texture2D>("Props/" + mesh + "_tex")); m.SetFloat("_Smoothness", 0.15f); m.SetFloat("_Cull", 0f);
            foreach (var r in p.GetComponentsInChildren<Renderer>()) { r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
            return p;
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
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane); Destroy(floor.GetComponent<Collider>()); floor.transform.SetParent(go.transform, false); floor.transform.localScale = new Vector3(4.4f, 1f, 4.4f); g.floor = floor.transform;
            floor.GetComponent<Renderer>().sharedMaterial = Surface("hangar_floor", new Vector2(7f, 7f), new Color(0.62f, 0.62f, 0.64f), 0.42f);
            Wall(go.transform, new Vector3(0f, 6f, 20f), Quaternion.identity, new Vector3(46f, 12f, 1f), Surface("hangar_brick", new Vector2(17f, 5.3f), new Color(0.62f, 0.6f, 0.58f), 0.12f));   // a brick a third larger than life: fewer repeats across the wall
            Wall(go.transform, new Vector3(-22f, 6f, 0f), Quaternion.Euler(0f, -90f, 0f), new Vector3(44f, 12f, 1f), Surface("hangar_metal", new Vector2(8f, 2.2f), new Color(0.6f, 0.62f, 0.66f), 0.3f));
            Wall(go.transform, new Vector3(22f, 6f, 0f), Quaternion.Euler(0f, 90f, 0f), new Vector3(44f, 12f, 1f), Surface("hangar_metal", new Vector2(8f, 2.2f), new Color(0.6f, 0.62f, 0.66f), 0.3f));
            var roof = GameObject.CreatePrimitive(PrimitiveType.Plane); Destroy(roof.GetComponent<Collider>()); roof.transform.SetParent(go.transform, false); roof.transform.localPosition = new Vector3(0f, 12f, 0f); roof.transform.localRotation = Quaternion.Euler(180f, 0f, 0f); roof.transform.localScale = new Vector3(4.4f, 1f, 4.4f);
            roof.GetComponent<Renderer>().sharedMaterial = Surface("hangar_metal", new Vector2(10f, 10f), new Color(0.22f, 0.23f, 0.25f), 0.1f);
            var steel = new Material(Resources.Load<Material>("BarrelLit")); steel.SetColor("_BaseColor", new Color(0.2f, 0.2f, 0.21f)); steel.SetFloat("_Metallic", 0.5f); steel.SetFloat("_Smoothness", 0.35f);
            for (int i = -2; i <= 2; i++) { var beam = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(beam.GetComponent<Collider>()); beam.transform.SetParent(go.transform, false); beam.transform.localPosition = new Vector3(0f, 11.4f, i * 8f); beam.transform.localScale = new Vector3(44f, 0.7f, 0.35f); beam.GetComponent<Renderer>().sharedMaterial = steel; }
            for (int i = -1; i <= 1; i++) { var post = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(post.GetComponent<Collider>()); post.transform.SetParent(go.transform, false); post.transform.localPosition = new Vector3(-21.6f, 6f, i * 12f); post.transform.localScale = new Vector3(0.5f, 12f, 0.5f); post.GetComponent<Renderer>().sharedMaterial = steel; }
            // the door: a bright opening in the back wall to the right, the night coming in cold
            var backdrop = Resources.Load<Shader>("Shaders/Backdrop"); Material doorMat;
            if (backdrop != null) { doorMat = new Material(backdrop); doorMat.SetTexture("_MainTex", Resources.Load<Texture2D>("Textures/door_night")); }   // the night outside, lit by itself: parked tanks in the fog, the moon, the searchlights
            else { doorMat = new Material(Resources.Load<Material>("VehicleLit")); doorMat.SetColor("_BaseColor", new Color(0.05f, 0.07f, 0.14f)); doorMat.SetFloat("_Smoothness", 0f); }
            var door = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(door.GetComponent<Collider>()); door.transform.SetParent(go.transform, false); door.transform.localPosition = new Vector3(18.5f, 4.2f, 19.7f); door.transform.localScale = new Vector3(6f, 8.4f, 1f); door.GetComponent<Renderer>().sharedMaterial = doorMat;
            // the opening's steel frame, and the shutter rolled up over it
            Block(go.transform, new Vector3(15.3f, 4.35f, 19.6f), new Vector3(0.4f, 8.7f, 0.5f), steel); Block(go.transform, new Vector3(21.7f, 4.35f, 19.6f), new Vector3(0.4f, 8.7f, 0.5f), steel); Block(go.transform, new Vector3(18.5f, 8.9f, 19.6f), new Vector3(6.8f, 0.5f, 0.5f), steel);
            var drum = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(drum.GetComponent<Collider>()); drum.transform.SetParent(go.transform, false); drum.transform.localPosition = new Vector3(18.5f, 9.55f, 19.35f); drum.transform.localRotation = Quaternion.Euler(0f, 0f, 90f); drum.transform.localScale = new Vector3(0.8f, 3.2f, 0.8f);
            drum.GetComponent<Renderer>().sharedMaterial = Surface("hangar_metal", new Vector2(6f, 1f), new Color(0.45f, 0.47f, 0.5f), 0.3f);
            // on the brick between the hunt board and the door: KEEP 'EM ROLLING
            var poster = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(poster.GetComponent<Collider>()); poster.transform.SetParent(go.transform, false); poster.transform.localPosition = new Vector3(12.95f, 3.1f, 19.92f); poster.transform.localRotation = Quaternion.Euler(0f, 0f, -1.2f); poster.transform.localScale = new Vector3(1.3f, 1.95f, 1f);
            var posterMat = new Material(Resources.Load<Material>("VehicleLit")); posterMat.SetTexture("_BaseMap", Resources.Load<Texture2D>("Textures/poster_rolling")); posterMat.SetFloat("_Smoothness", 0.15f); poster.GetComponent<Renderer>().sharedMaterial = posterMat;
            var hazeMat = new Material(Resources.Load<Material>("Additive")); hazeMat.SetTexture("_BaseMap", Lightswarm.ProceduralSprites.Glow(64, 0.2f).texture); hazeMat.SetColor("_BaseColor", new Color(0.16f, 0.22f, 0.4f, 1f));   // softer now the night has its own light
            var haze = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(haze.GetComponent<Collider>()); haze.transform.SetParent(go.transform, false); haze.transform.localPosition = new Vector3(18.5f, 4.5f, 19.5f); haze.transform.localScale = new Vector3(9f, 11f, 1f); haze.GetComponent<Renderer>().sharedMaterial = hazeMat;   // moonlight spilling in
            var doorLight = new GameObject("DoorLight").AddComponent<Light>(); doorLight.transform.SetParent(go.transform, false); doorLight.transform.localPosition = new Vector3(17f, 5f, 15f); doorLight.type = LightType.Point; doorLight.range = 24f; doorLight.intensity = 8f; doorLight.color = new Color(0.55f, 0.66f, 1f); doorLight.shadows = LightShadows.None;
            // a wide soft light in the middle so the walls and the roof read as walls, not as a void
            var amb = new GameObject("HangarAmbient").AddComponent<Light>(); amb.transform.SetParent(go.transform, false); amb.transform.localPosition = new Vector3(0f, 7f, 4f); amb.type = LightType.Point; amb.range = 52f; amb.intensity = 30f; amb.color = new Color(0.9f, 0.85f, 0.75f); amb.shadows = LightShadows.None;
            g.doorMat = doorMat; g.hazeMat = hazeMat; g.doorLight = doorLight; g.ambLight = amb; g.TickDoor(true);
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
            // the turntable: a riveted diamond-plate disc with its hazard ring, turning with the tank on it
            var disc = new GameObject("TurntablePlate", typeof(MeshFilter), typeof(MeshRenderer)); disc.transform.SetParent(g.stage, false);
            disc.GetComponent<MeshFilter>().sharedMesh = Disc(4.5f, 0.04f, 96, 0.965f);
            disc.GetComponent<MeshRenderer>().sharedMaterials = new[] { Surface("turntable", Vector2.one, new Color(0.82f, 0.82f, 0.82f), 0.42f), steel };
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
            g.BuildBoard(); g.DressWorkshop();
            return g;
        }

        /// <summary>The tank on the turntable takes its new name, and its marks as they stand.</summary>
        public void Repaint() { if (shown != null && shownId != null) shown.PaintTurret(Career.Name(shownId), Career.Cats(shownId)); }

        /// <summary>Puts the tank of that spec on the turntable (the last one goes); painted as the platoon is.</summary>
        public void Show(VehicleSpec spec)
        {
            if (spec == null || (shown != null && shownId == spec.id)) return;
            if (shown != null) Destroy(shown.gameObject);
            shown = Vehicle.Create(spec, true, Home, 0f); shown.transform.SetParent(stage, true); shownId = spec.id; Wear.Dress(shown, spec.id);   // as she came back from her last night
            shown.turretYaw = 0.35f; shown.Apply(); shown.enabled = false; shown.KillRings(Career.Rings(spec.id)); shown.PaintTurret(Career.Name(spec.id), Career.Cats(spec.id)); foreach (var t in shown.GetComponentsInChildren<Transform>()) if (t.name == "Commander") CrewIdle.Bring(t.gameObject, 9, 0.8f); ShowCrew(System.Array.Exists(Depot.Leaders, x => x.id == spec.id && x.nation == "su") ? "su" : "us");
            if (parkedTank == null && parked != null)
            {
                // one of the wingmen parked at the back, in the shadows
                var wing = VehicleSpec.ById(Depot.WingmanId); if (wing != null && VehicleSpec.Available(wing)) { parkedTank = Vehicle.Create(wing, false, parked.position, parked.eulerAngles.y * Mathf.Deg2Rad); parkedTank.transform.SetParent(parked, true); parkedTank.turretYaw = parkedTank.yaw + 0.5f; parkedTank.Apply(); parkedTank.enabled = false; }
            }
        }

        readonly System.Collections.Generic.List<GameObject> crewFigures = new System.Collections.Generic.List<GameObject>(); string crewNation;
        /// <summary>The leader's crew standing on the hangar floor in front of the turntable, turned to the camera, when there
        /// are figures of them (Props/crew_us_gunner and the like, exported facing +Z).</summary>
        static readonly string[] AtEase = { "map", "smoke", "rag", "mess" };   // what each seat is at in the hangar: gunner, loader, driver, radio
        /// <summary>The figures again after a change of crew: a woman taking a seat stands there herself.</summary>
        public void RefreshCrew() { var n = crewNation; crewNation = null; if (n != null) ShowCrew(n); }

        public void ShowCrew(string nation)
        {
            if (crewNation == nation) return; crewNation = nation;
            foreach (var f in crewFigures) Destroy(f); crewFigures.Clear();
            Vector3[] at = { new Vector3(-4.41f, 0f, -2.87f), new Vector3(-3.84f, 0f, -3.94f), new Vector3(-1.90f, 0f, -5.17f), new Vector3(-0.69f, 0f, -5.22f) };
            float[] turn = { -12f, -6f, 6f, 12f };   // toward the tank: the left pair to the right of the picture, the right pair to the left
            foreach (var a in System.Environment.GetCommandLineArgs())
            {
                var inv = System.Globalization.CultureInfo.InvariantCulture;
                if (a.StartsWith("--crewat=")) { var p = a.Substring(9).Split(','); for (int k = 0; k < 4 && 2 * k + 1 < p.Length; k++) at[k] = new Vector3(float.Parse(p[2 * k], inv), 0f, float.Parse(p[2 * k + 1], inv)); }
                if (a.StartsWith("--crewyaw=")) { var p = a.Substring(10).Split(','); for (int k = 0; k < 4 && k < p.Length; k++) turn[k] = float.Parse(p[k], inv); }
            }
            var eye = titleCam != null ? titleCam.transform.localPosition : new Vector3(-9.6f, 3.4f, -15.2f); var idle = new CrewIdle[4];
            for (int i = 0; i < 4; i++)
            {
                var man = Crew.Chosen(nation, Crew.Roles[i]); string id = "crew_" + man.id; var pf = Resources.Load<GameObject>("Props/" + id);   // the one in the seat, when there is a figure of her or him
                if (pf == null) { id = "crew_" + nation + "_" + Crew.Roles[i]; pf = Resources.Load<GameObject>("Props/" + id); }
                { string ease = id.EndsWith("_4") ? id + "_idle" : "crew_" + nation + "_" + AtEase[i]; var ep = Resources.Load<GameObject>("Props/" + ease); if (ep != null) { id = ease; pf = ep; } }   // at ease, at something
                if (pf == null) continue;
                var go = Instantiate(pf, transform); go.name = id; go.transform.localPosition = at[i];
                var look = eye - at[i]; look.y = 0f; go.transform.localRotation = Quaternion.LookRotation(look.normalized) * Quaternion.Euler(0f, turn[i], 0f);   // to the camera, a little toward the tank
                var mat = new Material(Resources.Load<Material>("VehicleLit")); mat.SetTexture("_BaseMap", Resources.Load<Texture2D>("Props/" + id + "_tex")); mat.SetFloat("_Cull", 0f);
                foreach (var r in go.GetComponentsInChildren<Renderer>()) { r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
                idle[i] = CrewIdle.Bring(go, i + (nation == "su" ? 4 : 0), Crew.Roles[i] == "radio" ? 0.5f : 1f);   // at ease, not statues; the radio operators hold a handset to the ear
                if (id.EndsWith("_smoke") || id.EndsWith("loader_4_idle")) Cigarette.Smoke(go, 1.8f);   // whoever is having a smoke
                crewFigures.Add(go);
            }
            for (int p = 0; p < 4; p += 2)   // the pairs: the left man looks to his left for the right one, who looks to his right
                if (idle[p] != null && idle[p + 1] != null) { idle[p].partner = idle[p + 1]; idle[p].partnerSide = -1f; idle[p + 1].partner = idle[p]; idle[p + 1].partnerSide = 1f; }
        }

        public void SetActive(bool on) { cam.enabled = on; gameObject.SetActive(on || titleOn); }
        /// <summary>The menu camera's framing: the tank and its crew between the name and the tiles for the title, high in the frame for the depot (a card fills the lower half).</summary>
        public void Frame(bool depot)
        {
            Vector3 at = depot ? new Vector3(-9.6f, 3.4f, -15.2f) : new Vector3(-13.03f, 4.22f, -20.63f), aim = depot ? new Vector3(0.2f, -1.7f, 0.3f) : new Vector3(0.2f, -1.3f, 0.3f);
            foreach (var a in System.Environment.GetCommandLineArgs())
                if (!depot && a.StartsWith("--titlecam=")) { var p = System.Array.ConvertAll(a.Substring(11).Split(','), s => float.Parse(s, System.Globalization.CultureInfo.InvariantCulture)); at = new Vector3(p[0], p[1], p[2]); aim = new Vector3(p[3], p[4], p[5]); }
            float fit = Mathf.Max(1f, 0.5625f / Mathf.Max(0.1f, titleCam.aspect));   // narrower than 9:16: further back, the same width of the hangar in the picture
            wantAt = aim + (at - aim) * fit; wantAim = aim;
            if (frameAt == frameAim) { frameAt = wantAt; frameAim = wantAim; titleCam.transform.localPosition = frameAt; titleCam.transform.LookAt(transform.position + frameAim); }   // the first framing a cut, the ones after it a glide
        }
        Vector3 frameAt, frameAim, wantAt, wantAim;   // the menu camera's framing as it glides to the one wanted, before the phone's tilt turns it

        /// <summary>The camera walks up to the hunt board, the trophy wall, or back to the title's framing.</summary>
        public void ViewBoard(bool on)
        {
            if (!on) { Frame(false); return; }
            float aspect = Mathf.Max(0.3f, titleCam.aspect), dist = Mathf.Clamp(2.4f / (Mathf.Tan(titleCam.fieldOfView * 0.5f * Mathf.Deg2Rad) * aspect), 6f, 13f);
            wantAim = BoardAt + new Vector3(0f, -0.1f, 0f); wantAt = wantAim + new Vector3(-0.16f, 0.1f, -1f).normalized * dist;
        }

        /// <summary>The parallax: the menu camera circles its aim a few degrees as the phone tilts, so the crew and the tank
        /// move against the walls behind them and the hangar reads as deep, behind the glass of the menu.</summary>
        void TiltTitle()
        {
            if (titleCam == null || !titleCam.enabled || frameAt == frameAim) return;
            float k = 1f - Mathf.Exp(-Time.unscaledDeltaTime * 3.5f); frameAt = Vector3.Lerp(frameAt, wantAt, k); frameAim = Vector3.Lerp(frameAim, wantAim, k);
            var t = Parallax.Tilt; var off = frameAt - frameAim; var side = Vector3.Cross(off, Vector3.up).normalized;
            titleCam.transform.localPosition = frameAim + Quaternion.AngleAxis(t.x * 3f, Vector3.up) * Quaternion.AngleAxis(t.y * 2f, side) * off;
            titleCam.transform.LookAt(transform.position + frameAim);
        }
        /// <summary>The menu backdrop: the tank turning slowly under the roof lights.</summary>
        public void SetTitle(bool on) { titleOn = on; titleCam.enabled = on; if (on) gameObject.SetActive(true); else { Parallax.Sleep(); if (!cam.enabled) gameObject.SetActive(false); } }
        public void Drag(float dx) { spinVel = dx * 0.35f; }
        static readonly float TestSpin = System.Array.Find(System.Environment.GetCommandLineArgs(), a => a.StartsWith("--spin=")) is string s ? float.Parse(s.Substring(7), System.Globalization.CultureInfo.InvariantCulture) : -1f;   // test switch --spin=degrees

        void Update()
        {
            if (!cam.enabled && !titleOn) return;
            TiltTitle(); TickDoor();
            TickRepair(Time.unscaledDeltaTime);
            spinVel = Mathf.MoveTowards(spinVel, welding != null ? 0f : titleOn && !cam.enabled ? 5f : 12f, Time.unscaledDeltaTime * 30f); spin += spinVel * Time.unscaledDeltaTime;   // under repair it stands
            if (TestSpin >= 0f) { spin = TestSpin; spinVel = 0f; }
            stage.localRotation = Quaternion.Euler(0f, spin, 0f);
            lamp.intensity = 34f + Mathf.Sin(Time.unscaledTime * 9f) * 0.8f;   // the hangar lamp hums
        }
    }
}

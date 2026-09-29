using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace IronNight
{
    /// <summary>
    /// Photo mode, from the pause sheet. The fight stops dead, as a shutter stops it: the fireball, the smoke, the rain
    /// in the air. The HUD steps aside and the camera swoops down from the platoon's view to a low three-quarter shot of
    /// the leader, from the first side nothing stands in front of. A finger (or the mouse) circles him, two fingers (or
    /// the wheel) come closer or stand back; a hedge or a tree in between brings the camera in front of it.
    /// While a photo is framed the camera draws into a picture of the screen's size, and the screen shows it through
    /// the look (Resources/Shaders/PhotoLook: the night's own colour, a 1944 sepia print, black and white; 1944 the
    /// first time, then the last one chosen). The game renders with no post-processing, so the looks are drawn here, not
    /// by volumes. A camera that draws nothing of the world puts the controls over the picture: URP draws the overlay
    /// UI only with a camera on the screen. The shutter renders the camera again, at least 1920 on the long side, lays
    /// the look on it, stamps a small IRON NIGHT in its corner and keeps it as a JPEG: on a phone in the gallery
    /// (Pictures/Iron Night) with SHARE beside it, on a computer in Pictures\Iron Night. A test copy of the game keeps
    /// its pictures in its own folder. --photo=N starts a night and takes two in look N, the second from the far side,
    /// then goes back to the pause sheet.
    /// </summary>
    public partial class Battle
    {
        const float PhotoFov = 44f, PhotoTilt = 5f;   // the tilt lifts the view a little over him: he stands below the middle, the sky above
        float photoYaw, photoPitch, photoDist, photoDistNow, photoIn, photoWas = 1f, photoAuto = -1f, photoFromFov, photoBackFov; int photoLook, photoAsk; bool photoTestDone;
        Vector3 photoAt, photoFromPos, photoBackPos; Quaternion photoFromRot, photoBackRot; byte[] photoWaiting; string photoWaitingName;
        RenderTexture photoFrame; Material photoMat; Camera photoScreen;
        static readonly int photoTest = PhotoArg();
        static int PhotoArg() { foreach (var a in System.Environment.GetCommandLineArgs()) if (a == "--photo") return 1; else if (a.StartsWith("--photo=") && int.TryParse(a.Substring(8), out int n)) return Mathf.Clamp(n, 0, 2); return -1; }
        static int PhotoMsaa() { var urp = UniversalRenderPipeline.asset; return urp != null ? Mathf.Max(1, urp.msaaSampleCount) : 1; }

        /// <summary>Test switch --photo=N: three seconds into the night, photo mode in look N, a picture a second after the
        /// swoop, then another from the far side, closer and higher, and back to the pause sheet a second later.</summary>
        void PhotoTestTick()
        {
            if (photoTest < 0 || photoTestDone || phase != Phase.Play || t < 3f) return;
            photoTestDone = true; Pause(); PhotoIn(); ChoosePhotoLook(photoTest); photoAuto = 0f;
        }

        /// <summary>Where the photo camera stands at a bearing, a height (degrees over the level) and a distance from him.</summary>
        Vector3 PhotoSpot(float yaw, float pitch, float dist)
        {
            float a = yaw * Mathf.Deg2Rad, e = pitch * Mathf.Deg2Rad;
            return photoAt + new Vector3(Mathf.Sin(a) * Mathf.Cos(e), Mathf.Sin(e), Mathf.Cos(a) * Mathf.Cos(e)) * dist;
        }

        void PhotoIn()
        {
            var L = Leader; if (phase != Phase.Pause || L == null || cam == null) return;
            if (photoMat == null) { var sh = Resources.Load<Shader>("Shaders/PhotoLook"); if (sh == null) return; photoMat = new Material(sh); }
            phase = Phase.Photo; photoWas = Time.timeScale; Time.timeScale = 0f;
            photoBackPos = photoFromPos = cam.transform.position; photoBackRot = photoFromRot = cam.transform.rotation; photoBackFov = photoFromFov = cam.fieldOfView; photoIn = 0f;
            photoAt = L.transform.position + Vector3.up * 1.2f;
            // the first shot: low, ahead of him and to one side, far enough that he fills most of the width, from the
            // first bearing where nothing stands in between
            var f = L.Forward; float heading = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg, aspect = Mathf.Max(0.3f, (float)Screen.width / Mathf.Max(1, Screen.height));
            photoPitch = 9f; photoDist = Mathf.Clamp(4.6f / (Mathf.Tan(PhotoFov * 0.5f * Mathf.Deg2Rad) * aspect), 12f, 30f); photoYaw = heading + 38f;
            foreach (float y in new[] { 38f, -38f, 70f, -70f, 115f, -115f, 160f, -160f })
                if (props.SightClear(PhotoSpot(heading + y, photoPitch, photoDist), photoAt)) { photoYaw = heading + y; break; }
            photoDistNow = photoDist;
            photoScreen = new GameObject("PhotoScreen").AddComponent<Camera>();
            photoScreen.cullingMask = 0; photoScreen.clearFlags = CameraClearFlags.SolidColor; photoScreen.backgroundColor = Color.black; photoScreen.depth = cam.depth + 10f;
            hud.HidePause(); hud.ShowPhoto(0); PhotoCanvas(); ChoosePhotoLook(PlayerPrefs.GetInt("photo.look", 1)); Sfx.Whoosh();
        }

        void PhotoOut()
        {
            if (phase != Phase.Photo) return;
            cam.targetTexture = null; if (photoFrame != null) { photoFrame.Release(); Destroy(photoFrame); photoFrame = null; }
            if (photoScreen != null) { photoScreen.enabled = false; Destroy(photoScreen.gameObject); photoScreen = null; }   // off at once: it clears to black after the camera
            cam.transform.SetPositionAndRotation(photoBackPos, photoBackRot); cam.fieldOfView = photoBackFov;
            Time.timeScale = photoWas; phase = Phase.Pause; photoAuto = -1f;
            hud.HidePhoto(); hud.ShowPause(!Sfx.Muted, !LowQuality);
        }

        /// <summary>The picture the camera draws into while a photo is framed: the screen's size, made again if that changes.</summary>
        void PhotoCanvas()
        {
            int w = Mathf.Max(64, Screen.width), h = Mathf.Max(64, Screen.height);
            if (photoFrame != null && photoFrame.width == w && photoFrame.height == h) return;
            cam.targetTexture = null; if (photoFrame != null) { photoFrame.Release(); Destroy(photoFrame); }
            photoFrame = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = PhotoMsaa() };
            cam.targetTexture = photoFrame; hud.ShowPhotoFrame(photoFrame, photoMat);
        }

        /// <summary>One of the three looks: 0 the night's own colour, 1 the 1944 print, 2 black and white.</summary>
        void ChoosePhotoLook(int look)
        {
            photoLook = Mathf.Clamp(look, 0, 2); PlayerPrefs.SetInt("photo.look", photoLook); hud.SetPhotoLook(photoLook);
            if (photoMat != null) photoMat.SetFloat("_Look", photoLook);
        }

        /// <summary>Photo mode's frame: the drag and the pinch move the camera round the leader; nothing else moves but the grain.</summary>
        void TickPhoto()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            var kb = Keyboard.current; if (kb != null && kb.escapeKey.wasPressedThisFrame) { PhotoOut(); return; }
            PhotoCanvas();
            var pad = hud.PhotoPad;
            if (pad != null)
            {
                var d = pad.TakeDrag(); float h = Mathf.Max(1f, Screen.height);
                photoYaw += d.x / h * 380f; photoPitch = Mathf.Clamp(photoPitch - d.y / h * 150f, 2f, 80f);
                photoDist = Mathf.Clamp(photoDist * pad.TakeZoom(), 5f, 45f);
            }
            // a hedge, a tree or a wall between him and the camera: the camera comes in front of it
            float clear = photoDist;
            for (int i = 0; i < 6 && !props.SightClear(PhotoSpot(photoYaw, photoPitch, clear), photoAt); i++) clear *= 0.78f;
            photoDistNow = Mathf.Lerp(photoDistNow, clear, 1f - Mathf.Exp(-dt * (clear < photoDistNow ? 16f : 6f)));
            var p = PhotoSpot(photoYaw, photoPitch, photoDistNow); var r = Quaternion.LookRotation(photoAt - p) * Quaternion.Euler(-PhotoTilt, 0f, 0f); float fov = PhotoFov;
            if (photoIn < 1f) { photoIn = Mathf.Min(1f, photoIn + dt / 1.2f); float k = Smoother(photoIn); p = Vector3.Lerp(photoFromPos, p, k); r = Quaternion.Slerp(photoFromRot, r, k); fov = Mathf.Lerp(photoFromFov, fov, k); }
            cam.transform.SetPositionAndRotation(p, r); cam.fieldOfView = fov;
            props.Tick();
            // film grain moves at the film's 24 frames a second, in cells that keep their size whatever the screen
            photoMat.SetFloat("_Seed", Mathf.Floor(Time.unscaledTime * 24f) % 509f); photoMat.SetFloat("_Grain", Mathf.Max(1f, Screen.height / 1100f));
            if (photoAuto >= 0f && photoIn >= 1f)
            {
                float before = photoAuto; photoAuto += dt;
                if (before < 1f && photoAuto >= 1f) { PhotoShutter(); photoYaw += 150f; photoPitch = 22f; photoDist *= 0.55f; }
                else if (before < 3f && photoAuto >= 3f) PhotoShutter();
                else if (photoAuto >= 4f) { photoAuto = -1f; PhotoOut(); }
            }
            if (photoAsk != 0) { bool yes = photoAsk == 1; photoAsk = 0; if (yes && photoWaiting != null) hud.PhotoNote(PhotoKeep(photoWaiting, photoWaitingName, out bool share), share); photoWaiting = null; }
        }

        /// <summary>The shutter: the camera again into a picture, the look laid on, the mark in its corner, kept; a flash and a click on the screen.</summary>
        void PhotoShutter()
        {
            if (phase != Phase.Photo || cam == null || photoIn < 1f) return;
            int sw = Mathf.Max(64, Screen.width), sh = Mathf.Max(64, Screen.height); float up = Mathf.Clamp(1920f / Mathf.Max(sw, sh), 1f, 2f);
            int w = Mathf.RoundToInt(sw * up), h = Mathf.RoundToInt(sh * up);
            var raw = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB, PhotoMsaa());
            var graded = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            cam.targetTexture = raw; cam.Render(); cam.targetTexture = photoFrame;
            photoMat.SetFloat("_Grain", Mathf.Max(1f, h / 1100f)); Graphics.Blit(raw, graded, photoMat);
            var was = RenderTexture.active; RenderTexture.active = graded;
            var pic = new Texture2D(w, h, TextureFormat.RGB24, false); pic.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            RenderTexture.active = was; RenderTexture.ReleaseTemporary(raw); RenderTexture.ReleaseTemporary(graded);
            PhotoMark(pic); pic.Apply();
            byte[] jpg = pic.EncodeToJPG(92); string name = "IronNight_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".jpg";
            hud.PhotoFlash(); Sfx.Shutter();
#if UNITY_ANDROID && !UNITY_EDITOR
            if (PhotoGallery.MustAsk)
            {
                // Android 9 and older: storage is asked for once, and the picture waits for the answer
                photoWaiting = jpg; photoWaitingName = name; hud.PhotoTaken(pic, "Allow storage to keep your photos", false);
                PhotoGallery.Ask(yes => photoAsk = yes ? 1 : 2); return;
            }
#endif
            string note = PhotoKeep(jpg, name, out bool canShare);
            hud.PhotoTaken(pic, note, canShare);
        }

        /// <summary>The small IRON NIGHT in the bottom right corner, a quarter of the picture wide, laid on at 80%.</summary>
        static void PhotoMark(Texture2D pic)
        {
            var asset = Resources.Load<TextAsset>("UI/photo_mark"); if (asset == null) return;
            var mark = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!mark.LoadImage(asset.bytes)) { Object.Destroy(mark); return; }
            int w = pic.width, mw = Mathf.RoundToInt(w * 0.28f), mh = Mathf.Max(1, Mathf.RoundToInt(mw * (float)mark.height / mark.width)), m = Mathf.RoundToInt(w * 0.045f);
            int x0 = w - mw - m, y0 = m;
            var px = pic.GetPixels(x0, y0, mw, mh);
            for (int y = 0; y < mh; y++) for (int x = 0; x < mw; x++)
            {
                // four samples to a pixel, as the mark is drawn larger than it lands; summed with their cover, so its clear edge adds nothing
                float r = 0f, g = 0f, b = 0f, a = 0f;
                for (int s = 0; s < 4; s++)
                {
                    var c = mark.GetPixelBilinear((x + 0.25f + 0.5f * (s & 1)) / mw, (y + 0.25f + 0.5f * (s >> 1)) / mh);
                    r += c.r * c.a; g += c.g * c.a; b += c.b * c.a; a += c.a;
                }
                const float k = 0.25f * 0.8f; r *= k; g *= k; b *= k; a *= k;
                int i = y * mw + x; var d = px[i];
                px[i] = new Color(d.r * (1f - a) + r, d.g * (1f - a) + g, d.b * (1f - a) + b, 1f);
            }
            pic.SetPixels(x0, y0, mw, mh, px); Object.Destroy(mark);
        }

        /// <summary>Keeps the picture and says where, for the note under its print; share is true when the phone can pass it on.</summary>
        static string PhotoKeep(byte[] jpg, string name, out bool share)
        {
            share = false;
            try
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                share = PhotoGallery.Save(jpg, name); return share ? "Saved to your gallery" : "The photo could not be saved";
#else
                bool real = Application.productName == "Iron Night";   // the test copies keep theirs to themselves
                string dir = real ? Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyPictures), "Iron Night") : Path.Combine(Application.persistentDataPath, "Photos");
                Directory.CreateDirectory(dir); string path = Path.Combine(dir, name);
                for (int n = 2; File.Exists(path); n++) path = Path.Combine(dir, Path.GetFileNameWithoutExtension(name) + "_" + n + ".jpg");
                File.WriteAllBytes(path, jpg); Debug.Log("Photo: " + path);
                return real ? "Saved to Pictures · Iron Night" : "Saved in the test copy's folder";
#endif
            }
            catch (System.Exception e) { Debug.LogWarning("Photo not saved: " + e.Message); return "The photo could not be saved"; }
        }

        void PhotoShare()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try { PhotoGallery.Share(); } catch (System.Exception e) { Debug.LogWarning("Photo not shared: " + e.Message); }
#endif
        }
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    /// <summary>
    /// The phone's gallery, through Android's MediaStore: the picture goes to Pictures/Iron Night. Android 10 and newer
    /// ask no permission for it; 9 and older ask once for storage (the manifest in Plugins/Android/PhotoGallery.androidlib
    /// asks for it only up to 9). SHARE hands the last picture to the phone's own share sheet.
    /// </summary>
    static class PhotoGallery
    {
        static AndroidJavaObject last;   // the last picture's content address, for SHARE

        static int Sdk { get { using (var v = new AndroidJavaClass("android.os.Build$VERSION")) return v.GetStatic<int>("SDK_INT"); } }
        public static bool MustAsk => Sdk < 29 && !UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.ExternalStorageWrite);

        public static void Ask(System.Action<bool> answer)
        {
            var cb = new UnityEngine.Android.PermissionCallbacks();
            cb.PermissionGranted += _ => answer(true); cb.PermissionDenied += _ => answer(false);
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.ExternalStorageWrite, cb);
        }

        public static bool Save(byte[] jpg, string name)
        {
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var resolver = activity.Call<AndroidJavaObject>("getContentResolver"))
            using (var media = new AndroidJavaClass("android.provider.MediaStore$Images$Media"))
            using (var images = media.GetStatic<AndroidJavaObject>("EXTERNAL_CONTENT_URI"))
            using (var values = new AndroidJavaObject("android.content.ContentValues"))
            {
                values.Call("put", "_display_name", name); values.Call("put", "mime_type", "image/jpeg");
                bool modern = Sdk >= 29;
                if (modern) values.Call("put", "relative_path", "Pictures/Iron Night");
                else
                {
                    // Android 9 and older: the file is written where the gallery looks, and the row points at it
                    string dir;
                    using (var env = new AndroidJavaClass("android.os.Environment"))
                    using (var pictures = env.CallStatic<AndroidJavaObject>("getExternalStoragePublicDirectory", "Pictures"))
                        dir = Path.Combine(pictures.Call<string>("getAbsolutePath"), "Iron Night");
                    Directory.CreateDirectory(dir); string path = Path.Combine(dir, name); File.WriteAllBytes(path, jpg);
                    values.Call("put", "_data", path);
                }
                var uri = resolver.Call<AndroidJavaObject>("insert", images, values);
                if (uri == null) return false;
                if (modern)
                {
                    var bytes = new sbyte[jpg.Length]; System.Buffer.BlockCopy(jpg, 0, bytes, 0, jpg.Length);
                    using (var stream = resolver.Call<AndroidJavaObject>("openOutputStream", uri)) { stream.Call("write", bytes); stream.Call("close"); }
                }
                last?.Dispose(); last = uri; return true;
            }
        }

        public static void Share()
        {
            if (last == null) return;
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var intents = new AndroidJavaClass("android.content.Intent"))
            using (var clips = new AndroidJavaClass("android.content.ClipData"))
            using (var intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.SEND"))
            {
                intent.Call<AndroidJavaObject>("setType", "image/jpeg").Dispose();
                intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.STREAM", last).Dispose();
                using (var clip = clips.CallStatic<AndroidJavaObject>("newRawUri", "", last)) intent.Call("setClipData", clip);
                intent.Call<AndroidJavaObject>("addFlags", 1).Dispose();   // FLAG_GRANT_READ_URI_PERMISSION: the app it goes to may read it
                using (var chooser = intents.CallStatic<AndroidJavaObject>("createChooser", intent, "Share the photo")) activity.Call("startActivity", chooser);
            }
        }
    }
#endif
}

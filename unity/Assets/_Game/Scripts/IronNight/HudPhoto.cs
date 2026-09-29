using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace IronNight
{
    /// <summary>
    /// Photo mode's screen: over the stopped fight, only what a camera needs. BACK, the three looks, the shutter, the
    /// last print in the corner and, on a phone, SHARE. A finger anywhere else circles the tank and two pinch (the
    /// Battle reads the PhotoPad); a tap on the picture hides the controls for a clear look, the next brings them
    /// back. The shutter's flash is on the screen only; the photo is the camera's alone, without any of this.
    /// </summary>
    public partial class Hud
    {
        public System.Action OnPhotoMode, OnPhotoBack, OnPhotoShutter, OnPhotoShare; public System.Action<int> OnPhotoLook;
        public PhotoPad PhotoPad { get; private set; }
        GameObject photoSheet, photoShare; CanvasGroup photoControls; Image photoFlash; RectTransform photoPrint; RawImage photoPrintPic, photoFrameView; Text photoHint, photoNote;
        readonly Image[] photoLookFill = new Image[3]; readonly Text[] photoLookLabel = new Text[3];
        float photoFlashA, photoHintLeft, photoNoteLeft, photoPrintT = 1f, photoControlsWant = 1f, photoBarsWas, photoHudWas;
        static readonly string[] PhotoLooks = { "Colour", "1944", "B&W" };
        static readonly Vector2 PrintAt = new Vector2(-330f, 270f), PrintWindow = new Vector2(134f, 164f);
        static Sprite disc;
        static Sprite Disc() => disc ??= Lightswarm.ProceduralSprites.RoundedRect(64, 128);

        void BuildPhoto(Transform root)
        {
            var amber = new Color(0.95f, 0.66f, 0.23f); var dim = new Color(0.66f, 0.64f, 0.59f);
            photoSheet = new GameObject("Photo", typeof(RectTransform)); photoSheet.transform.SetParent(root, false); Stretch(photoSheet);
            var view = new GameObject("View", typeof(RectTransform), typeof(RawImage)); view.transform.SetParent(photoSheet.transform, false); Stretch(view);
            photoFrameView = view.GetComponent<RawImage>(); photoFrameView.raycastTarget = false;
            var pad = new GameObject("Pad", typeof(RectTransform), typeof(Image)); pad.transform.SetParent(photoSheet.transform, false); Stretch(pad);
            pad.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);   // unseen, but it takes the touches
            PhotoPad = pad.AddComponent<PhotoPad>(); PhotoPad.OnTap = () => photoControlsWant = photoControlsWant > 0.5f ? 0f : 1f;

            var ctl = new GameObject("Controls", typeof(RectTransform), typeof(CanvasGroup)); ctl.transform.SetParent(photoSheet.transform, false); Stretch(ctl);
            photoControls = ctl.GetComponent<CanvasGroup>(); var c = ctl.transform;
            // shades at the top and the bottom, so the controls read over a bright sky or a fire
            var low = MakeImage(c, "Shade", new Vector2(0.5f, 0f), Vector2.zero, new Vector2(1400f, 820f), new Color(0.01f, 0.01f, 0.02f, 0.72f)); low.sprite = Lightswarm.ProceduralSprites.GradientDown(128, 1.6f);
            var high = MakeImage(c, "TopShade", new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(1400f, 340f), new Color(0.01f, 0.01f, 0.02f, 0.6f)); high.sprite = low.sprite;
            high.rectTransform.pivot = new Vector2(0.5f, 0.5f); high.rectTransform.localScale = new Vector3(1f, -1f, 1f);

            MakeGhost(c, "BACK", new Vector2(0f, 1f), new Vector2(130, -95), new Vector2(200, 80), 26, () => OnPhotoBack?.Invoke());
            var ey = MakeText(c, "Eyebrow", new Vector2(0.5f, 1f), new Vector2(0f, -95f), TextAnchor.MiddleCenter, 28, amber); ey.text = Spaced("PHOTO MODE"); ey.font = LabelFont(); ey.rectTransform.pivot = new Vector2(0.5f, 0.5f); ey.rectTransform.sizeDelta = new Vector2(560f, 50f);
            photoHint = MakeText(c, "Hint", new Vector2(0.5f, 1f), new Vector2(0f, -170f), TextAnchor.MiddleCenter, 28, dim); photoHint.rectTransform.pivot = new Vector2(0.5f, 0.5f); photoHint.rectTransform.sizeDelta = new Vector2(1000f, 50f);
            photoHint.text = Application.isMobilePlatform ? "Drag to circle the tank · pinch to come closer" : "Drag to circle the tank · the wheel comes closer"; Fit(photoHint, 20);

            // the three looks, over the shutter
            for (int i = 0; i < 3; i++)
            {
                int k = i;
                var b = MakeButton(c, PhotoLooks[i], new Vector2(0.5f, 0f), new Vector2((i - 1) * 250f, 500f), new Vector2(232f, 84f), 28, () => OnPhotoLook?.Invoke(k));
                photoLookFill[i] = b.GetComponent<Image>(); photoLookLabel[i] = b.transform.Find("Label").GetComponent<Text>();
                photoLookLabel[i].font = LabelFont(); photoLookLabel[i].text = Spaced(PhotoLooks[i].ToUpperInvariant());
            }

            // the shutter: a white ring round a white disc, like a phone's own camera
            var sh = MakeButton(c, "Shutter", new Vector2(0.5f, 0f), new Vector2(0f, 270f), new Vector2(200f, 200f), 20, () => OnPhotoShutter?.Invoke());
            var ring = sh.GetComponent<Image>(); ring.sprite = Lightswarm.ProceduralSprites.Ring(192, 0.05f); ring.type = Image.Type.Simple; ring.color = new Color(1f, 1f, 1f, 0.95f);
            sh.transform.Find("Label").GetComponent<Text>().text = "";
            var face = MakeImage(sh.transform, "Disc", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(154f, 154f), new Color(1f, 1f, 1f, 0.92f)); face.sprite = Disc(); face.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            // the last print: a white-bordered photograph dropped in the corner
            var pr = MakeImage(c, "Print", new Vector2(0.5f, 0f), PrintAt, new Vector2(150f, 196f), new Color(0.95f, 0.93f, 0.88f, 1f));
            photoPrint = pr.rectTransform; photoPrint.pivot = new Vector2(0.5f, 0.5f); photoPrint.localRotation = Quaternion.Euler(0f, 0f, 5f);
            var ps = MakeImage(pr.transform, "Shadow", new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(190f, 236f), new Color(0f, 0f, 0f, 0.55f)); ps.sprite = Shadow(); ps.type = Image.Type.Sliced; ps.rectTransform.pivot = new Vector2(0.5f, 0.5f); ps.transform.SetAsFirstSibling();
            var pic = new GameObject("Picture", typeof(RectTransform), typeof(RawImage)); pic.transform.SetParent(pr.transform, false);
            var prt = (RectTransform)pic.transform; prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f); prt.sizeDelta = PrintWindow; prt.anchoredPosition = new Vector2(0f, 8f);
            photoPrintPic = pic.GetComponent<RawImage>(); photoPrintPic.raycastTarget = false; pr.gameObject.SetActive(false);
            photoNote = MakeText(c, "Note", new Vector2(0.5f, 0f), new Vector2(0f, 400f), TextAnchor.MiddleCenter, 24, amber); photoNote.font = LabelFont(); photoNote.rectTransform.pivot = new Vector2(0.5f, 0.5f); photoNote.rectTransform.sizeDelta = new Vector2(900f, 44f); photoNote.text = "";

            photoShare = MakeGhost(c, "Share", new Vector2(0.5f, 0f), new Vector2(330f, 270f), new Vector2(210f, 90f), 28, () => OnPhotoShare?.Invoke()); photoShare.SetActive(false);

            var fl = new GameObject("Flash", typeof(RectTransform), typeof(Image)); fl.transform.SetParent(photoSheet.transform, false); Stretch(fl);
            photoFlash = fl.GetComponent<Image>(); photoFlash.color = new Color(1f, 1f, 1f, 0f); photoFlash.raycastTarget = false;
            photoSheet.SetActive(false);
        }

        /// <summary>Photo mode opens: the HUD steps aside at once and the controls come up, with a hint for a few seconds.</summary>
        public void ShowPhoto(int look)
        {
            photoBarsWas = barsWant; photoHudWas = hudWant; Cinema(false, false, true);
            photoSheet.transform.SetAsLastSibling(); photoSheet.SetActive(true);
            photoControlsWant = 1f; photoControls.alpha = 0f; photoHintLeft = 5f; photoFlashA = 0f; photoFlash.color = new Color(1f, 1f, 1f, 0f);
            SetPhotoLook(look);
        }

        /// <summary>Back to the pause sheet: the HUD as it was before.</summary>
        public void HidePhoto() { photoSheet.SetActive(false); photoFrameView.texture = null; Cinema(photoBarsWas > 0.5f, photoHudWas > 0.5f, true); }

        /// <summary>The camera's picture, full screen through the look, while a photo is framed.</summary>
        public void ShowPhotoFrame(Texture frame, Material look) { photoFrameView.texture = frame; photoFrameView.material = look; }

        public void SetPhotoLook(int look)
        {
            for (int i = 0; i < 3; i++)
            {
                bool on = i == look;
                photoLookFill[i].color = on ? new Color(0.96f, 0.68f, 0.24f, 1f) : new Color(0.06f, 0.07f, 0.09f, 0.6f);
                photoLookLabel[i].color = on ? new Color(0.12f, 0.09f, 0.04f) : new Color(0.93f, 0.91f, 0.86f);
            }
        }

        /// <summary>The shutter fell: the screen flashes white.</summary>
        public void PhotoFlash() { photoFlashA = 0.85f; }

        /// <summary>The picture is kept: its print flies from the middle of the screen into the corner, the note says where
        /// it went, and SHARE comes up when the phone can pass it on.</summary>
        public void PhotoTaken(Texture2D pic, string note, bool share)
        {
            if (photoPrintPic.texture != null && photoPrintPic.texture != pic) Destroy(photoPrintPic.texture);
            photoPrintPic.texture = pic;
            float a = (float)pic.width / pic.height, want = PrintWindow.x / PrintWindow.y;   // the picture cut to the print's window, from its middle
            photoPrintPic.uvRect = a > want ? new Rect((1f - want / a) * 0.5f, 0f, want / a, 1f) : new Rect(0f, (1f - a / want) * 0.5f, 1f, a / want);
            photoPrint.gameObject.SetActive(true); photoPrintT = 0f;
            PhotoNote(note, share);
        }

        public void PhotoNote(string note, bool share) { photoNote.text = Spaced(note.ToUpperInvariant()); photoNoteLeft = 3.5f; photoShare.SetActive(share); }

        void TickPhotoSheet(float dt)
        {
            if (photoSheet == null || !photoSheet.activeSelf) return;
            photoControls.alpha = Mathf.MoveTowards(photoControls.alpha, photoControlsWant, dt * 5f);
            photoControls.blocksRaycasts = photoControls.interactable = photoControlsWant > 0.5f;
            if (photoFlashA > 0f) { photoFlashA = Mathf.Max(0f, photoFlashA - dt * 2.2f); photoFlash.color = new Color(1f, 1f, 1f, photoFlashA * photoFlashA); }
            if (photoHintLeft > 0f) { photoHintLeft -= dt; var hc = photoHint.color; hc.a = Mathf.Clamp01(photoHintLeft / 0.6f); photoHint.color = hc; }
            if (photoNoteLeft > 0f) { photoNoteLeft -= dt; var nc = photoNote.color; nc.a = Mathf.Clamp01(photoNoteLeft / 0.5f); photoNote.color = nc; }
            if (photoPrintT < 1f)
            {
                photoPrintT = Mathf.Min(1f, photoPrintT + dt / 0.5f); float e = 1f - (1f - photoPrintT) * (1f - photoPrintT) * (1f - photoPrintT);
                float mid = canvasRect != null ? canvasRect.rect.height * 0.5f : 1170f;
                photoPrint.anchoredPosition = Vector2.Lerp(new Vector2(0f, mid), PrintAt, e);
                float s = Mathf.Lerp(3f, 1f, e); photoPrint.localScale = new Vector3(s, s, 1f); photoPrint.localRotation = Quaternion.Euler(0f, 0f, 5f * e);
            }
        }

        /// <summary>The pause sheet's way into photo mode: a small camera drawn before the words.</summary>
        static void CameraGlyph(Transform parent, Vector2 at, Color ink)
        {
            var body = MakeImage(parent, "Camera", new Vector2(0.5f, 0.5f), at, new Vector2(50f, 34f), ink); body.sprite = Lightswarm.ProceduralSprites.RoundedRect(8, 32); body.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var hump = MakeImage(body.transform, "Hump", new Vector2(0.5f, 1f), new Vector2(-4f, 6f), new Vector2(20f, 10f), ink); hump.sprite = body.sprite; hump.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var lens = MakeImage(body.transform, "Lens", new Vector2(0.5f, 0.5f), new Vector2(2f, -1f), new Vector2(22f, 22f), new Color(0.08f, 0.09f, 0.1f, 1f)); lens.sprite = Disc(); lens.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var glass = MakeImage(lens.transform, "Glass", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(12f, 12f), ink * new Color(1f, 1f, 1f, 0.8f)); glass.sprite = Disc(); glass.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }
    }

    /// <summary>The photo camera's touch surface: one finger (or the mouse) drags, two pinch, the wheel steps in and out;
    /// a touch that hardly moves is a tap. The Battle takes what gathered since its last frame.</summary>
    public class PhotoPad : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IScrollHandler
    {
        public System.Action OnTap;
        readonly Dictionary<int, Vector2> fingers = new Dictionary<int, Vector2>();
        Vector2 drag; float zoom = 1f, spread; bool moved;

        public Vector2 TakeDrag() { var d = drag; drag = Vector2.zero; return d; }
        public float TakeZoom() { float z = zoom; zoom = 1f; return z; }

        public void OnPointerDown(PointerEventData e) { moved = fingers.Count > 0; fingers[e.pointerId] = e.position; spread = Spread(); }
        public void OnDrag(PointerEventData e)
        {
            if (!fingers.ContainsKey(e.pointerId)) return;
            fingers[e.pointerId] = e.position;
            if (fingers.Count == 1) drag += e.delta;
            else { float s = Spread(); if (spread > 1f && s > 1f) zoom *= spread / s; spread = s; }
            if ((e.position - e.pressPosition).sqrMagnitude > 900f) moved = true;
        }
        public void OnPointerUp(PointerEventData e)
        {
            fingers.Remove(e.pointerId); spread = Spread();
            if (fingers.Count == 0 && !moved && (e.position - e.pressPosition).sqrMagnitude < 900f) OnTap?.Invoke();
        }
        public void OnScroll(PointerEventData e) { if (e.scrollDelta.y != 0f) zoom *= e.scrollDelta.y > 0f ? 0.88f : 1.14f; }
        void OnDisable() { fingers.Clear(); drag = Vector2.zero; zoom = 1f; }

        float Spread()
        {
            if (fingers.Count < 2) return 0f;
            Vector2 a = default, b = default; int n = 0;
            foreach (var p in fingers.Values) { if (n == 0) a = p; else if (n == 1) b = p; n++; }
            return (a - b).magnitude;
        }
    }
}

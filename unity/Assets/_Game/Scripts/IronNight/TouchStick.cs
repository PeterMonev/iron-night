using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace IronNight
{
    /// <summary>
    /// One-thumb control drawn on the HUD canvas: press anywhere, the ring appears there, the knob follows the finger,
    /// <see cref="Direction"/> is the clamped offset (screen up = away from the camera = world +Z). Touch first, mouse
    /// only when the window is focused (touch-screen laptops report phantom mouse presses otherwise).
    /// </summary>
    public class TouchStick : MonoBehaviour
    {
        public float radiusPixels = 110f;
        public Vector2 Direction { get; private set; }
        public bool Active { get; private set; }
        public bool Blocked;   // true while a sheet is open
        public readonly System.Collections.Generic.List<RectTransform> Blockers = new System.Collections.Generic.List<RectTransform>();   // buttons the thumb may press without driving
        public Vector2 TapAt { get; private set; } public bool Tapped { get; private set; }   // a press released within 0.25 s and 18 px: a tap, held until ConsumeTap
        public bool ConsumeTap() { bool t = Tapped; Tapped = false; return t; }
        float pressTime; Vector2 pressAt; bool moved;

        Image ring, knob; RectTransform canvasRect; Vector2 origin; bool keyboard;

        public void Build(Canvas canvas)
        {
            canvasRect = canvas.GetComponent<RectTransform>();
            ring = Hud.MakeImage(canvas.transform, "StickRing", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300, 300), new Color(1f, 0.95f, 0.85f, 0.18f));
            ring.sprite = Lightswarm.ProceduralSprites.Ring(128, 0.05f);
            knob = Hud.MakeImage(canvas.transform, "StickKnob", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110, 110), new Color(1f, 0.7f, 0.36f, 0.5f));
            knob.sprite = Lightswarm.ProceduralSprites.Glow(64, 0.6f);
            ring.rectTransform.pivot = knob.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            SetVisible(false);
        }

        void Update()
        {
            bool pressed = false; Vector2 pos = Vector2.zero;
            var touch = Touchscreen.current;
            if (touch != null)
            {
                var phase = touch.primaryTouch.phase.ReadValue();
                if (phase == UnityEngine.InputSystem.TouchPhase.Began || phase == UnityEngine.InputSystem.TouchPhase.Moved || phase == UnityEngine.InputSystem.TouchPhase.Stationary)
                { pressed = true; pos = touch.primaryTouch.position.ReadValue(); }
            }
            if (!pressed && Mouse.current != null && Application.isFocused)
            {
                pos = Mouse.current.position.ReadValue();
                pressed = Mouse.current.leftButton.isPressed && pos.x >= 0f && pos.y >= 0f && pos.x <= Screen.width && pos.y <= Screen.height;
            }
            if (Blocked) pressed = false;
            if (pressed && !Active) foreach (var r in Blockers) { if (r != null && r.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(r, pos, null)) { pressed = false; break; } }   // a press that starts on a button is the button's
            // keyboard on the desktop build: arrows or WASD drive the leader without the stick
            var kb = Keyboard.current;
            if (kb != null && !Blocked)
            {
                var k = Vector2.zero;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) k.y += 1f; if (kb.sKey.isPressed || kb.downArrowKey.isPressed) k.y -= 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) k.x -= 1f; if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) k.x += 1f;
                if (k != Vector2.zero) { if (Active) SetVisible(false); Active = true; keyboard = true; Direction = k.normalized; return; }
                if (keyboard) { keyboard = false; Active = false; Direction = Vector2.zero; }
            }

            if (pressed && !Active) { Active = true; origin = pos; Direction = Vector2.zero; SetVisible(true); pressTime = Time.unscaledTime; pressAt = pos; moved = false; }
            else if (!pressed && Active) { Active = false; Direction = Vector2.zero; SetVisible(false); if (!moved && Time.unscaledTime - pressTime < 0.25f) { Tapped = true; TapAt = pressAt; } return; }
            if (Active && (pos - pressAt).magnitude > 18f) moved = true;
            if (!Active) return;

            Direction = Vector2.ClampMagnitude((pos - origin) / radiusPixels, 1f);
            Place(ring.rectTransform, origin); Place(knob.rectTransform, origin + Direction * radiusPixels);
        }

        void Place(RectTransform rt, Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out var local);
            rt.anchoredPosition = local;
        }

        void SetVisible(bool v) { if (ring) ring.enabled = v; if (knob) knob.enabled = v; }
    }
}

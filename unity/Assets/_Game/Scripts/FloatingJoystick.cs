using UnityEngine;
using UnityEngine.InputSystem;

namespace Lightswarm
{
    /// <summary>
    /// One-thumb control: press anywhere, drag to move. The ring appears where the finger landed,
    /// the knob follows the finger, and <see cref="Direction"/> is the clamped offset (length 0..1).
    /// Works with touch and mouse through the Input System's generic Pointer device.
    /// </summary>
    public class FloatingJoystick : MonoBehaviour
    {
        public float radiusPixels = 90f;
        public Vector2 Direction { get; private set; }
        public bool Active { get; private set; }

        SpriteRenderer ring, knob;
        Camera cam;
        Vector2 originScreen;

        public void Build(Camera camera)
        {
            cam = camera;
            ring = MakeSprite("JoyRing", ProceduralSprites.Ring(), new Color(1f, 0.95f, 0.85f, 0.25f), 3f);
            knob = MakeSprite("JoyKnob", ProceduralSprites.Glow(64, 0.6f), new Color(1f, 0.7f, 0.36f, 0.45f), 1.1f);
            SetVisible(false);
        }

        SpriteRenderer MakeSprite(string name, Sprite sprite, Color color, float scale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite; sr.color = color; sr.sortingOrder = 100;
            go.transform.localScale = Vector3.one * scale;
            return sr;
        }

        void Update()
        {
            // touch first (phones), then the mouse; Pointer.current alone reports phantom presses on laptops with touch screens
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

            if (pressed && !Active)
            {
                Active = true; originScreen = pos; Direction = Vector2.zero;
                SetVisible(true);
            }
            else if (!pressed && Active) { Release(); return; }

            if (Active)
            {
                Vector2 offset = pos - originScreen;
                Direction = Vector2.ClampMagnitude(offset / radiusPixels, 1f);
                var o = cam.ScreenToWorldPoint(new Vector3(originScreen.x, originScreen.y, 10f));
                var k = cam.ScreenToWorldPoint(new Vector3(originScreen.x + Direction.x * radiusPixels, originScreen.y + Direction.y * radiusPixels, 10f));
                ring.transform.position = new Vector3(o.x, o.y, 0f);
                knob.transform.position = new Vector3(k.x, k.y, 0f);
            }
        }

        void Release() { Active = false; Direction = Vector2.zero; SetVisible(false); }
        void SetVisible(bool v) { if (ring) ring.enabled = v; if (knob) knob.enabled = v; }
    }
}

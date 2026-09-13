using UnityEngine;

namespace Lightswarm
{
    /// <summary>
    /// The night: a huge dark sprite with a soft transparent hole in its centre, kept over the guardian. Everything
    /// under it (forest, sparks' ground glow) is coloured only inside the hole; shadows and fireflies draw above it.
    /// The hole grows with the swarm. Pure sprites, no lights: it behaves the same on every phone.
    /// </summary>
    public class DarknessMask : MonoBehaviour
    {
        const int Size = 1024;
        const float HoleFraction = 1f / 12f; // hole radius as a fraction of the texture width

        SpriteRenderer sr;
        float holeUnits = 2.6f;

        public void Build()
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color[Size * Size];
            float inner = HoleFraction * 0.75f, outer = HoleFraction * 1.5f;
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                float dx = (x + 0.5f) / Size - 0.5f, dy = (y + 0.5f) / Size - 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(inner, outer, d));
                px[y * Size + x] = new Color(0.015f, 0.02f, 0.05f, 0.93f * a);
            }
            tex.SetPixels(px); tex.Apply();
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
            sr.sortingOrder = 14;
            SetHole(holeUnits);
        }

        /// <summary>Radius of the clear area in world units; the sprite is scaled so its edge stays far off-screen.</summary>
        public void SetHole(float radiusUnits)
        {
            holeUnits = radiusUnits;
            transform.localScale = Vector3.one * (radiusUnits / HoleFraction);
        }

        public void Follow(Vector2 center) { transform.position = new Vector3(center.x, center.y, 0f); }
    }
}

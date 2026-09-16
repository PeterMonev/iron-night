using UnityEngine;

namespace Lightswarm
{
    /// <summary>
    /// Placeholder art generated at runtime, so the prototype needs no texture files.
    /// Every sprite is a small texture with a baked soft glow; colour comes from the SpriteRenderer tint.
    /// </summary>
    public static class ProceduralSprites
    {
        const int PixelsPerUnit = 64;

        public static Sprite Glow(int size = 64, float core = 0.15f)
        {
            var tex = NewTexture(size, size);
            var c = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c)) / c;
                float a = d < core ? 1f : Mathf.Clamp01(1f - (d - core) / (1f - core));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
            return Finish(tex);
        }

        /// <summary>A firefly: wide soft glow with a small bright core. Tinted per type by the SpriteRenderer.</summary>
        public static Sprite Firefly(int size = 64)
        {
            var tex = NewTexture(size, size);
            var c = new Vector2(size * 0.5f, size * 0.5f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c) / (size * 0.5f);
                float glow = Mathf.Clamp01(1f - d); glow *= glow * 0.7f;
                float core = Mathf.Clamp01(1f - d / 0.18f);
                float a = Mathf.Clamp01(glow + core);
                tex.SetPixel(x, y, new Color(1f, Mathf.Lerp(0.72f, 0.97f, core), Mathf.Lerp(0.3f, 0.85f, core), a));
            }
            return Finish(tex);
        }

        /// <summary>Soft dark ellipse that sits on the ground under a hovering firefly.</summary>
        public static Sprite GroundShadow(int size = 32)
        {
            var tex = NewTexture(size, size);
            var c = new Vector2(size * 0.5f, size * 0.5f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c) / (size * 0.5f);
                tex.SetPixel(x, y, new Color(0f, 0f, 0f, Mathf.Clamp01(1f - d) * 0.7f));
            }
            return Finish(tex);
        }

        /// <summary>A shadow: dark soft blob with two glowing eyes.</summary>
        public static Sprite Shadow(int size = 64)
        {
            var tex = NewTexture(size, size);
            var c = new Vector2(size * 0.5f, size * 0.5f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c) / (size * 0.5f);
                float a = Mathf.Clamp01(1f - Mathf.Pow(d, 3f));
                tex.SetPixel(x, y, new Color(0.02f, 0.02f, 0.05f, a));
            }
            foreach (var ex in new[] { size * 0.38f, size * 0.62f })
            {
                var eye = new Vector2(ex, size * 0.58f);
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), eye) / (size * 0.07f);
                    if (d < 1f) tex.SetPixel(x, y, Color.Lerp(tex.GetPixel(x, y), new Color(1f, 0.3f, 0.48f, 1f), 1f - d * d));
                }
            }
            return Finish(tex);
        }

        public static Sprite Ring(int size = 96, float thickness = 0.06f)
        {
            var tex = NewTexture(size, size);
            var c = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c)) / c;
                float a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.92f) / thickness);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            return Finish(tex);
        }

        /// <summary>The Allied white star: five points, a thin ring round it the way the 1944 invasion markings had.</summary>
        public static Sprite Star(int size = 256)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { filterMode = FilterMode.Trilinear, wrapMode = TextureWrapMode.Clamp }; float c = size * 0.5f;
            // the outline: five tips and five inner corners, filled by the even-odd rule; four by four samples a pixel for clean edges
            var poly = new Vector2[10]; for (int i = 0; i < 10; i++) { float a = Mathf.PI / 2f + i * Mathf.PI / 5f, rr = i % 2 == 0 ? 0.92f : 0.92f * 0.382f; poly[i] = new Vector2(c + Mathf.Cos(a) * c * rr, c + Mathf.Sin(a) * c * rr); }
            bool Inside(Vector2 p) { bool inside = false; for (int i = 0, j = 9; i < 10; j = i++) if ((poly[i].y > p.y) != (poly[j].y > p.y) && p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x) inside = !inside; return inside; }
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                int hits = 0; for (int sy = 0; sy < 4; sy++) for (int sx = 0; sx < 4; sx++) if (Inside(new Vector2(x + (sx + 0.5f) / 4f, y + (sy + 0.5f) / 4f))) hits++;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, hits / 16f));
            }
            return Finish(tex);
        }

        /// <summary>The Balkenkreuz: a black cross with white bars along the arms, on nothing.</summary>
        public static Sprite Balkenkreuz(int size = 256)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { filterMode = FilterMode.Trilinear, wrapMode = TextureWrapMode.Clamp }; float c = size * 0.5f;
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float u = Mathf.Abs(x + 0.5f - c) / c, v = Mathf.Abs(y + 0.5f - c) / c;   // 0 at the centre, 1 at the edge
                bool armX = v < 0.3f && u < 0.9f, armY = u < 0.3f && v < 0.9f;             // the black cross
                bool barX = v > 0.3f && v < 0.44f && u > 0.3f && u < 0.9f, barY = u > 0.3f && u < 0.44f && v > 0.3f && v < 0.9f;   // the white bars beside the arms
                bool tipX = u > 0.76f && u < 0.9f && v < 0.44f && v > 0.3f, tipY = v > 0.76f && v < 0.9f && u < 0.44f && u > 0.3f;
                Color col = armX || armY ? new Color(0.05f, 0.05f, 0.05f, 1f) : barX || barY || tipX || tipY ? new Color(1f, 1f, 1f, 1f) : new Color(0f, 0f, 0f, 0f);
                tex.SetPixel(x, y, col);
            }
            return Finish(tex);
        }

        static Texture2D NewTexture(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var clear = new Color(0, 0, 0, 0);
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) tex.SetPixel(x, y, clear);
            return tex;
        }

        static Sprite Finish(Texture2D tex)
        {
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }
    }
}

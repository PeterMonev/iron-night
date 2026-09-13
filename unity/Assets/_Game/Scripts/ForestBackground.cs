using UnityEngine;

namespace Lightswarm
{
    /// <summary>
    /// The forest floor: one procedurally painted tile (moss, grass strokes, flowers, tree canopies) repeated under
    /// the camera. The night mask above it hides its colour outside the swarm's glow.
    /// </summary>
    public class ForestBackground : MonoBehaviour
    {
        const int TileSize = 512;   // pixels
        const float TileUnits = 16f; // world units per tile
        SpriteRenderer sr;
        Transform cam;

        public void Build(Camera camera)
        {
            cam = camera.transform;
            var tex = Paint();
            var sprite = Sprite.Create(tex, new Rect(0, 0, TileSize, TileSize), new Vector2(0.5f, 0.5f), TileSize / TileUnits, 0, SpriteMeshType.FullRect);
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = sprite; sr.material = Resources.Load<Material>("SpriteUnlit"); sr.drawMode = SpriteDrawMode.Tiled; sr.size = new Vector2(TileUnits * 5f, TileUnits * 5f); sr.sortingOrder = -10;
        }

        void LateUpdate()
        {
            // snap to the tile grid so the pattern stays fixed in the world while the camera travels
            var p = cam.position;
            transform.position = new Vector3(Mathf.Round(p.x / TileUnits) * TileUnits, Mathf.Round(p.y / TileUnits) * TileUnits, 0f);
        }

        static Texture2D Paint()
        {
            var tex = new Texture2D(TileSize, TileSize, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var px = new Color[TileSize * TileSize];
            var rng = new System.Random(7);
            float R() => (float)rng.NextDouble();
            var ground = new Color(0.07f, 0.17f, 0.15f);
            for (int i = 0; i < px.Length; i++) px[i] = ground;

            void Blob(float cx, float cy, float r, Color c, float strength)
            {
                int x0 = Mathf.FloorToInt(cx - r), x1 = Mathf.CeilToInt(cx + r), y0 = Mathf.FloorToInt(cy - r), y1 = Mathf.CeilToInt(cy + r);
                for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / r; if (d > 1f) continue;
                    int ix = ((x % TileSize) + TileSize) % TileSize, iy = ((y % TileSize) + TileSize) % TileSize; // wrap so the tile seams
                    px[iy * TileSize + ix] = Color.Lerp(px[iy * TileSize + ix], c, (1f - d) * strength);
                }
            }

            for (int i = 0; i < 90; i++) Blob(R() * TileSize, R() * TileSize, 20f + R() * 60f, R() < 0.5f ? new Color(0.15f, 0.36f, 0.25f) : new Color(0.12f, 0.27f, 0.31f), 0.5f);
            var dots = new[] { new Color(1f, 0.5f, 0.69f), new Color(0.5f, 0.78f, 1f), new Color(1f, 0.83f, 0.42f), new Color(1f, 0.61f, 0.23f), new Color(0.79f, 0.71f, 1f) };
            for (int i = 0; i < 160; i++) Blob(R() * TileSize, R() * TileSize, 1.5f + R() * 2f, dots[rng.Next(dots.Length)], 0.9f);
            for (int i = 0; i < 26; i++)
            {
                float x = R() * TileSize, y = R() * TileSize, r = 18f + R() * 26f;
                for (int k = 0; k < 6; k++) { float a = R() * 6.28f, d = R() * r * 0.6f; Blob(x + Mathf.Cos(a) * d, y + Mathf.Sin(a) * d, r * (0.5f + R() * 0.5f), new Color(0.09f, 0.26f, 0.21f), 0.85f); }
                Blob(x - r * 0.25f, y + r * 0.3f, r * 0.35f, new Color(0.22f, 0.5f, 0.36f), 0.35f);
            }
            tex.SetPixels(px); tex.Apply();
            return tex;
        }
    }
}

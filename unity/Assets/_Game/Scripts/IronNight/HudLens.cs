using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IronNight
{
    /// <summary>
    /// Rain on the lens: on a rainy night drops gather on the glass between the world and the HUD, some small, a few
    /// big; the big ones swell and run down, drawn out, leaving a trail of beads, and all of them dry away. A drop is a
    /// bead of water painted in code: a faint darkening, a darker rim, the light gathered in its lower half and a glint
    /// up and to the left. Scaled time, so they slow with the killcam and stand still with the fight in photo mode. They
    /// live on a canvas of their own under the HUD, so their moving never rebuilds it. None in the snow, none on low
    /// quality (the rain is off there) and none in the menus: the Battle says when (LensRain).
    /// </summary>
    public partial class Hud
    {
        class Drop { public Image img; public RectTransform rt; public float age, life, size, slideAt, speed, trailGap; public bool bead; }
        readonly List<Drop> drops = new List<Drop>(); readonly Stack<Image> dropPool = new Stack<Image>(); RectTransform lens; float dropIn; static Sprite dropSprite;
        public bool LensRain;

        void BuildLens(Transform canvasT)
        {
            var go = new GameObject("Lens", typeof(RectTransform), typeof(Canvas)); go.transform.SetParent(canvasT, false); Stretch(go);
            go.transform.SetSiblingIndex(0); lens = (RectTransform)go.transform;
        }

        void TickLens(float dt)
        {
            if (lens == null || canvasRect == null) return;
            float W = canvasRect.rect.width, H = canvasRect.rect.height;
            if (LensRain && drops.Count < 48 && (dropIn -= dt) <= 0f)
            {
                dropIn = Random.Range(0.16f, 0.45f);
                SpawnDrop(new Vector2(Random.Range(0.04f, 0.96f) * W, Random.Range(0.1f, 0.97f) * H), Random.value < 0.15f ? Random.Range(30f, 46f) : Random.Range(9f, 26f), false);
            }
            for (int i = drops.Count - 1; i >= 0; i--)
            {
                var d = drops[i]; d.age += dt; if (!LensRain) d.life = Mathf.Min(d.life, d.age + 1.5f);   // the night over: they dry
                var p = d.rt.anchoredPosition;
                if (d.slideAt > 0f && d.age > d.slideAt)
                {
                    // heavy enough, it runs: faster and faster, wavering, drawn out, beads left behind it
                    d.speed = Mathf.Min(d.speed + dt * 380f, 260f); float dy = d.speed * dt; p.y -= dy; p.x += Mathf.Sin(d.age * 7f + i) * dt * 14f;
                    d.trailGap -= dy; if (d.trailGap <= 0f && drops.Count < 64) { d.trailGap = Random.Range(10f, 22f); SpawnDrop(p + new Vector2(0f, d.size * 0.35f), d.size * Random.Range(0.22f, 0.36f), true); }
                    d.rt.localScale = new Vector3(0.9f, 1.2f, 1f); d.rt.anchoredPosition = p; d.life = Mathf.Max(d.life, d.age + 1f);
                }
                float a = Mathf.Clamp01(d.age / 0.3f) * Mathf.Clamp01((d.life - d.age) / 1.2f);
                d.img.color = new Color(1f, 1f, 1f, a);
                if (d.age >= d.life || p.y < -60f) { d.img.gameObject.SetActive(false); dropPool.Push(d.img); drops.RemoveAt(i); }
            }
        }

        void SpawnDrop(Vector2 at, float size, bool bead)
        {
            Image img;
            if (dropPool.Count > 0) { img = dropPool.Pop(); img.gameObject.SetActive(true); }
            else { img = MakeImage(lens, "Drop", Vector2.zero, Vector2.zero, new Vector2(20f, 20f), Color.white); img.sprite = DropSprite(); img.rectTransform.pivot = new Vector2(0.5f, 0.5f); }
            var d = new Drop { img = img, rt = img.rectTransform, size = size, bead = bead, trailGap = 14f, life = bead ? Random.Range(2f, 4f) : Random.Range(4f, 8f) };
            d.slideAt = !bead && size > 20f && Random.value < 0.55f ? Random.Range(0.6f, 2.8f) : -1f;
            d.rt.anchoredPosition = at; d.rt.sizeDelta = new Vector2(size, size * 1.06f); d.rt.localScale = Vector3.one; img.color = new Color(1f, 1f, 1f, 0f);
            drops.Add(d);
        }

        /// <summary>A bead of water, 64 pixels: a faint darkening, a darker rim, light gathered low inside, a glint up and left.</summary>
        static Sprite DropSprite()
        {
            if (dropSprite != null) return dropSprite;
            const int S = 64; var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp }; var px = new Color[S * S];
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float u = (x + 0.5f) / S * 2f - 1f, v = (y + 0.5f) / S * 2f - 1f, d = Mathf.Sqrt(u * u + v * v);
                if (d >= 1f) { px[y * S + x] = new Color(1f, 1f, 1f, 0f); continue; }
                float edge = Mathf.Clamp01((1f - d) / 0.12f), rim = Mathf.Clamp01(1f - Mathf.Abs(d - 0.86f) / 0.12f);
                float low = Mathf.Clamp01((-v - 0.25f) / 0.5f) * Mathf.Clamp01(1f - Mathf.Abs(d - 0.62f) / 0.25f);
                float spec = Mathf.Clamp01(1f - new Vector2(u + 0.32f, v - 0.36f).magnitude / 0.2f);
                float lum = 0.12f + 0.55f * low + 1.1f * spec * spec - 0.1f * rim, a = edge * (0.22f + 0.35f * rim + 0.45f * low + 0.9f * spec);
                px[y * S + x] = new Color(lum, lum * 1.02f, lum * 1.06f, Mathf.Clamp01(a));
            }
            tex.SetPixels(px); tex.Apply();
            return dropSprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}

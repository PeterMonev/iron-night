using UnityEngine;
using UnityEngine.UI;

namespace IronNight
{
    /// <summary>
    /// The settings sheet's two volume sliders, Sound and Music, each a row like the switches under them: the name at the
    /// left, a track with an amber fill and a round handle, the level in per cent at the right. Dragging sets
    /// Sfx.SoundVolume or Sfx.MusicVolume at once, so the level is heard while it is set.
    /// </summary>
    public partial class Hud
    {
        Slider soundSlider, musicSlider; Text soundPct, musicPct; static Sprite knob;   // knob: a disc, a rounded square all corner

        /// <summary>A volume row centred at y on the settings sheet.</summary>
        Slider VolumeRow(Transform parent, string label, float y, System.Action<float> changed, out Text pct)
        {
            var ink = new Color(0.93f, 0.91f, 0.86f); var amber = new Color(0.95f, 0.66f, 0.23f);
            var row = MakeImage(parent, "Row " + label, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(880, 104), new Color(0.08f, 0.09f, 0.1f, 0.96f)); row.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var name = MakeText(row.transform, "Label", new Vector2(0f, 0.5f), new Vector2(40f, 0f), TextAnchor.MiddleLeft, 36, ink); name.text = label; name.rectTransform.sizeDelta = new Vector2(200f, 60f);
            pct = MakeText(row.transform, "Pct", new Vector2(1f, 0.5f), new Vector2(-36f, 0f), TextAnchor.MiddleRight, 32, amber); pct.font = BoldFont(); pct.rectTransform.sizeDelta = new Vector2(120f, 60f);

            var go = new GameObject("Slider", typeof(RectTransform), typeof(Slider)); go.transform.SetParent(row.transform, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f); rt.anchoredPosition = new Vector2(20f, 0f); rt.sizeDelta = new Vector2(440f, 64f);
            var track = MakeImage(go.transform, "Track", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440f, 12f), new Color(1f, 1f, 1f, 0.14f)); track.sprite = Rounded(); track.type = Image.Type.Sliced; track.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var area = new GameObject("Fill Area", typeof(RectTransform)); area.transform.SetParent(go.transform, false);
            var ar = (RectTransform)area.transform; ar.anchorMin = new Vector2(0f, 0.5f); ar.anchorMax = new Vector2(1f, 0.5f); ar.sizeDelta = new Vector2(0f, 12f); ar.anchoredPosition = Vector2.zero;
            var fill = MakeImage(area.transform, "Fill", new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero, amber); fill.sprite = Rounded(); fill.type = Image.Type.Sliced;
            var fr = fill.rectTransform; fr.anchorMin = new Vector2(0f, 0f); fr.anchorMax = new Vector2(0f, 1f); fr.pivot = new Vector2(0f, 0.5f); fr.sizeDelta = Vector2.zero;
            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform)); handleArea.transform.SetParent(go.transform, false);
            var hr = (RectTransform)handleArea.transform; hr.anchorMin = new Vector2(0f, 0f); hr.anchorMax = new Vector2(1f, 1f); hr.sizeDelta = new Vector2(-40f, 0f); hr.anchoredPosition = Vector2.zero;
            var handle = MakeImage(handleArea.transform, "Handle", new Vector2(0f, 0.5f), Vector2.zero, new Vector2(44f, 44f), ink); handle.sprite = knob ??= Lightswarm.ProceduralSprites.RoundedRect(32, 64); handle.raycastTarget = true;
            handle.rectTransform.pivot = new Vector2(0.5f, 0.5f); handle.rectTransform.anchorMin = handle.rectTransform.anchorMax = new Vector2(0f, 0.5f); handle.rectTransform.sizeDelta = new Vector2(44f, 44f);
            track.raycastTarget = true;   // a press anywhere on the track moves the handle there

            var s = go.GetComponent<Slider>(); s.fillRect = fr; s.handleRect = handle.rectTransform; s.targetGraphic = handle; s.direction = Slider.Direction.LeftToRight; s.minValue = 0f; s.maxValue = 1f;
            var nav = s.navigation; nav.mode = Navigation.Mode.None; s.navigation = nav;
            var t = pct; s.onValueChanged.AddListener(v => { changed(v); t.text = Mathf.RoundToInt(v * 100f) + "%"; });
            return s;
        }

        /// <summary>The sliders set to the levels kept, without sounding a change.</summary>
        void RefreshVolumes()
        {
            if (soundSlider != null) { soundSlider.SetValueWithoutNotify(Sfx.SoundVolume); soundPct.text = Mathf.RoundToInt(Sfx.SoundVolume * 100f) + "%"; }
            if (musicSlider != null) { musicSlider.SetValueWithoutNotify(Sfx.MusicVolume); musicPct.text = Mathf.RoundToInt(Sfx.MusicVolume * 100f) + "%"; }
        }
    }
}

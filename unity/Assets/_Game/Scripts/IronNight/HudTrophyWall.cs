using UnityEngine;
using UnityEngine.UI;

namespace IronNight
{
    /// <summary>
    /// The trophy wall seen up close, from MOST WANTED: the hangar's camera walks up to the hunt board, where the dead
    /// aces' prints are crossed out and their trophies hang over them (GarageLife.HangTrophy). The picture is the title's
    /// own camera, so the tilt of the phone still moves it; BACK walks back to the title's framing.
    /// </summary>
    public partial class Hud
    {
        GameObject wallSheet;

        void ShowTrophyWall()
        {
            if (garage == null) return;
            if (wallSheet != null) Destroy(wallSheet);
            wallSheet = new GameObject("TrophyWall", typeof(RectTransform)); wallSheet.transform.SetParent(canvas.transform, false); Stretch(wallSheet);
            if (curtain != null) wallSheet.transform.SetSiblingIndex(curtain.transform.GetSiblingIndex());   // under the curtain, over MOST WANTED
            var t = wallSheet.transform;
            var view = new GameObject("View", typeof(RectTransform), typeof(RawImage)); view.transform.SetParent(t, false); Stretch(view);
            var raw = view.GetComponent<RawImage>(); raw.texture = garage.TitleTexture; raw.raycastTarget = true;   // takes the taps: nothing under it answers
            var top = MakeImage(t, "TopShade", new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(1400f, 380f), new Color(0.01f, 0.01f, 0.02f, 0.75f)); top.sprite = Lightswarm.ProceduralSprites.GradientDown(128, 1.6f);
            top.rectTransform.pivot = new Vector2(0.5f, 0.5f); top.rectTransform.localScale = new Vector3(1f, -1f, 1f);
            MakeGhost(t, "BACK", new Vector2(0f, 1f), new Vector2(130, -95), new Vector2(200, 80), 26, CloseTrophyWall);
            var ey = MakeText(t, "Eyebrow", new Vector2(0.5f, 1f), new Vector2(0f, -170f), TextAnchor.MiddleCenter, 26, OpAmber); ey.text = Spaced("TROPHY WALL"); ey.font = LabelFont(); ey.rectTransform.pivot = new Vector2(0.5f, 0.5f); ey.rectTransform.sizeDelta = new Vector2(700f, 44f);
            int n = Nemesis.Dead.Count;
            var ln = MakeText(t, "Line", new Vector2(0.5f, 1f), new Vector2(0f, -220f), TextAnchor.MiddleCenter, 26, OpDim); ln.rectTransform.pivot = new Vector2(0.5f, 0.5f); ln.rectTransform.sizeDelta = new Vector2(900f, 44f);
            ln.text = n + (n == 1 ? " ace of eight" : " aces of eight") + " crossed off · their trophies on the board";
            garage.ViewBoard(true); Sfx.Whoosh();
        }

        void CloseTrophyWall()
        {
            if (wallSheet != null) Destroy(wallSheet);
            wallSheet = null; if (garage != null) garage.ViewBoard(false);
        }
    }
}

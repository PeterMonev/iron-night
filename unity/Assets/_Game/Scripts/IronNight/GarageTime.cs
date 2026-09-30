using UnityEngine;

namespace IronNight
{
    /// <summary>
    /// The hangar by the clock: the open door shows the player's own hour and lets its light in. Morning, a low gold sun
    /// through the mist; day, a pale overcast; evening, a red sunset; night, the moon and the searchlights (the first
    /// picture). The pictures are Textures/door_&lt;morning|day|evening|night&gt;; while one is missing the night stays,
    /// light and all. Looked at again every half minute, so a menu left open turns with the day.
    /// </summary>
    public partial class Garage
    {
        Material doorMat, hazeMat; Light doorLight, ambLight; string doorTime; float doorLook;

        public static string TimeOfDay { get { int h = System.DateTime.Now.Hour; return h >= 5 && h < 10 ? "morning" : h >= 10 && h < 17 ? "day" : h >= 17 && h < 21 ? "evening" : "night"; } }

        void TickDoor(bool now = false)
        {
            if (!now && (doorLook -= Time.unscaledDeltaTime) > 0f) return;
            doorLook = 30f;
            string want = TimeOfDay; var tex = Resources.Load<Texture2D>("Textures/door_" + want);
            if (tex == null) { want = "night"; tex = Resources.Load<Texture2D>("Textures/door_night"); }
            if (want == doorTime) return;
            doorTime = want;
            if (doorMat != null && tex != null && doorMat.HasProperty("_MainTex")) doorMat.SetTexture("_MainTex", tex);
            Color light, haze; float power, fill;
            switch (want)
            {
                case "morning": light = new Color(1f, 0.8f, 0.55f); power = 14f; fill = 36f; haze = new Color(0.45f, 0.32f, 0.16f); break;
                case "day": light = new Color(0.92f, 0.95f, 1f); power = 18f; fill = 44f; haze = new Color(0.34f, 0.36f, 0.38f); break;
                case "evening": light = new Color(1f, 0.55f, 0.3f); power = 13f; fill = 32f; haze = new Color(0.5f, 0.22f, 0.1f); break;
                default: light = new Color(0.55f, 0.66f, 1f); power = 8f; fill = 30f; haze = new Color(0.16f, 0.22f, 0.4f); break;
            }
            if (doorLight != null) { doorLight.color = light; doorLight.intensity = power; }
            if (ambLight != null) ambLight.intensity = fill;
            if (hazeMat != null) hazeMat.SetColor("_BaseColor", haze);
        }
    }
}

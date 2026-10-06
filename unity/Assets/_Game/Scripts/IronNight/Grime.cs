using UnityEngine;

namespace IronNight
{
    /// <summary>
    /// What the night leaves on a tank (IronNight/Grime, a second pass over every lit part): mud, dust or snow by the
    /// front, soot and scrapes by the hits. In the night each tank gets it as it goes (Battle); a tank of ours keeps its
    /// soot and its dirt back to the hangar (Wear) until the mechanics have been at it.
    /// </summary>
    public class Grime : MonoBehaviour
    {
        static Shader shader; static Texture2D noise;
        Material mat;

        /// <summary>The grime pass on every lit part of a vehicle, clean to start with; null without the shader.</summary>
        public static Grime Dress(GameObject root)
        {
            if (shader == null) shader = Resources.Load<Shader>("Shaders/Grime"); if (shader == null) return null;
            if (noise == null) noise = Fx.FlameNoise(128);
            var g = root.AddComponent<Grime>(); g.mat = new Material(shader); g.mat.SetTexture("_Noise", noise);
            float top = 0.5f;
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>())
            {
                var mats = r.sharedMaterials; if (mats.Length == 0 || mats[0] == null || !mats[0].shader.name.Contains("Lit")) continue;   // the paint, not the rings or the numbers
                var more = new Material[mats.Length + 1]; mats.CopyTo(more, 0); more[mats.Length] = g.mat; r.sharedMaterials = more;
                top = Mathf.Max(top, r.bounds.max.y - root.transform.position.y);
            }
            g.mat.SetFloat("_Height", top); g.LateUpdate(); return g;
        }

        /// <summary>How much of each: mud or dust (its factor: below 1 darkens, above lightens), snow, soot and scrapes.</summary>
        public void Set(float mud, Color tint, float snow, float soot)
        {
            mat.SetFloat("_Mud", Mathf.Clamp01(mud)); mat.SetColor("_MudTint", tint); mat.SetFloat("_Snow", Mathf.Clamp01(snow)); mat.SetFloat("_Soot", Mathf.Clamp01(soot));
        }

        void LateUpdate() { if (mat != null) mat.SetMatrix("_Root", transform.worldToLocalMatrix); }
        void OnDestroy() { if (mat != null) Destroy(mat); }

        /// <summary>A front's dirt: its colour factor, how much mud or dust, how much snow.</summary>
        public static void Front(string theatre, bool wet, out Color tint, out float mud, out float snow)
        {
            switch (theatre)
            {
                case "ardennes": tint = new Color(0.62f, 0.55f, 0.46f); mud = 0.35f; snow = 0.9f; break;
                case "kursk": tint = new Color(1.25f, 1.12f, 0.9f); mud = 0.65f; snow = 0f; break;   // the steppe's pale dust
                case "italy": tint = new Color(1.2f, 0.95f, 0.78f); mud = 0.6f; snow = 0f; break;   // red Italian dust
                default: tint = new Color(0.6f, 0.5f, 0.38f); mud = 0.7f; snow = 0f; break;   // Norman mud
            }
            if (wet && snow == 0f) { tint = new Color(0.55f, 0.46f, 0.36f); mud = Mathf.Min(1f, mud + 0.25f); }   // rain turns any dust to mud
        }
    }

    /// <summary>A tank of ours as it came back from its last night: soot and dirt, till the mechanics are done with it.</summary>
    public static class Wear
    {
        static string K(string id, string what) => "wear." + id + "." + what;
        /// <summary>The night's marks on a tank, and the time they stay (the end of a repair, or half an hour).</summary>
        public static void Record(string id, float soot, float mud, string theatre, bool wet, long untilTicks)
        {
            if (string.IsNullOrEmpty(id)) return;
            PlayerPrefs.SetFloat(K(id, "soot"), Mathf.Clamp01(soot)); PlayerPrefs.SetFloat(K(id, "mud"), Mathf.Clamp01(mud)); PlayerPrefs.SetString(K(id, "front"), theatre);
            PlayerPrefs.SetInt(K(id, "wet"), wet ? 1 : 0); PlayerPrefs.SetString(K(id, "until"), untilTicks.ToString()); PlayerPrefs.Save();
        }
        /// <summary>The soot still on a tank: none once its repair is over (the dirt stays till the next night).</summary>
        public static float Soot(string id)
        {
            if (!long.TryParse(PlayerPrefs.GetString(K(id, "until"), "0"), out long until) || GameClock.UtcNow.Ticks >= until) return 0f;
            return PlayerPrefs.GetFloat(K(id, "soot"), 0f);
        }
        /// <summary>A tank of ours dressed as it came back.</summary>
        public static void Dress(Vehicle v, string id)
        {
            var g = v.GetComponent<Grime>(); if (g == null) return;
            Grime.Front(PlayerPrefs.GetString(K(id, "front"), "normandy"), PlayerPrefs.GetInt(K(id, "wet"), 0) == 1, out var tint, out _, out float snow);
            float mud = PlayerPrefs.GetFloat(K(id, "mud"), 0f);
            g.Set(mud, tint, snow * (mud > 0f ? 1f : 0f), Soot(id));
        }
    }
}

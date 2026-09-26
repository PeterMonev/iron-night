using UnityEngine;
using UnityEngine.SceneManagement;

namespace IronNight
{
    /// <summary>A war map night's end (the sector is set in Build from the map's launch).</summary>
    public partial class Battle
    {
        /// <summary>Held until dawn: the sector is taken (or held against the counterattack) and paid for, once, and the
        /// next sector along the road is offered. Lost: the same sector again.</summary>
        void MapEnd(bool dawn, string statLine)
        {
            var s = mapSector; string line = statLine; bool defended = Campaign.Counter == s;
            if (dawn && !mapPaid) { mapPaid = true; var won = Campaign.Win(s); if (won != null) line += "\n" + won; }
            var next = dawn ? Campaign.Next(s) : null;
            hud.ShowEnd(dawn, line, dawn ? !doubled : !revived, "ROAD TO BERLIN · " + s.name.ToUpperInvariant(),
                dawn ? (defended ? "Line held" : s.id == "berlin" ? "Victory" : "Liberated") : "Attack failed",
                dawn ? (next != null ? "On to " + next.name : "Back to the map") : "Attack again");
            hud.ShowHold(false);
            hud.OnAgain = () =>
            {
                var go = dawn ? next : s;
                if (go != null) PlayerPrefs.SetString("map.launch", go.id); else PlayerPrefs.SetInt("map.open", 1);
                PlayerPrefs.Save(); SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            };
        }
    }
}

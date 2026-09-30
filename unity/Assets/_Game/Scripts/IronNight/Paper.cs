using System.Globalization;
using UnityEngine;

namespace IronNight
{
    /// <summary>
    /// The morning paper: the night told as the next morning's front page of a field newspaper, THE FRONT LINE COURIER
    /// for the Americans and THE STEEL FRONT for the Soviets (both made up). Dated in the campaign's own month, a day
    /// for each night fought; the headline, the story and the caption made from what happened (a boss or an ace killed
    /// first, then the dawn held or lost, the raid, the convoy, the stand); the commander and the gunner by name. The
    /// picture is the night's best moment if the killcam caught one, else the camera as the night ended. Handed to the
    /// HUD as the end sheet comes up (Hud.SetPaper); its button there opens the page.
    /// </summary>
    public partial class Battle
    {
        public class PaperStory { public string masthead, dateline, headline, sub, body, caption; public Texture picture; }

        PaperStory MorningPaper(bool dawn)
        {
            bool su = theatre == "kursk"; var inv = CultureInfo.InvariantCulture;
            var p = new PaperStory { masthead = su ? "THE STEEL FRONT" : "THE FRONT LINE COURIER" };
            var date = new System.DateTime(su ? 1943 : 1944, su ? 7 : winter ? 12 : 7, 1 + Depot.NightsFought % 28);
            p.dateline = (NightPlace + " · " + date.ToString("dddd, MMMM d, yyyy", inv) + " · field edition").ToUpperInvariant();
            string tanks = su ? "T-34s" : "Shermans", enemy = kills == 1 ? "one enemy tank" : kills + " enemy tanks";
            string ground = route == "village" ? "the village" : route == "bocage" ? (su ? "the tree belts" : "the hedgerows") : (su ? "the steppe" : "the open fields");
            string who = commander != null ? commander.name : "their commander", gunner = Crew.Chosen(Depot.Nation, "gunner")?.name ?? "the gunner";
            string tiger = nightTigers > 0 ? (nightTigers == 1 ? ", a Tiger among them" : ", " + nightTigers + " Tigers among them") : "";

            if (bossKilled) { p.headline = "TIGER ACE BURNS NEAR " + NightPlace.ToUpperInvariant(); p.sub = "The feared German tank is destroyed" + (dawn ? " before dawn" : " in the night"); }
            else if (nightAces > 0 && nem != null) { p.headline = nem.Title.ToUpperInvariant() + (string.IsNullOrEmpty(nem.nick) ? "" : ", " + nem.nick.ToUpperInvariant() + ",") + " IS DEAD"; p.sub = "An enemy ace falls to our tanks near " + NightPlace; }
            else if (stand) { p.headline = dawn ? "THE LAST STAND HOLDS" : "THE LAST STAND FALLS"; p.sub = enemy + " stopped" + tiger + (dawn ? "; the line is still there at first light" : " before the position was overrun"); }
            else if (convoy) { p.headline = dawn ? "CONVOY GETS THROUGH THE NIGHT" : "CONVOY AMBUSHED IN THE DARK"; p.sub = "Its escort of " + tanks + " destroys " + enemy + tiger; }
            else if (sneak) { p.headline = dawn ? "NIGHT RAIDERS STRIKE BEHIND THE LINES" : "NIGHT RAID MEETS HEAVY FIRE"; p.sub = enemy + " destroyed" + tiger; }
            else if (dawn && kills >= 10) { p.headline = "PLATOON HOLDS " + ground.ToUpperInvariant() + " TILL DAWN"; p.sub = char.ToUpperInvariant(enemy[0]) + enemy.Substring(1) + " destroyed" + tiger; }
            else if (dawn) { p.headline = "THE LINE HOLDS THROUGH THE NIGHT"; p.sub = char.ToUpperInvariant(enemy[0]) + enemy.Substring(1) + " and " + nightInfantry + " infantry stopped" + tiger; }
            else { p.headline = "TANKS FALL BACK AFTER A NIGHT OF FIGHTING"; p.sub = char.ToUpperInvariant(enemy[0]) + enemy.Substring(1) + " knocked out before the leader was hit" + tiger; }

            var sb = new System.Text.StringBuilder();
            sb.Append(NightPlace).Append(", ").Append(date.ToString("MMMM d", inv)).Append(". — Under cover of darkness a platoon of ").Append(tanks).Append(" led by ").Append(who)
              .Append(stand ? " dug in and waited" : " went into ").Append(stand ? "" : ground).Append(" last night. ");
            if (kills > 0) sb.Append(gunner).Append("'s gun accounted for most of the ").Append(enemy).Append(" destroyed").Append(tiger).Append(". ");
            if (nightInfantry > 0) sb.Append(nightInfantry).Append(nightInfantry == 1 ? " tank hunter was" : " tank hunters were").Append(" stopped before they could fire. ");
            if (objectivesReached > 0) sb.Append("The platoon took ").Append(objectivesReached == 1 ? "its objective" : objectivesReached + " objectives").Append(" on the way. ");
            if (wingmenLost > 0) sb.Append(wingmenLost == 1 ? "One tank of the platoon was lost. " : wingmenLost + " tanks of the platoon were lost. ");
            sb.Append(dawn ? "At first light they were still on the field, and our correspondent found the men tired but in good spirits." : "Before dawn the leader's tank was knocked out; its crew got out and are safe behind the lines.");
            p.body = sb.ToString();

            // the picture: the killcam's best moment, or the camera as the night ended
            if (momentFrames != null && momentTaken > 0) { p.picture = momentFrames[Mathf.Clamp(momentTaken / 2, 0, momentFrames.Length - 1)]; p.caption = (string.IsNullOrEmpty(momentTitle) ? "The moment it happened" : momentTitle) + ", " + NightPlace + "."; }
            else if (cam != null)
            {
                var rt = new RenderTexture(768, 512, 24); var was = cam.targetTexture; cam.targetTexture = rt; cam.Render(); cam.targetTexture = was;
                p.picture = rt; p.caption = "The platoon" + (dawn ? " at first light" : " as the night ended") + ", " + NightPlace + ".";
            }
            return p;
        }
    }
}

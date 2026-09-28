using UnityEngine;
using UnityEngine.InputSystem;

namespace IronNight
{
    /// <summary>
    /// The camera's big moments, timed in real seconds whatever the slow motion. The opening shot: low behind the leader
    /// with the front's name over the dark, then a crane up to the platoon's view (a tap skips it). The killcam: a big kill
    /// (the boss, an ace, a King Tiger, now and then a Tiger or a Panther) seen from low by the wreck in slow motion, the
    /// camera swinging round it under a caption. The orbit: the leader, or what is left of him, circled slowly behind the
    /// end sheet. While a shot runs it has the camera and the HUD steps aside; the platoon's view comes back with a swoop.
    /// </summary>
    public partial class Battle
    {
        enum CamShot { None, Intro, Kill, Orbit }
        CamShot camShot; float shotT, shotLen, shotIn, shotYaw, shotSpin, shotLift, backLeft, backLen, killRest, introSide, introHigh; bool introBegun, levelWaiting;
        Vector3 shotAt, leaderAt, fromPos; Quaternion fromRot; float fromFov; Vehicle shotFollow;   // a moving tank the shot keeps in the middle (an ace arriving)
        const float Fov = 50f, IntroLen = 5.6f, IntroCrane = 2.9f, KillLen = 2.6f;
        static readonly Quaternion TopRot = Quaternion.LookRotation(new Vector3(0f, -44f, 40f));
        static readonly Vector2[] IntroSpots = { new Vector2(1.3f, 3.4f), new Vector2(-1.8f, 3.4f), new Vector2(4f, 3.8f), new Vector2(-4f, 3.8f), new Vector2(1.2f, 7f) };   // across and up from behind the leader, tried in turn until the view is clear

        static Vector3 TopPos(Vehicle L) => L.transform.position + new Vector3(0f, 52f, -42.5f);   // the platoon's view, as PlaceCamera keeps it
        static Vector3 TopAim(Vehicle L) => L.transform.position + new Vector3(0f, 0f, 4.77f);   // the ground it looks at
        static float Smooth(float x) => x * x * (3f - 2f * x);
        static float Smoother(float x) => x * x * x * (x * (x * 6f - 15f) + 10f);

        /// <summary>The play begins: the thumb drives, the first objective is briefed.</summary>
        void Begin() { phase = Phase.Play; stick.Blocked = false; Sfx.Theme("battle"); hud.SetAmmo(apRounds, heRounds, loadHe); if (objective != null) Brief(objective.kind); if (suppliesGranted) { suppliesGranted = false; OfferCards(CardPool(), "SUPPLIES FOR THE NIGHT"); } }   // the racks shown from the start, not after the first shot, and the card of the supplies chosen

        /// <summary>The night's opening shot. False when there is nothing to film, and the night simply begins.</summary>
        bool IntroShot()
        {
            var L = Leader; if (L == null || cam == null) return false;
            phase = Phase.Intro; stick.Blocked = true; introBegun = false;
            var o = L.transform.position; var f = L.Forward; var rt = new Vector3(f.z, 0f, -f.x); var aim = o + Vector3.up * 1.6f;
            introSide = IntroSpots[0].x; introHigh = IntroSpots[0].y;
            foreach (var s in IntroSpots)
            {
                var from = o - f * 15f + rt * s.x + Vector3.up * s.y; var to = o - f * 12f + rt * (s.x * 0.75f) + Vector3.up * (s.y + 0.4f);
                if (props.SightClear(from, aim) && props.SightClear(to, aim)) { introSide = s.x; introHigh = s.y; break; }
            }
            StartShot(CamShot.Intro, IntroLen, 0f);
            IntroWords(out var eyebrow, out var title, out var sub);
            hud.Cinema(true, false, true); hud.CineCard(eyebrow, title, sub, 0.45f, 3.2f, false); hud.CineSkip(true);
            return true;
        }

        /// <summary>The words over the opening shot: what kind of night, then the front (an operation's night, or the
        /// day's rule, when there is one), then the country and the month, the way in and the weather.</summary>
        void IntroWords(out string eyebrow, out string title, out string sub)
        {
            string front = TheatreName.ToUpperInvariant();
            eyebrow = stand ? "LAST STAND" : convoy ? "CONVOY" : sneak ? "NIGHT RAID" : daily ? "DAILY CHALLENGE" : opNight > 0 ? op.name.ToUpperInvariant() + " · NIGHT " + opNight + " OF " + op.nights.Length : "NIGHT ASSAULT";
            title = opNight > 0 ? opN.name.ToUpperInvariant() : daily ? Daily.RuleOf(dailyDay).name.ToUpperInvariant() : front;
            string place = title != front ? front : theatre == "kursk" ? "SOVIET UNION" : winter ? "BELGIUM" : "FRANCE";
            string when = theatre == "kursk" ? "JULY 1943" : winter ? "DECEMBER 1944" : "JULY 1944";
            string way = route == "village" ? "VILLAGE" : route == "bocage" ? (theatre == "kursk" ? "TREE BELTS" : "BOCAGE") : (theatre == "kursk" ? "STEPPE" : "OPEN FIELDS");
            if (mapSector != null) { eyebrow = "ROAD TO BERLIN" + (Campaign.Counter == mapSector ? " · COUNTERATTACK" : ""); title = mapSector.name.ToUpperInvariant(); place = mapSector.country; when = mapSector.when; }   // a war map sector: its own name and date
            string sky = weather == Weather.Fog ? " · FOG" : weather == Weather.Rain ? (winter ? " · SNOW" : " · RAIN") : "";
            sub = place + " · " + when + " · " + way + sky;
        }

        /// <summary>A big kill: the killcam, when the moment is free and the kill is worth it. Points: what it paid.</summary>
        void Killcam(Vehicle v, int points)
        {
            if (phase != Phase.Play || camShot != CamShot.None || backLeft > 0f || v == null) return;
            // the boss and a named ace always; a King Tiger, or now and then a Tiger or a Panther, when the last one was a while ago
            bool must = v == boss || v == ace, cat = v.spec == VehicleSpec.Tiger || v.spec == VehicleSpec.Panther || v.spec == VehicleSpec.TigerAce;
            if (!must && (killRest > 0f || !(v.spec == VehicleSpec.KingTiger || (cat && Random.value < 0.3f)))) return;
            shotAt = v.transform.position;
            // from the platoon's side of the wreck, a little round, swinging whichever way the view stays clear
            var L = Leader; var to = L != null ? L.transform.position - shotAt : -v.Forward; float face = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
            shotYaw = face + 40f; shotSpin = 26f; shotLift = 0f; var aim = shotAt + Vector3.up * 1.6f; bool found = false;
            foreach (var c in new[] { 40f, -40f, 100f, -100f, 160f, -160f })
            {
                float spin = c > 0f ? 26f : -26f;
                if (SweepClear(face + c, spin * KillLen, aim, true)) { shotYaw = face + c; shotSpin = spin; found = true; break; }
            }
            if (!found) shotLift = 8f;   // no clear way round down among the trees and houses: film it from above them
            StartShot(CamShot.Kill, KillLen, 0.4f); SlowMo(KillLen - 0.3f); stick.Blocked = true; killRest = 35f;
            string eyebrow = v == boss ? "BOSS · KNOCKED OUT" : v == ace ? "ACE · KNOCKED OUT" : "KNOCKED OUT", title = v.spec.name.ToUpperInvariant(), sub = "+" + points.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
            if (v == ace && aceName != null) { var parts = aceName.Split(new[] { " · " }, System.StringSplitOptions.None); title = parts[0].ToUpperInvariant(); sub = v.spec.name.ToUpperInvariant() + " · " + sub; }
            if (v == ace && nem != null) { eyebrow = nem.met > 1 ? "NEMESIS · KNOCKED OUT" : "ACE · KNOCKED OUT"; sub = nemEscaped ? "HE BAILED OUT · HE WILL BE BACK" : "BOUNTY +" + Nemesis.BountyPoints(nem).ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + (Nemesis.BountyGold(nem) > 0 ? " · +" + Nemesis.BountyGold(nem) + " GOLD" : ""); }
            hud.Cinema(true, false); hud.CineCard(eyebrow, title, sub, 0.2f, KillLen - 0.1f, true); hud.Toast("", 0.01f); MomentBegins(v, eyebrow, title, sub);   // the caption says it: no toast after
        }

        // ---- the moment of the night: five pictures of its best killcam, shown as a reel over the end sheet ----
        static readonly float[] MomentAt = { 0.5f, 0.9f, 1.3f, 1.7f, 2.1f };   // real seconds into the killcam, after its swoop in
        RenderTexture[] momentFrames; RenderTexture momentCapture; int momentRank = -1, momentTaken; bool momentRecording, momentShown; string momentEyebrow, momentTitle, momentSub;

        /// <summary>A killcam begins: if it beats the night's best so far, its pictures become the night's moment.</summary>
        void MomentBegins(Vehicle v, string eyebrow, string title, string sub)
        {
            int rank = v == boss ? 4 : v == ace ? 3 : v.spec == VehicleSpec.KingTiger ? 2 : 1;
            if (rank <= momentRank) return;
            momentRank = rank; momentTaken = 0; momentRecording = true;
            int m = Mathf.FloorToInt(t / 60f), s = Mathf.FloorToInt(t % 60f);
            momentEyebrow = eyebrow; momentTitle = title; momentSub = (sub + " · " + m + ":" + s.ToString("00") + " · " + TheatreName).ToUpperInvariant();
        }

        /// <summary>During the killcam, at each of its moments: a picture from the camera as it stands, without the screen's words.</summary>
        void TickMoment()
        {
            if (!momentRecording) return;
            if (momentTaken >= MomentAt.Length) { momentRecording = false; if (momentCapture != null) { momentCapture.Release(); momentCapture = null; } return; }
            if (shotT < MomentAt[momentTaken]) return;
            int w = Mathf.Max(64, Mathf.RoundToInt(Screen.width * 0.6f)), h = Mathf.Max(64, Mathf.RoundToInt(Screen.height * 0.6f));
            if (momentCapture == null || momentCapture.width != w || momentCapture.height != h) { if (momentCapture != null) momentCapture.Release(); momentCapture = new RenderTexture(w, h, 24) { antiAliasing = 2 }; }   // the one the camera draws into, smoothed as the game is
            var req = new UnityEngine.Rendering.RenderPipeline.StandardRequest { destination = momentCapture };
            if (UnityEngine.Rendering.RenderPipeline.SupportsRenderRequest(cam, req)) UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(cam, req); else { cam.targetTexture = momentCapture; cam.Render(); cam.targetTexture = null; }
            momentFrames ??= new RenderTexture[MomentAt.Length];
            var rt = momentFrames[momentTaken];
            if (rt == null || rt.width != w || rt.height != h) { if (rt != null) rt.Release(); rt = momentFrames[momentTaken] = new RenderTexture(w, h, 0); }
            Graphics.Blit(momentCapture, rt);   // resolved out of the smoothing, kept without depth
            momentTaken++;
        }

        /// <summary>The night over: its moment, once, over the end sheet, if a killcam gave it two pictures or more.</summary>
        void ShowMoment() { if (momentShown || momentRank < 0 || momentTaken < 2) return; momentShown = true; hud.ShowMoment(momentFrames, momentTaken, momentEyebrow, momentTitle, momentSub); }

        void OnDestroy() { if (momentFrames != null) foreach (var rt in momentFrames) if (rt != null) rt.Release(); if (momentCapture != null) momentCapture.Release(); }

        /// <summary>An old enemy back on the field: the camera goes to him for a moment, his name and what they call him
        /// under it. No slow motion: the night goes on.</summary>
        void NemesisCam(Vehicle v, Nemesis.Ace a)
        {
            if (phase != Phase.Play || camShot != CamShot.None || backLeft > 0f || v == null) return;
            shotAt = v.transform.position; var L = Leader; var to = L != null ? L.transform.position - shotAt : v.Forward; float face = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
            shotYaw = face + 40f; shotSpin = 26f; shotLift = 0f; var aim = shotAt + Vector3.up * 1.6f; bool found = false;
            foreach (var c in new[] { 40f, -40f, 100f, -100f }) { float spin = c > 0f ? 26f : -26f; if (SweepClear(face + c, spin * KillLen, aim, true)) { shotYaw = face + c; shotSpin = spin; found = true; break; } }
            if (!found) shotLift = 8f;
            StartShot(CamShot.Kill, KillLen, 0.45f); shotFollow = v; stick.Blocked = true;
            hud.Cinema(true, false); hud.CineCard(a.met > 2 ? "HE IS BACK · MET " + a.met + " TIMES" : "HE IS BACK", a.Title.ToUpperInvariant(), (string.IsNullOrEmpty(a.nick) ? Nemesis.Tank(a).name : a.nick).ToUpperInvariant(), 0.2f, KillLen - 0.1f, true);
        }

        /// <summary>Whether the camera's swing, from a bearing through so many degrees, keeps a clear view of the aim and
        /// stays out of houses and tree crowns all the way (five looks along it).</summary>
        bool SweepClear(float yaw0, float sweep, Vector3 aim, bool kill)
        {
            for (int i = 0; i <= 4; i++) { float k = i / 4f, y = yaw0 + sweep * k; var at = kill ? KillSpot(y, k) : OrbitSpot(y); if (!props.SightClear(at, aim) || (kill && !TanksClear(at, aim))) return false; }
            return true;
        }

        /// <summary>No tank, ours or theirs, standing in the way of a low look at the wreck.</summary>
        bool TanksClear(Vector3 from, Vector3 to)
        {
            for (int i = 1; i <= 12; i++)
            {
                var q = Vector3.Lerp(from, to, i / 13f); if (q.y > 3.4f) continue;
                foreach (var e in foes) { var d = e.transform.position - q; if (!e.dead && d.x * d.x + d.z * d.z < e.spec.radius * e.spec.radius) return false; }
                foreach (var e in platoon) { var d = e.transform.position - q; if (d.x * d.x + d.z * d.z < e.spec.radius * e.spec.radius) return false; }
            }
            return true;
        }

        /// <summary>Where the killcam stands, k of the way through its swing: drifting back and up as it goes round.</summary>
        Vector3 KillSpot(float yawDeg, float k)
        {
            float a = yawDeg * Mathf.Deg2Rad, r = Mathf.Lerp(19f, 22f, k);
            return shotAt + new Vector3(Mathf.Sin(a) * r, Mathf.Lerp(4.2f, 6.5f, k) + shotLift, Mathf.Cos(a) * r);
        }

        /// <summary>Where the end's orbit stands at a bearing: high and wide round the leader.</summary>
        Vector3 OrbitSpot(float yawDeg) { float a = yawDeg * Mathf.Deg2Rad; return shotAt + new Vector3(Mathf.Sin(a) * 28f, 12f + shotLift, Mathf.Cos(a) * 28f); }   // just over the tree crowns, so none comes up in front of the lens

        /// <summary>The night is over: the camera leaves the platoon's view and circles the leader, or his wreck, slowly.</summary>
        void EndOrbit()
        {
            if (cam == null) return;
            StartShot(CamShot.Orbit, 0f, 1.6f); shotAt = leaderAt; shotYaw = 200f; shotSpin = 4.5f; shotLift = 0f; bool found = false;
            // the first bearing, and the way round, from which the next quarter of a minute sees him clear
            var aim = shotAt + Vector3.up * 1.3f;
            foreach (var y in new[] { 200f, 160f, 240f, 120f, 280f, 80f })
            {
                if (SweepClear(y, 60f, aim, false)) { shotYaw = y; shotSpin = 4.5f; found = true; break; }
                if (SweepClear(y, -60f, aim, false)) { shotYaw = y; shotSpin = -4.5f; found = true; break; }
            }
            if (!found) shotLift = 6f;
            hud.Cinema(false, false);
        }

        void StartShot(CamShot s, float len, float swoop)
        {
            camShot = s; shotT = 0f; shotLen = len; shotIn = swoop; backLeft = 0f; shotFollow = null;
            fromPos = cam.transform.position; fromRot = cam.transform.rotation; fromFov = cam.fieldOfView;
        }

        /// <summary>The shot is over; the camera swoops back to the platoon's view from wherever the shot left it.</summary>
        void EndShot(float back)
        {
            fromPos = cam.transform.position; fromRot = cam.transform.rotation; fromFov = cam.fieldOfView;
            camShot = CamShot.None; backLeft = backLen = back;
        }

        /// <summary>A tap (on its release, so it drives nothing), the space bar or Escape: the opening is cut short.</summary>
        static bool SkipPressed()
        {
            var ts = Touchscreen.current; if (ts != null && ts.primaryTouch.press.wasReleasedThisFrame) return true;
            var m = Mouse.current; if (m != null && Application.isFocused && m.leftButton.wasReleasedThisFrame) return true;
            var kb = Keyboard.current; return kb != null && (kb.spaceKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame);
        }

        /// <summary>Runs the camera while a shot has it (true), or while it swoops back from one. Called by PlaceCamera.</summary>
        bool TickShot(Vehicle L)
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f); if (killRest > 0f) killRest -= dt;
            if (camShot == CamShot.None)
            {
                if (backLeft <= 0f || L == null) return false;
                backLeft -= dt; float k = Smoother(1f - Mathf.Max(0f, backLeft) / backLen);
                cam.transform.SetPositionAndRotation(Vector3.Lerp(fromPos, TopPos(L), k), Quaternion.Slerp(fromRot, TopRot, k)); cam.fieldOfView = Mathf.Lerp(fromFov, Fov, k);
                flareLight.transform.position = L.transform.position + Vector3.up * 11f;
                return true;
            }
            if (camShot == CamShot.Intro && L == null) { camShot = CamShot.None; cam.fieldOfView = Fov; return false; }
            if (camShot == CamShot.Intro && SkipPressed())
            {
                // cut short: the words go, the HUD comes, the camera swoops up to the platoon from where it is
                hud.CineCardOut(); hud.Cinema(false, true); hud.CineSkip(false); if (!introBegun) { introBegun = true; Begin(); }
                EndShot(0.9f); return TickShot(L);
            }
            shotT += dt;
            Vector3 p; Quaternion r; float fov;
            if (camShot == CamShot.Intro) IntroPose(L, out p, out r, out fov);
            else if (camShot == CamShot.Kill)
            {
                if (shotFollow != null && !shotFollow.dead) shotAt = Vector3.Lerp(shotAt, shotFollow.transform.position, 1f - Mathf.Exp(-dt * 6f));
                p = KillSpot(shotYaw + shotSpin * shotT, Mathf.Clamp01(shotT / KillLen));
                r = Quaternion.LookRotation(shotAt + Vector3.up * 1.6f - p); fov = 50f;
            }
            else
            {
                // high and wide, looking a little under the leader: he rides at the top of the screen, above the words
                p = OrbitSpot(shotYaw + shotSpin * shotT);
                r = Quaternion.LookRotation(shotAt + Vector3.up * 1.3f - p) * Quaternion.Euler(17f, 0f, 0f); fov = 46f;
            }
            if (shotIn > 0f && shotT < shotIn) { float k = Smoother(shotT / shotIn); p = Vector3.Lerp(fromPos, p, k); r = Quaternion.Slerp(fromRot, r, k); fov = Mathf.Lerp(fromFov, fov, k); }
            if (shake > 0f) { p += Random.insideUnitSphere * (shake * 0.15f); shake = Mathf.Max(0f, shake - dt * 4f); }
            cam.transform.SetPositionAndRotation(p, r); cam.fieldOfView = fov;
            if (camShot == CamShot.Kill) TickMoment();
            if (L != null) flareLight.transform.position = L.transform.position + Vector3.up * 11f;

            if (camShot == CamShot.Intro)
            {
                if (!introBegun && shotT >= IntroLen - 0.7f) { introBegun = true; hud.Cinema(false, true); hud.CineSkip(false); Begin(); }   // the HUD comes back as the crane settles
                if (shotT >= IntroLen) { camShot = CamShot.None; cam.fieldOfView = Fov; }   // the crane ends on the platoon's view: nothing to swoop back from
            }
            else if (camShot == CamShot.Kill && shotT >= KillLen) { EndShot(0.55f); hud.Cinema(false, true); if (phase == Phase.Play) stick.Blocked = false; stick.ConsumeTap(); }
            else if (camShot == CamShot.Orbit && phase == Phase.Play) { EndShot(0.9f); hud.Cinema(false, true); }   // repaired, or holding on into the day: back to the fight
            return true;
        }

        /// <summary>The opening: pushing in slowly, low behind the leader and looking out over his turret into the dark, then
        /// the crane up to the platoon's view, swinging out a little on its way and tilting down onto the ground ahead.</summary>
        void IntroPose(Vehicle L, out Vector3 p, out Quaternion r, out float fov)
        {
            var o = L.transform.position; var f = L.Forward; var rt = new Vector3(f.z, 0f, -f.x);
            float a = Mathf.Clamp01(shotT / IntroCrane), push = 1f - (1f - a) * (1f - a);
            var low = o - f * Mathf.Lerp(15f, 12f, push) + rt * (introSide * Mathf.Lerp(1f, 0.75f, push)) + Vector3.up * (introHigh + 0.4f * push);
            var lowAim = o + f * 40f + Vector3.up * 0.5f;
            float b = Mathf.Clamp01((shotT - IntroCrane) / (IntroLen - IntroCrane)), k = Smoother(b), kAim = Smooth(Mathf.Clamp01(b * 1.8f));   // the eye drops onto the tank early, so it stays in the picture as the camera climbs
            p = Vector3.Lerp(low, TopPos(L), k) + rt * (Mathf.Sin(b * Mathf.PI) * 3f);
            r = Quaternion.LookRotation(Vector3.Lerp(lowAim, TopAim(L), kAim) - p);
            fov = Mathf.Lerp(52f, Fov, k);
        }
    }
}

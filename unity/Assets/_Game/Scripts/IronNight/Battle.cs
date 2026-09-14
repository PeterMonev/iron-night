using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace IronNight
{
    /// <summary>
    /// One night assault: the platoon (leader driven by the thumb, wingmen holding formation, every turret aiming on its
    /// own), the enemy waves and armoured columns, shells and hits, signal flares that bring reinforcements, XP and the
    /// level-up cards, the night lighting and the camera. Everything is built in code from the generated meshes.
    /// </summary>
    public class Battle : MonoBehaviour
    {
        class Shell { public Vector3 pos, vel; public bool friendly, he, bounced, faust; public float dmg, life, trail; public Transform vis; }
        enum Weather { Clear, Overcast, Fog, Rain }
        static bool LowQuality { get => PlayerPrefs.GetInt("quality", 1) == 0; set { PlayerPrefs.SetInt("quality", value ? 0 : 1); PlayerPrefs.Save(); } }
        class Sky { public Color ambient, fog, moonColor; public float fogDensity, moonIntensity, shadow; }
        class Flare { public Vector3 pos; public float age; public Transform vis; public Light light; public Material mat; }
        class Wreck { public Vehicle v; public float age, burnTimer; public Light fire; }
        class ArtyShell { public Vector3 at; public float timer; }
        class Objective { public Vector3 pos; public Transform marker; public float smokeTimer, held; public int n; public bool hold; }
        class Drop { public Vector3 pos; public float height, age; public int kind; public Transform crate, chute, marker; public LineRenderer lines; }
        class Mortar { public Vector3 at; public float timer; public Transform ring; }
        class Star { public Vector3 pos; public float height, life, puff; public Transform flare, chute; public Light light; }
        enum Phase { Title, Play, LevelUp, Pause, End }

        const float NightLength = 300f;         // five minutes of darkness, dawn at 5:00
        const float ShellSpeed = 62f, EnemyShellSpeed = 55f, GroundTile = 40f;

        Camera cam; Hud hud; TouchStick stick; Fx fx; Props props; Tracks tracks; Infantry infantry;
        int nightInfantry, combo; float lastKill = -10f; bool veteran;
        Weather weather; Sky sky; float rumbleTimer = 12f, flicker, enemyRangeMul = 1f, ammoMul = 1f, ammoLeft, dropTimer = 35f; ParticleSystem rain; Material groundMaterial;
        Objective objective; int objectivesReached; bool winter;
        readonly List<Mortar> mortars = new List<Mortar>(); float mortarTimer = 100f, starTimer = 70f; Star star; bool lit; int shotsFired, shotsHit;
        int talliedKills, talliedTigers, talliedGuns, talliedInfantry, talliedObjectives; bool talliedAce, logged; readonly List<Drop> drops = new List<Drop>(); Material crateMaterial, chuteMaterial;
        Transform ground; Light flareLight, moon; float shake; int banked, nightTigers, nightPaks, nightFlares; bool nightRecorded, bossKilled;
        readonly List<Vehicle> platoon = new List<Vehicle>(); readonly List<Vehicle> foes = new List<Vehicle>();
        readonly List<Shell> shells = new List<Shell>(); readonly List<Flare> flares = new List<Flare>(); readonly List<Wreck> wrecks = new List<Wreck>();
        Material shellTemplate; Texture2D glowTex;
        Formation formation = Formation.Wedge; Phase phase = Phase.Title; bool reserveGranted;
        float t, spawnTimer = 6f, leaderShield; int level = 1, xp, xpNeed = 6, score, kills, maxPlatoon = 4, reinforcements;
        bool wave2, wave3, wave4, revived, doubled, bossSpawned; Vehicle boss;
        static readonly bool debugBoss = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--boss") >= 0;   // test switch: the boss comes at 0:06
        static readonly bool debugDawn = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--dawn") >= 0;   // test switch: the night starts at 4:10
        static readonly bool debugMid = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--mid") >= 0;   // test switch: 1:40, mortars and a star shell at once
        static readonly bool debugInfantry = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--infantry") >= 0;   // test switch: a squad at 0:04
        bool debugSquadSent;
        float damageMul = 1f, reloadMul = 1f, rangeMul = 1f, speedMul = 1f, scatterMul = 1f, turretMul = 1f; bool he, gunners, hasSmoke, fireflyNext, firstNight;
        float artyInterval, artyTimer, smokeLeft, smokeCooldown; int wingmanBonus, hintIndex; readonly List<ArtyShell> arty = new List<ArtyShell>();
        static readonly float[] steerAngles = { 0f, 35f, -35f, 70f, -70f, 110f, -110f };
        static readonly string[] hints = { "Drag anywhere to drive", "The turrets aim and fire on their own", "Flares of destroyed enemies bring reinforcements", "Hedges stop tanks: gates and lanes lead through", "Farm buildings stop shells: use them as cover" };
        static readonly float[] hintTimes = { 0.5f, 6f, 16f, 30f, 50f };

        Vehicle Leader => platoon.Count > 0 ? platoon[0] : null;

        public void Build(Camera camera, Hud h, TouchStick s, Fx effects)
        {
            cam = camera; hud = h; stick = s; fx = effects;
            shellTemplate = Resources.Load<Material>("Additive"); glowTex = Lightswarm.ProceduralSprites.Glow(64, 0.3f).texture;
            PickWeather();
            BuildWorld();
            // the night always starts with the leader alone; the platoon grows from flares to 3, a rewarded ad opens a 4th slot.
            // The depot's permanent upgrades set the starting numbers
            Depot.Load(); firstNight = Depot.NightsFought == 0;
            reloadMul = Depot.ReloadMul; rangeMul = Depot.RangeMul; speedMul = Depot.SpeedMul; maxPlatoon = 3;
            if (weather == Weather.Fog) { rangeMul *= 0.8f; enemyRangeMul = 0.8f; } else if (weather == Weather.Overcast) enemyRangeMul = 0.9f; else if (weather == Weather.Rain) { speedMul *= 0.92f; enemyRangeMul = 0.95f; }
            hud.SetConditions(winter ? "Ardennes" : "Normandy", winter && weather == Weather.Rain ? "Snow" : weather.ToString(), weather == Weather.Fog ? "everyone sees 20% less" : weather == Weather.Overcast ? "a dark night, the enemy sees 10% less" : weather == Weather.Rain ? (winter ? "the platoon slows in the drifts, the enemy sees 5% less" : "mud slows the platoon, the enemy sees 5% less") : "");
            hud.OnQuality = () => { LowQuality = !LowQuality; ApplyQuality(); hud.ShowPause(!Sfx.Muted, !LowQuality); };
            if (veteran) hud.Toast("Veteran night · points ×1.5", 3f);
            hud.OnDaily = () => { Depot.ClaimDaily(); hud.ShowTitle(reserveGranted); };
            platoon.Add(Vehicle.Create(VehicleSpec.ById(Depot.LeaderId), true, Vector3.zero, 0f)); Leader.hp = Depot.LeaderHp;
            NextObjective();
            hud.OnFormation = f => formation = f;
            hud.OnAd = OnAd; hud.OnAgain = () => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            hud.OnStart = () => { hud.HideTitle(); phase = Phase.Play; stick.Blocked = false; };
            hud.OnDepot = () => { stick.Blocked = true; hud.ShowDepot(); };
            hud.OnBack = () => { if (phase == Phase.End) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); else hud.ShowTitle(reserveGranted); };
            hud.OnReserveAd = () => { reserveGranted = true; maxPlatoon = 4; hud.ShowTitle(true); };   // the ad is a mock: granted at once
            hud.OnPause = Pause; hud.OnResume = Resume; hud.OnSound = () => { Sfx.Muted = !Sfx.Muted; hud.ShowPause(!Sfx.Muted, !LowQuality); };
            hud.OnQuit = () => { Resume(); revived = true; End(false); };   // no rewarded repair after walking away
            if (debugDawn) t = 250f; if (debugMid) { t = 100f; mortarTimer = 6f; starTimer = 9f; }
            hud.Set(t, platoon.Count); hud.SetLevel(level, 0f);
            PlaceCamera(true);
            stick.Blocked = true; hud.ShowTitle(false); Debug.Log("Iron Night: battle built, debugBoss=" + debugBoss + " args=" + string.Join(" ", System.Environment.GetCommandLineArgs()));
        }

        void BuildWorld()
        {
            // a 400 m plain pasture plane under everything, kept under the camera on a 40 m grid; the fields, lanes,
            // hedges, farms and searchlight posts on top of it are Props, built cell by cell around the camera
            var g = GameObject.CreatePrimitive(PrimitiveType.Plane); g.name = "Ground"; Destroy(g.GetComponent<Collider>());
            g.transform.localScale = new Vector3(GroundTile, 1f, GroundTile);
            var gm = new Material(Resources.Load<Material>("GroundLit"));
            gm.SetTexture("_BaseMap", Resources.Load<Texture2D>(winter ? "Textures/field_snow_pasture" : "Textures/field_pasture")); gm.SetTextureScale("_BaseMap", new Vector2(10f, 10f)); gm.SetColor("_BaseColor", new Color(0.9f, 0.9f, 0.88f));
            g.GetComponent<Renderer>().sharedMaterial = gm; g.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ground = g.transform;
            props = new GameObject("Props").AddComponent<Props>(); props.winter = winter; props.Build(cam); props.fx = fx;
            tracks = new GameObject("Tracks").AddComponent<Tracks>(); tracks.Build();
            infantry = new GameObject("Infantry").AddComponent<Infantry>(); infantry.Build(); veteran = Depot.Veteran;

            // night: moonlight with soft shadows, a cold ambient, fog swallowing the distance
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = sky.ambient;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Exponential; RenderSettings.fogDensity = sky.fogDensity; RenderSettings.fogColor = sky.fog;
            var moonGo = new GameObject("Moon"); moon = moonGo.AddComponent<Light>();
            moon.type = LightType.Directional; moon.color = sky.moonColor; moon.intensity = sky.moonIntensity; moon.shadows = LightShadows.Soft; moon.shadowStrength = sky.shadow;
            if (weather == Weather.Rain) { BuildRain(); if (!winter) gm.SetFloat("_Smoothness", 0.55f); } groundMaterial = gm; Sfx.Ambient(weather == Weather.Rain && !winter);
            ApplyQuality();
            moonGo.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
            // the flare light over the platoon: warm, follows the leader, grows with the platoon
            var fl = new GameObject("FlareLight"); flareLight = fl.AddComponent<Light>();
            flareLight.type = LightType.Point; flareLight.color = new Color(1f, 0.72f, 0.4f); flareLight.intensity = 18f; flareLight.range = 40f; flareLight.shadows = LightShadows.None;
        }

        void PlaceCamera(bool snap)
        {
            var L = Leader; if (L == null) return;
            var want = L.transform.position + new Vector3(0f, 44f, -36f);
            cam.transform.position = snap ? want : Vector3.Lerp(cam.transform.position, want, 1f - Mathf.Exp(-Time.deltaTime * 4f));
            cam.transform.LookAt(cam.transform.position + new Vector3(0f, -44f, 40f));
            if (shake > 0f) { cam.transform.position += Random.insideUnitSphere * (shake * 0.5f); shake = Mathf.Max(0f, shake - Time.deltaTime * 3f); }
            ground.position = new Vector3(Mathf.Round(cam.transform.position.x / GroundTile) * GroundTile, -0.08f, Mathf.Round(cam.transform.position.z / GroundTile) * GroundTile);
            flareLight.transform.position = L.transform.position + Vector3.up * 11f;
        }

        void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            fx.Tick(dt);
            var kb = Keyboard.current; if (kb != null && kb.escapeKey.wasPressedThisFrame) { if (phase == Phase.Play) Pause(); else if (phase == Phase.Pause) Resume(); }
            if (phase != Phase.Play) { PlaceCamera(false); TickWrecks(dt); Sfx.Engine(0f); props.Tick(); return; }
            t += dt;
            if (firstNight && hintIndex < hints.Length && t >= hintTimes[hintIndex]) { hud.Toast(hints[hintIndex], 3.5f); hintIndex++; }
            Lighting(Mathf.Clamp01((t - 255f) / 45f), dt);
            if (smokeLeft > 0f) smokeLeft -= dt; if (smokeCooldown > 0f) smokeCooldown -= dt;
            TickArtillery(dt);
            var L = Leader;
            if (leaderShield > 0f) leaderShield -= dt;
            if (rain != null) rain.transform.position = L.transform.position + Vector3.up * 30f;

            // the leader drives where the thumb points; the wingmen hold their slots
            if (stick.Active) L.Drive(stick.Direction, dt);
            for (int i = 1; i < platoon.Count; i++)
            {
                var v = platoon[i]; var slot = Slot(i); var d = slot - v.transform.position; d.y = 0f;
                if (d.magnitude > 1.2f) v.Drive(Steer(v, new Vector2(d.x, d.z) * (Mathf.Clamp01(d.magnitude / 5f))), dt);
            }
            foreach (var v in platoon) { v.speedMul = speedMul; v.damageMul = damageMul; v.rangeMul = rangeMul; v.reloadMul = reloadMul; v.turretMul = turretMul; }

            // turrets: nearest foe in range, else drift home
            float leaderTurret = L.turretYaw;
            foreach (var v in platoon)
            {
                v.reloadLeft -= dt;
                var target = Nearest(foes, v.transform.position, v.Range * 1.15f);
                if (target != null) { bool on = v.Aim(target.transform.position, dt); if (on && v.reloadLeft <= 0f && Dist(v, target) <= v.Range) Fire(v, target); }
                else v.IdleTurret(dt);
                v.Apply();
            }

            Sfx.Turret(Mathf.Abs(Mathf.DeltaAngle(leaderTurret * Mathf.Rad2Deg, L.turretYaw * Mathf.Rad2Deg)) * Mathf.Deg2Rad / Mathf.Max(dt, 1e-4f));
            foreach (var v in platoon) TickMg(v, dt);
            infantry.Tick(dt, platoon, props, FireFaust);
            int crushed = infantry.Crush(platoon); if (crushed > 0) { InfantryKilled(crushed, L.transform.position); hud.Toast("Run down", 1.5f); }

            // enemies: close in, then hold and shoot
            for (int i = 0; i < foes.Count; i++)
            {
                var e = foes[i]; e.reloadLeft -= dt;
                var target = Nearest(platoon, e.transform.position, 1000f); if (target == null) continue; e.rangeMul = enemyRangeMul * (lit ? 1.2f : 1f);
                float dist = Dist(e, target);
                if (!e.spec.isGun && dist > e.Range * 0.8f)
                {
                    var goal = target.transform.position;
                    if (e.flank != 0 && dist > 20f) { var toT = goal - e.transform.position; toT.y = 0f; toT.Normalize(); goal += new Vector3(toT.z, 0f, -toT.x) * (e.flank * 18f); }   // a Panzer IV works round the side
                    var d = goal - e.transform.position; e.Drive(Steer(e, new Vector2(d.x, d.z)), dt);
                }
                bool on = e.Aim(target.transform.position, dt);
                bool blind = smokeLeft > 0f && dist > 9f;                 // the smoke screen: they cannot see us from afar
                if (on && !blind && e.reloadLeft <= 0f && dist <= e.Range) Fire(e, target);
                e.Apply();
            }

            TickShells(dt); if (phase != Phase.Play) return;                    // the leader may have just died
            TickFlares(dt); TickWrecks(dt); TickSpawns(dt);
            KeepApart();
            foreach (var v in platoon) v.transform.position = props.PushOut(v.transform.position, v.spec.radius * 0.7f);
            foreach (var e in foes) if (!e.spec.isGun) e.transform.position = props.PushOut(e.transform.position, e.spec.radius * 0.7f);
            foreach (var v in platoon) tracks.Mark(v); foreach (var e in foes) if (!e.spec.isGun) tracks.Mark(e);
            foreach (var v in platoon) Smoulder(v, dt); foreach (var e in foes) Smoulder(e, dt);
            props.Tick();
            flareLight.range = 34f + platoon.Count * 3f;
            Sfx.Engine(stick.Active ? stick.Direction.magnitude : 0f);
            PlaceCamera(false);
            hud.Set(t, platoon.Count); hud.SetLeader(Mathf.CeilToInt(L.hp), Mathf.CeilToInt(Depot.LeaderHp));
            hud.Indicators(foes, cam);
            TickObjective(dt); TickDrops(dt); TickMortars(dt); TickStar(dt);
            if (ammoLeft > 0f) { ammoLeft -= dt; if (ammoLeft <= 0f) { ammoMul = 1f; hud.Toast("APCR spent"); } }
            hud.Radar(foes, platoon, L.transform.position, objective != null ? objective.pos : Vector3.zero, objective != null);
            hud.HpBars(foes, cam, boss);
            if (objective != null) { var od = objective.pos - L.transform.position; od.y = 0f; hud.Objective(objective.pos, od.magnitude, cam, true); }
            TickObjectiveLabel();
            if (t >= NightLength) End(true);
        }

        Vector3 Slot(int i)
        {
            var L = Leader; var f = L.Forward; var r = new Vector3(f.z, 0f, -f.x); float lat = 0f, back = 0f;
            switch (formation)
            {
                case Formation.Wedge: { int row = (i - 1) / 2 + 1; float side = (i % 2 == 1) ? -1f : 1f; lat = side * 6.5f * row; back = 5.5f * row; break; }
                case Formation.Column: back = 7.5f * i; break;
                case Formation.Line: { int k = (i - 1) / 2 + 1; float side = (i % 2 == 1) ? -1f : 1f; lat = side * 7.5f * k; back = 0.6f * k; break; }
                default: lat = 6f * i; back = 5.5f * i; break;
            }
            return L.transform.position + r * lat - f * back;
        }

        static float Dist(Vehicle a, Vehicle b) { var d = a.transform.position - b.transform.position; d.y = 0f; return d.magnitude; }

        static Vehicle Nearest(List<Vehicle> list, Vector3 from, float maxDist)
        {
            Vehicle best = null; float bd = maxDist * maxDist;
            foreach (var v in list) { if (v.dead) continue; var d = v.transform.position - from; d.y = 0f; float q = d.sqrMagnitude; if (q < bd) { bd = q; best = v; } }
            return best;
        }

        void Fire(Vehicle v, Vehicle target)
        {
            v.reloadLeft = v.spec.reload * v.reloadMul * (v.friendly ? 1f : Mathf.Lerp(1.9f, 1.1f, t / 120f) * (firstNight ? 1.3f : 1f)); v.Recoil();
            var scatter = (v.friendly ? 1.5f * scatterMul : 4f * (smokeLeft > 0f ? 3f : 1f) * (lit ? 0.5f : 1f)) * Mathf.Deg2Rad * Random.Range(-1f, 1f);
            if (v.friendly) shotsFired++;
            var dir = Quaternion.Euler(0f, scatter * Mathf.Rad2Deg, 0f) * v.GunDirection; var pos = v.MuzzlePosition;
            float speed = v.friendly ? ShellSpeed : EnemyShellSpeed;
            var vis = fx.Tracer(v.friendly ? new Color(1f, 1f, 0.9f, 1f) : new Color(1f, 0.75f, 0.55f, 1f), v.friendly ? new Color(1f, 0.85f, 0.45f, 0.6f) : new Color(1f, 0.45f, 0.25f, 0.6f));
            vis.position = pos; vis.rotation = Quaternion.LookRotation(cam.transform.forward, dir);
            shells.Add(new Shell { pos = pos, vel = dir * speed, friendly = v.friendly, he = v.friendly && he, dmg = v.spec.damage * v.damageMul * (v.friendly ? ammoMul : 1f), life = v.Range / speed + 0.25f, vis = vis });
            fx.MuzzleFlash(pos, dir); Sfx.Shot(pos, v.friendly, v.spec.gunLength > 3f || v.spec.isGun); if (v.friendly) Sfx.Reload(v.transform.position);
        }

        void TickShells(float dt)
        {
            for (int i = shells.Count - 1; i >= 0; i--)
            {
                var s = shells[i]; s.pos += s.vel * dt; s.life -= dt; s.trail += s.vel.magnitude * dt;
                if (s.trail > 4f && !s.bounced) { s.trail = 0f; fx.Trail(s.pos); }
                // the tracer: a stretched glow along the flight, facing the camera
                s.vis.position = s.pos; s.vis.rotation = Quaternion.LookRotation(cam.transform.forward, s.vel);
                var hitList = s.friendly ? foes : platoon; Vehicle hit = null;
                if (!s.bounced) foreach (var v in hitList) { if (v.dead) continue; var d = v.transform.position - s.pos; d.y = 0f; if (d.sqrMagnitude < v.spec.radius * v.spec.radius) { hit = v; break; } }
                if (hit != null && Ricochets(s, hit))
                {
                    // glances off: sparks, a whine, and the shell tumbles away over the hull
                    var away = s.pos - hit.transform.position; away.y = 0f; away.Normalize();
                    s.vel = Vector3.Reflect(s.vel, away) * 0.45f + away * 10f + Vector3.up * 16f; s.life = 0.8f; s.bounced = true;
                    fx.Spark(new Vector3(s.pos.x, 1.8f, s.pos.z), away); Sfx.Ricochet(s.pos); hud.Popup(hit.transform.position, "Ricochet", new Color(0.8f, 0.82f, 0.86f));
                    hit = null;
                }
                if (hit != null)
                {
                    if (s.friendly) shotsHit++;
                    Damage(hit, s.dmg, s.pos);
                    if (s.he) { foreach (var v in hitList) if (v != hit && !v.dead && Dist(v, hit) < 5f) Damage(v, s.dmg * 0.5f, v.transform.position); if (s.friendly) InfantryKilled(infantry.Blast(s.pos, 5f), s.pos); }
                    fx.Hit(new Vector3(s.pos.x, 1.6f, s.pos.z), s.he ? 1.5f : 1f);
                }
                if (s.bounced) s.vel += Vector3.down * (30f * dt);
                if (hit == null && !s.bounced && props.Blocks(s.pos)) { fx.Hit(s.pos, 0.6f); Sfx.Hit(s.pos); s.life = 0f; }
                else if (hit == null && s.life <= 0f) { var g = new Vector3(s.pos.x, 0f, s.pos.z); fx.Dust(g); props.Crater(g, 2.2f); }   // spent: into the dirt
                if (hit != null || s.life <= 0f) { fx.Release(s.vis); shells.RemoveAt(i); }
            }
        }

        void Damage(Vehicle v, float dmg, Vector3 at)
        {
            if (v.friendly && v == Leader && leaderShield > 0f) return;
            v.Hit(dmg); Sfx.Hit(v.transform.position);
            if (v.friendly && v == Leader) { hud.Flash(); shake = Mathf.Max(shake, 0.8f); Buzz(); }
            if (v.hp > 0f) { if (v == Leader) { hud.Toast("Leader hit"); if (hasSmoke && smokeCooldown <= 0f) PopSmoke(); } return; }
            tracks.Forget(v);
            v.Wreck(); fx.Explosion(v.transform.position); Sfx.Explosion(v.transform.position); InfantryKilled(infantry.Blast(v.transform.position, 6f), v.transform.position);
            var fire = new GameObject("WreckFire").AddComponent<Light>(); fire.type = LightType.Point; fire.color = new Color(1f, 0.5f, 0.2f); fire.range = 16f; fire.intensity = 6f; fire.shadows = LightShadows.None;
            fire.transform.position = v.transform.position + Vector3.up * 2.5f;
            wrecks.Add(new Wreck { v = v, fire = fire });
            if (v.friendly)
            {
                bool wasLeader = v == Leader; platoon.Remove(v);
                if (wasLeader || platoon.Count == 0) { End(false); return; }
                hud.Toast("Wingman lost");
            }
            else
            {
                foes.Remove(v); kills++;
                if (v.spec == VehicleSpec.Tiger || v.spec == VehicleSpec.TigerAce) nightTigers++; if (v.spec.isGun) nightPaks++;
                int worth = v.spec == VehicleSpec.Tiger ? 5 : v.spec == VehicleSpec.TigerAce ? 12 : v.spec == VehicleSpec.Flak88 ? 4 : 2; score += worth * 50; xp += worth;
                hud.Popup(v.transform.position, "+" + worth * 50, new Color(0.95f, 0.66f, 0.23f)); shake = Mathf.Max(shake, Dist(v, Leader) < 25f ? 0.5f : 0.2f);
                combo = Time.time - lastKill < 4f ? combo + 1 : 1; lastKill = Time.time;
                if (combo >= 2) { int bonus = combo * 25; score += bonus; hud.Popup(v.transform.position + Vector3.up * 3f, "×" + combo + " +" + bonus, new Color(1f, 0.92f, 0.6f)); if (combo == 3) hud.Toast("Triple kill", 1.8f); else if (combo == 5) hud.Toast("Rampage", 1.8f); }
                if (v == boss) { score += 1500; bossKilled = true; shake = 2f; hud.HideBoss(); hud.Toast("Tiger Ace destroyed · +1500"); }
                SpawnFlare(v.transform.position);
                if (xp >= xpNeed) LevelUp(); else hud.SetLevel(level, (float)xp / xpNeed);
            }
        }

        void SpawnFlare(Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(go.GetComponent<Collider>()); go.name = "Flare";
            var m = new Material(shellTemplate); m.SetTexture("_BaseMap", glowTex); m.SetColor("_BaseColor", new Color(1f, 0.8f, 0.45f, 1f));
            var r = go.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; go.transform.localScale = Vector3.one * 3f;
            var l = new GameObject("FlareGlow").AddComponent<Light>(); l.type = LightType.Point; l.color = new Color(1f, 0.75f, 0.4f); l.range = 14f; l.intensity = 5f; l.shadows = LightShadows.None;
            l.transform.position = pos + Vector3.up * 2f;
            flares.Add(new Flare { pos = pos + new Vector3(Random.Range(-3f, 3f), 1.5f, Random.Range(-3f, 3f)), vis = go.transform, light = l, mat = m });
        }

        void TickFlares(float dt)
        {
            var L = Leader; if (L == null) return;
            for (int i = flares.Count - 1; i >= 0; i--)
            {
                var f = flares[i]; f.age += dt;
                f.vis.position = f.pos + Vector3.up * (0.3f * Mathf.Sin(f.age * 3f)); f.vis.rotation = cam.transform.rotation; f.vis.localScale = Vector3.one * (2.6f + 0.6f * Mathf.Sin(f.age * 9f));
                var d = L.transform.position - f.pos; d.y = 0f;
                if (d.magnitude < 4f)
                {
                    Sfx.Pickup();
                    nightFlares++;
                    if (platoon.Count < maxPlatoon) Reinforce(); else { score += 50; hud.Popup(f.pos, "+50", new Color(1f, 0.85f, 0.5f)); }
                    Destroy(f.mat); Destroy(f.vis.gameObject); Destroy(f.light.gameObject); flares.RemoveAt(i);
                }
                else if (f.age > 40f) { Destroy(f.mat); Destroy(f.vis.gameObject); Destroy(f.light.gameObject); flares.RemoveAt(i); }
            }
        }

        void Reinforce()
        {
            reinforcements++;
            var spec = reinforcements % 3 == 0 || fireflyNext ? VehicleSpec.Firefly : VehicleSpec.Sherman; fireflyNext = false;
            var L = Leader; var v = Vehicle.Create(spec, true, L.transform.position - L.Forward * 12f, L.yaw); v.hp += Depot.WingmanHpBonus + wingmanBonus;
            platoon.Add(v); hud.Toast("Reinforcement · " + spec.name); hud.Set(t, platoon.Count);
        }

        void TickWrecks(float dt)
        {
            for (int i = wrecks.Count - 1; i >= 0; i--)
            {
                var w = wrecks[i]; w.age += dt; w.burnTimer -= dt;
                if (w.age < 14f && w.burnTimer <= 0f) { w.burnTimer = 0.14f; fx.Burn(w.v.transform.position); }
                if (w.fire != null) { w.fire.intensity = w.age < 14f ? 4f + 3f * Mathf.PerlinNoise(w.age * 7f, 0.3f) : Mathf.Max(0f, w.fire.intensity - dt * 2f); }
                if (w.age > 45f) { Destroy(w.fire.gameObject); Destroy(w.v.gameObject); wrecks.RemoveAt(i); }
            }
        }

        void TickSpawns(float dt)
        {
            spawnTimer -= dt;
            float interval = Mathf.Lerp(8f, 2.8f, t / 240f) * (firstNight ? 1.25f : 1f);
            if (debugInfantry && !debugSquadSent && t > 4f) { debugSquadSent = true; var L0 = Leader; infantry.Spawn(L0.transform.position + L0.Forward * 30f, -L0.Forward); hud.Toast("Infantry! Panzerfausts, 12 o'clock", 2.8f); }
            if (spawnTimer <= 0f && foes.Count < 12)
            {
                spawnTimer = interval;
                var L = Leader; float ahead = L.yaw + Random.Range(-1.7f, 1.7f);
                var dir = new Vector3(Mathf.Sin(ahead), 0f, Mathf.Cos(ahead));
                float roll = Random.value;
                if (t > 45f && roll < 0.2f && infantry.squads.Count < 3)
                {
                    // tank hunters on foot, out of the dark ahead
                    var pos = props.PushOut(L.transform.position + dir * Random.Range(30f, 40f), 2f);
                    infantry.Spawn(pos, -dir); hud.Toast("Infantry! Panzerfausts, " + Clock(pos), 2.8f);
                }
                else if (roll < 0.4f && t > 20f)
                {
                    // an anti-tank gun dug in on the platoon's way, waiting
                    var pos = props.PushOut(L.transform.position + L.Forward * 38f + new Vector3(Random.Range(-14f, 14f), 0f, 0f), 3f); float gyaw = L.yaw + Mathf.PI;
                    var nests = props.Nests(L.transform.position, L.Forward, 26f, 60f);
                    if (nests.Count > 0) { var nest = nests[Random.Range(0, nests.Count)]; var toL = L.transform.position - nest; toL.y = 0f; toL.Normalize(); pos = nest - toL * 3.2f; gyaw = Mathf.Atan2(toL.x, toL.z); }   // behind the sandbags, facing us
                    var gspec = VehicleSpec.Pak40;
                    if (t > 100f && Random.value < 0.35f)
                    {
                        // the searchlight posts have an 88 with them: long reach, hard hit, slow to turn
                        var posts = props.Posts(L.transform.position, L.Forward, 34f, 66f);
                        if (posts.Count > 0) { var post = posts[Random.Range(0, posts.Count)]; var toL = L.transform.position - post; toL.y = 0f; toL.Normalize(); pos = props.PushOut(post + toL * 8f, 3f); gyaw = Mathf.Atan2(toL.x, toL.z); gspec = VehicleSpec.Flak88; }
                    }
                    var gun = Foe(gspec, pos, gyaw);
                    hud.Toast((gspec == VehicleSpec.Flak88 ? "88! Flak gun, " : "Anti-tank gun dug in, ") + Clock(pos), 2.8f);
                }
                else
                {
                    var spec = (t > 90f && Random.value < Mathf.Lerp(0.1f, 0.35f, (t - 90f) / 180f)) ? VehicleSpec.Tiger : VehicleSpec.PanzerIV;
                    var pos = L.transform.position + dir * Random.Range(44f, 52f);
                    Foe(spec, pos, Mathf.Atan2(-dir.x, -dir.z));
                    if (spec == VehicleSpec.Tiger) hud.Toast("Tiger! " + Clock(pos), 2.8f);
                }
            }
            if (t >= 120f && !wave2) { wave2 = true; Column(); }
            if (t >= 200f && !wave3) { wave3 = true; Counterattack(); }
            if (t >= 240f && !wave4) { wave4 = true; Column(); }
            if ((t >= 270f || (debugBoss && t >= 6f)) && !bossSpawned) { bossSpawned = true; Boss(); }
            if (boss != null && !boss.dead) hud.SetBoss(boss.hp / boss.spec.hp);
        }

        /// <summary>The 4:30 boss: a Tiger ace with two Panzer IV escorts, straight at the platoon from the dark ahead.</summary>
        void Boss()
        {
            var L = Leader; var f = L.Forward; var r = new Vector3(f.z, 0f, -f.x);
            boss = Foe(VehicleSpec.TigerAce, L.transform.position + f * 52f, L.yaw + Mathf.PI);
            foreach (var side in new[] { -1f, 1f }) Foe(VehicleSpec.PanzerIV, L.transform.position + f * 58f + r * side * 9f, L.yaw + Mathf.PI);
            hud.ShowBoss(VehicleSpec.TigerAce.name); hud.Toast("Tiger Ace · kill it before dawn"); Debug.Log("Iron Night: boss spawned at " + t.ToString("0.0"));
        }

        /// <summary>An armoured column crossing the front 34 m ahead: four Panzer IV and a Tiger in a line.</summary>
        void Column()
        {
            var L = Leader; var f = L.Forward; var r = new Vector3(f.z, 0f, -f.x); float side = Random.value < 0.5f ? -1f : 1f;
            for (int i = 0; i < 5; i++)
            {
                var spec = i == 0 ? VehicleSpec.Tiger : VehicleSpec.PanzerIV;
                var pos = L.transform.position + f * (34f + i * 3f) - r * side * (46f + i * 9f);
                Foe(spec, pos, Mathf.Atan2(r.x * side, r.z * side));
            }
            hud.Toast("Armoured column, " + Clock(L.transform.position - r * side * 50f), 2.8f);
        }

        /// <summary>Vehicles push each other apart so the platoon never stacks and enemies keep a spacing.</summary>
        void KeepApart()
        {
            void Push(List<Vehicle> a, List<Vehicle> b)
            {
                foreach (var x in a) foreach (var y in b)
                {
                    if (x == y || x.dead || y.dead) continue;
                    var d = y.transform.position - x.transform.position; d.y = 0f; float min = x.spec.radius + y.spec.radius + 0.6f;
                    if (d.sqrMagnitude < min * min && d.sqrMagnitude > 0.001f) { var push = d.normalized * (min - d.magnitude) * 0.5f; if (!x.spec.isGun) x.transform.position -= push; if (!y.spec.isGun) y.transform.position += push; }
                }
            }
            Push(platoon, platoon); Push(foes, foes); Push(platoon, foes);
        }

        void LevelUp()
        {
            xp -= xpNeed; level++; xpNeed = 6 + level * 3; hud.SetLevel(level, (float)xp / xpNeed);
            var all = new List<Hud.Card>
            {
                new Hud.Card { id = "he", title = "HE shells", desc = "High explosive: every hit also damages tanks within 5 m." },
                new Hud.Card { id = "apcr", title = "APCR rounds", desc = "Tungsten core. +50% damage for the whole platoon." },
                new Hud.Card { id = "rapid", title = "Faster loaders", desc = "The crews reload 20% faster." },
                new Hud.Card { id = "radar", title = "Night optics", desc = "Gunners see 15% farther in the dark." },
                new Hud.Card { id = "engine", title = "Tuned engines", desc = "The platoon drives 15% faster." },
                new Hud.Card { id = "repair", title = "Field repair", desc = "The leader is fully repaired and toughened by 1." },
                new Hud.Card { id = "reinf", title = "Reinforcements", desc = "A Sherman joins the platoon right now (if there is a slot)." },
                new Hud.Card { id = "arty", rare = true, title = "Artillery support", desc = artyInterval > 0f ? "The battery answers faster: a salvo every " + Mathf.Max(10f, artyInterval - 6f).ToString("0") + " s." : "A battery of 25-pounders on call: a salvo lands on the enemy every 24 s." },
                new Hud.Card { id = "smoke", rare = true, title = "Smoke dischargers", desc = "When the leader is hit the platoon vanishes in smoke for 6 s; the enemy loses its aim." },
                new Hud.Card { id = "firefly", rare = true, title = "Firefly conversion", desc = "One Sherman is refitted with the 17-pounder now; the next reinforcement is a Firefly too." },
                new Hud.Card { id = "gunners", rare = true, title = "Veteran gunners", desc = "Crews that fire true: scatter cut by two thirds, turrets swing 30% faster." },
                new Hud.Card { id = "plates", title = "Applique armour", desc = "Welded plates: every wingman, now and later, takes 2 more hits." },
            };
            if (he) all.RemoveAll(c => c.id == "he"); if (gunners) all.RemoveAll(c => c.id == "gunners"); if (hasSmoke) all.RemoveAll(c => c.id == "smoke");
            if (artyInterval > 0f && artyInterval <= 12f) all.RemoveAll(c => c.id == "arty");
            if (!platoon.Exists(p => p != Leader && p.spec == VehicleSpec.Sherman) && platoon.Count >= maxPlatoon) all.RemoveAll(c => c.id == "firefly");
            var pick = new List<Hud.Card>(); while (pick.Count < 3 && all.Count > 0) { int i = Random.Range(0, all.Count); pick.Add(all[i]); all.RemoveAt(i); }
            phase = Phase.LevelUp; stick.Blocked = true; Sfx.LevelUp();
            hud.ShowCards(pick, id =>
            {
                switch (id)
                {
                    case "he": he = true; break;
                    case "apcr": damageMul *= 1.5f; break;
                    case "rapid": reloadMul *= 0.8f; break;
                    case "radar": rangeMul *= 1.15f; break;
                    case "engine": speedMul *= 1.15f; break;
                    case "repair": Leader.hp = Depot.LeaderHp + 1f; break;
                    case "reinf": Reinforce(); break;
                    case "arty": artyInterval = artyInterval > 0f ? Mathf.Max(10f, artyInterval - 6f) : 24f; artyTimer = 4f; break;
                    case "smoke": hasSmoke = true; break;
                    case "firefly":
                    {
                        fireflyNext = true; var w = platoon.Find(p => p != Leader && p.spec == VehicleSpec.Sherman);
                        if (w != null) { int k = platoon.IndexOf(w); var f = Vehicle.Create(VehicleSpec.Firefly, true, w.transform.position, w.yaw); f.hp = w.hp; f.turretYaw = w.turretYaw; tracks.Forget(w); Destroy(w.gameObject); platoon[k] = f; fireflyNext = false; }
                        break;
                    }
                    case "gunners": gunners = true; scatterMul = 0.35f; turretMul = 1.3f; break;
                    case "plates": wingmanBonus += 2; foreach (var p in platoon) if (p != Leader) p.hp += 2f; break;
                }
                phase = Phase.Play; stick.Blocked = false;
            });
        }

        /// <summary>Tonight's weather: most nights are clear; overcast, fog and rain each change the light and the fight.</summary>
        void PickWeather()
        {
            float r = Random.value; weather = r < 0.45f ? Weather.Clear : r < 0.7f ? Weather.Overcast : r < 0.85f ? Weather.Fog : Weather.Rain;
            Depot.Load(); winter = Depot.NightsFought >= 1 && Random.value < 0.5f;   // the first night is always Normandy
            var args = System.Environment.GetCommandLineArgs();   // test switches: --fog, --rain, --overcast, --clear, --winter, --summer
            foreach (Weather w in System.Enum.GetValues(typeof(Weather))) if (System.Array.IndexOf(args, "--" + w.ToString().ToLowerInvariant()) >= 0) weather = w;
            if (System.Array.IndexOf(args, "--winter") >= 0) winter = true; if (System.Array.IndexOf(args, "--summer") >= 0) winter = false;
            switch (weather)
            {
                case Weather.Overcast: sky = new Sky { ambient = new Color(0.17f, 0.19f, 0.27f), fog = new Color(0.025f, 0.03f, 0.045f), fogDensity = 0.008f, moonColor = new Color(0.7f, 0.74f, 0.9f), moonIntensity = 1.7f, shadow = 0.5f }; break;
                case Weather.Fog: sky = new Sky { ambient = new Color(0.24f, 0.26f, 0.32f), fog = new Color(0.09f, 0.1f, 0.12f), fogDensity = 0.016f, moonColor = new Color(0.8f, 0.82f, 0.9f), moonIntensity = 1.9f, shadow = 0.45f }; break;
                case Weather.Rain: sky = new Sky { ambient = new Color(0.15f, 0.17f, 0.24f), fog = new Color(0.03f, 0.035f, 0.05f), fogDensity = 0.009f, moonColor = new Color(0.66f, 0.7f, 0.86f), moonIntensity = 1.5f, shadow = 0.4f }; break;
                default: sky = new Sky { ambient = new Color(0.24f, 0.27f, 0.36f), fog = new Color(0.03f, 0.045f, 0.07f), fogDensity = 0.0065f, moonColor = new Color(0.82f, 0.86f, 1f), moonIntensity = 2.6f, shadow = 0.8f }; break;
            }
            if (winter) { sky.ambient = sky.ambient * 1.15f + new Color(0.02f, 0.02f, 0.04f); sky.fog = sky.fog * 1.4f + new Color(0.02f, 0.02f, 0.03f); sky.moonColor = new Color(sky.moonColor.r * 0.95f, sky.moonColor.g, Mathf.Min(1f, sky.moonColor.b * 1.05f)); }
            LightShaft.boost = weather == Weather.Fog ? 1.8f : weather == Weather.Rain ? 1.3f : 1f;
        }

        /// <summary>Low quality for weak phones: no moon shadows, no post-processing, no rain or snow.</summary>
        void ApplyQuality()
        {
            bool low = LowQuality;
            moon.shadows = low ? LightShadows.None : LightShadows.Soft;
            var volume = FindAnyObjectByType<UnityEngine.Rendering.Volume>(); if (volume != null) volume.weight = low ? 0f : 1f;
            if (rain != null) { if (low) rain.Stop(); else if (!rain.isPlaying) rain.Play(); }
        }

        /// <summary>The night's light every frame: the sky of the weather, then the last 45 seconds turning to dawn
        /// (k runs 0..1), then the flicker of a barrage somewhere over the horizon.</summary>
        void Lighting(float k, float dt)
        {
            rumbleTimer -= dt;
            if (rumbleTimer <= 0f) { rumbleTimer = 9f + Random.value * 18f; flicker = 0.35f; Sfx.Rumble(); }
            if (flicker > 0f) flicker = Mathf.Max(0f, flicker - dt * 1.4f);
            if (k <= 0f && flicker <= 0f) return;
            float e = k * k; var fl = new Color(0.2f, 0.17f, 0.14f) * (flicker * flicker * 4f);
            RenderSettings.ambientLight = Color.Lerp(sky.ambient, new Color(0.42f, 0.4f, 0.44f), e) + fl;
            RenderSettings.fogColor = Color.Lerp(sky.fog, new Color(0.3f, 0.26f, 0.28f), e); RenderSettings.fogDensity = Mathf.Lerp(sky.fogDensity, Mathf.Min(sky.fogDensity, 0.004f), e);
            moon.color = Color.Lerp(sky.moonColor, new Color(1f, 0.82f, 0.62f), e); moon.intensity = Mathf.Lerp(sky.moonIntensity, 3.6f, e);
            moon.transform.rotation = Quaternion.Euler(Mathf.Lerp(52f, 22f, e), Mathf.Lerp(-35f, -80f, e), 0f);
        }

        /// <summary>Rain: thin pale streaks falling through a 70 m box over the platoon, stretched by their speed.</summary>
        void BuildRain()
        {
            var go = new GameObject("Rain"); rain = go.AddComponent<ParticleSystem>(); rain.Stop();
            var main = rain.main; main.startSpeed = winter ? 3f : 40f; main.startLifetime = winter ? 14f : 1.1f; main.startSize = winter ? 0.28f : 0.07f; main.maxParticles = 2000; main.simulationSpace = ParticleSystemSimulationSpace.World; main.gravityModifier = winter ? 0.08f : 1.5f;
            main.startColor = winter ? new Color(0.9f, 0.92f, 0.98f, 0.7f) : new Color(0.7f, 0.75f, 0.85f, 0.45f);
            var em = rain.emission; em.rateOverTime = winter ? 140f : 1300f;
            if (winter) { var vel = rain.velocityOverLifetime; vel.enabled = true; vel.x = new ParticleSystem.MinMaxCurve(-1.2f, 1.2f); vel.z = new ParticleSystem.MinMaxCurve(-1.2f, 1.2f); }
            var sh = rain.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(70f, 1f, 90f); sh.rotation = new Vector3(90f, 0f, 0f);
            var r = go.GetComponent<ParticleSystemRenderer>(); if (winter) r.renderMode = ParticleSystemRenderMode.Billboard; else { r.renderMode = ParticleSystemRenderMode.Stretch; r.lengthScale = 22f; r.velocityScale = 0f; }
            var m = new Material(Resources.Load<Material>("Additive")); m.SetTexture("_BaseMap", glowTex); m.SetColor("_BaseColor", new Color(0.5f, 0.55f, 0.65f, 0.35f)); r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            go.transform.position = Vector3.up * 30f; rain.Play();
        }

        /// <summary>Armour is sloped and shells glance: most likely off a front plate, rarely off a side, never from
        /// HE. The Tiger's slab front bounces a little more; the 17-pounder and APCR bite through.</summary>
        bool Ricochets(Shell s, Vehicle target)
        {
            if (target.spec.isGun || s.he || s.faust) return false;
            float facing = Vector3.Dot(s.vel.normalized, target.Forward);          // -1: straight into its front, +1: into its rear
            float chance = facing < -0.5f ? 0.1f : facing > 0.5f ? 0.02f : 0.05f;    // rare: a surprise, not a rule
            if (target.spec == VehicleSpec.Tiger || target.spec == VehicleSpec.TigerAce) chance *= 1.5f;
            if (s.friendly && s.dmg >= 2f) chance *= 0.5f;
            return Random.value < chance;
        }

        /// <summary>Where something is, the way a commander calls it: "2 o'clock", from the leader's heading.</summary>
        string Clock(Vector3 at)
        {
            var L = Leader; if (L == null) return ""; var d = at - L.transform.position; d.y = 0f;
            float rel = Mathf.DeltaAngle(L.yaw * Mathf.Rad2Deg, Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg);
            int hour = Mathf.RoundToInt(rel / 30f); if (hour <= 0) hour += 12;
            return hour + " o'clock";
        }

        static void Buzz()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

        /// <summary>The next objective: a point 110 to 170 m ahead, within forty degrees of north, marked with green
        /// smoke and a light. Reaching it is worth 300 and the next one is set.</summary>
        void NextObjective()
        {
            var L = Leader; float ang = (Random.value - 0.5f) * 80f * Mathf.Deg2Rad, dist = 110f + Random.value * 60f;
            var pos = L.transform.position + new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang)) * dist;
            bool hold = objectivesReached > 0 && Random.value < 0.5f;
            objective = new Objective { pos = pos, n = objectivesReached + 1, hold = hold, marker = fx.Marker(pos, new Color(0.35f, 0.95f, 0.45f), hold ? 15f : 9f) };
            if (phase == Phase.Play) hud.Toast("Objective " + objective.n + " · " + (hold ? "hold the crossing, " : "") + Mathf.RoundToInt(dist) + " m ahead", 3f);
        }

        void TickObjective(float dt)
        {
            if (objective == null) return; var L = Leader;
            objective.smokeTimer -= dt; if (objective.smokeTimer <= 0f) { objective.smokeTimer = 0.45f; fx.Signal(objective.pos + Vector3.up * 0.6f, new Color(0.3f, 0.8f, 0.4f, 0.75f)); }
            objective.pos = props.PushOut(objective.pos, 5f);   // once its cell is loaded, it steps out of hedges and walls
            objective.marker.position = objective.pos;
            var d = objective.pos - L.transform.position; d.y = 0f;
            if (objective.hold)
            {
                // hold: twenty-five seconds inside the ring, in one go or in pieces; the ring turns amber while it counts
                bool inside = d.magnitude < 15f; if (inside) objective.held += dt;
                if (inside != (objective.marker.GetComponentInChildren<Light>().color.g < 0.8f)) fx.Tint(objective.marker, inside ? new Color(1f, 0.75f, 0.3f) : new Color(0.35f, 0.95f, 0.45f));
                if (inside) hud.Objective(objective.pos, 0f, cam, true, "Hold " + Mathf.CeilToInt(25f - objective.held) + " s");
                if (objective.held >= 25f)
                {
                    score += 450; objectivesReached++; shake = Mathf.Max(shake, 0.3f); Sfx.Pickup();
                    hud.Popup(objective.pos, "+450", new Color(0.35f, 0.95f, 0.45f)); hud.Toast("Crossing held · +450", 2.6f);
                    Destroy(objective.marker.gameObject); objective = null; NextObjective();
                }
                return;
            }
            if (d.magnitude < 12f)
            {
                score += 300; objectivesReached++; shake = Mathf.Max(shake, 0.3f); Sfx.Pickup();
                hud.Popup(objective.pos, "+300", new Color(0.35f, 0.95f, 0.45f)); hud.Toast("Objective " + objective.n + " reached · +300", 2.6f);
                Destroy(objective.marker.gameObject); objective = null; NextObjective();
            }
        }

        void TickObjectiveLabel() { }

        /// <summary>A badly hit vehicle trails engine smoke.</summary>
        void Smoulder(Vehicle v, float dt)
        {
            if (v.dead || v.spec.isGun || v.hp > v.spec.hp * 0.45f) return;
            v.smokeTimer -= dt; if (v.smokeTimer > 0f) return; v.smokeTimer = 0.3f;
            fx.EngineSmoke(v.transform.position - v.Forward * 2f + Vector3.up * 2f);
        }

        /// <summary>3:20: four Panzer IV and a Tiger come up from behind the platoon.</summary>
        void Counterattack()
        {
            var L = Leader; var f = L.Forward; var r = new Vector3(f.z, 0f, -f.x);
            for (int i = 0; i < 5; i++) Foe(i == 2 ? VehicleSpec.Tiger : VehicleSpec.PanzerIV, L.transform.position - f * (42f + i * 4f) + r * ((i - 2) * 9f), L.yaw);
            hud.Toast("Counterattack from the rear, 6 o'clock", 3f);
        }

        /// <summary>Supply drops: a crate under a parachute comes down near the platoon every minute or so. The leader
        /// drives over it: repair, APCR ammunition for a while, a smoke screen, or a radio that calls the guns.</summary>
        void TickDrops(float dt)
        {
            dropTimer -= dt; var L = Leader;
            if (dropTimer <= 0f)
            {
                dropTimer = 50f + Random.value * 25f;
                float a = Random.value * Mathf.PI * 2f; var pos = props.PushOut(L.transform.position + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * (16f + Random.value * 14f), 3f);
                var d = new Drop { pos = pos, height = 55f, kind = Random.Range(0, 4) };
                if (crateMaterial == null) { crateMaterial = new Material(Resources.Load<Material>("BarrelLit")); crateMaterial.SetColor("_BaseColor", new Color(0.45f, 0.36f, 0.22f)); crateMaterial.SetFloat("_Metallic", 0f); crateMaterial.SetFloat("_Smoothness", 0.2f); chuteMaterial = new Material(Resources.Load<Material>("VehicleLit")); chuteMaterial.SetColor("_BaseColor", new Color(0.85f, 0.82f, 0.72f)); chuteMaterial.SetFloat("_Cull", 0f); }
                d.crate = GameObject.CreatePrimitive(PrimitiveType.Cube).transform; Destroy(d.crate.GetComponent<Collider>()); d.crate.localScale = new Vector3(1.3f, 1f, 1.3f); d.crate.GetComponent<Renderer>().sharedMaterial = crateMaterial;
                d.chute = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform; Destroy(d.chute.GetComponent<Collider>()); d.chute.localScale = new Vector3(6f, 3.2f, 6f); d.chute.GetComponent<Renderer>().sharedMaterial = chuteMaterial;
                d.lines = new GameObject("Shrouds").AddComponent<LineRenderer>(); d.lines.positionCount = 8; d.lines.startWidth = d.lines.endWidth = 0.05f; d.lines.material = chuteMaterial; d.lines.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                drops.Add(d); hud.Toast("Supply drop coming down, " + Clock(pos), 2.8f);
            }
            for (int i = drops.Count - 1; i >= 0; i--)
            {
                var d = drops[i]; d.age += dt;
                if (d.height > 0f)
                {
                    d.height = Mathf.Max(0f, d.height - 6f * dt); var sway = new Vector3(Mathf.Sin(d.age * 1.3f), 0f, Mathf.Cos(d.age * 0.9f)) * 0.6f;
                    d.crate.position = d.pos + Vector3.up * (d.height + 0.5f) + sway; d.chute.position = d.crate.position + Vector3.up * 4.5f;
                    for (int k = 0; k < 4; k++) { float ang = k * Mathf.PI / 2f; d.lines.SetPosition(k * 2, d.crate.position + Vector3.up * 0.5f); d.lines.SetPosition(k * 2 + 1, d.chute.position + new Vector3(Mathf.Cos(ang), -1.2f, Mathf.Sin(ang)) * 2.6f); }
                    if (d.height <= 0f)
                    {
                        // landed: the canopy collapses beside the crate, a light marks it
                        d.chute.position = d.pos + new Vector3(2.5f, 0.15f, 1f); d.chute.localScale = new Vector3(5f, 0.3f, 5f); d.lines.enabled = false;
                        d.marker = fx.Marker(d.pos, new Color(1f, 0.75f, 0.35f), 5f); d.age = 0f;
                    }
                    continue;
                }
                var toL = L.transform.position - d.pos; toL.y = 0f;
                if (toL.magnitude < 4.5f)
                {
                    Sfx.Pickup();
                    switch (d.kind)
                    {
                        case 0: L.hp = Mathf.Min(Depot.LeaderHp + 2f, L.hp + 2f); hud.Toast("Repair kit · leader +2", 2.6f); break;
                        case 1: ammoMul = 1.5f; ammoLeft = 25f; hud.Toast("APCR ammunition · +50% damage for 25 s", 2.8f); break;
                        case 2: if (smokeCooldown <= 0f) PopSmoke(); else { L.hp = Mathf.Min(Depot.LeaderHp + 2f, L.hp + 1f); hud.Toast("Spare parts · leader +1", 2.6f); } break;
                        default: if (!FireMission()) { score += 150; hud.Toast("Radio set · +150", 2.6f); } break;
                    }
                    hud.Popup(d.pos, "Supplies", new Color(1f, 0.85f, 0.5f)); RemoveDrop(d); drops.RemoveAt(i);
                }
                else if (d.age > 70f) { RemoveDrop(d); drops.RemoveAt(i); }
            }
        }

        /// <summary>Enemy mortars from 1:30: five rounds around a point near the platoon, red rings on the ground two
        /// seconds ahead of them. Drive out of the rings.</summary>
        void TickMortars(float dt)
        {
            var L = Leader;
            if (t > 90f) { mortarTimer -= dt; if (mortarTimer <= 0f) {
                mortarTimer = 32f + Random.value * 18f; var r = new Vector3(L.Forward.z, 0f, -L.Forward.x);
                var centre = L.transform.position + L.Forward * Random.Range(-4f, 12f) + r * Random.Range(-9f, 9f);
                for (int i = 0; i < 5; i++) { var at = centre + new Vector3(Random.Range(-7f, 7f), 0f, Random.Range(-7f, 7f)); float when = 2.3f + i * 0.3f; mortars.Add(new Mortar { at = at, timer = when, ring = fx.Marker(at, new Color(1f, 0.25f, 0.2f), 7f) }); fx.Incoming(at, when); }
                Sfx.Whistle(centre); hud.Toast("Incoming! Mortars, " + Clock(centre) + " · move", 2.6f);
            } }
            for (int i = mortars.Count - 1; i >= 0; i--)
            {
                var mo = mortars[i]; mo.timer -= dt; if (mo.timer > 0f) continue;
                Destroy(mo.ring.gameObject); fx.Explosion(mo.at); Sfx.Artillery(mo.at); props.Crater(mo.at, 3.5f); mortars.RemoveAt(i); shake = Mathf.Max(shake, 0.4f);
                foreach (var v in platoon.ToArray()) { if (v.dead) continue; var d = v.transform.position - mo.at; d.y = 0f; if (d.magnitude < 6f) Damage(v, 1f, mo.at); }
                InfantryKilled(infantry.Blast(mo.at, 6f), mo.at);
                if (phase != Phase.Play) return;
            }
        }

        /// <summary>Star shells from 1:00: a flare on a parachute comes down over the platoon and burns for sixteen
        /// seconds; whoever is within 40 m of it is lit up, and the enemy shoots straighter and farther.</summary>
        void TickStar(float dt)
        {
            var L = Leader;
            if (star == null && t > 60f) { starTimer -= dt; if (starTimer <= 0f) {
                starTimer = 45f + Random.value * 25f;
                star = new Star { pos = L.transform.position + new Vector3(Random.Range(-8f, 8f), 0f, Random.Range(-4f, 12f)), height = 42f, life = 16f, flare = fx.StarFlare() };
                star.chute = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform; Destroy(star.chute.GetComponent<Collider>()); star.chute.localScale = new Vector3(3f, 1.6f, 3f); star.chute.GetComponent<Renderer>().sharedMaterial = chuteMaterial != null ? chuteMaterial : star.chute.GetComponent<Renderer>().material;
                star.light = new GameObject("StarLight").AddComponent<Light>(); star.light.type = LightType.Point; star.light.color = new Color(1f, 0.96f, 0.86f); star.light.range = 80f; star.light.intensity = 70f; star.light.shadows = LightShadows.None;
                Sfx.Flak(star.pos + Vector3.up * 30f); hud.Toast("Star shell! You are lit up · move", 3f);
            } }
            if (star == null) { lit = false; return; }
            star.life -= dt; star.height = Mathf.Max(6f, star.height - 1.7f * dt); star.pos += new Vector3(0.5f, 0f, 0.3f) * dt;
            var at = star.pos + Vector3.up * star.height; star.flare.position = at; star.flare.GetChild(0).rotation = cam.transform.rotation; star.chute.position = at + Vector3.up * 3f; star.light.transform.position = at;
            float burn = Mathf.Clamp01(star.life / 3f) * (0.85f + 0.15f * Mathf.PerlinNoise(Time.time * 9f, 0.5f)); star.light.intensity = 70f * burn; star.flare.GetChild(0).localScale = Vector3.one * (7f * (0.5f + 0.5f * burn));
            star.puff -= dt; if (star.puff <= 0f) { star.puff = 0.25f; fx.Signal(at, new Color(0.8f, 0.8f, 0.8f, 0.5f)); }
            var d = L.transform.position - star.pos; d.y = 0f; lit = star.life > 0f && d.magnitude < 40f;
            if (star.life <= 0f) { fx.Release(star.flare); Destroy(star.chute.gameObject); Destroy(star.light.gameObject); star = null; lit = false; }
        }

        void RemoveDrop(Drop d) { Destroy(d.crate.gameObject); Destroy(d.chute.gameObject); Destroy(d.lines.gameObject); if (d.marker != null) Destroy(d.marker.gameObject); }

        /// <summary>An enemy vehicle into the fight; veteran nights give it half again the hits.</summary>
        Vehicle Foe(VehicleSpec spec, Vector3 pos, float yaw)
        {
            var e = Vehicle.Create(spec, false, pos, yaw); e.turretYaw = e.yaw; if (veteran) e.hp *= 1.5f; if (spec == VehicleSpec.PanzerIV && Random.value < 0.5f) e.flank = Random.value < 0.5f ? -1 : 1; foes.Add(e); return e;
        }

        /// <summary>The coaxial and bow machine guns: a burst at any tank hunter within 24 m in front of the gun or the
        /// hull, a tracer every tenth of a second, one hit in three.</summary>
        void TickMg(Vehicle v, float dt)
        {
            v.mgTimer -= dt; v.mgSound -= dt; if (v.mgTimer > 0f) return;
            var m = infantry.Nearest(v.transform.position, 24f); if (m == null) return;
            var to = m.pos - v.transform.position; to.y = 0f; to.Normalize();
            if (Vector3.Dot(to, v.GunDirection) < 0.5f && Vector3.Dot(to, v.Forward) < 0.7f) return;
            v.mgTimer = 0.1f;
            var from = v.transform.position + Vector3.up * 1.7f + v.GunDirection * 2f;
            fx.MgTracer(from, (to + Random.insideUnitSphere * 0.04f).normalized);
            if (v.mgSound <= 0f) { v.mgSound = 0.75f; Sfx.Mg(v.transform.position); }
            if (Random.value < 0.33f) { infantry.Kill(m); InfantryKilled(1, m.pos); }
        }

        void InfantryKilled(int n, Vector3 at)
        {
            if (n <= 0) return; nightInfantry += n; score += 20 * n; xp += n;
            hud.Popup(at + Vector3.up, "+" + 20 * n, new Color(0.85f, 0.85f, 0.8f));
            if (xp >= xpNeed) LevelUp(); else hud.SetLevel(level, (float)xp / xpNeed);
        }

        /// <summary>A Panzerfaust: a slow rocket from the man's shoulder, two hits when it lands, never a ricochet.</summary>
        void FireFaust(Infantry.Soldier m, Vehicle target)
        {
            var from = m.pos + Vector3.up * 1.4f; var dir = target.transform.position - m.pos; dir.y = 0f; dir.Normalize();
            dir = Quaternion.Euler(0f, Random.Range(-3f, 3f), 0f) * dir;
            var vis = fx.Tracer(new Color(1f, 0.8f, 0.6f, 1f), new Color(1f, 0.5f, 0.3f, 0.6f)); vis.position = from; vis.rotation = Quaternion.LookRotation(cam.transform.forward, dir);
            shells.Add(new Shell { pos = from, vel = dir * 32f, friendly = false, faust = true, dmg = 2f, life = 0.6f, vis = vis });
            fx.MuzzleFlash(from, dir); Sfx.Faust(from);
        }

        void Pause() { if (phase != Phase.Play) return; phase = Phase.Pause; stick.Blocked = true; Sfx.Quiet(true); hud.ShowPause(!Sfx.Muted, !LowQuality); }
        void Resume() { if (phase != Phase.Pause) return; hud.HidePause(); Sfx.Quiet(false); phase = Phase.Play; stick.Blocked = false; }
        void OnApplicationPause(bool paused) { if (paused) Pause(); }   // the phone: a call, the home button

        /// <summary>Turns a wanted direction away from hedges and buildings: the straight way if it is clear, else the
        /// nearest clear way to either side, so vehicles slide along a hedge to its gate instead of pushing at it.</summary>
        Vector2 Steer(Vehicle v, Vector2 wanted)
        {
            float mag = wanted.magnitude; if (mag < 0.05f) return wanted;
            var w = new Vector3(wanted.x, 0f, wanted.y) / mag; float r = v.spec.radius * 0.7f; var at = v.transform.position;
            foreach (var a in steerAngles)
            {
                var d = Quaternion.Euler(0f, a, 0f) * w;
                if (props.Free(at + d * 4f, r) && props.Free(at + d * 8f, r)) return new Vector2(d.x, d.z) * mag;
            }
            return wanted;
        }

        void PopSmoke()
        {
            smokeLeft = 6f; smokeCooldown = 30f; hud.Toast("Smoke!");
            foreach (var v in platoon) for (int i = 0; i < 5; i++) { var o = Random.insideUnitCircle * 4.5f; fx.SmokeCloud(v.transform.position + new Vector3(o.x, 1.5f + Random.value * 1.5f, o.y), 5f + Random.value * 2f); }
        }

        /// <summary>The artillery card: every so often a salvo of four falls on the thickest group of enemies near the
        /// platoon, never within 12 m of one of ours.</summary>
        /// <summary>A salvo of four on the thickest group of enemies near the platoon, never within 12 m of one of ours.
        /// Returns false when there is nothing worth the shells.</summary>
        bool FireMission()
        {
            Vehicle best = null; int bestN = 0;
            foreach (var e in foes)
            {
                if (e.dead || Dist(e, Leader) > 70f) continue; bool nearOurs = false; foreach (var p in platoon) if (Dist(e, p) < 12f) nearOurs = true; if (nearOurs) continue;
                int k = 0; foreach (var o in foes) if (!o.dead && Dist(e, o) < 10f) k++;
                if (k > bestN) { bestN = k; best = e; }
            }
            if (best == null) return false;
            hud.Toast("Artillery · fire mission"); Sfx.Whistle(best.transform.position);
            for (int i = 0; i < 4; i++) { var at = best.transform.position + new Vector3(Random.Range(-6f, 6f), 0f, Random.Range(-6f, 6f)); float when = 1.1f + i * 0.35f; arty.Add(new ArtyShell { at = at, timer = when }); fx.Incoming(at, when); }
            return true;
        }

        void TickArtillery(float dt)
        {
            if (artyInterval > 0f) { artyTimer -= dt; if (artyTimer <= 0f) artyTimer = FireMission() ? artyInterval : 3f; }
            for (int i = arty.Count - 1; i >= 0; i--)
            {
                var a = arty[i]; a.timer -= dt; if (a.timer > 0f) continue;
                fx.Explosion(a.at); Sfx.Artillery(a.at); props.Crater(a.at, 5f); arty.RemoveAt(i); InfantryKilled(infantry.Blast(a.at, 7f), a.at);
                foreach (var e in foes.ToArray()) { if (e.dead) continue; var d = e.transform.position - a.at; d.y = 0f; if (d.magnitude < 7f) Damage(e, d.magnitude < 3.5f ? 2f : 1f, a.at); }
            }
        }

        void End(bool dawn)
        {
            if (phase == Phase.End) return;
            phase = Phase.End; stick.Blocked = true;
            int earned = Mathf.RoundToInt(score * (veteran ? 1.5f : 1f)) * (doubled ? 2 : 1) + (dawn ? 500 : 0);
            Depot.AddPoints(earned - banked); banked = earned;                       // a revived night banks only what is new
            if (!nightRecorded) { Depot.RecordNight(kills, t); nightRecorded = true; }
            var done = Missions.Report(new Missions.Night { kills = kills, tigers = nightTigers, paks = nightPaks, flares = nightFlares, level = level, time = t, boss = bossKilled, objectives = objectivesReached, infantry = nightInfantry });
            Depot.Tally("kills", kills - talliedKills); talliedKills = kills; Depot.Tally("tigers", nightTigers - talliedTigers); talliedTigers = nightTigers;
            Depot.Tally("guns", nightPaks - talliedGuns); talliedGuns = nightPaks; Depot.Tally("infantry", nightInfantry - talliedInfantry); talliedInfantry = nightInfantry;
            Depot.Tally("objectives", objectivesReached - talliedObjectives); talliedObjectives = objectivesReached;
            if (bossKilled && !talliedAce) { talliedAce = true; Depot.Tally("aces", 1); }
            if (dawn) { Depot.Tally("dawns", 1); if (veteran) Depot.Tally("veteranDawns", 1); }
            if (kills >= 15 && shotsFired > 0 && shotsHit * 2 >= shotsFired) Depot.Tally("sharp", 1);
            if (!logged) { logged = true; Depot.LogNight(winter ? "Ardennes" : "Normandy", kills, t, score, dawn); }
            var medals = Medals.Check();
            int bonus = 0; var lines = new System.Text.StringBuilder(); foreach (var o in done) { bonus += o.reward; lines.Append("\nOrder carried out · " + o.Title + " · +" + o.reward); }
            foreach (var md in medals) { bonus += Medals.Reward; lines.Append("\nMedal · " + md.name + " · +" + Medals.Reward); }
            hud.SetEndPoints(earned + bonus);
            int m = Mathf.FloorToInt(t / 60f), s = Mathf.FloorToInt(t % 60f);
            int acc = shotsFired > 0 ? Mathf.RoundToInt(100f * shotsHit / shotsFired) : 0;
            hud.ShowEnd(dawn, $"{kills} enemy vehicles destroyed · {nightInfantry} infantry\n{m}:{s:00} held · level {level} · {objectivesReached} objectives\nGunnery {acc}% · Score {score * (doubled ? 2 : 1)}" + lines, dawn ? !doubled : !revived);
        }

        void OnAd()
        {
            // the rewarded video is a mock here: the reward is granted after a moment
            if (phase != Phase.End) return;
            hud.SetAdNote("30 s ad · mock, skipping…");
            if (t >= NightLength) { doubled = true; Depot.AddPoints(score); banked += score; hud.SetEndPoints(banked); hud.ShowEnd(true, $"{kills} enemy vehicles destroyed\nScore {score * 2} (doubled)", false); return; }
            revived = true;
            var L = Vehicle.Create(VehicleSpec.Sherman, true, platoon.Count > 0 ? platoon[0].transform.position - platoon[0].Forward * 8f : Vector3.zero, platoon.Count > 0 ? platoon[0].yaw : 0f);
            L.hp = Depot.LeaderHp; platoon.Insert(0, L); leaderShield = 4f;
            hud.HideEnd(); phase = Phase.Play; stick.Blocked = false; hud.Toast("Field repair · back in the fight");
        }
    }
}

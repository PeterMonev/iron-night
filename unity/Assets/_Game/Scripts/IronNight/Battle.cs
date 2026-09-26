using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace IronNight
{
    /// <summary>
    /// One night assault: the platoon (leader driven by the thumb, wingmen holding formation, every turret aiming on its
    /// own), the enemy waves and armoured columns, shells and hits, ammunition crates salvaged from the wrecks, XP and the
    /// level-up cards, the night lighting and the camera. Everything is built in code from the generated meshes.
    /// </summary>
    public partial class Battle : MonoBehaviour
    {
        class Shell { public Vector3 pos, vel; public bool friendly, he, bounced, faust; public float dmg, life, trail; public Transform vis; }
        // the racks: what the leader carries into the night and what is left of it
        int apRounds = 34, heRounds = 12, apMax = 34, heMax = 12; bool loadHe, heAuto; float dryToast;
        float abilityLeft, abilityCool, spotLeft;   // the commander's one card, and the ten seconds his spotting lasts
        // what the night's cards have given the platoon
        bool wetStowage, savedTonight, sandbags, dozer, canister, phosphorus, radioNet, aceGunner, recovery, wideTracks, spotterPlane;
        float planeTimer = 30f; readonly List<Vehicle> burning = new List<Vehicle>();
        readonly Dictionary<Vehicle, float> burn = new Dictionary<Vehicle, float>();
        enum Weather { Clear, Overcast, Fog, Rain }
        static bool LowQuality { get => PlayerPrefs.GetInt("quality", 1) == 0; set { PlayerPrefs.SetInt("quality", value ? 0 : 1); PlayerPrefs.Save(); } }
        class Sky { public Color ambient, fog, moonColor; public float fogDensity, moonIntensity, shadow; }
        class Wreck { public Vehicle v; public float age, burnTimer; public Light fire; }
        class ArtyShell { public Vector3 at; public float timer; }
        class Objective { public Vector3 pos; public Transform marker; public GameObject grenade; public float smokeTimer, held, salvo, clock; public int n, hits; public bool hold; public string kind = "reach"; public List<Vehicle> targets = new List<Vehicle>(); public List<GameObject> props = new List<GameObject>(); public List<Transform> figures = new List<Transform>(); }
        class Mine { public Vector3 pos; public Transform vis; public bool friendly; }   // friendly: laid in a last stand, set off by the enemy only
        class Drop { public Vector3 pos; public float height, age, lift, top; public int kind; public Transform crate, chute, marker; public LineRenderer lines; public Mesh canopy; public Vector3[] rest; }   // lift: the crate origin above its base; top: where the shrouds tie
        class Mortar { public Vector3 at; public float timer; public Transform ring; public bool bomb; }   // bomb: a dive bomber's, heavier
        class Star { public Vector3 pos; public float height, life, puff; public Transform flare, chute; public Light light; }
        enum Phase { Title, Play, LevelUp, Pause, End }

        static readonly float NightLength = NightArg();   // five minutes of darkness, dawn at 5:00 (--night=30 for tests)
        static float NightArg() { foreach (var a in System.Environment.GetCommandLineArgs()) if (a.StartsWith("--night=")) return float.Parse(a.Substring(8)); return 300f; }
        const float ShellSpeed = 62f, EnemyShellSpeed = 55f, GroundTile = 40f;

        Camera cam; Hud hud; TouchStick stick; Fx fx; Props props; Tracks tracks; Infantry infantry;
        int nightInfantry, combo; float lastKill = -10f; bool veteran;
        Weather weather; Sky sky; float rumbleTimer = 12f, flicker, lightningTimer = 8f, lightning, thunderIn, enemyRangeMul = 1f, ammoMul = 1f, ammoLeft, dropTimer = debugDrops ? 3f : 35f; ParticleSystem rain;
        Objective objective; int objectivesReached; bool winter;
        readonly List<Mortar> mortars = new List<Mortar>(); float mortarTimer = 100f, starTimer = 70f; Star star; bool lit; int shotsFired, shotsHit;
        int talliedKills, talliedTigers, talliedGuns, talliedInfantry, talliedObjectives; bool talliedAce, logged; readonly List<Drop> drops = new List<Drop>(); Material crateMaterial, chuteMaterial;
        class Salvage { public Vector3 pos; public int ap, he; public float age; public Transform crate; public Light glow; }
        readonly List<Salvage> salvage = new List<Salvage>(); readonly List<Vector3> cratePos = new List<Vector3>(); Material salvageMaterial;
        class HeHole { public Vehicle v; public Vector3 local; public float left, tick; }
        readonly List<HeHole> holes = new List<HeHole>();
        class Toss { public Transform t; public Vehicle owner; public Vector3 vel, spin; public Quaternion rest; public float jet, tick, h, restY; public bool bounced, down; }
        readonly List<Toss> tosses = new List<Toss>(); float slowLeft; readonly bool[] rubbleDone = new bool[2];   // turrets in the air; real seconds of slow motion left
        Light flareLight, moon; float shake; int banked, nightTigers, nightPaks, nightCrates; bool nightRecorded, bossKilled;
        readonly List<Vehicle> platoon = new List<Vehicle>(); readonly List<Vehicle> foes = new List<Vehicle>();
        readonly List<Shell> shells = new List<Shell>(); readonly List<Wreck> wrecks = new List<Wreck>();
        Material shellTemplate; Texture2D glowTex;
        Formation formation = Formation.Wedge; Phase phase = Phase.Title; bool reserveGranted;
        Operations.Op op; Operations.Night opN; int opNight, wingmenLost; float goalsTick;
        bool daily; string dailyDay = "", rule = "";   // the daily challenge: its day and the rule of the night
        int crewSteady, crewEagle, crewSnap, crewHands, crewRacks, crewHe, crewFoot, crewMech, crewRough, crewBow, crewSignals, crewSpot;   // the levels of the crew's gifts (Crew)
        float AirCoolFull => 60f - 6f * crewSignals;
        string leaderId = "", leaderName = "", careerLine = ""; int careerXp, careerKills, crewXpBanked; bool careerNight, careerDawn; float careerDamage = 1f, careerReload = 1f, careerSpeed = 1f;   // the leader's own tank   // an operation night: the operation and which of its nights (0: a free night)
        int nightTracked, nightFocus, talliedTracked, nightLamps, talliedLamps, nightAces; bool litBySearchlight;
        bool crewCounted, crewLostTonight; int crewBefore;   // the crew's nights: counted at dawn, lost with the leader unless he is pulled back
        Vehicle focus; float focusLeft; Transform focusRing;   // the enemy the platoon was told to hit
        float pointLeft; Vector3 point;                      // a spot the platoon was told to shell (the fuel dump)
        readonly List<Mine> mines = new List<Mine>(); float mineTimer = 130f; static Material mineMaterial;
        bool endless, daybreak;                              // holding on past dawn
        Infantry.Soldier observer; float observerTimer = 95f, observerAge; Transform observerRing;   // the forward observer on the flank
        float t, spawnTimer = 6f, leaderShield; int level = 1, xp, xpNeed = 6, score, kills, maxPlatoon = 4, reinforcements;
        bool wave1, wave2, wave3, wave4, revived, doubled, bossSpawned; Vehicle boss;
        Vehicle ace; string aceName; float aceTimer = 105f, weatherTurn;
        string theatre = "normandy", builtTheatre = "normandy";   // the map: it also decides the nation
        string TheatreName => theatre == "kursk" ? "Kursk" : winter ? "Ardennes" : "Normandy";
        string route = "open", builtRoute = "open"; float routePay = 1.1f;   // the way in the player chose, the one the country was built for, and what it pays
        enum Order { Follow, Hold, Advance } Order order = Order.Follow; Vector3[] holdAt = new Vector3[8];   // what the wingmen were told   // a named enemy of the night, and the hour the weather turns
        static readonly string[] AceNames = { "Hptm. Keller", "Ofw. Brandt", "Ltn. Hoffmann", "Fw. Ziegler", "Hptm. Vogel", "Ofw. Reinhardt", "Ltn. Stahl", "Fw. Neumann" };
        static readonly bool debugBoss = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--boss") >= 0;   // test switch: the boss comes at 0:06
        static readonly bool debugDawn = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--dawn") >= 0;   // test switch: the night starts at 4:10
        static readonly bool debugCrew = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--crew") >= 0;
        static readonly bool debugDump = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--dump") >= 0;   // test switch: every objective is a fuel dump
        static readonly bool debugMid = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--mid") >= 0;   // test switch: 1:40, mortars and a star shell at once
        static readonly bool debugZoo = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--zoo") >= 0;   // test switch: one of each new enemy at 0:04
        static readonly bool debugGuns = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--guns") >= 0;   // test switch: a PaK and an 88 out of range ahead, to look at
        static readonly bool debugDrops = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--drops") >= 0;   // test switch: the first supply drop at 0:03
        static readonly bool debugHe = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--he") >= 0;   // test switch: the leader starts loaded with HE
        static readonly bool debugAir = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--air") >= 0;   // test switch: a strike 34 m ahead at 0:05 (with --zoo there is something there)
        static readonly bool debugRaid = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--raid") >= 0;   // test switch: a raid at 0:03 and every 14 s after
        static readonly bool debugRubble = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--rubble") >= 0;   // test switch: heavy blasts 25 m ahead at 0:02 and 0:05, a dozer blade fitted
        static readonly bool debugCook = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--cookoff") >= 0;   // test switch: every kill cooks off, in slow motion
        static readonly bool debugKeil = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--keil") >= 0;   // test switch: a Panzerkeil at 0:04
        static readonly bool debugSalvage = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--salvage") >= 0;   // test switch: three crates ahead at 0:02, the racks nearly dry
        static readonly bool debugInfantry = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--infantry") >= 0;   // test switch: a squad at 0:04
        static readonly bool debugWeak = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--weak") >= 0;   // test switch: every enemy goes down to one shell
        bool debugSquadSent;
        float damageMul = 1f, reloadMul = 1f, rangeMul = 1f, speedMul = 1f, scatterMul = 1f, turretMul = 1f; float heBurst = 5f; bool he, gunners, hasSmoke, fireflyNext, firstNight; string bonus = "";
        VehicleSpec Wingman => VehicleSpec.ById(Depot.WingmanId);
        float artyInterval, artyTimer, smokeLeft, smokeCooldown; int wingmanBonus, hintIndex; readonly List<ArtyShell> arty = new List<ArtyShell>();
        static readonly float[] steerAngles = { 0f, 35f, -35f, 70f, -70f, 110f, -110f };
        static readonly string[] hints = { "Drag anywhere to drive", "The turrets aim and fire on their own", "Wrecks leave ammunition: drive over the crates", "Hedges stop tanks: gates and lanes lead through", "Farm buildings stop shells: use them as cover" };
        static readonly float[] hintTimes = { 0.5f, 6f, 16f, 30f, 50f };

        Vehicle Leader => platoon.Count > 0 ? platoon[0] : null;

        public void Build(Camera camera, Hud h, TouchStick s, Fx effects)
        {
            cam = camera; hud = h; stick = s; fx = effects; Time.timeScale = 1f;
            shellTemplate = Resources.Load<Material>("Additive"); glowTex = Lightswarm.ProceduralSprites.Glow(64, 0.3f).texture;
            route = PlayerPrefs.GetString("route", "open"); foreach (var arg in System.Environment.GetCommandLineArgs()) if (arg.StartsWith("--route=")) route = arg.Substring(8);
            theatre = PlayerPrefs.GetString("theatre", "normandy"); foreach (var arg in System.Environment.GetCommandLineArgs()) if (arg.StartsWith("--theatre=")) theatre = arg.Substring(10);   // test switch: --theatre=kursk
            Depot.Load(); if (!Depot.TheatreOpen(theatre) && System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--theatre=" + theatre) < 0) theatre = "normandy";
            // an operation night brings its own front and way in
            var launch = PlayerPrefs.GetString("op.launch", ""); PlayerPrefs.DeleteKey("op.launch"); foreach (var arg in System.Environment.GetCommandLineArgs()) if (arg.StartsWith("--op=")) launch = arg.Substring(5);   // test switch: --op=cobra:3
            if (launch.Length > 0) { var lp = launch.Split(':'); op = Operations.ById(lp[0]); if (op != null && lp.Length > 1 && int.TryParse(lp[1], out opNight) && opNight >= 1 && opNight <= op.nights.Length) { opN = op.nights[opNight - 1]; theatre = op.theatre; route = opN.route; } else { op = null; opNight = 0; } }
            var dl = PlayerPrefs.GetString("daily.launch", ""); PlayerPrefs.DeleteKey("daily.launch");
            foreach (var arg in System.Environment.GetCommandLineArgs()) { if (arg == "--daily") dl = Daily.Today; else if (arg.StartsWith("--daily=")) dl = arg.Substring(8); }
            if (opNight == 0 && dl.Length == 8)
            {
                daily = true; dailyDay = dl; rule = Daily.RuleOf(dl).id; foreach (var arg in System.Environment.GetCommandLineArgs()) if (arg.StartsWith("--rule=")) rule = arg.Substring(7);
                theatre = Daily.Theatre(dl); route = Daily.Route(dl); Random.InitState(Daily.Seed(dl));   // the same first throw of the dice for everyone
            }
            // a last stand holds a crossroads in a village on the chosen front
            var sl = PlayerPrefs.GetString("stand.launch", ""); PlayerPrefs.DeleteKey("stand.launch"); foreach (var arg in System.Environment.GetCommandLineArgs()) if (arg == "--stand") sl = theatre;   // test switch: --stand
            if (opNight == 0 && !daily && sl.Length > 0) { stand = true; theatre = sl; route = "village"; }
            builtRoute = route; Props.Route = route; routePay = stand ? 0.5f : route == "village" ? 1.2f : route == "bocage" ? 1.15f : 1.1f;   // the country is built for this way in
            PickWeather(); weatherTurn = NightLength * Random.Range(0.35f, 0.62f);
            builtTheatre = theatre; Props.Theatre = theatre; if (theatre == "kursk") routePay *= 1.15f; if (opNight > 0 || daily) weatherTurn = 0f; string nation = theatre == "kursk" ? "su" : "us"; if (Depot.Nation != nation) Depot.Nation = nation;
            BuildWorld();
            // the night always starts with the leader alone; the platoon grows from the salvaged crates to 3, a rewarded ad opens a 4th slot.
            // The depot's permanent upgrades set the starting numbers
            Depot.Load(); firstNight = Depot.NightsFought == 0;
            reloadMul = Depot.ReloadMul; rangeMul = Depot.RangeMul * (Depot.CrewLevel >= 3 ? 1.05f : 1f); speedMul = Depot.SpeedMul; maxPlatoon = rule == "alone" ? 1 : 3;
            if (rule == "stukas") raidTimer = 5f; if (rule == "aces") aceTimer = 60f;
            if (weather == Weather.Fog) { rangeMul *= 0.8f; enemyRangeMul = 0.8f; } else if (weather == Weather.Overcast) enemyRangeMul = 0.9f; else if (weather == Weather.Rain) { speedMul *= 0.92f; enemyRangeMul = 0.95f; }
            hud.SetConditions((stand ? "Last stand · " : daily ? "Daily · " : "") + TheatreName + (route == "village" ? " · village" : route == "bocage" ? (theatre == "kursk" ? " · tree belts" : " · bocage") : (theatre == "kursk" ? " · steppe" : " · open fields")), winter && weather == Weather.Rain ? "Snow" : weather.ToString(), weather == Weather.Fog ? "everyone sees 20% less" : weather == Weather.Overcast ? "a dark night, the enemy sees 10% less" : weather == Weather.Rain ? (winter ? "the platoon slows in the drifts, the enemy sees 5% less" : "mud slows the platoon, the enemy sees 5% less") : "");
            hud.OnQuality = () => { LowQuality = !LowQuality; ApplyQuality(); hud.SetQualityLabel(!LowQuality); };
            if (veteran) hud.Toast("Veteran night · points ×1.5", 3f);
            hud.OnDaily = () => { Depot.ClaimDaily(); hud.ShowTitle(reserveGranted); };
            var leaderSpec = VehicleSpec.ById(Depot.LeaderId); if (!VehicleSpec.Available(leaderSpec)) leaderSpec = VehicleSpec.ById(Depot.WingmanId);
            foreach (var arg in System.Environment.GetCommandLineArgs()) if (arg.StartsWith("--tank=")) leaderSpec = VehicleSpec.ById(arg.Substring(7));   // test switch: --tank=is2
            var startAt = Vector3.zero; foreach (var arg in System.Environment.GetCommandLineArgs()) if (arg.StartsWith("--at=")) { var xz = arg.Substring(5).Split(','); startAt = new Vector3(float.Parse(xz[0]), 0f, float.Parse(xz[1])); }   // test switch: --at=0,95
            foreach (var arg in System.Environment.GetCommandLineArgs()) if (arg == "--careertest") Career.Add(leaderSpec.id, 4000, 320, false, false, 0);   // test switch: a seasoned tank
            platoon.Add(Vehicle.Create(leaderSpec, true, startAt, 0f)); Leader.hp = Depot.LeaderHp;
            leaderId = leaderSpec.id; leaderName = leaderSpec.name; careerDamage = Career.DamageMul(leaderId); careerReload = Career.ReloadMul(leaderId); careerSpeed = Career.SpeedMul(leaderId); Leader.KillRings(Career.Rings(leaderId));
            { var n = Depot.Nation; crewSteady = Crew.Gift(n, "steady"); crewEagle = Crew.Gift(n, "eagle"); crewSnap = Crew.Gift(n, "snap"); crewHands = Crew.Gift(n, "hands"); crewRacks = Crew.Gift(n, "racks"); crewHe = Crew.Gift(n, "he");
              crewFoot = Crew.Gift(n, "foot"); crewMech = Crew.Gift(n, "mech"); crewRough = Crew.Gift(n, "rough"); crewBow = Crew.Gift(n, "bow"); crewSignals = Crew.Gift(n, "signals"); crewSpot = Crew.Gift(n, "spot");
              heBurst = 5f + 0.4f * crewHe; hud.radarMul = 1f + 0.08f * crewSpot; }
            // the commander riding with the leader
            bonus = Depot.CommanderBonus; commander = System.Array.Find(Depot.Commanders, c => c.id == Depot.CommanderId && c.nation == Depot.Nation); if (bonus == "reload") reloadMul *= 0.85f; if (bonus == "speed") speedMul *= 1.12f; if (bonus == "armour") Leader.hp += 1f;
            apMax += Depot.Level("ammo") * 6 + 3 * crewRacks; heMax += Depot.Level("ammo") * 3 + crewRacks; apRounds = apMax; heRounds = heMax; if (debugHe) { heMax = heRounds = 60; loadHe = true; }
            if (rule == "short") { apRounds = Mathf.CeilToInt(apMax * 0.5f); heRounds = Mathf.CeilToInt(heMax * 0.5f); }   // half the racks
            hud.OnAmmo = () => { heAuto = false; loadHe = !loadHe; if (loadHe && heRounds <= 0) loadHe = false; else if (!loadHe && apRounds <= 0 && heRounds > 0) loadHe = true; Sfx.Click(); hud.SetAmmo(apRounds, heRounds, loadHe); };
            hud.OnAbility = UseAbility;
            NextObjective();
            hud.OnFormation = f => formation = f;
            hud.OnOrder = () =>
            {
                order = order == Order.Follow ? Order.Hold : order == Order.Hold ? Order.Advance : Order.Follow;
                if (order == Order.Hold) for (int i = 1; i < platoon.Count && i < holdAt.Length; i++) holdAt[i] = platoon[i].transform.position;
                hud.SetOrder(order == Order.Follow ? "FOLLOW" : order == Order.Hold ? "HOLD" : "ADVANCE");
                hud.Toast(order == Order.Follow ? "Platoon, on me" : order == Order.Hold ? "Platoon, hold here" : "Platoon, push ahead", 1.8f); if (Random.value < 0.5f) Radio("start");
            };
            hud.OnRoute = r => { route = r; PlayerPrefs.SetString("route", r); PlayerPrefs.Save(); };
            hud.OnTheatre = t => theatre = t;
            hud.OnDailyChallenge = () => { PlayerPrefs.SetString("daily.launch", Daily.Today); PlayerPrefs.Save(); StartCoroutine(Curtained(() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex))); };
            hud.OnAir = () => { if (!airUp || airCool > 0f || phase != Phase.Play || strike != null) return; airArmed = !airArmed; airArmedLeft = 8f; hud.Toast(airArmed ? "Tap the target" : "Air strike called off", 2f); Sfx.Click(); };
            hud.OnAd = OnAd; hud.OnAgain = () => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            hud.OnStart = () => hud.ShowRoutes(() =>
            {
                if (route == builtRoute && theatre == builtTheatre) StartCoroutine(Curtained(StartNight));
                else { PlayerPrefs.SetInt("route.launch", 1); PlayerPrefs.Save(); StartCoroutine(Curtained(() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex))); }   // another way in: the country is built again
            });
            hud.garage = Garage.Build(); hud.garage.SetActive(false);
            hud.OnStand = th => { PlayerPrefs.SetString("stand.launch", th); PlayerPrefs.SetString("theatre", th); PlayerPrefs.Save(); StartCoroutine(Curtained(() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex))); };
            hud.OnOperation = (id, n) => { PlayerPrefs.SetString("op.launch", id + ":" + n); PlayerPrefs.Save(); StartCoroutine(Curtained(() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex))); };
            hud.OnDepot = () => { stick.Blocked = true; StartCoroutine(Curtained(hud.ShowDepot)); }; hud.OnHold = HoldOn;
            hud.OnBack = () => { if (phase == Phase.End) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); else StartCoroutine(Curtained(() => hud.ShowTitle(reserveGranted))); };
            hud.OnReserveAd = () => { reserveGranted = true; maxPlatoon = 4; hud.ShowTitle(true); };   // the ad is a mock: granted at once
            hud.OnPause = Pause; hud.OnResume = Resume; hud.OnSound = () => { Sfx.Muted = !Sfx.Muted; hud.ShowPause(!Sfx.Muted, !LowQuality); };
            hud.OnQuit = () => { Resume(); revived = true; End(false); };   // no rewarded repair after walking away
            hud.OnRestart = () => { Time.timeScale = 1f; if (stand) { PlayerPrefs.SetString("stand.launch", theatre); PlayerPrefs.Save(); } if (opNight > 0) { PlayerPrefs.SetString("op.launch", op.id + ":" + opNight); PlayerPrefs.Save(); } if (daily) { PlayerPrefs.SetString("daily.launch", dailyDay); PlayerPrefs.Save(); } SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); };
            if (debugDawn) t = 250f; if (debugMid) { t = 100f; mortarTimer = 6f; starTimer = 9f; observerTimer = 4f; mineTimer = 6f; }
            hud.Set(t, platoon.Count); hud.SetLevel(level, 0f);
            PlaceCamera(true);
            stick.Blocked = true; hud.ShowTitle(false);
            foreach (var arg in System.Environment.GetCommandLineArgs()) if (arg.StartsWith("--garage=")) { stick.Blocked = true; hud.ShowGarage(VehicleSpec.ById(arg.Substring(9))); } else if (arg.StartsWith("--dossier=")) { stick.Blocked = true; hud.ShowDossier(arg.Substring(10)); }
            hud.OnStart += () => Radio("start");
            if (opNight == 0 && !daily && PlayerPrefs.GetInt("route.launch", 0) == 1) { PlayerPrefs.SetInt("route.launch", 0); PlayerPrefs.Save(); StartNight(); Radio("start"); }
            if (stand) { StandBuild(); StartNight(); Radio("start"); string sp = Hud.UiSprite("stand_" + theatre) != null ? "stand_" + theatre : theatre == "kursk" ? "route_kursk_village" : theatre == "ardennes" ? "campaign_ardennes" : "campaign_lastpush"; hud.Briefing(sp, "Last stand · " + TheatreName, "Hold the crossroads through ten waves. Between them, dig in: sandbags, hedgehogs, mines."); }
            if (daily) { StartNight(); Radio("start"); var dr = Daily.RuleOf(dailyDay); hud.Briefing(Daily.Picture(dailyDay), "Daily challenge · " + dr.name, dr.line); }
            if (opNight > 0) { StartNight(); Radio("start"); hud.Briefing(Hud.UiSprite(opN.picture) != null ? opN.picture : op.cover, op.name + " · night " + opNight + " of " + op.nights.Length, opN.name + " · " + opN.second.text.ToLowerInvariant() + ", " + opN.third.text.ToLowerInvariant()); } Debug.Log("Iron Night: battle built, debugBoss=" + debugBoss + " args=" + string.Join(" ", System.Environment.GetCommandLineArgs()));
        }

        void BuildWorld()
        {
            // the ground, the fields, lanes, hedges, farms and searchlight posts are Props, built cell by cell around the camera
            props = new GameObject("Props").AddComponent<Props>(); props.winter = winter; props.wet = weather == Weather.Rain && !winter; Vehicle.Wet = props.wet; props.Build(cam); props.fx = fx;
            tracks = new GameObject("Tracks").AddComponent<Tracks>(); tracks.Build();
            infantry = new GameObject("Infantry").AddComponent<Infantry>(); infantry.Build(); veteran = Depot.Veteran || rule == "veteran";

            // night: moonlight with soft shadows, a cold ambient, fog swallowing the distance
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = sky.ambient;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Exponential; RenderSettings.fogDensity = sky.fogDensity; RenderSettings.fogColor = sky.fog;
            var moonGo = new GameObject("Moon"); moon = moonGo.AddComponent<Light>();
            moon.type = LightType.Directional; moon.color = sky.moonColor; moon.intensity = sky.moonIntensity; moon.shadows = LightShadows.Soft; moon.shadowStrength = sky.shadow;
            if (weather == Weather.Rain) { BuildRain(); if (!winter) props.SetWet(0.5f); } Sfx.Ambient(weather == Weather.Rain && !winter);
            ApplyQuality();
            moonGo.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
            // the flare light over the platoon: warm, follows the leader, grows with the platoon
            var fl = new GameObject("FlareLight"); flareLight = fl.AddComponent<Light>();
            flareLight.type = LightType.Point; flareLight.color = new Color(1f, 0.72f, 0.4f); flareLight.intensity = 18f; flareLight.range = 40f; flareLight.shadows = LightShadows.None;
        }

        void PlaceCamera(bool snap)
        {
            var L = Leader; if (L == null) return;
            var want = L.transform.position + new Vector3(0f, 52f, -42.5f);   // a little higher than before: more field in view, same 48-degree tilt
            cam.transform.position = snap ? want : Vector3.Lerp(cam.transform.position, want, 1f - Mathf.Exp(-Time.deltaTime * 4f));
            cam.transform.LookAt(cam.transform.position + new Vector3(0f, -44f, 40f));
            if (shake > 0f) { cam.transform.position += Random.insideUnitSphere * (shake * 0.5f); shake = Mathf.Max(0f, shake - Time.deltaTime * 3f); }
            flareLight.transform.position = L.transform.position + Vector3.up * 11f;
        }

        void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            if (slowLeft > 0f) { slowLeft -= Time.unscaledDeltaTime; Time.timeScale = slowLeft <= 0f ? 1f : slowLeft < 0.25f ? Mathf.Lerp(1f, 0.3f, slowLeft / 0.25f) : 0.3f; }
            fx.Tick(dt);
            var kb = Keyboard.current; if (kb != null && kb.escapeKey.wasPressedThisFrame) { if (phase == Phase.Play) Pause(); else if (phase == Phase.Pause) Resume(); }
            if (phase != Phase.Play) { PlaceCamera(false); TickWrecks(dt); TickTosses(dt); Sfx.Engine(0f); props.Tick(); return; }
            t += dt;
            if (firstNight && hintIndex < hints.Length && t >= hintTimes[hintIndex]) { hud.Toast(hints[hintIndex], 3.5f); hintIndex++; }
            Lighting(stand ? standDawn : Mathf.Clamp01((t - 255f) / 45f), dt);
            if (smokeLeft > 0f) smokeLeft -= dt; if (smokeCooldown > 0f) smokeCooldown -= dt;
            TickArtillery(dt);
            var L = Leader;
            if (leaderShield > 0f) leaderShield -= dt;
            if (rain != null) rain.transform.position = L.transform.position + L.Forward * 20f + Vector3.up * 30f; if (splashes != null) splashes.transform.position = L.transform.position + L.Forward * 10f + Vector3.up * 0.06f;

            // the leader drives where the thumb points; the wingmen hold their slots
            if (stick.Active) L.Drive(stick.Direction, dt); if (stand) StandKeep(L);
            for (int i = 1; i < platoon.Count; i++)
            {
                var v = platoon[i]; var slot = Slot(i);
                if (order == Order.Hold && i < holdAt.Length) slot = holdAt[i];
                else if (order == Order.Advance) slot += L.Forward * 22f;   // pushed out ahead of the leader, still in formation
                var d = slot - v.transform.position; d.y = 0f;
                if (d.magnitude > 1.2f) v.Drive(Steer(v, new Vector2(d.x, d.z) * (Mathf.Clamp01(d.magnitude / 5f))), dt);
            }
            TickAbility(dt); TickCards(dt); if (phase != Phase.Play) return;
            TickAce(dt); TickWeatherTurn(dt); TickAir(dt);
            if (opNight > 0) { goalsTick -= dt; if (goalsTick <= 0f) { goalsTick = 0.5f; hud.SetGoals(GoalLine(opN.second) + "      " + GoalLine(opN.third)); } }
            bool fast = abilityLeft > 0f && bonus == "reload", hard = abilityLeft > 0f && bonus == "heavy", quick = abilityLeft > 0f && bonus == "speed";
            foreach (var v in platoon) { if (v.trackOut > 0f) v.trackOut -= dt; v.speedMul = speedMul * (quick ? 1.6f : 1f); v.damageMul = damageMul * (hard ? 1.8f : 1f); v.rangeMul = rangeMul; v.reloadMul = reloadMul * (fast ? 0.45f : 1f) * (radioNet && v != Leader ? 0.75f : 1f); v.turretMul = turretMul; if (v == Leader) { v.speedMul *= careerSpeed * (1f + 0.03f * crewFoot); v.damageMul *= careerDamage * (1f + 0.03f * crewSteady); v.reloadMul *= careerReload * (1f - 0.03f * crewHands); v.rangeMul *= 1f + 0.03f * crewEagle; v.turretMul *= 1f + 0.08f * crewSnap; } }

            // a tap on an enemy: every gun onto it for eight seconds
            if (stick.ConsumeTap() && phase == Phase.Play)
            {
                var ray = cam.ScreenPointToRay(stick.TapAt); if (ray.direction.y < -0.01f) { var g = ray.origin + ray.direction * (-ray.origin.y / ray.direction.y); bool placed = !airArmed && StandPlace(g); bool called = !placed && airArmed; if (called) { airArmed = false; CallAir(g); } var pick = called || placed ? null : Nearest(foes, g, 9f);
                    if (!called && !placed && pick == null && objective != null && objective.kind == "dump" && new Vector2(g.x - objective.pos.x, g.z - objective.pos.z).magnitude < 10f) { point = objective.pos; pointLeft = 10f; hud.Toast("Shell the fuel dump", 1.6f); Sfx.Click(); }
                    else if (!called && !placed && pick == null) { var man = infantry.Nearest(g, 7f); if (man != null) { point = man.pos; pointLeft = 6f; hud.Toast(man.still ? "Shell the observer" : "Shell the infantry", 1.6f); Sfx.Click(); } }
                    if (pick != null) { focus = pick; focusLeft = 8f; if (focusRing == null) focusRing = fx.Marker(pick.transform.position, new Color(1f, 0.55f, 0.3f), 7f); focusRing.gameObject.SetActive(true); hud.Toast("Focus fire · " + pick.spec.name, 1.6f); Sfx.Click(); } }
            }
            if (focus != null) { focusLeft -= dt; if (focus.dead || focusLeft <= 0f) { focus = null; if (focusRing != null) focusRing.gameObject.SetActive(false); } else focusRing.position = focus.transform.position; }
            if (pointLeft > 0f) { pointLeft -= dt; }
            TickObserver(dt);
            // turrets: nearest foe in range, else drift home
            float leaderTurret = L.turretYaw;
            foreach (var v in platoon)
            {
                v.reloadLeft -= dt;
                var target = Nearest(foes, v.transform.position, v.Range * 1.15f);
                if (focus != null && Dist(v, focus) <= v.Range * 1.15f) target = focus;
                if (pointLeft > 0f && (target == null || focus == null))
                {
                    // told to shell a spot: every gun in range onto it
                    var toP = point - v.transform.position; toP.y = 0f;
                    if (toP.magnitude <= v.Range) { bool onP = v.Aim(point, dt); if (v.spec.casemate) { float offP = Mathf.DeltaAngle(v.yaw * Mathf.Rad2Deg, v.turretYaw * Mathf.Rad2Deg); v.turretYaw = v.yaw + Mathf.Clamp(offP, -14f, 14f) * Mathf.Deg2Rad; onP = onP && Mathf.Abs(offP) <= 14f; } if (onP && v.reloadLeft <= 0f && (v != Leader || apRounds > 0 || heRounds > 0)) Fire(v); v.Apply(); continue; }
                }
                if (target != null)
                {
                    bool on = v.Aim(target.transform.position, dt);
                    if (v.spec.casemate)
                    {
                        // no turret: the gun swings a little either side of the hull; a wingman holding its slot turns the hull itself
                        float off = Mathf.DeltaAngle(v.yaw * Mathf.Rad2Deg, v.turretYaw * Mathf.Rad2Deg);
                        if (v != L && Mathf.Abs(off) > 6f && (Slot(platoon.IndexOf(v)) - v.transform.position).magnitude <= 1.2f) v.yaw += Mathf.Sign(off) * v.spec.turnRate * 0.6f * dt;
                        v.turretYaw = v.yaw + Mathf.Clamp(off, -14f, 14f) * Mathf.Deg2Rad; on = on && Mathf.Abs(off) <= 14f;
                    }
                    if (on && v.reloadLeft <= 0f && Dist(v, target) <= v.Range) { if (v != Leader || apRounds > 0 || heRounds > 0) Fire(v, target); else Dry(); }
                }
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
                var e = foes[i]; e.reloadLeft -= dt; if (e.trackOut > 0f) e.trackOut -= dt;
                var target = Nearest(platoon, e.transform.position, 1000f); if (target == null) continue; e.rangeMul = enemyRangeMul * (lit ? 1.2f : 1f);
                float dist = Dist(e, target);
                if (e.spec.transport) { TickTransport(e, target, dist, dt); continue; }
                if (!e.spec.isGun && !e.spec.casemate && e.hp <= 1f && e.spec.hp >= 3f && e.fallBack <= 0f && e.fallenBack < 1 && dist < e.Range * 0.8f) { e.fallBack = 6f; e.fallenBack++; hud.Toast(e.spec.name + " falling back", 2f); }
                if (e.fallBack > 0f)
                {
                    // reversing out of range, gun still on us
                    e.fallBack -= dt; var away = e.transform.position - target.transform.position; away.y = 0f; e.Drive(Steer(e, new Vector2(away.x, away.z)), dt);
                }
                else if (!e.spec.isGun && dist > e.Range * 0.8f)
                {
                    var goal = target.transform.position;
                    if (e.flank != 0 && dist > 20f) { var toT = goal - e.transform.position; toT.y = 0f; toT.Normalize(); goal += new Vector3(toT.z, 0f, -toT.x) * (e.flank * 18f); }   // a Panzer IV works round the side
                    var d = goal - e.transform.position; e.Drive(Steer(e, new Vector2(d.x, d.z)), dt);
                }
                bool on = e.Aim(target.transform.position, dt);
                if (e.spec.casemate) { if (dist > e.Range * 0.8f) e.turretYaw = e.yaw; else e.yaw = e.turretYaw; }   // the StuG aims with the whole hull
                bool blind = smokeLeft > 0f && dist > 9f;                 // the smoke screen: they cannot see us from afar
                if (on && !blind && e.reloadLeft <= 0f && dist <= e.Range && e.spec.damage > 0f) Fire(e, target);
                e.Apply();
            }

            TickShells(dt); if (phase != Phase.Play) return;                    // the leader may have just died
            TickWrecks(dt); TickSpawns(dt);
            KeepApart();
            foreach (var v in platoon) { Bog(v, props.Ram(v.transform.position, v.spec.radius * 0.7f, v.Forward, dozer), dt); v.transform.position = props.PushOut(v.transform.position, v.spec.radius * 0.7f); }
            foreach (var e in foes) if (!e.spec.isGun) { Bog(e, props.Ram(e.transform.position, e.spec.radius * 0.7f, e.Forward, false), dt); e.transform.position = props.PushOut(e.transform.position, e.spec.radius * 0.7f); }
            foreach (var v in platoon) tracks.Mark(v); foreach (var e in foes) if (!e.spec.isGun) tracks.Mark(e);
            foreach (var v in platoon) Smoulder(v, dt); foreach (var e in foes) Smoulder(e, dt);
            props.platoon = L.transform.position; props.alert = false; props.Tick();
            flareLight.range = 34f + platoon.Count * 3f;
            Sfx.Engine(stick.Active ? stick.Direction.magnitude : 0f);
            PlaceCamera(false);
            hud.Set(t, platoon.Count); hud.SetLeader(Mathf.CeilToInt(L.hp), Mathf.CeilToInt(Depot.LeaderHp)); hud.SetTally(kills, score);
            hud.ReloadArc(L.transform.position + Vector3.up * 0.2f, L.reloadLeft <= 0f ? 1f : 1f - L.reloadLeft / Mathf.Max(0.1f, L.spec.reload * L.reloadMul), cam);
            hud.Indicators(foes, cam);
            TickObjective(dt); TickDrops(dt); TickSalvage(dt); TickHoles(dt); TickTosses(dt); TickMortars(dt); TickStar(dt); TickRaid(dt);
            if (ammoLeft > 0f) { ammoLeft -= dt; if (ammoLeft <= 0f) { ammoMul = 1f; hud.Toast("APCR spent"); } }
            hud.Radar(foes, platoon, L.transform.position, objective != null ? objective.pos : Vector3.zero, objective != null, cratePos);   // the commander's spotting shows them all: the markers are placed when it is called
            hud.HpBars(foes, cam, boss);
            if (objective != null) { var od = objective.pos - L.transform.position; od.y = 0f; hud.Objective(objective.pos, od.magnitude, cam, true); }
            TickObjectiveLabel();
            if (firstNight) Tutorial(dt);
            TickMines(dt);
            if (!stand && t >= NightLength && !endless) { Radio("dawn"); End(true); }
            if (endless && !daybreak && t >= NightLength + 120f) { daybreak = true; Depot.Tally("daybreak", 1); hud.Toast("Two minutes into daylight · Daybreak", 3f); }
        }

        /// <summary>Holding on past dawn: the night goes on in daylight, the enemy sees everything, the points count half again.</summary>
        void HoldOn()
        {
            if (endless || t < NightLength) return;
            endless = true; phase = Phase.Play; stick.Blocked = false; hud.HideEnd(); enemyRangeMul *= 1.25f; spawnTimer = Mathf.Min(spawnTimer, 4f);
            hud.Toast("Daylight · the enemy sees everything · points ×1.5", 3.5f); Radio("start");
        }

        /// <summary>The fuel dump: three shells into the barrels and the lot goes up.</summary>
        void DumpHit(Vector3 at)
        {
            objective.hits++; fx.Hit(at, 0.8f); Sfx.Hit(at); hud.Popup(at, objective.hits >= 3 ? "" : "Fuel dump " + objective.hits + "/3", new Color(1f, 0.7f, 0.4f));
            if (objective.hits < 3) return;
            var pos = objective.pos; score += 500; objectivesReached++; shake = Mathf.Max(shake, 0.9f); Sfx.Explosion(pos); Sfx.Pickup();
            foreach (var p in objective.props) { if (p == null) continue; fx.Explosion(p.transform.position + Vector3.up * 0.5f); props.Crater(p.transform.position, 3f); Destroy(p); }
            fx.Explosion(pos + Vector3.up); props.Crater(pos, 5f); props.Blast(pos, 9f, 4f);
            var fire = new GameObject("DumpFire").AddComponent<Light>(); fire.type = LightType.Point; fire.color = new Color(1f, 0.55f, 0.2f); fire.range = 26f; fire.intensity = 14f; fire.shadows = LightShadows.None; fire.transform.position = pos + Vector3.up * 3f; dumpFires.Add(fire);
            InfantryKilled(infantry.Blast(pos, 9f), pos); foreach (var e in foes.ToArray()) { var d = e.transform.position - pos; d.y = 0f; if (d.magnitude < 9f && !e.dead) Damage(e, 2f, pos); }
            hud.Popup(pos, "+500", new Color(1f, 0.6f, 0.3f)); hud.Toast("Fuel dump destroyed · +500", 2.6f);
            Destroy(objective.marker.gameObject); objective = null; pointLeft = 0f; NextObjective();
        }
        readonly List<Light> dumpFires = new List<Light>();

        /// <summary>The parachute fabric, made once; the star shell's canopy used to come before the first supply drop and showed pink for want of it.</summary>
        Material ChuteMaterial()
        {
            if (chuteMaterial == null) { chuteMaterial = new Material(Resources.Load<Material>("VehicleLit")); chuteMaterial.SetTexture("_BaseMap", Resources.Load<Texture2D>("Fx/chute_fabric")); chuteMaterial.SetTextureScale("_BaseMap", new Vector2(3f, 1f)); chuteMaterial.SetColor("_BaseColor", new Color(1.3f, 1.3f, 1.2f)); chuteMaterial.SetFloat("_Cull", 0f); chuteMaterial.SetFloat("_Smoothness", 0.12f); }
            return chuteMaterial;
        }

        /// <summary>The forward observer: from 1:30 a man with a radio kneels 30 m ahead, off to a side, and stays; while he lives
        /// the mortars come twice as fast and land on the platoon. Killing him (tap him, drive close for the MGs) is worth
        /// 250 and quiets the mortars for two minutes; another one turns up later.</summary>
        void TickObserver(float dt)
        {
            var L = Leader;
            if (observer == null)
            {
                if (t < 90f) return; observerTimer -= dt; if (observerTimer > 0f) return;
                var side = new Vector3(L.Forward.z, 0f, -L.Forward.x) * (Random.value < 0.5f ? 1f : -1f);
                var at = props.PushOut(L.transform.position + L.Forward * Random.Range(26f, 36f) + side * Random.Range(7f, 15f), 1.5f); var face = L.transform.position - at; face.y = 0f;   // ahead and to a side: on the screen, so he can be tapped
                observer = infantry.SpawnObserver(at, face.normalized); observerAge = 0f; observerRing = fx.Marker(at, new Color(1f, 0.35f, 0.3f), 5f, true);
                hud.Toast("Forward observer, " + Clock(at) + " · tap him: he is calling the mortars", 3.4f); Sfx.Click(); if (Random.value < 0.5f) Radio("hit");
                return;
            }
            observerAge += dt;
            if (observer.dead)
            {
                score += 250; hud.Popup(observer.pos, "Observer down · +250", new Color(1f, 0.8f, 0.5f)); hud.Toast("Observer down · the mortars go quiet", 2.8f); mortarTimer += 120f; Depot.Tally("observers", 1);
                Destroy(observerRing.gameObject); observer = null; observerTimer = 150f + Random.value * 60f; return;
            }
            var d = observer.pos - L.transform.position; d.y = 0f;
            if (d.magnitude > 160f || observerAge > 240f) { infantry.Kill(observer); Destroy(observerRing.gameObject); observer = null; observerTimer = 120f; return; }   // left behind: he packs up
            observerRing.position = observer.pos;
        }

        /// <summary>Sappers: from 2:00, every minute or so a half-track comes across the platoon's front sixty metres ahead
        /// (44 m) and drops a mine every four metres - kill it first and none go down. The same row of five mines goes down across the platoon's way, sixty
        /// metres ahead, a two-man team walking off from it. A mine takes a track and a hit from whoever runs onto it - the
        /// enemy too; a shell within two metres sets it off for 40.</summary>
        void TickMines(float dt)
        {
            var L = Leader;
            if (!stand && t > 120f) { mineTimer -= dt; if (mineTimer <= 0f && mines.Count < 12) {
                mineTimer = 60f + Random.value * 30f; var side = new Vector3(L.Forward.z, 0f, -L.Forward.x) * (Random.value < 0.5f ? 1f : -1f); var start = L.transform.position + L.Forward * 44f + side * 34f;
                if (VehicleSpec.Available(VehicleSpec.Halftrack)) { var ht = Foe(VehicleSpec.Halftrack, props.PushOut(start, 2f), Mathf.Atan2(-side.x, -side.z)); ht.sapper = true; ht.unloaded = true; ht.sapperLeft = 5; ht.lastMine = ht.transform.position; hud.Toast("Sapper half-track, " + Clock(start) + " · it is laying mines · kill it", 3.2f); }
                else { for (int i = -2; i <= 2; i++) LayMine(props.PushOut(start - side * 34f + side * (i * 4f), 1f)); hud.Toast("Sappers laid mines ahead, " + Clock(start) + " · shoot them or go round", 3.2f); }
                Sfx.Click();
            } }
            for (int i = mines.Count - 1; i >= 0; i--)
            {
                var m = mines[i]; var dl = m.pos - L.transform.position; dl.y = 0f;
                if (dl.magnitude > 220f) { Destroy(m.vis.gameObject); mines.RemoveAt(i); continue; }
                Vehicle on = null;
                if (!m.friendly) foreach (var v in platoon) { if (v.dead) continue; var d = v.transform.position - m.pos; d.y = 0f; if (d.magnitude < 1.8f) { on = v; break; } }
                if (on == null) foreach (var e in foes) { if (e.dead || e.spec.isGun) continue; var d = e.transform.position - m.pos; d.y = 0f; if (d.magnitude < 1.8f) { on = e; break; } }
                if (on == null) continue;
                if (dozer && on.friendly) { Blow(m); mines.RemoveAt(i); hud.Popup(m.pos, "Ploughed", new Color(0.8f, 0.8f, 0.7f)); continue; }
                Blow(m); mines.RemoveAt(i); on.trackOut = m.friendly ? 12f : 10f; Damage(on, m.friendly ? 2f : 1f, m.pos); if (!on.friendly) { nightTracked++; hud.Popup(m.pos, on.spec.name + " on a mine", new Color(1f, 0.8f, 0.5f)); } else hud.Toast("Mine! Track off", 2f);
                if (phase != Phase.Play) return;
            }
        }
        void LayMine(Vector3 at, bool friendly = false)
        {
            if (mineMaterial == null) { mineMaterial = new Material(Resources.Load<Material>("BarrelLit")); mineMaterial.SetColor("_BaseColor", new Color(0.16f, 0.15f, 0.13f)); mineMaterial.SetFloat("_Smoothness", 0.35f); }
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(go.GetComponent<Collider>()); go.name = "Mine"; go.transform.position = new Vector3(at.x, 0.05f, at.z); go.transform.localScale = new Vector3(0.55f, 0.05f, 0.55f);
            go.GetComponent<Renderer>().sharedMaterial = mineMaterial; go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(cap.GetComponent<Collider>()); cap.transform.SetParent(go.transform, false); cap.transform.localPosition = new Vector3(0f, 1f, 0f); cap.transform.localScale = new Vector3(0.45f, 0.6f, 0.45f); cap.GetComponent<Renderer>().sharedMaterial = mineMaterial;
            mines.Add(new Mine { pos = go.transform.position, vis = go.transform, friendly = friendly });
        }
        void Blow(Mine m) { fx.Explosion(m.pos); Sfx.Explosion(m.pos); props.Crater(m.pos, 2.5f); props.Blast(m.pos, 3f, 1f); shake = Mathf.Max(shake, 0.35f); Destroy(m.vis.gameObject); InfantryKilled(infantry.Blast(m.pos, 3f), m.pos); }
        bool MineShot(Vector3 at)
        {
            for (int i = mines.Count - 1; i >= 0; i--) { if (mines[i].friendly) continue; var d = mines[i].pos - at; d.y = 0f; if (d.magnitude < 2.2f) { Blow(mines[i]); mines.RemoveAt(i); score += 40; hud.Popup(at, "Mine · +40", new Color(1f, 0.85f, 0.5f)); return true; } }
            return false;
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

        void Fire(Vehicle v, Vehicle target) => Fire(v);
        void Fire(Vehicle v)
        {
            v.reloadLeft = v.spec.reload * v.reloadMul * (v.friendly ? 1f : Mathf.Lerp(1.9f, 1.1f, t / 120f) * (firstNight ? 1.3f : 1f)); v.Recoil();
            var scatter = (v.friendly ? 1.5f * scatterMul : 4f * (smokeLeft > 0f ? 3f : 1f) * (lit ? 0.5f : 1f)) * Mathf.Deg2Rad * Random.Range(-1f, 1f);
            bool leaderShot = v == Leader, heShot = he || (leaderShot && loadHe);
            if (leaderShot) { if (loadHe) heRounds--; else apRounds--; if (loadHe && heRounds <= 0 && apRounds > 0) { loadHe = false; heAuto = false; } else if (!loadHe && apRounds <= 0 && heRounds > 0) { loadHe = true; heAuto = true; } hud.SetAmmo(apRounds, heRounds, loadHe); }
            if (v.friendly) shotsFired++;
            var dir = Quaternion.Euler(0f, scatter * Mathf.Rad2Deg, 0f) * v.GunDirection; var pos = v.MuzzlePosition;
            float speed = v.friendly ? ShellSpeed : EnemyShellSpeed;
            var vis = fx.Tracer(v.friendly ? new Color(1f, 1f, 0.9f, 1f) : new Color(1f, 0.75f, 0.55f, 1f), v.friendly ? new Color(1f, 0.85f, 0.45f, 0.6f) : new Color(1f, 0.45f, 0.25f, 0.6f));
            vis.position = pos; vis.rotation = Quaternion.LookRotation(cam.transform.forward, dir);
            shells.Add(new Shell { pos = pos, vel = dir * speed, friendly = v.friendly, he = v.friendly && heShot, dmg = v.spec.damage * v.damageMul * (v.friendly ? ammoMul : 1f) * (leaderShot && loadHe ? 0.6f : 1f), life = v.Range / speed + 0.25f, vis = vis });
            if (canister && leaderShot) { int swept = infantry.Blast(pos + v.GunDirection * 8f, 8f); if (swept > 0) { InfantryKilled(swept, pos + v.GunDirection * 8f); fx.Dust(new Vector3(pos.x, 0.3f, pos.z) + v.GunDirection * 8f); } }
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
                // a wreck in the way stops the shell; a target in a hedge bank is missed one time in three
                if (hit == null && !s.bounced) foreach (var w in wrecks) { if ((w.v.transform.position - s.pos).sqrMagnitude < w.v.spec.radius * w.v.spec.radius * 0.6f && s.pos.y < 2.2f) { fx.Spark(s.pos + Vector3.up * 0.5f, -s.vel.normalized); Sfx.Ricochet(s.pos); s.life = 0f; break; } }
                if (hit != null && !s.bounced && props.InCover(hit.transform.position) && Random.value < 0.33f) { fx.Hit(new Vector3(s.pos.x, 0.8f, s.pos.z), 0.5f); if (hit.friendly) hud.Popup(hit.transform.position, "Hedge", new Color(0.55f, 0.8f, 0.5f)); if (s.he) props.Blast(hit.transform.position, 2f, 1.5f); s.life = 0f; hit = null; }
                if (hit != null && hit.friendly && !s.bounced && StandCovered(hit.transform.position, s.pos) && Random.value < 0.5f) { fx.Hit(new Vector3(s.pos.x, 0.8f, s.pos.z), 0.6f); hud.Popup(hit.transform.position, "Sandbags", new Color(0.85f, 0.8f, 0.6f)); s.life = 0f; hit = null; }
                if (hit != null && sandbags && hit == Leader && !s.bounced && Vector3.Dot((s.pos - hit.transform.position).normalized, hit.Forward) > 0.35f && Random.value < 0.35f)
                {
                    // hung on the front plate: the shell skates off the sandbags and the track links
                    var away2 = s.pos - hit.transform.position; away2.y = 0f; away2.Normalize();
                    s.vel = Vector3.Reflect(s.vel, away2) * 0.4f + away2 * 8f + Vector3.up * 14f; s.life = 0.7f; s.bounced = true;
                    fx.Spark(new Vector3(s.pos.x, 1.6f, s.pos.z), away2); Sfx.Ricochet(s.pos); hud.Popup(hit.transform.position, "Sandbags", new Color(0.85f, 0.8f, 0.6f)); hit = null;
                }
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
                    float dmg = s.dmg;
                    if (s.friendly && aceGunner && hit != lastHitFoe) dmg *= 2f;   // the first shot at a fresh target
                    if (s.friendly) lastHitFoe = hit;
                    if (s.friendly && phosphorus && !hit.friendly) { burn[hit] = 6f; if (!burning.Contains(hit)) burning.Add(hit); }
                    Damage(hit, dmg, s.pos);
                    TrackHit(hit, s.pos);
                    if (s.he) { float r = heBurst; foreach (var v in hitList) if (v != hit && !v.dead && Dist(v, hit) < r) Damage(v, s.dmg * 0.5f, v.transform.position); if (s.friendly) InfantryKilled(infantry.Blast(s.pos, r), s.pos); Hole(hit, s.pos); props.Blast(s.pos, r * 0.5f, 0.8f); }
                    else fx.Hit(new Vector3(s.pos.x, 1.6f, s.pos.z), 1f);
                }
                if (s.bounced) s.vel += Vector3.down * (30f * dt);
                if (hit == null && !s.bounced && s.friendly && objective != null && objective.kind == "dump" && new Vector2(s.pos.x - objective.pos.x, s.pos.z - objective.pos.z).magnitude < 4.5f && s.pos.y < 4f) { DumpHit(s.pos); s.life = 0f; }
                if (hit == null && !s.bounced && s.friendly && s.life > 0f && MineShot(s.pos)) s.life = 0f;
                if (hit == null && !s.bounced && s.friendly) { int men = infantry.Blast(s.pos, 1.6f); if (men > 0) { InfantryKilled(men, s.pos); fx.Hit(s.pos, 0.7f); s.life = 0f; } }
                if (hit == null && !s.bounced && s.friendly && props.HitLamp(s.pos)) { fx.Explosion(s.pos); Sfx.Explosion(s.pos); score += 150; hud.Popup(s.pos, "Searchlight out · +150", new Color(1f, 0.9f, 0.6f)); nightLamps++; s.life = 0f; }
                if (hit == null && !s.bounced && props.Blocks(s.pos)) { fx.Hit(s.pos, 0.6f); Sfx.Hit(s.pos); props.Strike(s.pos, s.he ? 1f : 0.35f); s.life = 0f; }
                else if (hit == null && s.life <= 0f) { var g = new Vector3(s.pos.x, 0f, s.pos.z); fx.Dust(g); props.Crater(g, 2.2f); }   // spent: into the dirt
                if (hit != null || s.life <= 0f) { fx.Release(s.vis); shells.RemoveAt(i); }
            }
        }

        /// <summary>A shell low on a tank's side, one time in five, throws a track: ten seconds stuck (the turret still works).</summary>
        void TrackHit(Vehicle v, Vector3 at)
        {
            if (v.dead || v.spec.isGun || v.trackOut > 0f || Random.value > 0.2f) return;
            var d = at - v.transform.position; d.y = 0f; if (Mathf.Abs(Vector3.Dot(d.normalized, v.Forward)) > 0.7f) return;   // from the side only
            v.trackOut = v == Leader ? 10f * (1f - 0.15f * crewMech) : 10f; fx.Spark(new Vector3(at.x, 0.6f, at.z), d.normalized); Sfx.Ricochet(at); if (!v.friendly) nightTracked++;
            if (v.friendly) { hud.Toast((v == Leader ? "Track knocked off · " : v.spec.name + " tracked · ") + "10 s", 2.8f); if (v == Leader) Buzz(); }
            else hud.Popup(v.transform.position, "Tracked", new Color(1f, 0.8f, 0.4f));
        }

        void Damage(Vehicle v, float dmg, Vector3 at)
        {
            if (v.friendly && v == Leader && leaderShield > 0f) return;
            if (v == Leader && wetStowage && !savedTonight && v.hp - dmg <= 0f) { savedTonight = true; v.hp = 1f; leaderShield = 2.5f; hud.Flash(); shake = Mathf.Max(shake, 0.9f); hud.Toast("The wet racks held · the leader is alive on one", 3.2f); Sfx.Ricochet(v.transform.position); if (Random.value < 0.7f) Radio("hit"); return; }
            if (!v.friendly && bonus == "heavy" && v.spec.hp >= 6f) dmg *= 1.5f;
            v.Hit(dmg); Sfx.Hit(v.transform.position);
            if (v.friendly && v == Leader) { hud.Flash(); shake = Mathf.Max(shake, 0.8f); Buzz(); }
            if (v.hp > 0f) { if (v == Leader) { hud.Toast("Leader hit"); if (Random.value < 0.4f) Radio("hit"); if (hasSmoke && smokeCooldown <= 0f) PopSmoke(); } return; }
            tracks.Forget(v);
            v.Wreck(); fx.Explosion(v.transform.position); Sfx.Explosion(v.transform.position); props.Scorch(v.transform, 6f + v.spec.radius * 1.5f); props.Blast(v.transform.position, 5f, 1.2f); InfantryKilled(infantry.Blast(v.transform.position, 6f), v.transform.position);
            var fire = new GameObject("WreckFire").AddComponent<Light>(); fire.type = LightType.Point; fire.color = new Color(1f, 0.5f, 0.2f); fire.range = 16f; fire.intensity = 6f; fire.shadows = LightShadows.None;
            fire.transform.position = v.transform.position + Vector3.up * 2.5f;
            wrecks.Add(new Wreck { v = v, fire = fire });
            if (!v.friendly) SpawnSalvage(v);
            if (Random.value < CookOffChance(v)) CookOff(v);
            bool gotOut = v.friendly ? (v == Leader || Random.value < 0.5f) : !v.spec.isGun && !v.spec.transport && Random.value < 0.35f;
            if (gotOut)
            {
                var away = Leader != null && v != Leader ? v.transform.position - Leader.transform.position : -v.Forward; away.y = 0f;   // ours to the rear, theirs away from us
                infantry.BailOut(v.transform.position, v.friendly ? -v.Forward : away.normalized, v.friendly ? Depot.Nation : "de", v == Leader ? 3 : Random.Range(1, 4));
            }
            if (v.friendly && v != Leader && recovery && recoverySpec == null) { recoverySpec = v.spec; recoveryLeft = 20f; hud.Toast("Recovery crew is on it · " + v.spec.name + " back in twenty seconds", 2.8f); }
            if (v.friendly)
            {
                bool wasLeader = v == Leader; platoon.Remove(v);
                if (wasLeader || platoon.Count == 0) { End(false); return; }
                wingmenLost++; hud.Toast(gotOut ? "Wingman lost · the crew got out" : "Wingman lost · no one got out");
            }
            else
            {
                foes.Remove(v); kills++; if (stand) StandKill(v);
                if (v.spec == VehicleSpec.Tiger || v.spec == VehicleSpec.TigerAce || v.spec == VehicleSpec.KingTiger) nightTigers++; if (Random.value < 0.35f) Radio("kill"); if (v == focus) nightFocus++; if (v.spec.isGun) nightPaks++;
                int worth = v.spec == VehicleSpec.Tiger ? 5 : v.spec == VehicleSpec.TigerAce ? 12 : v.spec == VehicleSpec.KingTiger ? 14 : v.spec == VehicleSpec.Flak88 ? 4 : v.spec == VehicleSpec.Hetzer ? 3 : v.spec == VehicleSpec.Nebelwerfer ? 3 : v.spec == VehicleSpec.Kubelwagen ? 4 : v.spec == VehicleSpec.Flak38 ? 1 : v.spec == VehicleSpec.Panther ? 5 : v.spec == VehicleSpec.StuG ? 3 : 2; score += worth * 50; xp += worth;
                if (v.spec == VehicleSpec.Panther) nightTigers++;   // the big cats count together for the missions
                hud.Popup(v.transform.position, "+" + worth * 50, new Color(0.95f, 0.66f, 0.23f)); shake = Mathf.Max(shake, Dist(v, Leader) < 25f ? 0.5f : 0.2f);
                combo = Time.time - lastKill < 4f ? combo + 1 : 1; lastKill = Time.time;
                if (combo >= 2) { int bonus = combo * 25; score += bonus; hud.Popup(v.transform.position + Vector3.up * 3f, "×" + combo + " +" + bonus, new Color(1f, 0.92f, 0.6f)); if (combo == 3) hud.Toast("Triple kill", 1.8f); else if (combo == 5) hud.Toast("Rampage", 1.8f); }
                if (v == boss) { score += 1500; bossKilled = true; shake = 2f; SlowMo(0.8f); hud.HideBoss(); hud.Toast("Tiger Ace destroyed · +1500"); }
                if (v == ace) { score += 600; shake = Mathf.Max(shake, 1f); SlowMo(0.7f); Depot.Tally("acesNamed", 1); nightAces++; hud.Popup(v.transform.position, "+600", new Color(1f, 0.8f, 0.4f)); hud.Toast(aceName + " is finished · +600", 3f); Radio("kill"); }
                if (xp >= xpNeed) LevelUp(); else hud.SetLevel(level, (float)xp / xpNeed);
            }
        }

        void Reinforce()
        {
            if (rule == "alone") return;
            reinforcements++;
            var spec = Depot.Nation == "us" && (reinforcements % 3 == 0 || fireflyNext) ? VehicleSpec.Firefly : Wingman; fireflyNext = false;
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
            if (stand) { TickStand(dt); return; }
            spawnTimer -= dt;
            float interval = Mathf.Lerp(8f, 2.8f, t / 240f) * (firstNight ? 1.25f : 1f) * (opNight > 0 ? 1f - 0.06f * (opNight - 1) : 1f);
            if (debugInfantry && !debugSquadSent && t > 4f) { debugSquadSent = true; var L0 = Leader; infantry.Spawn(L0.transform.position + L0.Forward * 30f, -L0.Forward); hud.Toast("Infantry! Panzerfausts, 12 o'clock", 2.8f); }
            if (debugGuns && !debugSquadSent && t > 2f)
            {
                debugSquadSent = true; var L0 = Leader; var f0 = L0.Forward; var r0 = new Vector3(f0.z, 0f, -f0.x);
                var g1 = Foe(VehicleSpec.Pak40, L0.transform.position + f0 * 20f - r0 * 8f, L0.yaw + Mathf.PI); var g2 = Foe(VehicleSpec.Flak88, L0.transform.position + f0 * 26f + r0 * 8f, L0.yaw + Mathf.PI); g1.hp = g2.hp = 999f; hud.Toast("Guns", 2f);
            }
            if (debugSalvage && !debugSquadSent && t > 2f)
            {
                debugSquadSent = true; var L0 = Leader; var f0 = L0.Forward; var r0 = new Vector3(f0.z, 0f, -f0.x);
                apRounds = 4; heRounds = 0; loadHe = false; hud.SetAmmo(apRounds, heRounds, loadHe);
                DropCrate(props.PushOut(L0.transform.position + f0 * 9f, 1.4f), 5, 1); DropCrate(props.PushOut(L0.transform.position + f0 * 16f + r0 * 2.5f, 1.4f), 3, 0); DropCrate(props.PushOut(L0.transform.position + f0 * 23f - r0 * 2f, 1.4f), 2, 1);
            }
            if (debugRubble && t > 2f && !rubbleDone[0]) { rubbleDone[0] = true; dozer = true; var L0 = Leader; props.Blast(L0.transform.position + L0.Forward * 25f, 12f, 30f); fx.Explosion(L0.transform.position + L0.Forward * 25f); }
            if (debugRubble && t > 5f && !rubbleDone[1]) { rubbleDone[1] = true; var L0 = Leader; props.Blast(L0.transform.position + L0.Forward * 40f, 12f, 30f); fx.Explosion(L0.transform.position + L0.Forward * 40f); }
            if (debugCook && !debugSquadSent && t > 2f)   // two Panzer IVs on their last legs 20 m ahead, to watch them go up
            {
                debugSquadSent = true; var L0 = Leader; var f0 = L0.Forward; var r0 = new Vector3(f0.z, 0f, -f0.x);
                foreach (var k in new[] { -5f, 5f }) { var e = Foe(VehicleSpec.PanzerIV, L0.transform.position + f0 * 20f + r0 * k, L0.yaw); e.hp = 0.5f; }
            }
            if (debugZoo && !debugSquadSent && t > 4f)
            {
                debugSquadSent = true; var L0 = Leader; var f0 = L0.Forward; var r0 = new Vector3(f0.z, 0f, -f0.x);
                Foe(VehicleSpec.Panther, L0.transform.position + f0 * 34f - r0 * 12f, L0.yaw + Mathf.PI); Foe(VehicleSpec.StuG, L0.transform.position + f0 * 34f + r0 * 12f, L0.yaw + Mathf.PI);
                Foe(VehicleSpec.Halftrack, L0.transform.position + f0 * 60f, L0.yaw + Mathf.PI); Foe(VehicleSpec.Flak88, L0.transform.position + f0 * 40f, L0.yaw + Mathf.PI); Foe(VehicleSpec.Pak40, L0.transform.position + f0 * 26f + r0 * 8f, L0.yaw + Mathf.PI);
                foreach (var sp in new[] { VehicleSpec.Hetzer, VehicleSpec.KingTiger, VehicleSpec.Flak38, VehicleSpec.Nebelwerfer, VehicleSpec.Kubelwagen }) if (VehicleSpec.Available(sp)) { var z = Foe(sp, L0.transform.position + f0 * (44f + 8f * System.Array.IndexOf(new[] { VehicleSpec.Hetzer, VehicleSpec.KingTiger, VehicleSpec.Flak38, VehicleSpec.Nebelwerfer, VehicleSpec.Kubelwagen }, sp)) - r0 * 14f, L0.yaw + Mathf.PI); z.hp = 999f; }
                infantry.Spawn(L0.transform.position + f0 * 24f, -f0); hud.Toast("Zoo", 2f);
            }
            if (spawnTimer <= 0f && foes.Count < (theatre == "kursk" ? 15 : 12))
            {
                spawnTimer = interval;
                var L = Leader; float ahead = L.yaw + Random.Range(-1.7f, 1.7f);
                var dir = new Vector3(Mathf.Sin(ahead), 0f, Mathf.Cos(ahead));
                float roll = Random.value;
                float infantryShare = route == "village" ? 0.3f : route == "bocage" ? 0.34f : 0.14f, gunShare = route == "village" ? 0.62f : route == "bocage" ? 0.5f : 0.3f;
                if (theatre == "kursk") gunShare = infantryShare + (gunShare - infantryShare) * 0.5f;   // the attacker does not dig in
                if (rule == "hunters") { float gunPart = gunShare - infantryShare; infantryShare = Mathf.Min(0.6f, infantryShare * 2f); gunShare = infantryShare + gunPart * 0.7f; }
                if (t > (rule == "hunters" ? 20f : route == "open" ? 45f : 30f) && roll < infantryShare && infantry.squads.Count < (route == "open" ? 3 : 4) + (rule == "hunters" ? 2 : 0))
                {
                    if (Random.value < 0.5f)
                    {
                        // a half-track brings them: kill it on the way and the squad never dismounts
                        var pos = props.PushOut(L.transform.position + dir * Random.Range(60f, 72f), 3f);
                        Foe(VehicleSpec.Halftrack, pos, Mathf.Atan2(-dir.x, -dir.z)); hud.Toast("Half-track with infantry, " + Clock(pos), 2.8f);
                    }
                    else
                    {
                        // tank hunters on foot, out of the dark ahead
                        var pos = props.PushOut(L.transform.position + dir * (route == "bocage" ? Random.Range(18f, 26f) : Random.Range(30f, 40f)), 2f);
                        infantry.Spawn(pos, -dir); hud.Toast("Infantry! Panzerfausts, " + Clock(pos), 2.8f);
                    }
                }
                else if (roll < gunShare && t > 20f)
                {
                    // an anti-tank gun dug in on the platoon's way, waiting
                    var pos = props.PushOut(L.transform.position + L.Forward * 38f + new Vector3(Random.Range(-14f, 14f), 0f, 0f), 3f); float gyaw = L.yaw + Mathf.PI;
                    var nests = props.Nests(L.transform.position, L.Forward, 26f, 60f);
                    if (nests.Count > 0) { var nest = nests[Random.Range(0, nests.Count)]; var toL = L.transform.position - nest; toL.y = 0f; toL.Normalize(); pos = nest - toL * 3.2f; gyaw = Mathf.Atan2(toL.x, toL.z); }   // behind the sandbags, facing us
                    var gspec = VehicleSpec.Pak40;
                    if (t > 40f && t <= 100f && Random.value < 0.3f && VehicleSpec.Available(VehicleSpec.Flak38))
                    {
                        // the light flak by a searchlight: it shoots at the sky, not at us, but it is worth the shell
                        var posts = props.Posts(L.transform.position, L.Forward, 30f, 66f);
                        if (posts.Count > 0) { var post = posts[Random.Range(0, posts.Count)]; var toL = L.transform.position - post; toL.y = 0f; toL.Normalize(); pos = props.PushOut(post - toL * 7f, 3f); gyaw = Random.value * 6.28f; gspec = VehicleSpec.Flak38; }
                    }
                    if (t > 100f && Random.value < 0.35f)
                    {
                        // the searchlight posts have an 88 with them: long reach, hard hit, slow to turn
                        var posts = props.Posts(L.transform.position, L.Forward, 34f, 66f);
                        if (posts.Count > 0) { var post = posts[Random.Range(0, posts.Count)]; var toL = L.transform.position - post; toL.y = 0f; toL.Normalize(); pos = props.PushOut(post + toL * 8f, 3f); gyaw = Mathf.Atan2(toL.x, toL.z); gspec = VehicleSpec.Flak88; }
                    }
                    var gun = Foe(gspec, pos, gyaw);
                    hud.Toast((gspec == VehicleSpec.Flak88 ? "88! Flak gun, " : gspec == VehicleSpec.Flak38 ? "Flak battery, " : "Anti-tank gun dug in, ") + Clock(pos), 2.8f);
                }
                else
                {
                    bool ks = theatre == "kursk"; float tigerFrom = ks ? 40f : 90f;   // Kursk: the Tigers lead from the start
                    var spec = (t > tigerFrom && Random.value < Mathf.Lerp(ks ? 0.18f : 0.1f, ks ? 0.4f : 0.35f, (t - tigerFrom) / 180f)) ? VehicleSpec.Tiger : VehicleSpec.PanzerIV;
                    if (spec == VehicleSpec.Tiger && t > (ks ? 90f : 150f) && Random.value < 0.5f) spec = VehicleSpec.Panther;
                    else if (spec == VehicleSpec.PanzerIV && t > 60f && Random.value < (ks ? 0.35f : 0.25f)) spec = VehicleSpec.StuG;
                    if (spec == VehicleSpec.StuG && !ks && Random.value < 0.5f && VehicleSpec.Available(VehicleSpec.Hetzer)) spec = VehicleSpec.Hetzer;   // the Hetzer is 1944
                    if (rule == "tigers" && t > 15f && spec != VehicleSpec.Tiger && spec != VehicleSpec.Panther && Random.value < 0.5f) spec = t > 60f && Random.value < 0.4f && VehicleSpec.Available(VehicleSpec.Panther) ? VehicleSpec.Panther : VehicleSpec.Tiger;
                    var pos = L.transform.position + dir * Random.Range(44f, 52f);
                    Foe(spec, pos, Mathf.Atan2(-dir.x, -dir.z));
                    if (spec == VehicleSpec.Tiger) { hud.Toast("Tiger! " + Clock(pos), 2.8f); Radio("tiger"); } else if (spec == VehicleSpec.Panther) hud.Toast("Panther! " + Clock(pos), 2.8f);
                }
            }
            if (theatre == "kursk" && ((t >= 75f && !wave1) || (debugKeil && t >= 4f && !wave1))) { wave1 = true; Panzerkeil(3); }
            if (t >= 120f && !wave2) { wave2 = true; if (theatre == "kursk") Panzerkeil(5); else Column(); }
            if (t >= 200f && !wave3) { wave3 = true; Counterattack(); }
            if (t >= 240f && !wave4) { wave4 = true; if (theatre == "kursk") Panzerkeil(5); else Column(); }
            if ((t >= (opNight == 5 ? 210f : 270f) || (debugBoss && t >= 6f)) && !bossSpawned) { bossSpawned = true; Boss(); }
            if (boss != null && !boss.dead) hud.SetBoss(boss.hp / boss.spec.hp);
        }

        /// <summary>The 4:30 boss: a Tiger ace with two Panzer IV escorts, straight at the platoon from the dark ahead.</summary>
        void Boss()
        {
            var L = Leader; var f = L.Forward; var r = new Vector3(f.z, 0f, -f.x);
            var bossSpec = winter && VehicleSpec.Available(VehicleSpec.KingTiger) ? VehicleSpec.KingTiger : VehicleSpec.TigerAce;
            boss = Foe(bossSpec, L.transform.position + f * 52f, L.yaw + Mathf.PI);
            foreach (var side in new[] { -1f, 1f }) Foe(VehicleSpec.PanzerIV, L.transform.position + f * 58f + r * side * 9f, L.yaw + Mathf.PI);
            hud.ShowBoss(bossSpec.name); hud.Toast(bossSpec == VehicleSpec.KingTiger ? "King Tiger · kill it before dawn" : "Tiger Ace · kill it before dawn"); Debug.Log("Iron Night: boss spawned at " + t.ToString("0.0"));
        }

        /// <summary>Kursk's wave: a Panzerkeil driving straight at the platoon from 62 m ahead, a Tiger at the tip and the
        /// escorts fanned out behind it: Panzer IVs, a StuG or two on the wings, a Panther or a second Tiger at the back.</summary>
        void Panzerkeil(int escorts)
        {
            var L = Leader; var fw = Quaternion.Euler(0f, Random.Range(-28f, 28f), 0f) * L.Forward; var rt = new Vector3(fw.z, 0f, -fw.x);
            var tip = L.transform.position + fw * 62f; float yaw = Mathf.Atan2(-fw.x, -fw.z);
            Foe(VehicleSpec.Tiger, props.PushOut(tip, 3f), yaw);
            float[] side = { 1f, -1f, 1f, -1f, 0f }, back = { 8f, 8f, 16f, 16f, 20f };
            for (int i = 0; i < escorts && i < 5; i++)
            {
                var spec = i == 4 ? (VehicleSpec.Available(VehicleSpec.Panther) && t > 150f ? VehicleSpec.Panther : VehicleSpec.Tiger) : i >= 2 && Random.value < 0.4f ? VehicleSpec.StuG : VehicleSpec.PanzerIV;
                Foe(spec, props.PushOut(tip + fw * back[i] + rt * side[i] * back[i] * 0.8f, 3f), yaw);
            }
            hud.Toast("Panzerkeil! " + Clock(tip), 3f); Radio("tiger"); Sfx.Rumble(); shake = Mathf.Max(shake, 0.4f);
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
                new Hud.Card { id = "he", title = "High explosive", desc = "Eight more HE rounds in the racks and a wider burst: 7 m instead of 5." },
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
                new Hud.Card { id = "wet", rare = true, title = "Wet stowage", desc = "The racks are jacketed in water: once tonight a killing hit leaves the leader alive on one." },
                new Hud.Card { id = "sandbags", title = "Sandbags and track links", desc = "Hung on the front plate: shells from ahead glance off twice as often." },
                new Hud.Card { id = "dozer", rare = true, title = "Dozer blade", desc = "Push through hedges and walls, and mines do nothing to the platoon." },
                new Hud.Card { id = "canister", title = "Canister shot", desc = "A shotgun round for the gun: infantry within 14 m are swept away with every shot." },
                new Hud.Card { id = "phos", title = "Phosphorus rounds", desc = "What you hit burns: another hit's worth of damage over six seconds." },
                new Hud.Card { id = "radionet", title = "Radio net", desc = "The wingmen hear everything: they load 25% faster and hold their slots tighter." },
                new Hud.Card { id = "ace", title = "Ace gunner", desc = "The first shot at a fresh target finds the weak spot: double damage." },
                new Hud.Card { id = "recovery", rare = true, title = "Recovery crew", desc = "A knocked-out wingman is dragged back and rejoins after twenty seconds." },
                new Hud.Card { id = "widetracks", title = "Wide tracks", desc = "Duckbills on the tracks: no more bogging down - full speed in mud and snow, sharper turns." },
                new Hud.Card { id = "plane", rare = true, title = "Spotter plane", desc = "An L-4 circles overhead: every 30 seconds it marks the strongest enemy group." },
            };
            if (he) all.RemoveAll(c => c.id == "he"); if (gunners) all.RemoveAll(c => c.id == "gunners"); if (hasSmoke) all.RemoveAll(c => c.id == "smoke");
            foreach (var taken in new[] { wetStowage ? "wet" : null, sandbags ? "sandbags" : null, dozer ? "dozer" : null, canister ? "canister" : null, phosphorus ? "phos" : null, radioNet ? "radionet" : null, aceGunner ? "ace" : null, recovery ? "recovery" : null, wideTracks ? "widetracks" : null, spotterPlane ? "plane" : null }) if (taken != null) all.RemoveAll(c => c.id == taken);
            if (artyInterval > 0f && artyInterval <= 12f) all.RemoveAll(c => c.id == "arty");
            if (rule == "alone") all.RemoveAll(c => c.id == "reinf" || c.id == "firefly" || c.id == "recovery" || c.id == "radionet" || c.id == "plates");   // nothing for wingmen that never come
            if (Depot.Nation != "us" || (!platoon.Exists(p => p != Leader && p.spec == VehicleSpec.Sherman) && platoon.Count >= maxPlatoon)) all.RemoveAll(c => c.id == "firefly");
            var pick = new List<Hud.Card>(); while (pick.Count < 3 && all.Count > 0) { int i = Random.Range(0, all.Count); pick.Add(all[i]); all.RemoveAt(i); }
            phase = Phase.LevelUp; stick.Blocked = true; Sfx.LevelUp();
            hud.ShowCards(pick, id =>
            {
                switch (id)
                {
                    case "he": { int add = Mathf.Min(heMax + 8 - heRounds, 8 + heMax - heRounds); heMax += 8; heRounds = Mathf.Min(heMax, heRounds + 8); heBurst = 7f + 0.4f * crewHe; hud.SetAmmo(apRounds, heRounds, loadHe); break; }
                    case "apcr": damageMul *= 1.5f; break;
                    case "rapid": reloadMul *= 0.8f; break;
                    case "radar": cardRange *= 1.15f; rangeMul *= 1.15f; break;
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
                    case "wet": wetStowage = true; break;
                    case "sandbags": sandbags = true; break;
                    case "dozer": dozer = true; hud.Toast("Dozer blade fitted · drive straight through the hedges", 3f); break;
                    case "canister": canister = true; break;
                    case "phos": phosphorus = true; break;
                    case "radionet": radioNet = true; reloadMul *= 1f; break;
                    case "ace": aceGunner = true; break;
                    case "recovery": recovery = true; break;
                    case "widetracks": wideTracks = true; speedMul *= 1.08f; turretMul *= 1.05f; break;
                    case "plane": spotterPlane = true; planeTimer = 6f; break;
                }
                phase = Phase.Play; stick.Blocked = false;
            });
        }

        /// <summary>The weather turns once, somewhere in the middle of the night: rain comes on, fog lifts off the
        /// fields, or the clouds break and the moon comes out. Whatever it does, everyone has to fight in it.</summary>
        void TickWeatherTurn(float dt)
        {
            if (weatherTurn <= 0f || t < weatherTurn) return; weatherTurn = 0f;
            var was = weather;
            weather = was == Weather.Rain ? Weather.Overcast : was == Weather.Fog ? Weather.Clear : was == Weather.Clear ? (Random.value < 0.5f ? Weather.Fog : Weather.Rain) : Weather.Rain;
            ApplyWeather();
            string line = weather == Weather.Rain ? "Rain coming down · the enemy shoots short" : weather == Weather.Fog ? "Fog rolling off the fields · nobody sees far" : weather == Weather.Clear ? "The clouds are breaking · the moon is out" : "The sky is closing in";
            hud.Toast(line, 3.4f); Sfx.Rumble();
        }

        /// <summary>Tonight's weather: most nights are clear; overcast, fog and rain each change the light and the fight.</summary>
        void PickWeather()
        {
            float r = Random.value; weather = r < 0.45f ? Weather.Clear : r < 0.7f ? Weather.Overcast : r < 0.85f ? Weather.Fog : Weather.Rain;
            winter = theatre == "ardennes";
            if (daily) { var w = Daily.Weather(dailyDay); weather = w == "fog" ? Weather.Fog : w == "rain" ? Weather.Rain : w == "overcast" ? Weather.Overcast : Weather.Clear; if (rule == "stukas") weather = Weather.Clear; }
            if (opNight > 0) weather = opN.weather == "fog" ? Weather.Fog : opN.weather == "rain" ? Weather.Rain : opN.weather == "overcast" ? Weather.Overcast : Weather.Clear;   // the night as it was briefed
            var args = System.Environment.GetCommandLineArgs();   // test switches: --fog, --rain, --overcast, --clear, --winter, --summer
            foreach (Weather w in System.Enum.GetValues(typeof(Weather))) if (System.Array.IndexOf(args, "--" + w.ToString().ToLowerInvariant()) >= 0) weather = w;
            if (System.Array.IndexOf(args, "--winter") >= 0) { winter = true; theatre = "ardennes"; } if (System.Array.IndexOf(args, "--summer") >= 0) { winter = false; if (theatre == "ardennes") theatre = "normandy"; }
            SkyFor();
        }

        /// <summary>The sky the current weather calls for; the winter nights are paler and the beams stand out in fog.</summary>
        void SkyFor()
        {
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

        /// <summary>Puts the sky into the scene: ambient, fog, moon, the rain or the snow, the wet ground, the ranges
        /// everyone fights at and the line under the clock.</summary>
        void ApplyWeather()
        {
            SkyFor();
            RenderSettings.ambientLight = sky.ambient; RenderSettings.fogDensity = sky.fogDensity; RenderSettings.fogColor = sky.fog;
            if (moon != null) { moon.color = sky.moonColor; moon.intensity = sky.moonIntensity; moon.shadowStrength = sky.shadow; }
            rangeMul = Depot.RangeMul * (Depot.CrewLevel >= 3 ? 1.05f : 1f) * cardRange * (weather == Weather.Fog ? 0.8f : 1f);
            enemyRangeMul = weather == Weather.Fog ? 0.8f : weather == Weather.Overcast ? 0.9f : weather == Weather.Rain ? 0.85f : 1f;
            bool wet = weather == Weather.Rain && !winter;
            if (props != null) { props.wet = wet; Vehicle.Wet = wet; if (wet) props.SetWet(0.5f); }
            if (weather == Weather.Rain) { if (rain == null) BuildRain(); else { rain.Play(); } } else if (rain != null) rain.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Sfx.Ambient(wet); LightShaft.boost = weather == Weather.Fog ? 1.8f : weather == Weather.Rain ? 1.3f : 1f;
            hud.SetConditions((stand ? "Last stand · " : daily ? "Daily · " : "") + TheatreName + (route == "village" ? " · village" : route == "bocage" ? (theatre == "kursk" ? " · tree belts" : " · bocage") : (theatre == "kursk" ? " · steppe" : " · open fields")), winter && weather == Weather.Rain ? "Snow" : weather.ToString(), weather == Weather.Fog ? "everyone sees 20% less" : weather == Weather.Rain ? "the enemy shoots short" : weather == Weather.Overcast ? "a dark night, the enemy sees less" : "the moon is up: they see you");
        }

        /// <summary>Low quality for weak phones: no moon shadows, no post-processing, no rain or snow.</summary>
        void ApplyQuality()
        {
            bool low = LowQuality;
            moon.shadows = low ? LightShadows.None : LightShadows.Soft;
            var volume = FindAnyObjectByType<UnityEngine.Rendering.Volume>(); if (volume != null) volume.weight = low ? 0f : 1f;
            if (rain != null) { if (low) rain.Stop(); else if (!rain.isPlaying) rain.Play(); } if (splashes != null) { if (low) splashes.Stop(); else if (!splashes.isPlaying) splashes.Play(); }
        }

        /// <summary>The night's light every frame: the sky of the weather, then the last 45 seconds turning to dawn
        /// (k runs 0..1), then the flicker of a barrage somewhere over the horizon.</summary>
        void Lighting(float k, float dt)
        {
            rumbleTimer -= dt;
            if (rumbleTimer <= 0f) { rumbleTimer = 9f + Random.value * 18f; flicker = 0.35f; Sfx.Rumble(); }
            if (weather == Weather.Rain && !winter) { lightningTimer -= dt; if (lightningTimer <= 0f) { lightningTimer = 14f + Random.value * 30f; lightning = 1f; thunderIn = 0.8f + Random.value * 1.6f; } }
            if (lightning > 0f) { lightning = Mathf.Max(0f, lightning - dt * 6f); }
            if (thunderIn > 0f) { thunderIn -= dt; if (thunderIn <= 0f) { Sfx.Rumble(); shake = Mathf.Max(shake, 0.15f); } }
            if (flicker > 0f) flicker = Mathf.Max(0f, flicker - dt * 1.4f);
            if (k <= 0f && flicker <= 0f && lightning <= 0f) return;
            float e = k * k; var fl = new Color(0.2f, 0.17f, 0.14f) * (flicker * flicker * 4f) + new Color(0.7f, 0.75f, 0.9f) * (lightning * lightning * lightning);
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
            var em = rain.emission; em.rateOverTime = winter ? 400f : 3600f;
            { var vel = rain.velocityOverLifetime; vel.enabled = true; vel.x = winter ? new ParticleSystem.MinMaxCurve(0.4f, 2.2f) : new ParticleSystem.MinMaxCurve(5f, 7f); vel.z = new ParticleSystem.MinMaxCurve(-1.2f, 1.2f); }   // a wind from the west
            var sh = rain.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(130f, 1f, 170f); sh.rotation = new Vector3(90f, 0f, 0f);
            var r = go.GetComponent<ParticleSystemRenderer>(); if (winter) r.renderMode = ParticleSystemRenderMode.Billboard; else { r.renderMode = ParticleSystemRenderMode.Stretch; r.lengthScale = 22f; r.velocityScale = 0f; }
            var m = new Material(Resources.Load<Material>("Additive")); m.SetTexture("_BaseMap", glowTex); m.SetColor("_BaseColor", new Color(0.5f, 0.55f, 0.65f, 0.35f)); r.sharedMaterial = m;
            if (winter)
            {
                // real flakes: the sheet of twelve clusters, one picked per flake, tumbling slowly
                var flakes = Resources.Load<Texture2D>("Fx/fx_snowflakes"); if (flakes != null) { m.SetTexture("_BaseMap", flakes); m.SetColor("_BaseColor", new Color(0.9f, 0.92f, 1f, 0.8f)); }
                var ts = rain.textureSheetAnimation; ts.enabled = true; ts.numTilesX = 4; ts.numTilesY = 3; ts.startFrame = new ParticleSystem.MinMaxCurve(0f, 11.99f); ts.frameOverTime = new ParticleSystem.MinMaxCurve(0f); ts.cycleCount = 1;
                var rot = rain.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f); main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
            }
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            go.transform.position = Vector3.up * 30f; rain.Play();
            if (!winter) BuildSplashes();
        }

        ParticleSystem splashes;
        /// <summary>Rain hitting the ground round the platoon: little crowns from the photographed sheet, flat on the ground.</summary>
        void BuildSplashes()
        {
            var tex = Resources.Load<Texture2D>("Fx/fx_splash"); if (tex == null) return;
            var go = new GameObject("Splashes"); splashes = go.AddComponent<ParticleSystem>(); splashes.Stop();
            var main = splashes.main; main.startSpeed = 0f; main.startLifetime = 0.35f; main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 0.9f); main.maxParticles = 600; main.simulationSpace = ParticleSystemSimulationSpace.World; main.startColor = new Color(0.8f, 0.85f, 0.95f, 0.5f); main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            var em = splashes.emission; em.rateOverTime = 900f;
            var sh = splashes.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(60f, 0.1f, 80f);
            var ts = splashes.textureSheetAnimation; ts.enabled = true; ts.numTilesX = 8; ts.numTilesY = 1; ts.frameOverTime = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0f, 1f, 1f)); ts.cycleCount = 1;
            var r = go.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.HorizontalBillboard; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            var m = new Material(Resources.Load<Material>("Additive")); m.SetTexture("_BaseMap", tex); m.SetColor("_BaseColor", new Color(0.6f, 0.65f, 0.75f, 0.6f)); r.sharedMaterial = m;
            go.transform.position = Vector3.up * 0.06f; splashes.Play();
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
            if (PlayerPrefs.GetInt("vibe", 1) == 1) Handheld.Vibrate();
#endif
        }

        /// <summary>The next objective: a point 110 to 170 m ahead, within forty degrees of north, marked with green
        /// smoke and a light. Reaching it is worth 300 and the next one is set.</summary>
        void NextObjective()
        {
            if (stand) return;   // the crossroads is the objective
            var L = Leader; float ang = (Random.value - 0.5f) * 80f * Mathf.Deg2Rad, dist = debugDump ? 19f : debugCrew ? 16f : 110f + Random.value * 60f;
            var pos = L.transform.position + new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang)) * dist;
            bool hold = objectivesReached > 0 && Random.value < 0.5f;
            // from the second objective on, one in three is a battery to destroy or a staff car to stop
            string kind = "reach"; float roll = Random.value;
            if (objectivesReached > 0 && roll < 0.2f && VehicleSpec.Available(VehicleSpec.Nebelwerfer)) kind = "battery";
            else if (objectivesReached > 0 && roll < 0.35f && VehicleSpec.Available(VehicleSpec.Kubelwagen)) kind = "car";
            else if (objectivesReached > 0 && roll < 0.5f) kind = "crew";
            else if (objectivesReached > 0 && roll < 0.65f) kind = "dump";
            if (debugDump) kind = "dump";   // test switch: every objective a fuel dump
            if (debugCrew) kind = "crew";   // test switch: every objective a stranded crew
            if (kind != "reach") hold = false;
            objective = new Objective { pos = pos, n = objectivesReached + 1, hold = hold, kind = kind, marker = fx.Marker(pos, kind == "reach" ? new Color(0.35f, 0.95f, 0.45f) : new Color(1f, 0.45f, 0.3f), hold ? 15f : 9f, true) };
            if (kind == "reach") objective.grenade = props.Spawn("marker_smoke", pos, Random.value * 360f);   // the coloured smoke grenade marking the spot
            if (kind == "battery")
            {
                // two launchers dug in at the spot, facing us; the objective is done when both are wrecks
                var toL = L.transform.position - pos; toL.y = 0f; float gy = Mathf.Atan2(toL.x, toL.z); var side = new Vector3(toL.z, 0f, -toL.x).normalized;
                foreach (float s in new[] { -1f, 1f }) objective.targets.Add(Foe(VehicleSpec.Nebelwerfer, props.PushOut(pos + side * s * 5f, 2f), gy));
            }
            if (kind == "crew")
            {
                // one of ours knocked out, two of the crew waiting by the wreck for a lift
                objective.pos = props.PushOut(pos, 5f); pos = objective.pos; objective.marker.position = pos;
                var w = props.Spawn("wreck", pos, Random.value * 360f); if (w != null) objective.props.Add(w);
                var toL = L.transform.position - pos; toL.y = 0f; toL.Normalize(); var side = new Vector3(toL.z, 0f, -toL.x);
                objective.figures.Add(infantry.Figure(pos + toL * 4f + side * 1.2f, toL, Depot.Nation)); objective.figures.Add(infantry.Figure(pos + toL * 4.5f - side * 1.4f, toL, Depot.Nation));
            }
            if (kind == "dump")
            {
                // a fuel dump: stacks of barrels under a guard of four; three shells into it and it goes up
                objective.pos = props.PushOut(pos, 6f); pos = objective.pos; objective.marker.position = pos;
                for (int i = 0; i < 4; i++) { float a = i * 1.6f + Random.value; var p = props.Spawn("barrels", pos + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * 2.6f, Random.value * 360f); if (p != null) objective.props.Add(p); }
                var toL = L.transform.position - pos; toL.y = 0f; infantry.Spawn(pos + toL.normalized * 7f, toL.normalized);
            }
            if (kind == "car")
            {
                // the staff car starts 45 m ahead and drives off away from us for forty seconds
                var car = Foe(VehicleSpec.Kubelwagen, props.PushOut(L.transform.position + L.Forward * 45f, 2f), L.yaw); car.unloaded = true; car.leaving = true; objective.targets.Add(car); objective.clock = 40f;
            }
            if (phase == Phase.Play) Brief(kind);
            if (phase == Phase.Play) hud.Toast("Objective " + objective.n + " · " + (kind == "battery" ? "destroy the rocket battery, " : kind == "dump" ? "fuel dump · tap it to shell it, " : kind == "crew" ? "crew of a knocked-out tank · pick them up, " : kind == "car" ? "staff car making a run for it · stop it" : (hold ? "hold the crossing, " : "")) + (kind == "car" ? "" : Mathf.RoundToInt(dist) + " m ahead"), 3f);
        }

        /// <summary>The gun is empty: the crew says so, once every few seconds, and the drops become the thing to drive for.</summary>
        void Dry()
        {
            if (dryToast > 0f) return; dryToast = 6f; Sfx.Click();
            Salvage near = null; float nd = float.MaxValue;
            foreach (var c in salvage) { var d = c.pos - Leader.transform.position; d.y = 0f; if (d.sqrMagnitude < nd) { nd = d.sqrMagnitude; near = c; } }
            hud.Toast(near != null ? "Racks empty · ammo crate, " + Clock(near.pos) : "Racks empty · salvage the next wreck", 2.6f); if (Random.value < 0.6f) Radio("hit");
        }

        /// <summary>The commander's one order of the night, on a long cooldown: what it does depends on the man riding with you.</summary>
        void UseAbility()
        {
            if (phase != Phase.Play || abilityCool > 0f || commander == null) return;
            var L = Leader; abilityCool = 52f; Sfx.Pickup(); hud.Flash();
            switch (bonus)
            {
                case "reload": abilityLeft = 9f; hud.Toast(commander.name + ": \"Load! Load! Load!\" · the gun works twice as fast", 3f); break;
                case "radar": spotLeft = 11f; abilityLeft = 11f; hud.Toast(commander.name + ": \"Every one of them, marked.\"", 3f); foreach (var e in foes) if (!e.dead && (e.transform.position - L.transform.position).magnitude < 130f) fx.Marker(e.transform.position, new Color(1f, 0.4f, 0.35f), 6f, true); break;
                case "heavy": abilityLeft = 9f; hud.Toast(commander.name + ": \"Aim for the mantlet. Now.\" · the shells bite deeper", 3f); break;
                case "armour": abilityLeft = 8f; leaderShield = 8f; hud.Toast(commander.name + ": \"Button up!\" · nothing gets through for eight seconds", 3f); break;
                case "repair": abilityLeft = 0f; L.hp = Mathf.Min(Depot.LeaderHp + 2f, L.hp + 3f); hud.SetLeader(Mathf.CeilToInt(L.hp), Mathf.CeilToInt(Depot.LeaderHp)); hud.Toast(commander.name + ": \"Patch her up.\" · leader +3", 3f); break;
                default: abilityLeft = 9f; hud.Toast(commander.name + ": \"Full throttle!\" · the platoon runs", 3f); break;
            }
            Depot.Tally("abilities", 1);
        }

        /// <summary>An ace of the night: a heavy with a name, thicker armour and a steadier gun. One at a time; killing
        /// him pays 600 and goes into the tally. His name rides over his turret while he lives.</summary>
        void TickAce(float dt)
        {
            if (ace != null && !ace.dead) { hud.NamePlate(aceName, ace.transform.position + Vector3.up * 3.4f, cam, ace.hp / (ace.spec.hp * 1.7f)); return; }
            if (ace != null && ace.dead) { hud.NamePlate(null, Vector3.zero, cam, 0f); ace = null; aceTimer = rule == "aces" ? 55f : 95f + Random.value * 40f; }
            if (t < (rule == "aces" ? 60f : 100f) || boss != null) return;
            aceTimer -= dt; if (aceTimer > 0f) return;
            var L = Leader; var spec = Random.value < 0.5f ? VehicleSpec.Tiger : Random.value < 0.6f ? VehicleSpec.Panther : VehicleSpec.PanzerIV;
            if (!VehicleSpec.Available(spec)) spec = VehicleSpec.PanzerIV;
            float a = (Random.value - 0.5f) * 1.2f; var at = L.transform.position + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * 62f;
            ace = Foe(spec, props.PushOut(at, 3f), Mathf.Atan2(L.transform.position.x - at.x, L.transform.position.z - at.z));
            ace.hp *= 1.7f; aceName = AceNames[Random.Range(0, AceNames.Length)] + " · " + spec.name;
            hud.Toast(aceName + " · an ace is on the field, " + Clock(at), 3.4f); Sfx.Whistle(at); if (Random.value < 0.7f) Radio("hit");
            fx.Marker(at, new Color(1f, 0.3f, 0.25f), 7f, true);
        }

        /// <summary>Phosphorus: what was hit goes on burning. The spotter plane marks the thickest group. The recovery
        /// crew drags a wingman back into the fight.</summary>
        void TickCards(float dt)
        {
            if (phosphorus && burning.Count > 0)
                for (int i = burning.Count - 1; i >= 0; i--)
                {
                    var v = burning[i]; if (v == null || v.dead) { burn.Remove(v); burning.RemoveAt(i); continue; }
                    float left = burn[v] - dt; burn[v] = left;
                    if (Random.value < dt * 6f) fx.Trail(v.transform.position + Vector3.up * (1.4f + Random.value));
                    if (left <= 0f) { burn.Remove(v); burning.RemoveAt(i); continue; }
                    if (Mathf.FloorToInt(left + dt) != Mathf.FloorToInt(left)) { Damage(v, 0.17f, v.transform.position); if (phase != Phase.Play) return; }
                }
            if (spotterPlane)
            {
                planeTimer -= dt;
                if (planeTimer <= 0f)
                {
                    planeTimer = 30f; var L = Leader; Vehicle best = null; int bestN = 0;
                    foreach (var e in foes)
                    {
                        if (e.dead) continue; int n = 0; foreach (var o in foes) if (!o.dead && (o.transform.position - e.transform.position).sqrMagnitude < 900f) n++;
                        if (n > bestN) { bestN = n; best = e; }
                    }
                    if (best != null) { var at = best.transform.position; foreach (var e in foes) if (!e.dead && (e.transform.position - at).sqrMagnitude < 900f) fx.Marker(e.transform.position, new Color(1f, 0.45f, 0.35f), 5f, true); hud.Toast("Spotter plane: " + bestN + " of them, " + Clock(at), 2.6f); Sfx.Click(); }
                }
            }
            if (recovery && recoveryLeft > 0f)
            {
                recoveryLeft -= dt;
                if (recoveryLeft <= 0f && recoverySpec != null && platoon.Count < maxPlatoon)
                {
                    var L = Leader; var v = Vehicle.Create(recoverySpec, true, L.transform.position - L.Forward * 9f, L.yaw); v.hp = Mathf.Max(2f, recoverySpec.hp * 0.6f) + wingmanBonus; platoon.Add(v);
                    hud.Toast("Recovered · " + recoverySpec.name + " is back in the platoon", 2.8f); Sfx.Pickup(); recoverySpec = null;
                }
            }
        }
        Vehicle lastHitFoe; VehicleSpec recoverySpec; float recoveryLeft; float cardRange = 1f;   // what the cards have added to the sight, kept apart from the weather

        void TickAbility(float dt)
        {
            if (abilityCool > 0f) abilityCool -= dt;
            if (abilityLeft > 0f) abilityLeft -= dt;
            if (spotLeft > 0f) spotLeft -= dt;
            hud.SetAbility(commander != null, abilityCool <= 0f ? 1f : 1f - abilityCool / 52f, abilityLeft > 0f);
            if (dryToast > 0f) dryToast -= dt;
        }

        /// <summary>The briefing card for an objective that is more than a point to reach.</summary>
        void Brief(string kind)
        {
            if (kind == "reach") return;
            hud.Briefing("obj_" + kind, kind == "battery" ? "Rocket battery" : kind == "car" ? "Staff car" : kind == "dump" ? "Fuel dump" : "Stranded crew", kind == "battery" ? "Two Nebelwerfers dug in ahead. Get close and put them out." : kind == "car" ? "An officer with maps is making a run for it. Stop the car." : kind == "dump" ? "Drums under a net, a guard. Tap it and the guns will do the rest." : "Two of ours by their wreck. Drive to them and pick them up.");
        }

        void TickObjective(float dt)
        {
            if (objective == null) return; var L = Leader;
            var od = objective.pos - L.transform.position; od.y = 0f;
            if (objective.kind == "battery")
            {
                objective.targets.RemoveAll(v => v == null || v.dead);
                if (objective.targets.Count == 0)
                {
                    score += 600; objectivesReached++; shake = Mathf.Max(shake, 0.3f); Sfx.Pickup(); hud.Popup(objective.pos, "+600", new Color(1f, 0.6f, 0.3f)); hud.Toast("Rocket battery destroyed · +600", 2.6f);
                    Destroy(objective.marker.gameObject); objective = null; NextObjective(); return;
                }
                // a salvo every fourteen seconds once we are within 90 m: six rockets, each a mortar round on the platoon
                objective.salvo -= dt;
                if (objective.salvo <= 0f && od.magnitude < 90f)
                {
                    objective.salvo = 14f; var centre = L.transform.position + L.Forward * 4f; hud.Toast("Nebelwerfer salvo incoming!", 2.4f); Sfx.Artillery(objective.pos);
                    for (int i = 0; i < 6; i++) { var at = centre + new Vector3(Random.Range(-9f, 9f), 0f, Random.Range(-9f, 9f)); float when = 3f + i * 0.25f; mortars.Add(new Mortar { at = at, timer = when, ring = fx.Marker(at, new Color(1f, 0.25f, 0.2f), 7f) }); fx.Incoming(at, when); }
                    foreach (var w in objective.targets) for (int i = 0; i < 3; i++) fx.Flak(w.transform.position + Vector3.up * 1.4f, (Vector3.up * 1.2f + (centre - w.transform.position).normalized * 0.4f + Random.insideUnitSphere * 0.08f).normalized);
                }
                objective.marker.position = objective.pos; hud.Objective(objective.pos, od.magnitude, cam, true, "Battery " + objective.targets.Count + "/2");
                return;
            }
            if (objective.kind == "crew")
            {
                objective.smokeTimer -= dt; if (objective.smokeTimer <= 0f) { objective.smokeTimer = 0.5f; fx.Signal(objective.pos + Vector3.up * 0.6f, new Color(1f, 0.35f, 0.3f, 0.7f)); }
                if (od.magnitude < 11f)
                {
                    // they run to the leader, patch him up and ride along
                    score += 400; objectivesReached++; Sfx.Pickup(); L.hp = Mathf.Min(Depot.LeaderHp + 1f, L.hp + 2f); Depot.Tally("rescued", 1);
                    foreach (var f in objective.figures) { if (f == null) continue; var to = L.transform.position - f.position; to.y = 0f; infantry.BailOut(f.position, to.normalized, Depot.Nation, 1); Destroy(f.gameObject); }
                    hud.Popup(objective.pos, "+400", new Color(0.4f, 0.9f, 0.6f)); hud.Toast("Crew picked up · the leader is patched up · +400", 2.8f); if (Random.value < 0.6f) Radio("kill");
                    Destroy(objective.marker.gameObject); objective = null; NextObjective(); return;
                }
                hud.Objective(objective.pos, od.magnitude, cam, true, "Crew");
                return;
            }
            if (objective.kind == "dump")
            {
                objective.smokeTimer -= dt; if (objective.smokeTimer <= 0f) { objective.smokeTimer = 0.6f; fx.Signal(objective.pos + Vector3.up * 0.8f, new Color(1f, 0.5f, 0.25f, 0.6f)); }
                hud.Objective(objective.pos, od.magnitude, cam, true, "Fuel dump " + objective.hits + "/3" + (pointLeft > 0f ? " · firing" : ""));
                return;
            }
            if (objective.kind == "car")
            {
                var car = objective.targets.Count > 0 ? objective.targets[0] : null; objective.clock -= dt;
                if (car == null || car.dead)
                {
                    score += 500; objectivesReached++; Sfx.Pickup(); hud.Popup(objective.pos, "+500", new Color(1f, 0.6f, 0.3f)); hud.Toast("Staff car stopped · +500", 2.6f);
                    Destroy(objective.marker.gameObject); objective = null; NextObjective(); return;
                }
                if (objective.clock <= 0f || (car.transform.position - L.transform.position).magnitude > 150f)
                {
                    hud.Toast("The staff car got away", 2.6f); foes.Remove(car); tracks.Forget(car); Destroy(car.gameObject);
                    Destroy(objective.marker.gameObject); objective = null; NextObjective(); return;
                }
                objective.pos = car.transform.position; objective.marker.position = objective.pos; od = objective.pos - L.transform.position; od.y = 0f;
                hud.Objective(objective.pos, od.magnitude, cam, true, "Staff car " + Mathf.CeilToInt(objective.clock) + " s");
                return;
            }
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
                    Destroy(objective.marker.gameObject); if (objective.grenade != null) Destroy(objective.grenade); objective = null; NextObjective();
                }
                return;
            }
            if (d.magnitude < 12f)
            {
                score += 300; objectivesReached++; shake = Mathf.Max(shake, 0.3f); Sfx.Pickup();
                hud.Popup(objective.pos, "+300", new Color(0.35f, 0.95f, 0.45f)); hud.Toast("Objective " + objective.n + " reached · +300", 2.6f);
                Destroy(objective.marker.gameObject); if (objective.grenade != null) Destroy(objective.grenade); objective = null; NextObjective();
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
                var d = new Drop { pos = pos, height = 55f, kind = Random.Range(0, 4) }; Flyover(pos);
                if (crateMaterial == null) { crateMaterial = new Material(Resources.Load<Material>("BarrelLit")); crateMaterial.SetColor("_BaseColor", new Color(0.45f, 0.36f, 0.22f)); crateMaterial.SetFloat("_Metallic", 0f); crateMaterial.SetFloat("_Smoothness", 0.2f); } ChuteMaterial();
                var cratePf = Resources.Load<GameObject>("Props/crate");
                if (cratePf != null) { d.crate = Instantiate(cratePf).transform; d.top = 1.2f; var cm = new Material(Resources.Load<Material>("VehicleLit")); cm.SetTexture("_BaseMap", Resources.Load<Texture2D>("Props/crate_tex")); cm.SetFloat("_Cull", 0f); foreach (var rr in d.crate.GetComponentsInChildren<Renderer>()) rr.sharedMaterial = cm; }
                else { d.crate = GameObject.CreatePrimitive(PrimitiveType.Cube).transform; Destroy(d.crate.GetComponent<Collider>()); d.crate.localScale = new Vector3(1.3f, 1f, 1.3f); d.lift = 0.5f; d.top = 0.5f; d.crate.GetComponent<Renderer>().sharedMaterial = crateMaterial; }
                d.canopy = Canopy(14, 6, 3.4f, 2.2f, out d.rest); d.chute = new GameObject("Canopy").transform; d.chute.gameObject.AddComponent<MeshFilter>().sharedMesh = d.canopy;
                var cr = d.chute.gameObject.AddComponent<MeshRenderer>(); cr.sharedMaterial = new Material(chuteMaterial); cr.sharedMaterial.SetColor("_BaseColor", ChuteColour(d.kind)); cr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                d.lines = new GameObject("Shrouds").AddComponent<LineRenderer>(); d.lines.positionCount = 12; d.lines.startWidth = d.lines.endWidth = 0.05f; d.lines.material = chuteMaterial; d.lines.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                drops.Add(d); hud.Toast("Supply drop coming down, " + Clock(pos), 2.8f);
            }
            TickPlanes(dt);
            for (int i = drops.Count - 1; i >= 0; i--)
            {
                var d = drops[i]; d.age += dt;
                if (d.height > 0f)
                {
                    d.height = Mathf.Max(0f, d.height - 6f * dt); var sway = new Vector3(Mathf.Sin(d.age * 1.3f), 0f, Mathf.Cos(d.age * 0.9f)) * 0.6f;
                    d.crate.position = d.pos + Vector3.up * (d.height + d.lift) + sway; d.chute.position = d.crate.position + Vector3.up * (4f + d.top); d.crate.rotation = Quaternion.Euler(0f, d.age * 9f, 0f);
                    d.chute.rotation = Quaternion.Euler(-sway.z * 6f, d.age * 9f, sway.x * 6f); Ripple(d.canopy, d.rest, d.age, 0.09f);
                    for (int k = 0; k < 6; k++) { float ang = k * Mathf.PI / 3f; d.lines.SetPosition(k * 2, d.crate.position + Vector3.up * d.top); d.lines.SetPosition(k * 2 + 1, d.chute.TransformPoint(new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * 3.4f)); }
                    if (d.height <= 0f)
                    {
                        // landed: the canopy collapses in a heap beside the crate, a light marks it
                        d.chute.position = d.pos + new Vector3(2.8f, 0.05f, 1.2f); d.chute.rotation = Quaternion.Euler(0f, d.age * 40f, 0f); Collapse(d.canopy, d.rest, d.kind); d.lines.enabled = false;
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
                        case 0: { float mend = bonus == "repair" ? 3f : 2f; L.hp = Mathf.Min(Depot.LeaderHp + 2f, L.hp + mend); hud.Toast("Repair kit · leader +" + mend, 2.6f); break; }
                        case 1: { int ap = Mathf.Min(apMax - apRounds, 14), he2 = Mathf.Min(heMax - heRounds, 6); apRounds += ap; heRounds += he2; hud.SetAmmo(apRounds, heRounds, loadHe); ammoMul = 1.5f; ammoLeft = 25f; hud.Toast($"Ammunition · +{ap} AP, +{he2} HE · APCR for 25 s", 2.8f); break; }
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
            bool spotted = observer != null && !observer.dead;
            if (t > 90f) { mortarTimer -= dt * (spotted ? 2f : 1f); if (mortarTimer <= 0f) {
                mortarTimer = 32f + Random.value * 18f; var r = new Vector3(L.Forward.z, 0f, -L.Forward.x);
                var centre = L.transform.position + L.Forward * Random.Range(-4f, 12f) + r * Random.Range(-9f, 9f);
                float sp = spotted ? 6f : 7f; if (spotted) centre = L.transform.position + L.Forward * Random.Range(3f, 10f) + r * Random.Range(-6f, 6f);   // called in by the observer: on us, and tight
                for (int i = 0; i < (spotted ? 4 : 5); i++) { var at = centre + new Vector3(Random.Range(-sp, sp), 0f, Random.Range(-sp, sp)); float when = 2.3f + i * 0.3f; mortars.Add(new Mortar { at = at, timer = when, ring = fx.Marker(at, new Color(1f, 0.25f, 0.2f), 7f) }); fx.Incoming(at, when); }
                Sfx.Whistle(centre); hud.Toast((spotted ? "Observer-called mortars, " : "Incoming! Mortars, ") + Clock(centre) + " · move", 2.6f);
            } }
            for (int i = mortars.Count - 1; i >= 0; i--)
            {
                var mo = mortars[i]; mo.timer -= dt; if (mo.timer > 0f) continue;
                Destroy(mo.ring.gameObject); fx.Explosion(mo.at); Sfx.Artillery(mo.at); props.Crater(mo.at, mo.bomb ? 6f : 3.5f); props.Blast(mo.at, mo.bomb ? 8f : 5f, mo.bomb ? 3.5f : 1.5f); mortars.RemoveAt(i); shake = Mathf.Max(shake, mo.bomb ? 0.9f : 0.4f);
                foreach (var v in platoon.ToArray()) { if (v.dead) continue; var d = v.transform.position - mo.at; d.y = 0f; if (d.magnitude < (mo.bomb ? 8f : 6f)) Damage(v, mo.bomb ? 2.5f : 1f, mo.at); }
                if (mo.bomb) foreach (var e in foes.ToArray()) { if (e.dead) continue; var d = e.transform.position - mo.at; d.y = 0f; if (d.magnitude < 8f) Damage(e, 2.5f, mo.at); }   // a bomb does not ask whose tank it is
                InfantryKilled(infantry.Blast(mo.at, mo.bomb ? 9f : 6f), mo.at);
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
                HangStar(L.transform.position + new Vector3(Random.Range(-8f, 8f), 0f, Random.Range(-4f, 12f)), "Star shell! You are lit up · move");
            } }
            TickStarBody(dt);
        }

        /// <summary>A flare on a parachute over a point, burning for sixteen seconds.</summary>
        void HangStar(Vector3 over, string toast)
        {
            {
                star = new Star { pos = over, height = 42f, life = 16f, flare = fx.StarFlare() };
                star.chute = new GameObject("StarCanopy").transform; star.chute.gameObject.AddComponent<MeshFilter>().sharedMesh = Canopy(10, 4, 1.5f, 1f, out _); star.chute.gameObject.AddComponent<MeshRenderer>(); star.chute.GetComponent<Renderer>().sharedMaterial = ChuteMaterial();
                star.light = new GameObject("StarLight").AddComponent<Light>(); star.light.type = LightType.Point; star.light.color = new Color(1f, 0.96f, 0.86f); star.light.range = 80f; star.light.intensity = 70f; star.light.shadows = LightShadows.None;
                Sfx.Flak(star.pos + Vector3.up * 30f); hud.Toast(toast, 3f);
            }
        }

        void TickStarBody(float dt)
        {
            var L = Leader;
            if (props.Lit && !litBySearchlight) { litBySearchlight = true; hud.Toast("Caught in the searchlight · drive out or shoot the lamp", 3f); }
            if (!props.Lit) litBySearchlight = false;
            if (star == null) { lit = props.Lit; return; }
            star.life -= dt; star.height = Mathf.Max(6f, star.height - 1.7f * dt); star.pos += new Vector3(0.5f, 0f, 0.3f) * dt;
            var at = star.pos + Vector3.up * star.height; star.flare.position = at; star.flare.GetChild(0).rotation = cam.transform.rotation; star.chute.position = at + Vector3.up * 3f; star.light.transform.position = at;
            float burn = Mathf.Clamp01(star.life / 3f) * (0.85f + 0.15f * Mathf.PerlinNoise(Time.time * 9f, 0.5f)); star.light.intensity = 70f * burn; star.flare.GetChild(0).localScale = Vector3.one * (7f * (0.5f + 0.5f * burn));
            star.puff -= dt; if (star.puff <= 0f) { star.puff = 0.25f; fx.Signal(at, new Color(0.8f, 0.8f, 0.8f, 0.5f)); }
            var d = L.transform.position - star.pos; d.y = 0f; lit = (star.life > 0f && d.magnitude < 40f) || props.Lit;
            if (star.life <= 0f) { fx.Release(star.flare); Destroy(star.chute.gameObject); Destroy(star.light.gameObject); star = null; lit = false; }
        }

        class Raid { public float t; public Vector3 a, b, dir, last; public bool lit, siren, marked, shown, whistled; public Transform plane, glow; }
        Raid raid; float raidTimer = 150f; Material stukaMaterial, stukaGlow;
        class AirStrike { public Vector3 target, dir; public float t, smoke, gun1, gun2; public bool roar, shown, fired1, fired2; public Transform p1, p2, g1, g2; }
        class Rocket { public Transform vis; public Vector3 from, to; public float t, flight; }
        AirStrike strike; readonly List<Rocket> rockets = new List<Rocket>(); float airCool; bool airUp, airArmed, airTested; float airArmedLeft; Material allyMaterial, stripeWhite, stripeBlack;

        /// <summary>A night raid: the drone of engines, flares over the platoon, a red line on the ground three seconds
        /// before the stick of five bombs lands on it, and the Ju 87 coming down out of the dark with its siren howling,
        /// pulling out low over the line and climbing away. From 2:30, every 80-120 s; not in fog or rain.</summary>
        void TickRaid(float dt)
        {
            var L = Leader; if (L == null) return;
            if (raid == null)
            {
                if (weather == Weather.Fog || weather == Weather.Rain || t < (debugRaid ? 3f : rule == "stukas" ? 60f : 150f)) return;
                raidTimer -= dt; if (raidTimer > 0f && !(debugRaid && raidTimer > 140f)) return;
                raidTimer = debugRaid ? 14f : rule == "stukas" ? 50f + Random.value * 20f : 80f + Random.value * 40f;
                float ang = Random.Range(-35f, 35f) * Mathf.Deg2Rad; var dir = new Vector3(Mathf.Cos(ang) * (Random.value < 0.5f ? -1f : 1f), 0f, Mathf.Sin(ang));   // across the screen: a dive from behind the camera would fill it
                var aim = L.transform.position + L.Forward * 6f;   // where the leader is heading, near enough
                raid = new Raid { a = aim - dir * 16f, b = aim + dir * 16f, dir = dir };
                Sfx.Drone(aim + Vector3.up * 40f - dir * 60f); hud.Toast("Aircraft overhead · Stukas!", 3f); if (Random.value < 0.7f) Radio("hit");
                return;
            }
            raid.t += dt; var mid = (raid.a + raid.b) * 0.5f;
            if (!raid.lit && raid.t >= 1f) { raid.lit = true; if (star == null) HangStar(mid + new Vector3(Random.Range(-6f, 6f), 0f, Random.Range(-6f, 6f)), "Flares! Bombers coming"); }
            if (!raid.siren && raid.t >= 1.4f) { raid.siren = true; Sfx.StukaDive(raid.a + raid.dir * 4f + Vector3.up * 18f); }
            if (!raid.marked && raid.t >= 1.8f)
            {
                raid.marked = true;
                for (int i = 0; i < 5; i++) { var at = Vector3.Lerp(raid.a, raid.b, i / 4f); float when = 3.2f + i * 0.16f; mortars.Add(new Mortar { at = at, timer = when, ring = fx.Marker(at, new Color(1f, 0.18f, 0.12f), 8f), bomb = true }); fx.Incoming(at, when); }
                hud.Toast("Dive bomber! Off the red line", 2.6f);
            }
            if (!raid.whistled && raid.t >= 4.4f) { raid.whistled = true; Sfx.Whistle(mid); }
            if (!raid.shown && raid.t >= 3.3f) { raid.shown = true; raid.plane = Stuka(out raid.glow); }
            if (raid.plane != null)
            {
                // down in a straight dive to the start of the line, pulled out at 18 m, then climbing away beyond its end
                float tau = raid.t - 3.3f; var p0 = raid.a - raid.dir * 90f + Vector3.up * 110f; var q = raid.a + raid.dir * 4f + Vector3.up * 18f;
                Vector3 pos;
                if (tau < 1.7f) pos = Vector3.Lerp(p0, q, Mathf.Pow(tau / 1.7f, 1.4f));
                else { float u = Mathf.Clamp01((tau - 1.7f) / 2.6f); var c = q + raid.dir * 45f + Vector3.up * 2f; var e = raid.b + raid.dir * 140f + Vector3.up * 95f; pos = (1 - u) * (1 - u) * q + 2 * (1 - u) * u * c + u * u * e; }
                var vel = pos - raid.plane.position; if (vel.sqrMagnitude > 1e-4f) raid.plane.rotation = Quaternion.LookRotation(vel.normalized, Vector3.up) * Quaternion.Euler(0f, 0f, Mathf.Sin(raid.t * 2f) * 6f);
                raid.plane.position = pos; if (raid.glow != null) raid.glow.rotation = cam.transform.rotation;
            }
            if (raid.t > 7.6f) { if (raid.plane != null) Destroy(raid.plane.gameObject); raid = null; }
        }

        /// <summary>The Ju 87 as it is seen at night: the model when there is one (Models/ju87_hull), else a dark
        /// gull-winged shape; either way with the glow of its exhaust stacks on the nose and their light on the ground.</summary>
        Transform Stuka(out Transform glowQuad)
        {
            var root = new GameObject("Ju 87").transform; root.position = raid.a - raid.dir * 90f + Vector3.up * 110f;
            if (stukaMaterial == null) { stukaMaterial = new Material(Resources.Load<Material>("VehicleLit")); var tex = Resources.Load<Texture2D>("Models/ju87"); if (tex != null) stukaMaterial.SetTexture("_BaseMap", tex); else stukaMaterial.SetColor("_BaseColor", new Color(0.09f, 0.1f, 0.1f)); }
            var pf = Resources.Load<GameObject>("Models/ju87_hull");
            if (pf != null) { var body = Instantiate(pf, root); foreach (var r in body.GetComponentsInChildren<Renderer>()) { r.sharedMaterial = stukaMaterial; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; } }
            else
            {
                // the fuselage, the inverted gull wings, the tail, the fixed undercarriage in its spats: enough of a shape against the flare light
                StukaPart(root, Vector3.zero, Quaternion.identity, new Vector3(1.2f, 1.3f, 11f));
                foreach (var s in new[] { -1f, 1f })
                {
                    StukaPart(root, new Vector3(s * 1.9f, -0.35f, 0.8f), Quaternion.Euler(0f, 0f, s * -12f), new Vector3(3.4f, 0.3f, 2.4f));
                    StukaPart(root, new Vector3(s * 5.2f, 0.25f, 0.6f), Quaternion.Euler(0f, 0f, s * 8f), new Vector3(3.8f, 0.25f, 1.9f));
                    StukaPart(root, new Vector3(s * 1.9f, -1.1f, 1.3f), Quaternion.identity, new Vector3(0.4f, 1.2f, 0.9f));
                }
                StukaPart(root, new Vector3(0f, 0.3f, -5f), Quaternion.identity, new Vector3(5f, 0.2f, 1.4f)); StukaPart(root, new Vector3(0f, 1.3f, -5.1f), Quaternion.identity, new Vector3(0.2f, 1.8f, 1.5f));
            }
            var lamp = new GameObject("Exhaust").AddComponent<Light>(); lamp.transform.SetParent(root, false); lamp.transform.localPosition = new Vector3(0f, 0.2f, 4.2f);
            lamp.type = LightType.Point; lamp.color = new Color(1f, 0.5f, 0.2f); lamp.range = 12f; lamp.intensity = 4f; lamp.shadows = LightShadows.None;
            if (stukaGlow == null) { stukaGlow = new Material(shellTemplate); stukaGlow.SetTexture("_BaseMap", glowTex); stukaGlow.SetColor("_BaseColor", new Color(1f, 0.55f, 0.25f, 1f)); }
            var g = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(g.GetComponent<Collider>()); g.name = "ExhaustGlow"; g.transform.SetParent(root, false); g.transform.localPosition = new Vector3(0f, 0.2f, 4.4f); g.transform.localScale = Vector3.one * 2.4f;
            var gr = g.GetComponent<Renderer>(); gr.sharedMaterial = stukaGlow; gr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; glowQuad = g.transform;
            return root;
        }
        void StukaPart(Transform root, Vector3 pos, Quaternion rot, Vector3 size) => PlanePart(root, stukaMaterial, pos, rot, size);
        void PlanePart(Transform root, Material m, Vector3 pos, Quaternion rot, Vector3 size)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(g.GetComponent<Collider>()); g.transform.SetParent(root, false); g.transform.localPosition = pos; g.transform.localRotation = rot; g.transform.localScale = size;
            var r = g.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>Air support: on station from 1:00, one strike a minute. AIR arms it and a tap marks the target: yellow
        /// smoke goes up on it, and a pair of Thunderbolts (Il-2s on the Eastern Front) comes in low across the field,
        /// guns walking up to the smoke, four rockets each into it, and climbs away.</summary>
        void TickAir(float dt)
        {
            bool su = Depot.Nation == "su";
            if (!airUp && rule != "noair" && (t >= 60f || debugAir)) { airUp = true; if (!debugAir) hud.Toast(su ? "Shturmoviks on station · press AIR" : "Thunderbolts on station · press AIR", 3.2f); }
            if (debugAir && !airTested && t > 5f) { airTested = true; var L0 = Leader; CallAir(L0.transform.position + L0.Forward * 34f); }
            if (airCool > 0f) airCool = Mathf.Max(0f, airCool - dt);
            if (airArmed) { airArmedLeft -= dt; if (airArmedLeft <= 0f) { airArmed = false; hud.Toast("Air strike called off", 1.6f); } }
            hud.SetAir(airUp, 1f - airCool / AirCoolFull, airArmed ? "MARK" : strike != null ? "INBOUND" : airCool > 0f ? Mathf.CeilToInt(airCool) + " s" : "READY", airArmed);
            TickRockets(dt);
            if (strike == null) return;
            var s = strike; s.t += dt;
            s.smoke -= dt; if (s.smoke <= 0f && s.t < 6f) { s.smoke = 0.22f; fx.Signal(s.target, new Color(0.95f, 0.82f, 0.2f, 0.75f)); }
            if (!s.shown && s.t >= 1.6f) { s.shown = true; s.p1 = Ally(su, out s.g1); s.p2 = Ally(su, out s.g2); }
            if (!s.roar && s.t >= 1.8f) { s.roar = true; Sfx.FighterPass(s.target - s.dir * 10f + Vector3.up * 20f); }
            FlyAlly(s.p1, s.g1, s, s.t - 1.6f, 0f); FlyAlly(s.p2, s.g2, s, s.t - 2.3f, 7f);
            Guns(s, s.p1, s.t - 2.5f, ref s.gun1, 0f, dt); Guns(s, s.p2, s.t - 3.2f, ref s.gun2, 7f, dt);
            if (!s.fired1 && s.t >= 2.9f) { s.fired1 = true; Salvo(s.p1, s, -6f, 0f); }
            if (!s.fired2 && s.t >= 3.6f) { s.fired2 = true; Salvo(s.p2, s, 2f, 7f); }
            if (s.t > 7f) { if (s.p1 != null) Destroy(s.p1.gameObject); if (s.p2 != null) Destroy(s.p2.gameObject); strike = null; }
        }

        void CallAir(Vector3 target)
        {
            float ang = Random.Range(-30f, 30f) * Mathf.Deg2Rad; var across = new Vector3(Mathf.Cos(ang) * (Random.value < 0.5f ? -1f : 1f), 0f, Mathf.Sin(ang));   // in from the side of the screen, seen side on
            target.y = 0f; airCool = AirCoolFull; strike = new AirStrike { target = target, dir = across };
            hud.Toast(Depot.Nation == "su" ? "Shturmoviks inbound · yellow smoke" : "Thunderbolts inbound · yellow smoke", 2.6f); Sfx.Click(); Radio("start");
        }

        /// <summary>A plane's run: a shallow dive in from the side to 18 m just short of the smoke, then climbing away past it.</summary>
        void FlyAlly(Transform p, Transform glow, AirStrike s, float tau, float off)
        {
            if (p == null) return;
            if (tau < 0f) { if (p.gameObject.activeSelf) p.gameObject.SetActive(false); return; }
            var side = new Vector3(s.dir.z, 0f, -s.dir.x) * off;
            var p0 = s.target - s.dir * 150f + Vector3.up * 70f + side; var q = s.target - s.dir * 12f + Vector3.up * 18f + side;
            Vector3 pos;
            if (tau < 1.6f) pos = Vector3.Lerp(p0, q, tau / 1.6f);
            else { float u = Mathf.Clamp01((tau - 1.6f) / 2.2f); var c = q + s.dir * 45f + Vector3.up * 1f; var e = s.target + s.dir * 160f + Vector3.up * 85f + side; pos = (1 - u) * (1 - u) * q + 2 * (1 - u) * u * c + u * u * e; }
            if (!p.gameObject.activeSelf) { p.gameObject.SetActive(true); p.position = pos; }
            var vel = pos - p.position; if (vel.sqrMagnitude > 1e-4f) p.rotation = Quaternion.LookRotation(vel.normalized, Vector3.up);
            p.position = pos; if (glow != null) glow.rotation = cam.transform.rotation;
        }

        /// <summary>The guns: the strike of the rounds walks up the line to the smoke in six tenths of a second.</summary>
        void Guns(AirStrike s, Transform plane, float tau, ref float next, float off, float dt)
        {
            if (plane == null || tau < 0f || tau > 0.6f) return;
            next -= dt; if (next > 0f) return; next = 0.045f;
            var side = new Vector3(s.dir.z, 0f, -s.dir.x);
            var at = s.target - s.dir * Mathf.Lerp(26f, 2f, tau / 0.6f) + side * (off + Random.Range(-1.5f, 1.5f));
            fx.MgTracer(plane.position, (at - plane.position).normalized); if (Random.value < 0.5f) fx.Dust(at); if (Random.value < 0.3f) Sfx.Mg(at);
            foreach (var v in foes.ToArray()) if (!v.dead && Flat(v.transform.position - at) < 2.6f) Damage(v, 0.35f, at);
            foreach (var v in platoon.ToArray()) if (!v.dead && Flat(v.transform.position - at) < 2.6f) Damage(v, 0.35f, at);
            InfantryKilled(infantry.Blast(at, 2.5f), at);
        }

        /// <summary>Four rockets off the rails, two from each wing, into the line round the smoke.</summary>
        void Salvo(Transform plane, AirStrike s, float start, float off)
        {
            if (plane == null) return; var side = new Vector3(s.dir.z, 0f, -s.dir.x);
            for (int i = 0; i < 4; i++)
            {
                var at = s.target + s.dir * (start + i * 3f) + side * (off + (i % 2 == 0 ? -1.6f : 1.6f));
                var vis = fx.Tracer(new Color(1f, 0.85f, 0.6f, 1f), new Color(1f, 0.55f, 0.25f, 0.7f));
                rockets.Add(new Rocket { vis = vis, from = plane.position + side * (i % 2 == 0 ? -2.5f : 2.5f), to = at, flight = 0.45f + i * 0.06f });
            }
            Sfx.Faust(plane.position);
        }

        void TickRockets(float dt)
        {
            for (int i = rockets.Count - 1; i >= 0; i--)
            {
                var r = rockets[i]; r.t += dt; float k = Mathf.Clamp01(r.t / r.flight);
                var pos = Vector3.Lerp(r.from, r.to, k); r.vis.position = pos; r.vis.rotation = Quaternion.LookRotation(cam.transform.forward, (r.to - r.from).normalized); fx.Trail(pos);
                if (k < 1f) continue;
                fx.Release(r.vis); rockets.RemoveAt(i);
                fx.Explosion(r.to); Sfx.Explosion(r.to); props.Crater(r.to, 3f); props.Blast(r.to, 4.5f, 2f); shake = Mathf.Max(shake, 0.35f);
                foreach (var v in foes.ToArray()) if (!v.dead && Flat(v.transform.position - r.to) < 4.5f) Damage(v, 2.2f, r.to);
                foreach (var v in platoon.ToArray()) if (!v.dead && Flat(v.transform.position - r.to) < 4.5f) Damage(v, 2.2f, r.to);
                InfantryKilled(infantry.Blast(r.to, 6f), r.to);
                if (phase != Phase.Play) return;
            }
        }
        static float Flat(Vector3 d) { d.y = 0f; return d.magnitude; }

        /// <summary>A P-47 Thunderbolt (an Il-2 on the Eastern Front) as it is seen at night: the model when there is one
        /// (Models/p47_hull, Models/il2_hull), else its dark shape - the Thunderbolt with the D-Day stripes on its wings -
        /// with the glow of its exhaust on the nose.</summary>
        Transform Ally(bool su, out Transform glowQuad)
        {
            var root = new GameObject(su ? "Il-2" : "P-47").transform; root.gameObject.SetActive(false); var id = su ? "il2" : "p47";
            if (allyMaterial == null) { allyMaterial = new Material(Resources.Load<Material>("VehicleLit")); var tex = Resources.Load<Texture2D>("Models/" + id); if (tex != null) allyMaterial.SetTexture("_BaseMap", tex); else allyMaterial.SetColor("_BaseColor", su ? new Color(0.16f, 0.2f, 0.14f) : new Color(0.2f, 0.21f, 0.15f)); }
            var pf = Resources.Load<GameObject>("Models/" + id + "_hull");
            if (pf != null) { var body = Instantiate(pf, root); foreach (var r in body.GetComponentsInChildren<Renderer>()) { r.sharedMaterial = allyMaterial; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; } }
            else
            {
                PlanePart(root, allyMaterial, Vector3.zero, Quaternion.identity, new Vector3(1.4f, 1.5f, 10.5f));
                PlanePart(root, allyMaterial, new Vector3(0f, -0.2f, 1f), Quaternion.identity, new Vector3(su ? 14.5f : 12.4f, 0.3f, 2.5f));
                PlanePart(root, allyMaterial, new Vector3(0f, 0.2f, -4.6f), Quaternion.identity, new Vector3(4.6f, 0.2f, 1.3f));
                PlanePart(root, allyMaterial, new Vector3(0f, 1.1f, -4.8f), Quaternion.identity, new Vector3(0.2f, 1.7f, 1.4f));
                PlanePart(root, allyMaterial, new Vector3(0f, 0f, 5.1f), Quaternion.identity, new Vector3(1.7f, 1.7f, 0.9f));
                if (!su)
                {
                    // the invasion stripes, white and black round the inner wings
                    if (stripeWhite == null) { stripeWhite = new Material(Resources.Load<Material>("VehicleLit")); stripeWhite.SetColor("_BaseColor", new Color(0.8f, 0.8f, 0.76f)); stripeBlack = new Material(Resources.Load<Material>("VehicleLit")); stripeBlack.SetColor("_BaseColor", new Color(0.04f, 0.04f, 0.04f)); }
                    foreach (var side in new[] { -1f, 1f }) for (int k = 0; k < 5; k++) PlanePart(root, k % 2 == 0 ? stripeWhite : stripeBlack, new Vector3(side * (1.9f + k * 0.46f), -0.18f, 1f), Quaternion.identity, new Vector3(0.46f, 0.32f, 2.52f));
                }
            }
            var lamp = new GameObject("Exhaust").AddComponent<Light>(); lamp.transform.SetParent(root, false); lamp.transform.localPosition = new Vector3(0f, 0.2f, 4.6f);
            lamp.type = LightType.Point; lamp.color = new Color(1f, 0.55f, 0.25f); lamp.range = 12f; lamp.intensity = 4f; lamp.shadows = LightShadows.None;
            if (stukaGlow == null) { stukaGlow = new Material(shellTemplate); stukaGlow.SetTexture("_BaseMap", glowTex); stukaGlow.SetColor("_BaseColor", new Color(1f, 0.55f, 0.25f, 1f)); }
            var g = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(g.GetComponent<Collider>()); g.name = "ExhaustGlow"; g.transform.SetParent(root, false); g.transform.localPosition = new Vector3(0f, 0.2f, 4.8f); g.transform.localScale = Vector3.one * 2.2f;
            var gr = g.GetComponent<Renderer>(); gr.sharedMaterial = stukaGlow; gr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; glowQuad = g.transform;
            return root;
        }

        // the transport that drops the crate: crosses the sky over the drop point and is gone
        class Plane { public Transform t; public Vector3 from, dir; public float age; }
        readonly List<Plane> planes = new List<Plane>(); Material planeMaterial;
        void Flyover(Vector3 over)
        {
            var pf = Resources.Load<GameObject>("Models/c47_hull"); if (pf == null) return;
            if (planeMaterial == null) { planeMaterial = new Material(Resources.Load<Material>("VehicleLit")); planeMaterial.SetTexture("_BaseMap", Resources.Load<Texture2D>("Models/c47")); planeMaterial.SetColor("_BaseColor", new Color(0.8f, 0.8f, 0.82f)); planeMaterial.SetFloat("_Cull", 0f); }
            var root = new GameObject("C-47").transform; var body = Instantiate(pf, root); body.transform.localScale = Vector3.one * 1.5f; body.transform.localRotation = Quaternion.Euler(0f, -62f, 0f);   // the model's nose, measured
            foreach (var r in body.GetComponentsInChildren<Renderer>()) { r.sharedMaterial = planeMaterial; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
            float a = Random.value * Mathf.PI * 2f; var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
            var p = new Plane { t = root, from = over - dir * 260f + Vector3.up * 95f, dir = dir }; root.position = p.from; root.rotation = Quaternion.LookRotation(dir); planes.Add(p);
            Sfx.Rumble();
        }
        void TickPlanes(float dt)
        {
            for (int i = planes.Count - 1; i >= 0; i--)
            {
                var p = planes[i]; p.age += dt; p.t.position = p.from + p.dir * (65f * p.age) + Vector3.up * Mathf.Sin(p.age * 0.7f) * 2f;
                if (p.age > 9f) { Destroy(p.t.gameObject); planes.RemoveAt(i); }
            }
        }

        /// <summary>Ammunition salvaged where an enemy died: a crate a couple of metres clear of the wreck, more in it
        /// for the heavier kills and now and then an HE round.</summary>
        void SpawnSalvage(Vehicle v)
        {
            int ap = v.spec.isGun ? 2 : v.spec.hp >= 6f ? 5 : v.spec.hp >= 3f ? 3 : 2; if (rule == "short") ap = Mathf.Max(1, ap - 1);
            DropCrate(props.PushOut(v.transform.position + new Vector3(Random.Range(-2.4f, 2.4f), 0f, Random.Range(-2.4f, 2.4f)), 1.4f), ap, Random.value < 0.3f ? 1 : 0);
        }

        /// <summary>A crate on the ground, fitted to 1.8 m whichever model it is, with a low warm light so the brass
        /// reads at night. When there are ten, the oldest goes.</summary>
        void DropCrate(Vector3 pos, int ap, int he)
        {
            if (salvage.Count >= 10) { Destroy(salvage[0].crate.gameObject); salvage.RemoveAt(0); }
            var s = new Salvage { pos = pos, ap = ap, he = he };
            var own = Resources.Load<GameObject>("Props/ammocrate"); var pf = own != null ? own : Resources.Load<GameObject>("Props/crate");
            if (salvageMaterial == null) { salvageMaterial = new Material(Resources.Load<Material>("VehicleLit")); salvageMaterial.SetTexture("_BaseMap", Resources.Load<Texture2D>(own != null ? "Props/ammocrate_tex" : "Props/crate_tex")); salvageMaterial.SetFloat("_Cull", 0f); }
            s.crate = Instantiate(pf).transform; s.crate.name = "AmmoCrate";
            var rs = s.crate.GetComponentsInChildren<Renderer>(); var b = rs[0].bounds;
            foreach (var r in rs) { b.Encapsulate(r.bounds); r.sharedMaterial = salvageMaterial; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
            float k = 1.8f / Mathf.Max(0.1f, Mathf.Max(b.size.x, b.size.z)); s.crate.localScale = Vector3.one * k;
            s.crate.position = pos - Vector3.up * b.min.y * k; s.crate.rotation = Quaternion.Euler(0f, Random.value * 360f, 0f);
            var glow = new GameObject("CrateGlow"); glow.transform.SetParent(s.crate, false); glow.transform.localPosition = new Vector3(0.8f, 1.1f, -0.6f) / k;
            s.glow = glow.AddComponent<Light>(); s.glow.type = LightType.Point; s.glow.range = 4.5f; s.glow.intensity = 1f; s.glow.color = new Color(1f, 0.84f, 0.6f); s.glow.shadows = LightShadows.None;
            salvage.Add(s);
        }

        /// <summary>Within four metres the leader takes what fits in the racks; now and then (one crate in seven) a new
        /// tank comes up with it while the platoon is short. A crate the racks have no room for stays a minute.</summary>
        void TickSalvage(float dt)
        {
            var L = Leader; if (L == null) return; cratePos.Clear();
            for (int i = salvage.Count - 1; i >= 0; i--)
            {
                var s = salvage[i]; s.age += dt; s.glow.intensity = 0.9f + Mathf.Sin(s.age * 3f) * 0.2f;
                var toL = L.transform.position - s.pos; toL.y = 0f;
                if (toL.magnitude < 4f && (apRounds < apMax || heRounds < heMax))
                {
                    int ap = Mathf.Min(apMax - apRounds, s.ap), he = Mathf.Min(heMax - heRounds, s.he);
                    apRounds += ap; heRounds += he; if (heAuto && apRounds > 0) { loadHe = false; heAuto = false; } hud.SetAmmo(apRounds, heRounds, loadHe);
                    Sfx.Pickup(); nightCrates++;
                    if (ap + he > 0) hud.Popup(s.pos, (ap > 0 ? "+" + ap + " AP" : "") + (ap > 0 && he > 0 ? " · " : "") + (he > 0 ? "+" + he + " HE" : ""), new Color(1f, 0.84f, 0.45f));
                    if (platoon.Count < maxPlatoon && Random.value < 0.15f) Reinforce();
                    Destroy(s.crate.gameObject); salvage.RemoveAt(i); continue;
                }
                if (s.age > 60f) { Destroy(s.crate.gameObject); salvage.RemoveAt(i); continue; }
                cratePos.Add(s.pos);
            }
        }

        /// <summary>Where an HE round burst on a hull: a point on the plate, a little out from the centre, that burns
        /// for six seconds and rides with the tank.</summary>
        void Hole(Vehicle v, Vector3 at)
        {
            var o = at - v.transform.position; o.y = 0f;
            var p = v.transform.position + o.normalized * Mathf.Max(o.magnitude, v.spec.radius * 0.8f) + Vector3.up * Mathf.Clamp(at.y, 1.1f, 1.9f);
            fx.HeImpact(p);
            if (v.dead) return;
            if (holes.Count >= 16) holes.RemoveAt(0);
            holes.Add(new HeHole { v = v, local = v.transform.InverseTransformPoint(p), left = 6f });
        }

        void TickHoles(float dt)
        {
            for (int i = holes.Count - 1; i >= 0; i--)
            {
                var h = holes[i]; h.left -= dt;
                if (h.v == null || h.left <= 0f) { holes.RemoveAt(i); continue; }
                h.tick -= dt; if (h.tick > 0f) continue; h.tick = 0.12f;
                fx.Ember(h.v.transform.TransformPoint(h.local), h.left / 6f);
            }
        }

        /// <summary>A hull slowed by what it has just gone through and by rubble under it; it picks up again over about
        /// a second. The platoon's own crashes are heard, and the leader's are felt.</summary>
        void Bog(Vehicle v, float keep, float dt)
        {
            if (v == Leader && crewRough > 0) keep = 1f - (1f - keep) * (1f - 0.12f * crewRough);   // the rough rider
            v.bog = Mathf.MoveTowards(v.bog, 1f, dt * 0.9f);
            if (keep < 1f) { v.bog = Mathf.Min(v.bog, keep); if (v.friendly) { Sfx.Crunch(v.transform.position); if (v == Leader) shake = Mathf.Max(shake, (1f - keep) * 0.45f); } }
            float rough = props.Rough(v.transform.position); if (v == Leader && crewRough > 0) rough = 1f - (1f - rough) * (1f - 0.12f * crewRough);
            v.bog = Mathf.Min(v.bog, rough);
        }

        /// <summary>How likely a kill is to set off its ammunition: the heavies carry more of it, the platoon's Shermans
        /// were known for it (wet stowage halves it), the ace and the boss always go up.</summary>
        float CookOffChance(Vehicle v)
        {
            if (v.spec.isGun || v.spec.casemate || v.spec.turretMesh == null) return 0f;
            if (debugCook || v == boss || v == ace) return 1f;
            return v.friendly ? (wetStowage ? 0.12f : 0.25f) : v.spec.hp >= 6f ? 0.35f : 0.28f;
        }

        /// <summary>The racks go up: the turret is thrown off to one side, turning over, and a jet of flame stands out of
        /// the ring behind it.</summary>
        void CookOff(Vehicle v)
        {
            var tr = v.BlowTurret(); if (tr == null) return;
            var cmd = tr.Find("Commander"); if (cmd != null) Destroy(cmd.gameObject);
            float h = 0.9f; var rs = tr.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0) { var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); h = Mathf.Clamp(b.max.y - tr.position.y, 0.5f, 1.6f); }
            var right = new Vector3(v.Forward.z, 0f, -v.Forward.x); var away = (right * (Random.value < 0.5f ? -1f : 1f) + v.Forward * Random.Range(-0.6f, 0.6f)).normalized * Random.Range(2.4f, 3.6f);   // off the side, clear of the hull
            tosses.Add(new Toss { t = tr, owner = v, h = h, vel = new Vector3(away.x, Random.Range(11f, 15f) * (v.spec.hp >= 6f ? 0.85f : 1f), away.z), spin = Random.onUnitSphere * Random.Range(200f, 460f), jet = 1.4f });
            fx.Explosion(tr.position + Vector3.up); Sfx.Explosion(tr.position);
            var L = Leader; shake = Mathf.Max(shake, L != null && Dist(v, L) < 30f ? 1f : 0.4f);
            if (debugCook) SlowMo(0.6f);
        }

        void TickTosses(float dt)
        {
            for (int i = tosses.Count - 1; i >= 0; i--)
            {
                var s = tosses[i];
                if (s.owner == null || s.t == null) { tosses.RemoveAt(i); continue; }
                if (s.jet > 0f) { s.jet -= dt; s.tick -= dt; if (s.tick <= 0f) { s.tick = 0.035f; fx.Jet(s.owner.transform.position + Vector3.up * s.owner.spec.ringHeight, Mathf.Clamp01(s.jet / 1.4f)); } }
                if (!s.down)
                {
                    s.vel += Vector3.down * 20f * dt; s.t.position += s.vel * dt;
                    if (!s.bounced) s.t.Rotate(s.spin * dt, Space.World); else s.t.rotation = Quaternion.Slerp(s.t.rotation, s.rest, dt * 7f);
                    if (s.t.position.y <= (s.bounced ? s.restY : 0.6f) && s.vel.y < 0f)
                    {
                        if (!s.bounced)
                        {
                            // the first strike: one bounce, then it settles the way it came down, on its ring or on its roof
                            s.bounced = true; bool roof = s.t.up.y < -0.2f;
                            s.rest = Quaternion.Euler(roof ? 180f + Random.Range(-8f, 8f) : Random.Range(-10f, 10f), s.t.eulerAngles.y, Random.Range(-10f, 10f)); s.restY = roof ? s.h - 0.05f : 0.05f;
                            s.vel = new Vector3(s.vel.x * 0.35f, 3f, s.vel.z * 0.35f);
                            if (s.t.position.y < s.restY) { var q = s.t.position; q.y = s.restY; s.t.position = q; }
                            fx.Dust(s.t.position); Sfx.Hit(s.t.position); shake = Mathf.Max(shake, 0.3f);
                        }
                        else { s.down = true; var p = s.t.position; p.y = s.restY; s.t.position = p; s.t.rotation = s.rest; s.t.SetParent(s.owner.transform, true); fx.Dust(p); }   // goes with the wreck when it is cleared
                    }
                }
                if (s.down && s.jet <= 0f) tosses.RemoveAt(i);
            }
        }

        /// <summary>A moment held: the game at 30% for this many real seconds, easing back at the end.</summary>
        void SlowMo(float seconds) { slowLeft = Mathf.Max(slowLeft, seconds); Time.timeScale = 0.3f; }

        void RemoveDrop(Drop d) { Destroy(d.crate.gameObject); Destroy(d.chute.gameObject); Destroy(d.lines.gameObject); if (d.marker != null) Destroy(d.marker.gameObject); Destroy(d.canopy); }

        static Color ChuteColour(int kind) { return new Color(1.7f, 1.7f, 1.6f); }   // white silk; the crate says what it carries   // repair white, ammunition red, smoke yellow, radio blue: the air force colour code

        /// <summary>A parachute canopy: a dome of gores that bulge between their seams, the apex at the top, the skirt
        /// at y = 0. Rendered from both sides. The rest positions come back for the ripple and the collapse.</summary>
        static Mesh Canopy(int gores, int rings, float radius, float height, out Vector3[] rest)
        {
            int seg = gores * 2; var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            v.Add(new Vector3(0f, height, 0f)); uv.Add(new Vector2(0.5f, 1f));
            const float span = 1.35f; float c0 = Mathf.Cos(span), s0 = Mathf.Sin(span);
            for (int r = 1; r <= rings; r++)
            {
                float t = (float)r / rings, ang = t * span, y = height * (Mathf.Cos(ang) - c0) / (1f - c0), rad = radius * Mathf.Sin(ang) / s0;
                for (int s = 0; s < seg; s++)
                {
                    float phi = s * Mathf.PI * 2f / seg, bulge = 1f + 0.06f * t * Mathf.Cos(gores * phi);   // seams pulled in, gores puffed out
                    v.Add(new Vector3(Mathf.Cos(phi) * rad * bulge, y - 0.12f * t * (1f - Mathf.Cos(gores * phi)) * 0.5f, Mathf.Sin(phi) * rad * bulge)); uv.Add(new Vector2((float)s / seg, 1f - t));
                }
            }
            for (int s = 0; s < seg; s++) { tri.Add(0); tri.Add(1 + (s + 1) % seg); tri.Add(1 + s); }
            for (int r = 1; r < rings; r++) for (int s = 0; s < seg; s++)
            {
                int a = 1 + (r - 1) * seg + s, b = 1 + (r - 1) * seg + (s + 1) % seg, c = a + seg, d = b + seg;
                tri.Add(a); tri.Add(b); tri.Add(c); tri.Add(b); tri.Add(d); tri.Add(c);
            }
            var m = new Mesh(); m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tri, 0); m.RecalculateNormals(); m.RecalculateBounds(); m.MarkDynamic(); rest = v.ToArray(); return m;
        }

        /// <summary>The canopy breathing in the wind: the skirt flutters, the crown barely moves.</summary>
        static readonly List<Vector3> rippleBuf = new List<Vector3>();
        static void Ripple(Mesh m, Vector3[] rest, float time, float amount)
        {
            rippleBuf.Clear();
            for (int i = 0; i < rest.Length; i++)
            {
                var p = rest[i]; float skirt = 1f - Mathf.Clamp01(p.y / 2.2f); float w = Mathf.Sin(time * 5.5f + p.x * 1.7f + p.z * 1.3f) * amount * skirt;
                rippleBuf.Add(new Vector3(p.x * (1f + w * 0.5f), p.y + w, p.z * (1f + w * 0.5f)));
            }
            m.SetVertices(rippleBuf);
        }

        /// <summary>The canopy on the ground: flattened to a rumpled heap, a little off round.</summary>
        static void Collapse(Mesh m, Vector3[] rest, int seed)
        {
            rippleBuf.Clear();
            for (int i = 0; i < rest.Length; i++)
            {
                var p = rest[i]; float n = Mathf.PerlinNoise(p.x * 0.9f + seed * 3f, p.z * 0.9f);
                rippleBuf.Add(new Vector3(p.x * (0.75f + 0.25f * n), 0.05f + p.y * 0.12f + n * 0.35f, p.z * (0.7f + 0.3f * (1f - n))));
            }
            m.SetVertices(rippleBuf); m.RecalculateNormals(); m.RecalculateBounds();
        }

        /// <summary>The half-track drives to thirty metres of the nearest tank, drops its squad off the back, turns
        /// round and leaves; out past 120 m it is gone.</summary>
        void TickTransport(Vehicle e, Vehicle target, float dist, float dt)
        {
            e.turretYaw = e.yaw;
            if (e.spec == VehicleSpec.Kubelwagen)
            {
                // the staff car runs straight away from us, weaving a little; it is not removed by distance here (the objective does that)
                var away = e.transform.position - target.transform.position; away.y = 0f; var wob = new Vector3(away.z, 0f, -away.x).normalized * Mathf.Sin(t * 1.3f) * 0.35f;
                e.Drive(Steer(e, new Vector2(away.x + wob.x * away.magnitude, away.z + wob.z * away.magnitude)), dt); e.Apply(); return;
            }
            if (e.sapper)
            {
                // across the front: straight on, a mine every four metres, then off and away
                if (e.sapperLeft > 0)
                {
                    e.Drive(Steer(e, new Vector2(e.Forward.x, e.Forward.z)), dt);
                    if ((e.transform.position - e.lastMine).magnitude >= 4f) { e.lastMine = e.transform.position; LayMine(e.transform.position - e.Forward * 3f); e.sapperLeft--; if (e.sapperLeft == 0) { e.leaving = true; hud.Toast("Mines down · the half-track is off", 2f); } }
                    e.Apply(); return;
                }
                var away = e.transform.position - target.transform.position; away.y = 0f; e.Drive(Steer(e, new Vector2(away.x, away.z)), dt); e.Apply();
                if (away.magnitude > 140f) { foes.Remove(e); tracks.Forget(e); Destroy(e.gameObject); }
                return;
            }
            if (!e.unloaded)
            {
                if (dist > 30f) { var d = target.transform.position - e.transform.position; e.Drive(Steer(e, new Vector2(d.x, d.z)), dt); }
                else { e.unloaded = true; e.leaving = true; infantry.Spawn(e.transform.position - e.Forward * 4f, e.Forward); hud.Toast("Infantry dismounting, " + Clock(e.transform.position), 2.6f); }
            }
            else
            {
                var away = e.transform.position - target.transform.position; away.y = 0f; e.Drive(Steer(e, new Vector2(away.x, away.z)), dt);
                if (dist > 120f) { foes.Remove(e); tracks.Forget(e); Destroy(e.gameObject); return; }
            }
            e.Apply();
        }

        /// <summary>An enemy vehicle into the fight; veteran nights give it half again the hits.</summary>
        Vehicle Foe(VehicleSpec spec, Vector3 pos, float yaw)
        {
            var e = Vehicle.Create(spec, false, pos, yaw); e.turretYaw = e.yaw; if (veteran) e.hp *= 1.5f; if (debugWeak) e.hp = 0.3f; if (spec == VehicleSpec.PanzerIV && Random.value < 0.5f) e.flank = Random.value < 0.5f ? -1 : 1; foes.Add(e); return e;
        }

        /// <summary>The coaxial and bow machine guns: a burst at any tank hunter within 24 m in front of the gun or the
        /// hull, a tracer every tenth of a second, one hit in three.</summary>
        void TickMg(Vehicle v, float dt)
        {
            v.mgTimer -= dt; v.mgSound -= dt; if (v.mgTimer > 0f) return;
            var m = infantry.Nearest(v.transform.position, 24f + (v == Leader ? 2f * crewBow : 0f)); if (m == null) return;
            var to = m.pos - v.transform.position; to.y = 0f; to.Normalize();
            if (Vector3.Dot(to, v.GunDirection) < 0.5f && Vector3.Dot(to, v.Forward) < 0.7f) return;
            v.mgTimer = 0.1f;
            var from = v.transform.position + Vector3.up * 1.7f + v.GunDirection * 2f;
            fx.MgTracer(from, (to + Random.insideUnitSphere * 0.04f).normalized);
            if (v.mgSound <= 0f) { v.mgSound = 0.75f; Sfx.Mg(v.transform.position); }
            if (Random.value < 0.33f + (v == Leader ? 0.05f * crewBow : 0f)) { infantry.Kill(m); InfantryKilled(1, m.pos); }
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

        void StartNight() { hud.HideTitle(); phase = Phase.Play; stick.Blocked = false; if (objective != null) Brief(objective.kind); }

        /// <summary>A change of screen behind the black: down, swap, up.</summary>
        System.Collections.IEnumerator Curtained(System.Action swap)
        {
            hud.Curtain(1f); yield return new WaitForSecondsRealtime(0.42f);
            swap(); yield return new WaitForSecondsRealtime(0.05f);
            hud.Curtain(0f);
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
                fx.Explosion(a.at); Sfx.Artillery(a.at); props.Crater(a.at, 5f); props.Blast(a.at, 7f, 3f); arty.RemoveAt(i); InfantryKilled(infantry.Blast(a.at, 7f), a.at);
                foreach (var e in foes.ToArray()) { if (e.dead) continue; var d = e.transform.position - a.at; d.y = 0f; if (d.magnitude < 7f) Damage(e, d.magnitude < 3.5f ? 2f : 1f, a.at); }
            }
        }

        void End(bool dawn)
        {
            if (phase == Phase.End) return;
            phase = Phase.End; stick.Blocked = true;
            int earned = Mathf.RoundToInt(score * (veteran ? 1.5f : 1f) * (endless ? 1.5f : 1f) * routePay) * (doubled ? 2 : 1) + (dawn || endless ? 500 : 0);
            Depot.AddPoints(earned - banked); banked = earned;                       // a revived night banks only what is new
            if (!nightRecorded) { Depot.RecordNight(kills, t); nightRecorded = true; }
            var done = Missions.Report(new Missions.Night { kills = kills, tigers = nightTigers, paks = nightPaks, crates = nightCrates, level = level, time = t, boss = bossKilled, objectives = objectivesReached, infantry = nightInfantry, tracked = nightTracked, focus = nightFocus, lamps = nightLamps, campaign = opNight == 5 && dawn ? 1 : 0 });
            Depot.Tally("kills", kills - talliedKills); talliedKills = kills; Depot.Tally("tigers", nightTigers - talliedTigers); talliedTigers = nightTigers;
            Depot.Tally("guns", nightPaks - talliedGuns); talliedGuns = nightPaks; Depot.Tally("infantry", nightInfantry - talliedInfantry); talliedInfantry = nightInfantry;
            Depot.Tally("objectives", objectivesReached - talliedObjectives); talliedObjectives = objectivesReached;
            if (bossKilled && !talliedAce) { talliedAce = true; Depot.Tally("aces", 1); }
            if (dawn && !crewCounted) { crewCounted = true; Depot.CrewNights = Depot.CrewNights + 1; Depot.Tally("crewNights", 1); }
            if (!dawn && !revived && !crewLostTonight) { crewLostTonight = true; crewBefore = Depot.CrewNights; Depot.CrewNights = 0; }
            if (dawn) { Depot.Tally("dawns", 1); if (veteran) Depot.Tally("veteranDawns", 1); } Depot.Tally("tracked", nightTracked - talliedTracked); talliedTracked = nightTracked; Depot.Tally("lamps", nightLamps - talliedLamps); talliedLamps = nightLamps;
            if (kills >= 15 && shotsFired > 0 && shotsHit * 2 >= shotsFired) Depot.Tally("sharp", 1);
            if (!logged) { logged = true; Depot.LogNight(TheatreName, kills, t, score, dawn); }
            {   // the leader's tank: its share of the night (a night that ends twice adds only what is new)
                int xpNow = Mathf.RoundToInt(score * (stand ? 0.05f : 0.1f)) + (dawn ? 100 : 0), gain = Mathf.Max(0, xpNow - careerXp);
                Career.Add(leaderId, gain, Mathf.Max(0, kills - careerKills), !careerNight, dawn && !careerDawn, score);
                careerXp = Mathf.Max(careerXp, xpNow); careerKills = kills; careerNight = true; if (dawn) careerDawn = true;
                careerLine = "\n" + leaderName + " · +" + gain + " XP" + (Career.AnyUpgrade(leaderId) ? " · an upgrade is ready" : "");
                int cxNow = Mathf.RoundToInt(score * (stand ? 0.05f : 0.1f)) + (dawn ? 100 : 0), cGain = Mathf.Max(0, cxNow - crewXpBanked); Depot.AddCrewXp(cGain); crewXpBanked = Mathf.Max(crewXpBanked, cxNow);   // the crew's share, in the second currency
                if (crewXpBanked > 0) careerLine += "\nCrew · +" + crewXpBanked + " XP to train with";
            }
            var medals = Medals.Check();
            int bonus = 0; var lines = new System.Text.StringBuilder(); foreach (var o in done) { bonus += o.reward; lines.Append("\nOrder carried out · " + o.Title + " · +" + o.reward); }
            foreach (var md in medals) { bonus += Medals.Reward; lines.Append("\nMedal · " + md.name + " · +" + Medals.Reward); }
            hud.SetEndPoints(earned + bonus);
            int m = Mathf.FloorToInt(t / 60f), s = Mathf.FloorToInt(t % 60f);
            int acc = shotsFired > 0 ? Mathf.RoundToInt(100f * shotsHit / shotsFired) : 0;
            string crewLine = dawn ? $"\nThe crew's {Depot.CrewNights}{(Depot.CrewNights == 1 ? "st" : Depot.CrewNights == 2 ? "nd" : Depot.CrewNights == 3 ? "rd" : "th")} night together · {Depot.CrewName}" : crewLostTonight && crewBefore > 0 ? $"\nThe crew got out, but {crewBefore} nights together are lost" : "";
            string statLine = $"{kills} enemy vehicles destroyed · {nightInfantry} infantry\n{m}:{s:00} held · level {level} · {objectivesReached} objectives\nGunnery {acc}% · Score {score * (doubled ? 2 : 1)}" + crewLine + careerLine + lines;
            if (stand) { StandEnd(dawn, statLine); return; }
            if (daily) { DailyEnd(dawn, statLine, dawn ? !doubled : !revived, earned + bonus); return; }
            if (opNight > 0) { OperationEnd(dawn, dawn ? !doubled : !revived, earned + bonus, bonus); return; }
            hud.ShowEnd(dawn, statLine + (endless ? "\nHeld into daylight · points ×1.5" : ""), dawn ? !doubled : !revived, endless && !dawn ? "DAYLIGHT · LEADER KNOCKED OUT" : null, endless && !dawn ? "The long night is over" : null);
            hud.ShowHold(dawn && !endless);
        }

        /// <summary>A daily challenge over: the night's own score (no ads, no multipliers) goes against the day's best, and
        /// the first run of the day is paid for the days in a row. Again fights the same day once more.</summary>
        void DailyEnd(bool dawn, string statLine, bool adAvailable, int points)
        {
            int before = Daily.Best(dailyDay), pay = Daily.Record(dailyDay, score);   // again after a revive: only the best moves, nothing is paid twice
            var en = System.Globalization.CultureInfo.InvariantCulture;
            var sb = new System.Text.StringBuilder(statLine);
            sb.Append(score > before ? "\nA new best for the day · " + score.ToString("N0", en) : "\nThe day's best · " + before.ToString("N0", en));
            if (pay > 0) sb.Append("\nDaily challenge · " + Daily.Streak + (Daily.Streak == 1 ? " day" : " days") + " in a row · +" + pay.ToString("N0", en));
            hud.SetEndPoints(points + pay);
            var dr = Daily.RuleOf(dailyDay);
            hud.ShowEnd(dawn, sb.ToString(), adAvailable, "DAILY CHALLENGE · " + dr.name.ToUpperInvariant(), dawn ? "The line holds" : "Assault over", "Fight the day again");
            hud.OnAgain = () => { PlayerPrefs.SetString("daily.launch", dailyDay); PlayerPrefs.Save(); SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); };
        }

        /// <summary>An operation night over. Its stars join the ones already won (each new one pays 300, the operation's
        /// last dawn 2000 the first time); after a dawn the next night is offered, after a loss the same night again.</summary>
        void OperationEnd(bool dawn, bool adAvailable, int points, int honours)
        {
            int mask = dawn ? 1 | (Met(opN.second) ? 2 : 0) | (Met(opN.third) ? 4 : 0) : 0;
            bool last = opNight == op.nights.Length, firstFinish = dawn && last && !Operations.Held(op.id, opNight);
            int fresh = Operations.Record(op.id, opNight, mask), pay = fresh * 300 + (firstFinish ? 2000 : 0);
            if (pay > 0) Depot.AddPoints(pay);
            var sb = new System.Text.StringBuilder();
            sb.Append(opN.second.text).Append(" · ").Append(Progress(opN.second));
            sb.Append("\n").Append(opN.third.text).Append(" · ").Append(Progress(opN.third));
            sb.Append($"\n{kills} vehicles · {Mathf.FloorToInt(t / 60f)}:{Mathf.FloorToInt(t % 60f):00} held · score {score * (doubled ? 2 : 1)}");
            var paid = new System.Collections.Generic.List<string>();
            if (fresh > 0) paid.Add($"{fresh} new star{(fresh > 1 ? "s" : "")} +{fresh * 300}");
            if (firstFinish) paid.Add($"{op.name} won +2000");
            if (honours > 0) paid.Add($"orders and medals +{honours}");
            if (paid.Count > 0) sb.Append("\n").Append(string.Join(" · ", paid));
            sb.Append(careerLine);
            hud.SetEndPoints(points + pay);
            string again = !dawn ? "Fight the night again" : last ? "Back to base" : "Night " + (opNight + 1) + " · " + op.nights[opNight].name;
            hud.ShowEnd(dawn, sb.ToString(), adAvailable, op.name.ToUpperInvariant() + " · NIGHT " + opNight + " OF " + op.nights.Length, dawn ? (last ? op.name + " is won" : "The line holds") : "The night is lost", again);
            hud.ShowStars(new[] { (mask & 1) != 0, (mask & 2) != 0, (mask & 4) != 0 });
            int launch = !dawn ? opNight : last ? 0 : opNight + 1;
            hud.OnAgain = () => { if (launch > 0) { PlayerPrefs.SetString("op.launch", op.id + ":" + launch); PlayerPrefs.Save(); } SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); };
        }

        /// <summary>Where the night stands on one of its goals.</summary>
        int OpStat(string s)
        {
            switch (s)
            {
                case "kills": return kills; case "tigers": return nightTigers; case "paks": return nightPaks; case "infantry": return nightInfantry;
                case "objectives": return objectivesReached; case "lamps": return nightLamps; case "aces": return nightAces; case "boss": return bossKilled ? 1 : 0;
                case "tracked": return nightTracked; case "crates": return nightCrates; case "intact": return wingmenLost == 0 ? 1 : 0;
                default: return 0;
            }
        }
        bool Met(Operations.Goal g) => OpStat(g.stat) >= g.n;
        string Progress(Operations.Goal g) => g.stat == "intact" ? (wingmenLost == 0 ? "none lost" : wingmenLost + " lost") : Mathf.Min(OpStat(g.stat), g.n) + " of " + g.n;
        /// <summary>A goal as it rides under the clock: amber once it is met.</summary>
        string GoalLine(Operations.Goal g)
        {
            string s = g.stat == "intact" ? (wingmenLost == 0 ? g.tag : "a wingman lost") : g.tag + " " + Mathf.Min(OpStat(g.stat), g.n) + "/" + g.n;
            return Met(g) ? "<color=#F5AD3D>" + s + "</color>" : s;
        }
        Depot.Commander commander; float radioCool;
        int tutorialStep; float tutorialAt;
        /// <summary>Five hints on the first night, each when its moment comes: driving, the turrets, the objective, the cards, the hedges.</summary>
        void Tutorial(float dt)
        {
            tutorialAt += dt;
            switch (tutorialStep)
            {
                case 0: if (tutorialAt > 1.5f) { hud.Toast("Drag anywhere to drive the leader", 4f); tutorialStep++; tutorialAt = 0f; } break;
                case 1: if (foes.Count > 0 && tutorialAt > 2f) { hud.Toast("The turrets aim and fire on their own · tap an enemy to focus fire", 4.5f); tutorialStep++; tutorialAt = 0f; } break;
                case 2: if (tutorialAt > 12f) { hud.Toast("The green arrow points to the objective · reach it for points", 4f); tutorialStep++; tutorialAt = 0f; } break;
                case 3: if (level >= 2 && tutorialAt > 4f) { hud.Toast("Hug a hedge: a third of the shells stop in the bank", 4f); tutorialStep++; tutorialAt = 0f; } break;
                case 4: if (platoon.Count >= 2 && tutorialAt > 3f) { hud.Toast("Wingmen hold formation · the buttons below change it", 4f); tutorialStep++; } break;
            }
        }
        /// <summary>A line over the radio from the commander in the hatch, in his own words; never two within eight seconds.</summary>
        void Radio(string when)
        {
            if (commander == null || Time.time < radioCool) return; radioCool = Time.time + 8f; string line = null; int r = Random.Range(0, 2);
            switch (commander.id)
            {
                case "kowalski": line = when == "start" ? "Kowalski: Eyes open. Nobody dies tonight." : when == "kill" ? (r == 0 ? "Kowalski: Scratch one. Keep loading." : "Kowalski: That's how it's done.") : when == "hit" ? "Kowalski: We're hit! Driver, move!" : when == "tiger" ? "Kowalski: Tiger. Flank it, don't front it." : when == "dawn" ? "Kowalski: Sun's up. Good work, all of you." : null; break;
                case "hale": line = when == "start" ? "Hale: Map says hedgerows all the way. Stay on the lanes." : when == "kill" ? (r == 0 ? "Hale: Target down. Mark it." : "Hale: Good shooting, gunner.") : when == "hit" ? "Hale: Damage report!" : when == "tiger" ? "Hale: Heavy armour, eleven o'clock. Use the hedge." : when == "dawn" ? "Hale: Objective secured. Well done." : null; break;
                case "rivers": line = when == "start" ? "Rivers: Come out fighting. Let's go." : when == "kill" ? (r == 0 ? "Rivers: Got him. Next." : "Rivers: Keep 'em coming.") : when == "hit" ? "Rivers: Shake it off. We're still rolling." : when == "tiger" ? "Rivers: Big cat. Mine." : when == "dawn" ? "Rivers: We held. Told you." : null; break;
                case "orlov": line = when == "start" ? "Orlov: Forward. Kursk was worse." : when == "kill" ? (r == 0 ? "Orlov: Burn." : "Orlov: One less.") : when == "hit" ? "Orlov: Armour holds. Drive." : when == "tiger" ? "Orlov: Tiger. Close in, hit the side." : when == "dawn" ? "Orlov: Dawn. We are still here." : null; break;
                case "samusenko": line = when == "start" ? "Samusenko: Check your engines. Go." : when == "kill" ? (r == 0 ? "Samusenko: Hit. Reload." : "Samusenko: Clean shot.") : when == "hit" ? "Samusenko: I'll patch it. Keep moving." : when == "tiger" ? "Samusenko: Heavy one. Don't stop." : when == "dawn" ? "Samusenko: Morning. Everyone still runs." : null; break;
                case "belov": line = when == "start" ? "Belov: Engines warm. Let's run them." : when == "kill" ? (r == 0 ? "Belov: Ha! Next one." : "Belov: Too slow, Fritz.") : when == "hit" ? "Belov: Ouch. Faster, then." : when == "tiger" ? "Belov: Tiger? We're faster." : when == "dawn" ? "Belov: Sunrise. Good drive." : null; break;
            }
            if (line != null) hud.Toast(line, 3f);
        }

        void OnAd()
        {
            // the rewarded video is a mock here: the reward is granted after a moment
            if (phase != Phase.End) return;
            hud.SetAdNote("30 s ad · mock, skipping…");
            if (t >= NightLength) { doubled = true; Depot.AddPoints(score); banked += score; hud.SetEndPoints(banked); hud.ShowEnd(true, $"{kills} enemy vehicles destroyed\nScore {score * 2} (doubled)", false); return; }
            revived = true;
            var L = Vehicle.Create(Wingman, true, platoon.Count > 0 ? platoon[0].transform.position - platoon[0].Forward * 8f : Vector3.zero, platoon.Count > 0 ? platoon[0].yaw : 0f);
            if (crewLostTonight) { Depot.CrewNights = crewBefore; crewLostTonight = false; }   // pulled out alive: the same men, their nights kept
            L.hp = Depot.LeaderHp; platoon.Insert(0, L); leaderShield = 4f;
            hud.HideEnd(); phase = Phase.Play; stick.Blocked = false; hud.Toast("Field repair · back in the fight");
        }
    }
}

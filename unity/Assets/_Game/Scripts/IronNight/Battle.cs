using System.Collections.Generic;
using UnityEngine;
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
        class Shell { public Vector3 pos, vel; public bool friendly, he; public float dmg, life; public Transform vis; public Material mat; }
        class Flare { public Vector3 pos; public float age; public Transform vis; public Light light; public Material mat; }
        class Wreck { public Vehicle v; public float age, burnTimer; public Light fire; }
        enum Phase { Play, LevelUp, End }

        const float NightLength = 300f;         // five minutes of darkness, dawn at 5:00
        const float ShellSpeed = 62f, EnemyShellSpeed = 55f, GroundTile = 40f;

        Camera cam; Hud hud; TouchStick stick; Fx fx;
        Transform ground; Light flareLight; readonly List<Transform> searchlights = new List<Transform>();
        readonly List<Vehicle> platoon = new List<Vehicle>(); readonly List<Vehicle> foes = new List<Vehicle>();
        readonly List<Shell> shells = new List<Shell>(); readonly List<Flare> flares = new List<Flare>(); readonly List<Wreck> wrecks = new List<Wreck>();
        Material shellTemplate; Texture2D glowTex;
        Formation formation = Formation.Wedge; Phase phase = Phase.Play;
        float t, spawnTimer = 6f, leaderShield; int level = 1, xp, xpNeed = 6, score, kills, maxPlatoon = 4, reinforcements;
        bool wave2, wave4, revived, doubled;
        float damageMul = 1f, reloadMul = 1f, rangeMul = 1f, speedMul = 1f; bool he;

        Vehicle Leader => platoon.Count > 0 ? platoon[0] : null;

        public void Build(Camera camera, Hud h, TouchStick s, Fx effects)
        {
            cam = camera; hud = h; stick = s; fx = effects;
            shellTemplate = Resources.Load<Material>("Additive"); glowTex = Lightswarm.ProceduralSprites.Glow(64, 0.3f).texture;
            BuildWorld();
            platoon.Add(Vehicle.Create(VehicleSpec.Sherman, true, Vector3.zero, 0f)); Leader.hp = 8f;
            platoon.Add(Vehicle.Create(VehicleSpec.Sherman, true, new Vector3(-6f, 0f, -5.5f), 0f));
            hud.OnFormation = f => formation = f;
            hud.OnAd = OnAd; hud.OnAgain = () => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            hud.Set(0f, platoon.Count); hud.SetLevel(level, 0f);
            PlaceCamera(true);
        }

        void BuildWorld()
        {
            // the field: one 400 m plane with the baked seamless tile, kept under the camera on a 40 m grid
            var g = GameObject.CreatePrimitive(PrimitiveType.Plane); g.name = "Ground"; Destroy(g.GetComponent<Collider>());
            g.transform.localScale = new Vector3(GroundTile, 1f, GroundTile);
            var gm = new Material(Resources.Load<Material>("GroundLit"));
            gm.SetTexture("_BaseMap", Resources.Load<Texture2D>("Textures/ground")); gm.SetTextureScale("_BaseMap", new Vector2(10f, 10f)); gm.SetColor("_BaseColor", new Color(0.58f, 0.6f, 0.55f));
            g.GetComponent<Renderer>().sharedMaterial = gm; g.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ground = g.transform;

            // night: moonlight with soft shadows, a cold ambient, fog swallowing the distance
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.17f, 0.2f, 0.3f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Exponential; RenderSettings.fogDensity = 0.011f; RenderSettings.fogColor = new Color(0.02f, 0.03f, 0.05f);
            var moonGo = new GameObject("Moon"); var moon = moonGo.AddComponent<Light>();
            moon.type = LightType.Directional; moon.color = new Color(0.72f, 0.78f, 1f); moon.intensity = 2.2f; moon.shadows = LightShadows.Soft; moon.shadowStrength = 0.8f;
            moonGo.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
            // the flare light over the platoon: warm, follows the leader, grows with the platoon
            var fl = new GameObject("FlareLight"); flareLight = fl.AddComponent<Light>();
            flareLight.type = LightType.Point; flareLight.color = new Color(1f, 0.72f, 0.4f); flareLight.intensity = 18f; flareLight.range = 40f; flareLight.shadows = LightShadows.None;
            // searchlights on the enemy side, sweeping
            for (int i = 0; i < 2; i++)
            {
                var sl = new GameObject("Searchlight " + i); var l = sl.AddComponent<Light>();
                l.type = LightType.Spot; l.color = new Color(0.82f, 0.88f, 1f); l.intensity = 60f; l.range = 220f; l.spotAngle = 5f; l.innerSpotAngle = 3f; l.shadows = LightShadows.None;
                var beam = fx.Beam(220f, 0.03f, new Color(0.75f, 0.82f, 1f, 0.16f)); beam.transform.SetParent(sl.transform, false);
                sl.transform.position = new Vector3(i == 0 ? -70f : 75f, 3f, 90f + i * 30f);
                searchlights.Add(sl.transform);
            }
        }

        void PlaceCamera(bool snap)
        {
            var L = Leader; if (L == null) return;
            var want = L.transform.position + new Vector3(0f, 30f, -27f);
            cam.transform.position = snap ? want : Vector3.Lerp(cam.transform.position, want, 1f - Mathf.Exp(-Time.deltaTime * 4f));
            cam.transform.LookAt(cam.transform.position + new Vector3(0f, -30f, 30f));
            ground.position = new Vector3(Mathf.Round(cam.transform.position.x / GroundTile) * GroundTile, 0f, Mathf.Round(cam.transform.position.z / GroundTile) * GroundTile);
            flareLight.transform.position = L.transform.position + Vector3.up * 11f;
        }

        void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            fx.Tick(dt);
            if (phase != Phase.Play) { PlaceCamera(false); TickWrecks(dt); return; }
            t += dt;
            var L = Leader;
            if (leaderShield > 0f) leaderShield -= dt;

            // the leader drives where the thumb points; the wingmen hold their slots
            if (stick.Active) L.Drive(stick.Direction, dt);
            for (int i = 1; i < platoon.Count; i++)
            {
                var v = platoon[i]; var slot = Slot(i); var d = slot - v.transform.position; d.y = 0f;
                if (d.magnitude > 1.2f) v.Drive(new Vector2(d.x, d.z) * (Mathf.Clamp01(d.magnitude / 5f)), dt);
            }
            foreach (var v in platoon) { v.speedMul = speedMul; v.damageMul = damageMul; v.rangeMul = rangeMul; v.reloadMul = reloadMul; }

            // turrets: nearest foe in range, else drift home
            foreach (var v in platoon)
            {
                v.reloadLeft -= dt;
                var target = Nearest(foes, v.transform.position, v.Range * 1.15f);
                if (target != null) { bool on = v.Aim(target.transform.position, dt); if (on && v.reloadLeft <= 0f && Dist(v, target) <= v.Range) Fire(v, target); }
                else v.IdleTurret(dt);
                v.Apply();
            }

            // enemies: close in, then hold and shoot
            for (int i = 0; i < foes.Count; i++)
            {
                var e = foes[i]; e.reloadLeft -= dt;
                var target = Nearest(platoon, e.transform.position, 1000f); if (target == null) continue;
                float dist = Dist(e, target);
                if (!e.spec.isGun && dist > e.Range * 0.8f) { var d = target.transform.position - e.transform.position; e.Drive(new Vector2(d.x, d.z), dt); }
                bool on = e.Aim(target.transform.position, dt);
                if (on && e.reloadLeft <= 0f && dist <= e.Range) Fire(e, target);
                e.Apply();
            }

            TickShells(dt); TickFlares(dt); TickWrecks(dt); TickSpawns(dt); TickSearchlights(dt);
            KeepApart();
            flareLight.range = 34f + platoon.Count * 3f;
            PlaceCamera(false);
            hud.Set(t, platoon.Count); hud.SetLeader(Mathf.CeilToInt(L.hp), 8);
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
            v.reloadLeft = v.spec.reload * v.reloadMul * (v.friendly ? 1f : Mathf.Lerp(1.9f, 1.1f, t / 120f));
            var scatter = (v.friendly ? 1.5f : 4f) * Mathf.Deg2Rad * Random.Range(-1f, 1f);
            var dir = Quaternion.Euler(0f, scatter * Mathf.Rad2Deg, 0f) * v.GunDirection; var pos = v.MuzzlePosition;
            float speed = v.friendly ? ShellSpeed : EnemyShellSpeed;
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(go.GetComponent<Collider>()); go.name = "Shell";
            var m = new Material(shellTemplate); m.SetTexture("_BaseMap", glowTex); m.SetColor("_BaseColor", v.friendly ? new Color(1f, 0.95f, 0.7f, 1f) : new Color(1f, 0.55f, 0.35f, 1f));
            var r = go.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.transform.localScale = new Vector3(0.7f, 2.6f, 1f);
            shells.Add(new Shell { pos = pos, vel = dir * speed, friendly = v.friendly, he = v.friendly && he, dmg = v.spec.damage * v.damageMul, life = v.Range / speed + 0.25f, vis = go.transform, mat = m });
            fx.MuzzleFlash(pos, dir);
        }

        void TickShells(float dt)
        {
            for (int i = shells.Count - 1; i >= 0; i--)
            {
                var s = shells[i]; s.pos += s.vel * dt; s.life -= dt;
                // the tracer: a stretched glow along the flight, facing the camera
                s.vis.position = s.pos; s.vis.rotation = Quaternion.LookRotation(cam.transform.forward, s.vel);
                var hitList = s.friendly ? foes : platoon; Vehicle hit = null;
                foreach (var v in hitList) { if (v.dead) continue; var d = v.transform.position - s.pos; d.y = 0f; if (d.sqrMagnitude < v.spec.radius * v.spec.radius) { hit = v; break; } }
                if (hit != null)
                {
                    Damage(hit, s.dmg, s.pos);
                    if (s.he) foreach (var v in hitList) if (v != hit && !v.dead && Dist(v, hit) < 5f) Damage(v, s.dmg * 0.5f, v.transform.position);
                    fx.Hit(new Vector3(s.pos.x, 1.6f, s.pos.z), s.he ? 1.5f : 1f);
                }
                if (hit != null || s.life <= 0f) { Destroy(s.mat); Destroy(s.vis.gameObject); shells.RemoveAt(i); }
            }
        }

        void Damage(Vehicle v, float dmg, Vector3 at)
        {
            if (v.friendly && v == Leader && leaderShield > 0f) return;
            v.Hit(dmg);
            if (v.hp > 0f) { if (v == Leader) hud.Toast("Leader hit"); return; }
            v.Wreck(); fx.Explosion(v.transform.position);
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
                int worth = v.spec == VehicleSpec.Tiger ? 5 : 2; score += worth * 50; xp += worth;
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
            var L = Leader;
            for (int i = flares.Count - 1; i >= 0; i--)
            {
                var f = flares[i]; f.age += dt;
                f.vis.position = f.pos + Vector3.up * (0.3f * Mathf.Sin(f.age * 3f)); f.vis.rotation = cam.transform.rotation; f.vis.localScale = Vector3.one * (2.6f + 0.6f * Mathf.Sin(f.age * 9f));
                var d = L.transform.position - f.pos; d.y = 0f;
                if (d.magnitude < 4f)
                {
                    if (platoon.Count < maxPlatoon) Reinforce(); else { score += 50; hud.Toast("+50"); }
                    Destroy(f.mat); Destroy(f.vis.gameObject); Destroy(f.light.gameObject); flares.RemoveAt(i);
                }
                else if (f.age > 40f) { Destroy(f.mat); Destroy(f.vis.gameObject); Destroy(f.light.gameObject); flares.RemoveAt(i); }
            }
        }

        void Reinforce()
        {
            reinforcements++;
            var spec = reinforcements % 3 == 0 ? VehicleSpec.Firefly : VehicleSpec.Sherman;
            var L = Leader; var v = Vehicle.Create(spec, true, L.transform.position - L.Forward * 12f, L.yaw);
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
            float interval = Mathf.Lerp(8f, 2.8f, t / 240f);
            if (spawnTimer <= 0f && foes.Count < 12)
            {
                spawnTimer = interval;
                var L = Leader; float ahead = L.yaw + Random.Range(-1.7f, 1.7f);
                var dir = new Vector3(Mathf.Sin(ahead), 0f, Mathf.Cos(ahead));
                float roll = Random.value;
                if (roll < 0.22f && t > 20f)
                {
                    // an anti-tank gun dug in on the platoon's way, waiting
                    var pos = L.transform.position + L.Forward * 38f + new Vector3(Random.Range(-14f, 14f), 0f, 0f);
                    var gun = Vehicle.Create(VehicleSpec.Pak40, false, pos, L.yaw + Mathf.PI); gun.turretYaw = gun.yaw; foes.Add(gun);
                }
                else
                {
                    var spec = (t > 90f && Random.value < Mathf.Lerp(0.1f, 0.35f, (t - 90f) / 180f)) ? VehicleSpec.Tiger : VehicleSpec.PanzerIV;
                    var pos = L.transform.position + dir * Random.Range(44f, 52f);
                    var e = Vehicle.Create(spec, false, pos, Mathf.Atan2(-dir.x, -dir.z)); e.turretYaw = e.yaw; foes.Add(e);
                }
            }
            if (t >= 120f && !wave2) { wave2 = true; Column(); }
            if (t >= 240f && !wave4) { wave4 = true; Column(); }
        }

        /// <summary>An armoured column crossing the front 34 m ahead: four Panzer IV and a Tiger in a line.</summary>
        void Column()
        {
            var L = Leader; var f = L.Forward; var r = new Vector3(f.z, 0f, -f.x); float side = Random.value < 0.5f ? -1f : 1f;
            for (int i = 0; i < 5; i++)
            {
                var spec = i == 0 ? VehicleSpec.Tiger : VehicleSpec.PanzerIV;
                var pos = L.transform.position + f * (34f + i * 3f) - r * side * (46f + i * 9f);
                var e = Vehicle.Create(spec, false, pos, Mathf.Atan2(r.x * side, r.z * side)); e.turretYaw = e.yaw; foes.Add(e);
            }
            hud.Toast("Armoured column");
        }

        void TickSearchlights(float dt)
        {
            var L = Leader;
            for (int i = 0; i < searchlights.Count; i++)
            {
                var s = searchlights[i]; float a = Mathf.Sin(t * 0.23f + i * 2.1f) * 0.9f;
                var toPlatoon = L.transform.position - s.position; float baseYaw = Mathf.Atan2(toPlatoon.x, toPlatoon.z);
                s.rotation = Quaternion.Euler(4f, (baseYaw + a) * Mathf.Rad2Deg, 0f);
                if (toPlatoon.magnitude > 150f) s.position = L.transform.position + L.Forward * 100f + new Vector3((i == 0 ? -1f : 1f) * 70f, 3f, 0f);
            }
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
                new Hud.Card { id = "reinf", title = "Reinforcements", desc = "A Sherman joins now; the platoon can grow by one more." },
            };
            if (he) all.RemoveAll(c => c.id == "he");
            var pick = new List<Hud.Card>(); while (pick.Count < 3 && all.Count > 0) { int i = Random.Range(0, all.Count); pick.Add(all[i]); all.RemoveAt(i); }
            phase = Phase.LevelUp; stick.Blocked = true;
            hud.ShowCards(pick, id =>
            {
                switch (id)
                {
                    case "he": he = true; break;
                    case "apcr": damageMul *= 1.5f; break;
                    case "rapid": reloadMul *= 0.8f; break;
                    case "radar": rangeMul *= 1.15f; break;
                    case "engine": speedMul *= 1.15f; break;
                    case "repair": Leader.hp = 9f; break;
                    case "reinf": maxPlatoon = Mathf.Min(6, maxPlatoon + 1); Reinforce(); break;
                }
                phase = Phase.Play; stick.Blocked = false;
            });
        }

        void End(bool dawn)
        {
            if (phase == Phase.End) return;
            phase = Phase.End; stick.Blocked = true;
            int m = Mathf.FloorToInt(t / 60f), s = Mathf.FloorToInt(t % 60f);
            hud.ShowEnd(dawn, $"{kills} enemy vehicles destroyed\n{m}:{s:00} held · level {level}\nScore {score * (doubled ? 2 : 1)}", dawn ? !doubled : !revived);
        }

        void OnAd()
        {
            // the rewarded video is a mock here: the reward is granted after a moment
            if (phase != Phase.End) return;
            hud.SetAdNote("30 s ad · mock, skipping…");
            if (t >= NightLength) { doubled = true; hud.ShowEnd(true, $"{kills} enemy vehicles destroyed\nScore {score * 2} (doubled)", false); return; }
            revived = true;
            var L = Vehicle.Create(VehicleSpec.Sherman, true, platoon.Count > 0 ? platoon[0].transform.position - platoon[0].Forward * 8f : Vector3.zero, platoon.Count > 0 ? platoon[0].yaw : 0f);
            L.hp = 8f; platoon.Insert(0, L); leaderShield = 4f;
            hud.HideEnd(); phase = Phase.Play; stick.Blocked = false; hud.Toast("Field repair · back in the fight");
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Lightswarm
{
    /// <summary>
    /// The whole prototype loop in one place: the guardian light moves with the joystick, the fireflies hold a
    /// formation around it, shadows home in from the edges, contact burns shadows and extinguishes fireflies, burnt
    /// shadows drop sparks that turn into new fireflies and fill the level bar; a full bar offers three cards.
    /// World units: camera half-height = 8. Rendering is one SpriteRenderer per entity; week one measures what a
    /// phone does with 500 + 300 of them.
    /// </summary>
    public class Swarm : MonoBehaviour
    {
        public enum Formation { Ring, Spear, Cloud, Spiral }
        public enum Kind { Wisp, Ember, Frost, Spark }

        [Header("Tuning")]
        public int startFireflies = 12;
        public int maxFireflies = 500;
        public float leaderSpeed = 4.4f;
        public float nightLength = 360f;
        public Formation formation = Formation.Ring;
        public bool stress;   // --stress: start with the full swarm and keep 300 shadows on screen, for frame-rate tests

        class Firefly { public Transform tr; public SpriteRenderer sr; public Vector2 pos; public float phase; public Kind kind; public bool dead; }
        class Shadow { public Transform tr; public SpriteRenderer sr; public Vector2 pos, vel; public float hp, r, speed, bite, contact, life, slow; public bool tide, ember, dead; }
        class Spark { public Transform tr; public Vector2 pos, vel; public float age; public bool dead; }
        class Burst { public Transform tr; public SpriteRenderer sr; public float t; }
        class Arc { public LineRenderer lr; public float t; }

        static readonly Color[] KindColor = { new(1f, 0.85f, 0.5f), new(1f, 0.45f, 0.22f), new(0.6f, 0.86f, 1f), new(0.8f, 0.72f, 1f) };
        static readonly float[] KindDps = { 4f, 4.8f, 3f, 3.4f };

        readonly List<Firefly> fireflies = new();
        readonly List<Shadow> shadows = new();
        readonly List<Spark> sparks = new();
        readonly List<Burst> bursts = new();
        readonly List<Arc> arcs = new();
        readonly Dictionary<long, List<Firefly>> grid = new();
        readonly Stack<GameObject> shadowPool = new(), sparkPool = new(), burstPool = new(), arcPool = new();

        Sprite fireflySprite, shadowSprite, sparkSprite, leaderSprite, ringSprite;
        Material unlit;
        Transform leader, root;
        Camera cam;
        FloatingJoystick joystick;
        Hud hud;

        Vector2 leaderPos, leaderVel, heading = Vector2.up;
        float t, spawnTimer = 1.5f, endTimer = -1f, sparkTimer;
        int peak, xp, xpNext = 10, level = 1, totalLight, kills;
        readonly int[] kindLevel = new int[4];
        float magnet = 1f, swift = 1f;
        bool tide2, tide4, over, paused;

        const float Cell = 0.7f;

        public int FireflyCount => fireflies.Count;
        public DarknessMask night;

        public void Build(Camera camera, FloatingJoystick joy, Hud overlay)
        {
            cam = camera; joystick = joy; hud = overlay;
            root = new GameObject("SwarmRoot").transform;
            unlit = Resources.Load<Material>("SpriteUnlit");
            fireflySprite = ProceduralSprites.Firefly();
            shadowSprite = ProceduralSprites.Shadow();
            sparkSprite = ProceduralSprites.Glow(32, 0.35f);
            leaderSprite = ProceduralSprites.Firefly(96);
            ringSprite = ProceduralSprites.Ring(96, 0.08f);

            var lg = new GameObject("Guardian");
            lg.transform.SetParent(root, false);
            var lsr = lg.AddComponent<SpriteRenderer>();
            lsr.sprite = leaderSprite; lsr.sortingOrder = 50; lsr.color = new Color(1f, 0.95f, 0.8f, 0.9f); lsr.material = unlit;
            lg.transform.localScale = Vector3.one * 1.3f;
            leader = lg.transform;

            kindLevel[0] = 1;
            for (int i = 0; i < startFireflies; i++) AddFirefly(Random.insideUnitCircle * 1.5f, Kind.Wisp);
            peak = fireflies.Count;
            hud.SetLevel(level, 0f);
        }

        // ---------- entities ----------
        void AddFirefly(Vector2 at, Kind kind)
        {
            if (fireflies.Count >= maxFireflies) return;
            var go = new GameObject("Firefly");
            go.transform.SetParent(root, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = fireflySprite; sr.sortingOrder = 20; sr.color = KindColor[(int)kind]; sr.material = unlit;
            fireflies.Add(new Firefly { tr = go.transform, sr = sr, pos = at, phase = Random.value * 100f, kind = kind });
            peak = Mathf.Max(peak, fireflies.Count);
        }

        Kind PickKind()
        {
            float total = 0f; for (int k = 0; k < 4; k++) total += kindLevel[k] > 0 ? (k == 0 ? 1.2f : kindLevel[k] * 0.9f) : 0f;
            float r = Random.value * total;
            for (int k = 0; k < 4; k++) { if (kindLevel[k] == 0) continue; r -= k == 0 ? 1.2f : kindLevel[k] * 0.9f; if (r <= 0f) return (Kind)k; }
            return Kind.Wisp;
        }

        void SpawnShadow(Vector2 at, string kind, Vector2? velocity = null)
        {
            float hpScale = 1f + t / 200f;
            var s = new Shadow { pos = at };
            switch (kind)
            {
                case "crawler": s.hp = 1.6f * hpScale; s.r = 0.25f; s.speed = 1.95f; s.bite = 1.4f; break;
                case "beast": s.hp = 16f * hpScale; s.r = 0.7f; s.speed = 0.68f; s.bite = 0.45f; break;
                case "tide": s.hp = 4f * hpScale; s.r = 0.375f; s.speed = 1.5f; s.bite = 0.8f; s.tide = true; s.vel = velocity ?? Vector2.zero; break;
                default: s.hp = 3f * hpScale; s.r = 0.35f; s.speed = 1.0f; s.bite = 0.9f; break;
            }
            GameObject go = shadowPool.Count > 0 ? shadowPool.Pop() : NewSpriteObject("Shadow", shadowSprite, 16, true);
            go.SetActive(true);
            go.transform.localScale = Vector3.one * (s.r * 3.2f);
            s.tr = go.transform; s.sr = go.GetComponent<SpriteRenderer>(); s.sr.color = Color.white;
            shadows.Add(s);
        }

        void SpawnSpark(Vector2 at)
        {
            GameObject go = sparkPool.Count > 0 ? sparkPool.Pop() : NewSpriteObject("Spark", sparkSprite, 30, true);
            go.SetActive(true);
            go.transform.localScale = Vector3.one * 0.5f;
            go.GetComponent<SpriteRenderer>().color = new Color(1f, 0.9f, 0.6f);
            float a = Random.value * Mathf.PI * 2f, v = 1f + Random.value * 1.75f;
            sparks.Add(new Spark { tr = go.transform, pos = at, vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * v });
        }

        void SpawnBurst(Vector2 at, float radius)
        {
            GameObject go = burstPool.Count > 0 ? burstPool.Pop() : NewSpriteObject("Burst", ringSprite, 40, true);
            go.SetActive(true);
            go.transform.position = new Vector3(at.x, at.y, 0f);
            bursts.Add(new Burst { tr = go.transform, sr = go.GetComponent<SpriteRenderer>(), t = 0f });
            go.transform.localScale = Vector3.one * (radius * 0.3f);
        }

        void SpawnArc(List<Vector2> points)
        {
            GameObject go;
            if (arcPool.Count > 0) go = arcPool.Pop();
            else
            {
                go = new GameObject("Arc"); go.transform.SetParent(root, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.material = unlit; lr.widthMultiplier = 0.07f; lr.useWorldSpace = true; lr.sortingOrder = 45; lr.numCornerVertices = 2;
            }
            go.SetActive(true);
            var line = go.GetComponent<LineRenderer>();
            var pts = new List<Vector3>();
            for (int i = 1; i < points.Count; i++)
                for (int k = 0; k <= 4; k++) { float u = k / 4f; Vector2 p = Vector2.Lerp(points[i - 1], points[i], u); if (k > 0 && k < 4) p += Random.insideUnitCircle * 0.12f; pts.Add(new Vector3(p.x, p.y, 0f)); }
            line.positionCount = pts.Count; line.SetPositions(pts.ToArray());
            arcs.Add(new Arc { lr = line, t = 0f });
        }

        GameObject NewSpriteObject(string name, Sprite sprite, int order, bool unlitMaterial)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite; sr.sortingOrder = order; if (unlitMaterial) sr.material = unlit;
            return go;
        }

        // ---------- formation slots, relative to the leader ----------
        Vector2 Slot(int i, int n)
        {
            switch (formation)
            {
                case Formation.Spear:
                {
                    int j = 0, idx = i; while (idx >= 2 + j) { idx -= 2 + j; j++; }
                    float x = -(0.85f + j * 0.425f), y = (idx - (1 + j) * 0.5f) * 0.45f;
                    return new Vector2(x * heading.x - y * heading.y, x * heading.y + y * heading.x);
                }
                case Formation.Cloud:
                {
                    float R = 1.25f + 0.3f * Mathf.Sqrt(n);
                    float a = Hash(i) * Mathf.PI * 2f + Mathf.Sin(t * 0.5f + Hash(i + 999) * 10f) * 0.3f;
                    float d = (0.3f + 0.7f * Mathf.Sqrt(Hash(i + 4242))) * R;
                    return new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * d;
                }
                case Formation.Spiral:
                {
                    float r = 0.275f * Mathf.Sqrt(i + 8), a = (i + 8) * 2.39996f + t * 0.6f;
                    return new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                }
                default:
                {
                    int k = 0, idx = i;
                    while (true)
                    {
                        float r = 1.1f + 0.65f * k; int cap = Mathf.Max(8, Mathf.FloorToInt(2f * Mathf.PI * r / 0.55f));
                        if (idx < cap) { float a = (float)idx / cap * Mathf.PI * 2f + t * 0.25f * (k % 2 == 0 ? -1f : 1f); return new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r; }
                        idx -= cap; k++;
                    }
                }
            }
        }

        static float Hash(int i) { uint x = (uint)i * 2654435761u; x ^= x >> 13; x *= 0x5bd1e995; x ^= x >> 15; return (x & 0xffffff) / 16777216f; }

        // ---------- the loop ----------
        void Update()
        {
            if (over) { endTimer -= Time.deltaTime; if (endTimer <= 0f) Restart(); return; }
            if (paused) return;
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            t += dt;

            // guardian
            Vector2 wish = joystick.Direction * leaderSpeed * swift;
            leaderVel = Vector2.Lerp(leaderVel, wish, Mathf.Min(1f, dt * 7f));
            leaderPos += leaderVel * dt;
            if (leaderVel.magnitude > 0.6f) heading = Vector2.Lerp(heading, leaderVel.normalized, Mathf.Min(1f, dt * 5f)).normalized;
            leader.position = new Vector3(leaderPos.x, leaderPos.y, 0f);
            if (night != null) { night.Follow(leaderPos); night.SetHole(2.4f + Mathf.Min(6f, fireflies.Count * 0.012f)); }
            var camPos = cam.transform.position;
            cam.transform.position = Vector3.Lerp(camPos, new Vector3(leaderPos.x, leaderPos.y, camPos.z), Mathf.Min(1f, dt * 4f));

            // fireflies follow their slots; a slow hover changes size and brightness a little
            int n = fireflies.Count; float k = 1f - Mathf.Exp(-dt * 6.5f);
            grid.Clear();
            for (int i = 0; i < n; i++)
            {
                var s = fireflies[i];
                Vector2 target = leaderPos + Slot(i, n) + new Vector2(Mathf.Sin(t * 3f + s.phase), Mathf.Cos(t * 2.3f + s.phase * 1.7f)) * 0.06f;
                s.pos += (target - s.pos) * k;
                s.tr.position = new Vector3(s.pos.x, s.pos.y, 0f);
                float z = 0.5f + 0.5f * Mathf.Sin(t * 1.6f + s.phase);
                s.tr.localScale = Vector3.one * (0.55f * (0.85f + 0.3f * z));
                var c = KindColor[(int)s.kind]; c.a = 0.65f + 0.35f * Mathf.Sin(t * 5f + s.phase); s.sr.color = c;
                long key = Key(s.pos);
                if (!grid.TryGetValue(key, out var cell)) { cell = new List<Firefly>(8); grid[key] = cell; }
                cell.Add(s);
            }

            SpawnTick(dt);

            // shadows: move, touch, burn, extinguish
            bool lost = false;
            for (int si = 0; si < shadows.Count; si++)
            {
                var s = shadows[si]; s.life += dt;
                if (s.tide) { s.pos += s.vel * dt; if (s.life > 16f) s.dead = true; }
                else { Vector2 d = leaderPos - s.pos; float dist = Mathf.Max(0.001f, d.magnitude); s.pos += d / dist * s.speed * (s.slow > 0f ? 0.5f : 1f) * dt; }
                if (s.slow > 0f) s.slow -= dt;
                float rr = s.r + 0.4f, rr2 = rr * rr; float dps = 0f; int cnt = 0; Firefly victim = null; bool ember = false, frost = false;
                int cx0 = Mathf.FloorToInt((s.pos.x - rr) / Cell), cx1 = Mathf.FloorToInt((s.pos.x + rr) / Cell), cy0 = Mathf.FloorToInt((s.pos.y - rr) / Cell), cy1 = Mathf.FloorToInt((s.pos.y + rr) / Cell);
                for (int cx = cx0; cx <= cx1; cx++)
                for (int cy = cy0; cy <= cy1; cy++)
                {
                    if (!grid.TryGetValue(Key(cx, cy), out var cell)) continue;
                    foreach (var f in cell)
                    {
                        if (f.dead || (f.pos - s.pos).sqrMagnitude > rr2) continue;
                        dps += KindDps[(int)f.kind] * (1f + 0.25f * (kindLevel[(int)f.kind] - 1)); cnt++;
                        if (f.kind == Kind.Ember) ember = true; else if (f.kind == Kind.Frost) frost = true;
                        if (victim == null || Random.value < 1f / cnt) victim = f;
                    }
                }
                if (cnt > 0)
                {
                    s.hp -= Mathf.Min(45f, dps) * dt; s.ember = ember; if (frost) s.slow = 0.6f;
                    s.contact += dt;
                    if (s.contact >= s.bite) { s.contact = 0f; if (victim != null) { victim.dead = true; lost = true; } }
                }
                else s.contact = Mathf.Max(0f, s.contact - dt * 2f);
                if (s.hp <= 0f && !s.dead) KillShadow(s);
                s.tr.position = new Vector3(s.pos.x, s.pos.y, 0f);
                s.tr.localScale = Vector3.one * (s.r * 3.2f * (1f + 0.07f * Mathf.Sin(t * 4f + si)));
                s.sr.color = s.slow > 0f ? new Color(0.7f, 0.85f, 1f) : Color.white;
            }
            for (int i = shadows.Count - 1; i >= 0; i--) if (shadows[i].dead) { Recycle(shadows[i].tr.gameObject, shadowPool); shadows.RemoveAt(i); }
            if (lost)
            {
                for (int i = fireflies.Count - 1; i >= 0; i--) if (fireflies[i].dead) { Destroy(fireflies[i].tr.gameObject); fireflies.RemoveAt(i); }
                if (fireflies.Count == 0) { End("Darkness"); return; }
            }

            // sparks drift to the guardian and become fireflies
            float pull = (2f + fireflies.Count * 0.003f) * magnet * (formation == Formation.Spiral ? 1.35f : 1f);
            for (int i = 0; i < sparks.Count; i++)
            {
                var p = sparks[i]; Vector2 d = leaderPos - p.pos; float dist = Mathf.Max(0.001f, d.magnitude);
                // inside the pull radius the spark rushes in; outside it drifts slowly toward the guardian and fades out after a while
                if (dist < pull) { float damp = Mathf.Exp(-3f * dt); p.vel = p.vel * damp + d / dist * 32f * dt; }
                else { p.vel *= Mathf.Exp(-1.6f * dt); p.vel += d / dist * 0.8f * dt; }
                p.pos += p.vel * dt; p.age += dt;
                p.tr.position = new Vector3(p.pos.x, p.pos.y, 0f);
                if (dist < 0.4f) { p.dead = true; Collect(p.pos); }
                else if (p.age > 20f) p.dead = true;
            }
            for (int i = sparks.Count - 1; i >= 0; i--) if (sparks[i].dead) { Recycle(sparks[i].tr.gameObject, sparkPool); sparks.RemoveAt(i); }

            // lightning from spark fireflies
            if (kindLevel[(int)Kind.Spark] > 0) { sparkTimer -= dt; if (sparkTimer <= 0f) { sparkTimer = 1.4f; Lightning(); } }

            // effects
            for (int i = bursts.Count - 1; i >= 0; i--)
            {
                var b = bursts[i]; b.t += dt; float q = b.t / 0.45f;
                if (q >= 1f) { Recycle(b.tr.gameObject, burstPool); bursts.RemoveAt(i); continue; }
                b.tr.localScale = Vector3.one * (0.6f + 2.6f * q); b.sr.color = new Color(1f, 0.5f, 0.2f, 1f - q);
            }
            for (int i = arcs.Count - 1; i >= 0; i--)
            {
                var a = arcs[i]; a.t += dt; float q = a.t / 0.28f;
                if (q >= 1f) { Recycle(a.lr.gameObject, arcPool); arcs.RemoveAt(i); continue; }
                var c = new Color(0.86f, 0.8f, 1f, 1f - q); a.lr.startColor = c; a.lr.endColor = c;
            }

            if (t >= 120f && !tide2) { tide2 = true; Tide(); }
            if (t >= 240f && !tide4) { tide4 = true; Tide(); }
            if (t >= nightLength) { End("Dawn"); return; }

            hud.Set(t, fireflies.Count);
        }

        void Collect(Vector2 at)
        {
            xp++; totalLight++;
            AddFirefly(at, PickKind());
            if (xp >= xpNext)
            {
                xp -= xpNext; level++; xpNext = Mathf.FloorToInt(xpNext * 1.3f + 4f);
                paused = true;
                hud.ShowCards(level, PickCards(), Apply);
            }
            hud.SetLevel(level, (float)xp / xpNext);
        }

        void KillShadow(Shadow s)
        {
            s.dead = true; kills++;
            int drops = s.r > 0.6f ? 5 : 1;
            for (int e = 0; e < drops; e++) SpawnSpark(s.pos);
            if (s.ember)
            {
                SpawnBurst(s.pos, 1.55f);
                foreach (var o in shadows) if (!o.dead && o != s && (o.pos - s.pos).magnitude < 1.55f) { o.hp -= 3.5f; if (o.hp <= 0f) KillShadow(o); }
            }
        }

        void Lightning()
        {
            var casters = new List<Firefly>(); foreach (var f in fireflies) if (f.kind == Kind.Spark) casters.Add(f);
            if (casters.Count == 0) return;
            int lv = kindLevel[(int)Kind.Spark];
            for (int i = 0; i < Mathf.Min(lv, 3); i++)
            {
                var f = casters[Random.Range(0, casters.Count)];
                var a = Nearest(f.pos, 3.75f, null); if (a == null) continue;
                var pts = new List<Vector2> { f.pos, a.pos }; a.hp -= 2.5f + lv * 0.8f;
                var b = Nearest(a.pos, 2.5f, a); if (b != null) { pts.Add(b.pos); b.hp -= 1.5f + lv * 0.5f; }
                SpawnArc(pts);
            }
        }

        Shadow Nearest(Vector2 from, float radius, Shadow skip)
        {
            Shadow best = null; float bd = radius * radius;
            foreach (var s in shadows) { if (s == skip || s.dead) continue; float d = (s.pos - from).sqrMagnitude; if (d < bd) { bd = d; best = s; } }
            return best;
        }

        // ---------- level-up cards ----------
        List<Hud.Card> PickCards()
        {
            var pool = new List<Hud.Card>();
            string[] kindTitle = { "", "Ember", "Frost", "Spark" };
            string[] kindDesc = { "", "Fire fireflies. A shadow they burn bursts and scorches the shadows around it.", "Ice fireflies. Every shadow they touch slows to half speed.", "Lightning to the nearest shadow that jumps to the next one." };
            for (int k = 1; k < 4; k++) if (kindLevel[k] < 3) pool.Add(new Hud.Card { id = "kind" + k, title = kindTitle[k] + (kindLevel[k] > 0 ? $"  {kindLevel[k] + 1}/3" : ""), desc = kindDesc[k] });
            foreach (Formation f in System.Enum.GetValues(typeof(Formation)))
                if (f != formation) pool.Add(new Hud.Card { id = "form" + (int)f, title = "Formation · " + f, desc = f switch { Formation.Spear => "The swarm sharpens along your heading. Focused damage.", Formation.Cloud => "A scattered swarm. Harder to snuff out, softer hits.", Formation.Spiral => "A turning spiral. Sparks fly in from farther away.", _ => "Even protection on every side." } });
            if (magnet < 2.5f) pool.Add(new Hud.Card { id = "magnet", title = "Beacon", desc = "Sparks fly in from 50% farther away." });
            if (swift < 1.6f) pool.Add(new Hud.Card { id = "swift", title = "Swiftness", desc = "The swarm moves 15% faster." });
            pool.Add(new Hud.Card { id = "brood", title = "Brood", desc = "+10 fireflies right now." });
            for (int i = pool.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (pool[i], pool[j]) = (pool[j], pool[i]); }
            return pool.GetRange(0, Mathf.Min(3, pool.Count));
        }

        void Apply(string id)
        {
            if (id.StartsWith("kind"))
            {
                int k = id[4] - '0'; kindLevel[k]++;
                int convert = Mathf.RoundToInt(fireflies.Count * (kindLevel[k] == 1 ? 0.25f : 0.15f));
                for (int i = 0; i < convert; i++) fireflies[Random.Range(0, fireflies.Count)].kind = (Kind)k;
            }
            else if (id.StartsWith("form")) formation = (Formation)(id[4] - '0');
            else if (id == "magnet") magnet *= 1.5f;
            else if (id == "swift") swift *= 1.15f;
            else if (id == "brood") for (int i = 0; i < 10; i++) AddFirefly(leaderPos + Random.insideUnitCircle * 1f, PickKind());
            paused = false;
        }

        // ---------- waves ----------
        void SpawnTick(float dt)
        {
            spawnTimer -= dt; if (spawnTimer > 0f) return;
            if (stress) { spawnTimer = 0.1f; for (int b = 0; b < 12 && shadows.Count < 300; b++) SpawnShadow(EdgePoint(0.9f), b % 5 == 0 ? "beast" : b % 3 == 0 ? "crawler" : "shade"); return; }
            float prog = Mathf.Min(1f, t / nightLength);
            spawnTimer = 1.25f - 0.95f * prog;
            if (shadows.Count > 230 && !stress) return;
            int batch = 1 + Mathf.FloorToInt(prog * 4f) + (Random.value < 0.35f ? 1 : 0);
            for (int b = 0; b < batch; b++)
            {
                float r = Random.value; string kind = "shade";
                if (t > 40f && r < 0.3f) kind = "crawler"; else if (t > 75f && r < 0.42f) kind = "beast";
                SpawnShadow(EdgePoint(0.9f), kind);
            }
        }

        void Tide()
        {
            float hh = cam.orthographicSize + 1f, hw = hh * cam.aspect; Vector2 c = cam.transform.position;
            int side = Random.Range(0, 4);
            for (int i = 0; i < 24; i++)
            {
                float u = (i + 0.5f) / 24f; Vector2 p, v;
                switch (side)
                {
                    case 0: p = new Vector2(c.x - hw + u * 2f * hw, c.y + hh); v = Vector2.down * 1.5f; break;
                    case 1: p = new Vector2(c.x - hw + u * 2f * hw, c.y - hh); v = Vector2.up * 1.5f; break;
                    case 2: p = new Vector2(c.x - hw, c.y - hh + u * 2f * hh); v = Vector2.right * 1.5f; break;
                    default: p = new Vector2(c.x + hw, c.y - hh + u * 2f * hh); v = Vector2.left * 1.5f; break;
                }
                SpawnShadow(p, "tide", v);
            }
            hud.Banner("The Tide");
            Invoke(nameof(ClearBanner), 1.8f);
        }

        Vector2 EdgePoint(float margin)
        {
            float hh = cam.orthographicSize + margin, hw = hh * cam.aspect; Vector2 c = cam.transform.position;
            int side = Random.Range(0, 4); float u = Random.value;
            switch (side)
            {
                case 0: return new Vector2(c.x - hw + u * 2f * hw, c.y + hh);
                case 1: return new Vector2(c.x - hw + u * 2f * hw, c.y - hh);
                case 2: return new Vector2(c.x - hw, c.y - hh + u * 2f * hh);
                default: return new Vector2(c.x + hw, c.y - hh + u * 2f * hh);
            }
        }

        void End(string title) { over = true; endTimer = 3f; hud.Banner($"{title}\nPeak swarm {peak} · Light {totalLight} · Shadows {kills}"); }
        void ClearBanner() { if (!over) hud.Banner(""); }

        void Restart()
        {
            foreach (var s in shadows) Recycle(s.tr.gameObject, shadowPool);
            foreach (var p in sparks) Recycle(p.tr.gameObject, sparkPool);
            foreach (var b in bursts) Recycle(b.tr.gameObject, burstPool);
            foreach (var a in arcs) Recycle(a.lr.gameObject, arcPool);
            foreach (var s in fireflies) Destroy(s.tr.gameObject);
            shadows.Clear(); sparks.Clear(); bursts.Clear(); arcs.Clear(); fireflies.Clear();
            t = 0f; spawnTimer = 1.5f; tide2 = tide4 = false; over = paused = false; leaderPos = leaderVel = Vector2.zero;
            xp = 0; xpNext = 10; level = 1; totalLight = kills = 0; magnet = swift = 1f; formation = Formation.Ring;
            for (int k = 0; k < 4; k++) kindLevel[k] = k == 0 ? 1 : 0;
            for (int i = 0; i < startFireflies; i++) AddFirefly(Random.insideUnitCircle * 1.5f, Kind.Wisp);
            peak = fireflies.Count; hud.Banner(""); hud.SetLevel(level, 0f);
        }

        static void Recycle(GameObject go, Stack<GameObject> pool) { go.SetActive(false); pool.Push(go); }
        static long Key(Vector2 p) => Key(Mathf.FloorToInt(p.x / Cell), Mathf.FloorToInt(p.y / Cell));
        static long Key(int cx, int cy) => ((long)cx << 32) ^ (uint)cy;
    }
}

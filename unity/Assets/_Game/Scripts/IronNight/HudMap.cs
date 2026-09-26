using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IronNight
{
    /// <summary>
    /// The war map on the screen: the map of Europe (a painted one when it is in Resources/UI/map_europe, a dark paper
    /// until then) to drag sideways, the two roads drawn sector to sector (gold where taken), the sectors as markers
    /// (taken, open to attack and pulsing, under counterattack in red, still ahead in grey), the operations as gold
    /// stars beside their battlefields, and the chosen sector's card with its attack button.
    /// </summary>
    public partial class Hud
    {
        public System.Action<string> OnMapSector;   // attack a sector: its night is launched
        GameObject mapSheet; Transform mapBody, mapCardRoot; ScrollRect mapScroll; string mapSelected; Text mapNote;
        readonly List<RectTransform> mapPulse = new List<RectTransform>();
        const float MapH = 1500f, ViewH = 1080f;   // the map drawn larger than its window: dragged both ways, the sectors apart
        static Sprite mapPlaceholder;
        static readonly Color MapRed = new Color(0.92f, 0.26f, 0.18f);

        /// <summary>The map picture: the painted map once it is in, a dark paper with a faint grid until then.</summary>
        static Sprite MapPicture()
        {
            var sp = UiSprite("map_europe"); if (sp != null) return sp;
            if (mapPlaceholder != null) return mapPlaceholder;
            const int W = 768, H = 512; var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear }; var px = new Color[W * H];
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.02f, y * 0.02f) * 0.05f + Mathf.PerlinNoise(x * 0.11f + 40f, y * 0.11f) * 0.02f;
                bool grid = x % 48 == 0 || y % 48 == 0; float u = x / (float)W - 0.5f, v = y / (float)H - 0.5f, vig = 1f - Mathf.Clamp01((u * u + v * v) * 1.6f);
                float k = (0.12f + n + (grid ? 0.03f : 0f)) * (0.55f + 0.45f * vig);
                px[y * W + x] = new Color(k * 1.18f, k * 1.1f, k * 0.9f, 1f);
            }
            tex.SetPixels(px); tex.Apply(); return mapPlaceholder = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>The war map, over the title; the chosen sector (or the one that needs the player) in view.</summary>
        public void ShowMap()
        {
            if (mapSheet == null)
            {
                mapSheet = new GameObject("WarMap", typeof(RectTransform), typeof(Image)); mapSheet.transform.SetParent(canvas.transform, false); Stretch(mapSheet); mapSheet.GetComponent<Image>().color = new Color(0.02f, 0.02f, 0.025f, 1f);
                var body = new GameObject("Body", typeof(RectTransform)); body.transform.SetParent(mapSheet.transform, false); Stretch(body); mapBody = body.transform;
            }
            if (curtain != null) mapSheet.transform.SetSiblingIndex(curtain.transform.GetSiblingIndex());   // under the curtain, over the title
            mapSheet.SetActive(true);
            string news = Campaign.Tick();
            var c = Campaign.Counter; mapSelected = c != null ? c.id : FirstOpen();
            BuildMap(news, true);
        }
        void CloseMap() { mapSheet.SetActive(false); mapPulse.Clear(); RefreshMapTile(); RefreshRewardButtons(); }
        static string FirstOpen() { foreach (var s in Campaign.All) if (Campaign.Open(s)) return s.id; return "berlin"; }

        void BuildMap(string news, bool scrollToSelected)
        {
            Vector2 keep = mapScroll != null ? mapScroll.content.anchoredPosition : Vector2.zero;
            foreach (Transform ch in mapBody) Destroy(ch.gameObject); mapPulse.Clear();
            var b = mapBody; var pic = MapPicture(); float mw = MapH * pic.rect.width / pic.rect.height, sx = mw / 1536f, sy = MapH / 1024f;
            MakeGhost(b, "BACK", new Vector2(0f, 1f), new Vector2(130, -95), new Vector2(200, 80), 26, CloseMap);
            MakeGhost(b, "OPERATIONS", new Vector2(1f, 1f), new Vector2(-170, -95), new Vector2(280, 80), 24, () => ShowOperations());
            var ey = MakeText(b, "Eyebrow", new Vector2(0.5f, 1f), new Vector2(0, -150), TextAnchor.MiddleCenter, 26, OpAmber); ey.text = Spaced("THE ROAD TO BERLIN"); ey.font = BoldFont(); ey.rectTransform.sizeDelta = new Vector2(900, 40);
            var ti = MakeText(b, "Title", new Vector2(0.5f, 1f), new Vector2(0, -186), TextAnchor.MiddleCenter, 100, OpInk); ti.text = Campaign.Victory ? "VICTORY" : "WAR MAP"; ti.font = DisplayFont(); ti.rectTransform.sizeDelta = new Vector2(900, 130); ti.verticalOverflow = VerticalWrapMode.Overflow;
            var st = MakeText(b, "State", new Vector2(0.5f, 1f), new Vector2(0, -300), TextAnchor.MiddleCenter, 24, OpDim); st.rectTransform.sizeDelta = new Vector2(1000, 36);
            var counter = Campaign.Counter;
            st.text = "West " + Campaign.TakenOn("west") + " of " + Campaign.WestCount + " · East " + Campaign.TakenOn("east") + " of " + Campaign.EastCount + (Campaign.Victory ? " · Berlin taken" : counter != null ? " · counterattack at " + counter.name + ", " + Campaign.CounterLeft + " left" : "");
            if (counter != null) st.color = MapRed;

            // the map to drag sideways
            var vp = new GameObject("Map", typeof(RectTransform), typeof(RectMask2D), typeof(Image), typeof(ScrollRect)); vp.transform.SetParent(b, false);
            var vr = vp.GetComponent<RectTransform>(); vr.anchorMin = vr.anchorMax = vr.pivot = new Vector2(0.5f, 1f); vr.anchoredPosition = new Vector2(0f, -345f); vr.sizeDelta = new Vector2(1040f, ViewH);
            vp.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
            var content = new GameObject("Paper", typeof(RectTransform), typeof(Image)); content.transform.SetParent(vp.transform, false); var cr = content.GetComponent<RectTransform>();
            cr.anchorMin = cr.anchorMax = cr.pivot = new Vector2(0f, 1f); cr.sizeDelta = new Vector2(mw, MapH); var paper = content.GetComponent<Image>(); paper.sprite = pic; paper.raycastTarget = true;
            var sr = vp.GetComponent<ScrollRect>(); sr.content = cr; sr.viewport = vr; sr.horizontal = true; sr.vertical = true; sr.movementType = ScrollRect.MovementType.Clamped; sr.inertia = true; sr.decelerationRate = 0.1f; sr.scrollSensitivity = 60f; mapScroll = sr;
            var ct = content.transform;
            System.Func<Campaign.Sector, Vector2> at = s => new Vector2(s.x * sx, -s.y * sy);
            // the roads, under the markers
            foreach (var s in Campaign.All) foreach (var a in s.after)
            {
                var p = Campaign.ById(a); if (p == null) continue; bool done = Campaign.Taken(p) && Campaign.Taken(s);
                Vector2 from = at(p), to = at(s), d = to - from;
                var ln = MakeImage(ct, "Road", new Vector2(0f, 1f), from, new Vector2(d.magnitude, done ? 7f : 4f), done ? new Color(0.98f, 0.74f, 0.32f, 0.95f) : new Color(1f, 0.95f, 0.85f, 0.28f)); ln.rectTransform.pivot = new Vector2(0f, 0.5f);
                ln.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            }
            // the operations, as gold stars beside their battlefields
            OpStar(ct, "cobra", at(Campaign.ById("omaha")) + new Vector2(8f, 78f));
            OpStar(ct, "bastogne", at(Campaign.ById("bastogne")) + new Vector2(52f, -50f));
            OpStar(ct, "prokhorovka", at(Campaign.ById("prokhorovka")) + new Vector2(-58f, 12f));
            // the sectors
            foreach (var s in Campaign.All) SectorMarker(ct, s, at(s), counter);
            var sel = Campaign.ById(mapSelected) ?? Campaign.ById(FirstOpen());
            Vector2 want = scrollToSelected && sel != null ? new Vector2(-(at(sel).x - 520f), -at(sel).y - ViewH * 0.5f) : keep;
            cr.anchoredPosition = new Vector2(Mathf.Clamp(want.x, -(mw - 1040f), 0f), Mathf.Clamp(want.y, 0f, MapH - ViewH));
            mapNote = MakeText(b, "News", new Vector2(0.5f, 1f), new Vector2(0, -1440), TextAnchor.MiddleCenter, 26, MapRed); mapNote.font = BoldFont(); mapNote.rectTransform.sizeDelta = new Vector2(1000, 40); mapNote.text = news ?? "";
            var cardRoot = new GameObject("Card", typeof(RectTransform)); cardRoot.transform.SetParent(b, false); var cardRt = cardRoot.GetComponent<RectTransform>(); cardRt.anchorMin = cardRt.anchorMax = cardRt.pivot = new Vector2(0.5f, 1f); cardRt.anchoredPosition = new Vector2(0f, -1480f); cardRt.sizeDelta = new Vector2(1000f, 380f); mapCardRoot = cardRoot.transform;
            SectorCard(sel);
        }

        void OpStar(Transform parent, string opId, Vector2 pos)
        {
            var op = Operations.ById(opId); if (op == null) return;
            var b = MakeButton(parent, "", new Vector2(0f, 1f), pos, new Vector2(56, 56), 20, () => ShowOperation(op)); b.GetComponent<Image>().color = new Color(0.08f, 0.07f, 0.05f, 0.85f); b.transform.Find("Label").gameObject.SetActive(false);
            var sti = MakeImage(b.transform, "Star", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40, 40), GoldInk); sti.sprite = StarSprite(); sti.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var tl = MakeText(b.transform, "Op", new Vector2(0.5f, 0f), new Vector2(0, -22), TextAnchor.UpperCenter, 16, GoldInk); tl.text = Spaced("OP"); tl.font = BoldFont(); tl.rectTransform.sizeDelta = new Vector2(80, 22); tl.rectTransform.pivot = new Vector2(0.5f, 1f);
        }
        /// <summary>The operations sheet opened on one operation's nights.</summary>
        void ShowOperation(Operations.Op op) { ShowOperations(); OpNights(op); }

        /// <summary>A sector's marker: taken in gold, open to attack with a pulsing ring, under counterattack in red, ahead in grey.</summary>
        void SectorMarker(Transform parent, Campaign.Sector s, Vector2 pos, Campaign.Sector counter)
        {
            bool taken = Campaign.Taken(s), open = Campaign.Open(s), hit = counter == s, big = s.id == "berlin", chosen = mapSelected == s.id; float size = big ? 76f : 54f;
            Color col = hit ? MapRed : taken ? GoldInk : open ? OpAmber : new Color(0.3f, 0.3f, 0.33f);
            if (open || hit)
            {
                var ring = MakeImage(parent, "Pulse", new Vector2(0f, 1f), pos, new Vector2(size * 1.9f, size * 1.9f), new Color(col.r, col.g, col.b, 0.5f)); ring.sprite = Lightswarm.ProceduralSprites.Ring(64, 0.08f); ring.rectTransform.pivot = new Vector2(0.5f, 0.5f); mapPulse.Add(ring.rectTransform);
            }
            if (chosen) { var sel = MakeImage(parent, "Chosen", new Vector2(0f, 1f), pos, new Vector2(size * 1.45f, size * 1.45f), Color.white); sel.sprite = Lightswarm.ProceduralSprites.Ring(64, 0.06f); sel.rectTransform.pivot = new Vector2(0.5f, 0.5f); }
            var b = MakeButton(parent, "", new Vector2(0f, 1f), pos, new Vector2(size, size), 20, () => { mapSelected = s.id; BuildMap(mapNote != null ? mapNote.text : null, false); });
            var bi = b.GetComponent<Image>(); bi.sprite = Lightswarm.ProceduralSprites.Glow(64, 0.9f); bi.type = Image.Type.Simple; bi.color = col; b.transform.Find("Label").gameObject.SetActive(false);
            var core = MakeImage(b.transform, "Core", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.5f, size * 0.5f), taken && !hit ? new Color(0.14f, 0.12f, 0.08f) : new Color(1f, 1f, 1f, open || hit ? 0.95f : 0.35f)); core.sprite = big ? StarSprite() : Lightswarm.ProceduralSprites.Glow(64, 0.9f); core.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var nm = MakeText(parent, "Name", new Vector2(0f, 1f), pos + new Vector2(0f, -size * 0.5f - 4f), TextAnchor.UpperCenter, big ? 26 : 20, taken ? new Color(0.98f, 0.9f, 0.7f) : open || hit ? OpAmber : new Color(0.75f, 0.73f, 0.68f)); nm.text = s.name; nm.font = BoldFont(); nm.rectTransform.sizeDelta = new Vector2(220, 30); nm.rectTransform.pivot = new Vector2(0.5f, 1f);
            var sh = nm.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(0f, 0f, 0f, 0.9f); sh.effectDistance = new Vector2(0f, -2f);
        }

        /// <summary>The chosen sector: its name and date, how the night will be, what it pays, and what can be done now.</summary>
        void SectorCard(Campaign.Sector s)
        {
            foreach (Transform ch in mapCardRoot) Destroy(ch.gameObject);
            if (s == null) return;
            var card = MakeCard(mapCardRoot, "Sector", new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1000f, 380f), new Color(0.07f, 0.07f, 0.08f, 0.97f), 0.2f); card.rectTransform.pivot = new Vector2(0.5f, 1f); var t = card.transform;
            bool taken = Campaign.Taken(s), open = Campaign.Open(s), hit = Campaign.Counter == s;
            if (hit || open) card.transform.Find("Edge").GetComponent<Image>().color = hit ? new Color(MapRed.r, MapRed.g, MapRed.b, 0.9f) : new Color(1f, 0.78f, 0.35f, 0.8f);
            int total = s.road == "west" ? Campaign.WestCount : Campaign.EastCount;
            var ey = MakeText(t, "Eyebrow", new Vector2(0f, 1f), new Vector2(36, -26), TextAnchor.UpperLeft, 22, OpAmber); ey.font = BoldFont(); ey.rectTransform.sizeDelta = new Vector2(900, 32);
            ey.text = Spaced(s.road == "both" ? "WHERE THE ROADS MEET" : (s.road == "west" ? "WESTERN ROAD" : "EASTERN ROAD") + " · SECTOR " + (s.depth + 1) + " OF " + total);
            var nm = MakeText(t, "Name", new Vector2(0f, 1f), new Vector2(34, -58), TextAnchor.UpperLeft, 76, OpInk); nm.text = s.name.ToUpperInvariant(); nm.font = DisplayFont(); nm.rectTransform.sizeDelta = new Vector2(920, 96); nm.verticalOverflow = VerticalWrapMode.Overflow;
            string way = s.route == "village" ? "village" : s.route == "bocage" ? (s.theatre == "kursk" ? "tree belts" : "bocage") : (s.theatre == "kursk" ? "steppe" : "open fields");
            var ln = MakeText(t, "Line", new Vector2(0f, 1f), new Vector2(36, -160), TextAnchor.UpperLeft, 24, OpDim); ln.rectTransform.sizeDelta = new Vector2(930, 36);
            ln.text = s.when.Substring(0, 1) + s.when.Substring(1).ToLowerInvariant() + " · " + way + " · " + s.weather + (s.theatre == "ardennes" ? " · snow" : "") + (s.veteran ? " · veteran enemy" : "") + (s.road == "east" || s.id == "berlin" ? " · Soviet tanks" : " · American tanks");
            var pay = MakeText(t, "Pay", new Vector2(0f, 1f), new Vector2(36, -200), TextAnchor.UpperLeft, 24, GoldInk); pay.rectTransform.sizeDelta = new Vector2(930, 36); pay.text = taken && !hit ? "Liberated" : hit ? "Beat it off: +800 points · a supply crate · +1 star" : Campaign.Pay(s);
            Vector2 btnPos = new Vector2(0f, 62f), btnSize = new Vector2(520f, 100f);
            if (hit)
            {
                MakePrimary(t, "Defend " + s.name, new Vector2(0.5f, 0f), btnPos, btnSize, 32, () => OnMapSector?.Invoke(s.id)).GetComponent<Image>().color = MapRed;
                var lf = MakeText(t, "Left", new Vector2(1f, 0f), new Vector2(-36, 100), TextAnchor.MiddleRight, 22, MapRed); lf.text = Campaign.CounterLeft + " left"; lf.font = BoldFont(); lf.rectTransform.sizeDelta = new Vector2(220, 30);
            }
            else if (open) MakePrimary(t, "Attack", new Vector2(0.5f, 0f), btnPos, btnSize, 38, () => OnMapSector?.Invoke(s.id));
            else
            {
                string why = taken ? "Held by the platoon" : !Campaign.RoadOpen(s) ? "Hold a night until dawn to open the Eastern Front" : "Take " + NamesBefore(s) + " first";
                var w = MakeText(t, "Why", new Vector2(0.5f, 0f), btnPos, TextAnchor.MiddleCenter, 28, taken ? new Color(0.45f, 0.85f, 0.5f) : OpDim); w.text = why; w.font = BoldFont(); w.rectTransform.sizeDelta = new Vector2(930, 60); w.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            }
        }
        static string NamesBefore(Campaign.Sector s)
        {
            var names = new List<string>(); foreach (var a in s.after) { var p = Campaign.ById(a); if (p != null && !Campaign.Taken(p)) names.Add(p.name); }
            return string.Join(" and ", names);
        }

        /// <summary>The first tile of the title: the war map, how far along, a counterattack in red.</summary>
        void RefreshMapTile()
        {
            if (opsCount == null) return;
            var c = Campaign.Counter;
            opsCount.text = Campaign.Victory ? "victory in Europe" : c != null ? "counterattack · " + c.name.ToLowerInvariant() : "west " + Campaign.TakenOn("west") + "/" + Campaign.WestCount + " · east " + Campaign.TakenOn("east") + "/" + Campaign.EastCount;
            opsCount.color = c != null ? MapRed : OpAmber;
        }

        /// <summary>The markers open to attack breathe; from the Hud's Update.</summary>
        void TickMap()
        {
            if (mapSheet == null || !mapSheet.activeSelf) return;
            float k = 0.85f + 0.25f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3.2f));
            foreach (var r in mapPulse) if (r != null) r.localScale = new Vector3(k, k, 1f);
        }
    }
}

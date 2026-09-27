using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace IronNight
{
    /// <summary>
    /// The self-test of the desktop build: --qa=seconds plays the game by itself and counts every error in the log.
    /// An autopilot drives the leader through the nights (circling the nearest enemy at gun range, making for the
    /// objective, turning back out of whatever it sticks on) and now and then taps an enemy or a button of the HUD;
    /// between the nights a monkey taps whatever a player could tap, at random: every sheet, tab, card, offer and back
    /// button, and scrolls the lists. Pictures of the screen go to --qashots=folder; at the end every distinct error,
    /// with how often it came and its first stack, goes to the log and the game quits. --qaseed=n repeats a run. The
    /// sound is off and nothing is opened outside the game.
    /// </summary>
    public partial class Battle
    {
        public static bool QaOn { get; private set; }

        /// <summary>From the bootstrap on every scene load: the runner is made once and outlives the reloads.</summary>
        public static void QaStart()
        {
            if (QaOn || Application.isMobilePlatform) return;
            float secs = 0f; string shots = null; int seed = 1; var inv = System.Globalization.CultureInfo.InvariantCulture;
            foreach (var a in System.Environment.GetCommandLineArgs())
            {
                if (a.StartsWith("--qa=")) float.TryParse(a.Substring(5), System.Globalization.NumberStyles.Float, inv, out secs);
                else if (a.StartsWith("--qashots=")) shots = a.Substring(10);
                else if (a.StartsWith("--qaseed=")) int.TryParse(a.Substring(9), out seed);
            }
            if (secs <= 0f) return;
            QaOn = true; Random.InitState(seed);
            var go = new GameObject("QA"); DontDestroyOnLoad(go);
            var r = go.AddComponent<QaRunner>(); r.left = secs; r.shots = shots; r.rng = new System.Random(seed); runner = r;
            Debug.Log("QA: self-test for " + secs + " s, seed " + seed + (shots != null ? ", pictures to " + shots : ""));
        }

        static QaRunner runner;
        /// <summary>Something worth a picture just happened at a place (a hull through a hedge): half a second later a
        /// picture of the screen, named with where the leader is on it, and a close one of the place from a camera of its own.</summary>
        public static void QaNote(string what, Vector3 at) { if (runner != null) runner.Note(what, at); }

        class QaRunner : MonoBehaviour
        {
            public float left; public string shots; public System.Random rng;
            Battle b; Phase lastPhase = (Phase)(-1);
            int errors, taps, menuTaps, shotN, nights; float nextTap = 2f, nextShot = 2f, nextPlayTap = 6f, shotIn = -1f, stuckT, flip = 1f, wanderT; string shotLabel;
            Vector3 lastPos, wander = Vector3.forward;
            readonly Dictionary<string, int> seen = new Dictionary<string, int>(); readonly List<string> order = new List<string>(); readonly Dictionary<string, string> stacks = new Dictionary<string, string>();
            static readonly string[] WaysOut = { "BACK", "CLOSE", "LATER", "DONE" };   // taken after a few taps on the same screen
            string screen; int onScreen;
            static readonly string[] WaysIn = { "TOBATTLE", "INTOTHENIGHT", "ATTACK", "DEFEND" };   // the buttons into a night (spaces out, as some are letter-spaced): held back until the menus have had their share

            int notes; float closeIn = -1f; Vector3 closeAt, closeFrom; string closeLabel;
            public void Note(string what, Vector3 at)
            {
                if (notes >= 16 || shotIn > 0f || closeIn > 0f || b == null || b.Leader == null) return;
                notes++; var sp = b.cam.WorldToScreenPoint(b.Leader.transform.position);
                shotIn = 0.55f; shotLabel = what + "-x" + Mathf.RoundToInt(sp.x) + "-y" + Mathf.RoundToInt(Screen.height - sp.y);
                closeIn = 0.6f; closeAt = at; closeFrom = b.Leader.Forward; closeLabel = what + "-close";
                Debug.Log("QA note: " + shotLabel);
            }

            /// <summary>A picture of a place from a camera of its own: ten metres back along the way the leader came,
            /// a little to the side and five up, rendered straight to a file.</summary>
            void Close()
            {
                if (string.IsNullOrEmpty(shots) || b == null || b.cam == null) return;
                var go = new GameObject("QA close"); var c = go.AddComponent<Camera>(); c.CopyFrom(b.cam); c.enabled = false; c.fieldOfView = 50f;
                var f = closeFrom; f.y = 0f; if (f.sqrMagnitude < 0.01f) f = Vector3.forward; f.Normalize();
                go.transform.position = closeAt - f * 10f + Vector3.Cross(Vector3.up, f) * 4f + Vector3.up * 5f; go.transform.LookAt(closeAt + Vector3.up * 0.8f);
                var rt = new RenderTexture(960, 640, 24); c.targetTexture = rt; c.Render();
                var was = RenderTexture.active; RenderTexture.active = rt; var tex = new Texture2D(960, 640, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 960, 640), 0, 0); tex.Apply(); RenderTexture.active = was;
                System.IO.Directory.CreateDirectory(shots); System.IO.File.WriteAllBytes(System.IO.Path.Combine(shots, (++shotN).ToString("0000") + "_" + closeLabel + ".png"), tex.EncodeToPNG());
                c.targetTexture = null; Destroy(tex); rt.Release(); Destroy(rt); Destroy(go);
            }

            void OnEnable() { Application.logMessageReceived += OnLog; }
            void OnDisable() { Application.logMessageReceived -= OnLog; }

            void OnLog(string msg, string stack, LogType type)
            {
                if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
                errors++; var key = msg.Length > 240 ? msg.Substring(0, 240) : msg;
                if (seen.ContainsKey(key)) { seen[key]++; return; }
                seen[key] = 1; order.Add(key); stacks[key] = stack; shotIn = 0.05f; shotLabel = "error";
            }

            void Update()
            {
                float dt = Time.unscaledDeltaTime; left -= dt;
                if (left <= 0f) { Finish(); return; }
                AudioListener.volume = 0f;   // the scene's sound setting is put back on every load
                if (shotIn > 0f) { shotIn -= dt; if (shotIn <= 0f) Shot(shotLabel); }
                if (closeIn > 0f) { closeIn -= dt; if (closeIn <= 0f) Close(); }
                if (b == null) b = FindAnyObjectByType<Battle>();
                if (b == null || b.hud == null || b.stick == null) return;
                if (b.phase != lastPhase)
                {
                    if (lastPhase == Phase.Play && b.phase == Phase.End) nights++;
                    if (b.phase == Phase.Title) menuTaps = 0;
                    lastPhase = b.phase; shotIn = 0.6f; shotLabel = b.phase.ToString().ToLowerInvariant();
                }
                nextShot -= dt; if (nextShot <= 0f) { nextShot = b.phase == Phase.Play ? 6f : 15f; Shot(b.phase.ToString().ToLowerInvariant()); }
                if (b.phase == Phase.Play) { Pilot(); return; }
                TouchStick.Pilot = Vector2.zero;
                nextTap -= dt; if (nextTap > 0f) return;
                nextTap = 0.5f + (float)rng.NextDouble() * 0.6f;
                if (rng.NextDouble() < 0.12) Scroll(); else Tap(false);
            }

            /// <summary>The leader driven by itself: round the nearest enemy at gun range, else to the objective, else a wander.</summary>
            void Pilot()
            {
                var L = b.Leader; if (L == null) { TouchStick.Pilot = Vector2.zero; return; }
                var pos = L.transform.position; Vehicle near = null; float nd = float.MaxValue;
                foreach (var e in b.foes) { if (e == null || e.dead) continue; float d = (e.transform.position - pos).sqrMagnitude; if (d < nd) { nd = d; near = e; } }
                nd = Mathf.Sqrt(nd); Vector3 want;
                if (near != null && nd < 75f)
                {
                    var n = near.transform.position - pos; n.y = 0f; n.Normalize();
                    want = new Vector3(n.z, 0f, -n.x) * flip + n * (nd < 20f ? -0.9f : nd > 36f ? 0.7f : 0.1f);
                }
                else if (b.objective != null) { want = b.objective.pos - pos; want.y = 0f; }
                else
                {
                    wanderT -= Time.deltaTime;
                    if (wanderT <= 0f) { wanderT = 5f + (float)rng.NextDouble() * 5f; float a = (float)rng.NextDouble() * Mathf.PI * 2f; wander = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)); }
                    want = wander;
                }
                // stuck on something: go round the other way for a while
                stuckT += Time.deltaTime;
                if (stuckT >= 2f) { if ((pos - lastPos).magnitude < 2f) { flip = -flip; wander = -wander; wanderT = 3f; } stuckT = 0f; lastPos = pos; }
                TouchStick.Pilot = want.sqrMagnitude > 0.001f ? new Vector2(want.x, want.z).normalized : Vector2.zero;
                nextPlayTap -= Time.deltaTime;
                if (nextPlayTap <= 0f)
                {
                    nextPlayTap = 4f + (float)rng.NextDouble() * 6f;
                    if (near != null && rng.NextDouble() < 0.5) b.stick.FakeTap(b.cam.WorldToScreenPoint(near.transform.position)); else Tap(true);
                }
            }

            /// <summary>Taps a button a player could tap now: enabled, on the screen, and the first thing under its middle.</summary>
            void Tap(bool inPlay)
            {
                var es = EventSystem.current; if (es == null) return;
                var live = new List<Button>(); Button wayIn = null, wayOut = null; var sig = new System.Text.StringBuilder();
                foreach (var btn in FindObjectsByType<Button>(FindObjectsSortMode.None))
                {
                    if (!btn.isActiveAndEnabled || !btn.interactable || !OnTop(btn, es)) continue;
                    string label = Label(btn);
                    if (Seen(btn) < 0.1f) { string where = Path(btn.transform); if (hidden.Add(where)) Debug.LogWarning("QA hidden: a button no one can see takes taps in " + b.phase + ": " + where + " [" + label + "]"); continue; }
                    if (inPlay && (btn.name.Contains("Pause") || label == "II") && rng.NextDouble() > 0.1) continue;   // a pause now and then, not every time
                    string bare = Bare(label); if (sig.Length < 200) sig.Append(bare).Append('|');
                    if (System.Array.IndexOf(WaysOut, bare) >= 0) wayOut = btn;
                    if (System.Array.Exists(WaysIn, w => bare.StartsWith(w))) { wayIn = btn; if (menuTaps < 25) continue; }
                    live.Add(btn);
                }
                if (live.Count == 0) return;
                string here = live.Count + ":" + sig; onScreen = here == screen ? onScreen + 1 : 0; screen = here;
                var pick = !inPlay && wayOut != null && onScreen >= 4 ? wayOut : wayIn != null && menuTaps >= 25 && rng.NextDouble() < 0.3 ? wayIn : live[rng.Next(live.Count)];
                taps++; if (!inPlay) menuTaps++;
                Debug.Log("QA tap " + taps + " (" + b.phase + "): " + Path(pick.transform) + " [" + Label(pick) + "]");
                var ped = new PointerEventData(es) { position = Middle(pick), button = PointerEventData.InputButton.Left };
                try { ExecuteEvents.Execute(pick.gameObject, ped, ExecuteEvents.pointerClickHandler); }
                catch (System.Exception ex) { Debug.LogException(ex); }
                if (taps % 2 == 0 && shotIn <= 0f) { shotIn = 0.45f; shotLabel = Clean(Label(pick)); }
            }

            void Scroll()
            {
                var live = new List<ScrollRect>(); foreach (var s in FindObjectsByType<ScrollRect>(FindObjectsSortMode.None)) if (s.isActiveAndEnabled) live.Add(s);
                if (live.Count == 0) return; var sr = live[rng.Next(live.Count)];
                sr.normalizedPosition = new Vector2((float)rng.NextDouble(), (float)rng.NextDouble());
                Debug.Log("QA scroll: " + Path(sr.transform));
            }

            static Vector2 Middle(Button btn)
            {
                var rt = (RectTransform)btn.transform; var c = new Vector3[4]; rt.GetWorldCorners(c);
                var canvas = btn.GetComponentInParent<Canvas>(); var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
                return RectTransformUtility.WorldToScreenPoint(cam, (c[0] + c[2]) * 0.5f);
            }
            static bool OnTop(Button btn, EventSystem es)
            {
                var at = Middle(btn); if (at.x < 1f || at.y < 1f || at.x > Screen.width - 1f || at.y > Screen.height - 1f) return false;
                var hits = new List<RaycastResult>(); es.RaycastAll(new PointerEventData(es) { position = at }, hits);
                return hits.Count > 0 && (hits[0].gameObject == btn.gameObject || hits[0].gameObject.transform.IsChildOf(btn.transform));
            }
            readonly HashSet<string> hidden = new HashSet<string>();
            /// <summary>How much of a button can be seen: the clearest of its graphics (a label on a clear ground counts)
            /// times every fading group above it.</summary>
            static float Seen(Button btn)
            {
                float a = 0f; foreach (var g in btn.GetComponentsInChildren<Graphic>()) if (g.enabled) a = Mathf.Max(a, g.color.a);
                for (var t = btn.transform; t != null; t = t.parent) { var g = t.GetComponent<CanvasGroup>(); if (g != null) a *= g.alpha; }
                return a;
            }
            static string Label(Button btn) { var t = btn.GetComponentInChildren<Text>(); return t != null && !string.IsNullOrEmpty(t.text) ? t.text.Replace("\n", " ") : btn.name; }
            static string Path(Transform t) { var s = t.name; for (var p = t.parent; p != null && p.parent != null; p = p.parent) s = p.name + "/" + s; return s; }
            static string Bare(string s) { var sb = new System.Text.StringBuilder(); foreach (var ch in s) if (!char.IsWhiteSpace(ch)) sb.Append(char.ToUpperInvariant(ch)); return sb.ToString(); }
            static string Clean(string s) { s = Bare(s); var sb = new System.Text.StringBuilder(); foreach (var ch in s) if (char.IsLetterOrDigit(ch)) sb.Append(char.ToLowerInvariant(ch)); else if (sb.Length > 0 && sb[sb.Length - 1] != '-') sb.Append('-'); var r = sb.ToString().Trim('-'); return r.Length > 32 ? r.Substring(0, 32) : r; }

            void Shot(string label)
            {
                if (string.IsNullOrEmpty(shots) || shotN >= 900) return;
                System.IO.Directory.CreateDirectory(shots);
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(shots, (++shotN).ToString("0000") + "_" + (string.IsNullOrEmpty(label) ? "shot" : label) + ".png"));
            }

            void Finish()
            {
                enabled = false; TouchStick.Pilot = null;
                var sb = new System.Text.StringBuilder();
                sb.Append("QA DONE: ").Append(errors).Append(" errors, ").Append(order.Count).Append(" distinct, ").Append(hidden.Count).Append(" hidden buttons, ").Append(taps).Append(" taps, ").Append(nights).Append(" nights, ").Append(shotN).Append(" pictures");
                foreach (var k in order) sb.Append("\n--- x").Append(seen[k]).Append(": ").Append(k).Append('\n').Append(stacks[k]);
                foreach (var h in hidden) sb.Append("\n--- hidden: ").Append(h);
                Debug.Log(sb.ToString());
                Application.Quit();
            }
        }
    }
}

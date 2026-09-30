using UnityEngine;
using UnityEngine.InputSystem;

namespace IronNight
{
    /// <summary>
    /// The phone in the hand: short taps, each its own, where there was one long buzz. A tick for a button and for each
    /// peg of the dispatch wheel, a click for the leader's own shot, a heavy click when he is hit, a double for a tank
    /// killed, a thump and a roll for a heavy tank or an ace going up and for a blast close by, a rising three for a
    /// prize. Android's predefined effects (Android 10 and newer) where the phone has them, else one-shots and waveforms
    /// with their strength; nothing on a computer. The setting "vibe" turns it all off.
    /// </summary>
    public static class Haptics
    {
        public static bool On => PlayerPrefs.GetInt("vibe", 1) == 1;
        public static void Tick() => Play(2, 12, 70);        // EFFECT_TICK
        public static void Click() => Play(0, 22, 150);      // EFFECT_CLICK
        public static void Heavy() => Play(5, 45, 255);      // EFFECT_HEAVY_CLICK
        public static void Double() => Play(1, 60, 200);     // EFFECT_DOUBLE_CLICK
        public static void Boom() => Wave(new long[] { 0, 70, 30, 130 }, new[] { 0, 255, 0, 110 });
        public static void Prize() => Wave(new long[] { 0, 25, 70, 25, 70, 70 }, new[] { 0, 90, 0, 160, 0, 255 });

#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject vibrator; static AndroidJavaClass effect; static int sdk; static bool strength, failed;

        static bool Ready()
        {
            if (failed) return false; if (vibrator != null) return true;
            try
            {
                using (var v = new AndroidJavaClass("android.os.Build$VERSION")) sdk = v.GetStatic<int>("SDK_INT");
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                    vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                if (vibrator == null || !vibrator.Call<bool>("hasVibrator")) { failed = true; return false; }
                effect = new AndroidJavaClass("android.os.VibrationEffect"); strength = vibrator.Call<bool>("hasAmplitudeControl");
                return true;
            }
            catch (System.Exception) { failed = true; return false; }
        }

        static void Vibrate(AndroidJavaObject e) { using (e) vibrator.Call("vibrate", e); }
#endif

        /// <summary>A predefined effect where the phone has them, else a one-shot of that length and strength (0-255).</summary>
        static void Play(int predefined, int ms, int power)
        {
            if (!On) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Ready()) return;
            try
            {
                if (sdk >= 29) Vibrate(effect.CallStatic<AndroidJavaObject>("createPredefined", predefined));
                else Vibrate(effect.CallStatic<AndroidJavaObject>("createOneShot", (long)ms, strength ? power : -1));
            }
            catch (System.Exception) { failed = true; Handheld.Vibrate(); }   // once, the old buzz; after that the phone is left alone
#endif
        }

        /// <summary>A pattern: pauses and pulses in milliseconds, each pulse with its strength; on/off where the phone has no strengths.</summary>
        static void Wave(long[] ms, int[] power)
        {
            if (!On) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Ready()) return;
            try { Vibrate(strength ? effect.CallStatic<AndroidJavaObject>("createWaveform", ms, power, -1) : effect.CallStatic<AndroidJavaObject>("createWaveform", ms, -1)); }
            catch (System.Exception) { failed = true; Handheld.Vibrate(); }
#endif
        }
    }

    /// <summary>
    /// The phone's tilt for the menus' parallax: gravity (or the accelerometer) against the way the phone has been held
    /// for the last few seconds, so any way of holding it is level and only a turn of the wrist moves the picture. On a
    /// computer the mouse stands in, more gently. -1..1 each way, smoothed. The sensors are on only while a menu asks.
    /// </summary>
    public static class Parallax
    {
        static Vector3 rest; static bool resting; static Vector2 tilt; static int frame = -1;

        public static Vector2 Tilt { get { Tick(); return tilt; } }

        static void Tick()
        {
            if (frame == Time.frameCount) return; frame = Time.frameCount;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f); var want = Vector2.zero;
            var grav = GravitySensor.current; var acc = grav == null ? Accelerometer.current : null;
            if (grav != null && !grav.enabled) InputSystem.EnableDevice(grav);
            if (acc != null && !acc.enabled) InputSystem.EnableDevice(acc);
            var g = grav != null ? grav.gravity.ReadValue() : acc != null ? acc.acceleration.ReadValue() : Vector3.zero;
            if (g.sqrMagnitude > 0.01f)
            {
                g.Normalize();
                if (!resting) { rest = g; resting = true; }
                rest = Vector3.Slerp(rest, g, 1f - Mathf.Exp(-dt / 2.5f));   // the hold drifts back to level over a few seconds
                var d = g - rest; want = new Vector2(Mathf.Clamp(d.x / 0.22f, -1f, 1f), Mathf.Clamp(d.y / 0.22f, -1f, 1f));
            }
            else if (Mouse.current != null && Application.isFocused)
            {
                var m = Mouse.current.position.ReadValue();
                want = new Vector2(Mathf.Clamp01(m.x / Mathf.Max(1f, Screen.width)) * 2f - 1f, Mathf.Clamp01(m.y / Mathf.Max(1f, Screen.height)) * 2f - 1f) * 0.6f;
            }
            tilt = Vector2.Lerp(tilt, want, 1f - Mathf.Exp(-dt * 5f));
        }

        /// <summary>The menus are gone: the sensors off, the tilt back to level.</summary>
        public static void Sleep()
        {
            if (GravitySensor.current != null && GravitySensor.current.enabled) InputSystem.DisableDevice(GravitySensor.current);
            if (Accelerometer.current != null && Accelerometer.current.enabled) InputSystem.DisableDevice(Accelerometer.current);
            resting = false; tilt = Vector2.zero;
        }
    }
}

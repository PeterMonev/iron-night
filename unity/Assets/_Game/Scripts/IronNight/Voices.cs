using System.Collections.Generic;
using UnityEngine;

namespace IronNight
{
    /// <summary>
    /// The voices, spoken ahead of time by Kokoro and put through a radio (art/pipeline/voices.py) into
    /// Resources/Audio/Voice: the commanders on the radio (radio_&lt;commander&gt;_&lt;when&gt;), a spotter calling a target
    /// and then its bearing (spot_&lt;nation&gt;_&lt;what&gt;, spot_&lt;nation&gt;_clock&lt;1-12&gt;) and the adjutant in the menu
    /// (adj_&lt;nation&gt;_&lt;kind&gt;_&lt;rank&gt;). One voice speaks at a time: what comes while one is speaking waits its turn,
    /// three clips at most, the rest is dropped (the words are on the screen anyway). A missing clip is silence.
    /// </summary>
    public static class Voices
    {
        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        static AudioClip Clip(string id) { if (!clips.TryGetValue(id, out var c)) clips[id] = c = Resources.Load<AudioClip>("Audio/Voice/" + id); return c; }

        /// <summary>A commander's line on the radio: start, kill0, kill1, hit, tiger or dawn.</summary>
        public static void Commander(string commander, string when) => Sfx.Voice(Clip("radio_" + commander + "_" + when));

        /// <summary>The spotter calling a target (tiger, panther, guns, eightyeight, flak, infantry, halftrack, column,
        /// keil, ace, observer, mines), then where it is by the clock, in the platoon's own nation's voice.</summary>
        public static void Callout(string what, int hour) { string n = Depot.Nation == "su" ? "su" : "us"; Sfx.Voice(Clip("spot_" + n + "_" + what), Clip("spot_" + n + "_clock" + Mathf.Clamp(hour, 1, 12))); }

        /// <summary>The adjutant: morning, afternoon, evening, night or congrats, for the rank he holds.</summary>
        public static void Adjutant(string nation, string kind) => Sfx.Voice(Clip("adj_" + (nation == "su" ? "su" : "us") + "_" + kind + "_" + Mathf.Clamp(Depot.RankIndex, 0, 11)));
    }
}

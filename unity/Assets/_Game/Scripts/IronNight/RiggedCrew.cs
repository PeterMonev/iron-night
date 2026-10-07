using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace IronNight
{
    /// <summary>
    /// A crew figure rigged in Mixamo (Resources/Rigs/rig_*, a Humanoid) playing real motion-captured motions
    /// (Resources/Rigs/Anims/anim_*): a few of them in turn, each held eight to sixteen seconds and blended into the next
    /// over a second, or one chosen from outside (Play) - a walker's walk and his pause. Played through a Playable graph,
    /// so no animator controller is needed.
    /// </summary>
    public class RiggedCrew : MonoBehaviour
    {
        PlayableGraph graph; AnimationMixerPlayable mixer; float[] weight; int now; float next; bool cycling = true;
        public Animator Animator { get; private set; }

        /// <summary>The rig named (rig_us_gunner and the like) with these motions (smoking, talking ...); null when the rig
        /// or every motion is missing.</summary>
        public static RiggedCrew Make(Transform parent, string rig, string[] motions)
        {
            var pf = Resources.Load<GameObject>("Rigs/" + rig); if (pf == null) return null;
            var clips = new System.Collections.Generic.List<AnimationClip>();
            foreach (var m in motions) { var c = Clip(m); if (c != null) clips.Add(c); }
            if (clips.Count == 0) return null;
            var go = Instantiate(pf, parent); go.name = rig;
            var anim = go.GetComponentInChildren<Animator>(); if (anim == null) anim = go.AddComponent<Animator>();
            anim.applyRootMotion = false; anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var mat = new Material(Resources.Load<Material>("VehicleLit")); mat.SetTexture("_BaseMap", Resources.Load<Texture2D>("Rigs/" + rig + "_tex")); mat.SetFloat("_Cull", 0f); mat.SetFloat("_Smoothness", 0.15f);
            foreach (var r in go.GetComponentsInChildren<SkinnedMeshRenderer>()) { r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; r.updateWhenOffscreen = true; }
            var rc = go.AddComponent<RiggedCrew>(); rc.Animator = anim; rc.Build(clips.ToArray()); return rc;
        }

        static AnimationClip Clip(string motion)
        {
            foreach (var c in Resources.LoadAll<AnimationClip>("Rigs/Anims/anim_" + motion)) if (!c.name.StartsWith("__preview")) return c;
            return null;
        }

        void Build(AnimationClip[] clips)
        {
            graph = PlayableGraph.Create(name); graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            var output = AnimationPlayableOutput.Create(graph, "crew", Animator);
            mixer = AnimationMixerPlayable.Create(graph, clips.Length); output.SetSourcePlayable(mixer);
            weight = new float[clips.Length];
            for (int i = 0; i < clips.Length; i++)
            {
                var p = AnimationClipPlayable.Create(graph, clips[i]); p.SetTime(Random.value * clips[i].length);   // nobody in step with anybody
                graph.Connect(p, 0, mixer, i);
            }
            now = Random.Range(0, clips.Length); weight[now] = 1f; Apply(); next = Time.time + Random.Range(8f, 16f);
            graph.Play();
        }

        /// <summary>From now on this motion only (a walker walking, or stopped), blended in over a third of a second.</summary>
        public void Play(int motion) { cycling = false; now = Mathf.Clamp(motion, 0, weight.Length - 1); }

        void Update()
        {
            if (!graph.IsValid()) return;
            if (cycling && weight.Length > 1 && Time.time > next) { now = (now + 1 + Random.Range(0, weight.Length - 1)) % weight.Length; next = Time.time + Random.Range(8f, 16f); }
            float k = Time.deltaTime / (cycling ? 1f : 0.3f);
            for (int i = 0; i < weight.Length; i++) weight[i] = Mathf.MoveTowards(weight[i], i == now ? 1f : 0f, k);
            Apply();
        }

        void Apply() { for (int i = 0; i < weight.Length; i++) mixer.SetInputWeight(i, weight[i]); }

        void OnDestroy() { if (graph.IsValid()) graph.Destroy(); }
    }
}

using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace magus.cutscene
{
    /// <summary>One fade on a ScreenFadeTrack: lerps ScreenFader from StartAlpha to
    /// EndAlpha across the clip's length. 1 = black, 0 = clear - so the default is a
    /// fade-in from black. All the actual work happens in ScreenFadeMixerBehaviour.</summary>
    [Serializable]
    public class ScreenFadeClip : PlayableAsset, ITimelineClipAsset
    {
        [Range(0f, 1f)]
        public float StartAlpha = 1f;

        [Range(0f, 1f)]
        public float EndAlpha = 0f;

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            return Playable.Create(graph);
        }
    }
}

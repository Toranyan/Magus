using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace magus.cutscene
{
    /// <summary>Timeline track that drives ScreenFader - no binding needed, since
    /// ScreenFader is a self-creating singleton. Add via the Timeline window's "+" menu,
    /// then add Screen Fade Clips on it. Lets a cutscene fade in only after its first frame
    /// has posed the actors (e.g. the player already lying down), instead of the story
    /// graph fading in before the Timeline has bound anything.</summary>
    [TrackColor(0.1f, 0.1f, 0.1f)]
    [TrackClipType(typeof(ScreenFadeClip))]
    public class ScreenFadeTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            var playable = ScriptPlayable<ScreenFadeMixerBehaviour>.Create(graph, inputCount);

            var fades = new List<ScreenFadeMixerBehaviour.Fade>();
            foreach (var clip in GetClips())
            {
                if (clip.asset is ScreenFadeClip fadeClip)
                {
                    fades.Add(new ScreenFadeMixerBehaviour.Fade
                    {
                        Start = clip.start,
                        End = clip.end,
                        StartAlpha = fadeClip.StartAlpha,
                        EndAlpha = fadeClip.EndAlpha
                    });
                }
            }
            playable.GetBehaviour().Fades = fades;

            return playable;
        }
    }
}

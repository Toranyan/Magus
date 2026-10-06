using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace magus.cutscene
{
    /// <summary>Evaluates ScreenFadeTrack against the director's current time rather than
    /// per-clip weights, so the result is exact regardless of frame timing: inside a clip
    /// it lerps; past a clip it holds that clip's EndAlpha (so a fade-in reliably reaches
    /// fully clear even if the last frame landed just short of the clip's end); before the
    /// first clip it leaves ScreenFader untouched (e.g. still black from a
    /// ScreenFadeAction).</summary>
    public class ScreenFadeMixerBehaviour : PlayableBehaviour
    {
        public struct Fade
        {
            public double Start;
            public double End;
            public float StartAlpha;
            public float EndAlpha;
        }

        public List<Fade> Fades = new List<Fade>();

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            // Timeline editor preview - don't spawn a ScreenFader into the edit-mode scene.
            if (!Application.isPlaying)
            {
                return;
            }

            if (playable.GetGraph().GetResolver() is not PlayableDirector director)
            {
                return;
            }

            if (TryEvaluate(director.time, out var alpha))
            {
                ScreenFader.Instance.SetAlpha(alpha);
            }
        }

        private bool TryEvaluate(double time, out float alpha)
        {
            alpha = 0f;
            var found = false;
            var latestEnd = double.MinValue;

            foreach (var fade in Fades)
            {
                if (time >= fade.Start && time <= fade.End)
                {
                    var duration = fade.End - fade.Start;
                    var t = duration > 0 ? (float)((time - fade.Start) / duration) : 1f;
                    alpha = Mathf.Lerp(fade.StartAlpha, fade.EndAlpha, t);
                    return true;
                }

                if (time > fade.End && fade.End > latestEnd)
                {
                    latestEnd = fade.End;
                    alpha = fade.EndAlpha;
                    found = true;
                }
            }

            return found;
        }
    }
}

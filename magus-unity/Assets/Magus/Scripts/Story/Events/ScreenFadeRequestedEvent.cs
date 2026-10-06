namespace magus.story
{
    /// <summary>Published by ScreenFadeAction. CutsceneManager subscribes, drives
    /// ScreenFader, and answers with ScreenFadeFinishedEvent once the fade completes.</summary>
    public class ScreenFadeRequestedEvent
    {
        /// <summary>0 = fully clear, 1 = fully black.</summary>
        public float TargetAlpha;
        public float Duration;
    }
}

using System;
using tora.eventbus;

namespace magus.story
{
    /// <summary>Fades the screen to/from black via ScreenFadeRequestedEvent - see
    /// CutsceneManager, which owns the actual ScreenFader. Async: the node waits for the
    /// fade to finish before running its next async action, so e.g. [ScreenFadeAction
    /// (black), StartBattleAction, StartTimelineAction] hides the map load and the
    /// cutscene's first frame. Fading back in is usually the cutscene's own job (a Screen
    /// Fade track in the Timeline), so actors are already posed when the screen clears.</summary>
    [Serializable]
    public class ScreenFadeAction : IAsyncStoryAction
    {
        /// <summary>0 = fully clear, 1 = fully black.</summary>
        public float TargetAlpha = 1f;

        /// <summary>Seconds. 0 snaps immediately.</summary>
        public float Duration = 0.5f;

        /// <summary>Fire-and-forget fallback if something calls Execute() directly instead of
        /// going through StoryNode/StoryManager's async sequencing.</summary>
        public void Execute(StoryBlackboard blackboard)
        {
            ExecuteAsync(blackboard, null);
        }

        public void ExecuteAsync(StoryBlackboard blackboard, Action onComplete)
        {
            void OnFinished(ScreenFadeFinishedEvent e)
            {
                EventBus.Unsubscribe<ScreenFadeFinishedEvent>(OnFinished);
                onComplete?.Invoke();
            }

            EventBus.Subscribe<ScreenFadeFinishedEvent>(OnFinished);
            EventBus.Publish(new ScreenFadeRequestedEvent
            {
                TargetAlpha = TargetAlpha,
                Duration = Duration
            });
        }
    }
}

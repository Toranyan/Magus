using System;
using tora.eventbus;

namespace magus.story
{
    /// <summary>Requests playing a cutscene, via TimelineRequestedEvent - see CutsceneManager,
    /// which owns actual playback. Mirrors StartBattleAction/StartDialogueAction. Async: the
    /// node that fires this doesn't complete (and its children don't unlock) until
    /// CutsceneFinishedEvent reports this exact cutscene done - see IAsyncStoryAction.</summary>
    [Serializable]
    public class StartTimelineAction : IAsyncStoryAction
    {
        /// <summary>Addressables path to a cutscene prefab (PlayableDirector + its
        /// TimelineAsset + any actors/camera the timeline binds to, all self-contained -
        /// see CutsceneManager), e.g. "Cutscenes/intro_cutscene".</summary>
        public string CutsceneAddress;

        /// <summary>Fire-and-forget fallback if something calls Execute() directly instead of
        /// going through StoryNode/StoryManager's async sequencing.</summary>
        public void Execute(StoryBlackboard blackboard)
        {
            ExecuteAsync(blackboard, null);
        }

        public void ExecuteAsync(StoryBlackboard blackboard, Action onComplete)
        {
            void OnFinished(CutsceneFinishedEvent e)
            {
                if (e.CutsceneAddress != CutsceneAddress)
                {
                    return;
                }

                EventBus.Unsubscribe<CutsceneFinishedEvent>(OnFinished);
                onComplete?.Invoke();
            }

            EventBus.Subscribe<CutsceneFinishedEvent>(OnFinished);
            EventBus.Publish(new TimelineRequestedEvent
            {
                CutsceneAddress = CutsceneAddress
            });
        }
    }
}

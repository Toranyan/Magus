using System;
using tora.eventbus;

namespace magus.story
{
    /// <summary>Requests playing a cutscene, via TimelineRequestedEvent - see CutsceneManager,
    /// which owns actual playback. Mirrors StartBattleAction/StartDialogueAction.</summary>
    [Serializable]
    public class StartTimelineAction : IStoryAction
    {
        /// <summary>Addressables path to a cutscene prefab (PlayableDirector + its
        /// TimelineAsset + any actors/camera the timeline binds to, all self-contained -
        /// see CutsceneManager), e.g. "Cutscenes/intro_cutscene".</summary>
        public string CutsceneAddress;

        public void Execute(StoryBlackboard blackboard)
        {
            EventBus.Publish(new TimelineRequestedEvent
            {
                CutsceneAddress = CutsceneAddress
            });
        }
    }
}

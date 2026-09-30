using System;
using tora.eventbus;

namespace magus.story
{
    /// <summary>Marks a StoryNode as a save point. Publishes CheckpointReachedEvent rather
    /// than calling CheckpointManager directly, same decoupling as StartBattleAction/
    /// StartDialogueAction/StartTimelineAction.</summary>
    [Serializable]
    public class CheckpointAction : IStoryAction
    {
        public string CheckpointId;

        public void Execute(StoryBlackboard blackboard)
        {
            EventBus.Publish(new CheckpointReachedEvent
            {
                CheckpointId = CheckpointId
            });
        }
    }
}

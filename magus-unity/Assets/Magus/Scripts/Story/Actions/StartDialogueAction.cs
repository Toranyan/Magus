using System;
using tora.eventbus;

namespace magus.story
{
    /// <summary>Requests playing a conversation, via ConversationRequestedEvent - see
    /// DialogueManager, which owns the actual playback. Mirrors StartBattleAction.</summary>
    [Serializable]
    public class StartDialogueAction : IStoryAction
    {
        /// <summary>Addressables path to a DialogueGraph, e.g. "Dialogue/intro_conversation".</summary>
        public string GraphAddress;

        public void Execute(StoryBlackboard blackboard)
        {
            EventBus.Publish(new ConversationRequestedEvent
            {
                GraphAddress = GraphAddress
            });
        }
    }
}

using System;

namespace magus.story
{
    /// <summary>Passes once the conversation at DialogueAddress has been played to the end -
    /// e.g. "talked to the elder" gating the next quest step. Backed by a flag DialogueManager
    /// sets (StoryFlags.ConversationSeen), so it's persisted with the save like any flag.</summary>
    [Serializable]
    public class ConversationSeenCondition : IStoryCondition
    {
        /// <summary>Same Addressables address StartDialogueAction/DialogueCutsceneSignal play,
        /// e.g. "Dialogue/ch01_0001".</summary>
        public string DialogueAddress;
        public bool Expected = true;

        public bool Evaluate(StoryBlackboard blackboard)
        {
            return blackboard.HasFlag(StoryFlags.ConversationSeen(DialogueAddress)) == Expected;
        }
    }
}

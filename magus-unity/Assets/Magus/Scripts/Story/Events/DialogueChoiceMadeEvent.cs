namespace magus.story
{
    /// <summary>Published by DialogueManager (via DialogueRunner.ResponseRecorded) when the
    /// player picks a Choice entry's option that has a VariableKey set. StoryManager
    /// subscribes and writes it into its own StoryBlackboard - Dialogue never writes
    /// StoryBlackboard directly, only publishes what happened. See
    /// Docs/Design/DialogueSystem.md#dialogue-data and StorySystem.md#blackboard.</summary>
    public class DialogueChoiceMadeEvent
    {
        public string VariableKey;
        public string Value;
    }
}

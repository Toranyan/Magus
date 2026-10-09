namespace magus.story
{
    /// <summary>Gameplay reporting a fact to the story - published on EventBus, usually by one
    /// of the components in Magus/Scripts/Story/Triggers/ (StoryTriggerZone, StoryEventOnDeath)
    /// or by DialogueManager when a conversation finishes. StoryManager records Id as a
    /// StoryBlackboard flag, then re-evaluates the graph - so gate a StoryNode on it with a
    /// StoryFlagCondition whose Flag equals this Id. See Docs/Design/StorySystem.md#progression.</summary>
    public class StoryEvent
    {
        public string Id;
    }
}

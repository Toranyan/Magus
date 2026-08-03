namespace magus.story
{
    /// <summary>Gameplay-facing trigger published on EventBus to make StoryManager re-evaluate its graph.
    /// Id is not yet consumed by any condition - BossKilled/Item/ConversationSeen conditions are
    /// out of scope until the systems they depend on (combat events, inventory, dialogue) exist.</summary>
    public class StoryEvent
    {
        public string Id;
    }
}

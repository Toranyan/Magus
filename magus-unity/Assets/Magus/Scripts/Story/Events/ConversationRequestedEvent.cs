namespace magus.story
{
    /// <summary>Published by StartDialogueAction. DialogueManager subscribes directly (unlike
    /// BattleStartRequestedEvent, no GameManager intermediary is needed - playing a
    /// conversation doesn't change GameState) and calls Play(GraphAddress).</summary>
    public class ConversationRequestedEvent
    {
        public string GraphAddress;
    }
}

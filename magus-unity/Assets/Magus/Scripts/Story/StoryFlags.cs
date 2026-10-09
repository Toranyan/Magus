namespace magus.story
{
    /// <summary>Flag names the code itself generates, so the producer and the condition that
    /// reads them can't drift apart. Hand-authored flags (StoryTriggerZone, StoryEventOnDeath,
    /// SetFlagAction) are free-form strings instead.</summary>
    public static class StoryFlags
    {
        private const string ConversationSeenPrefix = "seen:";

        /// <summary>Set by DialogueManager when the conversation at this Addressables address
        /// plays through to its end (not when interrupted) - read by ConversationSeenCondition.</summary>
        public static string ConversationSeen(string dialogueAddress)
        {
            return ConversationSeenPrefix + dialogueAddress;
        }
    }
}

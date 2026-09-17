namespace magus.story
{
    /// <summary>Published by StartTimelineAction. CutsceneManager subscribes directly and
    /// plays it, same shape as ConversationRequestedEvent/BattleStartRequestedEvent.</summary>
    public class TimelineRequestedEvent
    {
        public string CutsceneAddress;
    }
}

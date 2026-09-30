namespace magus.story
{
    /// <summary>Published by CutsceneManager once a cutscene - and any dialogue it triggered
    /// via DialogueCutsceneSignal - has fully finished, whether it played out naturally or
    /// was interrupted by another Play()/Stop() call. StartTimelineAction subscribes to know
    /// when to unblock the StoryNode waiting on it.</summary>
    public class CutsceneFinishedEvent
    {
        public string CutsceneAddress;
    }
}

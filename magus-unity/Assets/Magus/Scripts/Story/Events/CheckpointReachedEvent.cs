namespace magus.story
{
    /// <summary>Published by CheckpointAction. CheckpointManager subscribes directly and
    /// saves - same shape as the other Start*Action/*RequestedEvent pairs.</summary>
    public class CheckpointReachedEvent
    {
        public string CheckpointId;
    }
}

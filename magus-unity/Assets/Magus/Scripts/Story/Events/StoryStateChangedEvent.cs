namespace magus.story
{
    /// <summary>Published by StoryManager at the end of every Evaluate() - i.e. whenever
    /// flags, variables or completed nodes may have changed, including after Load and
    /// ResetProgress. Carries nothing: listeners (e.g. StoryFlagGate) re-read whatever they
    /// care about through StoryManager's read-only accessors.</summary>
    public class StoryStateChangedEvent
    {
    }
}

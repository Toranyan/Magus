using System;

namespace magus.story
{
    /// <summary>Implement alongside IStoryAction for actions whose real-world effect takes
    /// time and should block the node's completion (and its children unlocking) until
    /// finished - e.g. a cutscene. Multiple async actions on one node run sequentially, in
    /// list order, not in parallel - see StoryManager.RunAsyncActions. Plain IStoryAction
    /// implementations remain fire-and-forget and never block anything.</summary>
    public interface IAsyncStoryAction : IStoryAction
    {
        void ExecuteAsync(StoryBlackboard blackboard, Action onComplete);
    }
}

namespace magus.story
{
    public interface IStoryCondition
    {
        bool Evaluate(StoryBlackboard blackboard);
    }
}

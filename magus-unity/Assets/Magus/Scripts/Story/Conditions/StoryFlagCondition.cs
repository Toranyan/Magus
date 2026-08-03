using System;

namespace magus.story
{
    [Serializable]
    public class StoryFlagCondition : IStoryCondition
    {
        public string Flag;
        public bool Expected = true;

        public bool Evaluate(StoryBlackboard blackboard)
        {
            return blackboard.HasFlag(Flag) == Expected;
        }
    }
}

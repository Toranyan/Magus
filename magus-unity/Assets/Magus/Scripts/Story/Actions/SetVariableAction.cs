using System;

namespace magus.story
{
    [Serializable]
    public class SetVariableAction : IStoryAction
    {
        public string Key;
        public StoryVariable Value;

        public void Execute(StoryBlackboard blackboard)
        {
            blackboard.SetVariable(Key, Value);
        }
    }
}

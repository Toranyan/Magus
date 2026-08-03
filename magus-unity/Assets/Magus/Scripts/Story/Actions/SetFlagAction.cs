using System;

namespace magus.story
{
    [Serializable]
    public class SetFlagAction : IStoryAction
    {
        public string Flag;
        public bool Value = true;

        public void Execute(StoryBlackboard blackboard)
        {
            if (Value)
            {
                blackboard.SetFlag(Flag);
            }
            else
            {
                blackboard.ClearFlag(Flag);
            }
        }
    }
}

using System;
using UnityEngine;

namespace magus.story
{
    /// <summary>Debug/POC action - prints Message to the console when the node completes.</summary>
    [Serializable]
    public class LogMessageAction : IStoryAction
    {
        public string Message;

        public void Execute(StoryBlackboard blackboard)
        {
            Debug.Log(Message);
        }
    }
}

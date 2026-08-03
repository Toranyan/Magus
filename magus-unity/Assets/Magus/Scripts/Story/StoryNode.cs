using System;
using System.Collections.Generic;
using UnityEngine;
using tora.graph;

namespace magus.story
{
    [Serializable]
    public class StoryNode : GraphNode
    {
        public string Name;

        [SerializeReference]
        public List<IStoryCondition> Conditions = new List<IStoryCondition>();

        [SerializeReference]
        public List<IStoryAction> Actions = new List<IStoryAction>();

        public int Priority;
        public List<string> Tags = new List<string>();

        public bool EvaluateConditions(StoryBlackboard blackboard)
        {
            foreach (var condition in Conditions)
            {
                if (condition == null || !condition.Evaluate(blackboard))
                {
                    return false;
                }
            }
            return true;
        }

        public void ExecuteActions(StoryBlackboard blackboard)
        {
            foreach (var action in Actions)
            {
                action?.Execute(blackboard);
            }
        }
    }
}

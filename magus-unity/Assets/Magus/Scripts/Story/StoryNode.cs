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

        /// <summary>Runs every plain (non-async) action. Async actions are skipped here -
        /// StoryManager runs those separately, sequentially, via GetAsyncActions(), since
        /// they block the node's completion until they signal done.</summary>
        public void ExecuteActions(StoryBlackboard blackboard)
        {
            foreach (var action in Actions)
            {
                if (action is IAsyncStoryAction)
                {
                    continue;
                }
                action?.Execute(blackboard);
            }
        }

        public List<IAsyncStoryAction> GetAsyncActions()
        {
            var result = new List<IAsyncStoryAction>();
            foreach (var action in Actions)
            {
                if (action is IAsyncStoryAction asyncAction)
                {
                    result.Add(asyncAction);
                }
            }
            return result;
        }
    }
}

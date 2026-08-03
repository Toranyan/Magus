using System;
using UnityEngine;

namespace magus.story
{
    public enum VariableComparison
    {
        Equals,
        NotEquals,
        GreaterThan,
        GreaterOrEqual,
        LessThan,
        LessOrEqual
    }

    [Serializable]
    public class VariableCondition : IStoryCondition
    {
        public string Key;
        public VariableComparison Comparison;
        public StoryVariable Value;

        public bool Evaluate(StoryBlackboard blackboard)
        {
            if (!blackboard.TryGetVariable(Key, out var current) || current.Type != Value.Type)
            {
                return false;
            }

            switch (current.Type)
            {
                case StoryVariable.ValueType.Bool:
                    return Comparison == VariableComparison.NotEquals
                        ? current.BoolValue != Value.BoolValue
                        : current.BoolValue == Value.BoolValue;
                case StoryVariable.ValueType.Int:
                    return CompareNumeric(current.IntValue, Value.IntValue);
                case StoryVariable.ValueType.Float:
                    return CompareNumeric(current.FloatValue, Value.FloatValue);
                case StoryVariable.ValueType.String:
                    return Comparison == VariableComparison.NotEquals
                        ? current.StringValue != Value.StringValue
                        : current.StringValue == Value.StringValue;
                default:
                    return false;
            }
        }

        private bool CompareNumeric(float current, float target)
        {
            switch (Comparison)
            {
                case VariableComparison.Equals: return Mathf.Approximately(current, target);
                case VariableComparison.NotEquals: return !Mathf.Approximately(current, target);
                case VariableComparison.GreaterThan: return current > target;
                case VariableComparison.GreaterOrEqual: return current >= target;
                case VariableComparison.LessThan: return current < target;
                case VariableComparison.LessOrEqual: return current <= target;
                default: return false;
            }
        }
    }
}

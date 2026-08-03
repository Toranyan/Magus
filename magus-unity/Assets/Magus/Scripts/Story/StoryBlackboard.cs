using System;
using System.Collections.Generic;

namespace magus.story
{
    /// <summary>Global-scope only in v1 - see Docs/Design/StorySystem.md#blackboard.</summary>
    public class StoryBlackboard
    {
        private readonly Dictionary<string, StoryVariable> _variables = new Dictionary<string, StoryVariable>();
        private readonly HashSet<string> _flags = new HashSet<string>();

        public bool HasFlag(string flag) => _flags.Contains(flag);

        public void SetFlag(string flag) => _flags.Add(flag);

        public void ClearFlag(string flag) => _flags.Remove(flag);

        public bool TryGetVariable(string key, out StoryVariable variable) => _variables.TryGetValue(key, out variable);

        public void SetVariable(string key, StoryVariable value) => _variables[key] = value;

        public StoryBlackboardSnapshot CaptureSnapshot()
        {
            var snapshot = new StoryBlackboardSnapshot();

            foreach (var flag in _flags)
            {
                snapshot.Flags.Add(flag);
            }

            foreach (var kvp in _variables)
            {
                snapshot.Variables.Add(new StoryVariableEntry { Key = kvp.Key, Value = kvp.Value });
            }

            return snapshot;
        }

        public void RestoreSnapshot(StoryBlackboardSnapshot snapshot)
        {
            _flags.Clear();
            _variables.Clear();

            if (snapshot == null)
            {
                return;
            }

            foreach (var flag in snapshot.Flags)
            {
                _flags.Add(flag);
            }

            foreach (var entry in snapshot.Variables)
            {
                _variables[entry.Key] = entry.Value;
            }
        }
    }

    [Serializable]
    public class StoryBlackboardSnapshot
    {
        public List<string> Flags = new List<string>();
        public List<StoryVariableEntry> Variables = new List<StoryVariableEntry>();
    }

    [Serializable]
    public class StoryVariableEntry
    {
        public string Key;
        public StoryVariable Value;
    }
}

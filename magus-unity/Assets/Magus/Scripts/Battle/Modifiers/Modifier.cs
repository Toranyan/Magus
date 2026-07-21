using System;
using System.Threading;

namespace magus.battle
{
    // Inert data record — see Docs/Design/Modifier System.md.
    // Created and owned by a Buff/Debuff/Passive Skill/Item; never mutated after creation.
    public class Modifier
    {
        private static long _sequenceCounter;

        public ModifierType Type { get; }
        public ModifierOperation Operation { get; }
        public float Value { get; }
        public ModifierTag[] Tags { get; }

        // Only meaningful for Operation == Override.
        public int Priority { get; }

        // Monotonic acquire order, used as the Override tiebreak ("last applied wins").
        public long AcquiredAt { get; }

        public ModifierOwnerId Owner { get; }

        public Modifier(
            ModifierType type,
            ModifierOperation operation,
            float value,
            ModifierOwnerId owner,
            ModifierTag[] tags = null,
            int priority = 0)
        {
            Type = type;
            Operation = operation;
            Value = value;
            Owner = owner;
            Tags = tags ?? Array.Empty<ModifierTag>();
            Priority = priority;
            AcquiredAt = Interlocked.Increment(ref _sequenceCounter);
        }
    }
}

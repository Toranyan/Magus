using System;

namespace magus.battle
{
    // Opaque handle a Buff/Debuff/Passive Skill/Item holds so it can later
    // remove exactly the Modifiers it registered. Never consulted by resolution.
    public readonly struct ModifierOwnerId : IEquatable<ModifierOwnerId>
    {
        private readonly Guid _id;

        private ModifierOwnerId(Guid id)
        {
            _id = id;
        }

        public static ModifierOwnerId New() => new ModifierOwnerId(Guid.NewGuid());

        public bool Equals(ModifierOwnerId other) => _id.Equals(other._id);
        public override bool Equals(object obj) => obj is ModifierOwnerId other && Equals(other);
        public override int GetHashCode() => _id.GetHashCode();

        public static bool operator ==(ModifierOwnerId left, ModifierOwnerId right) => left.Equals(right);
        public static bool operator !=(ModifierOwnerId left, ModifierOwnerId right) => !left.Equals(right);
    }
}

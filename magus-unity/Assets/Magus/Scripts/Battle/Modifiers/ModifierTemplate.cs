using System;

namespace magus.battle
{
    // Serialized template for a Modifier, authored inside StatusEffectMasterData.
    // Owner/AcquiredAt only exist on the real Modifier, assigned at instantiation time.
    [Serializable]
    public class ModifierTemplate
    {
        public ModifierType Type;
        public ModifierOperation Operation;
        public float Value;
        public ModifierTag[] Tags;
        public int Priority;

        public Modifier ToModifier(ModifierOwnerId owner)
        {
            return new Modifier(Type, Operation, Value, owner, Tags, Priority);
        }
    }
}

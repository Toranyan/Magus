using System;
using magus.battle;

namespace magus.master
{
    [Serializable]
    public class StatusEffectMasterData : BaseMasterData
    {
        public string Name;
        public string Description;
        public string Icon;

        public StatusEffectCategory Category;
        public StackRule StackRule;

        public float Duration;       // <= 0 means Permanent (Passive Skill, Item)
        public float TickInterval;   // <= 0 means no periodic side effect

        public ModifierTemplate[] Modifiers;
    }
}

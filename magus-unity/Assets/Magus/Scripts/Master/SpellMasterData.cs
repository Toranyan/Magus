using System;
using magus.battle;

namespace magus.master
{
    [Serializable]
    public class SpellMasterData : BaseMasterData
    {
        public string Name;
        public string Description;

        public SpellType SpellType;
        public SpellExecutorType ExecutorType;
        public ElementType Element;
        public StrategicCategory Category;
        public SpellTargetingType TargetingType;

        public float BaseCastTime;
        public float BaseManaCost;
        public float BaseCooldown;
        public float BaseDuration;
        public float BaseSpeed;
        public float BaseRange;
        public float BaseSize;
        public float BaseCount;

        public string[] AssetIds;   // [0] = main prefab, additional entries spell-specific

        public float[] Params;      // spell-specific extra parameters

        public string[] StatusEffectIds;   // StatusEffectMasterData ids applied by Buff/Debuff executor types
    }
}

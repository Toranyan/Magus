using magus.master;
using UnityEngine;

namespace magus.battle
{
    // Stateless runtime copy of SpellMasterData.
    // No owner, no cooldown — those live in UnitSpellInstance.
    public class SpellInfo
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }

        public SpellType SpellType { get; }
        public ElementType Element { get; }
        public StrategicCategory Category { get; }
        public SpellTargetingType TargetingType { get; }

        public float CastTime { get; }
        public float ManaCost { get; }
        public float Cooldown { get; }
        public float Duration { get; }
        public float Speed { get; }
        public float Range { get; }
        public float Size { get; }
        public float Count { get; }

        public string[] AssetIds { get; }
        public float[] Params { get; }

        public SpellInfo(SpellMasterData data)
        {
            if (data == null)
            {
                Debug.LogError("SpellInfo created with null SpellMasterData");
                return;
            }

            Id          = data.Id;
            Name        = data.Name;
            Description = data.Description;

            SpellType     = data.SpellType;
            Element       = data.Element;
            Category      = data.Category;
            TargetingType = data.TargetingType;

            CastTime  = data.BaseCastTime;
            ManaCost  = data.BaseManaCost;
            Cooldown  = data.BaseCooldown;
            Duration  = data.BaseDuration;
            Speed     = data.BaseSpeed;
            Range     = data.BaseRange;
            Size      = data.BaseSize;
            Count     = data.BaseCount;

            AssetIds = data.AssetIds;
            Params   = data.Params;
        }
    }
}

using System;
using UnityEngine;

namespace magus.battle
{
    public enum LootEntryKind
    {
        Guaranteed,
        Chance,
        WeightedPool,
    }

    /// <summary>
    /// A single row in a LootTable. Guaranteed/Chance entries resolve to either Effect
    /// or NestedTable (whichever is assigned); WeightedPool instead picks exactly one
    /// of WeightedOptions. The amount lives here, on the entry, not on a prefab or
    /// asset - PickupSpawner picks the prefab from Effect.Kind, so the same "XP Orb"
    /// prefab is reused for every amount variation.
    /// </summary>
    [Serializable]
    public class LootEntry
    {
        public LootEntryKind Kind = LootEntryKind.Guaranteed;

        [Tooltip("Used when Kind is Guaranteed or Chance. Set either Effect or NestedTable, not both.")]
        public PickupEffect Effect;
        public LootTable NestedTable;
        public int Quantity = 1;

        [Range(0f, 1f)]
        [Tooltip("Used when Kind is Chance.")]
        public float Chance = 1f;

        [Tooltip("Used when Kind is WeightedPool. Exactly one option is selected.")]
        public WeightedLootOption[] WeightedOptions;
    }

    /// <summary>One choice inside a LootEntry's WeightedPool.</summary>
    [Serializable]
    public class WeightedLootOption
    {
        public PickupEffect Effect;
        public LootTable NestedTable;
        public int Quantity = 1;
        public float Weight = 1f;
    }
}

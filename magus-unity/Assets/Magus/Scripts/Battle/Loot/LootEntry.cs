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
    /// A single row in a LootTable. Guaranteed/Chance entries resolve to either Pickup
    /// or NestedTable (whichever is assigned); WeightedPool instead picks exactly one
    /// of WeightedOptions.
    /// </summary>
    [Serializable]
    public class LootEntry
    {
        public LootEntryKind Kind = LootEntryKind.Guaranteed;

        [Tooltip("Addressable id of a Pickup prefab. Used when Kind is Guaranteed or Chance. Set either PickupPrefabId or NestedTable, not both.")]
        public string PickupPrefabId;
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
        public string PickupPrefabId;
        public LootTable NestedTable;
        public int Quantity = 1;
        public float Weight = 1f;
    }
}

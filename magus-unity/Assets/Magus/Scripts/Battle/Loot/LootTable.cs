using System.Collections.Generic;
using UnityEngine;

namespace magus.battle
{
    /// <summary>
    /// Data-driven collection of LootEntries. Reusable across enemies/chests/bosses;
    /// entries may nest other LootTables instead of spawning a Pickup directly.
    /// Never instantiates GameObjects - only appends PickupSpawnRequests.
    /// </summary>
    [CreateAssetMenu(fileName = "LootTable", menuName = "Scriptable Objects/Loot/LootTable")]
    public class LootTable : ScriptableObject
    {
        [SerializeField]
        private LootEntry[] _entries;

        public void Roll(LootContext context, List<PickupSpawnRequest> results)
        {
            if (_entries == null) return;

            foreach (var entry in _entries)
                RollEntry(entry, context, results);
        }

        private static void RollEntry(LootEntry entry, LootContext context, List<PickupSpawnRequest> results)
        {
            switch (entry.Kind)
            {
                case LootEntryKind.Guaranteed:
                    Resolve(entry.PickupPrefabId, entry.NestedTable, entry.Quantity, context, results);
                    break;

                case LootEntryKind.Chance:
                    if (Random.value <= entry.Chance)
                        Resolve(entry.PickupPrefabId, entry.NestedTable, entry.Quantity, context, results);
                    break;

                case LootEntryKind.WeightedPool:
                    var picked = PickWeighted(entry.WeightedOptions);
                    if (picked != null)
                        Resolve(picked.PickupPrefabId, picked.NestedTable, picked.Quantity, context, results);
                    break;
            }
        }

        private static void Resolve(string pickupPrefabId, LootTable nestedTable, int quantity, LootContext context, List<PickupSpawnRequest> results)
        {
            if (nestedTable != null)
            {
                nestedTable.Roll(context, results);
                return;
            }

            if (string.IsNullOrEmpty(pickupPrefabId) || quantity <= 0) return;

            results.Add(new PickupSpawnRequest
            {
                PickupPrefabId = pickupPrefabId,
                Quantity = quantity,
                Position = context.Victim != null ? context.Victim.transform.position : Vector3.zero,
            });
        }

        private static WeightedLootOption PickWeighted(WeightedLootOption[] options)
        {
            if (options == null || options.Length == 0) return null;

            float total = 0f;
            foreach (var option in options)
                total += Mathf.Max(0f, option.Weight);

            if (total <= 0f) return null;

            float roll = Random.value * total;
            float cumulative = 0f;

            foreach (var option in options)
            {
                cumulative += Mathf.Max(0f, option.Weight);
                if (roll <= cumulative) return option;
            }

            return options[options.Length - 1];
        }
    }
}

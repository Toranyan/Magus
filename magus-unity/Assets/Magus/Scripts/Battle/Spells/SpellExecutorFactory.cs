using System;
using System.Collections.Generic;
using magus.battle.spells.executors;
using UnityEngine;

namespace magus.battle
{
    public static class SpellExecutorFactory
    {
        private static readonly Dictionary<SpellType, Func<ISpellExecutor>> _map = new()
        {
            { SpellType.Fireball, () => new FireballSpellExecutor() },
            // TODO: register remaining 17 Tier 1 spell executors as they are implemented
        };

        public static ISpellExecutor Create(SpellInfo info)
        {
            if (_map.TryGetValue(info.SpellType, out var factory))
                return factory();

            Debug.LogError($"[SpellExecutorFactory] No executor registered for SpellType {info.SpellType}");
            return null;
        }
    }
}

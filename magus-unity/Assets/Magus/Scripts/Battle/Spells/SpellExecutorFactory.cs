using System;
using System.Collections.Generic;
using magus.battle.spells.executors;
using UnityEngine;

namespace magus.battle
{
    public static class SpellExecutorFactory
    {
        private static readonly Dictionary<SpellExecutorType, Func<ISpellExecutor>> _map = new()
        {
            { SpellExecutorType.Projectile, () => new ProjectileSpellExecutor() },
            { SpellExecutorType.Buff, () => new StatusEffectSpellExecutor() },
            { SpellExecutorType.Debuff, () => new StatusEffectSpellExecutor() },
            // TODO: register additional executor types as they are implemented
        };

        public static ISpellExecutor Create(SpellInfo info)
        {
            if (_map.TryGetValue(info.ExecutorType, out var factory))
                return factory();

            Debug.LogError($"[SpellExecutorFactory] No executor registered for SpellExecutorType {info.ExecutorType}");
            return null;
        }
    }
}

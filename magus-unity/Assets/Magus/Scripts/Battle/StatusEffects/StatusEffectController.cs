using System;
using System.Collections.Generic;
using magus.master;
using UnityEngine;

namespace magus.battle
{
    // Per-Unit owner/lifecycle manager for Status Effects (Buffs/Debuffs).
    // Applies/removes StatusEffectInstances, enforces each definition's StackRule,
    // and ticks Duration/TickInterval. See Docs/Design/Buff and Debuff System.md.
    [RequireComponent(typeof(Unit))]
    public class StatusEffectController : MonoBehaviour
    {
        private Unit _unit;
        private readonly List<StatusEffectInstance> _active = new List<StatusEffectInstance>();

        public event Action<StatusEffectInstance> Applied;
        public event Action<StatusEffectInstance> Removed;

        public IReadOnlyList<StatusEffectInstance> Active => _active;

        private void Awake()
        {
            _unit = GetComponent<Unit>();
        }

        private void Update()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var instance = _active[i];
                instance.Tick(Time.deltaTime);
                if (instance.IsExpired)
                {
                    RemoveInstance(instance);
                }
            }
        }

        public StatusEffectInstance Apply(StatusEffectMasterData data)
        {
            if (data.StackRule != StackRule.Stack)
            {
                var existing = _active.Find(s => s.Data == data);
                if (existing != null)
                {
                    switch (data.StackRule)
                    {
                        case StackRule.Refresh:
                            existing.RefreshDuration();
                            return existing;
                        case StackRule.StackDurationOnly:
                            existing.ExtendDuration();
                            return existing;
                        case StackRule.Ignore:
                            return existing;
                    }
                }
            }

            var instance = new StatusEffectInstance(data, _unit);
            instance.Apply(_unit.Modifiers);
            _active.Add(instance);
            Applied?.Invoke(instance);
            return instance;
        }

        public void RemoveInstance(StatusEffectInstance instance)
        {
            if (!_active.Remove(instance)) return;
            instance.Remove(_unit.Modifiers);
            Removed?.Invoke(instance);
        }

        public void RemoveAll(StatusEffectMasterData data)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (_active[i].Data == data)
                    RemoveInstance(_active[i]);
            }
        }
    }
}

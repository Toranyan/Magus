using System;
using System.Collections.Generic;
using magus.master;

namespace magus.battle
{
    // Runtime instance of a StatusEffectMasterData applied to a Unit.
    // See Docs/Design/Buff and Debuff System.md.
    public class StatusEffectInstance
    {
        public StatusEffectMasterData Data { get; }
        public Unit Target { get; }
        public ModifierOwnerId OwnerId { get; }

        public float RemainingDuration { get; private set; }
        public bool IsPermanent => Data.Duration <= 0f;
        public bool IsExpired => !IsPermanent && RemainingDuration <= 0f;

        // Fired every Data.TickInterval, e.g. for Burning's periodic damage.
        // This system only drives the timer — the side effect itself is external.
        public event Action<StatusEffectInstance> Ticked;

        private readonly List<Modifier> _registeredModifiers = new List<Modifier>();
        private float _tickTimer;

        public StatusEffectInstance(StatusEffectMasterData data, Unit target)
        {
            Data = data;
            Target = target;
            OwnerId = ModifierOwnerId.New();
            RemainingDuration = data.Duration;
        }

        public void Apply(ModifierCollection modifiers)
        {
            if (Data.Modifiers == null) return;

            foreach (var template in Data.Modifiers)
            {
                var modifier = template.ToModifier(OwnerId);
                modifiers.Add(modifier);
                _registeredModifiers.Add(modifier);
            }
        }

        // Deregisters exactly the Modifiers this instance registered.
        public void Remove(ModifierCollection modifiers)
        {
            modifiers.RemoveAll(OwnerId);
            _registeredModifiers.Clear();
        }

        public void RefreshDuration() => RemainingDuration = Data.Duration;

        public void ExtendDuration() => RemainingDuration += Data.Duration;

        public void Tick(float deltaTime)
        {
            if (!IsPermanent)
                RemainingDuration -= deltaTime;

            if (Data.TickInterval > 0f)
            {
                _tickTimer += deltaTime;
                while (_tickTimer >= Data.TickInterval)
                {
                    _tickTimer -= Data.TickInterval;
                    Ticked?.Invoke(this);
                }
            }
        }
    }
}

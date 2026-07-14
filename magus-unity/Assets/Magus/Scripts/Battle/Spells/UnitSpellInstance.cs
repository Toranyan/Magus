using UnityEngine;

namespace magus.battle
{
    // A spell equipped to a specific unit.
    // Owns cooldown state and the link to the caster.
    public class UnitSpellInstance
    {
        public SpellInfo Info { get; }
        public IBattleEntity Owner { get; }

        public float CooldownRemaining { get; private set; }
        public bool IsReady => CooldownRemaining <= 0f;

        // When true, the owner will keep attempting to cast this spell automatically
        // (e.g. every frame) instead of only on explicit input. TryCast already no-ops
        // while on cooldown, so it's safe to call repeatedly.
        public bool Autocast { get; set; }

        public UnitSpellInstance(SpellInfo info, IBattleEntity owner)
        {
            Info  = info;
            Owner = owner;
        }

        public void Tick(float deltaTime)
        {
            if (CooldownRemaining > 0f)
                CooldownRemaining -= deltaTime;
        }

        // Returns false if the spell cannot be cast right now.
        public bool TryCast(SpellCastContext context)
        {
            if (!IsReady)
            {
                Debug.Log($"[UnitSpellInstance] {Info.Name} is on cooldown ({CooldownRemaining:F1}s remaining)");
                return false;
            }

            // TODO: check mana (requires ManaComponent on owner — not yet implemented)

            var executor = SpellExecutorFactory.Create(Info);
            if (executor == null)
                return false;

            executor.Execute(context);

            CooldownRemaining = Info.Cooldown;
            return true;
        }
    }
}

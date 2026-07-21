using magus.master;
using UnityEngine;

namespace magus.battle.spells.executors
{
    // Shared by SpellExecutorType.Buff and SpellExecutorType.Debuff — applying a
    // Status Effect works identically either way; StatusEffectMasterData.Category
    // is what distinguishes a Buff from a Debuff, not the executor.
    public class StatusEffectSpellExecutor : ISpellExecutor
    {
        public void Execute(SpellCastContext context)
        {
            var targetUnit = context.Target as Unit ?? context.Caster as Unit;
            if (targetUnit == null)
            {
                Debug.LogError("[StatusEffectSpellExecutor] No valid Unit target or caster to apply status effect to.");
                return;
            }

            var controller = targetUnit.GetComponent<StatusEffectController>();
            if (controller == null)
            {
                Debug.LogError($"[StatusEffectSpellExecutor] {targetUnit.name} has no StatusEffectController.");
                return;
            }

            if (context.Info.StatusEffectIds == null) return;

            foreach (var id in context.Info.StatusEffectIds)
            {
                var data = MasterData.GetMasterData<StatusEffectMasterData>(id);
                if (data == null) continue; // GetMasterData already logs a warning

                controller.Apply(data);
            }
        }
    }
}

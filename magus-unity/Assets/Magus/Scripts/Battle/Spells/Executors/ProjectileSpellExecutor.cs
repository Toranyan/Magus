using Cysharp.Threading.Tasks;
using UnityEngine;

namespace magus.battle.spells.executors
{
    public class ProjectileSpellExecutor : ISpellExecutor
    {
        public void Execute(SpellCastContext context)
        {
            ExecuteAsync(context).Forget();
        }

        private async UniTaskVoid ExecuteAsync(SpellCastContext context)
        {
            var proj = await BattleController.Instance.ProjectileManager
                .CreateProjectile(context.Info.AssetIds[0], context.Caster);

            proj.transform.SetParent(null);


			// determine origin
            var castPos = context.CastPosition;
			castPos.y += 1f;
			// if caster is a unit, find projectile origin transform
			var caster = context.Caster as Unit;
            if (caster != null)
            {
                castPos = caster.ProjectileOrigin ? caster.ProjectileOrigin.position : castPos;
            }
            proj.transform.position = castPos;

            var targetPos = context.TargetPosition;
            targetPos.y += 1f;

            var dir = (targetPos - castPos);
            dir.y = 0f;

            // Setup() calls DamageDealer.BeginAttack(), which snapshots this Damage
            // value through the caster's Modifiers - must be set before Setup().
            if (proj.DamageDealer != null)
                proj.DamageDealer.Damage = context.Info.Damage;

            proj.Setup(dir.normalized * context.Info.Speed);

            // TODO: set elemental damage type on projectile (requires DamageType per element)
        }
    }
}

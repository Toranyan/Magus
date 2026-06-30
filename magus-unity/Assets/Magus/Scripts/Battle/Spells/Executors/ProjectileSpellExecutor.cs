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
            // TODO: cast time — wait context.Info.CastTime before spawning (requires player FSM lock)

            var proj = await BattleController.Instance.ProjectileManager
                .CreateProjectile(context.Info.AssetIds[0], context.Caster);

            proj.transform.SetParent(null);

            var castPos = context.CastPosition;
            castPos.y += 1f;
            proj.transform.position = castPos;

            var targetPos = context.TargetPosition;
            targetPos.y += 1f;

            var dir = (targetPos - castPos);
            dir.y = 0f;
            proj.Setup(dir.normalized * context.Info.Speed);

            // TODO: set elemental damage type on projectile (requires DamageType per element)
        }
    }
}

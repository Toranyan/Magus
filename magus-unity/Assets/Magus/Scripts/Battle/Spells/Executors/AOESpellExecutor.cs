using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace magus.battle.spells.executors
{
    // Meteor-style spells: a telegraph effect plays at the target position, then
    // after an impact delay the impact effect and a one-shot AoE damage volume
    // spawn there.
    //
    // AssetIds: [0] = telegraph/fall effect, [1] = impact effect, [2] = damage dealer prefab (needs AoEDamageDealer).
    // Params:   [0] = impact delay in seconds, [1] = blast radius (falls back to Info.Size if unset).
    public class AOESpellExecutor : ISpellExecutor
    {
        public void Execute(SpellCastContext context)
        {
            ExecuteAsync(context).Forget();
        }

        private async UniTaskVoid ExecuteAsync(SpellCastContext context)
        {
            var info = context.Info;
            var targetPos = context.TargetPosition;

            var impactDelay = info.Params != null && info.Params.Length > 0 ? info.Params[0] : 0f;
            var radius = info.Params != null && info.Params.Length > 1 && info.Params[1] > 0f
                ? info.Params[1]
                : info.Size;

            if (HasAsset(info, 0))
                await BattleController.Instance.EffectManager.CreateEffect(info.AssetIds[0], targetPos);

            if (impactDelay > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(impactDelay));

            if (HasAsset(info, 1))
            {
                var impactEffect = await BattleController.Instance.EffectManager.CreateEffect(info.AssetIds[1], targetPos);
                impactEffect.transform.localScale = Vector3.one * radius;
            }

            if (HasAsset(info, 2))
            {
                var dealerObj = await BattleController.Instance.EffectManager.CreateEffect(info.AssetIds[2], targetPos);
                dealerObj.transform.localScale = Vector3.one * radius;

                var aoeDealer = dealerObj.GetComponent<AoEDamageDealer>();
                if (aoeDealer != null)
                    aoeDealer.Detonate(context.Caster, radius, info.Damage);
                else
                    Debug.LogError($"[AOESpellExecutor] {info.AssetIds[2]} has no AoEDamageDealer component.");

                dealerObj.GetComponent<PoolableHandler>()?.ReturnToPool();
            }
        }

        private static bool HasAsset(SpellInfo info, int index)
        {
            return info.AssetIds != null && info.AssetIds.Length > index && !string.IsNullOrEmpty(info.AssetIds[index]);
        }
    }
}

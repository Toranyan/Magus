using Cysharp.Threading.Tasks;
using UnityEngine;

namespace magus.battle
{
	public class ProjectileAbiltyExecutor : IAbilityExecutor
	{
		public void ExecuteAbility(AbilityExecutionContext context)
		{

			var speed = context.Info.MasterData.OtherParams[0];

			switch (context.Info.TargetingType)
			{
				case AbilityTargetingType.TargetPosition:
					CreateProjectile(context.SourcePosition, context.TargetPosition, context.Info.AssetId[0], context.Owner, speed).Forget();
					break;
				case AbilityTargetingType.TargetObject:
				{
					// Prefer GameObject positions when available, otherwise fall back to provided positions
					Vector3 startPos = context.Source != null && context.Source.GameObject != null
						? context.Source.GameObject.transform.position
						: context.SourcePosition;

					Vector3 targetPos = context.Target != null && context.Target.GameObject != null
						? context.Target.GameObject.transform.position
						: context.TargetPosition;

					CreateProjectile(startPos, targetPos, context.Info.AssetId[0], context.Owner, speed).Forget();
				}
				break;
				default:

					break;
			}
		}


		private async UniTask CreateProjectile(Vector3 startPos, Vector3 targetPos, string assetId, IBattleEntity owner, float speed)
		{
			//TODO get the projectile from data

			var proj = await BattleController.Instance.ProjectileManager.CreateProjectile(assetId, owner);
			proj.transform.SetParent(null);

			//TODO setup projectile params

			proj.transform.position = startPos;

			var deltaVec = targetPos - startPos;
			deltaVec.y =0;
			var initVec = deltaVec.normalized * speed;

			proj.Setup(initVec);
		}


	}

}

using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace magus.battle
{
    public class ProjectileManager : MonoBehaviour
    {

		[SerializeField]
		private ObjectPoolManager _poolManager;

		public async UniTask Init(string[] ids)
		{
			await _poolManager.Init(ids);
		}


		public async UniTask<ProjectileBase> CreateProjectile(string id, IBattleEntity owner)
		{
			var handle = await _poolManager.Allocate(id);
			handle.gameObject.SetActive(true);

			var proj = handle.GetComponent<ProjectileBase>();
			proj.ClearEvents();
			proj.Killed += OnProjectileKilled;
			proj.SetOwner(owner);

			return proj;
		}


		public void DeallocProjectile(ProjectileBase projectile)
		{
			var handle = projectile.GetComponent<PoolableHandler>();
			if (handle != null)
			{
				handle.ReturnToPool();
			} 
			else 
			{
				Debug.LogError("Unable to return to pool");
			}
		}

		private void OnProjectileKilled(ProjectileBase projectile)
		{
			DeallocProjectile(projectile);
		}

	}
}
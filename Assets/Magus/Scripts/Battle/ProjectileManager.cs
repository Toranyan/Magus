using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace magus.battle
{
    public class ProjectileManager : MonoBehaviour
    {
		private Dictionary<string, ObjectPooler<ProjectileBase>> _poolDict = new();

		public void Init()
		{

		}

		public async UniTask Init(string[] ids)
		{
			await UniTask.WhenAll(
				ids.Select(id => CreatePool(id))
			);
		}


		public async UniTask<ProjectileBase> CreateProjectile(string id, IOwner owner)
		{
			ProjectileBase proj;
			if (_poolDict.TryGetValue(id, out var pool))
			{
				proj = pool.Allocate();
				return proj;
			} else
			{
				var newPool = await CreatePool(id);

				proj = newPool.Allocate();
			}
			return proj;
		}

		public void DeallocProjectile(ProjectileBase projectile)
		{
			var id = GetProjectileId(projectile);

			if (_poolDict.TryGetValue(id, out var pool))
			{
				pool.Free(projectile);
			} else
			{
				Debug.LogError("Deallocating a projectile that was not allocated through manager");
			}
		}

		private async UniTask<ProjectileBase> GetProjectilePrefab(string id)
		{
			//TODO Get from master
			var obj = await Addressables.LoadAssetAsync<GameObject>("GameObjects/Projectiles/Fireball.prefab");
			var proj = obj.GetComponent<ProjectileBase>();
			return proj;
		}

		private string GetProjectileId(ProjectileBase projectile)
		{
			return "1";
		}

		private async UniTask<ObjectPooler<ProjectileBase>> CreatePool(string id)
		{
			var prefab = await GetProjectilePrefab(id);
			var newPool = new ObjectPooler<ProjectileBase>();
			newPool.Initialize(prefab, this.transform, OnProjectileCreated);
			_poolDict[id] = newPool;
			return newPool;
		}

		private void OnProjectileCreated(ProjectileBase projectile)
		{
			//called when the pool is initialized or expanded

			//Setup events
			projectile.Killed += OnProjectileKilled;
			projectile.Revived += OnProjectileRevived;
		}

		private void OnProjectileRevived(ProjectileBase projectile)
		{
			//Theoretically the proj was allocated through here already, no need to do anything
		}

		private void OnProjectileKilled(ProjectileBase projectile)
		{
			DeallocProjectile(projectile);
		}

	}
}
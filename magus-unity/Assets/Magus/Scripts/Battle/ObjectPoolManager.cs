using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace magus.battle
{

    public class ObjectPoolManager : MonoBehaviour
    {
        private readonly Dictionary<string, ObjectPooler<PoolableHandler>> _poolDict = new();
        private readonly Dictionary<string, UniTask<ObjectPooler<PoolableHandler>>> _pendingPools = new();

		/// <summary>Warms the pools for these ids ahead of time. Optional - Allocate()
		/// creates a pool on demand if one doesn't exist yet.</summary>
		public async UniTask Preload(string[] ids)
		{
			await UniTask.WhenAll(
				ids.Select(id => GetOrCreatePool(id))
			);
		}

		public async UniTask<PoolableHandler> Allocate(string id)
		{
			var pool = await GetOrCreatePool(id);
			return pool.Allocate();
		}

		/// <summary>
		/// Returns the cached pool for this id, creating it on first request. Concurrent
		/// requests for the same id (e.g. Preload racing a Create call) share a single
		/// creation rather than creating the pool twice.
		/// </summary>
		private UniTask<ObjectPooler<PoolableHandler>> GetOrCreatePool(string id)
		{
			if (_poolDict.TryGetValue(id, out var pool))
				return UniTask.FromResult(pool);

			if (!_pendingPools.TryGetValue(id, out var pending))
			{
				pending = CreatePool(id).Preserve();
				_pendingPools[id] = pending;
			}

			return pending;
		}

		public void Dealloc(PoolableHandler obj)
		{
			var id = GetId(obj);

			if (_poolDict.TryGetValue(id, out var pool))
			{
				pool.Free(obj);
			} else
			{
				Debug.LogError("Deallocating an object that was not allocated through manager");
			}
		}

		private async UniTask<PoolableHandler> GetPrefab(string id)
		{
			//TODO Get from master
			var obj = await Addressables.LoadAssetAsync<GameObject>(id);
			var monoBehaviour = obj.GetComponent<PoolableHandler>();
			return monoBehaviour;
		}

		private string GetId(PoolableHandler obj)
		{
			//from database
			return "1";
		}

		private async UniTask<ObjectPooler<PoolableHandler>> CreatePool(string id)
		{
			var prefab = await GetPrefab(id);
			var newPool = new ObjectPooler<PoolableHandler>();
			newPool.ObjectCreated += OnObjCreated;
			newPool.Initialize(prefab, this.transform);
			_poolDict[id] = newPool;
			return newPool;
		}

		private void OnObjCreated(ObjectPooler<PoolableHandler> pool, PoolableHandler obj)
		{
			//called when the pool is initialized or expanded and the object is instantiated
			obj.Pool = pool;

		}
		
	}
}
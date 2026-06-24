using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace magus.battle
{

    public class ObjectPoolManager : MonoBehaviour
    {
        private Dictionary<string, ObjectPooler<PoolableHandler>> _poolDict = new();

		public async UniTask Init(string[] ids)
		{
			await UniTask.WhenAll(
				ids.Select(id => CreatePool(id))
			);
		}

		public async UniTask<PoolableHandler> Allocate(string id)
		{
			PoolableHandler obj;
			if (_poolDict.TryGetValue(id, out var pool))
			{
				obj = pool.Allocate();
				//obj.SetOwner(owner);
				return obj;
			} else
			{
				var newPool = await CreatePool(id);
				obj = newPool.Allocate();

				//proj.SetOwner(owner);
			}
			return obj;
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
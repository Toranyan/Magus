using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

namespace magus.battle
{
    public class EffectManager : MonoBehaviour
    {
        [SerializeField]
        private ObjectPoolManager _poolManager;

        private readonly HashSet<PoolableHandler> _activeEffects = new();

        /// <summary>Warms the effect pools for these ids ahead of time. Optional -
        /// CreateEffect() creates a pool on demand if one doesn't exist yet.</summary>
        public async UniTask Preload(string[] ids)
        {
            await _poolManager.Preload(ids);
        }


        public async UniTask<GameObject> CreateEffect(string id, Vector3 position)
		{
            var obj = await CreateEffect(id);
            obj.transform.position = position;
            obj.gameObject.SetActive(true);
            return obj;
		}

        public async UniTask<GameObject> CreateEffect(string id)
		{
            var handle = await _poolManager.Allocate(id);
            handle.Returned += OnEffectReturned;
            _activeEffects.Add(handle);
            return handle.gameObject;
        }

        /// <summary>Force-returns every active effect to its pool (e.g. an explosion
        /// mid-animation). Used to clear the battlefield (e.g. on battle reset).</summary>
        public void ClearAll()
        {
            foreach (var handle in new List<PoolableHandler>(_activeEffects))
            {
                handle.ReturnToPool();
            }
        }

        private void OnEffectReturned(PoolableHandler handle)
        {
            handle.Returned -= OnEffectReturned;
            _activeEffects.Remove(handle);
        }

    }
}
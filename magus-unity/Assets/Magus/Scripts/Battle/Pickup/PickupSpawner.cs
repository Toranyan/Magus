using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

namespace magus.battle
{
    /// <summary>
    /// Turns PickupSpawnRequests from the Loot System into world Pickup instances.
    /// Decides spawn spread/pooling/effect playback only - never decides what drops.
    /// </summary>
    public class PickupSpawner : MonoBehaviour
    {
        [SerializeField] private ObjectPoolManager _poolManager;
        [SerializeField] private EffectManager _effectManager;

        [SerializeField]
        private float _spawnSpreadRadius = 0.5f;

        private readonly HashSet<Pickup> _activePickups = new();

        /// <summary>Warms the pickup pools for these ids ahead of time. Optional -
        /// Spawn() creates a pool on demand if one doesn't exist yet.</summary>
        public async UniTask Preload(string[] ids)
        {
            await _poolManager.Preload(ids);
        }

        public void Spawn(PickupSpawnRequest request)
        {
            SpawnAsync(request).Forget();
        }

        /// <summary>Force-returns every active pickup to its pool (no collection
        /// effect applied). Used to clear the battlefield (e.g. on battle reset).</summary>
        public void ClearAll()
        {
            foreach (var pickup in new List<Pickup>(_activePickups))
            {
                pickup.Expire();
            }
        }

        private async UniTask SpawnAsync(PickupSpawnRequest request)
        {
            if (request.Pickup == null || request.Quantity <= 0) return;

            for (int i = 0; i < request.Quantity; i++)
                await SpawnOne(request.Pickup, request.Position);
        }

        private async UniTask SpawnOne(PickupData data, Vector3 position)
        {
            var handle = await _poolManager.Allocate(data.PrefabAddressableId);
            var pickup = handle.GetComponent<Pickup>();
            if (pickup == null)
            {
                Debug.LogError($"[PickupSpawner] Prefab for '{data.PrefabAddressableId}' has no Pickup component.");
                return;
            }

            pickup.ClearEvents();
            pickup.Collected += OnPickupRemoved;
            pickup.Expired += OnPickupRemoved;
            pickup.EffectRequested += OnPickupEffectRequested;

            handle.transform.position = position + RandomSpread();
            handle.gameObject.SetActive(true);

            pickup.Initialize(data);

            _activePickups.Add(pickup);
        }

        private Vector3 RandomSpread()
        {
            var offset = Random.insideUnitCircle * _spawnSpreadRadius;
            return new Vector3(offset.x, 0f, offset.y);
        }

        private void OnPickupEffectRequested(string effectId, Vector3 position)
        {
            _effectManager?.CreateEffect(effectId, position).Forget();
        }

        private void OnPickupRemoved(Pickup pickup)
        {
            pickup.Collected -= OnPickupRemoved;
            pickup.Expired -= OnPickupRemoved;
            pickup.EffectRequested -= OnPickupEffectRequested;
            _activePickups.Remove(pickup);
        }
    }
}

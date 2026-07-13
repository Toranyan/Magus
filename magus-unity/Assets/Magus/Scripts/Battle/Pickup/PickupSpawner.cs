using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace magus.battle
{
    /// <summary>
    /// Turns PickupSpawnRequests from the Loot System into world Pickup instances.
    /// Decides spawn spread/pooling/effect playback and which prefab represents each
    /// PickupEffectKind - never decides what drops or how much.
    /// </summary>
    public class PickupSpawner : MonoBehaviour
    {
        [Serializable]
        private struct PrefabMapping
        {
            public PickupEffectKind Kind;
            public string PrefabId;
        }

        [SerializeField] private ObjectPoolManager _poolManager;
        [SerializeField] private EffectManager _effectManager;

        [SerializeField]
        private float _spawnSpreadRadius = 0.5f;

        [Tooltip("Which prefab represents each PickupEffectKind. GrantItem is not listed here - once an item system exists, an item pickup's prefab will come from the item's own type, not from this table.")]
        [SerializeField]
        private PrefabMapping[] _prefabsByKind;

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
            if (request.Effect == null || request.Quantity <= 0) return;

            if (!TryGetPrefabId(request.Effect.Kind, out var prefabId))
            {
                Debug.LogWarning($"[PickupSpawner] No prefab mapped for {request.Effect.Kind}; dropped loot is lost.");
                return;
            }

            for (int i = 0; i < request.Quantity; i++)
                await SpawnOne(prefabId, request.Effect, request.Position);
        }

        private async UniTask SpawnOne(string prefabId, PickupEffect effect, Vector3 position)
        {
            var handle = await _poolManager.Allocate(prefabId);
            var pickup = handle.GetComponent<Pickup>();
            if (pickup == null)
            {
                Debug.LogError($"[PickupSpawner] Prefab for '{prefabId}' has no Pickup component.");
                return;
            }

            pickup.ClearEvents();
            pickup.Collected += OnPickupRemoved;
            pickup.Expired += OnPickupRemoved;
            pickup.EffectRequested += OnPickupEffectRequested;

            pickup.SetEffect(effect);
            handle.transform.position = position + RandomSpread();
            handle.gameObject.SetActive(true); // triggers Pickup.OnEnable, which resets its runtime state

            _activePickups.Add(pickup);
        }

        private bool TryGetPrefabId(PickupEffectKind kind, out string prefabId)
        {
            if (_prefabsByKind != null)
            {
                foreach (var mapping in _prefabsByKind)
                {
                    if (mapping.Kind == kind)
                    {
                        prefabId = mapping.PrefabId;
                        return !string.IsNullOrEmpty(prefabId);
                    }
                }
            }

            prefabId = null;
            return false;
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

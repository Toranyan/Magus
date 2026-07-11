using System;
using UnityEngine;

namespace magus.battle
{
    /// <summary>
    /// A collectible object in the world. Sits alongside a PoolableHandler on the
    /// pickup prefab; PickupSpawner initializes it with PickupData after allocating
    /// it from the pool. Owns the idle/collect/expire lifecycle only - it does not
    /// decide what should drop (LootTable) or play effects directly (EffectManager,
    /// reached indirectly via EffectRequested the same way ProjectileBase does it).
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class Pickup : MonoBehaviour
    {
        [SerializeField]
        private SphereCollider _collectionTrigger;

        private PickupData _data;
        private PoolableHandler _handle;
        private float _aliveTime;
        private bool _collectible;

        public event Action<Pickup> Collected;
        public event Action<Pickup> Expired;

        /// <summary>Fired when a VFX/SFX prefab should play. Pickup never talks to
        /// EffectManager directly - PickupSpawner forwards this, same decoupling as
        /// ProjectileBase.EffectRequested.</summary>
        public event Action<string, Vector3> EffectRequested;

        private void Awake()
        {
            _handle = GetComponent<PoolableHandler>();
        }

        public void ClearEvents()
        {
            Collected = null;
            Expired = null;
            EffectRequested = null;
        }

        public void Initialize(PickupData data)
        {
            _data = data;
            _aliveTime = 0f;
            _collectible = data.CollectionDelay <= 0f;

            if (_collectionTrigger != null)
                _collectionTrigger.radius = data.PickupRadius;

            RequestEffect(data.SpawnVfxId);
            RequestEffect(data.SpawnSfxId);
        }

        private void Update()
        {
            if (_data == null) return;

            _aliveTime += Time.deltaTime;

            if (!_collectible && _aliveTime >= _data.CollectionDelay)
                _collectible = true;

            if (_data.Lifetime > 0f && _aliveTime >= _data.Lifetime)
                Expire();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_collectible || _data == null) return;

            var unit = other.GetComponent<Unit>();
            if (unit == null || !unit.IsAlive) return;

            Collect(unit);
        }

        private void Collect(Unit collector)
        {
            _data.Effect?.Apply(collector);

            RequestEffect(_data.CollectionVfxId);
            RequestEffect(_data.CollectionSfxId);

            Collected?.Invoke(this);
            _handle?.ReturnToPool();
        }

        /// <summary>Returns this pickup to its pool without applying its effect.
        /// Called on its own timeout, or externally (e.g. PickupSpawner.ClearAll on
        /// battle reset) to force-remove it early.</summary>
        public void Expire()
        {
            Expired?.Invoke(this);
            _handle?.ReturnToPool();
        }

        private void RequestEffect(string effectId)
        {
            if (!string.IsNullOrEmpty(effectId))
                EffectRequested?.Invoke(effectId, transform.position);
        }
    }
}

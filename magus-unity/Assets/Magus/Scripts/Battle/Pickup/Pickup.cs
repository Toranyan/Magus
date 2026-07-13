using System;
using UnityEngine;

namespace magus.battle
{
    /// <summary>
    /// A collectible object in the world. All configuration lives directly on this
    /// component so a prefab can be tuned and manually placed in a scene with no
    /// external data asset. PickupSpawner allocates/positions instances spawned from
    /// the Loot System; hand-placed instances get the same lifecycle for free since
    /// OnEnable (not an external Initialize call) resets runtime state.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class Pickup : MonoBehaviour
    {
        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _icon;
        [SerializeField] private SphereCollider _collectionTrigger;

        [SerializeField] private float _pickupRadius = 0.5f;
        [SerializeField] private float _magnetRadius = 3f;

        [Tooltip("Seconds before an uncollected pickup is returned to the pool. 0 = never expires.")]
        [SerializeField] private float _lifetime = 20f;

        [Tooltip("Seconds after spawn before the pickup can be collected.")]
        [SerializeField] private float _collectionDelay = 0.25f;

        [SerializeField] private string _spawnVfxId;
        [SerializeField] private string _collectionVfxId;
        [SerializeField] private string _spawnSfxId;
        [SerializeField] private string _collectionSfxId;

        [SerializeField] private PickupEffect _effect;

        [SerializeField]
        private PoolableHandler _handle;

        [SerializeField]
        private float _aliveTime;

        [SerializeField]
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

            if (_collectionTrigger != null)
                _collectionTrigger.radius = _pickupRadius;
        }

        private void OnEnable()
        {
            _aliveTime = 0f;
            _collectible = _collectionDelay <= 0f;

            RequestEffect(_spawnVfxId);
            RequestEffect(_spawnSfxId);
        }

        public void ClearEvents()
        {
            Collected = null;
            Expired = null;
            EffectRequested = null;
        }

        /// <summary>Overrides this instance's effect for one spawn. Called by
        /// PickupSpawner so a single prefab (e.g. "XP Orb") can grant a different
        /// amount per LootEntry. A hand-placed instance that's never spawned this
        /// way keeps using its own inspector-set default.</summary>
        public void SetEffect(PickupEffect effect)
        {
            _effect = effect;
        }

        private void Update()
        {
            _aliveTime += Time.deltaTime;

            if (!_collectible && _aliveTime >= _collectionDelay)
                _collectible = true;

            if (_lifetime > 0f && _aliveTime >= _lifetime)
                Expire();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_collectible) return;

            var unit = other.GetComponent<Unit>();
            if (unit == null || !unit.IsAlive) return;

            Collect(unit);
        }

        private void Collect(Unit collector)
        {
            _effect?.Apply(collector);

            RequestEffect(_collectionVfxId);
            RequestEffect(_collectionSfxId);

            Collected?.Invoke(this);
            ReturnOrDeactivate();
        }

        /// <summary>Returns this pickup to its pool without applying its effect.
        /// Called on its own timeout, or externally (e.g. PickupSpawner.ClearAll on
        /// battle reset) to force-remove it early.</summary>
        public void Expire()
        {
            Expired?.Invoke(this);
            ReturnOrDeactivate();
        }

        /// <summary>Pooled instances return to their pool; a hand-placed instance
        /// that was never allocated through ObjectPoolManager just deactivates.</summary>
        private void ReturnOrDeactivate()
        {
            if (_handle != null && _handle.Pool != null)
                _handle.ReturnToPool();
            else
                gameObject.SetActive(false);
        }

        private void RequestEffect(string effectId)
        {
            if (!string.IsNullOrEmpty(effectId))
                EffectRequested?.Invoke(effectId, transform.position);
        }
    }
}

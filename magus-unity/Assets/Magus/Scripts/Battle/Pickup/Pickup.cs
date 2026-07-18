using Cysharp.Threading.Tasks;
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

        [Tooltip("Radius that starts pulling an eligible collector's pickup toward it. 0 = no magnet.")]
        [SerializeField] private float _magnetRadius = 3f;

        [Tooltip("Units/second the pickup moves toward its magnet target.")]
        [SerializeField] private float _magnetSpeed = 8f;

        [Tooltip("Seconds before an uncollected pickup is returned to the pool. 0 = never expires.")]
        [SerializeField] private float _lifetime = 20f;

        [Tooltip("Seconds after spawn before the pickup can be collected.")]
        [SerializeField] private float _collectionDelay = 0.25f;

        [SerializeField] private string _spawnVfxId;
        [SerializeField] private string _collectionVfxId;
        [SerializeField] private string _spawnSfxId;
        [SerializeField] private string _collectionSfxId;

        [SerializeField] private PickupEffect _effect;

        [Tooltip("TeamId allowed to collect this pickup. Defaults to 0, the player team.")]
        [SerializeField] private int _collectorTeamId = 0;

        [SerializeField]
        private ParticleSystem[] _particleSystems;

        [SerializeField]
        private TrailRenderer[] _trailRenderers;

        [Tooltip("Objects to disable immediately on collect/expire (e.g. the main mesh) while particles/trails keep fading. Re-enabled in OnEnable for the next pooled use.")]
        [SerializeField]
        private GameObject[] _bodyObjects;

        /// <summary>Delay between the pickup being collected/expired (particles/trails stopped) and
        /// Collected/Expired firing so it can be returned to the pool. Gives trailing effects time to fade out.</summary>
        [SerializeField]
        private float _cleanupDelay = 1f;

        [SerializeField]
        private PoolableHandler _handle;

        [SerializeField]
        private float _aliveTime;

        [SerializeField]
        private bool _collectible;

        private bool _isAlive;

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
            _isAlive = true;

            ResetVisuals();

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

        /// <summary>Overrides which TeamId may collect this instance, same override
        /// pattern as SetEffect - a hand-placed instance keeps its own default.</summary>
        public void SetCollectorTeamId(int teamId)
        {
            _collectorTeamId = teamId;
        }

        private void Update()
        {
            if (!_isAlive) return;

            _aliveTime += Time.deltaTime;

            if (!_collectible && _aliveTime >= _collectionDelay)
                _collectible = true;

            if (_lifetime > 0f && _aliveTime >= _lifetime)
            {
                Expire();
                return;
            }

            if (_collectible && _magnetRadius > 0f)
                UpdateMagnet();
        }

        /// <summary>Pulls the pickup toward the nearest eligible collector within
        /// _magnetRadius, at a constant speed. Only runs once _collectible so a
        /// pickup doesn't dart away mid-spawn-delay; actual collection still happens
        /// through OnTriggerEnter once it arrives.</summary>
        private void UpdateMagnet()
        {
            var target = FindMagnetTarget();
            if (target == null) return;

            transform.position = Vector3.MoveTowards(transform.position, target.transform.position, _magnetSpeed * Time.deltaTime);
        }

        private Unit FindMagnetTarget()
        {
            var mask = LayerMask.GetMask("Character");
            var hits = Physics.OverlapSphere(transform.position, _magnetRadius, mask);

            Unit closest = null;
            float closestDistSqr = float.MaxValue;

            foreach (var hit in hits)
            {
                var unit = hit.GetComponent<Unit>();
                if (unit == null || !unit.IsAlive || unit.TeamId != _collectorTeamId)
                    continue;

                float distSqr = (unit.transform.position - transform.position).sqrMagnitude;
                if (distSqr < closestDistSqr)
                {
                    closest = unit;
                    closestDistSqr = distSqr;
                }
            }

            return closest;
        }

        private void OnTriggerEnter(Collider other)
        {
			OnTriggerEnterOrStay(other);
		}

        private void OnTriggerStay(Collider other)
        {
            OnTriggerEnterOrStay(other);
		}

        private void OnTriggerEnterOrStay(Collider other)
        {
			if (!_collectible) return;

			var unit = other.GetComponent<Unit>();
			if (unit == null || !unit.IsAlive || unit.TeamId != _collectorTeamId) return;

			Collect(unit);
		}

		private void Collect(Unit collector)
        {
            if (!_isAlive) return;
            _isAlive = false;
            _collectible = false;

            _effect?.Apply(collector);

            RequestEffect(_collectionVfxId);
            RequestEffect(_collectionSfxId);

            Finish(wasCollected: true, immediate: false);
        }

        /// <summary>Stops the pickup and lets its particles fade out; Expired fires after
        /// _cleanupDelay so the pool reclaim doesn't cut off trailing particles. Called on
        /// its own timeout, or externally (e.g. PickupSpawner.ClearAll on battle reset) with
        /// immediate=true to skip the delay and force-remove it right away.</summary>
        public void Expire(bool immediate = false)
        {
            if (!_isAlive) return;
            _isAlive = false;
            _collectible = false;

            Finish(wasCollected: false, immediate);
        }

        private void Finish(bool wasCollected, bool immediate)
        {
            DisableParticles();
            DisableTrails();
            DisableBody();

            if (immediate || _cleanupDelay <= 0f)
            {
                FireOutcome(wasCollected);
                FinalizeCleanup();
            }
            else
            {
                WaitAndFinish(wasCollected).Forget();
            }
        }

        private async UniTaskVoid WaitAndFinish(bool wasCollected)
        {
            await UniTask.Delay(
                TimeSpan.FromSeconds(_cleanupDelay),
                cancellationToken: this.GetCancellationTokenOnDestroy()
            );

            FireOutcome(wasCollected);
            FinalizeCleanup();
        }

        private void FireOutcome(bool wasCollected)
        {
            if (wasCollected)
                Collected?.Invoke(this);
            else
                Expired?.Invoke(this);
        }

        /// <summary>Wipes any trail data that hasn't faded out on its own (e.g. a
        /// TrailRenderer.time longer than _cleanupDelay) before returning to the pool,
        /// so the next reuse doesn't spawn with a stale trail attached.</summary>
        private void FinalizeCleanup()
        {
            ClearTrails();
            ReturnOrDeactivate();
        }

        /// <summary>Restores particles/trails/body to their live-and-visible state.
        /// Called on (re)spawn so a pickup pulled fresh from the pool doesn't come back
        /// with the stopped/hidden state left over from its previous collect/expire.</summary>
        private void ResetVisuals()
        {
            EnableParticles();
            EnableTrails();
            EnableBody();
        }

        private void DisableParticles()
        {
            if (_particleSystems == null) return;

            foreach (var ps in _particleSystems)
            {
                if (ps == null) continue;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        private void EnableParticles()
        {
            if (_particleSystems == null) return;

            foreach (var ps in _particleSystems)
            {
                if (ps == null) continue;
                ps.Play(true);
            }
        }

        private void DisableTrails()
        {
            if (_trailRenderers == null) return;

            foreach (var trail in _trailRenderers)
            {
                if (trail == null) continue;
                trail.emitting = false;
            }
        }

        private void EnableTrails()
        {
            if (_trailRenderers == null) return;

            foreach (var trail in _trailRenderers)
            {
                if (trail == null) continue;
                trail.emitting = true;
            }
        }

        private void ClearTrails()
        {
            if (_trailRenderers == null) return;

            foreach (var trail in _trailRenderers)
            {
                if (trail == null) continue;
                trail.Clear();
            }
        }

        private void DisableBody()
        {
            if (_bodyObjects == null) return;

            foreach (var go in _bodyObjects)
            {
                if (go == null) continue;
                go.SetActive(false);
            }
        }

        private void EnableBody()
        {
            if (_bodyObjects == null) return;

            foreach (var go in _bodyObjects)
            {
                if (go == null) continue;
                go.SetActive(true);
            }
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

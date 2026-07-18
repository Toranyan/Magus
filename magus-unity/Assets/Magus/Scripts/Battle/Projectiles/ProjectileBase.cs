using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace magus.battle
{
    /// <summary>
    /// Handles projectile movement and lifetime.
    /// Damage delivery is handled by the attached DamageDealer.
    /// Collision detection is handled here and forwarded to DamageDealer.
    /// </summary>
    public class ProjectileBase : MonoBehaviour
    {
        [SerializeField]
        private float _lifeTime = 10f;

        [SerializeField]
        private string _deathEffectId;

        [SerializeField]
        private bool _killOnCollision = true;

        [SerializeField]
        private DamageDealer _damageDealer;

        [SerializeField]
        private ParticleSystem[] _particleSystems;

        [SerializeField]
        private TrailRenderer[] _trailRenderers;

        [Tooltip("Objects to disable immediately on kill (e.g. the main mesh) while particles/trails keep fading. Re-enabled in Setup for the next pooled use.")]
        [SerializeField]
        private GameObject[] _bodyObjects;

        /// <summary>Delay between the projectile being disabled (particles/trails stopped) and
        /// Killed firing so it can be returned to the pool. Gives trailing effects time to fade out.</summary>
        [SerializeField]
        private float _cleanupDelay = 2f;

        private Vector3 _velocity;
        private bool _isAlive;
        private float _aliveTime;

        public DamageDealer DamageDealer => _damageDealer;

        public bool IsKillOnCollide
        {
            get => _killOnCollision;
            set => _killOnCollision = value;
        }

        public event Action<ProjectileBase> Killed;

        /// <summary>
        /// Fired when the projectile dies so external services (e.g. EffectManager) can spawn the effect.
        /// Gameplay code should not directly spawn visual effects.
        /// </summary>
        public event Action<string, Vector3> EffectRequested;

        public void SetOwner(IBattleEntity owner)
        {
            _damageDealer?.SetOwner(owner);
        }

        public void Setup(Vector3 initialVelocity)
        {
            _velocity = initialVelocity;
            _isAlive = true;
            _aliveTime = 0f;
            _damageDealer?.BeginAttack();

            ResetVisuals();
        }

        public void ClearEvents()
        {
            Killed = null;
            EffectRequested = null;
        }

        /// <summary>Stops the projectile and lets it fade out; Killed fires after
        /// _cleanupDelay so the pool reclaim doesn't cut off trailing particles.
        /// Pass immediate=true (e.g. battlefield reset) to skip the delay and fire Killed right away.</summary>
        public void Kill(bool immediate = false)
        {
            if (!_isAlive) return;
            _isAlive = false;
            _damageDealer?.EndAttack();
            _velocity = Vector3.zero;

			DisableParticles();
            DisableTrails();
            DisableBody();

            if (!string.IsNullOrEmpty(_deathEffectId))
            {
                EffectRequested?.Invoke(_deathEffectId, transform.position);
            }

            if (immediate)
            {
                ClearTrails();
                Killed?.Invoke(this);
            }
            else
            {
                WaitAndFireKilled().Forget();
            }
        }

        /// <summary>Restores particles/trails/body to their live-and-visible state.
        /// Called on (re)spawn so a projectile pulled fresh from the pool doesn't come
        /// back with the stopped/hidden state left over from its previous kill.</summary>
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

        /// <summary>Wipes any trail data that hasn't faded out on its own (e.g. a
        /// TrailRenderer.time longer than _cleanupDelay) before the projectile is
        /// returned to the pool, so the next reuse doesn't spawn with a stale trail attached.</summary>
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

        private async UniTaskVoid WaitAndFireKilled()
        {
            await UniTask.Delay(
                TimeSpan.FromSeconds(_cleanupDelay),
                cancellationToken: this.GetCancellationTokenOnDestroy()
            );

            ClearTrails();
            Killed?.Invoke(this);
        }

        private void Update()
        {
            if (!_isAlive) return;

            _aliveTime += Time.deltaTime;
            if (_aliveTime >= _lifeTime)
            {
                Kill();
                return;
            }

            transform.position += _velocity * Time.deltaTime;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isAlive) return;

            var receiver = other.GetComponent<DamageReceiver>();
            if (receiver == null) return;

            bool hit = _damageDealer != null
                ? _damageDealer.TryDamage(receiver, transform.position, -transform.forward)
                : false;

            if (hit && _killOnCollision)
            {
                Kill();
            }
        }
    }
}

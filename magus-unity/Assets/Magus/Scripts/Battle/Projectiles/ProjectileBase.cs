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

        /// <summary>Delay between the projectile being disabled (particles stopped) and
        /// Killed firing so it can be returned to the pool. Gives trailing particles time to fade out.</summary>
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

            if (!string.IsNullOrEmpty(_deathEffectId))
            {
                EffectRequested?.Invoke(_deathEffectId, transform.position);
            }

            if (immediate)
            {
                Killed?.Invoke(this);
            }
            else
            {
                WaitAndFireKilled().Forget();
            }
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

        private async UniTaskVoid WaitAndFireKilled()
        {
            await UniTask.Delay(
                TimeSpan.FromSeconds(_cleanupDelay),
                cancellationToken: this.GetCancellationTokenOnDestroy()
            );

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

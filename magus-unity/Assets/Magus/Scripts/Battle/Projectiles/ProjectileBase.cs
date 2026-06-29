using System;
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

        public void Kill()
        {
            if (!_isAlive) return;
            _isAlive = false;
            _damageDealer?.EndAttack();
            gameObject.SetActive(false);

            if (!string.IsNullOrEmpty(_deathEffectId))
            {
                EffectRequested?.Invoke(_deathEffectId, transform.position);
            }

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

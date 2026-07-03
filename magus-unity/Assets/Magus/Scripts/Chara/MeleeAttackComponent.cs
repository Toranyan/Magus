using UnityEngine;
using magus.battle;
using Cysharp.Threading.Tasks;
using System;

namespace magus.chara
{
    public class MeleeAttackComponent : MonoBehaviour, IUnitAttack
    {
        [SerializeField] private float _attackDelay;
        [SerializeField] private float _attackCooldown;
        [SerializeField] private float _attackRange;
        [SerializeField] private float _hitWindowDuration;

        /// <summary>
        /// The trigger collider that defines the hit volume.
        /// Must be on the same GameObject as this component so OnTriggerEnter fires here.
        /// For child hitboxes, add a ColliderEventForwarder on the child instead.
        /// </summary>
        [SerializeField] private Collider _hitCollider;
        [SerializeField] private DamageDealer _damageDealer;

        public float Range => _attackRange;
        public bool CanAttack => _cooldownTimer >= _attackCooldown;

        private Unit _owner;
        private float _cooldownTimer;

        private void Awake()
        {
            _hitCollider.enabled = false;
        }

        private void Update()
        {
            if (_cooldownTimer < _attackCooldown)
                _cooldownTimer += Time.deltaTime;
        }

        public void SetOwner(Unit owner)
        {
            _owner = owner;
            _damageDealer.SetOwner(owner);
        }

        public void Attack(Vector3 targetPosition)
        {
            if (!CanAttack) return;
            AttackAsync().Forget();
        }

        private async UniTaskVoid AttackAsync()
        {
            _cooldownTimer = 0f;

            if (_attackDelay > 0f)
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(_attackDelay),
                    cancellationToken: this.GetCancellationTokenOnDestroy()
                );
            }

            if (_owner == null || !_owner.IsAlive) return;

            _damageDealer.BeginAttack();
            _hitCollider.enabled = true;

            await UniTask.Delay(
                TimeSpan.FromSeconds(_hitWindowDuration),
                cancellationToken: this.GetCancellationTokenOnDestroy()
            );

            _hitCollider.enabled = false;
            _damageDealer.EndAttack();
        }

        private void OnTriggerEnter(Collider other)
        {
            var receiver = other.GetComponent<DamageReceiver>();
            if (receiver != null)
                _damageDealer.TryDamage(receiver, other.ClosestPoint(transform.position));
        }
    }
}

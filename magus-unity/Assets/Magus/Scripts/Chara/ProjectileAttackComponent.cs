using UnityEngine;
using magus.battle;
using Cysharp.Threading.Tasks;
using System;

namespace magus.chara
{
    public class ProjectileAttackComponent : MonoBehaviour, IUnitAttack
    {
        [SerializeField] private float _attackDelay;
        [SerializeField] private float _attackCooldown;
        [SerializeField] private float _attackRange;
        [SerializeField] private float _initialSpeed = 1f;
        [SerializeField] private bool _lockY;
        [SerializeField] private string _projectileId;
        [SerializeField] private Transform _spawnPosition;

        public float Range => _attackRange;
        public bool CanAttack => _cooldownTimer >= _attackCooldown;

        private Unit _owner;
        private float _cooldownTimer;

        public void SetOwner(Unit owner)
        {
            _owner = owner;
        }

        public void Attack(Vector3 targetPosition)
        {
            if (!CanAttack) return;
            FireAsync(targetPosition).Forget();
        }

        private void Update()
        {
            if (_cooldownTimer < _attackCooldown)
            {
                _cooldownTimer += Time.deltaTime;
            }
        }

        private async UniTaskVoid FireAsync(Vector3 targetPosition)
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

            var proj = await BattleController.Instance.ProjectileManager.CreateProjectile(_projectileId, _owner);
            proj.transform.SetParent(null);
            proj.transform.position = _spawnPosition.position;

            var dir = targetPosition - _spawnPosition.position;
            if (_lockY) dir.y = 0f;
            proj.Setup(dir.normalized * _initialSpeed);
        }
    }
}

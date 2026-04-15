using UnityEngine;
using magus.battle;
using Cysharp.Threading.Tasks;
using System;

namespace magus.chara
{

    public class ProjectileAttackComponent : MonoBehaviour
    {

        [SerializeField]
        private float _attackDelay;

        [SerializeField]
        private float _attackCooldown;

        [SerializeField]
        private float _attackRange;

        [SerializeField]
        private float _attackRecoilTime;

        [SerializeField]
        private string _projectileId;

        [SerializeField]
        private string _launchEffectId;

        [SerializeField]
        private float _initialSpeed = 1;

        [SerializeField]
        private Transform _spawnPosition;

        [SerializeField]
        private bool _lockY;

        public float AttackDelay => _attackDelay;
        public float AttackRange => _attackRange;

        public bool CanAttack => _timeSinceLastAttack >= _attackCooldown;

        public bool IsRecoiling => _timeSinceLastAttack < _attackRecoilTime;


        private double _timeSinceLastAttack;

        private IBattleEntity _owner;

        private string _projectileAdressableId;

        private bool _isInitialized = false;

        public void Awake()
		{
            Initialize();
        }

		public void Initialize()
		{
            if (_isInitialized)
			{
                return;
			}

            _projectileAdressableId = _projectileId;

            _isInitialized = true;

        }

		public void Setup(IBattleEntity owner)
		{
            _owner = owner;
		}

		private void Update()
		{
            //TODO unless paused 
            _timeSinceLastAttack += Time.deltaTime;
		}

		public void AttackTarget(GameCharaController target)
		{
            if(_timeSinceLastAttack < _attackCooldown)
			{
                //in cooldown
                return;
			}

            if (target == null)
			{
                AttackNoTarget().Forget();
			} else
			{
                AttackTargetAsync(target).Forget();
            }
		}

        private async UniTask AttackNoTarget()
		{
            _timeSinceLastAttack = 0;
            await UniTask.Delay(TimeSpan.FromSeconds(_attackDelay));

            //check if still alive?

            //make proj
            var proj = await SpawnProjectile();

            var initVec = transform.forward;
            initVec = initVec.normalized * _initialSpeed;

            proj.Setup(initVec);
        }

        private async UniTask AttackTargetAsync(GameCharaController target)
		{
            _timeSinceLastAttack = 0;
            await UniTask.Delay(TimeSpan.FromSeconds(_attackDelay));

            //check if still alive?

            //make proj
            var proj = await SpawnProjectile();

            var initVec = target.transform.position - _spawnPosition.position;
            if (_lockY)
			{
                initVec.y = 0;
			}
            initVec = initVec.normalized * _initialSpeed;

            proj.Setup(initVec);
		}

        private async UniTask<ProjectileBase> SpawnProjectile()
		{
            //make sure id is initialized
            Initialize();

            var proj = await BattleController.Instance.ProjectileManager.CreateProjectile(_projectileAdressableId, _owner);
            proj.transform.SetParent(null);
            proj.transform.position = _spawnPosition.position;
            return proj;
		}

        
    }
}
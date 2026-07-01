using magus.battle;
using UnityEngine;
using System;

namespace magus.chara
{
    public class EnemyController : MonoBehaviour
    {
        [SerializeField] private Unit _unit;
        [SerializeField] private GameCharaController _charaController;
        [SerializeField] private float _detectRange;
        [SerializeField] private MonoBehaviour _attackBehaviour;

        private IUnitAttack _attack;
        private Unit _target;
        private State _state;

        public event Action<EnemyController> Killed;

        private enum State { Idle, Chase, Attack }

        private void Awake()
        {
            _attack = _attackBehaviour as IUnitAttack;
            if (_attack == null)
                Debug.LogError($"[EnemyController] {name}: _attackBehaviour does not implement IUnitAttack.");

            _unit.Killed += OnUnitKilled;
            _charaController.DeathAnimationFinished += OnDeathAnimationFinished;
        }

        private void OnEnable()
        {
            Setup();
        }

        private void Setup()
        {
            _unit.Setup();
            _charaController.Setup();
            _attack?.SetOwner(_unit);
            _target = null;
            _state = State.Idle;
        }

        private void Update()
        {
            if (!_unit.IsAlive) return;

            switch (_state)
            {
                case State.Idle:   UpdateIdle();   break;
                case State.Chase:  UpdateChase();  break;
                case State.Attack: UpdateAttack(); break;
            }
        }

        private void UpdateIdle()
        {
            _charaController.SetMoveVector(Vector3.zero);
            _target = FindTarget();
            if (_target != null)
                SetState(State.Chase);
        }

        private void UpdateChase()
        {
            if (!IsTargetValid())
            {
                SetState(State.Idle);
                return;
            }

            float dist = Vector3.Distance(_target.transform.position, transform.position);
            if (dist <= _attack.Range)
            {
                SetState(State.Attack);
                return;
            }

            _charaController.SetMoveVector(_target.transform.position - transform.position);
        }

        private void UpdateAttack()
        {
            if (!IsTargetValid())
            {
                SetState(State.Idle);
                return;
            }

            float dist = Vector3.Distance(_target.transform.position, transform.position);
            if (dist > _attack.Range)
            {
                SetState(State.Chase);
                return;
            }

            _charaController.SetMoveVector(Vector3.zero);
            _attack?.Attack(_target.transform.position);
        }

        private void SetState(State state)
        {
            _state = state;
        }

        private bool IsTargetValid()
        {
            return _target != null && _target.IsAlive;
        }

        private Unit FindTarget()
        {
            var mask = LayerMask.GetMask("Character");
            var colliders = Physics.OverlapSphere(transform.position, _detectRange, mask);

            Unit closest = null;
            float closestDistSqr = float.MaxValue;

            foreach (var col in colliders)
            {
                var unit = col.GetComponent<Unit>();
                if (unit == null || unit == _unit || unit.TeamId == _unit.TeamId || !unit.IsAlive)
                    continue;

                float distSqr = (col.transform.position - transform.position).sqrMagnitude;
                if (distSqr < closestDistSqr)
                {
                    closest = unit;
                    closestDistSqr = distSqr;
                }
            }

            return closest;
        }

        private void OnUnitKilled()
        {
            _charaController.StartDeathAnimation();
            Killed?.Invoke(this);
        }

        private void OnDeathAnimationFinished()
        {
            var handle = GetComponent<PoolableHandler>();
            if (handle)
                handle.ReturnToPool();
            gameObject.SetActive(false);
        }
    }
}

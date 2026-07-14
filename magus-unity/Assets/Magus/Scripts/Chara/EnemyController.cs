using magus.battle;
using UnityEngine;
using System;

namespace magus.chara
{
    public class EnemyController : MonoBehaviour
    {
        [SerializeField] private GameCharaController _charaController;
        [SerializeField] private float _detectRange;

        [Header("Idle Wander")]
        [SerializeField] private float _wanderRadius = 3f;
        [SerializeField] private float _wanderIntervalMin = 2f;
        [SerializeField] private float _wanderIntervalMax = 5f;

        private Unit _target;
        private State _state;

        private Vector3 _wanderOrigin;
        private Vector3 _wanderTarget;
        private float _nextWanderPickTime;

        public event Action<EnemyController> Killed;

        private Unit Unit => _charaController.Unit;
        private IUnitAttack Attack => _charaController.Attack;

        private enum State { Idle, Chase, Attack }

        private void Awake()
        {
            _charaController.Killed += OnUnitKilled;
            _charaController.DeathAnimationFinished += OnDeathAnimationFinished;
        }

        private void OnEnable()
        {
            Setup();
        }

        private void Setup()
        {
            _charaController.Setup();
            _target = null;
            _state = State.Idle;

            _wanderOrigin = transform.position;
            _nextWanderPickTime = Time.time;
        }

        private void Update()
        {
            if (!Unit.IsAlive) return;

            switch (_state)
            {
                case State.Idle:   UpdateIdle();   break;
                case State.Chase:  UpdateChase();  break;
                case State.Attack: UpdateAttack(); break;
            }
        }

        private void UpdateIdle()
        {
            _target = FindTarget();
            if (_target != null)
            {
                SetState(State.Chase);
                return;
            }

            if (Time.time >= _nextWanderPickTime)
                PickNewWanderTarget();

            _charaController.MoveToPosition(_wanderTarget);
        }

        private void PickNewWanderTarget()
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle * _wanderRadius;
            _wanderTarget = _wanderOrigin + new Vector3(offset.x, 0f, offset.y);
            _nextWanderPickTime = Time.time + UnityEngine.Random.Range(_wanderIntervalMin, _wanderIntervalMax);
        }

        private void UpdateChase()
        {
            if (!IsTargetValid())
            {
                SetState(State.Idle);
                return;
            }

            float dist = Vector3.Distance(_target.transform.position, transform.position);
            if (dist <= Attack.Range)
            {
                SetState(State.Attack);
                return;
            }

            _charaController.MoveToPosition(_target.transform.position, Attack.Range, _target.transform);
        }

        private void UpdateAttack()
        {
            if (!IsTargetValid())
            {
                SetState(State.Idle);
                return;
            }

            float dist = Vector3.Distance(_target.transform.position, transform.position);
            if (dist > Attack.Range)
            {
                SetState(State.Chase);
                return;
            }

            _charaController.SetMoveVector(Vector3.zero);
            Attack?.Attack(_target.transform.position);
        }

        private void SetState(State state)
        {
            if (state == State.Idle && _state != State.Idle)
                _nextWanderPickTime = Time.time;

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
                if (unit == null || unit == Unit || unit.TeamId == Unit.TeamId || !unit.IsAlive)
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

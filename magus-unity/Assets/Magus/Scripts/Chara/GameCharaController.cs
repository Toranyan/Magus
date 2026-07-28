using UnityEngine;
using System;
using Cysharp.Threading.Tasks;
using magus.battle;

namespace magus.chara
{
    /// <summary>
    /// Combines a Unit (combat data) with a ModelController (presentation).
    /// Owns the death sequencing: triggers the death animation when the Unit
    /// is killed, then notifies listeners once the animation finishes.
    /// </summary>
    public class GameCharaController : MonoBehaviour
    {
        [SerializeField] private Unit _unit;
        [SerializeField] private ModelController _modelController;
        [SerializeField] private MonoBehaviour _attackBehaviour;

        [Header("Obstacle Avoidance")]
        [SerializeField] private float _obstacleCheckRadius = 0.4f;
        [SerializeField] private float _obstacleCheckDistance = 1.5f;
        [SerializeField] private float _obstacleCheckHeight = 1f;
        [SerializeField] private float _obstacleAvoidAngleStep = 20f;
        [SerializeField] private float _obstacleAvoidMaxAngle = 160f;

        private IUnitAttack _attack;
        private int _obstacleLayerMask;
        private float _avoidSign;

        public Unit Unit => _unit;
        public IUnitAttack Attack => _attack;

        public event Action Killed;
        public event Action DeathAnimationFinished;

        private void Awake()
        {
            _attack = _attackBehaviour as IUnitAttack;
            if (_attack == null)
                Debug.LogError($"[GameCharaController] {name}: _attackBehaviour does not implement IUnitAttack.");

            _unit.Killed += HandleUnitKilled;
            _modelController.DeathAnimationFinished += OnModelDeathAnimationFinished;
            _modelController.DeathEffectRequested += OnDeathEffectRequested;

            _obstacleLayerMask = LayerMask.GetMask("Character", "Map", "Doodad");
        }

        public void Setup()
        {
            _unit.Setup();
            _modelController.Setup();
            _modelController.SetOwner(_unit);
            _attack?.SetOwner(_unit);
        }

        public void SetMoveVector(Vector3 moveVec)
        {
            _modelController.SetMoveVector(moveVec);
        }

        /// <summary>
        /// Moves toward a target position on the horizontal plane. Returns true once
        /// within stoppingDistance, at which point movement is stopped. ignoreTarget
        /// excludes a specific transform's colliders from obstacle checks - use this
        /// when the destination is itself a unit (e.g. a chase target), so it isn't
        /// treated as an obstacle to dodge around.
        /// </summary>
        public bool MoveToPosition(Vector3 targetPosition, float stoppingDistance = 0.1f, Transform ignoreTarget = null)
        {
            Vector3 toTarget = targetPosition - transform.position;
            toTarget.y = 0f;

            if (toTarget.magnitude <= stoppingDistance)
            {
                SetMoveVector(Vector3.zero);
                return true;
            }

            Vector3 direction = FindClearDirection(toTarget.normalized, ignoreTarget);
            SetMoveVector(direction);
            return false;
        }

        /// <summary>
        /// Returns desiredDirection if unobstructed, otherwise fans out left/right in
        /// increasing angle steps until a clear direction is found. Falls back to
        /// Vector3.zero (stand still) if fully boxed in. Keeps steering to the same
        /// side as the previous frame when possible, to avoid oscillating between
        /// left and right options each frame.
        /// </summary>
        private Vector3 FindClearDirection(Vector3 desiredDirection, Transform ignoreTarget)
        {
            if (!IsObstructed(desiredDirection, ignoreTarget))
            {
                _avoidSign = 0f;
                return desiredDirection;
            }

            float preferredSign = _avoidSign != 0f ? _avoidSign : 1f;
            float otherSign = -preferredSign;

            for (float angle = _obstacleAvoidAngleStep; angle <= _obstacleAvoidMaxAngle; angle += _obstacleAvoidAngleStep)
            {
                Vector3 preferredOption = Quaternion.Euler(0f, angle * preferredSign, 0f) * desiredDirection;
                if (!IsObstructed(preferredOption, ignoreTarget))
                {
                    _avoidSign = preferredSign;
                    return preferredOption;
                }

                Vector3 otherOption = Quaternion.Euler(0f, angle * otherSign, 0f) * desiredDirection;
                if (!IsObstructed(otherOption, ignoreTarget))
                {
                    _avoidSign = otherSign;
                    return otherOption;
                }
            }

            return Vector3.zero;
        }

        private bool IsObstructed(Vector3 direction, Transform ignoreTarget)
        {
            Vector3 origin = transform.position + Vector3.up * _obstacleCheckHeight;
            var hits = Physics.SphereCastAll(origin, _obstacleCheckRadius, direction, _obstacleCheckDistance, _obstacleLayerMask, QueryTriggerInteraction.Ignore);

            foreach (var hit in hits)
            {
                if (hit.collider.transform.IsChildOf(transform))
                    continue;
                if (ignoreTarget != null && hit.collider.transform.IsChildOf(ignoreTarget))
                    continue;
                return true;
            }

            return false;
        }

        private void HandleUnitKilled()
        {
            _modelController.SetMoveVector(Vector3.zero);
            _modelController.StartDeathAnimation();
            Killed?.Invoke();
        }

        private void OnModelDeathAnimationFinished()
        {
            DeathAnimationFinished?.Invoke();
        }

        private void OnDeathEffectRequested(string effectId, Vector3 position)
        {
            BattleController.Instance.EffectManager.CreateEffect(effectId, position).Forget();
        }
    }
}

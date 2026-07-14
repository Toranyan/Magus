using UnityEngine;
using System;
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

        private IUnitAttack _attack;

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
        }

        public void Setup()
        {
            _unit.Setup();
            _modelController.Setup();
            _attack?.SetOwner(_unit);
        }

        public void SetMoveVector(Vector3 moveVec)
        {
            _modelController.SetMoveVector(moveVec);
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
    }
}

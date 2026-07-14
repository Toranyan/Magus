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

        public Unit Unit => _unit;

        public event Action Killed;
        public event Action DeathAnimationFinished;

        private void Awake()
        {
            _unit.Killed += HandleUnitKilled;
            _modelController.DeathAnimationFinished += OnModelDeathAnimationFinished;
        }

        public void Setup()
        {
            _unit.Setup();
            _modelController.Setup();
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

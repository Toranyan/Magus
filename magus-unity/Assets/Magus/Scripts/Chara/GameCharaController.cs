using UnityEngine;
using System;

namespace magus.chara
{
    /// <summary>
    /// Bridges the controller layer to ModelController.
    /// Responsible for movement direction and death animation sequencing only.
    /// Does not know about Unit, combat, or teams.
    /// </summary>
    public class GameCharaController : MonoBehaviour
    {
        [SerializeField] private ModelController _modelController;

        public event Action DeathAnimationFinished;

        private void Awake()
        {
            _modelController.DeathAnimationFinished += OnDeathAnimationFinished;
        }

        public void Setup()
        {
            _modelController.Setup();
        }

        public void SetMoveVector(Vector3 moveVec)
        {
            _modelController.SetMoveVector(moveVec);
        }

        public void StartDeathAnimation()
        {
            _modelController.StartDeathAnimation();
        }

        private void OnDeathAnimationFinished()
        {
            DeathAnimationFinished?.Invoke();
        }
    }
}

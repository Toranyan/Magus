using UnityEngine;
using magus.battle;
using System;

namespace magus.chara
{
    public class GameCharaController : MonoBehaviour, IBattleEntity
    {
        [SerializeField]
        private ModelController _modelController;

        [SerializeField]
        private Unit _unit;

        [SerializeField]
        private DamageReceiver _damageReceiver;

        public float DetectRange => _unit != null ? _detectRange : 0f;

        [SerializeField]
        private float _detectRange;

        public int TeamId => _unit != null ? _unit.TeamId : 0;

        public GameObject GameObject => this.gameObject;

        public bool IsAlive => _unit != null && _unit.IsAlive;

        public event Action Killed;

        private bool _isInitialized;

        public void Init()
        {
            if (_isInitialized) return;

            _unit.Killed += OnUnitKilled;
            _modelController.DeathAnimationFinished += OnDeathAnimationFinished;

            _isInitialized = true;
        }

        public void Setup()
        {
            Killed = null;
            _modelController.Setup();
            _damageReceiver.ClearEvents();
            _unit.Setup();
        }

        public void Kill()
        {
            Debug.Log($"{name} killed");
            Killed?.Invoke();
            _modelController.StartDeathAnimation();
        }

        public void SetMoveVector(Vector3 moveVec)
        {
            _modelController.SetMoveVector(moveVec);
        }

        private void OnUnitKilled()
        {
            Kill();
        }

        private void OnDeathAnimationFinished()
        {
            var handle = GetComponent<PoolableHandler>();
            if (handle)
            {
                handle.ReturnToPool();
                gameObject.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}

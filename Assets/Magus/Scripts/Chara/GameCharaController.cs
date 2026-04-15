using System.Collections;
using System.Collections.Generic;
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
        private int _teamId;

        [SerializeField]
        private DamageReceiver _damageReceiver;

        [SerializeField]
        private float _initalHealth;

        [SerializeField]
        private float _detectRange;

        public float Health => _health;
        public float DetectRange => _detectRange;

        public bool IsAttackReady => _isAttackReady;

        public int TeamId => _teamId;

        public GameObject GameObject => this.gameObject;

		public bool IsAlive => _isAlive;

        public event Action Killed;


        private float _health;
        private bool _isAlive = true;

        private bool _isInitialized = false;

        private bool _isAttackReady;

        public void Init()
		{
            if (_isInitialized)
			{
                return;
			}
            _damageReceiver.DamageReceived += OnDamageReceived;

            _modelController.DeathAnimationFinished += OnDeathAnimationFinished;


            _isInitialized = true;
		}

        public void Setup()
		{
            Killed = null;
            _modelController.Setup();
            _damageReceiver.Setup(TeamId);
            _health = _initalHealth;
            _isAlive = true;
		}

        public void Kill()
		{
            Debug.Log($"{name} killed");
            _isAlive = false;
            
            
            Killed?.Invoke();


            _modelController.StartDeathAnimation();
		}

        public void SetMoveVector(Vector3 moveVec)
		{
            _modelController.SetMoveVector(moveVec);
		}

        private void OnDamageReceived(DamageInfo info)
		{
            _health -= info.Amount;
            if (_health <= 0)
			{
                Kill();
			}
		}

        private void AttackTarget (GameCharaController target)
		{

		}

        private void OnDeathAnimationFinished()
		{
            //TODO is there a better place to return to pool
            var handle = GetComponent<PoolableHandler>();

            if (handle)
			{
                handle.ReturnToPool();
                gameObject.SetActive(false);
            } else
			{
                //not a poolable?
                //just disable

                gameObject.SetActive(false);
			}
            
		}


    }
}
using magus.battle;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace magus.chara
{
    public class EnemyController : MonoBehaviour
    {
		[SerializeField]
		private GameCharaController _charaController;

		[SerializeField]
		private ProjectileAttackComponent _projAttackComponent;

		private bool _isInitialized = false;

		private GameCharaController _targetChara;

		public Action<EnemyController> Killed;

		private enum State
		{
			Idle,
			Follow,
			Attack,
		}

		public void Awake()
		{
			Init();
		}

		public void Init()
		{
			if(_isInitialized)
			{
				return;
			}
			_charaController.Init();
			_isInitialized = true;
		}

		public void OnEnable()
		{
			//TODO manual setup
			Setup();
		}

		public void Setup()
		{
			_charaController.Setup();
			_charaController.Killed += OnKilled;
			_projAttackComponent.Setup(_charaController);
		}

		private void Update()
		{
			if (!_charaController.IsAlive)
			{
				return;
			}

			if (_targetChara == null)
			{
				_charaController.SetMoveVector(Vector3.zero);
				FindTarget();
			} else
			{
				var dist = Vector3.Distance(_targetChara.transform.position, transform.position);
				if (dist <= _projAttackComponent.AttackRange)
				{
					_charaController.SetMoveVector(Vector3.zero);
					AttackTarget(_targetChara);
				} else if (_projAttackComponent.IsRecoiling)
				{
					//Unable to move
				} else 
				{
					MoveTowardTarget(_targetChara.transform.position);
				}
			}

		}

		private void MoveTowardTarget(Vector3 targetPos)
		{
			var vecDelta = targetPos - transform.position;
			_charaController.SetMoveVector(vecDelta);
		}


		private void FindTarget()
		{
			var layer = LayerMask.NameToLayer("Character");
			var mask = 1 << layer;
			var colliders = Physics.OverlapSphere(transform.position, _charaController.DetectRange, mask);

			//get closest
			var closestDist = _targetChara ? Vector3.Distance(_targetChara.transform.position, transform.position) : float.MaxValue;
			foreach (var col in colliders)
			{
				var chara = col.GetComponent<GameCharaController>();
				if (chara)
				{
					if (chara.TeamId != _charaController.TeamId)
					{
						if (_targetChara == null)
						{
							_targetChara = chara;
							closestDist = Vector3.Distance(_targetChara.transform.position, transform.position);
						} else
						{
							var dist = Vector3.Distance(chara.transform.position, transform.position);

							if (dist < closestDist)
							{
								_targetChara = chara;
								closestDist = Vector3.Distance(_targetChara.transform.position, transform.position);
							}
						}
					}
				}
			}

		}

		private void Attack()
		{
			if (_targetChara != null)
			{
				AttackTarget(_targetChara);
			} else
			{
				AttackEmpty();
			}
		}

		private void AttackTarget(GameCharaController chara)
		{
			var attackComponent = GetComponent<ProjectileAttackComponent>();

			attackComponent.AttackTarget(chara);

		}

		/// <summary>
		/// Attack with no target
		/// </summary>
		private void AttackEmpty()
		{

		}

		private void OnKilled()
		{
			//Call listeners 

			Killed?.Invoke(this);

			//TODO score? exp?
		}

	}

}
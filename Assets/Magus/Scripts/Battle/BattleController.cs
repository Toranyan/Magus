using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using tora.singleton;

using tora.fsm;
using magus.chara;

namespace magus.battle
{
	public class BattleController : SingletonComponent<BattleController>
	{
		[SerializeField]
		private PlayerController _playerController;

		[SerializeField]
		private ProjectileManager _projectileManager;

		public PlayerController PlayerController => _playerController;

		public ProjectileManager ProjectileManager => _projectileManager;

		private void Start()
		{
			//create fsm
			StateMachine fsm = new StateMachine();

			//fsm.SetState()
			Init();
		}

		public void Init()
		{

			_projectileManager.Init(new[] { "1" });
		}
	}

}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using tora.singleton;

using tora.fsm;
using magus.chara;
using Cysharp.Threading.Tasks;
using magus.master;
using Cysharp.Threading.Tasks.Triggers;

namespace magus.battle
{
	public class BattleController : SingletonComponent<BattleController>
	{
		[SerializeField]
		private PlayerController _playerController;

		[SerializeField]
		private ProjectileManager _projectileManager;

		[SerializeField]
		private EffectManager _effectManager;

		public PlayerController PlayerController => _playerController;

		public ProjectileManager ProjectileManager => _projectileManager;

		public EffectManager EffectManager => _effectManager;

		private void Start()
		{
			//create fsm
			StateMachine fsm = new StateMachine();

			//fsm.SetState()
			Init();
		}

		public void Init()
		{
			InitAsync().Forget();
			
		}

		private async UniTask InitAsync()
		{

			await MasterData.LoadDataAsync();

			_projectileManager.Init(new[] {
				"Prefabs/Projectiles/BlackHole",
				"Prefabs/Projectiles/Fireball",
			}).Forget();
			_effectManager.Init(new[] {
				"Prefabs/Effects/BallExplosion",
				"Prefabs/Effects/Explosion_01",
			}).Forget();

			_playerController.Initialize();

		}
	}

}
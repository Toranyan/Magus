using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using tora.singleton;
using magus.chara;
using Cysharp.Threading.Tasks;
using magus.master;
using Cysharp.Threading.Tasks.Triggers;
using tora.camera;

namespace magus.battle
{
	/// <summary>
	/// A battle is Init -> Play -> Result, in a straight line, with "retry" just
	/// looping back to Init - no back-and-forth between phases, so this is a plain
	/// sequential flow rather than a state machine. Init() is the only phase
	/// implemented so far; Play/Result don't exist yet (no win/loss condition, no
	/// result screen) - add them here as further awaited steps when they're built,
	/// and only reach for an FSM if a phase ends up needing real enter/exit gating.
	/// </summary>
	public class BattleController : SingletonComponent<BattleController>
	{
		[SerializeField]
		private PlayerController _playerController;

		[SerializeField]
		private ProjectileManager _projectileManager;

		[SerializeField]
		private EffectManager _effectManager;

		[SerializeField]
		private FollowCamera _followCamera;

		public PlayerController PlayerController => _playerController;

		public ProjectileManager ProjectileManager => _projectileManager;

		public EffectManager EffectManager => _effectManager;

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


			_followCamera.FollowTarget = _playerController.gameObject;
			_followCamera.LookTarget = _playerController.gameObject;

		}
	}

}
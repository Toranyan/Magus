using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
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
		private PickupSpawner _pickupSpawner;

		[SerializeField]
		private FollowCamera _followCamera;

		[SerializeField]
		private GameObject _battle3DRoot;

		public PlayerController PlayerController => _playerController;

		public ProjectileManager ProjectileManager => _projectileManager;

		public EffectManager EffectManager => _effectManager;

		public PickupSpawner PickupSpawner => _pickupSpawner;

		/// <summary>Full battle setup: loads the map and player from the given
		/// addressable paths (or generates a random map, TODO, if MapAddress is empty),
		/// then runs the same required setup as InitRequired(). Use this for real
		/// gameplay.</summary>
		public void Init(BattleInitOptions options)
		{
			InitAsync(options).Forget();
		}

		private async UniTask InitAsync(BattleInitOptions options)
		{
			await LoadMapAsync(options.MapAddress);
			await LoadPlayerAsync(options.PlayerPrefabAddress);
			await InitRequiredAsync();
		}

		/// <summary>Setup every battle needs regardless of how the map/player got into
		/// the scene: master data, player setup, camera follow target. Called by Init()
		/// after it spawns the map/player; called directly by DebugBattleBootstrapper,
		/// whose dev scenes already have both placed by hand.</summary>
		public void InitRequired()
		{
			InitRequiredAsync().Forget();
		}

		private async UniTask InitRequiredAsync()
		{
			await MasterData.LoadDataAsync();

			_playerController.Initialize();
			_playerController.Unit.Killed += OnPlayerKilled;

			_followCamera.FollowTarget = _playerController.gameObject;
			_followCamera.LookTarget = _playerController.gameObject;
		}

		/// <summary>Clears all dynamic battle content - active projectiles, effects,
		/// pickups, and spawned enemies - back to a clean map. Does not touch the player.</summary>
		public void Reset()
		{
			_projectileManager.ClearAll();
			_effectManager.ClearAll();
			_pickupSpawner.ClearAll();

			foreach (var spawner in _battle3DRoot.GetComponentsInChildren<ObjectSpawner>())
			{
				spawner.ResetSpawner();
			}
		}

		private void OnPlayerKilled()
		{
			Reset();

			// TODO: show game over UI
			// TODO: return to main menu (GameManager.Instance.ChangeState(GameState.MainMenu))
		}

		private async UniTask LoadMapAsync(string mapAddress)
		{
			if (string.IsNullOrEmpty(mapAddress))
			{
				GenerateRandomMap();
				return;
			}

			var mapPrefab = await Addressables.LoadAssetAsync<GameObject>(mapAddress);
			Instantiate(mapPrefab, _battle3DRoot.transform);
		}

		private void GenerateRandomMap()
		{
			// TODO: procedural map generation
			Debug.LogWarning("[BattleController] Random map generation is not implemented yet.");
		}

		private async UniTask LoadPlayerAsync(string playerPrefabAddress)
		{
			var playerPrefab = await Addressables.LoadAssetAsync<GameObject>(playerPrefabAddress);
			var playerInstance = Instantiate(playerPrefab, _battle3DRoot.transform);
			_playerController = playerInstance.GetComponent<PlayerController>();
		}
	}

}
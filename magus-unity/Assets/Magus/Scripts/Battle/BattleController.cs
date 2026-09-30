using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using tora.singleton;
using tora.save;
using tora.eventbus;
using magus.chara;
using Cysharp.Threading.Tasks;
using magus.master;
using Cysharp.Threading.Tasks.Triggers;
using tora.camera;
using magus.story;
using magus.cutscene;

namespace magus.battle
{
	/// <summary>
	/// A battle is Init -> Play -> Result, in a straight line, with "retry" just
	/// looping back to Init - no back-and-forth between phases, so this is a plain
	/// sequential flow rather than a state machine. Init() is the only phase
	/// implemented so far; Play/Result don't exist yet (no win/loss condition, no
	/// result screen) - add them here as further awaited steps when they're built,
	/// and only reach for an FSM if a phase ends up needing real enter/exit gating.
	///
	/// Also an ISaveParticipant: remembers the last BattleInitOptions it was given so
	/// Continue can resume the same map/player. Deliberately separate from StoryManager's
	/// save data - replaying completed story nodes does not re-run their actions (so an
	/// old StartBattleAction won't re-fire), so "what map am I in" has to be its own
	/// piece of persisted state. See Docs/Design/StorySystem.md.
	/// </summary>
	public class BattleController : SingletonComponent<BattleController>, ISaveParticipant
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

		[SerializeField]
		private ShaderGlobalsUpdater _shaderGlobalsUpdater;

		public PlayerController PlayerController => _playerController;

		public ProjectileManager ProjectileManager => _projectileManager;

		public EffectManager EffectManager => _effectManager;

		public PickupSpawner PickupSpawner => _pickupSpawner;

		public string SaveKey => "battle";

		private const string DefaultSpawnPointId = "default";

		private BattleInitOptions _lastInitOptions;
		private GameObject _mapInstance;

		private void Awake()
		{
			SaveSystem.Register(this);
		}

		private void OnDestroy()
		{
			SaveSystem.Unregister(this);
		}

		/// <summary>Full battle setup: loads the map and player from the given
		/// addressable paths (or generates a random map, TODO, if MapAddress is empty),
		/// then runs the same required setup as InitRequired(). Use this for real
		/// gameplay.</summary>
		public void Init(BattleInitOptions options)
		{
			_lastInitOptions = options;
			InitAsync(options).Forget();
		}

		/// <summary>Re-enters Battle with whatever map/player was last saved, if any.
		/// Called explicitly by Continue/Load Game - not automatically when save data is
		/// loaded, since that also happens on every boot (StoryManager.Initialize), and
		/// that must not skip the title screen. Returns false if there's nothing saved.</summary>
		public bool TryResumeSavedBattle()
		{
			if (_lastInitOptions == null)
			{
				return false;
			}

			EventBus.Publish(new BattleStartRequestedEvent
			{
				MapAddress = _lastInitOptions.MapAddress,
				PlayerPrefabAddress = _lastInitOptions.PlayerPrefabAddress
			});
			return true;
		}

		public string CaptureState()
		{
			return _lastInitOptions == null ? null : JsonUtility.ToJson(_lastInitOptions);
		}

		public void RestoreState(string json)
		{
			_lastInitOptions = string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<BattleInitOptions>(json);
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
			_playerController.Died += OnPlayerKilled;

			_followCamera.FollowTarget = _playerController.gameObject;
			_followCamera.LookTarget = _playerController.gameObject;

			if (_shaderGlobalsUpdater != null)
			{
				_shaderGlobalsUpdater.SetPlayer(_playerController.transform);
			}

			// Common completion point for both Init() and the direct InitRequired() dev
			// path - see StartBattleAction, which waits for this before its node completes.
			EventBus.Publish(new BattleReadyEvent());
		}

		/// <summary>Clears all dynamic battle content - active projectiles, effects,
		/// pickups, and spawned enemies - back to a clean map. 
		/// Reset the player</summary>
		public void Reset()
		{
			_projectileManager.ClearAll();
			_effectManager.ClearAll();
			_pickupSpawner.ClearAll();

			foreach (var spawner in _battle3DRoot.GetComponentsInChildren<ObjectSpawner>())
			{
				spawner.ResetSpawner();
			}

			foreach (var spawner in _battle3DRoot.GetComponentsInChildren<UnitSpawner>())
			{
				spawner.ResetSpawner();
			}

			_playerController.transform.position = FindSpawnPosition();
			_playerController.Reset();

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
			_mapInstance = Instantiate(mapPrefab, _battle3DRoot.transform);
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
			playerInstance.transform.position = FindSpawnPosition();
			_playerController = playerInstance.GetComponent<PlayerController>();

			CutsceneManager.Instance.RegisterActor(CutsceneManager.PlayerActorName, playerInstance);
		}

		/// <summary>Resolves to a SpawnPoint on the current map matching
		/// _lastInitOptions.SpawnPointId (set per-battle by StartBattleAction, so different
		/// StoryNodes can land the player in different spots on the same map). Falls back to
		/// a SpawnPoint with Id "default" if the requested one isn't found, then to whatever
		/// SpawnPoint exists at all, then to the map's own root position (better than a
		/// hardcoded world-space guess - map roots aren't necessarily near world origin,
		/// map_test_02's is at local (-427, 0, -424) - but still not guaranteed walkable),
		/// logging a warning at each fallback step so a missing spawn point is an obvious
		/// cause, not a silent fall through the map.</summary>
		private Vector3 FindSpawnPosition()
		{
			if (_mapInstance == null)
			{
				return new Vector3(0, 1, 0); // no map instance (random map path) - TODO once procedural maps exist
			}

			var requestedId = string.IsNullOrEmpty(_lastInitOptions?.SpawnPointId) ? DefaultSpawnPointId : _lastInitOptions.SpawnPointId;

			SpawnPoint anySpawnPoint = null;
			SpawnPoint defaultSpawnPoint = null;

			foreach (var spawnPoint in _mapInstance.GetComponentsInChildren<SpawnPoint>(true))
			{
				if (spawnPoint.Id == requestedId)
				{
					return spawnPoint.transform.position;
				}

				anySpawnPoint ??= spawnPoint;
				if (spawnPoint.Id == DefaultSpawnPointId)
				{
					defaultSpawnPoint = spawnPoint;
				}
			}

			var fallback = defaultSpawnPoint != null ? defaultSpawnPoint : anySpawnPoint;
			if (fallback != null)
			{
				Debug.LogWarning($"[BattleController] Map '{_mapInstance.name}' has no SpawnPoint with Id '{requestedId}' - using '{fallback.Id}' instead.");
				return fallback.transform.position;
			}

			Debug.LogWarning($"[BattleController] Map '{_mapInstance.name}' has no SpawnPoints at all - falling back to the map's root position, which may not be walkable. Add a SpawnPoint component to a GameObject in the map prefab.");
			return _mapInstance.transform.position + Vector3.up;
		}
	}

}
using Cysharp.Threading.Tasks;
using magus.chara;
using magus.master;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace magus.battle
{
    /// <summary>
    /// Spawns units in an area. Like ObjectSpawner, but resolves the prefab and base
    /// stats from a UnitMasterData looked up by Addressables key instead of a direct
    /// prefab reference.
    /// </summary>
    public class UnitSpawner : MonoBehaviour
    {
        [SerializeField]
        private Collider _spawnArea;

        [SerializeField]
        private float _spawnRate;

        [Tooltip("Addressables key for the UnitMasterDataUnity asset describing the unit to spawn.")]
        [SerializeField]
        private string _unitMasterDataKey;

        [SerializeField]
        private Transform _targetParent;

        [SerializeField]
        private int _maxObjects = 10;

        [SerializeField]
        private int _maxSpawns = 20;

        private const int MaxSpawnPositionAttempts = 10;

        private ObjectPooler<PoolableHandler> _pooler = new();

        private UnitMasterData _unitMasterData;

        private double _lastSpawnTime;

        private bool _initialized;
        private bool _initializing;

        private int _spawnCount = 0;

        public async UniTask Initialize()
        {
            if (_initialized || _initializing) return;
            _initializing = true;

            var masterDataAsset = await Addressables.LoadAssetAsync<UnitMasterDataUnity>(_unitMasterDataKey);
            _unitMasterData = masterDataAsset.Data;

            var prefab = await Addressables.LoadAssetAsync<GameObject>(_unitMasterData.PrefabId);
            var poolableHandler = prefab.GetComponent<PoolableHandler>();
            if (poolableHandler == null)
            {
                Debug.LogError($"[UnitSpawner] Prefab '{_unitMasterData.PrefabId}' has no PoolableHandler component.");
                _initializing = false;
                return;
            }

            _pooler.Initialize(poolableHandler, this.transform);

            _initialized = true;
            _initializing = false;
        }

        /// <summary>Returns every currently spawned unit to the pool and allows a
        /// fresh batch to spawn. Used to clear the battlefield (e.g. on battle reset).</summary>
        public void ResetSpawner()
        {
            if (!_initialized) return;

            _pooler.FreeAll();
            _spawnCount = 0;
        }

        private void Update()
        {
            if (!_initialized)
            {
                if (!_initializing)
                    Initialize().Forget();
                return;
            }

            if (Time.time - _lastSpawnTime > 1.0 / _spawnRate
                && _pooler.AllocatedCount < _maxObjects
                && _spawnCount < _maxSpawns
                )
            {
                SpawnUnit();
            }
        }

        private void SpawnUnit()
        {
            var obj = _pooler.Allocate();
            obj.transform.SetParent(_targetParent);
            obj.transform.position = GetRandomSpawnPosition();

            var charaController = obj.GetComponent<GameCharaController>();
            charaController?.ApplyMasterData(_unitMasterData);

            obj.gameObject.SetActive(true);
            _lastSpawnTime = Time.time;
            _spawnCount++;

            var handler = obj.GetComponent<PoolableHandler>();
            if (handler)
            {
                handler.Pool = _pooler;
            }
        }

        /// <summary>Picks a random point inside _spawnArea, sampling only X/Z (Y is
        /// fixed to the area's own center height). Falls back to the area's center
        /// if no in-shape point turns up within a few tries - covers non-box shapes
        /// (sphere, capsule, convex mesh) where a bounds-only sample can land outside
        /// the actual collider.</summary>
        private Vector3 GetRandomSpawnPosition()
        {
            var bounds = _spawnArea.bounds;

            for (int i = 0; i < MaxSpawnPositionAttempts; i++)
            {
                var candidate = new Vector3(
                    Random.Range(bounds.min.x, bounds.max.x),
                    bounds.center.y,
                    Random.Range(bounds.min.z, bounds.max.z)
                );

                if (_spawnArea.ClosestPoint(candidate) == candidate)
                    return candidate;
            }

            return bounds.center;
        }
    }
}

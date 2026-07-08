using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace magus.battle
{
    /// <summary>
    /// Spawns objects in an area
    /// </summary>
    public class ObjectSpawner : MonoBehaviour
    {
        [SerializeField]
        private Collider _spawnArea;

        [SerializeField]
        private float _spawnRate;

        [SerializeField]
        private PoolableHandler _objectPrefab;

        [SerializeField]
        private Transform _targetParent;

        [SerializeField]
        private int _maxObjects = 10;

        [SerializeField]
        private int _maxSpawns = 20;

        private const int MaxSpawnPositionAttempts = 10;

        private ObjectPooler<PoolableHandler> _pooler = new();

        private double _lastSpawnTime;

        private bool _initialized;

        private int _spawnCount = 0;

		public void Initialize()
		{
            if (_initialized)
			{
                return;
			}
            _pooler.Initialize(_objectPrefab, this.transform);

            _initialized = true;
		}

		/// <summary>Returns every currently spawned object to the pool and allows a
		/// fresh batch to spawn. Used to clear the battlefield (e.g. on battle reset).</summary>
		public void ResetSpawner()
		{
            if (!_initialized) return;

            _pooler.FreeAll();
            _spawnCount = 0;
		}

		private void Update()
		{
            Initialize();

            if (Time.time - _lastSpawnTime > 1.0 / _spawnRate
                && _pooler.AllocatedCount < _maxObjects
                && _spawnCount < _maxSpawns
                )
			{
                SpawnObject();
			}
		}

        private void SpawnObject()
		{
            var obj = _pooler.Allocate();
            obj.transform.SetParent(_targetParent);
            obj.transform.position = GetRandomSpawnPosition();
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
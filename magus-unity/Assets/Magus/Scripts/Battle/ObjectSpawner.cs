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
            obj.transform.localPosition = transform.localPosition;
            obj.gameObject.SetActive(true);
            _lastSpawnTime = Time.time;
            _spawnCount++;

            var handler = obj.GetComponent<PoolableHandler>();
            if (handler)
			{
                handler.Pool = _pooler;
			}


		}

	}


}
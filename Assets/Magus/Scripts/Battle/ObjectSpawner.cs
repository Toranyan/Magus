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
        private MonoBehaviour _objectPrefab;

        [SerializeField]
        private Transform _targetParent;

        [SerializeField]
        private int _maxObjects;

        private ObjectPooler<MonoBehaviour> _pooler = new();

        private double _lastSpawnTime;

        private bool _initialized;

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
                && _pooler.AllocatedCount < _maxObjects)
			{
                SpawnObject();
			}
		}

        private void SpawnObject()
		{
            Debug.Log("Spawn at: " + Time.time);
            var obj = _pooler.Allocate();
            obj.transform.SetParent(_targetParent);
            obj.transform.localPosition = transform.localPosition;
            obj.gameObject.SetActive(true);
            _lastSpawnTime = Time.time;
		}

	}


}
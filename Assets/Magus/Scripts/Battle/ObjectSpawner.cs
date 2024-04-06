using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace App.Battle
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

        private ObjectPooler<MonoBehaviour> _pooler;

        private double _lastSpawnTime;

		public void Initialize()
		{
            _pooler.Initialize(_objectPrefab, this.transform);
		}

		private void Update()
		{

            if (Time.time - _lastSpawnTime > 1 / _spawnRate
                && _pooler.AllocatedCount < _maxObjects)
			{
                SpawnObject();
			}
		}

        private void SpawnObject()
		{
            var obj = _pooler.Allocate();
            obj.transform.SetParent(_targetParent);
            _lastSpawnTime = Time.time;
		}

	}


}
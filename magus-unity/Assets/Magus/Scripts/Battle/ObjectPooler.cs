using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace magus.battle
{

    public class ObjectPooler<T> where T : MonoBehaviour
    {

        private Queue<T> _pool;
        private List<T> _allocatedObjects;

        private float _expansionBatchSize = 10;
        private T _objPrefab;
        private Transform _poolContainer;

        public event Action<ObjectPooler<T>, T> ObjectCreated;

        public int AllocatedCount
        {
            get { return _allocatedObjects.Count; }
        }

        public void Initialize(T prefab, Transform poolContainer)
        {
            _poolContainer = poolContainer;
            _objPrefab = prefab;
            InitializePool();
        }

        private void InitializePool()
        {
            _pool = new Queue<T>();
            _allocatedObjects = new List<T>();

            ExpandPool();
        }

        public T Allocate()
        {
            T obj = null;
            if (!_pool.TryDequeue(out obj))
            {
                ExpandPool();
                if (_pool.TryDequeue(out obj))
                {
                    Debug.LogError("Unable to allocate");
                }
            }
            _allocatedObjects.Add(obj);
            return obj;
        }

        public void Free(T obj)
        {
            obj.gameObject.SetActive(false);
            _pool.Enqueue(obj);
            _allocatedObjects.Remove(obj);
            obj.transform.SetParent(_poolContainer);
        }

        private void ExpandPool()
        {
            for (int i = 0; i < _expansionBatchSize; i++)
            {
                var obj = GameObject.Instantiate<T>(_objPrefab);
                _pool.Enqueue(obj);
                obj.gameObject.SetActive(false);
                obj.transform.SetParent(_poolContainer);

                ObjectCreated?.Invoke(this, obj);
            }
        }

    }

}
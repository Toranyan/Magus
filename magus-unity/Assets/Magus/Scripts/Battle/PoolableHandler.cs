using System;
using UnityEngine;

namespace magus.battle
{
    public class PoolableHandler : MonoBehaviour
    {
        public ObjectPooler<PoolableHandler> Pool { get; set; }

        /// <summary>Raised after this instance is returned to its pool, however that
        /// happened (self-timed, collision, or a manager forcing a clear).</summary>
        public event Action<PoolableHandler> Returned;

        public void ReturnToPool()
		{
            Pool?.Free(this);
            Returned?.Invoke(this);
		}
    }

}

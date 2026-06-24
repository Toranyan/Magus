using UnityEngine;

namespace magus.battle
{
    public class PoolableHandler : MonoBehaviour
    {
        public ObjectPooler<PoolableHandler> Pool { get; set; }
        
        public void ReturnToPool()
		{
            Pool?.Free(this);
		}
    }

}

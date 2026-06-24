using Cysharp.Threading.Tasks;
using UnityEngine;

namespace magus.battle
{
    public class EffectManager : MonoBehaviour
    {
        [SerializeField]
        private ObjectPoolManager _poolManager;

        public async UniTask Init(string[] ids)
        {
            await _poolManager.Init(ids);
        }


        public async UniTask<GameObject> CreateEffect(string id, Vector3 position)
		{
            var obj = await CreateEffect(id);
            obj.transform.position = position;
            obj.gameObject.SetActive(true);
            return obj;
		}

        public async UniTask<GameObject> CreateEffect(string id)
		{
            var handle = await _poolManager.Allocate(id);
            return handle.gameObject;
        }

    }
}
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace magus.battle
{
    public class ProjectileManager : MonoBehaviour
    {
        [SerializeField]
        private ObjectPoolManager _poolManager;

        [SerializeField]
        private EffectManager _effectManager;

        public async UniTask Init(string[] ids)
        {
            await _poolManager.Init(ids);
        }

        public async UniTask<ProjectileBase> CreateProjectile(string id, IBattleEntity owner)
        {
            var handle = await _poolManager.Allocate(id);
            handle.gameObject.SetActive(true);

            var proj = handle.GetComponent<ProjectileBase>();
            proj.ClearEvents();
            proj.SetOwner(owner);
            proj.EffectRequested += OnProjectileEffectRequested;
            proj.Killed += OnProjectileKilled;

            return proj;
        }

        public void DeallocProjectile(ProjectileBase projectile)
        {
            var handle = projectile.GetComponent<PoolableHandler>();
            if (handle != null)
            {
                handle.ReturnToPool();
            }
            else
            {
                Debug.LogError("Unable to return projectile to pool");
            }
        }

        private void OnProjectileEffectRequested(string effectId, Vector3 position)
        {
            _effectManager?.CreateEffect(effectId, position).Forget();
        }

        private void OnProjectileKilled(ProjectileBase projectile)
        {
            projectile.EffectRequested -= OnProjectileEffectRequested;
            DeallocProjectile(projectile);
        }
    }
}

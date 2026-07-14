using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

namespace magus.battle
{
    public class ProjectileManager : MonoBehaviour
    {
        [SerializeField]
        private ObjectPoolManager _poolManager;

        [SerializeField]
        private EffectManager _effectManager;

        private readonly HashSet<ProjectileBase> _activeProjectiles = new();

        /// <summary>Warms the projectile pools for these ids ahead of time. Optional -
        /// CreateProjectile() creates a pool on demand if one doesn't exist yet.</summary>
        public async UniTask Preload(string[] ids)
        {
            await _poolManager.Preload(ids);
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

            _activeProjectiles.Add(proj);
            return proj;
        }

        /// <summary>Force-kills every active projectile and returns it to its pool.
        /// Used to clear the battlefield (e.g. on battle reset).</summary>
        public void ClearAll()
        {
            foreach (var proj in new List<ProjectileBase>(_activeProjectiles))
            {
                proj.Kill(immediate: true);
            }
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
            projectile.Killed -= OnProjectileKilled;
            _activeProjectiles.Remove(projectile);
            DeallocProjectile(projectile);
        }
    }
}

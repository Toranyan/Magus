using System.Collections.Generic;
using UnityEngine;

namespace magus.battle
{
    /// <summary>
    /// Delivers damage to DamageReceivers on behalf of an owner.
    /// Handles team filtering and optional duplicate-hit prevention.
    /// Attach to any object that should deal damage: projectiles, hitboxes, explosions, traps.
    /// </summary>
    public class DamageDealer : MonoBehaviour
    {
        [SerializeField]
        private float _damage;

        [SerializeField]
        private DamageType _damageType = DamageType.Physical;

        [SerializeField]
        private ElementType _element;

        /// <summary>
        /// When true, each DamageReceiver can only be hit once per attack window.
        /// Set false for DoT areas that should tick multiple times per target.
        /// </summary>
        [SerializeField]
        private bool _preventDuplicateHits = true;

        private IBattleEntity _owner;
        private readonly HashSet<DamageReceiver> _hitReceivers = new();
        private bool _isActive;

        public float Damage
        {
            get => _damage;
            set => _damage = value;
        }

        public ElementType Element
        {
            get => _element;
            set => _element = value;
        }

        public void SetOwner(IBattleEntity owner) => _owner = owner;

        /// <summary>
        /// Opens the attack window. Clears the hit-deduplication set.
        /// Call this when a swing, projectile, or hazard becomes active.
        /// </summary>
        public void BeginAttack()
        {
            _isActive = true;
            _hitReceivers.Clear();
        }

        /// <summary>
        /// Closes the attack window. No damage will be dealt until BeginAttack is called again.
        /// </summary>
        public void EndAttack()
        {
            _isActive = false;
        }

        /// <summary>
        /// Attempts to deal damage to a target.
        /// Skips friendly targets, inactive dealers, and duplicate hits when configured.
        /// Returns true if damage was applied.
        /// </summary>
        public bool TryDamage(DamageReceiver target, Vector3 hitPosition, Vector3 hitNormal = default)
        {
            if (!_isActive || target == null) return false;
            if (_owner != null && target.TeamId == _owner.TeamId) return false;
            if (_preventDuplicateHits && _hitReceivers.Contains(target)) return false;

            if (_preventDuplicateHits)
            {
                _hitReceivers.Add(target);
            }

            target.Damage(new DamageInfo
            {
                Amount            = _damage,
                Source            = _owner,
                SourceTeamId      = _owner?.TeamId ?? -1,
                Type              = _damageType,
                Element           = _element,
                HitPosition       = hitPosition,
                HitNormal         = hitNormal,
            });

            return true;
        }
    }
}

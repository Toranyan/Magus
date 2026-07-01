using UnityEngine;

namespace magus.battle
{
    /// <summary>
    /// Implemented by any component that can execute an attack on behalf of a Unit.
    /// Attach one or more to a character prefab. EnemyController and future player melee
    /// systems drive attacks through this interface rather than referencing concrete types.
    /// </summary>
    public interface IUnitAttack
    {
        /// <summary>Maximum range at which this attack can be used.</summary>
        float Range { get; }

        /// <summary>True when the attack is off cooldown and ready to fire.</summary>
        bool CanAttack { get; }

        /// <summary>Assign the owning Unit so the attack can set team and source on DamageInfo.</summary>
        void SetOwner(Unit owner);

        /// <summary>Execute the attack toward a world-space position.</summary>
        void Attack(Vector3 targetPosition);
    }
}

using UnityEngine;

namespace magus.battle
{
    /// <summary>
    /// Immutable value describing a single combat hit.
    /// Constructed by DamageDealer and passed through the damage pipeline.
    /// </summary>
    public struct DamageInfo
    {
        public float Amount;
        public DamageType Type;
        public ElementType Element;

        /// <summary>The entity that initiated the damage.</summary>
        public IBattleEntity Source;

        /// <summary>Team of the source at the time of the hit.</summary>
        public int SourceTeamId;

        public Vector3 HitPosition;
        public Vector3 HitNormal;
        public Vector3 KnockbackDirection;
        public float KnockbackForce;
        public bool IsCritical;
    }
}

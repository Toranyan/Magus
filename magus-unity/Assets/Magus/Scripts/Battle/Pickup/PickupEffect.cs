using UnityEngine;

namespace magus.battle
{
    /// <summary>
    /// What happens when a Pickup is collected. Each concrete effect is its own
    /// ScriptableObject asset (e.g. "Heal Small", "Heal Large") so designers can
    /// author/rebalance without code, the same way PickupData assets work.
    /// </summary>
    public abstract class PickupEffect : ScriptableObject
    {
        public abstract void Apply(Unit collector);
    }
}

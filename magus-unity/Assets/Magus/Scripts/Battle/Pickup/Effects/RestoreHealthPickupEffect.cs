using UnityEngine;

namespace magus.battle
{
    [CreateAssetMenu(fileName = "RestoreHealthPickupEffect", menuName = "Scriptable Objects/Loot/Pickup Effects/Restore Health")]
    public class RestoreHealthPickupEffect : PickupEffect
    {
        [SerializeField]
        private float _amount;

        public override void Apply(Unit collector)
        {
            collector?.Heal(_amount);
        }
    }
}

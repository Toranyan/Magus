using UnityEngine;

namespace magus.battle
{
    [CreateAssetMenu(fileName = "RestoreManaPickupEffect", menuName = "Scriptable Objects/Loot/Pickup Effects/Restore Mana")]
    public class RestoreManaPickupEffect : PickupEffect
    {
        [SerializeField]
        private float _amount;

        public override void Apply(Unit collector)
        {
            collector?.RestoreMana(_amount);
        }
    }
}

using UnityEngine;
using magus.chara;

namespace magus.battle
{
    [CreateAssetMenu(fileName = "AddCurrencyPickupEffect", menuName = "Scriptable Objects/Loot/Pickup Effects/Add Currency")]
    public class AddCurrencyPickupEffect : PickupEffect
    {
        [SerializeField]
        private int _amount;

        public override void Apply(Unit collector)
        {
            var progression = collector != null ? collector.GetComponentInParent<PlayerProgression>() : null;
            progression?.AddCurrency(_amount);
        }
    }
}

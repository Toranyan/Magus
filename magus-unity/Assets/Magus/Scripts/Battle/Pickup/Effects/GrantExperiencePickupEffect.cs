using UnityEngine;
using magus.chara;

namespace magus.battle
{
    [CreateAssetMenu(fileName = "GrantExperiencePickupEffect", menuName = "Scriptable Objects/Loot/Pickup Effects/Grant Experience")]
    public class GrantExperiencePickupEffect : PickupEffect
    {
        [SerializeField]
        private int _amount;

        public override void Apply(Unit collector)
        {
            var progression = collector != null ? collector.GetComponentInParent<PlayerProgression>() : null;
            progression?.AddExperience(_amount);
        }
    }
}

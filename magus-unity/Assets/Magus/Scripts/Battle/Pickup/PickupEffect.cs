using System;
using magus.chara;

namespace magus.battle
{
    public enum PickupEffectKind
    {
        None,
        RestoreHealth,
        RestoreMana,
        GrantExperience,
        AddCurrency,

        /// <summary>Stubbed until an item/inventory system exists. Unlike the other
        /// kinds, this won't resolve its prefab from PickupSpawner's Kind table -
        /// an item pickup's visual depends on the item's own type, not on it being
        /// a "GrantItem" effect.</summary>
        GrantItem,
    }

    /// <summary>
    /// What happens when a Pickup is collected. A plain serialized field on Pickup
    /// (or a LootTable's inline data) rather than an asset - every current effect is
    /// just "apply this amount to something," so a kind + amount is all that's needed.
    /// </summary>
    [Serializable]
    public class PickupEffect
    {
        public PickupEffectKind Kind;
        public float Amount;

        public void Apply(Unit collector)
        {
            if (collector == null) return;

            switch (Kind)
            {
                case PickupEffectKind.RestoreHealth:
                    collector.Heal(Amount);
                    break;

                case PickupEffectKind.RestoreMana:
                    collector.RestoreMana(Amount);
                    break;

                case PickupEffectKind.GrantExperience:
                    collector.GetComponentInParent<PlayerProgression>()?.AddExperience((int)Amount);
                    break;

                case PickupEffectKind.AddCurrency:
                    collector.GetComponentInParent<PlayerProgression>()?.AddCurrency((int)Amount);
                    break;

                case PickupEffectKind.GrantItem:
                    // TODO: no item/inventory system yet - wire this up once one exists.
                    break;
            }
        }
    }
}

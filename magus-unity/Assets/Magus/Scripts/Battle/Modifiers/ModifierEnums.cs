namespace magus.battle
{
    public enum ModifierType
    {
        CastTime,
        Cooldown,
        Speed,
        Range,
        Size,
        ProjectileCount,
        Damage,
        MoveSpeed,

        // Flag-only types — resolved via ModifierCollection.HasFlag, not Resolve.
        Stunned,
        Silenced,
        Rooted,
        Disarmed,
        Invulnerable,
        // TODO: extend as new alterable stats / flags are identified
    }

    public enum ModifierOperation
    {
        AddToBase,
        Percent,
        AddToFinal,
        Override,
        Flag,
    }

    public enum ModifierTag
    {
        Fire,
        Water,
        Wind,
        Earth,
        Light,
        Dark,
        Projectile,
        AoE,
        Melee,
        // TODO: work in progress, extend as needed
    }
}

namespace magus.battle
{
    public enum SpellType
    {
        Fireball  = 0,
        WaterJet  = 1,
        BlackHole = 2,
        // TODO: add remaining 15 Tier 1 spell types
    }

    public enum SpellExecutorType
    {
        Projectile = 0,
        // TODO: DOTArea, Barrier, Buff, Debuff as needed
    }

    public enum ElementType
    {
        Fire,
        Water,
        Wind,
        Earth,
        Light,
        Dark,
    }

    public enum StrategicCategory
    {
        Offense,
        Defense,
        Support,
    }

    public enum SpellTargetingType
    {
        TargetObject,
        TargetPosition,
        Untargeted,
        Global,
    }
}

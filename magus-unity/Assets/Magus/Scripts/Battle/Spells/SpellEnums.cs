namespace magus.battle
{
    public enum DamageType
    {
        Physical,
        Magical,
        True,
    }


    public enum SpellType
    {
        Fireball  = 0,
        WaterJet  = 1,
        BlackHole = 2,
        Meteor    = 3,
        // TODO: add remaining 15 Tier 1 spell types
    }

    public enum SpellExecutorType
    {
        Projectile = 0,
        Buff = 1,
        Debuff = 2,
        AoE = 3,
        // TODO: DOTArea, Barrier as needed
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

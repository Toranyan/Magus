namespace magus.battle
{
    public enum SpellType
    {
        Fireball = 0,
        WaterJet = 1,
        // TODO: add remaining 16 Tier 1 spell types
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

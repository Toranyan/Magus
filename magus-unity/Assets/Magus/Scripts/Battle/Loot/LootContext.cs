namespace magus.battle
{
    /// <summary>
    /// Contextual info passed into a loot roll. Luck/Difficulty/Wave are not wired to
    /// real systems yet - they exist as a stable API for future modifiers.
    /// </summary>
    public class LootContext
    {
        /// <summary>The entity that landed the killing blow, from Unit.LastDamageSource.
        /// Typed as IBattleEntity (matching DamageInfo.Source) rather than Unit, since
        /// that's what the damage pipeline actually tracks.</summary>
        public IBattleEntity Killer;

        public Unit Victim;

        /// <summary>TeamId allowed to collect the pickups this roll produces, from
        /// LootDropper's own serialized default. 0 (player team) unless overridden.</summary>
        public int CollectorTeamId;

        public float Luck = 1f;
        public float Difficulty = 1f;
        public int Wave;
    }
}

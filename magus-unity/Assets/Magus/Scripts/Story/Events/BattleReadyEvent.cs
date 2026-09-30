namespace magus.story
{
    /// <summary>Published by BattleController once battle setup has actually finished (map +
    /// player fully loaded and initialized) - not just requested. StartBattleAction
    /// subscribes to know when its node can complete, closing the race where a child node's
    /// action (e.g. a cutscene binding to the live player) could fire before the player
    /// even existed yet.</summary>
    public class BattleReadyEvent
    {
    }
}

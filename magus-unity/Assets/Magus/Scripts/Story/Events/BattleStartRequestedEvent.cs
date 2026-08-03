namespace magus.story
{
    /// <summary>Published by StartBattleAction. magus.game listens for this rather than
    /// StorySystem calling into GameManager directly, keeping Story presentation/flow
    /// independent per Docs/Design/StorySystem.md.</summary>
    public class BattleStartRequestedEvent
    {
        public string MapAddress;
        public string PlayerPrefabAddress;
    }
}

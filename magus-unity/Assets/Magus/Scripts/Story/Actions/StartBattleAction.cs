using System;
using tora.eventbus;

namespace magus.story
{
    /// <summary>Requests entering Battle with the given map/player, via
    /// BattleStartRequestedEvent - see GameManager, which owns the actual state
    /// transition and the pending BattleInitOptions.</summary>
    [Serializable]
    public class StartBattleAction : IStoryAction
    {
        public string MapAddress;
        public string PlayerPrefabAddress;

        public void Execute(StoryBlackboard blackboard)
        {
            EventBus.Publish(new BattleStartRequestedEvent
            {
                MapAddress = MapAddress,
                PlayerPrefabAddress = PlayerPrefabAddress
            });
        }
    }
}

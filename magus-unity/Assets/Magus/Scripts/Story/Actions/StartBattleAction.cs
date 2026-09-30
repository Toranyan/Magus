using System;
using tora.eventbus;

namespace magus.story
{
    /// <summary>Requests entering Battle with the given map/player, via
    /// BattleStartRequestedEvent - see GameManager, which owns the actual state
    /// transition and the pending BattleInitOptions. Async: the node that fires this
    /// doesn't complete (and its children don't unlock) until BattleReadyEvent reports
    /// the map/player are actually fully loaded, not just requested - see
    /// IAsyncStoryAction. Without this, a child node's action (e.g. a cutscene binding to
    /// the live player) could fire before the player had even been spawned yet.</summary>
    [Serializable]
    public class StartBattleAction : IAsyncStoryAction
    {
        public string MapAddress;
        public string PlayerPrefabAddress;

        /// <summary>Which SpawnPoint (by Id) on the map to place the player at - see
        /// SpawnPoint, BattleController.FindSpawnPosition. Empty falls back to "default".</summary>
        public string SpawnPointId;

        /// <summary>Fire-and-forget fallback if something calls Execute() directly instead of
        /// going through StoryNode/StoryManager's async sequencing.</summary>
        public void Execute(StoryBlackboard blackboard)
        {
            ExecuteAsync(blackboard, null);
        }

        public void ExecuteAsync(StoryBlackboard blackboard, Action onComplete)
        {
            void OnReady(BattleReadyEvent e)
            {
                EventBus.Unsubscribe<BattleReadyEvent>(OnReady);
                onComplete?.Invoke();
            }

            EventBus.Subscribe<BattleReadyEvent>(OnReady);
            EventBus.Publish(new BattleStartRequestedEvent
            {
                MapAddress = MapAddress,
                PlayerPrefabAddress = PlayerPrefabAddress,
                SpawnPointId = SpawnPointId
            });
        }
    }
}

using System;
using UnityEngine;
using tora.singleton;
using tora.eventbus;
using tora.save;
using magus.story;

namespace magus.checkpoint
{
    /// <summary>Skeleton: saves whenever a CheckpointReachedEvent fires (from a StoryNode's
    /// CheckpointAction). Deliberately minimal for now - CurrentCheckpointId is in-memory
    /// only, not persisted, since nothing consumes it yet (e.g. no "respawn at last
    /// checkpoint" logic exists). Add persistence (ISaveParticipant) once something
    /// actually needs to read it back.</summary>
    public class CheckpointManager : SingletonComponent<CheckpointManager>
    {
        public string CurrentCheckpointId { get; private set; }

        public event Action<string> CheckpointReached;

        private void Awake()
        {
            EventBus.Subscribe<CheckpointReachedEvent>(OnCheckpointReached);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<CheckpointReachedEvent>(OnCheckpointReached);
        }

        private void OnCheckpointReached(CheckpointReachedEvent e)
        {
            ReachCheckpoint(e.CheckpointId);
        }

        public void ReachCheckpoint(string checkpointId)
        {
            CurrentCheckpointId = checkpointId;
            SaveSystem.Save();
            Debug.Log($"[CheckpointManager] Reached checkpoint '{checkpointId}' - saved.");
            CheckpointReached?.Invoke(checkpointId);
        }
    }
}

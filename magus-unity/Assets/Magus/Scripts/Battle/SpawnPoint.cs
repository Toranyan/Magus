using UnityEngine;

namespace magus.battle
{
    /// <summary>Marker for a named player spawn location. Add one or more to a map prefab;
    /// BattleInitOptions.SpawnPointId picks which one BattleController uses. Id is
    /// independent of the GameObject's own name, so renaming for readability in the
    /// Hierarchy doesn't break anything referencing it. Every map should have at least one
    /// with Id "default" - see BattleController.FindSpawnPosition.</summary>
    public class SpawnPoint : MonoBehaviour
    {
        public string Id = "default";
    }
}

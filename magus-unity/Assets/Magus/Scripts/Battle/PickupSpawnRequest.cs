using UnityEngine;

namespace magus.battle
{
    /// <summary>
    /// Produced by LootTable.Roll, consumed by PickupSpawner. The Loot System decides
    /// what drops and where (Position comes from the LootContext.Victim); the Pickup
    /// System decides how it appears in the world. PickupPrefabId is the addressable
    /// id of a prefab carrying a Pickup component - there is no separate data asset,
    /// the prefab's own fields are the data.
    /// </summary>
    public struct PickupSpawnRequest
    {
        public string PickupPrefabId;
        public int Quantity;
        public Vector3 Position;
    }
}

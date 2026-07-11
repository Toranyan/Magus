using UnityEngine;

namespace magus.battle
{
    /// <summary>
    /// Produced by LootTable.Roll, consumed by PickupSpawner. The Loot System decides
    /// what drops and where (Position comes from the LootContext.Victim); the Pickup
    /// System decides how it appears in the world.
    /// </summary>
    public struct PickupSpawnRequest
    {
        public PickupData Pickup;
        public int Quantity;
        public Vector3 Position;
    }
}

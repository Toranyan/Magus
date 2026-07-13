using UnityEngine;

namespace magus.battle
{
    /// <summary>
    /// Produced by LootTable.Roll, consumed by PickupSpawner. The Loot System decides
    /// what drops and where (Position comes from the LootContext.Victim) and how much
    /// (Effect.Amount); the Pickup System decides which prefab represents Effect.Kind
    /// and how it appears in the world.
    /// </summary>
    public struct PickupSpawnRequest
    {
        public PickupEffect Effect;
        public int Quantity;
        public Vector3 Position;
    }
}

using UnityEngine;

namespace magus.battle
{
    /// <summary>
    /// Static data describing a pickup. Shared across all instances of that pickup
    /// type, so designers can rebalance without touching code. PrefabAddressableId
    /// resolves through ObjectPoolManager the same way spell/ability assets do.
    /// </summary>
    [CreateAssetMenu(fileName = "PickupData", menuName = "Scriptable Objects/Loot/PickupData")]
    public class PickupData : ScriptableObject
    {
        public string DisplayName;
        public Sprite Icon;

        [Tooltip("Addressable id of the world prefab, resolved via ObjectPoolManager.")]
        public string PrefabAddressableId;

        public float PickupRadius = 0.5f;
        public float MagnetRadius = 3f;

        [Tooltip("Seconds before an uncollected pickup is returned to the pool. 0 = never expires.")]
        public float Lifetime = 20f;

        [Tooltip("Seconds after spawn before the pickup can be collected.")]
        public float CollectionDelay = 0.25f;

        public string SpawnVfxId;
        public string CollectionVfxId;
        public string SpawnSfxId;
        public string CollectionSfxId;

        public PickupEffect Effect;
    }
}

using System;
using UnityEngine;

namespace magus.master
{
    [Serializable]
    public class UnitMasterData : BaseMasterData
    {
        public string Name;

        public float BaseMaxHp;
        public float BaseMaxMana;
        public float BaseThreatRating;
        public float MoveSpeed;

        [Tooltip("Addressables key for this unit's prefab.")]
        public string PrefabId;

        public string LevelProgressionTableId;
    }
}

using magus.master;
using UnityEngine;

namespace magus.battle
{
    public enum AbilityTargetingType
    {
        TargetObject,
        TargetPosition,
        Untargeted,
        Global,
    }

    public enum AbilityType
    {
        Projectile,
        Point,
        Area,
        Buff,
        Debuff,
    }

    [System.Serializable]
    public class AbilityInfo
    {
        public AbilityMasterData MasterData;

        public string Id;

        public AbilityType Type;
        public AbilityTargetingType TargetingType;

        public string[] AssetId;

        public int Level;

        public float Cooldown;
        public float Range;
        public float Delay;
        public float Duration;

        public IBattleEntity Owner;

        public AbilityInfo(AbilityMasterData masteraData, IBattleEntity owner)
        {
            if (masteraData == null)
            {
                Debug.LogError("AbilityInfo constructor received null AbilityMasterData");
                return;
            }

            MasterData = masteraData;
			
            Id = masteraData.Id;

            Type = masteraData.Type;
            TargetingType = masteraData.TargetingType;
            
            AssetId = masteraData.AssetId;

            Owner = owner;
		}


    }

}

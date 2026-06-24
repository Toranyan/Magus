using magus.battle;
using System;

namespace magus.master
{
	[Serializable]
	public class AbilityMasterData : BaseMasterData
    {
		public string Name;

		public AbilityType Type;
		public AbilityTargetingType TargetingType;

		public string[] AssetId;

		public float BaseCooldown;
		public float BaseRange;
		public float BaseDelay;
		public float BaseDuration;

		public float[] OtherParams;

	}

}

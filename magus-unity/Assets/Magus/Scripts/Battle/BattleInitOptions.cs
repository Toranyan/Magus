using System;

namespace magus.battle
{
	/// <summary>Parameters for BattleController.Init() - what map and player to spawn.
	/// Not needed by BattleController.InitRequired(), which assumes both are already
	/// placed in the scene (e.g. by DebugBattleBootstrapper's dev scenes). Serializable
	/// so BattleController can save/restore it directly - see BattleController's
	/// ISaveParticipant implementation.</summary>
	[Serializable]
	public class BattleInitOptions
	{
		/// <summary>Addressable path to the map prefab, e.g. "Prefabs/Maps/map_test_01".
		/// Null/empty generates a random map instead (TODO).</summary>
		public string MapAddress;

		/// <summary>Addressable path to the player character prefab, e.g. "Prefabs/Units/pc_test_01".</summary>
		public string PlayerPrefabAddress;
	}
}

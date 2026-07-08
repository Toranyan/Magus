using tora.fsm;
using magus.battle;

namespace magus.game
{
	/// <summary>Top-level "we're playing a battle" state. Kicks off BattleController's
	/// setup; battle's own internal phases (init/play/result) are a linear sequence
	/// inside BattleController, not GameFSM's concern.</summary>
	public class BattleGameState : State
	{
		public override void OnEnter(IState prevState)
		{
			// TODO: source these from map/character selection once that UI exists
			BattleController.Instance.Init(new BattleInitOptions
			{
				MapAddress = "Prefabs/Maps/map_test_01",
				PlayerPrefabAddress = "Prefabs/Units/pc_test_01",
			});
		}
	}
}

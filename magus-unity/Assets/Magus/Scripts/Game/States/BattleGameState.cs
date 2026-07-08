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
			BattleController.Instance.Init();
		}
	}
}

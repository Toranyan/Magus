using UnityEngine;
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
			var options = GameManager.Instance.PendingBattleInitOptions;

			if (options == null)
			{
				Debug.LogError("[BattleGameState] Entered Battle with no PendingBattleInitOptions set - " +
					"expected a StartBattleAction (see StorySystem) to have run first via BattleStartRequestedEvent.");
				return;
			}

			BattleController.Instance.Init(options);
		}
	}
}

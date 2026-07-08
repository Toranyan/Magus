using tora.fsm;

namespace magus.game
{
	/// <summary>Top-level game flow: MainMenu &lt;-&gt; Battle. Sub-screens within a
	/// state (options, pause, results) belong to that state's own UI/FSM, not here -
	/// only promote something to a GameState if it changes which systems are active.</summary>
	public class GameFSM : StateMachine
	{
		private readonly MainMenuState _mainMenuState = new();
		private readonly BattleGameState _battleGameState = new();

		public void ChangeState(GameState state)
		{
			IState nextState = state switch
			{
				GameState.MainMenu => _mainMenuState,
				GameState.Battle => _battleGameState,
				_ => null,
			};

			nextState?.Init(this);
			SetState(nextState);
		}
	}
}

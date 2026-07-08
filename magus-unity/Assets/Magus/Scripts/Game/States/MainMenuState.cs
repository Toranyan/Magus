using tora.fsm;
using magus.ui;

namespace magus.game
{
	/// <summary>Front door of the game: opens the title screen. Its own sub-screens
	/// (options, etc.) are handled by UIManager's navigation stack, not by GameFSM.</summary>
	public class MainMenuState : State
	{
		public override void OnEnter(IState prevState)
		{
			UIManager.Instance.PushView<UITitle>();
		}

		public override void OnExit(IState nextState)
		{
			UIManager.Instance.Close<UITitle>();
		}
	}
}

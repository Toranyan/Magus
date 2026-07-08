using tora.singleton;

namespace magus.game
{
	/// <summary>Owns the top-level GameFSM. Entry point for triggering game-flow
	/// transitions (e.g. Title's Play button, a future Battle result screen's
	/// "return to menu" button).</summary>
	public class GameManager : SingletonComponent<GameManager>
	{
		private readonly GameFSM _fsm = new();

		public GameState CurrentState { get; private set; }

		public void ChangeState(GameState state)
		{
			CurrentState = state;
			_fsm.ChangeState(state);
		}

		private void Update()
		{
			_fsm.Update();
		}
	}
}

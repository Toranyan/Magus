using tora.singleton;
using tora.eventbus;
using magus.battle;
using magus.story;

namespace magus.game
{
	/// <summary>Owns the top-level GameFSM. Entry point for triggering game-flow
	/// transitions (e.g. Title's Play button, a future Battle result screen's
	/// "return to menu" button).</summary>
	public class GameManager : SingletonComponent<GameManager>
	{
		private readonly GameFSM _fsm = new();

		public GameState CurrentState { get; private set; }

		/// <summary>Set by StartBattleAction via BattleStartRequestedEvent, read by
		/// BattleGameState.OnEnter() instead of hardcoding what map/player to load.</summary>
		public BattleInitOptions PendingBattleInitOptions { get; private set; }

		private void Awake()
		{
			EventBus.Subscribe<BattleStartRequestedEvent>(OnBattleStartRequested);
		}

		private void OnDestroy()
		{
			EventBus.Unsubscribe<BattleStartRequestedEvent>(OnBattleStartRequested);
		}

		private void OnBattleStartRequested(BattleStartRequestedEvent e)
		{
			PendingBattleInitOptions = new BattleInitOptions
			{
				MapAddress = e.MapAddress,
				PlayerPrefabAddress = e.PlayerPrefabAddress
			};
			ChangeState(GameState.Battle);
		}

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

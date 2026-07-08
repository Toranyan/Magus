using UnityEngine;
using magus.game;

namespace magus
{
	/// <summary>
	/// Entry point for the (single, persistent) main scene. Owns scene-level
	/// bootstrapping concerns that don't belong to any single screen - e.g. kicking
	/// off the top-level GameFSM in its starting state before any screen reacts to input.
	/// </summary>
	public class MainBootstrapper : MonoBehaviour
	{
		private void Awake()
		{
			GameManager.Instance.ChangeState(GameState.MainMenu);
		}
	}
}

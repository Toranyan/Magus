using UnityEngine;
using magus.ui;

namespace magus
{
	/// <summary>
	/// Entry point for the title/main scene. Owns scene-level bootstrapping concerns
	/// that don't belong to any single screen - e.g. seeding the UI navigation stack
	/// with its root state before any screen reacts to input.
	/// </summary>
	public class MainBootstrapper : MonoBehaviour
	{
		private void Awake()
		{
			//UIManager.Instance.PushState(new TitleUIState());
			UIManager.Instance.PushView<UITitle>();
		}
	}
}

using tora.fsm;
using tora.ui;

namespace magus.ui
{
	/// <summary>
	/// Root of the title screen's navigation stack. The title screen itself has no
	/// open/close behaviour of its own — it's always visible — so this state only
	/// exists to give Pop() from OptionsUIState somewhere to return to.
	/// </summary>
	public class TitleUIState : State, IUIState
	{
	}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using tora.ui;
using magus.story;
using magus.battle;



namespace magus.ui {

	public class UITitle : UIViewBase {

		#region Serialized Fields
		[SerializeField]
		private Button _newGameButton;

		[SerializeField]
		private Button _continueButton;

		[SerializeField]
		private Button _loadGameButton;

		[SerializeField]
		private Button _optionsButton;

		[SerializeField]
		private UIAnimatedView _optionsWindow;

		#endregion

		#region Member Functions

		private void Awake() {

			//init events
			_newGameButton.onClick.AddListener(OnClickNewGameButton);
			_continueButton.onClick.AddListener(OnClickContinueButton);
			_loadGameButton.onClick.AddListener(OnClickLoadGameButton);
			_optionsButton.onClick.AddListener(OnClickOptionsButton);

		}


		private void OnClickNewGameButton() {

			//fresh run - clear any prior story progress. Evaluating the reset graph is
			//expected to run a StartBattleAction if/when the story wants to enter Battle -
			//see GameManager.OnBattleStartRequested. Don't ChangeState directly here, or
			//Battle would end up entered twice.
			StoryManager.Instance.ResetProgress();
		}

		private void OnClickContinueButton() {

			//StoryManager already loads its save on Awake; explicit call here keeps this
			//button correct if that ever changes. Story progress alone won't re-enter
			//Battle (already-completed nodes don't re-fire their actions), so explicitly
			//ask BattleController to resume whatever map/player it last saved, if any.
			StoryManager.Instance.Load();
			BattleController.Instance.TryResumeSavedBattle();
		}

		private void OnClickLoadGameButton() {

			//SaveSystem is single-slot in v1 (Docs/Design/SaveSystem.md) - same as Continue
			//until multiple save slots exist
			OnClickContinueButton();
		}

		private void OnClickOptionsButton() {
			//UIManager wraps the view in a ViewState and pushes it - no direct
			//UIStateManager reference needed here
			UIManager.Instance.PushView(_optionsWindow);
		}

		#endregion

	}



}

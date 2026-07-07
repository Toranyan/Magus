using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using tora.ui;
using magus.singleton;
using magus.system;



namespace magus.ui {

	public class UITitle : UIViewBase {

		#region Serialized Fields
		[SerializeField]
		private Button _playButton;

		[SerializeField]
		private Button _optionsButton;

		[SerializeField]
		private UIAnimatedView _optionsWindow;

		#endregion

		#region Member Functions

		private void Awake() {

			//init events
			_playButton.onClick.AddListener(OnClickPlayButton);
			_optionsButton.onClick.AddListener(OnClickOptionsButton);

		}


		private void OnClickPlayButton() {

			//tell controller to transition
			SceneController.Instance.ChangeScene(Scene.Battle);
		}

		private void OnClickOptionsButton() {
			//UIManager wraps the view in a ViewState and pushes it - no direct
			//UIStateManager reference needed here
			UIManager.Instance.PushView(_optionsWindow);
		}

		#endregion

	}



}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using tora.singleton;
using UnityEngine.InputSystem;
using System;

namespace magus.input
{
    public class InputManager : SingletonComponent<InputManager>
    {

		[SerializeField]
		private InputActionAsset _actionAsset;

		[SerializeField]
		private PlayerInput _playerInput;


		public PlayerInput PlayerInput
		{
			get { return _playerInput; }
		}

		//public event Action<InputAction.CallbackContext> ActionTriggered;
		public Dictionary<PlayerInputType, Dictionary<InputPhase, List<Action<InputAction.CallbackContext>>>> InputCallbackDict = new Dictionary<PlayerInputType, Dictionary<InputPhase, List<Action<InputAction.CallbackContext>>>>();

		private static readonly string PLAYER_ACTION_MAP_NAME = "Player";

		public enum PlayerInputType {
			Move,
			MovePointer,
			Attack,
			Interact,
			Ability_01,
			Ability_02,
		}

		public enum InputPhase
		{
			Started,
			Cancelled,
			Performed,
		}

		public event Action MoveTriggered;


		public void Awake()
		{
			Init();
		}

		public void Init()
		{
			var actionMap = _actionAsset.FindActionMap(PLAYER_ACTION_MAP_NAME);
			for (int i = 0; i < actionMap.actions.Count; i++)
			{
				//listen to each action in action map
				PlayerInputType inputType = (PlayerInputType)i;
				InputCallbackDict[inputType] = new Dictionary<InputPhase, List<Action<InputAction.CallbackContext>>>();

				//each input phase
				InputCallbackDict[inputType][InputPhase.Started] = new List<Action<InputAction.CallbackContext>>();
				InputCallbackDict[inputType][InputPhase.Cancelled] = new List<Action<InputAction.CallbackContext>>();
				InputCallbackDict[inputType][InputPhase.Performed] = new List<Action<InputAction.CallbackContext>>();

				var inputAction = actionMap.FindAction(inputType.ToString());

				if (inputAction == null)
				{
					Debug.LogWarning($"Action not found : {inputType}");
					continue;
				}

				inputAction.started += (context) => {
					OnInputTriggered(inputType, InputPhase.Started, context);
				};
				inputAction.performed += (context) => {
					OnInputTriggered(inputType, InputPhase.Performed, context);
				};
				inputAction.canceled += (context) => {
					OnInputTriggered(inputType, InputPhase.Cancelled, context);
				};

				Debug.Log($"Action callback added : {(PlayerInputType)i}");

				inputAction.Enable();
			}
		}



		private void OnInputTriggered(PlayerInputType input, InputPhase phase, InputAction.CallbackContext context)
		{
			var list = InputCallbackDict[input][phase];
			foreach (var callback in list)
			{
				callback.Invoke(context);
			}
		}

		public void AddActionCallback(PlayerInputType input, InputPhase phase, Action<InputAction.CallbackContext> callback)
		{
			InputCallbackDict[input][phase].Add(callback);
		}

		public void RemoveActionCallback(PlayerInputType input, InputPhase phase, Action<InputAction.CallbackContext> callback)
		{
			InputCallbackDict[input][phase].Remove(callback);
		}

	}

}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using App.Input;

namespace App.Chara
{
    public class PlayerController : MonoBehaviour
    {

        [SerializeField]
        private ModelController _modelController;


        private void Start()
        {
            Initialize();
        }

		private void Initialize()
		{
            //input setup
            //InputManager.Instance.AddActionCallback(InputManager.PlayerInputType.Move, InputManager.InputPhase.Started, OnMoveInput);
            //InputManager.Instance.AddActionCallback(InputManager.PlayerInputType.Move, InputManager.InputPhase.Cancelled, OnMoveInput);
        }

        private void SetupInput()
		{

		}


		private void Update()
		{
            UpdateMoveVector();
		}


		private void UpdateMoveVector()
		{
            var inputVec = InputManager.Instance.PlayerInput.actions["Move"].ReadValue<Vector2>();
            var vector = Vector3.zero;
            vector.x = inputVec.x;
            vector.z = inputVec.y;
            _modelController.SetMoveVector(vector);
        }


		private void OnMoveInput(InputAction.CallbackContext context)
		{
            
            var val = context.action.ReadValue<Vector2>();
            Debug.Log(val);
            //translate
            var vector = Vector3.zero;
            vector.x = val.x;
            vector.z = val.y;
            _modelController.SetMoveVector(vector);
        }

    }
}
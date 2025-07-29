using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using magus.input;
using UnityEngine.AddressableAssets;
using magus.battle;
using Cysharp.Threading.Tasks;

namespace magus.chara
{
    public class PlayerController : MonoBehaviour, IOwner
    {
        [SerializeField]
        private GameCharaController _gameCharaController;


        public int TeamId => _gameCharaController.TeamId;


        private void Start()
        {
            Initialize();
        }

		private void Initialize()
		{
            //input setup
            //InputManager.Instance.AddActionCallback(InputManager.PlayerInputType.Move, InputManager.InputPhase.Started, OnMoveInput);
            //InputManager.Instance.AddActionCallback(InputManager.PlayerInputType.Move, InputManager.InputPhase.Cancelled, OnMoveInput);
            InputManager.Instance.AddActionCallback(InputManager.PlayerInputType.Attack, InputManager.InputPhase.Performed, OnAttackInput);
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
            _gameCharaController.SetMoveVector(vector);
        }


		private void OnMoveInput(InputAction.CallbackContext context)
		{
            
            var val = context.action.ReadValue<Vector2>();
            Debug.Log(val);
            //translate
            var vector = Vector3.zero;
            vector.x = val.x;
            vector.z = val.y;
            _gameCharaController.SetMoveVector(vector);
        }

        private void OnAttackInput(InputAction.CallbackContext context)
		{
            Debug.Log("OnAttackInput");

            //TODO use projectilemanager
            CreateProjectile().Forget();
        }

        private async UniTask CreateProjectile()
		{
            var proj = await BattleController.Instance.ProjectileManager.CreateProjectile("1", this);
            var initVec = new Vector3(0, 0, 1);
            proj.transform.SetParent(null);

            var initPos = transform.position;
            initPos.y = 1;
            proj.transform.position = initPos;
            

            var enemy = GetClosestEnemy();
            if (enemy != null)
			{
                var deltaVec = enemy.transform.position - transform.position;
                initVec = deltaVec.normalized;
                Debug.Log($"initvec : {initVec}");
			}
            else
			{
                initVec = this.transform.rotation *  Vector3.forward;
			}

            proj.Setup(initVec);
            proj.Revive();
        }

        private GameCharaController GetClosestEnemy()
		{
            LayerMask characterLayer = LayerMask.GetMask("Character");
            float radius = 30f; // max search distance

            var hits = Physics.OverlapSphere(this.transform.position, radius, characterLayer);
            GameCharaController closest = null;
            float closestDistanceSqr = float.MaxValue;

            foreach (var hit in hits)
            {
                var controller = hit.GetComponent<GameCharaController>();
                if (controller != null && controller != this && controller.TeamId != TeamId)
                {
                    float distSqr = (controller.transform.position - this.transform.position).sqrMagnitude;
                    if (distSqr < closestDistanceSqr)
                    {
                        closestDistanceSqr = distSqr;
                        closest = controller;
                    }
                }
            }
            return closest;
        }

    }
}
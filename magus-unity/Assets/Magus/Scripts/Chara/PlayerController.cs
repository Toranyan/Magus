using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using magus.input;
using UnityEngine.AddressableAssets;
using magus.battle;
using Cysharp.Threading.Tasks;
using magus.master;

namespace magus.chara
{
    public class PlayerController : MonoBehaviour, IBattleEntity
    {
        [SerializeField]
        private GameCharaController _gameCharaController;

        public int TeamId => _gameCharaController.TeamId;

        // implement IBattleEntity.GameObject (IOwner extends IBattleEntity)
        public GameObject GameObject => this.gameObject;

        private GameCharaController _targetEnemy;

        private List<SpellInstance> _equippedSpells = new List<SpellInstance>();

        private List<IAbilityExecutor> _abilityExecutors = new List<IAbilityExecutor>();


        private Dictionary<int, AbilityInfo> _abilities = new Dictionary<int, AbilityInfo>();

		private void Start()
        {
        }

		public void Initialize()
		{
            //input setup
            //InputManager.Instance.AddActionCallback(InputManager.PlayerInputType.Move, InputManager.InputPhase.Started, OnMoveInput);
            //InputManager.Instance.AddActionCallback(InputManager.PlayerInputType.Move, InputManager.InputPhase.Cancelled, OnMoveInput);
            InputManager.Instance.AddActionCallback(InputManager.PlayerInputType.Attack, InputManager.InputPhase.Performed, OnAttackInput);

            InputManager.Instance.AddActionCallback(InputManager.PlayerInputType.Ability_01, InputManager.InputPhase.Performed, (c) => OnAbilityInput(0));
			InputManager.Instance.AddActionCallback(InputManager.PlayerInputType.Ability_02, InputManager.InputPhase.Performed, (c) => OnAbilityInput(1));

			//TEMP
			//_equippedSpells.Add(SpellFactory.CreateSpell())

			//initialize abilities
			//var abilityMaster1 = MasterData.GetMasterData<AbilityMasterData>("ability_fireball_01");
   //         var ability1 = new AbilityInfo(abilityMaster1, this);
   //         _abilities.Add(ability1);

			//var abilityMaster2 = MasterData.GetMasterData<AbilityMasterData>("ability_blackhole_01");
			//var ability2 = new AbilityInfo(abilityMaster2, this);
			//_abilities.Add(ability2);
			//_abilityExecutors.Add(AbilityExecutorFactory.CreateAbilityExecutor(ability1));

            _gameCharaController.Setup();
        }

        public void SetAbility(int index, AbilityInfo abilityInfo)
		{

        }

        private void SetupInput()
		{

		}


		private void Update()
		{
            UpdateMoveVector();

            UpdateTarget();
		}


		private void UpdateMoveVector()
		{
            var inputVec = InputManager.Instance.PlayerInput.actions["Move"].ReadValue<Vector2>();
            var vector = Vector3.zero;
            vector.x = inputVec.x;
            vector.z = inputVec.y;
            _gameCharaController.SetMoveVector(vector);
        }

        private void UpdateTarget()
		{
            _targetEnemy = GetClosestEnemy();

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
            //CreateProjectile().Forget();
        }

        private void OnAbilityInput(int index)
		{
			if (_abilities.TryGetValue(index, out var abilityInfo))
			{
				var executionContext = new AbilityExecutionContext {
					Owner = this,
					Source = this,
					Target = _targetEnemy,
					SourcePosition = this.transform.position,
					TargetPosition = _targetEnemy != null ? _targetEnemy.transform.position : this.transform.position + this.transform.forward * 10f,
					Info = abilityInfo
				};

				var executor = AbilityExecutorFactory.CreateAbilityExecutor(abilityInfo);
				executor.ExecuteAbility(executionContext);
			} else
			{
				Debug.LogWarning($"Ability at index {index} not found.");
				return;
			}
		}

        //private async UniTask CreateProjectile()
        //{
        //	var proj = await BattleController.Instance.ProjectileManager.CreateProjectile("Prefabs/Projectiles/Fireball.prefab", this);
        //	var initVec = new Vector3(0, 0, 1);
        //	proj.transform.SetParent(null);

        //	var initPos = transform.position;
        //	initPos.y += 1;
        //	proj.transform.position = initPos;


        //	var enemy = GetClosestEnemy();
        //	if (enemy != null)
        //	{
        //		var targetVec = enemy.transform.position;
        //		targetVec.y += 1;
        //		var deltaVec = targetVec - initPos;
        //		deltaVec.y = 0;
        //		initVec = deltaVec.normalized;
        //		Debug.Log($"initvec : {initVec}");
        //	} else
        //	{
        //		initVec = this.transform.rotation * Vector3.forward;
        //	}

        //	proj.Setup(initVec);
        //}

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
                if (controller != null && controller.gameObject != this.gameObject && controller.TeamId != TeamId)
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

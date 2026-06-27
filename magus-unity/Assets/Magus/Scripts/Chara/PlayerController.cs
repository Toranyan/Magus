using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using magus.input;
using magus.battle;
using magus.master;
using Cysharp.Threading.Tasks;

namespace magus.chara
{
    public class PlayerController : MonoBehaviour, IBattleEntity
    {
        [SerializeField]
        private GameCharaController _gameCharaController;

        public int TeamId => _gameCharaController.TeamId;
        public GameObject GameObject => this.gameObject;

        // Three prepared spell slots
        private UnitSpellInstance[] _preparedSpells = new UnitSpellInstance[3];

        private GameCharaController _targetEnemy;

        public void Initialize()
        {
            InputManager.Instance.AddActionCallback(InputManager.PlayerInputType.Attack,    InputManager.InputPhase.Performed, OnAttackInput);
            InputManager.Instance.AddActionCallback(InputManager.PlayerInputType.Ability_01, InputManager.InputPhase.Performed, (c) => OnSpellInput(0));
            InputManager.Instance.AddActionCallback(InputManager.PlayerInputType.Ability_02, InputManager.InputPhase.Performed, (c) => OnSpellInput(1));

            // TODO: bind third spell slot when input action is available

            // TODO: load spells from run data / spell draft instead of hardcoding
            LoadDefaultSpells();

            _gameCharaController.Setup();
        }

        public void SetSpell(int index, UnitSpellInstance spell)
        {
            if (index < 0 || index >= _preparedSpells.Length)
            {
                Debug.LogError($"[PlayerController] Spell slot index {index} out of range");
                return;
            }
            _preparedSpells[index] = spell;
        }

        private void LoadDefaultSpells()
        {
            var fireballMaster = MasterData.GetMasterData<SpellMasterData>("spell_fireball_01");
            if (fireballMaster != null)
                _preparedSpells[0] = new UnitSpellInstance(new SpellInfo(fireballMaster), this);

            // TODO: populate slots 1 and 2 once more spells exist
        }

        private void Update()
        {
            UpdateMoveVector();
            UpdateTarget();
            TickSpells();
        }

        private void UpdateMoveVector()
        {
            var inputVec = InputManager.Instance.PlayerInput.actions["Move"].ReadValue<Vector2>();
            var vector   = Vector3.zero;
            vector.x = inputVec.x;
            vector.z = inputVec.y;
            _gameCharaController.SetMoveVector(vector);
        }

        private void UpdateTarget()
        {
            _targetEnemy = GetClosestEnemy();
        }

        private void TickSpells()
        {
            foreach (var spell in _preparedSpells)
                spell?.Tick(Time.deltaTime);
        }

        private void OnAttackInput(InputAction.CallbackContext context)
        {
            // TODO: basic attack (non-spell)
        }

        private void OnSpellInput(int index)
        {
            var spell = _preparedSpells[index];
            if (spell == null)
            {
                Debug.LogWarning($"[PlayerController] No spell equipped in slot {index}");
                return;
            }

            var targetPos = _targetEnemy != null
                ? _targetEnemy.transform.position
                : transform.position + transform.forward * spell.Info.Range;

            var context = new SpellCastContext
            {
                Info          = spell.Info,
                Caster        = this,
                Target        = _targetEnemy,
                CastPosition  = transform.position,
                TargetPosition = targetPos,
                IsCounter     = false, // TODO: set true when counter window is active
            };

            spell.TryCast(context);
        }

        private GameCharaController GetClosestEnemy()
        {
            LayerMask characterLayer = LayerMask.GetMask("Character");
            float radius = 30f;

            var hits = Physics.OverlapSphere(transform.position, radius, characterLayer);
            GameCharaController closest = null;
            float closestDistSqr = float.MaxValue;

            foreach (var hit in hits)
            {
                var controller = hit.GetComponent<GameCharaController>();
                if (controller != null && controller.gameObject != gameObject && controller.TeamId != TeamId)
                {
                    float distSqr = (controller.transform.position - transform.position).sqrMagnitude;
                    if (distSqr < closestDistSqr)
                    {
                        closestDistSqr = distSqr;
                        closest = controller;
                    }
                }
            }
            return closest;
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;
using System;
using magus.input;
using magus.battle;
using magus.master;

namespace magus.chara
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private GameCharaController _charaController;
        [SerializeField] private PlayerProgression _progression;

        public Unit Unit => _charaController.Unit;
        public PlayerProgression Progression => _progression;

        /// <summary>Fires once the death animation/ragdoll has fully played out and
        /// the player object has been deactivated - the correct point to reset and
        /// respawn, rather than reacting to Unit.Killed directly (which fires before
        /// the death sequence has finished).</summary>
        public event Action Died;

		private UnitSpellInstance[] _preparedSpells = new UnitSpellInstance[3];
        private Unit _targetEnemy;

        private void Awake()
        {
            _charaController.DeathAnimationFinished += OnDeathAnimationFinished;
        }

        public void Initialize()
        {
            InputManager.Instance.AddActionCallback(InputManager.PlayerInputType.Attack,     InputManager.InputPhase.Performed, OnAttackInput);
            InputManager.Instance.AddActionCallback(InputManager.PlayerInputType.Ability_01, InputManager.InputPhase.Performed, (c) => OnSpellInput(0));
            InputManager.Instance.AddActionCallback(InputManager.PlayerInputType.Ability_02, InputManager.InputPhase.Performed, (c) => OnSpellInput(1));

            // TODO: bind third spell slot when input action is available
            // TODO: load spells from run data / spell draft instead of hardcoding
            //LoadDefaultSpells();

            _charaController.Setup();
        }

		public void Reset()
		{
			gameObject.SetActive(true);
			_charaController.Setup();
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
                _preparedSpells[0] = new UnitSpellInstance(new SpellInfo(fireballMaster), Unit);

            // TODO: populate slots 1 and 2 once more spells exist
        }

        private void Update()
        {
            if (!Unit.IsAlive) return;
            UpdateMoveVector();
            UpdateTarget();
            TickSpells();
            AutocastSpells();
        }

        private void UpdateMoveVector()
        {
            var inputVec = InputManager.Instance.PlayerInput.actions["Move"].ReadValue<Vector2>();
            _charaController.SetMoveVector(new Vector3(inputVec.x, 0f, inputVec.y));
        }

        private void UpdateTarget()
        {
            _targetEnemy = FindClosestEnemy();
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

            TryCastSpell(spell);
        }

        private void AutocastSpells()
        {
            foreach (var spell in _preparedSpells)
            {
                if (spell != null && spell.Autocast)
                    TryCastSpell(spell);
            }
        }

        private bool TryCastSpell(UnitSpellInstance spell)
        {
            var targetPos = _targetEnemy != null
                ? _targetEnemy.transform.position
                : transform.position + transform.forward * spell.Info.Range;

            var context = new SpellCastContext
            {
                Info           = spell.Info,
                Caster         = Unit,
                Target         = _targetEnemy,
                CastPosition   = transform.position,
                TargetPosition = targetPos,
            };

            return spell.TryCast(context);
        }

        private Unit FindClosestEnemy()
        {
            var mask = LayerMask.GetMask("Character");
            var hits = Physics.OverlapSphere(transform.position, 30f, mask);

            Unit closest = null;
            float closestDistSqr = float.MaxValue;

            foreach (var hit in hits)
            {
                var unit = hit.GetComponent<Unit>();
                if (unit == null || unit == Unit || unit.TeamId == Unit.TeamId || !unit.IsAlive)
                    continue;

                float distSqr = (unit.transform.position - transform.position).sqrMagnitude;
                if (distSqr < closestDistSqr)
                {
                    closest = unit;
                    closestDistSqr = distSqr;
                }
            }

            return closest;
        }

        private void OnDeathAnimationFinished()
        {
            gameObject.SetActive(false);
            Died?.Invoke();
        }
    }
}

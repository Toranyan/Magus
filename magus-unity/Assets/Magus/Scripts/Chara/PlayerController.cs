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

        [Tooltip("Ground colliders hit-tested when aiming a manual-target (TargetPosition) spell.")]
        [SerializeField] private LayerMask _groundLayerMask;

        public Unit Unit => _charaController.Unit;
        public PlayerProgression Progression => _progression;

        /// <summary>Fires once the death animation/ragdoll has fully played out and
        /// the player object has been deactivated - the correct point to reset and
        /// respawn, rather than reacting to Unit.Killed directly (which fires before
        /// the death sequence has finished).</summary>
        public event Action Died;

		// Slot 0 has no dedicated cast button (see BattleUIView) - slots 1-4 are bound
		// to ability buttons/input.
		private UnitSpellInstance[] _preparedSpells = new UnitSpellInstance[5];
        private Unit _targetEnemy;

        // Manual targeting (SpellTargetingType.TargetPosition): set by OnSpellInput,
        // resolved by the next Attack (left-click) instead of casting immediately.
        private bool _isAiming;
        private UnitSpellInstance _aimingSpell;
        private Vector3 _aimPosition;

        private void Awake()
        {
            _charaController.DeathAnimationFinished += OnDeathAnimationFinished;
        }

        public void Initialize()
        {
            InputManager.Instance.AddActionCallback(InputManager.PlayerInputType.Attack,     InputManager.InputPhase.Performed, OnAttackInput);
            InputManager.Instance.AddActionCallback(InputManager.PlayerInputType.Ability_01, InputManager.InputPhase.Performed, (c) => OnSpellInput(1));
            InputManager.Instance.AddActionCallback(InputManager.PlayerInputType.Ability_02, InputManager.InputPhase.Performed, (c) => OnSpellInput(2));

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

        public UnitSpellInstance GetSpell(int index)
        {
            return (index >= 0 && index < _preparedSpells.Length) ? _preparedSpells[index] : null;
        }

        /// <summary>Attempts to cast the spell in the given slot. Used by BattleUIView's
        /// spell buttons, as well as OnSpellInput for keyboard/gamepad-bound slots.</summary>
        public bool CastSpellSlot(int index)
        {
            var spell = GetSpell(index);
            if (spell == null)
                return false;

            if (spell.Info.TargetingType == SpellTargetingType.TargetPosition)
            {
                BeginAiming(spell);
                return true;
            }

            return TryCastSpell(spell);
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

            if (_isAiming)
                UpdateAiming();
        }

        private void UpdateMoveVector()
        {
            var inputVec = InputManager.Instance.PlayerInput.actions["Move"].ReadValue<Vector2>();
            _charaController.SetMoveVector(new Vector3(inputVec.x, 0f, inputVec.y));
        }

        private void UpdateTarget()
        {
            _targetEnemy = FindAutoTarget();
        }

        private void TickSpells()
        {
            foreach (var spell in _preparedSpells)
                spell?.Tick(Time.deltaTime);
        }

        private void OnAttackInput(InputAction.CallbackContext context)
        {
            if (_isAiming)
            {
                ConfirmAiming();
                return;
            }

            // TODO: basic attack (non-spell)
        }

        private void OnSpellInput(int index)
        {
            if (GetSpell(index) == null)
            {
                Debug.LogWarning($"[PlayerController] No spell equipped in slot {index}");
                return;
            }

            CastSpellSlot(index);
        }

        private void AutocastSpells()
        {
            foreach (var spell in _preparedSpells)
            {
                // Manual (TargetPosition) spells need a click to place - can't autocast.
                if (spell != null && spell.Autocast && spell.Info.TargetingType != SpellTargetingType.TargetPosition)
                    TryCastSpell(spell);
            }
        }

        // Enters aiming mode instead of casting immediately - the next Attack
        // (left-click) confirms the raycast ground position and casts from there.
        private void BeginAiming(UnitSpellInstance spell)
        {
            _aimingSpell = spell;
            _isAiming = true;
            _aimPosition = transform.position + transform.forward * spell.Info.Range;
        }

        private void UpdateAiming()
        {
            var screenPos = InputManager.Instance.PlayerInput.actions["MovePointer"].ReadValue<Vector2>();
            var ray = Camera.main.ScreenPointToRay(screenPos);

            if (Physics.Raycast(ray, out var hit, 200f, _groundLayerMask))
            {
                _aimPosition = hit.point;
            }
        }

        private void ConfirmAiming()
        {
            _isAiming = false;
            var spell = _aimingSpell;
            _aimingSpell = null;

            var context = new SpellCastContext
            {
                Info           = spell.Info,
                Caster         = Unit,
                Target         = null,
                CastPosition   = transform.position,
                TargetPosition = _aimPosition,
            };

            spell.TryCast(context);
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

        // Automatic-mode spell targeting: highest ThreatRating in range, ties broken by distance.
        private Unit FindAutoTarget()
        {
            var mask = LayerMask.GetMask("Character");
            var hits = Physics.OverlapSphere(transform.position, 30f, mask);

            Unit best = null;
            float bestThreat = float.NegativeInfinity;
            float bestDistSqr = float.MaxValue;

            foreach (var hit in hits)
            {
                var unit = hit.GetComponent<Unit>();
                if (unit == null || unit == Unit || unit.TeamId == Unit.TeamId || !unit.IsAlive)
                    continue;

                float distSqr = (unit.transform.position - transform.position).sqrMagnitude;

                bool isBetter = unit.ThreatRating > bestThreat ||
                    (Mathf.Approximately(unit.ThreatRating, bestThreat) && distSqr < bestDistSqr);

                if (isBetter)
                {
                    best = unit;
                    bestThreat = unit.ThreatRating;
                    bestDistSqr = distSqr;
                }
            }

            return best;
        }

        private void OnDeathAnimationFinished()
        {
            gameObject.SetActive(false);
            Died?.Invoke();
        }
    }
}

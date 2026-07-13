using UnityEngine;
using UnityEngine.InputSystem;
using magus.input;
using magus.battle;
using magus.master;

namespace magus.chara
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private Unit _unit;
        [SerializeField] private GameCharaController _charaController;
        [SerializeField] private PlayerProgression _progression;

        public Unit Unit => _unit;
        public PlayerProgression Progression => _progression;

		private UnitSpellInstance[] _preparedSpells = new UnitSpellInstance[3];
        private Unit _targetEnemy;

        private void Awake()
        {
            _unit.Killed += OnUnitKilled;
            _charaController.DeathAnimationFinished += OnDeathAnimationFinished;
        }

        public void Initialize()
        {
            InputManager.Instance.AddActionCallback(InputManager.PlayerInputType.Attack,     InputManager.InputPhase.Performed, OnAttackInput);
            InputManager.Instance.AddActionCallback(InputManager.PlayerInputType.Ability_01, InputManager.InputPhase.Performed, (c) => OnSpellInput(0));
            InputManager.Instance.AddActionCallback(InputManager.PlayerInputType.Ability_02, InputManager.InputPhase.Performed, (c) => OnSpellInput(1));

            // TODO: bind third spell slot when input action is available
            // TODO: load spells from run data / spell draft instead of hardcoding
            LoadDefaultSpells();

            _unit.Setup();
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
                _preparedSpells[0] = new UnitSpellInstance(new SpellInfo(fireballMaster), _unit);

            // TODO: populate slots 1 and 2 once more spells exist
        }

        private void Update()
        {
            if (!_unit.IsAlive) return;
            UpdateMoveVector();
            UpdateTarget();
            TickSpells();
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

            var targetPos = _targetEnemy != null
                ? _targetEnemy.transform.position
                : transform.position + transform.forward * spell.Info.Range;

            var context = new SpellCastContext
            {
                Info           = spell.Info,
                Caster         = _unit,
                Target         = _targetEnemy,
                CastPosition   = transform.position,
                TargetPosition = targetPos,
            };

            spell.TryCast(context);
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
                if (unit == null || unit == _unit || unit.TeamId == _unit.TeamId || !unit.IsAlive)
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

        private void OnUnitKilled()
        {
            _charaController.StartDeathAnimation();
        }

        private void OnDeathAnimationFinished()
        {
            gameObject.SetActive(false);
        }
    }
}

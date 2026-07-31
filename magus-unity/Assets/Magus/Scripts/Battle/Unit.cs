using System;
using magus.master;
using UnityEngine;

namespace magus.battle
{
    /// <summary>
    /// Root combat object. Owns team identity, HP, and mana.
    /// Attach to any entity that participates in combat.
    /// </summary>
    public class Unit : MonoBehaviour, IBattleEntity
    {
        [SerializeField] private int _teamId;
        [SerializeField] private float _maxHp;
        [SerializeField] private float _maxMana;

        [SerializeField]
        private Transform _projectileOrigin;

        [SerializeField]
        private float _threatRating;

        public int TeamId => _teamId;

        /// <summary>Relative priority as an automatic-targeting pick. Automatic-mode spell
        /// targeting picks the highest ThreatRating in range, ties broken by distance.</summary>
        public float ThreatRating => _threatRating;

        public GameObject GameObject => gameObject;

        public ModifierCollection Modifiers { get; } = new ModifierCollection();

        public float CurrentHp { get; private set; }
        public float MaxHp => _maxHp;
        public bool IsAlive => CurrentHp > 0f;

        public float CurrentMana { get; private set; }
        public float MaxMana => _maxMana;

        public bool IsCasting { get; private set; }

        public Transform ProjectileOrigin => _projectileOrigin;

        /// <summary>Source of the most recent hit received. Used by LootDropper to
        /// populate LootContext.Killer since Killed itself carries no payload.</summary>
        public IBattleEntity LastDamageSource { get; private set; }

        private LevelProgressionTable _levelProgressionTable;

        public int Level { get; private set; } = 1;
        public int CurrentExperience { get; private set; }

        public event Action Killed;
        public event Action<DamageInfo> DamageReceived;
        public event Action<float> ManaChanged;
        public event Action CastInterrupted;
        public event Action<int> LeveledUp;

        public void Setup()
        {
            CurrentHp = _maxHp;
            CurrentMana = _maxMana;
            IsCasting = false;
        }

        public void StartCast()
        {
            IsCasting = true;
        }

        public void EndCast()
        {
            IsCasting = false;
        }

        /// <summary>
        /// Cancels an active cast. Called by ReceiveDamage on a heavy hit, or by stun.
        /// No-op if not currently casting.
        /// </summary>
        public void InterruptCast()
        {
            if (!IsCasting) return;
            IsCasting = false;
            CastInterrupted?.Invoke();
        }

        public void ReceiveDamage(DamageInfo info)
        {
            if (!IsAlive) return;
            CurrentHp = Mathf.Max(0f, CurrentHp - info.Amount);
            LastDamageSource = info.Source;
            DamageReceived?.Invoke(info);
            if (!IsAlive)
            {
                OnKilled();
            }
        }

        public void Heal(float amount)
        {
            if (!IsAlive) return;
            CurrentHp = Mathf.Min(_maxHp, CurrentHp + amount);
        }

        public bool TrySpendMana(float amount)
        {
            if (CurrentMana < amount) return false;
            CurrentMana -= amount;
            ManaChanged?.Invoke(CurrentMana);
            return true;
        }

        public void RestoreMana(float amount)
        {
            CurrentMana = Mathf.Min(_maxMana, CurrentMana + amount);
            ManaChanged?.Invoke(CurrentMana);
        }

        public void Kill()
        {
            CurrentHp = 0;
        }

        /// <summary>Assigns the table used to resolve XP thresholds for leveling up.</summary>
        public void AssignLevelProgressionTable(LevelProgressionTable table)
        {
            _levelProgressionTable = table;
        }

        /// <summary>
        /// Applies base stats from a UnitMasterData and resolves its LevelProgressionTable,
        /// if any. Does not reset current HP/mana - call Setup() afterwards for that.
        /// </summary>
        public void ApplyMasterData(UnitMasterData masterData)
        {
            _maxHp = masterData.BaseMaxHp;
            _maxMana = masterData.BaseMaxMana;
            _threatRating = masterData.BaseThreatRating;

            if (!string.IsNullOrEmpty(masterData.LevelProgressionTableId))
            {
                var progressionData = MasterData.GetMasterData<LevelProgressionMasterData>(masterData.LevelProgressionTableId);
                if (progressionData != null)
                    AssignLevelProgressionTable(new LevelProgressionTable(progressionData));
            }
        }

        /// <summary>
        /// Grants XP and levels up as many times as the table allows. No-op without an
        /// assigned LevelProgressionTable, or once the table's max level is reached.
        /// </summary>
        public void AddExperience(int amount)
        {
            if (amount <= 0 || _levelProgressionTable == null) return;

            CurrentExperience += amount;

            while (_levelProgressionTable.TryGetXpToNextLevel(Level, out var xpToNextLevel) && CurrentExperience >= xpToNextLevel)
            {
                CurrentExperience -= xpToNextLevel;
                Level++;
                LeveledUp?.Invoke(Level);
            }
        }

        private void OnKilled()
        {
			InterruptCast();

			Killed?.Invoke();
		}

    }
}

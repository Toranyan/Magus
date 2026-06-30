using System;
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

        public int TeamId => _teamId;
        public GameObject GameObject => gameObject;

        public float CurrentHp { get; private set; }
        public float MaxHp => _maxHp;
        public bool IsAlive => CurrentHp > 0f;

        public float CurrentMana { get; private set; }
        public float MaxMana => _maxMana;

        public event Action Killed;
        public event Action<DamageInfo> DamageReceived;
        public event Action<float> ManaChanged;

        public void Setup()
        {
            CurrentHp = _maxHp;
            CurrentMana = _maxMana;
        }

        public void ReceiveDamage(DamageInfo info)
        {
            if (!IsAlive) return;
            CurrentHp = Mathf.Max(0f, CurrentHp - info.Amount);
            DamageReceived?.Invoke(info);
            if (!IsAlive) Killed?.Invoke();
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
    }
}

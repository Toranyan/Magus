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

        [SerializeField]
        private Transform _projectileOrigin;

        public int TeamId => _teamId;
        public GameObject GameObject => gameObject;

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

        public event Action Killed;
        public event Action<DamageInfo> DamageReceived;
        public event Action<float> ManaChanged;
        public event Action CastInterrupted;

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

        private void OnKilled()
        {
			InterruptCast();

			Killed?.Invoke();
		}

    }
}

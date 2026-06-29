using System;
using UnityEngine;

namespace magus.battle
{
    /// <summary>
    /// Manages current and maximum HP for a combat entity.
    /// Does not know about collisions, teams, or damage sources.
    /// </summary>
    public class Health : MonoBehaviour
    {
        [SerializeField]
        private float _maxHp;

        public float CurrentHp { get; private set; }
        public float MaxHp => _maxHp;
        public bool IsDead => CurrentHp <= 0f;

        public event Action Died;
        public event Action<float> DamageApplied;
        public event Action<float> Healed;

        public void Setup()
        {
            CurrentHp = _maxHp;
        }

        public void Setup(float maxHp)
        {
            _maxHp = maxHp;
            CurrentHp = _maxHp;
        }

        public void ApplyDamage(float amount)
        {
            if (IsDead) return;
            CurrentHp = Mathf.Max(0f, CurrentHp - amount);
            DamageApplied?.Invoke(amount);
            if (IsDead)
            {
                Died?.Invoke();
            }
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
            Healed?.Invoke(amount);
        }
    }
}

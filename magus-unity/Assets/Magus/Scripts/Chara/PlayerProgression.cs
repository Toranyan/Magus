using System;
using UnityEngine;

namespace magus.chara
{
    /// <summary>
    /// Stub for player run progression. Tracks experience and currency totals so
    /// PickupEffects (Grant Experience, Add Currency) have a system to call into.
    /// Leveling/spending are not implemented yet - this only accumulates totals.
    /// </summary>
    public class PlayerProgression : MonoBehaviour
    {
        public int CurrentExperience { get; private set; }
        public int CurrentCurrency { get; private set; }

        public event Action<int> ExperienceChanged;
        public event Action<int> CurrencyChanged;

        public void AddExperience(int amount)
        {
            if (amount <= 0) return;
            CurrentExperience += amount;
            ExperienceChanged?.Invoke(CurrentExperience);
        }

        public void AddCurrency(int amount)
        {
            if (amount <= 0) return;
            CurrentCurrency += amount;
            CurrencyChanged?.Invoke(CurrentCurrency);
        }
    }
}

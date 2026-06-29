using System;
using UnityEngine;

namespace magus.battle
{
    /// <summary>
    /// Root combat object. Owns team identity, health, and damage coordination.
    /// Attach alongside Health on any entity that participates in combat.
    /// </summary>
    public class Unit : MonoBehaviour, IBattleEntity
    {
        [SerializeField]
        private int _teamId;

        [SerializeField]
        private Health _health;

        public int TeamId => _teamId;
        public GameObject GameObject => gameObject;
        public Health Health => _health;
        public bool IsAlive => _health != null && !_health.IsDead;

        public event Action Killed;
        public event Action<DamageInfo> DamageReceived;

        private void Awake()
        {
            if (_health != null)
            {
                _health.Died += OnHealthDied;
            }
        }

        public void Setup()
        {
            _health?.Setup();
        }

        /// <summary>
        /// Entry point for all incoming damage. Called by DamageReceiver.
        /// </summary>
        public void ReceiveDamage(DamageInfo info)
        {
            if (!IsAlive) return;
            _health?.ApplyDamage(info.Amount);
            DamageReceived?.Invoke(info);
        }

        private void OnHealthDied()
        {
            Killed?.Invoke();
        }
    }
}

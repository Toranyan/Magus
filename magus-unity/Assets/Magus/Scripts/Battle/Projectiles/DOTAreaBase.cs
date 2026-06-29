using System.Collections.Generic;
using UnityEngine;

namespace magus.battle
{
    /// <summary>
    /// Applies periodic damage to all DamageReceivers within its trigger volume.
    /// Requires a DamageDealer with PreventDuplicateHits = false so ticks can hit the same target repeatedly.
    /// </summary>
    public class DOTAreaBase : MonoBehaviour
    {
        [SerializeField]
        private float _damageTickInterval;

        [SerializeField]
        private DamageDealer _damageDealer;

        private readonly HashSet<DamageReceiver> _receivers = new();
        private float _timeSinceLastTick;

        public void SetOwner(IBattleEntity owner)
        {
            _damageDealer?.SetOwner(owner);
        }

        private void OnEnable()
        {
            _damageDealer?.BeginAttack();
        }

        private void OnDisable()
        {
            _damageDealer?.EndAttack();
            _receivers.Clear();
            _timeSinceLastTick = 0f;
        }

        private void Update()
        {
            if (_damageTickInterval <= 0f) return;

            _timeSinceLastTick += Time.deltaTime;
            if (_timeSinceLastTick >= _damageTickInterval)
            {
                _timeSinceLastTick = 0f;
                ApplyDamageTick();
            }
        }

        private void ApplyDamageTick()
        {
            if (_receivers.Count == 0 || _damageDealer == null) return;

            var toRemove = new List<DamageReceiver>();

            foreach (var r in _receivers)
            {
                if (r == null)
                {
                    toRemove.Add(r);
                    continue;
                }
                _damageDealer.TryDamage(r, r.transform.position);
            }

            foreach (var rem in toRemove)
            {
                _receivers.Remove(rem);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var receiver = other.GetComponent<DamageReceiver>();
            if (receiver != null)
            {
                _receivers.Add(receiver);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var receiver = other.GetComponent<DamageReceiver>();
            if (receiver != null)
            {
                _receivers.Remove(receiver);
            }
        }
    }
}

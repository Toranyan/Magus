using System.Collections.Generic;
using UnityEngine;

namespace magus.battle
{

    public class DOTAreaBase : MonoBehaviour
    {

        [SerializeField]
        private float _damageTickInterval;

        [SerializeField]
        private float _damagePerTick;

        [SerializeField]
        private DamageType _damageType;

        private IBattleEntity _owner;

        private readonly HashSet<DamageReceiver> _receivers = new HashSet<DamageReceiver>();

        private float _timeSinceLastTick;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (_damageTickInterval <= 0f)
            {
                // if interval is zero or negative, do nothing
                return;
            }

            _timeSinceLastTick += Time.deltaTime;
            if (_timeSinceLastTick >= _damageTickInterval)
            {
                _timeSinceLastTick = 0f;
                ApplyDamageTick();
            }
        }

        public void SetOwner(IBattleEntity owner)
        {
            _owner = owner;
        }

        private void ApplyDamageTick()
        {
            if (_receivers.Count == 0)
            {
                return;
            }

            var toRemove = new List<DamageReceiver>();

            foreach (var r in _receivers)
            {
                if (r == null)
                {
                    toRemove.Add(r);
                    continue;
                }

                // if owner exists and same team, skip
                if (_owner != null && r.TeamId == _owner.TeamId)
                {
                    continue;
                }

                r.Damage(new DamageInfo(
                    amount: _damagePerTick,
                    location: r.transform.position,
                    receiver: r.GetComponent<IBattleEntity>(),
					source: _owner,
                    type: _damageType
				));
            }

            // cleanup null entries
            foreach (var rem in toRemove)
            {
                _receivers.Remove(rem);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var receiver = other.gameObject.GetComponent<DamageReceiver>();
            if (receiver != null)
            {
                _receivers.Add(receiver);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var receiver = other.gameObject.GetComponent<DamageReceiver>();
            if (receiver != null)
            {
                _receivers.Remove(receiver);
            }
        }

        private void OnDisable()
        {
            _receivers.Clear();
            _timeSinceLastTick = 0f;
        }
    }

}
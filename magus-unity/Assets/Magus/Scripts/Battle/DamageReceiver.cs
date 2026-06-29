using System;
using UnityEngine;

namespace magus.battle
{
    /// <summary>
    /// Attached to colliders that can receive damage.
    /// Holds a reference to the owning Unit and forwards hits to it.
    /// Multiple DamageReceivers (body, head, weak point) can map to the same Unit.
    /// </summary>
    public class DamageReceiver : MonoBehaviour
    {
        [SerializeField]
        private Unit _unit;

        /// <summary>Fallback team ID when no Unit is assigned (editor transition support).</summary>
        [SerializeField]
        private int _teamId;

        public Unit Unit => _unit;

        public int TeamId => _unit != null ? _unit.TeamId : _teamId;

        public event Action<DamageInfo> DamageReceived;

        public static event Action<DamageInfo> GlobalDamageReceived;

        public void ClearEvents()
        {
            DamageReceived = null;
        }

        public void Damage(DamageInfo damageInfo)
        {
            _unit?.ReceiveDamage(damageInfo);
            DamageReceived?.Invoke(damageInfo);
            GlobalDamageReceived?.Invoke(damageInfo);
        }
    }
}

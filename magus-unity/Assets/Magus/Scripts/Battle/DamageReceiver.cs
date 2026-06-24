using System;
using UnityEngine;

namespace magus.battle
{
    public class DamageReceiver : MonoBehaviour
    {
        [SerializeField]
        private int _teamId;

        public event Action<DamageInfo> DamageReceived;

        public static event Action<DamageInfo> GlobalDamageReceived;

        public int TeamId { get => _teamId; }

		public void Restart()
		{
            ClearEvents();
		}

		public void ClearEvents()
		{
            DamageReceived = null;
		}

        public void Setup(int teamId)
		{
            _teamId = teamId;
		}

        public void Damage(DamageInfo damageInfo)
		{
            Debug.Log($"Damage recieved : {damageInfo}");
            DamageReceived?.Invoke(damageInfo);
            GlobalDamageReceived?.Invoke(damageInfo);
        }
    }
}

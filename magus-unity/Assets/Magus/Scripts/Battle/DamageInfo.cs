
using UnityEngine;

namespace magus.battle
{
    public class DamageInfo
    {
        public float Amount;
        public IBattleEntity Source;
        public IBattleEntity Receiver;
        public DamageType Type;
        public Vector3 Location;
        public DamageInfo(float amount, IBattleEntity source, IBattleEntity receiver, DamageType type, Vector3 location)
        {
            Amount = amount;
            Source = source;
            Receiver = receiver;
            Type = type;
            Location = location;
		}

	}
}

using UnityEngine;

namespace magus.battle
{
    public class AbilityExecutionContext
    {
        public AbilityInfo Info;

        public IBattleEntity Owner;

		public IBattleEntity Source;
        public IBattleEntity Target;

        public Vector3 SourcePosition;
        public Vector3 TargetPosition;

    }
}

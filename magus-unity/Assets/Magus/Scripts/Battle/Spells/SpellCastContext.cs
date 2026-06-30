using UnityEngine;

namespace magus.battle
{
    public class SpellCastContext
    {
        public SpellInfo Info;

        public IBattleEntity Caster;
        public IBattleEntity Target;    // null if untargeted

        public Vector3 CastPosition;
        public Vector3 TargetPosition;

    }
}

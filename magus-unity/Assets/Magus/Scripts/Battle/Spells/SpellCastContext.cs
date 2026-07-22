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

        // Info.CastTime resolved through the caster's Modifiers at the moment
        // TryCast committed to this cast. UnitSpellInstance.CastAsync waits this long
        // (if > 0) before invoking the executor - executors themselves always run
        // post-cast-time and don't need to read this.
        public float ResolvedCastTime;
    }
}

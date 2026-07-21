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
        // TryCast committed to this cast. No consumer reads this yet — casting is
        // still instant (see the cast-time TODO in ProjectileSpellExecutor) — but
        // executors that gate on cast time should read this instead of Info.CastTime.
        public float ResolvedCastTime;
    }
}

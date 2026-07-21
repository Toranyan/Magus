using System.Collections.Generic;
using System.Linq;

namespace magus.battle
{
    // Holds every active Modifier for one context (a Unit, or a specific
    // spell/projectile instance). See Docs/Design/Modifier System.md for the
    // resolution formula and scope-matching rules this implements.
    public class ModifierCollection
    {
        private readonly List<Modifier> _modifiers = new List<Modifier>();

        public void Add(Modifier modifier) => _modifiers.Add(modifier);

        // Called when an owner (Buff/Debuff/Skill/Item) is itself removed.
        public void RemoveAll(ModifierOwnerId owner) => _modifiers.RemoveAll(m => m.Owner == owner);

        // Three-stage pipeline: AddToBase, then Percent as one multiplier, then
        // AddToFinal. Override bypasses the pipeline and wins outright via priority.
        public float Resolve(ModifierType type, float baseValue, IReadOnlyList<ModifierTag> contextTags = null)
        {
            var matching = _modifiers
                .Where(m => m.Type == type && m.Operation != ModifierOperation.Flag && Matches(m, contextTags))
                .ToList();

            var overrides = matching.Where(m => m.Operation == ModifierOperation.Override).ToList();
            if (overrides.Count > 0)
            {
                var winner = overrides
                    .OrderByDescending(m => m.Priority)
                    .ThenByDescending(m => m.AcquiredAt)
                    .First();
                return winner.Value;
            }

            float addToBaseSum = matching.Where(m => m.Operation == ModifierOperation.AddToBase).Sum(m => m.Value);
            float percentSum = matching.Where(m => m.Operation == ModifierOperation.Percent).Sum(m => m.Value);
            float addToFinalSum = matching.Where(m => m.Operation == ModifierOperation.AddToFinal).Sum(m => m.Value);

            float intermediateBase = baseValue + addToBaseSum;
            float afterPercent = intermediateBase * (1f + percentSum);
            return afterPercent + addToFinalSum;
        }

        // Boolean gate — OR across every active Flag modifier of this Type whose Tags match.
        public bool HasFlag(ModifierType type, IReadOnlyList<ModifierTag> contextTags = null)
        {
            return _modifiers.Any(m => m.Type == type && m.Operation == ModifierOperation.Flag && Matches(m, contextTags));
        }

        // Global (no Tags) always matches. Tagged matches on any overlap with contextTags.
        private static bool Matches(Modifier modifier, IReadOnlyList<ModifierTag> contextTags)
        {
            if (modifier.Tags.Length == 0) return true;
            if (contextTags == null || contextTags.Count == 0) return false;

            for (int i = 0; i < modifier.Tags.Length; i++)
            {
                for (int j = 0; j < contextTags.Count; j++)
                {
                    if (modifier.Tags[i] == contextTags[j]) return true;
                }
            }

            return false;
        }
    }
}

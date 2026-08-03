using System;
using System.Collections.Generic;
using UnityEngine.Localization;

namespace magus.dialogue
{
    public enum DialogueEntryKind
    {
        Text,
        Choice
    }

    /// <summary>Single concrete class with a Kind discriminator, not polymorphic subclasses -
    /// a plain List&lt;DialogueEntry&gt; of one concrete type needs zero custom editor code
    /// (Unity's default Inspector list drawer handles add/remove/reorder out of the box),
    /// unlike a [SerializeReference] list, which needs one (see GraphFramework.md's
    /// BuildSerializeReferenceListField). Worth the few unused fields per Kind given that.</summary>
    [Serializable]
    public class DialogueEntry
    {
        public DialogueEntryKind Kind;

        // Text fields
        public string CharacterId;
        public string Expression;
        public LocalizedString Text;

        // Choice fields - picking an option never branches within this asset (see
        // Docs/Design/DialogueSystem.md#dialogue-data); it only records a response for a
        // later StoryNode to read, via VariableKey, then always continues to the next entry.
        public List<DialogueChoiceOption> Options = new List<DialogueChoiceOption>();

        /// <summary>StoryBlackboard variable key the chosen option's ResponseValue is recorded
        /// under. Empty/null means the choice isn't recorded anywhere.</summary>
        public string VariableKey;
    }

    [Serializable]
    public class DialogueChoiceOption
    {
        public LocalizedString Text;
        public string ResponseValue;
    }
}

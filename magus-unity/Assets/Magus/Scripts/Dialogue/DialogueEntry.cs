using System;
using System.Collections.Generic;
using UnityEngine.Localization;

namespace magus.dialogue
{
    public enum DialogueEntryKind
    {
        Text,
        Choice,

        /// <summary>Prompts the player to type a value (e.g. their name). Text is shown as
        /// the prompt; the submitted value is recorded under VariableKey, same as a Choice.</summary>
        TextInput
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

        /// <summary>StoryBlackboard variable key the chosen option's ResponseValue (Choice)
        /// or the typed value (TextInput) is recorded under. Empty/null means it isn't
        /// recorded anywhere. Later entries can show a recorded string with {VariableKey} -
        /// see DialogueManager.FormatVariables.</summary>
        public string VariableKey;

        // TextInput fields
        /// <summary>Pre-filled into the input field, and used if the player submits it empty.</summary>
        public string DefaultValue;

        /// <summary>0 = unlimited.</summary>
        public int MaxLength = 16;
    }

    [Serializable]
    public class DialogueChoiceOption
    {
        public LocalizedString Text;
        public string ResponseValue;
    }
}

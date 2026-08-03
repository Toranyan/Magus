using System;
using UnityEngine;

namespace magus.dialogue
{
    /// <summary>Linear index-based traversal through a DialogueAsset's Entries - no graph,
    /// no branching within the asset. Stays free of any EventBus/story dependency by design:
    /// ChooseOption() surfaces a recorded response via ResponseRecorded, and it's
    /// DialogueManager's job (not this class's) to actually publish that to EventBus - see
    /// Docs/Design/DialogueSystem.md#dialoguerunner.</summary>
    public class DialogueRunner
    {
        public DialogueAsset Asset { get; private set; }
        public DialogueEntry CurrentEntry { get; private set; }
        public bool IsPlaying { get; private set; }

        private int _index = -1;

        public event Action<DialogueEntry> EntryChanged;
        public event Action Ended;

        /// <summary>Fired when a Choice entry's option is picked and it has a VariableKey set -
        /// DialogueManager subscribes and publishes DialogueChoiceMadeEvent on EventBus.</summary>
        public event Action<string, string> ResponseRecorded;

        public void Start(DialogueAsset asset)
        {
            Asset = asset;
            _index = -1;
            IsPlaying = true;
            Advance();
        }

        /// <summary>Moves to the next entry. Not valid on a Choice entry (call ChooseOption
        /// instead) or once the conversation has ended.</summary>
        public void Advance()
        {
            if (!IsPlaying)
            {
                return;
            }

            if (CurrentEntry != null && CurrentEntry.Kind == DialogueEntryKind.Choice)
            {
                Debug.LogWarning("[DialogueRunner] Advance() called on a Choice entry - call ChooseOption() instead.");
                return;
            }

            MoveNext();
        }

        public void ChooseOption(int optionIndex)
        {
            if (!IsPlaying || CurrentEntry == null || CurrentEntry.Kind != DialogueEntryKind.Choice)
            {
                Debug.LogWarning("[DialogueRunner] ChooseOption() called while not on a Choice entry.");
                return;
            }

            if (optionIndex < 0 || optionIndex >= CurrentEntry.Options.Count)
            {
                Debug.LogWarning($"[DialogueRunner] ChooseOption({optionIndex}) out of range.");
                return;
            }

            var option = CurrentEntry.Options[optionIndex];
            if (!string.IsNullOrEmpty(CurrentEntry.VariableKey))
            {
                ResponseRecorded?.Invoke(CurrentEntry.VariableKey, option.ResponseValue);
            }

            // Every option continues to the same next entry - see DialogueEntry's Choice fields.
            MoveNext();
        }

        private void MoveNext()
        {
            _index++;

            if (Asset == null || _index >= Asset.Entries.Count)
            {
                Stop();
                return;
            }

            CurrentEntry = Asset.Entries[_index];
            EntryChanged?.Invoke(CurrentEntry);
        }

        public void Stop()
        {
            IsPlaying = false;
            CurrentEntry = null;
            Ended?.Invoke();
        }
    }
}

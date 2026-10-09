using System;
using System.Collections.Generic;
using UnityEngine;
using magus.chara;
using magus.dialogue;
using magus.story;

namespace magus.interaction
{
    /// <summary>An NPC the player can talk to. On Interact, picks which conversation to play
    /// from an ordered rule list - the first rule whose Conditions all pass wins, else the
    /// Default dialogue. Rules read the same saved StoryBlackboard as the story graph, so an
    /// NPC "remembers" through flags rather than state of its own, e.g.:
    ///
    ///   1. [StoryFlagCondition village_quest_done]               -> Dialogue/smith_thanks
    ///   2. [ConversationSeenCondition Dialogue/smith_intro]      -> Dialogue/smith_reminder
    ///   Default                                                  -> Dialogue/smith_intro
    ///
    /// Put the most specific rules first. See Docs/Design/DialogueSystem.md#npc-dialogue.</summary>
    public class NpcDialogue : Interactable
    {
        [Tooltip("Checked top to bottom - the first rule whose conditions all pass is played.")]
        [SerializeField] private List<NpcDialogueRule> _rules = new List<NpcDialogueRule>();

        [Tooltip("Played when no rule matches. Leave empty to make the NPC silent (and unfocusable) when nothing matches.")]
        [SerializeField] private string _defaultDialogueAddress;

        public override bool CanInteract => base.CanInteract && !string.IsNullOrEmpty(ResolveDialogueAddress());

        public override void Interact(PlayerController player)
        {
            var address = ResolveDialogueAddress();
            if (string.IsNullOrEmpty(address))
            {
                return;
            }

            DialogueManager.Instance.Play(address);
        }

        /// <summary>The conversation talking to this NPC would play right now, or null.</summary>
        public string ResolveDialogueAddress()
        {
            foreach (var rule in _rules)
            {
                if (rule != null && !string.IsNullOrEmpty(rule.DialogueAddress) && StoryManager.Instance.CheckConditions(rule.Conditions))
                {
                    return rule.DialogueAddress;
                }
            }

            return string.IsNullOrEmpty(_defaultDialogueAddress) ? null : _defaultDialogueAddress;
        }
    }

    [Serializable]
    public class NpcDialogueRule
    {
        [Tooltip("For your own reference in the Inspector - not used at runtime.")]
        public string Note;

        [Tooltip("All must pass. Same condition types as StoryNode (flags, variables, conversations seen).")]
        [SerializeReference, SubclassPicker]
        public List<IStoryCondition> Conditions = new List<IStoryCondition>();

        [Tooltip("Addressables address of the DialogueAsset, e.g. \"Dialogue/smith_intro\".")]
        public string DialogueAddress;
    }
}

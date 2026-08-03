using System.Collections.Generic;
using UnityEngine;

namespace magus.dialogue
{
    /// <summary>Plain sequential list, not a graph - see Docs/Design/DialogueSystem.md#dialogue-data
    /// for why. Branching between conversations is a StoryNode's job, not this asset's.</summary>
    [CreateAssetMenu(fileName = "DialogueAsset", menuName = "Magus/Dialogue/Dialogue Asset")]
    public class DialogueAsset : ScriptableObject
    {
        public List<DialogueEntry> Entries = new List<DialogueEntry>();
    }
}

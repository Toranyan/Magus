using System;
using UnityEngine;

namespace magus.master
{
    /// <summary>v1: DisplayName + one Portrait only - see Docs/Design/DialogueSystem.md#character-assets.
    /// Expressions/ThemeColor/Voice deferred until a node/UI actually needs them.</summary>
    [Serializable]
    public class CharacterMasterData : BaseMasterData
    {
        public string DisplayName;

        [Tooltip("Addressables key for this character's portrait sprite.")]
        public string PortraitAssetKey;
    }
}

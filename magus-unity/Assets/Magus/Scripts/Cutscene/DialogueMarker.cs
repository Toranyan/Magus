using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace magus.cutscene
{
    /// <summary>Timeline marker that plays a dialogue, carrying its own Addressables address -
    /// so one Timeline can hold any number of different conversations, each marker pointing
    /// at its own. (A Signal Emitter can't do this: Signal Receiver reactions are keyed by
    /// Signal Asset, so every emitter sharing one asset plays the same fixed address.)
    ///
    /// Add it from the Timeline window's Markers row (right-click > Add Dialogue Marker),
    /// or on the Signal Track. Delivered to DialogueCutsceneSignal.OnNotify, which must be on
    /// the PlayableDirector's GameObject (for the Markers row) or the track's bound
    /// GameObject - in a cutscene prefab these are normally the same root object.</summary>
    [Serializable]
    [DisplayName("Dialogue Marker")]
    public class DialogueMarker : Marker, INotification, INotificationOptionProvider
    {
        [Tooltip("Addressables address of the DialogueAsset, e.g. \"Dialogue/ch01_0001\".")]
        public string DialogueAddress;

        [Tooltip("Freeze the Timeline here until the dialogue ends (actors hold their pose). " +
                 "Off: the Timeline keeps playing underneath the dialogue.")]
        public bool PauseTimeline = true;

        public PropertyName id => new PropertyName(nameof(DialogueMarker));

        // Fire once per play, and still fire if playback jumps past the marker (a skipped
        // frame shouldn't lose a conversation).
        public NotificationFlags flags => NotificationFlags.TriggerOnce | NotificationFlags.Retroactive;
    }
}

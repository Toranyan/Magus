using UnityEngine;

namespace magus.cutscene
{
    /// <summary>Bridges a Timeline Signal to dialogue. Place on a GameObject inside the
    /// cutscene prefab (e.g. the same one as the PlayableDirector, so it's already there at
    /// author time - no runtime actor resolution needed for this). In the Timeline window:
    /// add a Signal Track, bind it to that GameObject, add a Signal Emitter at the point the
    /// conversation should start, create/assign a Signal Asset for it, then add a Signal
    /// Receiver component to the same GameObject with a reaction for that signal pointing
    /// at PlayDialogue - Unity's UnityEvent Inspector lets you set the dialogue's
    /// Addressables address as a fixed string argument right there, no code needed per
    /// cutscene.
    ///
    /// Goes through CutsceneManager.PlayDialogueDuringCutscene rather than calling
    /// DialogueManager.Play() directly, so the cutscene isn't considered finished (and
    /// doesn't publish CutsceneFinishedEvent) until this dialogue has also ended - see
    /// CutsceneManager.
    ///
    /// Two reactions to choose from in the Signal Receiver:
    /// - PlayDialogueAndWait: freezes the Timeline at the signal until the dialogue ends,
    ///   then continues. Use this for multi-beat cutscenes (pose -> talk -> animate -> talk)
    ///   - actors hold their pose while the dialogue is up.
    /// - PlayDialogue: the Timeline keeps playing underneath the dialogue.</summary>
    public class DialogueCutsceneSignal : MonoBehaviour
    {
        public void PlayDialogue(string dialogueAddress)
        {
            CutsceneManager.Instance.PlayDialogueDuringCutscene(dialogueAddress);
        }

        public void PlayDialogueAndWait(string dialogueAddress)
        {
            CutsceneManager.Instance.PlayDialogueDuringCutscene(dialogueAddress, pauseTimeline: true);
        }
    }
}

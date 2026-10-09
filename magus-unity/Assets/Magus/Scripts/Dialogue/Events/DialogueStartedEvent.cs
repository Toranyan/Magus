namespace magus.dialogue
{
    /// <summary>Published by DialogueManager when a conversation is requested while none was
    /// running - before its asset has even loaded, so the player is locked from the same
    /// frame they pressed talk. PlayerController takes an input lock on this.</summary>
    public class DialogueStartedEvent
    {
        public string DialogueAddress;
    }
}

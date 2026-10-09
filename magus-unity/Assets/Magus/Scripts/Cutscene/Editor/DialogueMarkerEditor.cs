using UnityEditor.Timeline;
using UnityEngine.Timeline;

namespace magus.cutscene.editor
{
    /// <summary>Shows a Dialogue Marker's address as its tooltip in the Timeline window, so
    /// markers can be told apart without selecting each one.</summary>
    [CustomTimelineEditor(typeof(DialogueMarker))]
    public class DialogueMarkerEditor : MarkerEditor
    {
        public override MarkerDrawOptions GetMarkerOptions(IMarker marker)
        {
            var options = base.GetMarkerOptions(marker);
            var dialogueMarker = (DialogueMarker)marker;

            options.tooltip = string.IsNullOrEmpty(dialogueMarker.DialogueAddress)
                ? "Dialogue Marker (no address set)"
                : $"{dialogueMarker.DialogueAddress}{(dialogueMarker.PauseTimeline ? " (pauses)" : "")}";

            if (string.IsNullOrEmpty(dialogueMarker.DialogueAddress))
            {
                options.errorText = "No DialogueAddress set.";
            }

            return options;
        }
    }
}

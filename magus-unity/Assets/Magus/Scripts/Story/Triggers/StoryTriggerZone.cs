using UnityEngine;
using tora.eventbus;
using magus.chara;

namespace magus.story
{
    /// <summary>Reports a StoryEvent when the player walks into this trigger collider - e.g.
    /// "entered_village". Place on a GameObject in a map prefab with a Collider set to
    /// Is Trigger. The player's CharacterController counts as a collider for trigger
    /// callbacks, so no Rigidbody is needed. Reporting again is harmless (the flag is just
    /// set again), but OncePerLoad avoids re-evaluating the graph every time the player
    /// steps back in.</summary>
    [RequireComponent(typeof(Collider))]
    public class StoryTriggerZone : MonoBehaviour
    {
        [Tooltip("Recorded as a story flag - gate a StoryNode on it with a StoryFlagCondition.")]
        [SerializeField] private string _eventId;

        [SerializeField] private bool _oncePerLoad = true;

        private bool _reported;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if ((_oncePerLoad && _reported) || string.IsNullOrEmpty(_eventId))
            {
                return;
            }

            if (other.GetComponentInParent<PlayerController>() == null)
            {
                return;
            }

            _reported = true;
            EventBus.Publish(new StoryEvent { Id = _eventId });
        }
    }
}

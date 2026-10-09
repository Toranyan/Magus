using UnityEngine;
using tora.eventbus;

namespace magus.story
{
    /// <summary>Turns its Targets on or off depending on a story flag - the "rebuild the world
    /// from facts" half of progression. Completed nodes' actions never re-run after a load,
    /// so anything a story beat changed in a map (boss gone, gate opened, NPC moved in) is
    /// restored by a gate reading the flag, not by replaying the beat. Also reacts live, so
    /// the change happens the moment the flag is set.
    ///
    /// Targets must not include this GameObject itself - a disabled gate stops listening.</summary>
    public class StoryFlagGate : MonoBehaviour
    {
        [SerializeField] private string _flag;

        [Tooltip("On: Targets are active only once the flag is set (e.g. an opened gate). " +
                 "Off: Targets are active only until it's set (e.g. a boss that's been killed).")]
        [SerializeField] private bool _activeWhenSet = true;

        [SerializeField] private GameObject[] _targets;

        [Tooltip("Also switch Targets the moment the flag changes. Turn off when the flag is " +
                 "set by the Target itself and it must finish what it's doing first - e.g. a boss " +
                 "whose StoryEventOnDeath would otherwise hide it mid death animation. It's still " +
                 "applied whenever this gate is enabled (map load / Continue).")]
        [SerializeField] private bool _reactLive = true;

        private void OnEnable()
        {
            EventBus.Subscribe<StoryStateChangedEvent>(OnStoryStateChanged);
            Apply();
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<StoryStateChangedEvent>(OnStoryStateChanged);
        }

        private void OnStoryStateChanged(StoryStateChangedEvent e)
        {
            if (_reactLive)
            {
                Apply();
            }
        }

        private void Apply()
        {
            var active = StoryManager.Instance.HasFlag(_flag) == _activeWhenSet;

            foreach (var target in _targets)
            {
                if (target != null && target != gameObject && target.activeSelf != active)
                {
                    target.SetActive(active);
                }
            }
        }
    }
}

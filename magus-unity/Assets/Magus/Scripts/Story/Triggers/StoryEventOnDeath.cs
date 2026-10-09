using UnityEngine;
using tora.eventbus;
using magus.battle;

namespace magus.story
{
    /// <summary>Reports a StoryEvent when this Unit dies - e.g. "slime_boss_killed". Attach
    /// alongside the Unit, same as LootDropper. On a regular pooled enemy every kill reports
    /// it again, which is harmless - the flag is simply already set.</summary>
    public class StoryEventOnDeath : MonoBehaviour
    {
        [SerializeField] private Unit _unit;

        [Tooltip("Recorded as a story flag - gate a StoryNode on it with a StoryFlagCondition.")]
        [SerializeField] private string _eventId;

        private void Awake()
        {
            if (_unit == null)
            {
                _unit = GetComponent<Unit>();
            }

            if (_unit != null)
            {
                _unit.Killed += OnUnitKilled;
            }
        }

        private void OnDestroy()
        {
            if (_unit != null)
            {
                _unit.Killed -= OnUnitKilled;
            }
        }

        private void OnUnitKilled()
        {
            if (!string.IsNullOrEmpty(_eventId))
            {
                EventBus.Publish(new StoryEvent { Id = _eventId });
            }
        }
    }
}

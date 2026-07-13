using System.Collections.Generic;
using UnityEngine;

namespace magus.battle
{
    /// <summary>
    /// Attach alongside a Unit (enemy, boss, chest, breakable) to roll its LootTable
    /// on death and forward the results to the scene's PickupSpawner.
    /// </summary>
    public class LootDropper : MonoBehaviour
    {
        [SerializeField] private Unit _unit;
        [SerializeField] private LootTable _lootTable;

        [Tooltip("TeamId allowed to collect the pickups this dropper produces. Defaults to 0, the player team, so enemies don't pick up their own drops.")]
        [SerializeField] private int _collectorTeamId = 0;

        private readonly List<PickupSpawnRequest> _requests = new();

        private void Awake()
        {
            if (_unit != null)
                _unit.Killed += OnUnitKilled;
        }

        private void OnUnitKilled()
        {
            if (_lootTable == null) return;

            var context = new LootContext
            {
                Killer = _unit.LastDamageSource,
                Victim = _unit,
                CollectorTeamId = _collectorTeamId,
            };

            _requests.Clear();
            _lootTable.Roll(context, _requests);

            foreach (var request in _requests)
                BattleController.Instance.PickupSpawner.Spawn(request);
        }
    }
}

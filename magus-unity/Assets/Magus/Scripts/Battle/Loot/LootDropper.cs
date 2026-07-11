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
            };

            _requests.Clear();
            _lootTable.Roll(context, _requests);

            foreach (var request in _requests)
                BattleController.Instance.PickupSpawner.Spawn(request);
        }
    }
}

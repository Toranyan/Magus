using System.Collections.Generic;
using magus.battle;
using UnityEngine;

namespace magus.ui
{
    public class UIStatusEffectList : MonoBehaviour
    {
        [SerializeField] private UIStatusEffectIndicator _indicatorPrefab;
        [SerializeField] private Transform _container;

        private readonly Dictionary<StatusEffectInstance, UIStatusEffectIndicator> _indicators =
            new Dictionary<StatusEffectInstance, UIStatusEffectIndicator>();

        public void AddStatusEffect(StatusEffectInstance instance)
        {
            if (_indicators.ContainsKey(instance)) return;

            var indicator = Instantiate(_indicatorPrefab, _container);
            indicator.SetStatusEffect(instance);
            _indicators.Add(instance, indicator);
        }

        public void RemoveStatusEffect(StatusEffectInstance instance)
        {
            if (!_indicators.TryGetValue(instance, out var indicator)) return;

            _indicators.Remove(instance);
            Destroy(indicator.gameObject);
        }
    }
}

using magus.battle;
using TMPro;
using UnityEngine;

namespace magus.ui
{
    // Skeleton - shows a status effect's name as raw text until icons/durations are designed.
    public class UIStatusEffectIndicator : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nameText;

        public void SetStatusEffect(StatusEffectInstance instance)
        {
            _nameText.text = instance.Data.Name;
        }
    }
}

using TMPro;
using UnityEngine;

namespace magus.ui
{
    // Skeleton - shows health as raw text until a proper bar display is designed.
    public class UIHealthIndicator : MonoBehaviour
    {
        [SerializeField] private TMP_Text _healthText;

        public void SetHealth(float currentHp, float maxHp)
        {
            _healthText.text = $"{currentHp:0}/{maxHp:0}";
        }
    }
}

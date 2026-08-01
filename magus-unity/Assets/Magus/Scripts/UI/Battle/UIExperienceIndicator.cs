using TMPro;
using UnityEngine;

namespace magus.ui
{
    // Skeleton - shows experience as raw text until a proper bar/level display is designed.
    public class UIExperienceIndicator : MonoBehaviour
    {
        [SerializeField] private TMP_Text _experienceText;

        public void SetExperience(int currentExperience)
        {
            _experienceText.text = currentExperience.ToString();
        }
    }
}

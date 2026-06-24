using Cysharp.Threading.Tasks;
using System;
using System.Threading.Tasks;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

namespace magus.ui
{
    public class DamageIndicator : MonoBehaviour
    {
        [SerializeField]
        private TMPro.TextMeshProUGUI _damageText;

        [SerializeField]
        private float _lifetime = 3;


        public event Action Finished;

		public void Setup(float damageAmount, Color color, float size)
        {
			// Setup the damage indicator with the damage amount
            _damageText.text = damageAmount.ToString("F0");
            _damageText.color = color;
            _damageText.fontSize = size;
		}

        public void StartAnimation()
        {
            // Start the damage indicator animation (e.g., fade out, move up)
            // This is a placeholder for the actual animation logic
            // You can use Unity's Animation or Tweening libraries here

            AnimateAsync().Forget();
		}

        private async UniTask AnimateAsync()
        {
            float elapsed = 0f;

            _damageText.transform.localPosition = Vector3.zero;


			while (elapsed < _lifetime)
            {
				elapsed += Time.deltaTime;

                _damageText.transform.localPosition += Vector3.up * 40f * Time.deltaTime / _lifetime;
                _damageText.alpha = 1f - elapsed / _lifetime;

				await UniTask.Yield(PlayerLoopTiming.Update);
			}


            Finished?.Invoke();
        }

	}
}

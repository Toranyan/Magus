using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace magus.battle
{

    public class DamageIndicator3d : MonoBehaviour
    {
        [SerializeField]
        private TextMeshPro _damageText;

        [SerializeField]
        private float _lifetime =1.5f;

        [SerializeField]
        private float _riseDistance =1.0f;

        [SerializeField]
        private bool _recomputeRotationPerUpdate = false;

        public event Action Finished;

        private Color _baseColor = Color.white;

        public void Setup(float damageAmount, Color color, float size)
        {
            if (_damageText != null)
            {
                _damageText.text = damageAmount.ToString("F0");
                _baseColor = new Color(color.r, color.g, color.b,1f);
                _damageText.color = _baseColor;
                _damageText.fontSize = size;
            }

            ApplyBillboardRotation();
        }

        public void StartAnimation()
        {
            AnimateAsync().Forget();
        }

        private async UniTask AnimateAsync()
        {
            float elapsed =0f;

            Vector3 startPos = transform.localPosition;
            Vector3 targetPos = startPos + Vector3.up * _riseDistance;

            while (elapsed < _lifetime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _lifetime);

                // position lerp
                transform.localPosition = Vector3.Lerp(startPos, targetPos, t);

                // fade
                if (_damageText != null)
                {
                    var c = _baseColor;
                    c.a =1f - t;
                    _damageText.color = c;
                }

                await UniTask.Yield();
            }

            Finished?.Invoke();
        }

        private void Update()
        {
            if (_recomputeRotationPerUpdate)
                ApplyBillboardRotation();
        }

        private void ApplyBillboardRotation()
        {
            var cam = Camera.main;
            if (cam != null)
            {
                transform.rotation = Quaternion.LookRotation(cam.transform.forward);
            }
        }
    }

}

using UnityEngine;

namespace magus.interaction
{
    /// <summary>Optional helper for an Interactable's indicator object (e.g. a small speech-
    /// bubble sprite above an NPC's head): keeps it facing the camera and gives it a slight
    /// bob, so it reads as "you can talk here" without a UI prompt.</summary>
    public class InteractionIndicator : MonoBehaviour
    {
        [SerializeField] private float _bobHeight = 0.05f;
        [SerializeField] private float _bobSpeed = 2f;

        private Vector3 _baseLocalPosition;

        private void Awake()
        {
            _baseLocalPosition = transform.localPosition;
        }

        private void OnEnable()
        {
            transform.localPosition = _baseLocalPosition;
        }

        private void LateUpdate()
        {
            var camera = Camera.main;
            if (camera != null)
            {
                transform.rotation = camera.transform.rotation;
            }

            if (_bobHeight > 0f)
            {
                transform.localPosition = _baseLocalPosition + Vector3.up * (Mathf.Sin(Time.time * _bobSpeed) * _bobHeight);
            }
        }
    }
}

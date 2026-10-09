using UnityEngine;
using magus.chara;

namespace magus.interaction
{
    /// <summary>Base for anything the player can walk up to and use with Interact (E) - NPCs
    /// (NpcDialogue), later doors, chests, signs. Registers itself with InteractionManager
    /// while enabled; InteractionManager focuses the nearest one in range and calls Interact.
    ///
    /// The Indicator is an optional child object (e.g. a small speech-bubble sprite with an
    /// InteractionIndicator component) shown only while this is the focused interactable -
    /// a quiet in-world cue rather than a UI prompt.</summary>
    public abstract class Interactable : MonoBehaviour
    {
        [Tooltip("Max flat (XZ) distance from the player, in meters.")]
        [SerializeField] private float _range = 2f;

        [Tooltip("Shown only while this is the focused interactable. Optional.")]
        [SerializeField] private GameObject _indicator;

        public float Range => _range;

        /// <summary>Override to temporarily refuse focus (e.g. nothing to say right now).</summary>
        public virtual bool CanInteract => isActiveAndEnabled;

        public abstract void Interact(PlayerController player);

        protected virtual void Awake()
        {
            SetFocused(false);
        }

        protected virtual void OnEnable()
        {
            InteractionManager.Register(this);
        }

        protected virtual void OnDisable()
        {
            InteractionManager.Unregister(this);
            SetFocused(false);
        }

        public void SetFocused(bool focused)
        {
            if (_indicator != null && _indicator.activeSelf != focused)
            {
                _indicator.SetActive(focused);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, _range);
        }
    }
}

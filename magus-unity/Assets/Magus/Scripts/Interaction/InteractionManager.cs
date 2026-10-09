using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using tora.singleton;
using magus.battle;
using magus.chara;
using magus.input;

namespace magus.interaction
{
    /// <summary>Tracks every enabled Interactable, focuses the nearest one within its own
    /// Range of the player each frame (showing its indicator), and calls Interact on it when
    /// Interact (E) is pressed. Created on demand by the first Interactable to register -
    /// nothing to place in a scene.
    ///
    /// Nothing is focused while the player's input is locked (cutscene, dialogue), so the
    /// indicator hides during a conversation and the same E press that advances dialogue
    /// can't also start a new one.</summary>
    public class InteractionManager : SingletonComponent<InteractionManager>
    {
        private static readonly List<Interactable> Registered = new List<Interactable>();

        private Interactable _focused;
        private InputManager _inputManager;

        public Interactable Focused => _focused;

        public static void Register(Interactable interactable)
        {
            if (!Registered.Contains(interactable))
            {
                Registered.Add(interactable);
            }

            // Make sure a manager exists to drive focus/input.
            _ = Instance;
        }

        public static void Unregister(Interactable interactable)
        {
            Registered.Remove(interactable);
        }

        /// <summary>Start, not Awake: InputManager builds its callback table in its own Awake.</summary>
        private void Start()
        {
            _inputManager = InputManager.Instance;
            _inputManager.AddActionCallback(InputManager.PlayerInputType.Interact, InputManager.InputPhase.Performed, OnInteractInput);
        }

        private void OnDestroy()
        {
            // Cached reference, not InputManager.Instance - at teardown that could re-create
            // an InputManager with no action asset.
            if (_inputManager != null)
            {
                _inputManager.RemoveActionCallback(InputManager.PlayerInputType.Interact, InputManager.InputPhase.Performed, OnInteractInput);
            }
        }

        private void Update()
        {
            SetFocus(FindFocusCandidate());
        }

        private Interactable FindFocusCandidate()
        {
            var player = GetPlayer();
            if (player == null || !player.InputEnabled)
            {
                return null;
            }

            var playerPos = player.transform.position;
            Interactable best = null;
            var bestDistSqr = float.MaxValue;

            foreach (var interactable in Registered)
            {
                if (interactable == null || !interactable.CanInteract)
                {
                    continue;
                }

                var offset = interactable.transform.position - playerPos;
                offset.y = 0f;
                var distSqr = offset.sqrMagnitude;

                if (distSqr <= interactable.Range * interactable.Range && distSqr < bestDistSqr)
                {
                    best = interactable;
                    bestDistSqr = distSqr;
                }
            }

            return best;
        }

        private void SetFocus(Interactable interactable)
        {
            if (_focused == interactable)
            {
                return;
            }

            if (_focused != null)
            {
                _focused.SetFocused(false);
            }

            _focused = interactable;

            if (_focused != null)
            {
                _focused.SetFocused(true);
            }
        }

        private void OnInteractInput(InputAction.CallbackContext context)
        {
            var player = GetPlayer();
            if (_focused == null || player == null || !player.InputEnabled || !_focused.CanInteract)
            {
                return;
            }

            var target = _focused;
            SetFocus(null);
            target.Interact(player);
        }

        private static PlayerController GetPlayer()
        {
            var player = BattleController.Instance.PlayerController;
            return player != null && player.isActiveAndEnabled ? player : null;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;
using tora.singleton;
using tora.save;
using tora.eventbus;
using magus.master;
using magus.ui;
using magus.story;
using magus.input;

namespace magus.dialogue
{
    /// <summary>Takes an Addressables path rather than a pre-loaded DialogueAsset - matches
    /// BattleController's Init(options) convention, and gives CaptureState something stable
    /// (the address string) to save.</summary>
    public class DialogueManager : SingletonComponent<DialogueManager>, ISaveParticipant
    {
        private readonly DialogueRunner _runner = new DialogueRunner();

        private string _currentAssetAddress;
        private bool _isPaused;

        public string SaveKey => "dialogue";

        public bool IsPlaying => _runner.IsPlaying && !_isPaused;

        public event Action<DialogueEntry> EntryChanged;
        public event Action Ended;

        private UIDialogueView _viewEventsWired;

        /// <summary>True from Play() until the conversation ends/fails - brackets
        /// DialogueStartedEvent/DialogueEndedEvent, even across the async asset load.</summary>
        private bool _sessionActive;

        /// <summary>Frame the current entry was shown on - TryAdvance ignores input from that
        /// same frame, so the Interact press that started a conversation can't also skip its
        /// first line.</summary>
        private int _entryShownFrame = -1;

        private InputManager _inputManager;

        /// <summary>{Key} tokens in resolved text - see FormatVariables.</summary>
        private static readonly Regex VariableTokenRegex = new Regex(@"\{(\w+)\}");

        private void Awake()
        {
            SaveSystem.Register(this);
            _runner.EntryChanged += OnEntryChanged;
            _runner.Ended += OnEnded;
            _runner.ResponseRecorded += OnResponseRecorded;
            EventBus.Subscribe<ConversationRequestedEvent>(OnConversationRequested);
        }

        /// <summary>Start, not Awake: InputManager builds its callback table in its own Awake,
        /// which isn't guaranteed to have run yet.</summary>
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

            EventBus.Unsubscribe<ConversationRequestedEvent>(OnConversationRequested);
            _runner.EntryChanged -= OnEntryChanged;
            _runner.Ended -= OnEnded;
            _runner.ResponseRecorded -= OnResponseRecorded;
            SaveSystem.Unregister(this);
        }

        private void OnConversationRequested(ConversationRequestedEvent e)
        {
            Play(e.GraphAddress);
        }

        private async UniTask<UIDialogueView> GetViewAsync()
        {
            var view = await UIManager.Instance.GetOrLoadViewAsync<UIDialogueView>();

            if (_viewEventsWired != view)
            {
                view.AdvanceRequested += OnAdvanceRequested;
                view.ChoiceSelected += OnChoiceSelected;
                view.TextSubmitted += OnTextSubmitted;
                _viewEventsWired = view;
            }

            return view;
        }

        private void OnAdvanceRequested()
        {
            if (IsPlaying)
            {
                _runner.Advance();
            }
        }

        private void OnChoiceSelected(int optionIndex)
        {
            if (IsPlaying)
            {
                _runner.ChooseOption(optionIndex);
            }
        }

        /// <summary>Empty/whitespace falls back to the entry's DefaultValue; if that's empty
        /// too, the submission is ignored and the prompt stays up.</summary>
        private void OnTextSubmitted(string value)
        {
            var entry = _runner.CurrentEntry;
            if (!IsPlaying || entry == null || entry.Kind != DialogueEntryKind.TextInput)
            {
                return;
            }

            value = value?.Trim();
            if (string.IsNullOrEmpty(value))
            {
                value = entry.DefaultValue?.Trim();
            }
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            _viewEventsWired?.HideTextInput();
            _runner.SubmitText(value);
        }

        private void OnResponseRecorded(string variableKey, string value)
        {
            EventBus.Publish(new DialogueChoiceMadeEvent { VariableKey = variableKey, Value = value });
        }

        public void Play(string assetAddress)
        {
            if (!_sessionActive)
            {
                _sessionActive = true;
                EventBus.Publish(new DialogueStartedEvent { DialogueAddress = assetAddress });
            }

            PlayAsync(assetAddress).Forget();
        }

        private async UniTask PlayAsync(string assetAddress)
        {
            DialogueAsset asset = null;
            try
            {
                asset = await Addressables.LoadAssetAsync<DialogueAsset>(assetAddress);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DialogueManager] Exception loading DialogueAsset at '{assetAddress}': {ex.Message}");
            }

            if (asset == null)
            {
                Debug.LogError($"[DialogueManager] Failed to load DialogueAsset at '{assetAddress}'.");

                // Don't leave the player locked by a conversation that never started.
                if (!_runner.IsPlaying)
                {
                    EndSession();
                }
                return;
            }

            _currentAssetAddress = assetAddress;
            _isPaused = false;
            _runner.Start(asset);
        }

        public void Stop()
        {
            // Cleared before _runner.Stop() so OnEnded can tell an interrupted conversation
            // (no address) from one that played to the end.
            _currentAssetAddress = null;
            _runner.Stop();
        }

        /// <summary>Advances a Text entry - bound to the Interact input (see Start), alongside
        /// the view's advance button. No-op on Choice/TextInput entries (they need a pick/
        /// submit), while paused, or on the frame the entry appeared.</summary>
        public bool TryAdvance()
        {
            if (!IsPlaying || _runner.CurrentEntry == null || _runner.CurrentEntry.Kind != DialogueEntryKind.Text)
            {
                return false;
            }

            if (Time.frameCount <= _entryShownFrame)
            {
                return false;
            }

            _runner.Advance();
            return true;
        }

        private void OnInteractInput(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            TryAdvance();
        }

        public void Pause()
        {
            _isPaused = true;
        }

        public void Resume()
        {
            _isPaused = false;
        }

        /// <summary>Advances immediately to the next entry. Stands in for "skip the typewriter
        /// and show the full line" until a typewriter effect exists (deferred - see
        /// Docs/Design/DialogueSystem.md#ui) - for now it's equivalent to Advance().</summary>
        public void Skip()
        {
            if (!IsPlaying)
            {
                return;
            }
            _runner.Advance();
        }

        private void OnEntryChanged(DialogueEntry entry)
        {
            _entryShownFrame = Time.frameCount;
            EntryChanged?.Invoke(entry);
            PresentEntryAsync(entry).Forget();
        }

        private async UniTask PresentEntryAsync(DialogueEntry entry)
        {
            var view = await GetViewAsync();
            view.Open();

            switch (entry.Kind)
            {
                case DialogueEntryKind.Text:
                    await PresentTextEntryAsync(view, entry);
                    break;
                case DialogueEntryKind.Choice:
                    await PresentChoiceEntryAsync(view, entry);
                    break;
                case DialogueEntryKind.TextInput:
                    await PresentTextInputEntryAsync(view, entry);
                    break;
            }
        }

        private async UniTask PresentTextEntryAsync(UIDialogueView view, DialogueEntry entry)
        {
            view.ClearChoices();
            view.HideTextInput();
            view.SetAdvanceButtonVisible(true);
            await ApplySpeakerAsync(view, entry.CharacterId);
            view.SetBodyText(await ResolveLocalizedStringAsync(entry.Text));
        }

        /// <summary>Text (if set) is shown as the prompt, with the speaker if CharacterId is
        /// set - e.g. an NPC asking "What's your name?".</summary>
        private async UniTask PresentTextInputEntryAsync(UIDialogueView view, DialogueEntry entry)
        {
            view.ClearChoices();
            view.SetAdvanceButtonVisible(false);
            await ApplySpeakerAsync(view, entry.CharacterId);
            view.SetBodyText(entry.Text != null && !entry.Text.IsEmpty ? await ResolveLocalizedStringAsync(entry.Text) : string.Empty);
            view.ShowTextInput(FormatVariables(entry.DefaultValue), entry.MaxLength);
        }

        private async UniTask PresentChoiceEntryAsync(UIDialogueView view, DialogueEntry entry)
        {
            view.HideTextInput();
            view.SetAdvanceButtonVisible(false);

            var optionTexts = new List<string>(entry.Options.Count);
            foreach (var option in entry.Options)
            {
                optionTexts.Add(await ResolveLocalizedStringAsync(option.Text));
            }

            view.ShowChoices(optionTexts);
        }

        private async UniTask ApplySpeakerAsync(UIDialogueView view, string characterId)
        {
            var character = MasterData.GetMasterData<CharacterMasterData>(characterId);
            if (character == null)
            {
                view.SetSpeaker(characterId, null);
                return;
            }

            Sprite portrait = null;
            if (!string.IsNullOrEmpty(character.PortraitAssetKey))
            {
                // A bad/missing portrait (e.g. an image not imported as a Sprite) throwing
                // here used to abort PresentTextEntryAsync entirely, leaving the body text
                // stuck on its placeholder - the actual bug behind this fix. Text should
                // never be held hostage by a broken portrait.
                try
                {
                    portrait = await Addressables.LoadAssetAsync<Sprite>(character.PortraitAssetKey);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[DialogueManager] Failed to load portrait '{character.PortraitAssetKey}' for '{characterId}': {ex.Message}");
                }
            }

            view.SetSpeaker(FormatVariables(character.DisplayName), portrait);
        }

        private static async UniTask<string> ResolveLocalizedStringAsync(UnityEngine.Localization.LocalizedString text)
        {
            return FormatVariables(await text.GetLocalizedStringAsync());
        }

        /// <summary>Replaces {Key} with StoryBlackboard variable Key (e.g. "{PlayerName}"
        /// after a TextInput entry recorded it). Unknown keys are left as-is. Read-only - see
        /// StoryManager.TryGetVariable. Localized entries using this must NOT be marked
        /// Smart in the string table, or Smart Format will try to resolve the braces itself
        /// (and fail, since no arguments are passed).</summary>
        private static string FormatVariables(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('{') < 0)
            {
                return text;
            }

            return VariableTokenRegex.Replace(text, match =>
                StoryManager.Instance.TryGetVariable(match.Groups[1].Value, out var variable)
                    ? variable.ToString()
                    : match.Value);
        }

        private void OnEnded()
        {
            // Reported before Ended fires, so the "seen" flag is already recorded when a
            // cutscene waiting on this dialogue finishes and its StoryNode completes.
            if (!string.IsNullOrEmpty(_currentAssetAddress))
            {
                EventBus.Publish(new StoryEvent { Id = StoryFlags.ConversationSeen(_currentAssetAddress) });
            }

            _currentAssetAddress = null;
            UIManager.Instance.Close<UIDialogueView>();
            EndSession();
            Ended?.Invoke();
        }

        private void EndSession()
        {
            if (!_sessionActive)
            {
                return;
            }

            _sessionActive = false;
            EventBus.Publish(new DialogueEndedEvent());
        }

        /// <summary>Round-trips the in-progress conversation's address/entry index through save
        /// data, matching BattleController's ISaveParticipant shape. Unlike BattleController,
        /// nothing currently acts on the restored value - there's no defined "resume mid-
        /// conversation" trigger point/UX yet (see Docs/Design/DialogueSystem.md's Future
        /// Extensions), so RestoreState only logs it for now rather than guessing at one.</summary>
        public string CaptureState()
        {
            if (string.IsNullOrEmpty(_currentAssetAddress) || !_runner.IsPlaying)
            {
                return null;
            }

            return JsonUtility.ToJson(new DialogueSaveData
            {
                AssetAddress = _currentAssetAddress,
                EntryIndex = _runner.Asset.Entries.IndexOf(_runner.CurrentEntry)
            });
        }

        public void RestoreState(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            var data = JsonUtility.FromJson<DialogueSaveData>(json);
            Debug.Log($"[DialogueManager] Save data has an in-progress conversation ('{data.AssetAddress}' @ entry {data.EntryIndex}) - not resumed, no trigger point defined yet.");
        }

        [Serializable]
        private class DialogueSaveData
        {
            public string AssetAddress;
            public int EntryIndex;
        }
    }
}

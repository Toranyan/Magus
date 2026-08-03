using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;
using tora.singleton;
using tora.save;
using tora.eventbus;
using magus.master;
using magus.ui;
using magus.story;

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

        private void Awake()
        {
            SaveSystem.Register(this);
            _runner.EntryChanged += OnEntryChanged;
            _runner.Ended += OnEnded;
            _runner.ResponseRecorded += OnResponseRecorded;
            EventBus.Subscribe<ConversationRequestedEvent>(OnConversationRequested);
        }

        private void OnDestroy()
        {
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

        private void OnResponseRecorded(string variableKey, string value)
        {
            EventBus.Publish(new DialogueChoiceMadeEvent { VariableKey = variableKey, Value = value });
        }

        public void Play(string assetAddress)
        {
            PlayAsync(assetAddress).Forget();
        }

        private async UniTask PlayAsync(string assetAddress)
        {
            var asset = await Addressables.LoadAssetAsync<DialogueAsset>(assetAddress);
            if (asset == null)
            {
                Debug.LogError($"[DialogueManager] Failed to load DialogueAsset at '{assetAddress}'.");
                return;
            }

            _currentAssetAddress = assetAddress;
            _isPaused = false;
            _runner.Start(asset);
        }

        public void Stop()
        {
            _runner.Stop();
            _currentAssetAddress = null;
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
            }
        }

        private async UniTask PresentTextEntryAsync(UIDialogueView view, DialogueEntry entry)
        {
            view.ClearChoices();
            view.SetAdvanceButtonVisible(true);
            await ApplySpeakerAsync(view, entry.CharacterId);
            view.SetBodyText(await ResolveLocalizedStringAsync(entry.Text));
        }

        private async UniTask PresentChoiceEntryAsync(UIDialogueView view, DialogueEntry entry)
        {
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
                portrait = await Addressables.LoadAssetAsync<Sprite>(character.PortraitAssetKey);
            }

            view.SetSpeaker(character.DisplayName, portrait);
        }

        private static async UniTask<string> ResolveLocalizedStringAsync(UnityEngine.Localization.LocalizedString text)
        {
            return await text.GetLocalizedStringAsync();
        }

        private void OnEnded()
        {
            _currentAssetAddress = null;
            UIManager.Instance.Close<UIDialogueView>();
            Ended?.Invoke();
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

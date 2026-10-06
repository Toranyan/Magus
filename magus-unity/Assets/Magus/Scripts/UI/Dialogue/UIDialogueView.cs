using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using tora.ui;

namespace magus.ui
{
    /// <summary>Dumb view - DialogueManager resolves character/localized content and calls
    /// these setters, same division of responsibility as UIBattleView. Name, Portrait,
    /// Text, Choices, and a text input (TextInput entries, e.g. naming the player) - see
    /// Docs/Design/DialogueSystem.md#ui.</summary>
    public class UIDialogueView : UIViewBase
    {
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private Image _portraitImage;
        [SerializeField] private TextMeshProUGUI _bodyText;
        [SerializeField] private Button _advanceButton;
        [SerializeField] private Transform _choicesContainer;
        [SerializeField] private Button _choiceButtonPrefab;

        [Header("Text Input (TextInput entries)")]
        [SerializeField] private GameObject _textInputRoot;
        [SerializeField] private TMP_InputField _textInputField;
        [SerializeField] private Button _textInputSubmitButton;

        private readonly List<Button> _spawnedChoiceButtons = new List<Button>();

        /// <summary>Player clicked to advance a Text node. Ignored while choices are showing -
        /// use ChoiceSelected instead.</summary>
        public event Action AdvanceRequested;

        /// <summary>Player picked the option at this index.</summary>
        public event Action<int> ChoiceSelected;

        /// <summary>Player confirmed the text input (submit button or Enter), with its raw,
        /// untrimmed value.</summary>
        public event Action<string> TextSubmitted;

        private void Awake()
        {
            _advanceButton.onClick.AddListener(() => AdvanceRequested?.Invoke());

            if (_textInputSubmitButton != null)
            {
                _textInputSubmitButton.onClick.AddListener(SubmitText);
            }
            if (_textInputField != null)
            {
                _textInputField.onSubmit.AddListener(_ => SubmitText());
            }

            HideTextInput();
        }

        public void ShowTextInput(string defaultValue, int maxLength)
        {
            if (_textInputRoot == null || _textInputField == null)
            {
                Debug.LogError("[UIDialogueView] TextInput entry shown, but the text input fields aren't assigned on the UIDialogueView prefab.");
                return;
            }

            _textInputRoot.SetActive(true);
            _textInputField.characterLimit = Mathf.Max(0, maxLength);
            _textInputField.text = defaultValue ?? string.Empty;
            _textInputField.Select();
            _textInputField.ActivateInputField();
        }

        public void HideTextInput()
        {
            if (_textInputRoot != null)
            {
                _textInputRoot.SetActive(false);
            }
        }

        private void SubmitText()
        {
            if (_textInputRoot == null || !_textInputRoot.activeInHierarchy)
            {
                return;
            }

            TextSubmitted?.Invoke(_textInputField.text);
        }

        public void SetSpeaker(string displayName, Sprite portrait)
        {
            _nameText.text = displayName;
            _portraitImage.sprite = portrait;
            _portraitImage.enabled = portrait != null;
        }

        public void SetBodyText(string text)
        {
            _bodyText.text = text;
        }

        public void SetAdvanceButtonVisible(bool visible)
        {
            _advanceButton.gameObject.SetActive(visible);
        }

        public void ShowChoices(IReadOnlyList<string> optionTexts)
        {
            ClearChoices();

            for (var i = 0; i < optionTexts.Count; i++)
            {
                var index = i;
                var button = Instantiate(_choiceButtonPrefab, _choicesContainer);

                var label = button.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                {
                    label.text = optionTexts[i];
                }

                button.onClick.AddListener(() => ChoiceSelected?.Invoke(index));
                _spawnedChoiceButtons.Add(button);
            }
        }

        public void ClearChoices()
        {
            foreach (var button in _spawnedChoiceButtons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }
            _spawnedChoiceButtons.Clear();
        }
    }
}

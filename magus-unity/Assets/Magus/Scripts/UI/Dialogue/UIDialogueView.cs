using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using tora.ui;

namespace magus.ui
{
    /// <summary>Dumb view - DialogueManager resolves character/localized content and calls
    /// these setters, same division of responsibility as UIBattleView. v1 fields only
    /// (Name, Portrait, Text, Choices) - see Docs/Design/DialogueSystem.md#ui.</summary>
    public class UIDialogueView : UIViewBase
    {
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private Image _portraitImage;
        [SerializeField] private TextMeshProUGUI _bodyText;
        [SerializeField] private Button _advanceButton;
        [SerializeField] private Transform _choicesContainer;
        [SerializeField] private Button _choiceButtonPrefab;

        private readonly List<Button> _spawnedChoiceButtons = new List<Button>();

        /// <summary>Player clicked to advance a Text node. Ignored while choices are showing -
        /// use ChoiceSelected instead.</summary>
        public event Action AdvanceRequested;

        /// <summary>Player picked the option at this index.</summary>
        public event Action<int> ChoiceSelected;

        private void Awake()
        {
            _advanceButton.onClick.AddListener(() => AdvanceRequested?.Invoke());
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

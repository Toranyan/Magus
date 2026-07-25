using System;
using Cysharp.Threading.Tasks;
using magus.battle;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace magus.ui
{
    // Shows a spell's icon, and while it's on cooldown, a radial mask over the icon
    // plus a countdown text, both hidden again once the cooldown reaches 0.
    // Spell and cast-button listeners can be (re)bound at any time via SetSpell/CastRequested.
    public class SpellCastIndicator : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private Image _cooldownMask;
        [SerializeField] private TextMeshProUGUI _cooldownText;

        [Tooltip("Left unassigned for indicators with no cast button (e.g. slot 0).")]
        [SerializeField] private Button _castButton;

        /// <summary>Raised when the cast button is clicked. No-op source if this indicator has no button.</summary>
        public event Action CastRequested;

        private UnitSpellInstance _spell;

        // Bumped on every SetSpell call so a still-in-flight icon load from a previous
        // spell can tell it's been superseded and discard its result instead of
        // clobbering the icon that was set after it.
        private int _iconLoadVersion;

        private void Awake()
        {
            if (_castButton != null)
                _castButton.onClick.AddListener(() => CastRequested?.Invoke());

            SetCooldownVisible(false);
        }

        public void SetSpell(UnitSpellInstance spell)
        {
            _spell = spell;

            _icon.enabled = false;
            _icon.sprite = null;
            LoadIconAsync(spell?.Info.IconAssetKey, ++_iconLoadVersion).Forget();

            SetCooldownVisible(false);
        }

        private async UniTaskVoid LoadIconAsync(string assetKey, int version)
        {
            if (string.IsNullOrEmpty(assetKey))
                return;

            var sprite = await Addressables.LoadAssetAsync<Sprite>(assetKey);

            if (version != _iconLoadVersion)
                return;

            _icon.sprite = sprite;
            _icon.enabled = sprite != null;
        }

        private void Update()
        {
            if (_spell == null || _spell.IsReady)
            {
                SetCooldownVisible(false);
                return;
            }

            SetCooldownVisible(true);

            float duration = _spell.LastCooldownDuration;
            _cooldownMask.fillAmount = duration > 0f ? Mathf.Clamp01(_spell.CooldownRemaining / duration) : 0f;
            _cooldownText.text = Mathf.CeilToInt(_spell.CooldownRemaining).ToString();
        }

        private void SetCooldownVisible(bool visible)
        {
            _cooldownMask.gameObject.SetActive(visible);
            _cooldownText.gameObject.SetActive(visible);
        }
    }
}

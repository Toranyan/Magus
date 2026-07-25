using System;
using magus.battle;
using tora.ui;
using UnityEngine;

namespace magus.ui
{
    public class UIBattleView : UIViewBase
    {
        // Index 0 = player's 1st spell slot (no button, e.g. basic attack), 1-4 = the
        // 4 button-cast spell slots. Kept in lockstep with PlayerController's slots.
        [SerializeField] private SpellCastIndicator[] _spellIndicators = new SpellCastIndicator[5];

        /// <summary>Raised with the slot index (1-4) when one of the button indicators is clicked.</summary>
        public event Action<int> SpellCastRequested;

        private void Awake()
        {
            for (int i = 0; i < _spellIndicators.Length; i++)
            {
                int slot = i;
                _spellIndicators[i].CastRequested += () => SpellCastRequested?.Invoke(slot);
            }
        }

        public void SetSpell(int slot, UnitSpellInstance spell)
        {
            if (slot < 0 || slot >= _spellIndicators.Length)
            {
                Debug.LogError($"[UIBattleView] Spell slot index {slot} out of range");
                return;
            }

            _spellIndicators[slot].SetSpell(spell);
        }
    }
}
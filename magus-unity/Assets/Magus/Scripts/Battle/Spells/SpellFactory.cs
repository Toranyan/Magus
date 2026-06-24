using UnityEngine;
using magus.master;
using System.Collections.Generic;
using System;

namespace magus.battle
{

    public class SpellFactory
    {
        private static readonly Dictionary<SpellType, Func<SpellMasterData, SpellInstance>> _spellMap =
            new Dictionary<SpellType, Func<SpellMasterData, SpellInstance>>
            {
                { SpellType.Fireball, data => new FireballSpellInstance(data) },
            };

        public static SpellInstance CreateSpell(SpellMasterData data)
		{
            return _spellMap[data.SpellType](data);
		}

    }

}

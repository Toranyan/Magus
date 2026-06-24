using System;
using UnityEngine;
using magus.battle;

namespace magus.battle
{

    public class AbilityEntity
	{

		public AbilityInfo Info { get; }

		public IBattleEntity Owner { get; private set; }

		private IAbilityExecutor _executor;

		// Constructor that defers executor creation until an Owner is provided
		public AbilityEntity(AbilityInfo info)
		{
			Info = info ?? throw new ArgumentNullException(nameof(info));
			// Do not create executor here because Owner may not be available yet.
		}

	}
    
}

using UnityEngine;
using magus.master;

namespace magus.battle
{

	public class FireballSpellInstance : SpellInstance
	{

		public FireballSpellInstance(SpellMasterData data) : base(data)
		{

		}

		public override void Cast(Vector3 position, GameObject caster)
		{
			throw new System.NotImplementedException();
		}
	}


}
using magus.master;
using UnityEngine;

namespace magus.battle
{

    public abstract class SpellInstance
    {

        protected SpellMasterData data;

        public SpellInstance(SpellMasterData data)
		{
            this.data = data;
		}

        public abstract void Cast(Vector3 position, GameObject caster);

    }

}

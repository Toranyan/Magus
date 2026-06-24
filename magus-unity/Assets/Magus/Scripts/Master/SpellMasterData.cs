using System;
using magus.battle;

namespace magus.master
{

    [Serializable]
    public class SpellMasterData : BaseMasterData
    {
        public string Name;

        public SpellType SpellType;

        public string Description;

        public float BaseCastTime;
        public float BaseManaCost;

        public float BaseDuration;
        public float BaseSpeed;

        public float BaseRange;
        public float BaseSize;

        public float BaseCount;

        public float[] Params;
    
    }

}

using System;

namespace magus.master
{
    [Serializable]
    public class LevelProgressionEntry
    {
        public int Level;
        public int XpToNextLevel;
    }

    [Serializable]
    public class LevelProgressionMasterData : BaseMasterData
    {
        public LevelProgressionEntry[] Entries;
    }
}

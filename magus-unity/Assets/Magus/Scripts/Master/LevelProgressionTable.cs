using System.Collections.Generic;

namespace magus.master
{
    /// <summary>
    /// Runtime lookup built from a LevelProgressionMasterData's Entries. Maps a unit's
    /// current level to the XP required to reach the next one; a level with no entry
    /// is treated as the max level.
    /// </summary>
    public class LevelProgressionTable
    {
        private readonly Dictionary<int, int> _xpToNextLevelByLevel = new();

        public LevelProgressionTable(LevelProgressionMasterData masterData)
        {
            if (masterData?.Entries == null) return;

            foreach (var entry in masterData.Entries)
                _xpToNextLevelByLevel[entry.Level] = entry.XpToNextLevel;
        }

        public bool TryGetXpToNextLevel(int level, out int xpToNextLevel)
        {
            return _xpToNextLevelByLevel.TryGetValue(level, out xpToNextLevel);
        }

        public bool IsMaxLevel(int level) => !_xpToNextLevelByLevel.ContainsKey(level);
    }
}

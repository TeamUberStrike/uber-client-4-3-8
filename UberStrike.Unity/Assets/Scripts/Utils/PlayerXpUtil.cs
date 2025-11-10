
using UberStrike.DataCenter.Common.Entities;
using Cmune.Util;
using UnityEngine;

public static class PlayerXpUtil
{
    public static void GetXpRangeForLevel(int level, out int minXp, out int maxXp)
    {
        level = Mathf.Clamp(level, 1, MaxPlayerLevel);
        minXp = 0; maxXp = 0;

        if (level < MaxPlayerLevel)
        {
            ApplicationDataManager.XpByLevel.TryGetValue(level, out minXp);
            ApplicationDataManager.XpByLevel.TryGetValue(level + 1, out maxXp);
        }
        // in case this is max level we hack upper bound since it is not in our table
        else
        {
            ApplicationDataManager.XpByLevel.TryGetValue(MaxPlayerLevel, out minXp);
            maxXp = minXp + 1;
        }
    }

    public static string GetLevelDescription(int level)
    {
        if (level >= MaxPlayerLevel) return "Uber Space";
        else return "Lvl " + level;
    }

    public static int GetLevelForXp(int xp)
    {
        for (int i = MaxPlayerLevel; i > 0; i--)
        {
            int level;

            if (ApplicationDataManager.XpByLevel.TryGetValue(i, out level) && xp >= level)
                return i;
        }

        CmuneDebug.LogError("Level calculation based on player XP failed !");
        return 1;
    }

    // The highest level a player can reach in Uberstrike, default is 30
    public static int MaxPlayerLevel
    {
        get { return UberStrikeCommonConfig.LevelCap; }
    }
}

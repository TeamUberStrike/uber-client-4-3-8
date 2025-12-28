using System.Collections.Generic;
using UberStrike.Realtime.Common;
using UnityEngine;

public static class GameDataComparer
{
    public static bool SortAscending = false;
}

public class GameDataMapComparer : IComparer<GameMetaData>
{
    public int Compare(GameMetaData a, GameMetaData b)
    {
        int c = a.MapID - b.MapID;
        return c == 0 ? GameDataNameComparer.StaticCompare(a, b) : GameDataComparer.SortAscending ? c : -c;
    }
}

public class GameDataTimeComparer : IComparer<GameMetaData>
{
    public int Compare(GameMetaData a, GameMetaData b)
    {
        int c = a.RoundTime - b.RoundTime;
        return GameDataComparer.SortAscending ? c : -c;
    }
}

public class GameDataRuleComparer : IComparer<GameMetaData>
{
    public int Compare(GameMetaData a, GameMetaData b)
    {
        int c = (short)a.GameMode - (short)b.GameMode;
        return c == 0 ? GameDataNameComparer.StaticCompare(a, b) : GameDataComparer.SortAscending ? c : -c;
    }
}

public class GameDataAccessComparer : IComparer<GameMetaData>
{
    public int Compare(GameMetaData a, GameMetaData b)
    {
        int c = 0;

        if (GameDataComparer.SortAscending)
        {
            if (a.IsPublic && b.IsPublic) c = 2;
            else if (a.IsPublic) c = 1;
            else if (b.IsPublic) c = -1;
        }
        else
        {
            if (!a.IsPublic && !b.IsPublic) c = 2;
            else if (!a.IsPublic) c = 1;
            else if (!b.IsPublic) c = -1;
        }

        return c;
    }
}

public class GameDataNameComparer : IComparer<GameMetaData>
{
    public int Compare(GameMetaData a, GameMetaData b)
    {
        return StaticCompare(a, b);
    }

    public static int StaticCompare(GameMetaData a, GameMetaData b)
    {
        return GameDataComparer.SortAscending ? string.Compare(b.RoomName, a.RoomName) : string.Compare(a.RoomName, b.RoomName);
    }
}

public class GameDataPlayerComparer : IComparer<GameMetaData>
{
    public int Compare(GameMetaData a, GameMetaData b)
    {
        int c = a.ConnectedPlayers - b.ConnectedPlayers;
        return c == 0 ? GameDataNameComparer.StaticCompare(a, b) : GameDataComparer.SortAscending ? c : -c;
    }
}

public class GameDataQualityComparer : IComparer<GameMetaData>
{
    public int Compare(GameMetaData a, GameMetaData b)
    {
        int c = (int)((a.RoomJoinValue - b.RoomJoinValue) * 10);

        return GameDataComparer.SortAscending ? c : -c;
    }
}

public class GameDataLatencyComparer : IComparer<GameMetaData>
{
    public int Compare(GameMetaData a, GameMetaData b)
    {
        int c = (int)(a.Latency - b.Latency);

        return GameDataComparer.SortAscending ? c : -c;
    }
}

public class GameDataRestrictionComparer : IComparer<GameMetaData>
{
    private int _playerLevel = 0;
    private IComparer<GameMetaData> _baseComparer;

    public GameDataRestrictionComparer(int playerLevel, IComparer<GameMetaData> baseComparer)
    {
        _playerLevel = playerLevel;
        _baseComparer = baseComparer;
    }

    public int Compare(GameMetaData x, GameMetaData y)
    {
        if (x.HasLevelRestriction || y.HasLevelRestriction) return _playerLevel < 5 ? NoobLevelsUp(x, y) : VeteranLevelsUp(x, y);
        else return _baseComparer.Compare(x, y);
    }

    private int NoobLevelsUp(GameMetaData x, GameMetaData y)
    {
        return (x.LevelMin < 5 && x.LevelMin != 0 ? x.LevelMin - 100 : x.LevelMin) - (y.LevelMin < 5 && y.LevelMin != 0 ? y.LevelMin - 100 : y.LevelMin);
    }

    private int VeteranLevelsUp(GameMetaData x, GameMetaData y)
    {
        return (x.LevelMin < 5 ? x.LevelMin + 100 : x.LevelMin) - (y.LevelMin < 5 ? y.LevelMin + 100 : y.LevelMin);
    }
}
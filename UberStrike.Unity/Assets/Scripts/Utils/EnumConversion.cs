using UberStrike.Core.Types;
using UberStrike.Realtime.Common;

public static class EnumConversion
{
    public static short GetGameModeID(this GameModeType mode)
    {
        switch (mode)
        {
            case GameModeType.DeathMatch: return (short)GameMode.DeathMatch;
            case GameModeType.TeamDeathMatch: return (short)GameMode.TeamDeathMatch;
            case GameModeType.EliminationMode: return (short)GameMode.TeamElimination;
            default: return (short)GameMode.Training;
        }
    }
}
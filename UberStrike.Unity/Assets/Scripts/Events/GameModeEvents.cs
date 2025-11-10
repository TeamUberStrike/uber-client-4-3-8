using UberStrike.Realtime.Common;
using UberStrike.DataCenter.Common.Entities;
using UberStrike.Core.Types;

public class TeamGameEndEvent
{
    public int RedTeamSplats { get; set; }
    public int BlueTeamSplats { get; set; }
}

public class OnModeInitializedEvent { }

public class OnMatchStartEvent
{
    public int MatchCount { get; set; }
    public int MatchEndServerTicks { get; set; }
}

public class OnMatchEndEvent { }

public class OnUpdateDeathMatchScoreEvent
{
    public int MyScore { get; set; }
    public int OtherPlayerScore { get; set; }
    public bool IsLeading { get; set; }
}

public class OnUpdateTeamScoreEvent
{
    public int BlueScore { get; set; }
    public int RedScore { get; set; }
    public bool IsLeading { get; set; }
}

public class OnUpdateRoundStatsEvent
{
    public int Round { get; set; }
    public int BlueWinRoundCount { get; set; }
    public int RedWinRoundCount { get; set; }
}

public class OnGraceTimeCountdownEvent { }

public class OnTeamEliminationRoundStartEvent
{
    public int RoundCount { get; set; }
    public int RoundEndServerTicks { get; set; }
}

public class OnTeamEliminationRoundEndEvent
{
    public TeamID WinTeamID { get; set; }
}

public class OnTeamEliminationSyncRoundTimeEvent
{
    public int RoundEndServerTicks { get; set; }
}

public class OnSetWaitingForPlayersEvent { }

public class OnSetEndOfMatchCountdownEvent 
{
    public int SecondsUntilNextMatch { get; set; }
}

public class OnChangeTeamSuccessEvent
{
    public TeamID CurrentTeamID { get; set; }
}

public class OnChangeTeamFailEvent
{
    public enum FailReason
    {
        None = 0,
        CannotChangeToATeamWithEqual,
        OnlyOneTeamChangePerLife,
    }

    public FailReason Reason { get; set; }
}

public class OnPlayerChangeTeamEvent
{
    public int PlayerID { get; set; }
    public CharacterInfo PlayerInfo { get; set; }
    public TeamID TargetTeamID { get; set; }
}

public class OnPlayerDamageEvent
{
    public float Angle { get; set; }
    public float DamageValue { get; set; }
}

public class OnPlayerKillEnemyEvent
{
    public CharacterInfo EmemyInfo { get; set; }
    public UberstrikeItemClass WeaponCategory { get; set; }
    public BodyPart BodyHitPart { get; set; }
}

public class OnPlayerKilledEvent
{
    public CharacterInfo ShooterInfo { get; set; }
    public UberstrikeItemClass WeaponCategory { get; set; }
    public BodyPart BodyHitPart { get; set; }
}

public class OnPlayerSuicideEvent
{
    public CharacterInfo PlayerInfo { get; set; }
}

public class OnPlayerDeadEvent { }

public class OnPlayerRespawnEvent { }

public class OnPlayerPauseEvent { }

public class OnPlayerUnpauseEvent { }

public class OnPlayerSpectatingEvent { }

public class OnPlayerUnspecatingEvent { }

public class OnCameraZoomInEvent { }

public class OnCameraZoomOutEvent { }

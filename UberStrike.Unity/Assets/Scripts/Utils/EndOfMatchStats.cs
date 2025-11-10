using UberStrike.Realtime.Common;
using UberStrike.DataCenter.Common.Entities;

public class EndOfMatchStats : Singleton<EndOfMatchStats>
{
    public string KillXP { get; private set; }
    public string DamageXP { get; private set; }
    public string NutshotXP { get; private set; }
    public string HeadshotXP { get; private set; }
    public string SmackdownXP { get; private set; }

    public string Suicides { get; private set; }
    public string KDR { get; private set; }
    public string Deaths { get; private set; }

    public string PointsEarned { get; private set; }
    public string XPEarned { get; private set; }

    private EndOfMatchData _data;
    public EndOfMatchData Data
    {
        get { return _data; }
        set
        {
            _data = value;
            OnDataUpdated();
        }
    }

    private EndOfMatchStats()
    {
        Data = new EndOfMatchData()
        {
            MostValuablePlayers = new System.Collections.Generic.List<StatsSummary>(),
            PlayerStatsBestPerLife = new StatsCollection(),
            PlayerStatsTotal = new StatsCollection(),
            PlayerXpEarned = new System.Collections.Generic.Dictionary<byte, ushort>(),
        };
    }

    private void OnDataUpdated()
    {
        KillXP = Data.PlayerXpEarned.ContainsKey(PlayerXPEventViewId.Splat) ? Data.PlayerXpEarned[PlayerXPEventViewId.Splat].ToString() : "0";
        HeadshotXP = Data.PlayerXpEarned.ContainsKey(PlayerXPEventViewId.HeadShot) ? Data.PlayerXpEarned[PlayerXPEventViewId.HeadShot].ToString() : "0";
        SmackdownXP = Data.PlayerXpEarned.ContainsKey(PlayerXPEventViewId.Humiliation) ? Data.PlayerXpEarned[PlayerXPEventViewId.Humiliation].ToString() : "0";
        NutshotXP = Data.PlayerXpEarned.ContainsKey(PlayerXPEventViewId.Nutshot) ? Data.PlayerXpEarned[PlayerXPEventViewId.Nutshot].ToString() : "0";
        DamageXP = Data.PlayerXpEarned.ContainsKey(PlayerXPEventViewId.Damage) ? Data.PlayerXpEarned[PlayerXPEventViewId.Damage].ToString() : "0";

        Deaths = Data.PlayerStatsTotal.Deaths.ToString();
        Suicides = (-Data.PlayerStatsTotal.Suicides).ToString();
        KDR = Data.PlayerStatsTotal.GetKdr().ToString("N1");
        PointsEarned = Data.PlayerStatsTotal.Points.ToString();
        XPEarned = Data.PlayerStatsTotal.Xp.ToString();
    }
}
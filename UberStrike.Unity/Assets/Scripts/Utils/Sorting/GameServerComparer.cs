
using System.Collections.Generic;
using Cmune.Realtime.Common;

public class GameServerNameComparer : IComparer<GameServerView>
{
    public int Compare(GameServerView a, GameServerView b)
    {
        return StaticCompare(a, b);
    }

    public static int StaticCompare(GameServerView a, GameServerView b)
    {
        return string.Compare(b.Name, a.Name);
    }
}

public class GameServerLatencyComparer : IComparer<GameServerView>
{
    public int Compare(GameServerView a, GameServerView b)
    {
        return StaticCompare(a, b);
    }

    public static int StaticCompare(GameServerView a, GameServerView b)
    {
        int r = 1;
        int pingA = a.Data.State == ServerLoadData.Status.Alive ? a.Latency : 1000;
        int pingB = b.Data.State == ServerLoadData.Status.Alive ? b.Latency : 1000;

        if (a.Latency == b.Latency)
            return string.Compare(b.Name, a.Name);

        return (pingA > pingB) ? r : r * (-1);
    }
}

public class GameServerPlayerCountComparer : IComparer<GameServerView>
{
    public int Compare(GameServerView a, GameServerView b)
    {
        return StaticCompare(a, b);
    }

    public static int StaticCompare(GameServerView a, GameServerView b)
    {
        int r = 1;

        if (a.Data.PlayersConnected == b.Data.PlayersConnected)
            return string.Compare(b.Name, a.Name);

        return (a.Data.State == ServerLoadData.Status.Alive ? a.Data.PlayersConnected : 1000) > (b.Data.State == ServerLoadData.Status.Alive ? b.Data.PlayersConnected : 1000) ? r : r * (-1);
    }
}

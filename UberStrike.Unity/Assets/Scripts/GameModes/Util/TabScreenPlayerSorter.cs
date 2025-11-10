using System.Collections.Generic;
using UberStrike.Realtime.Common;

static class TabScreenPlayerSorter
{
    class PlayerSplatSorter : Comparer<CharacterInfo>
    {
        public override int Compare(CharacterInfo x, CharacterInfo y)
        {
            return y.Kills - x.Kills;
        }
    }

    public static void SortDeathMatchPlayers(IEnumerable<CharacterInfo> toBeSortedPlayers)
    {
        List<CharacterInfo> players = new List<CharacterInfo>(toBeSortedPlayers);
        players.Sort(new PlayerSplatSorter());
        TabScreenPanelGUI.Instance.SetPlayerListAll(players);
    }

    public static void SortTeamMatchPlayers(IEnumerable<CharacterInfo> toBeSortedPlayers)
    {
        List<CharacterInfo> bluePlayers = new List<CharacterInfo>();
        List<CharacterInfo> redPlayers = new List<CharacterInfo>();

        foreach (CharacterInfo i in toBeSortedPlayers)
        {
            if (i.TeamID == TeamID.BLUE) bluePlayers.Add(i);
            else if (i.TeamID == TeamID.RED) redPlayers.Add(i);
            //else UnityEngine.Debug.LogError("player [" + i.PlayerName + "] ins't in any team!");
        }

        bluePlayers.Sort(new PlayerSplatSorter());
        redPlayers.Sort(new PlayerSplatSorter());

        TabScreenPanelGUI.Instance.SetPlayerListBlue(bluePlayers);
        TabScreenPanelGUI.Instance.SetPlayerListRed(redPlayers);
    }

}

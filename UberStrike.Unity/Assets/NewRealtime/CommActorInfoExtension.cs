using UberStrike.Core.Models;

static class CommActorInfoExtension
{
    public static void Sync(this CommActorInfo original, CommActorInfo data)
    {
        original.ClanTag = data.ClanTag;
        original.CurrentRoom = data.CurrentRoom;
        original.ModerationFlag = data.ModerationFlag;
        original.ModInformation = data.ModInformation;
        original.Ping = data.Ping;
        original.PlayerName = data.PlayerName;
    }
}
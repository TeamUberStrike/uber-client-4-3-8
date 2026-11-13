
using Cmune.DataCenter.Common.Entities;
using Cmune.Realtime.Common;
using UberStrike.Realtime.Common;

public class CommUser
{
    public CommUser(CommActorInfo user)
    {
        SetActor(user);
    }

    public CommUser(CharacterInfo user)
    {
        IsInGame = true;
        Cmid = user.Cmid;
        Name = user.PlayerName;
        ActorId = user.ActorId;
    }

    public CommUser(PublicProfileView profile)
    {
        if (profile != null)
        {
            IsFriend = true;
            Cmid = profile.Cmid;
            AccessLevel = profile.AccessLevel;
            Name = string.IsNullOrEmpty(profile.GroupTag) ? profile.Name : "[" + profile.GroupTag + "] " + profile.Name;
        }
    }

    public CommUser(ClanMemberView member)
    {
        if (member != null)
        {
            IsClanMember = true;
            Cmid = member.Cmid;
            AccessLevel = 0;
            Name = string.IsNullOrEmpty(PlayerDataManager.ClanTag) ? member.Name : "[" + PlayerDataManager.ClanTag + "] " + member.Name;
        }
    }

    public override int GetHashCode()
    {
        return Cmid;
    }

    public void SetActor(CommActorInfo actor)
    {
        if (actor != null)
        {
            Cmid = actor.Cmid;
            AccessLevel = (MemberAccessLevel)actor.AccessLevel;
            ActorId = actor.ActorId;
            Name = actor.PlayerName;
            IsInGame = actor.IsInGame;
            Channel = actor.Channel;
            ModerationFlag = actor.ModerationFlag;
            ModerationInfo = actor.ModInformation;
            CurrentGame = actor.CurrentRoom.Number != StaticRoomID.CommCenter ? actor.CurrentRoom : CmuneRoomID.Empty;
        }
        else
        {
            ActorId = 0;
            IsInGame = false;
            CurrentGame = CmuneRoomID.Empty;
        }
    }

    #region Properties
    
    public int Cmid { get; private set; }
    public string Name
    {
        set
        {
            _name = ShortName = value;
            int idx = _name.IndexOf("]");
            if (idx > 0 && idx + 1 < _name.Length)
                ShortName = _name.Substring(idx + 1);
        }
        get { return _name; }
    }
    public int ActorId { get; private set; }
    public string ShortName { get; private set; }
    public MemberAccessLevel AccessLevel { get; private set; }
    public PresenceType PresenceIndex
    {
        get
        {
            if (IsOnline)
                return IsInGame ? PresenceType.InGame : PresenceType.Online;
            else
                return PresenceType.Offline;
        }
    }
    public int ModerationFlag { get; private set; }
    public string ModerationInfo { get; private set; }
    public ChannelType Channel { get; private set; }
    public CmuneRoomID CurrentGame { get; set; }
    public bool IsFriend { get; set; }
    public bool IsClanMember { get; set; }
    public bool IsInGame { get; set; }
    public bool IsOnline { get { return ActorId > 0; } }
    #endregion

    #region Fields
    private string _name = string.Empty;
    #endregion
}

using System.Collections.Generic;

public static class CommUserComparer
{
    public static int UserNameCompare(CommUser f1, CommUser f2)
    {
        return string.Compare(f1.ShortName, f2.ShortName, true);
    }
}

/// <summary>
/// Comparison to sort two comm users
/// </summary>
public class CommUserNameComparer : Comparer<CommUser>
{
    public override int Compare(CommUser f1, CommUser f2)
    {
        return CommUserComparer.UserNameCompare(f1, f2);
    }
}

/// <summary>
/// Comparison to sort two comm users
/// </summary>
public class CommUserFriendsComparer : Comparer<CommUser>
{
    public override int Compare(CommUser f1, CommUser f2)
    {
        if ((f1.IsFriend || f1.IsClanMember) && (f2.IsFriend || f2.IsClanMember))
        {
            return CommUserComparer.UserNameCompare(f1, f2);
        }
        else if (f2.IsFriend || f2.IsClanMember)
        {
            return 1;
        }
        else if (f1.IsFriend || f1.IsClanMember)
        {
            return -1;
        }
        else
        {
            return CommUserComparer.UserNameCompare(f1, f2);
        }
    }
}

/// <summary>
/// Comparison to sort two comm users
/// </summary>
public class CommUserPresenceComparer: Comparer<CommUser>
{
    public override int Compare(CommUser f1, CommUser f2)
    {
        ////sort user with new message up
        //Dialog d;
        //if (Instance._dialogsByCmid.TryGetValue(f1.Cmid, out d) && d != null && d.HasUnreadMessage)
        //    return -1;
        //else if (Instance._dialogsByCmid.TryGetValue(f2.Cmid, out d) && d != null && d.HasUnreadMessage)
        //    return 1;

        if (f1.PresenceIndex == f2.PresenceIndex)
        {
            return CommUserComparer.UserNameCompare(f1, f2);
        }
        else if (f1.PresenceIndex == PresenceType.Offline)
        {
            return 1;
        }
        else if (f2.PresenceIndex == PresenceType.Offline)
        {
            return -1;
        }
        else
        {
            return CommUserComparer.UserNameCompare(f1, f2);
        }

        ////otherwise sort by presence/name
        //switch (f1.PresenceIndex)
        //{
        //    case PresenceType.Online:
        //        {
        //            switch (f2.PresenceIndex)
        //            {
        //                case PresenceType.Online: return UserNameCompare(f1, f2);
        //                case PresenceType.InGame: return -1;
        //                default: return -1;
        //            }
        //        }
        //    case PresenceType.InGame:
        //        {
        //            switch (f2.PresenceIndex)
        //            {
        //                case PresenceType.Offline: return -1;
        //                case PresenceType.InGame: return UserNameCompare(f1, f2);
        //                default: return 1;
        //            }
        //        }
        //    default:
        //        {
        //            switch (f2.PresenceIndex)
        //            {
        //                case PresenceType.Offline: return UserNameCompare(f1, f2);
        //                case PresenceType.InGame: return 1;
        //                default: return 1;
        //            }
        //        }
        //}
    }
}

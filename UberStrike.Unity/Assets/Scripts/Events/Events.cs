using UberStrike.Realtime.Common;
using Cmune.DataCenter.Common.Entities;

public class BuyItemEvent
{
    public int Result { get; set; }
}

public class LogoutEvent
{
}

public class LoginEvent
{
    public LoginEvent(MemberAccessLevel level)
    {
        AccessLevel = level;
    }

    public MemberAccessLevel AccessLevel { get; private set; }
}

public class UpdateRecommendationEvent
{
}
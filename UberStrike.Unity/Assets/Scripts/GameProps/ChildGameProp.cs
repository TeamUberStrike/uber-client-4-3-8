using UberStrike.Realtime.Common;

public class ChildGameProp : BaseGameProp
{
    public BaseGameProp ParentProp;

    public override void ApplyDamage(DamageInfo d)
    {
        ParentProp.ApplyDamage(d);
    }
}

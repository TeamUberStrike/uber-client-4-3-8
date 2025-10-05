
using Cmune.Realtime.Common;
using UberStrike.Core.Types;
public enum ReticuleForSecondaryAction
{
    None,
    Default,
    DefaultZoom,
}

public enum WeaponSecondaryAction
{
    None,
    Zoom,
    IronSight,
    ExplosionTrigger,
}

[System.Flags]
public enum DamageEffectType
{
    None = BIT_FLAGS.BIT_NONE,
    SlowDown = BIT_FLAGS.BIT_01
}

[System.Serializable]
public class ZoomInfo
{
    public ZoomInfo()
    {
        MinMultiplier = 1;
        MaxMultiplier = 1;
        DefaultMultiplier = 1;
        CurrentMultiplier = 1;
    }

    public ZoomInfo(float defaultMultiplier, float minMultiplier, float maxMultiplier)
    {
        MinMultiplier = minMultiplier;
        MaxMultiplier = maxMultiplier;
        DefaultMultiplier = defaultMultiplier;
        CurrentMultiplier = DefaultMultiplier;
    }

    public float MinMultiplier;
    public float MaxMultiplier;
    public float DefaultMultiplier;
    public float CurrentMultiplier;
}

[System.Serializable]
public class DamageEffect
{
    public DamageEffectType Type;
    public float Value;
}
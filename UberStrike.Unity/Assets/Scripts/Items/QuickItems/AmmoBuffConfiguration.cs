using UnityEngine;
[System.Serializable]
public class AmmoBuffConfiguration : QuickItemConfiguration
{
    private const int MaxAmmo = 200;
    private const int StartAmmo = 100;

    [CustomProperty("AmmoIncrease")]
    public IncreaseStyle AmmoIncrease;
    public int IncreaseFrequency;
    public int IncreaseTimes;
    [CustomProperty("AmmoPoints")]
    public int PointsGain;

    [CustomProperty("RobotDestruction")]
    public int RobotLifeTimeMilliSeconds;
    [CustomProperty("ScrapsDestruction")]
    public int ScrapsLifeTimeMilliSeconds;

    public bool IsNeedCharge { get { return WarmUpTime > 0; } }
    public bool IsOverTime { get { return IncreaseTimes > 0; } }
    public bool IsInstant { get { return !IsNeedCharge && !IsOverTime; } }

    public string GetAmmoBonusDescription()
    {
        int multiplier = IncreaseTimes == 0 ? 1 : IncreaseTimes;
        switch (AmmoIncrease)
        {
            case IncreaseStyle.Absolute: return (multiplier * PointsGain).ToString();
            case IncreaseStyle.PercentFromMax: return string.Format("{0}% of the max ammo", PointsGain);
            case IncreaseStyle.PercentFromStart: return string.Format("{0}% of the start ammo", PointsGain);
            default: return "n/a";
        }
    }
}
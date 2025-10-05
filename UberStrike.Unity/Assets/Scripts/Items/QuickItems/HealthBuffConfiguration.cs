using UnityEngine;

[System.Serializable]
public class HealthBuffConfiguration : QuickItemConfiguration
{
    private const int MaxHealth = 200;
    private const int StartHealth = 100;

    [CustomProperty("IncreaseStyle")]
    public IncreaseStyle HealthIncrease;
    [CustomProperty("Frequency")]
    public int IncreaseFrequency;
    [CustomProperty("Times")]
    public int IncreaseTimes;
    [CustomProperty("HealthPoints")]
    public int PointsGain;

    [CustomProperty("RobotDestruction")]
    public int RobotLifeTimeMilliSeconds;
    [CustomProperty("ScrapsDestruction")]
    public int ScrapsLifeTimeMilliSeconds;

    public bool IsHealNeedCharge { get { return WarmUpTime > 0; } }
    public bool IsHealOverTime { get { return IncreaseTimes > 0; } }
    public bool IsHealInstant { get { return !IsHealNeedCharge && !IsHealOverTime; } }

    public string GetHealthBonusDescription()
    {
        int multiplier = IncreaseTimes == 0 ? 1 : IncreaseTimes;
        switch (HealthIncrease)
        {
            case IncreaseStyle.Absolute: return (multiplier * PointsGain).ToString() + "HP";
            case IncreaseStyle.PercentFromMax: return Mathf.RoundToInt(MaxHealth * multiplier * PointsGain / 100f) + "HP";
            case IncreaseStyle.PercentFromStart: return Mathf.RoundToInt(StartHealth * multiplier * PointsGain / 100f) + "HP";
            default: return "n/a";
        }
    }
}
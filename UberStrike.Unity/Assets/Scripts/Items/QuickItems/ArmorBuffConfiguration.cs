using UnityEngine;
[System.Serializable]
public class ArmorBuffConfiguration : QuickItemConfiguration
{
    private const int MaxArmor = 200;
    private const int StartArmor = 100;

    public IncreaseStyle ArmorIncrease;
    public int IncreaseFrequency;
    public int IncreaseTimes;
    [CustomProperty("ArmorPoints")]
    public int PointsGain;

    [CustomProperty("RobotDestruction")]
    public int RobotLifeTimeMilliSeconds;
    [CustomProperty("ScrapsDestruction")]
    public int ScrapsLifeTimeMilliSeconds;

    public bool IsNeedCharge { get { return WarmUpTime > 0; } }
    public bool IsOverTime { get { return IncreaseTimes > 0; } }
    public bool IsInstant { get { return !IsNeedCharge && !IsOverTime; } }

    public string GetArmorBonusDescription()
    {
        int multiplier = IncreaseTimes == 0 ? 1 : IncreaseTimes;
        switch (ArmorIncrease)
        {
            case IncreaseStyle.Absolute: return (multiplier * PointsGain).ToString();
            case IncreaseStyle.PercentFromMax: return Mathf.RoundToInt(MaxArmor * multiplier * PointsGain / 100f) + "AP";
            case IncreaseStyle.PercentFromStart: return Mathf.RoundToInt(StartArmor * multiplier * PointsGain / 100f) + "AP";
            default: return "n/a";
        }
    }
}
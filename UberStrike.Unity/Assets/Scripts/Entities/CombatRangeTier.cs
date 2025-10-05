
using UnityEngine;
[System.Serializable]
public class CombatRangeTier
{
    public int CloseRange = 1;
    public int MediumRange = 1;
    public int LongRange = 1;

    public CombatRangeCategory RangeCategory
    {
        get
        {
            int max = Mathf.Max(CloseRange, MediumRange, LongRange);
            return (CloseRange == max ? CombatRangeCategory.Close : 0) | (MediumRange == max ? CombatRangeCategory.Medium : 0) | (LongRange == max ? CombatRangeCategory.Far : 0);
        }
    }

    public int GetTierForRange(CombatRangeCategory range)
    {
        switch (range)
        {
            case CombatRangeCategory.Close: return CloseRange;
            case CombatRangeCategory.Medium: return MediumRange;
            case CombatRangeCategory.Far: return LongRange;
            case CombatRangeCategory.CloseMedium: return Mathf.RoundToInt((CloseRange + MediumRange) / 2f);
            case CombatRangeCategory.MediumFar: return Mathf.RoundToInt((MediumRange + LongRange) / 2f);
            default: return Mathf.RoundToInt((CloseRange + MediumRange + LongRange) / 3f);
        }
    }
}
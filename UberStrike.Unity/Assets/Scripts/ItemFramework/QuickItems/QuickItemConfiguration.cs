
using UberStrike.Core.Models.Views;

public class QuickItemConfiguration : UberStrikeItemQuickView
{
    [CustomProperty("Amount")]
    private int _totalAmount = 0;
    public int AmountRemaining { get { return _totalAmount; } set { _totalAmount = value; } }

    [CustomProperty("RechargeTime")]
    private int _rechargeTime = 0;
    public int RechargeTime { get { return _rechargeTime; } }

    [CustomProperty("SlowdownOnCharge")]
    private float _slowdownOnCharge = 2.0f;
    public float SlowdownOnCharge { get { return _slowdownOnCharge; } }
}
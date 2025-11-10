using UberStrike.Core.Models.Views;
using UnityEngine;

public class GearItem : BaseUnityItem
{
    [SerializeField]
    private GearItemConfiguration _config;

    public GearItemConfiguration Configuration { get { return _config; } set { _config = value; } }

    public override BaseUberStrikeItemView ItemView
    {
        get { return Configuration; }
    }
}
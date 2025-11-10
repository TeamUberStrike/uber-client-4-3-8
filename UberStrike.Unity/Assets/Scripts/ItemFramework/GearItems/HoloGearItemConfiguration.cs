
using UberStrike.Core.Models.Views;
using UberStrike.Core.Types;
using UnityEngine;

[System.Serializable]
public class HoloGearItemConfiguration : UberStrikeItemGearView
{
    [SerializeField]
    private AvatarType _holo;

    public AvatarType Holo { get { return _holo; } set { _holo = value; } }
}
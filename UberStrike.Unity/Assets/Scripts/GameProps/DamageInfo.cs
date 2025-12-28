
using UberStrike.DataCenter.Common.Entities;
using UberStrike.Realtime.Common;
using UnityEngine;
using UberStrike.Core.Types;

public class DamageInfo
{
    public short Damage { get; set; }
    public Vector3 Force { get; set; }
    public Vector3 Hitpoint { get; set; }
    public BodyPart BodyPart { get; set; }
    public int ShotID { get; set; }
    public int WeaponID { get; set; }
    public UberstrikeItemClass WeaponClass { get; set; }
    public float CriticalStrikeBonus { get; set; }
    public DamageEffectType DamageEffectFlag { get; set; }
    public float DamageEffectValue { get; set; }

    public DamageInfo(short damage)
    {
        this.Damage = damage;
        this.Force = Vector3.zero;
    }
}
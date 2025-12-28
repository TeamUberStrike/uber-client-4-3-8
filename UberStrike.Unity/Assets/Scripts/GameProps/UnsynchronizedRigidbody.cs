using UnityEngine;
using UberStrike.Realtime.Common;

[RequireComponent(typeof(Rigidbody))]
public class UnsynchronizedRigidbody : BaseGameProp
{
    public override void ApplyDamage(DamageInfo d)
    {
        Rigidbody.AddForceAtPosition(d.Force, d.Hitpoint);
    }
}


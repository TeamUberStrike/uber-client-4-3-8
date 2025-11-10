using UnityEngine;
using Cmune.Util;

public class DoorTrigger : BaseGameProp
{
    private DoorBehaviour _doorLogic;

    void Awake()
    {
        gameObject.layer = (int)UberstrikeLayer.Props;
    }

    public void SetDoorLogic(DoorBehaviour logic)
    {
        _doorLogic = logic;
    }

    public override void ApplyDamage(DamageInfo shot)
    {
        if (_doorLogic)
        {
            _doorLogic.Open();
        }
        else
        {
            CmuneDebug.LogError("The DoorCollider " + gameObject.name + " is not assigned to a DoorMechanism!");
        }
    }
}
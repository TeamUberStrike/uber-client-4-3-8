using UnityEngine;
using System.Collections;

public class PowerupHealth : Powerup
{
    public int Health;

    public override void Apply()
    {
        GameState.LocalCharacter.Health += (short)Health;
    }
}

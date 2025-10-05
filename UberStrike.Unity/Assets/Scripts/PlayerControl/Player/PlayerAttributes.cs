using UnityEngine;

[System.Serializable]
public class PlayerAttributes
{
    #region PROPERTIES

    public float Speed
    {
        get { return LevelEnviroment.PlayerWalkSpeed; }
    }

    public float JumpForce
    {
        get { return LevelEnviroment.PlayerJumpSpeed; }
    }

    #endregion

    #region CONSTANTS

    public const float HEIGHT_NORMAL = 1.6f;
    public const float HEIGHT_DUCKED = 0.9f;
    public const float CENTER_OFFSET_DUCKED = -0.4f;
    public const float CENTER_OFFSET_NORMAL = -0.1f;

    #endregion
}
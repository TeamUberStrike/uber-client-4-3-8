using UnityEngine;

public class LevelEnviroment : MonoSingleton<LevelEnviroment>
{
    public EnviromentSettings Settings;

    public const float MovementSpeed = 1f;
    public const float Modifier = 0.035f;
    public const float PlayerWalkSpeed = 7;
    public const float PlayerJumpSpeed = 15;
}
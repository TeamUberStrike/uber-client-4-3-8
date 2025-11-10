using Cmune.Util;
using UnityEngine;

[RequireComponent(typeof(Animation))]
public class AirBalloon : MonoBehaviour
{
    AnimationState animationState;

    private void Start()
    {
        animationState = animation[animation.clip.name];
    }

    private void Update()
    {
        if (GameState.HasCurrentGame && GameState.CurrentGame.IsMatchRunning)
            animationState.time = GameState.CurrentGame.GameTime;
        else
            animationState.time += Time.deltaTime;
    }
}
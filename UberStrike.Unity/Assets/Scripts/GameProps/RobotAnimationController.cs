using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RobotAnimationController : MonoBehaviour
{
    private List<string> _animationNames = new List<string>();
    private Transform _transform;
    private Animation _animation;

	// Use this for initialization
    void Awake()
    {
        _transform = transform;
        _animation = _transform.GetComponent<Animation>();
        _animationNames.Add("BotToBall");
        _animationNames.Add("BallToBot");
        _animationNames.Add("Dance");
        _animationNames.Add("StandToShoot");
        _animationNames.Add("Shoot");
    }

    public void PlayAnimationHard(int animationNr)
    {
        _animation.Stop();
        _animation.Play(_animationNames[animationNr]);
    }
    public void PlayAnimationHard(string animationName)
    {
        _animation.Stop();
        _animation.Play(animationName);
    }

    public bool CheckIfActive(int animationNr)
    {
        return _animation.IsPlaying(_animationNames[animationNr]);
    }
    public bool CheckIfActive(string animationName)
    {
        return _animation.IsPlaying(animationName);
    }

    public void AnimationStop()
    {
        _animation.Stop();
    }
}

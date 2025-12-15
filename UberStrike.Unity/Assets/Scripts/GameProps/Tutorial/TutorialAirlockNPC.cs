using UnityEngine;
using System.Collections;

public class TutorialAirlockNPC : MonoBehaviour
{
    private Transform _transform;

    private Vector3 _finalPos = Vector3.zero;

    private enum State
    {
        Moving = 0,
        Talking
    }

    private State _state = State.Moving;

    private void Awake()
    {
        AnimationState walk;

        walk = GetComponent<Animation>()[AnimationIndex.TutorialGuideWalk.ToString()];

        walk.enabled = true;
        walk.weight = 1;
        walk.speed = 1;

        _transform = transform;
    }

    private void Update()
    {
        if (_transform && _state == State.Moving)
        {
            if (Vector3.SqrMagnitude(_transform.position - _finalPos) < 0.1f)
            {
                _state = State.Talking;

                GetComponent<Animation>().Stop(AnimationIndex.heavyGunUpDown.ToString());

                GetComponent<Animation>().Blend(AnimationIndex.TutorialGuideWalk.ToString(), 0);
                GetComponent<Animation>().Blend(AnimationIndex.TutorialGuideAirlock.ToString(), 1);

                StartCoroutine(StartIdleAnimation());
            }
            else
            {
                _transform.position += _transform.forward * Time.deltaTime * 0.7f;
            }
        }
    }

    private IEnumerator StartIdleAnimation()
    {
        yield return new WaitForSeconds(130 / 25f);

        GetComponent<Animation>().Blend(AnimationIndex.TutorialGuideAirlock.ToString(), 0);
        GetComponent<Animation>().Blend(AnimationIndex.TutorialGuideIdle.ToString(), 1);
    }

    public void SetFinalPosition(Vector3 pos)
    {
        _finalPos = pos;
    }
}

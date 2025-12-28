using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TutorialAirlockDoor : MonoBehaviour
{
    public enum AnimPlayMode
    {
        Forward,
        Backward
    }

    public AnimPlayMode PlayMode;
    public Collider BlockCollider;

    private bool _entered = false;

    public void Reset()
    {
        if (_entered)
        {
            if (PlayMode == AnimPlayMode.Backward)
                transform.rotation = Quaternion.Euler(0, 180 + transform.rotation.eulerAngles.y, 0);
            else
                BlockCollider.enabled = true;
        }

        _entered = false;
        
    }

    private void OnTriggerEnter(Collider c)
    {
        if (LevelTutorial.Instance.AirlockDoorAnim && !_entered)
        {
            AnimationState s = LevelTutorial.Instance.AirlockDoorAnim["DoorOpen"];

            _entered = true;

            if (PlayMode == AnimPlayMode.Backward)
            {
                if (s)
                {
                    s.weight = 1;
                    s.speed = -1;
                    s.normalizedTime = 1;
                    s.enabled = true;
                }
                else
                {
                    Debug.LogError("Failed to get door animation state!");
                }

                transform.rotation = Quaternion.Euler(0, 180 + transform.rotation.eulerAngles.y, 0);

                SfxManager.Play2dAudioClip(LevelTutorial.Instance.BigDoorClose);
            }
            else
            {
                s.enabled = false;
                s.weight = 0;
                s.speed = 1;
                s.normalizedTime = 0;

                LevelTutorial.Instance.AirlockDoorAnim.Play();

                StartCoroutine(StartHideMe(s.length));
            }
        }
    }

    private IEnumerator StartHideMe(float time)
    {
        yield return new WaitForSeconds(time);

        BlockCollider.enabled = false;
    }
}

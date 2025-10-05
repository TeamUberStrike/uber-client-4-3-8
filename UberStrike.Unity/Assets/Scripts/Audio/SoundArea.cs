using UnityEngine;

public class SoundArea : MonoBehaviour
{
    [SerializeField]
    private FootStepSoundType _footStep;

    private void OnTriggerEnter(Collider other)
    {
        SetFootStep(other);
    }

    private void OnTriggerStay(Collider other)
    {
        SetFootStep(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Avatar")
        {
            CharacterTrigger trigger = other.GetComponent<CharacterTrigger>();
            if (trigger && trigger.Avatar && trigger.Avatar.Decorator && GameState.HasCurrentSpace)
            {
                trigger.Avatar.Decorator.SetFootStep(GameState.CurrentSpace.DefaultFootStep);
            }
        }
    }

    private void SetFootStep(Collider other)
    {
        if (other.tag == "Avatar")
        {
            CharacterTrigger trigger = other.GetComponent<CharacterTrigger>();
            if (trigger && trigger.Avatar && trigger.Avatar.Decorator)
            {
                trigger.Avatar.Decorator.SetFootStep(_footStep);
            }
        }
    }
}

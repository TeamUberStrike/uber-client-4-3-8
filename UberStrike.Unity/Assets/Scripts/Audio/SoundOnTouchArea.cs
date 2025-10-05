using UnityEngine;

public class SoundOnTouchArea : MonoBehaviour
{
    [SerializeField]
    private Transform source;

    private void OnTriggerStay(Collider other)
    {
        if (other.tag == "Avatar")
        {
            CharacterTrigger trigger = other.GetComponent<CharacterTrigger>();
            if (trigger && trigger.Avatar.IsLocal)
            {
                source.position = GameState.LocalCharacter.Position + Vector3.down;
            }
        }
    }
}
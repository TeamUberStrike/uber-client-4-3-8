using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DeathArea : MonoBehaviour
{
    private void Awake()
    {
        if (GetComponent<Collider>()) GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider c)
    {
        if (c.tag == "Player" && GameState.HasCurrentPlayer)
        {
            LevelBoundary.KillPlayer();
        }
    }
}

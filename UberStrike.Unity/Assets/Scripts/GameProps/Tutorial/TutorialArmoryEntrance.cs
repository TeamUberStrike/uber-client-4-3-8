using UnityEngine;
using System.Collections;

public class TutorialArmoryEntrance : MonoBehaviour
{
    public Collider Block;

    private bool _entered;

    private void OnTriggerEnter(Collider c)
    {
        if (!_entered && c.tag == "Player")
        {
            _entered = true;

            /* block the armory entrance */

            if (GameState.HasCurrentGame)
            {
                if (GameState.CurrentGame is TutorialGameMode)
                {
                    TutorialGameMode tutorial = GameState.CurrentGame as TutorialGameMode;

                    tutorial.Sequence.OnArmoryEnter();

                    Block.isTrigger = false;

                    StartCoroutine(StartDeleteMe());
                }
            }
        }
    }

    private IEnumerator StartDeleteMe()
    {
        yield return new WaitForEndOfFrame();

        Destroy(gameObject);
    }
}

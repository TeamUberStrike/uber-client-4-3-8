using UnityEngine;
using System.Collections;

class RobotPiecesLogic : MonoBehaviour
{
    [SerializeField]
    private AudioClip[] _robotExplosionAudios;
    [SerializeField]
    private AudioClip[] _robotScrapsDestructionAudios;
    [SerializeField]
    private GameObject _robotPieces;

    public void ExplodeRobot(GameObject robotObject, int lifeTimeMilliSeconds)
    {
        if (_robotPieces != null)
        {
            foreach (var r in _robotPieces.GetComponentsInChildren<Rigidbody>())
            {
                r.AddExplosionForce(5, transform.position, 2, 0, ForceMode.Impulse);
            }
        }

        GetComponent<AudioSource>().clip = _robotExplosionAudios[Random.Range(0, _robotExplosionAudios.Length)];
        GetComponent<AudioSource>().Play();

        MonoRoutine.Start(DestroyRobotPieces(robotObject, lifeTimeMilliSeconds));
    }

    public void PlayRobotScrapsDestructionAudio()
    {
        GetComponent<AudioSource>().clip = _robotScrapsDestructionAudios[Random.Range(0, _robotScrapsDestructionAudios.Length)];
        GetComponent<AudioSource>().Play();
    }

    private IEnumerator DestroyRobotPieces(GameObject robotObject, int lifeTimeMilliSeconds)
    {
        yield return new WaitForSeconds(lifeTimeMilliSeconds / 1000);
        PlayRobotScrapsDestructionAudio();
        yield return new WaitForSeconds(GetComponent<AudioSource>().clip.length);
        GameObject.Destroy(robotObject);
    }
}

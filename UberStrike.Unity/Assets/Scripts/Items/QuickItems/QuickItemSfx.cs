using System.Collections;
using UnityEngine;

public class QuickItemSfx : MonoBehaviour
{
    public int ID { get; set; }

    public bool IsShortAudio
    {
        get { return _isShortAudio; }
        set
        {
            _isShortAudio = value;
            AudioSource audioSource = GetComponentInChildren<AudioSource>();
            if (audioSource != null)
            {
                audioSource.clip = _isShortAudio ? _shortLoopAudio : _normalLoopAudio;
            }
        }
    }

    public Transform Parent { get; set; }
    public Vector3 Offset { get; set; }

    public void Play(int robotLifeTime, int scrapsLifeTime, bool isInstant)
    {
        IsShortAudio = isInstant;
        AudioSource audioSource = GetComponentInChildren<AudioSource>();
        if (audioSource != null)
        {
            audioSource.Play();
        }

        StartCoroutine(StopEffectAfterSeconds(robotLifeTime, scrapsLifeTime));
    }

    public void Explode(int scrapsLifeTime)
    {
        var pieces = GameObject.Instantiate(_robotPiecesPrefab, _robotTransform.position, Quaternion.identity) as GameObject;
        if (pieces != null)
        {
            RobotPiecesLogic robotPiecesLogic = pieces.GetComponentInChildren<RobotPiecesLogic>();
            robotPiecesLogic.ExplodeRobot(pieces, scrapsLifeTime);
        }
        Destroy();
    }

    public void Destroy()
    {
        GameObject.Destroy(gameObject);
    }

    private IEnumerator StopEffectAfterSeconds(int robotLifeTime, int scrapsLifeTime)
    {
        yield return new WaitForSeconds(robotLifeTime / 1000);

        QuickItemSfxController.Instance.RemoveEffect(ID);
        Explode(scrapsLifeTime);
    }

    [SerializeField]
    private GameObject _robotPiecesPrefab;
    [SerializeField]
    private AudioClip _shortLoopAudio;
    [SerializeField]
    private AudioClip _normalLoopAudio;
    [SerializeField]
    private Transform _robotTransform;

    private bool _isShortAudio;
}
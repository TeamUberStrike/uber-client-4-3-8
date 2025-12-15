using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ForceField : MonoBehaviour
{
    #region Fields

    [SerializeField]
    private Vector3 _direction;

    [SerializeField]
    private int _force = 1000;

    private float gizmofactor = 0.0055f;

    #endregion

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        gameObject.layer = (int)UberstrikeLayer.IgnoreRaycast;
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.tag == "Player")
        {
            GameState.LocalPlayer.MoveController.ApplyForce(_direction.normalized * _force, CharacterMoveController.ForceType.Exclusive);

            SfxManager.Play2dAudioClip(SoundEffectType.PropsJumpPad2D);
        }
        else if (collider.gameObject.layer == (int)UberstrikeLayer.RemotePlayer)
        {
            SfxManager.Play3dAudioClip(SoundEffectType.PropsJumpPad, 1.0f, 0.1f, 10.0f, AudioRolloffMode.Linear, transform.position);
        }

        //else if (collider.tag == "Prop")
        //{
        //    BaseGameProp prop = collider.GetComponent<BaseGameProp>();
        //    if (prop != null)
        //    {
        //        prop.ApplyForce(Vector3.zero, _direction.normalized * _force * 2);
        //    }
        //}
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawSphere(transform.localPosition, 0.2f);
        Vector3 v = _direction.normalized;
        v.y *= 0.6f;
        Gizmos.DrawLine(transform.localPosition, transform.localPosition + v * Mathf.Log(_force) * _force * gizmofactor);//);
    }
}
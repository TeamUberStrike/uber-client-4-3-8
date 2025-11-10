using System.Collections;
using UberStrike.Realtime.Common;
using UnityEngine;

public class PlayerDropCoinPickupItem : PickupItem
{
    public float Timeout = 10;

    private float _timeout;

    private IEnumerator Start()
    {
        //Debug.LogError(PickupID + ": " + transform.position.x.ToString("f5") + "," + transform.position.y.ToString("f5") + "," + transform.position.z.ToString("f5"));

        _timeout = Time.time + Timeout;

        //find the clostest position on the ground
        Vector3 oldpos = transform.position;
        Vector3 newpos = oldpos;
        RaycastHit hit;
        if (UnityEngine.Physics.Raycast(oldpos + Vector3.up, Vector3.down, out hit, 100, UberstrikeLayerMasks.ProtectionMask))
        {
            if (oldpos.y > (hit.point.y + 1))
                newpos = hit.point + Vector3.up;
        }

        //slowly move down and wait until countdown is finished
        _timeout = Time.time + Timeout;
        float time = 0;
        while (_timeout > Time.time)
        {
            yield return new WaitForEndOfFrame();
            time += Time.deltaTime;

            transform.position = Vector3.Lerp(oldpos, newpos, time);
        }

        //StartCoroutine(StartHidingPickupForSeconds(0));
        SetItemAvailable(false);

        enabled = false;

        yield return new WaitForSeconds(2);

        GameObject.Destroy(gameObject);
    }

    private void Update()
    {
        if (_pickupItem)
        {
            _pickupItem.Rotate(Vector3.up, 150 * Time.deltaTime, Space.Self);
        }
    }

    protected override bool OnPlayerPickup()
    {
        if (GameState.HasCurrentGame)
        {
            GameState.CurrentGame.PickupPowerup(PickupID, PickupItemType.Coin, 1);

            PickupNameHud.Instance.DisplayPickupName("Point", PickUpMessageType.Coin);
            XpPtsHud.Instance.GainPoints(1);

            PlayLocalPickupSound(SoundEffectType.GameGetCredits);

            StartCoroutine(StartHidingPickupForSeconds(0));
        }

        return true;
    }

    protected override void OnRemotePickup()
    {
        PlayRemotePickupSound(SoundEffectType.GameGetCredits, this.transform.position);
    }
}
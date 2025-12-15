using UnityEngine;

public class GrenadeProjectile : Projectile
{
    protected override void Start()
    {
        base.Start();

        if (Detonator != null) Detonator.Direction = Vector3.zero;

        Rigidbody.useGravity = true;

        //grenade is spinning around a random local axis,
        //creates a more dynamic feeling of the grenade, purely visual effect
        Rigidbody.AddRelativeTorque(Random.insideUnitSphere.normalized * 10);
    }

    protected override void OnTriggerEnter(Collider c)
    {
        if (!IsProjectileExploded)
        {
            //if we touch an object that is a player or movable prop any grenade explodes immediately
            if (LayerUtil.IsLayerInMask(UberstrikeLayerMasks.GrenadeCollisionMask, c.gameObject.layer))
            {
                ProjectileManager.Instance.RemoveProjectile(ID, true);
                GameState.CurrentGame.RemoveProjectile(ID, true);
            }

            PlayBounceSound(c.transform.position);
        }
    }

    protected override void OnCollisionEnter(Collision c)
    {
        if (!IsProjectileExploded)
        {
            //if we touch an object that is a player or movable prop any grenade explodes immediately
            if (LayerUtil.IsLayerInMask(UberstrikeLayerMasks.GrenadeCollisionMask, c.gameObject.layer))
            {
                ProjectileManager.Instance.RemoveProjectile(ID, true);
                GameState.CurrentGame.RemoveProjectile(ID, true);
            }
            else if (_sticky)
            {
                Rigidbody.isKinematic = true;
                GetComponent<Collider>().isTrigger = true;

                if (c.contacts.Length > 0)
                {
                    transform.position = c.contacts[0].point + c.contacts[0].normal * GetComponent<Collider>().bounds.extents.sqrMagnitude;
                }
            }

            PlayBounceSound(c.transform.position);
        }
    }

    #region FIELDS

    [SerializeField]
    private bool _sticky = false;

    #endregion
}

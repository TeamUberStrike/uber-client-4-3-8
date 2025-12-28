
using Cmune.Realtime.Common.Utils;
using UnityEngine;

public class InstantHitWeapon : BaseWeaponLogic
{
    private BaseWeaponDecorator _decorator;
    private bool _supportIronSight = false;

    public InstantHitWeapon(WeaponItem item, BaseWeaponDecorator decorator, IWeaponController controller)
        : base(item, controller)
    {
        _decorator = decorator;
        _supportIronSight = (item.Configuration.SecondaryAction == WeaponSecondaryAction.IronSight ? true : false);
    }

    public override BaseWeaponDecorator Decorator
    {
        get
        {
            return _decorator;
        }
    }

    public override void Shoot(Ray ray, out CmunePairList<BaseGameProp, ShotPoint> hits)
    {
        const int range = 1000;

        hits = null;

        BaseGameProp gameProp;

        Vector3 dir = WeaponDataManager.ApplyDispersion(ray.direction, Config, _supportIronSight);
        int shotCount = Controller.NextProjectileId();

        RaycastHit hit;
        if (Physics.Raycast(ray.origin, dir, out hit, range, Controller.IsLocal ? UberstrikeLayerMasks.ShootMask : UberstrikeLayerMasks.ShootMaskRemotePlayer))
        {
            HitPoint hitPoint = new HitPoint(hit.point, TagUtil.GetTag(hit.collider));

            gameProp = hit.collider.GetComponent<BaseGameProp>();
            if (gameProp)
            {
                hits = new CmunePairList<BaseGameProp, ShotPoint>(1);
                hits.Add(gameProp, new ShotPoint(hit.point, shotCount));
            }

            Decorator.PlayImpactSoundAt(hitPoint);
        }
        else
        {
            hit.point = ray.origin + ray.direction * 1000f;
        }

        if (Decorator)
        {
            Decorator.ShowShootEffect(new RaycastHit[] { hit });
        }

        OnHits(hits);
    }
}

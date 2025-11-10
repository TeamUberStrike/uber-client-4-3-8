
using System.Collections.Generic;
using Cmune.Realtime.Common.Utils;
using UnityEngine;

public class InstantMultiHitWeapon : BaseWeaponLogic
{
    public InstantMultiHitWeapon(WeaponItem item, BaseWeaponDecorator decorator, int shotGauge, IWeaponController controller)
        : base(item, controller)
    {
        ShotgunGauge = shotGauge;

        _decorator = decorator;
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
        Dictionary<BaseGameProp, ShotPoint> allHits = new Dictionary<BaseGameProp, ShotPoint>(ShotgunGauge);

        HitPoint hitPoint = null;
        ShotPoint point;
        RaycastHit hit;
        BaseGameProp gameProp;
        Vector3 dir;
        int range;

        RaycastHit[] allHitsArray = new RaycastHit[ShotgunGauge];
        int shotCount = Controller.NextProjectileId();

        range = 1000;

        for (int i = 0; i < ShotgunGauge; i++)
        {
            dir = WeaponDataManager.ApplyDispersion(ray.direction, Config, false);
            if (Physics.Raycast(ray.origin, dir, out hit, range, Controller.IsLocal ? UberstrikeLayerMasks.ShootMask : UberstrikeLayerMasks.ShootMaskRemotePlayer))
            {
                if (hitPoint == null)
                    hitPoint = new HitPoint(hit.point, TagUtil.GetTag(hit.collider));

                gameProp = hit.collider.GetComponent<BaseGameProp>();
                if (gameProp)
                {
                    if (allHits.TryGetValue(gameProp, out point))
                    {
                        point.AddPoint(hit.point);
                    }
                    else
                        allHits.Add(gameProp, new ShotPoint(hit.point, shotCount));
                }
                else
                {
                    //object is not shootable
                }

                allHitsArray[i] = hit;
            }
            else
            {
                allHitsArray[i].point = ray.origin + ray.direction * 1000f;
                allHitsArray[i].normal = hit.normal;
            }
        }

        Decorator.PlayImpactSoundAt(hitPoint);

        hits = new CmunePairList<BaseGameProp, ShotPoint>(allHits.Count);
        foreach (KeyValuePair<BaseGameProp, ShotPoint> p in allHits)
            hits.Add(p.Key, p.Value);

        if (Decorator)
        {
            Decorator.ShowShootEffect(allHitsArray);
        }

        //WATER EFFECT
        //if (Physics.Raycast(ray.origin, ray.direction, out hit, range, UberstrikeLayerMasks.WaterMask))
        //{
        //    if (Decorator)
        //        Decorator.Shoot(new RaycastHit[] { hit });
        //}

        OnHits(hits);
    }

    #region FIELDS

    private int ShotgunGauge;

    private BaseWeaponDecorator _decorator;

    #endregion
}

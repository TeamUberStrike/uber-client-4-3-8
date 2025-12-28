using System.Collections;
using Cmune.Realtime.Common.Utils;
using UnityEngine;

public class MeleeWeapon : BaseWeaponLogic
{
    private MeleeWeaponDecorator _decorator;

    public override BaseWeaponDecorator Decorator
    {
        get
        {
            return _decorator;
        }
    }

    public MeleeWeapon(WeaponItem item, MeleeWeaponDecorator decorator, IWeaponController controller)
        : base(item, controller)
    {
        _decorator = decorator;

        //Config.AccuracySpread = new Vector2(1, 3);
    }

    public override float HitDelay
    {
        get { return 0.2f; }
    }

    public override void Shoot(Ray ray, out CmunePairList<BaseGameProp, ShotPoint> hits)
    {
        Vector3 origin = ray.origin;
        origin.y -= 0.1f;

        ray.origin = origin;

        hits = null;

        BaseGameProp gameProp;
        float range = 1;

        int layerMask = Controller.IsLocal ? UberstrikeLayerMasks.ShootMask : UberstrikeLayerMasks.ShootMaskRemotePlayer;
        float distance = 1;
        RaycastHit[] sphereHits = Physics.SphereCastAll(ray, range, distance, layerMask);

        // Iterate sphere hits array and find the object closest to the origin
        // Debug out the closest object and furthest object
        int shotCount = Controller.NextProjectileId();

        if (sphereHits != null && sphereHits.Length > 0)
        {
            hits = new CmunePairList<BaseGameProp, ShotPoint>();

            float closestHit = Mathf.Infinity;
            RaycastHit closestHitRay = sphereHits[0];

            for (int i = 0; i < sphereHits.Length; i++)
            {
                RaycastHit h = sphereHits[i];
                Vector3 hitDir = h.point - ray.origin;

                if (Vector3.Dot(ray.direction, hitDir) > 0 && h.distance < closestHit)
                {
                    closestHit = h.distance;
                    closestHitRay = h;
                }
            }

            if (closestHitRay.collider)
            {
                gameProp = closestHitRay.collider.GetComponent<BaseGameProp>();

                if (gameProp != null)
                    hits.Add(gameProp, new ShotPoint(closestHitRay.point, shotCount));

                if (_decorator)
                    _decorator.StartCoroutine(StartShowingEffect(closestHitRay, ray.origin, HitDelay));
            }
        }
        else if (_decorator)
        {
            _decorator.ShowShootEffect(new RaycastHit[] { });
        }

        EmitWaterImpactParticles(ray, range);

        OnHits(hits);
    }

    private IEnumerator StartShowingEffect(RaycastHit hit, Vector3 origin, float delay)
    {
        if (_decorator)
            _decorator.ShowShootEffect(new RaycastHit[] { hit });

        yield return new WaitForSeconds(delay);

        Decorator.PlayImpactSoundAt(new HitPoint(hit.point, TagUtil.GetTag(hit.collider)));
    }

    private void EmitWaterImpactParticles(Ray ray, float radius)
    {
        Vector3 origin = ray.origin;
        Vector3 hit = origin + ray.direction * radius;

        if (GameState.HasCurrentSpace && GameState.CurrentSpace.HasWaterPlane &&
            ((origin.y > GameState.CurrentSpace.WaterPlaneHeight && hit.y < GameState.CurrentSpace.WaterPlaneHeight) ||
            (origin.y < GameState.CurrentSpace.WaterPlaneHeight && hit.y > GameState.CurrentSpace.WaterPlaneHeight)))
        {
            Vector3 point = hit;
            point.y = GameState.CurrentSpace.WaterPlaneHeight;

            if (!Mathf.Approximately(ray.direction.y, 0))
            {
                point.x = (GameState.CurrentSpace.WaterPlaneHeight - hit.y) / ray.direction.y * ray.direction.x + hit.x;
                point.z = (GameState.CurrentSpace.WaterPlaneHeight - hit.y) / ray.direction.y * ray.direction.z + hit.z;
            }

            MoveTrailrendererObject trail = Decorator.TrailRenderer;
            ParticleEffectController.ShowHitEffect(ParticleConfigurationType.MeleeDefault, SurfaceEffectType.WaterEffect, Vector3.up, point, Vector3.up, origin, 1, ref trail, Decorator.transform);
        }
    }
}
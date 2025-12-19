
using System;
using System.Collections;
using Cmune.Realtime.Common.Utils;
using UnityEngine;

public class ProjectileWeapon : BaseWeaponLogic
{
    private ProjectileWeaponDecorator _decorator;
    public event Action<ProjectileInfo> OnProjectileShoot;

    #region Properties

    public override BaseWeaponDecorator Decorator { get { return _decorator; } }

    public int MaxConcurrentProjectiles { get; private set; }

    public int MinProjectileDistance { get; private set; }

    public int ProjetileCountPerShoot { get; set; }

    public bool HasProjectileLimit
    {
        get { return MaxConcurrentProjectiles > 0; }
    }

    #endregion

    public ProjectileWeapon(WeaponItem item, ProjectileWeaponDecorator decorator, IWeaponController controller)
        : base(item, controller)
    {
        _decorator = decorator;

        MaxConcurrentProjectiles = item.Configuration.MaxConcurrentProjectiles;
        MinProjectileDistance = item.Configuration.MinProjectileDistance;
        ExplosionType = item.Configuration.ParticleEffect;
        ProjetileCountPerShoot = item.Configuration.ProjectilesPerShot;
    }

    public ParticleConfigurationType ExplosionType { get; private set; }

    public override void Shoot(Ray ray, out CmunePairList<BaseGameProp, ShotPoint> hits)
    {
        hits = null;

        RaycastHit hit;

        //if the raycast hits something, we just directly create an explosion
        if (MinProjectileDistance > 0 && Physics.Raycast(ray.origin, ray.direction, out hit, MinProjectileDistance, UberstrikeLayerMasks.LocalRocketMask))
        {
            int shotCount = Controller.NextProjectileId();

            hits = new CmunePairList<BaseGameProp, ShotPoint>(1);
            hits.Add(null, new ShotPoint(hit.point, shotCount));

            ShowExplosionEffect(hit.point, hit.normal, ray.direction, shotCount);
            if (OnProjectileShoot != null)
            {
                OnProjectileShoot(new ProjectileInfo(shotCount, new Ray(hit.point, -ray.direction)));
            }
        }
        else
        {
            if (_decorator) _decorator.ShowShootEffect(new RaycastHit[] { });
            MonoRoutine.Start(EmitProjectile(ray));
        }
    }

    public void ShowExplosionEffect(Vector3 position, Vector3 normal, Vector3 direction, int projectileId)
    {
        //show visuals
        if (_decorator)
            _decorator.ShowExplosionEffect(position, normal, ExplosionType);
    }

    private IEnumerator EmitProjectile(Ray ray)
    {
        if (ProjetileCountPerShoot > 1)
        {
            float angle = 360 / ProjetileCountPerShoot;
            
            for (int i = 0; i < ProjetileCountPerShoot; i++)
            {
                //we have to check if the dcorator is null, because it's possible that we recreate your laodout between 1 shot and the other
                if (_decorator != null)
                {
                    int shotCount = Controller.NextProjectileId();
                    ray.origin = _decorator.MuzzlePosition + Quaternion.AngleAxis(angle * i, _decorator.transform.forward) * _decorator.transform.up * 0.2f;
                    var p = EmitProjectile(ray, shotCount, GameState.LocalCharacter.ActorId);

                    if (OnProjectileShoot != null)
                    {
                        OnProjectileShoot(new ProjectileInfo(shotCount, ray)
                            {
                                Projectile = p
                            });
                    }
                    yield return new WaitForSeconds(0.2f);
                }
            }
        }
        else
        {
            int shotCount = Controller.NextProjectileId();
            var p = EmitProjectile(ray, shotCount, GameState.LocalCharacter.ActorId);
            if (OnProjectileShoot != null)
            {
                OnProjectileShoot(new ProjectileInfo(shotCount, ray)
                {
                    Projectile = p
                });
            }
        }
    }

    public Projectile EmitProjectile(Ray ray, int projectileID, int actorID)
    {
        //send the missle on the way
        if (_decorator)
        {
            Projectile projectile = GameObject.Instantiate(_decorator.Missle, _decorator.MuzzlePosition, Quaternion.LookRotation(ray.direction)) as Projectile;
            if (projectile)
            {
                //Let projectile know it's ID
                projectile.gameObject.tag = "Prop";
                projectile.ExplosionEffect = ExplosionType;
				if(_decorator.MissileTimeOut > 0)
				{
                	projectile.TimeOut = _decorator.MissileTimeOut;
				}
                projectile.SetExplosionSound(_decorator.ExplosionSound);

                projectile.transform.position = ray.origin + MinProjectileDistance * ray.direction;

                if (Controller.IsLocal)
                {
                    projectile.gameObject.layer = (int)UberstrikeLayer.LocalProjectile;
                }
                else
                {
                    projectile.gameObject.layer = (int)UberstrikeLayer.RemoteProjectile;
                }

                //ignore self collision for local & remote player
                CharacterConfig cfg;
                if (GameState.CurrentGame != null && GameState.CurrentGame.TryGetCharacter(actorID, out cfg) && cfg.Decorator)
                {
                    if (projectile.gameObject.active)
                    {
                        foreach (CharacterHitArea a in cfg.Decorator.HitAreas)
                        {
                            if (a.gameObject.active)
                            {
                                Physics.IgnoreCollision(projectile.gameObject.collider, a.collider);
                            }
                        }
                    }
                }

                projectile.MoveInDirection(ray.direction * WeaponConfigurationHelper.GetProjectileSpeed(Config));

                return projectile;
            }
        }

        return null;
    }
}
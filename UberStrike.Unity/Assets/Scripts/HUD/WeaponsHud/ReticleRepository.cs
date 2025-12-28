using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UberStrike.DataCenter.Common.Entities;
using UberStrike.Core.Types;

class ReticleRepository : Singleton<ReticleRepository>
{
    public Reticle GetReticle(UberstrikeItemClass type)
    {
        if (_reticles.ContainsKey(type))
        {
            return _reticles[type];
        }
        else
        {
            return null;
        }
    }

    public void UpdateAllReticles()
    {
        foreach (Reticle reticle in _reticles.Values)
        {
            reticle.Update();
        }
    }

    private ReticleRepository()
    {
        InitReticles();
    }

    private void InitReticles()
    {
        _reticles = new Dictionary<UberstrikeItemClass, Reticle>(8);

        Reticle machineGun = new Reticle();
        machineGun.SetInnerScale(HudTextures.MGScale, 1.2f);
        machineGun.SetTranslate(HudTextures.MGTranslate, 5);

        Reticle sniperRifle = new Reticle();
        sniperRifle.SetInnerScale(HudTextures.SRScale, 1.2f);
        sniperRifle.SetTranslate(HudTextures.SRTranslate, 5);

        Reticle shotGun = new Reticle();
        shotGun.SetInnerScale(HudTextures.SGScaleInside, 1.2f);
        shotGun.SetOutterScale(HudTextures.SGScaleOutside, 2f);

        Reticle cannon = new Reticle();
        cannon.SetRotate(HudTextures.CNRotate, 60);
        cannon.SetInnerScale(HudTextures.CNScale, 1.5f);

        Reticle handGun = new Reticle();
        handGun.SetTranslate(HudTextures.HGTraslate, 5);

        Reticle splatterGun = new Reticle();
        splatterGun.SetInnerScale(HudTextures.SPScale, 1.2f);
        splatterGun.SetTranslate(HudTextures.SPTranslate, 5);

        Reticle launcher = new Reticle();
        launcher.SetInnerScale(HudTextures.LRScale, 1.2f);
        launcher.SetTranslate(HudTextures.LRTranslate, 5);

        Reticle melee = new Reticle();
        melee.SetTranslate(HudTextures.MWTranslate, 5);

        _reticles.Add(UberstrikeItemClass.WeaponMachinegun, machineGun);
        _reticles.Add(UberstrikeItemClass.WeaponSniperRifle, sniperRifle);
        _reticles.Add(UberstrikeItemClass.WeaponShotgun, shotGun);
        _reticles.Add(UberstrikeItemClass.WeaponCannon, cannon);
        _reticles.Add(UberstrikeItemClass.WeaponHandgun, handGun);
        _reticles.Add(UberstrikeItemClass.WeaponSplattergun, splatterGun);
        _reticles.Add(UberstrikeItemClass.WeaponLauncher, launcher);
        _reticles.Add(UberstrikeItemClass.WeaponMelee, melee);
    }

    private Dictionary<UberstrikeItemClass, Reticle> _reticles;
}

using UnityEngine;

public class ParticleCobfigurationPerWeapon : MonoBehaviour
{
    [SerializeField]
    private WeaponImpactEffectConfiguration _weaponImpactEffectConfiguration;

    public WeaponImpactEffectConfiguration WeaponImpactEffectConfiguration
    {
        get { return _weaponImpactEffectConfiguration; }
    }
}

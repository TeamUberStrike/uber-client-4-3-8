
using UnityEngine;

[System.Serializable]
public class ExplosiveGrenadeConfiguration : QuickItemConfiguration
{
    [CustomProperty("Damage")]
    [SerializeField]
    private int _damage = 100;
    public int Damage { get { return _damage; } }

    [CustomProperty("SplashRadius")]
    [SerializeField]
    private int _splash = 2;
    public int SplashRadius { get { return _splash; } }

    [CustomProperty("LifeTime")]
    [SerializeField]
    private int _lifeTime = 15;
    public int LifeTime { get { return _lifeTime; } }

    [CustomProperty("Bounciness")]
    [SerializeField]
    private int _bounciness = 3;
    public float Bounciness { get { return _bounciness * 0.1f; } }

    [CustomProperty("Sticky")]
    [SerializeField]
    private bool _isSticky = true;
    public bool IsSticky { get { return _isSticky; } }

    [SerializeField]
    private int _speed = 15;
    public int Speed { get { return _speed; } }
}
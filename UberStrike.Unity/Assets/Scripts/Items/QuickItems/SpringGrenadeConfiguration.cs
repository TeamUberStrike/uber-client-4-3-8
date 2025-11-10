
using UnityEngine;

[System.Serializable]
public class SpringGrenadeConfiguration : QuickItemConfiguration
{
    [SerializeField]
    private Vector3 _jumpDirection = Vector3.up;
    public Vector3 JumpDirection { get { return _jumpDirection; } }

    [CustomProperty("Force")]
    [SerializeField]
    private int _force = 1250;
    public int Force { get { return _force; } }

    [CustomProperty("LifeTime")]
    [SerializeField]
    private int _lifeTime = 15;
    public int LifeTime { get { return _lifeTime; } }

    [CustomProperty("Sticky")]
    [SerializeField]
    private bool _isSticky = true;
    public bool IsSticky { get { return _isSticky; } }

    [SerializeField]
    private int _speed = 10;
    public int Speed { get { return _speed; } }
}
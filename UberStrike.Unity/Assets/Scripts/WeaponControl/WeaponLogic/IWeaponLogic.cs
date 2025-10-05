
using Cmune.Realtime.Common.Utils;
using UnityEngine;

public interface IWeaponLogic
{
    void Shoot(Ray ray, out CmunePairList<BaseGameProp, ShotPoint> hits);

    BaseWeaponDecorator Decorator { get; }
}

public class ShotPoint
{
    private Vector3 _aggregatedPoint;
    public int ProjectileId { get; private set; }
    public int Count { get; private set; }

    public ShotPoint(Vector3 point, int projectileId)
    {
        AddPoint(point);
        ProjectileId = projectileId;
    }

    public void AddPoint(Vector3 point)
    {
        _aggregatedPoint += point;
        Count++;
    }

    public Vector3 MidPoint
    {
        get { return _aggregatedPoint / Count; }
    }
}

public class HitPoint
{
    public HitPoint(Vector3 p, string t)
    {
        Point = p;
        Tag = t;
    }

    public Vector3 Point { get; private set; }
    public string Tag { get; private set; }
}
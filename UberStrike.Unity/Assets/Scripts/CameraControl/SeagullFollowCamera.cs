using UnityEngine;
using System.Collections;

public class SeagullFollowCamera : MonoBehaviour
{
    public Transform Seagull;
    public float _positionDamping;
    public float _rotationDamping;

    private Transform _transformCache;

    void LateUpdate()
    {
        if (!Seagull) return;
        if (!_transformCache) _transformCache = transform;

        _transformCache.position = Vector3.Lerp(_transformCache.position, Seagull.position, Time.deltaTime * _positionDamping);
        _transformCache.rotation = Quaternion.Lerp(_transformCache.rotation, Seagull.rotation, Time.deltaTime * _rotationDamping);
    }
}

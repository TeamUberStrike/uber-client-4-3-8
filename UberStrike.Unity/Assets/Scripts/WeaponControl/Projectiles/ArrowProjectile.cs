using UnityEngine;
using System.Collections;

public class ArrowProjectile : MonoBehaviour
{
    //private Transform _transform;
    //private Transform _parent;

    //private Vector3 _localPos;
    //private Vector3 _localRot;

    /// <summary>
    /// Destroy projectile instantly
    /// </summary>
    public void Destroy()
    {
        Destroy(gameObject);
    }

    /// <summary>
    /// Destroy projectile after given time
    /// </summary>
    /// <param name="timeDelay">Time delay for object destruction</param>
    public void Destroy(int timeDelay)
    {
        Destroy(gameObject, timeDelay);
    }

    public void SetParent(Transform parent)
    {
        transform.parent = parent;
        //_parent = parent;

        //if (_parent && _transform)
        //{
        //    _localPos = _parent.InverseTransformPoint(_transform.position);
        //    _localRot = _parent.InverseTransformDirection(_transform.forward);
        //}
    }

    //private void Awake()
    //{
    //    _transform = transform;
    //}

    //private void Update()
    //{
    //    if (_transform && _parent)
    //    {
    //        _transform.position = _parent.TransformPoint(_localPos);
    //        _transform.rotation = Quaternion.LookRotation(_parent.TransformDirection(_localRot));
    //    }
    //}
}
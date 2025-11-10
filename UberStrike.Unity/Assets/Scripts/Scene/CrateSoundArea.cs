using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SoundArea))]
public class CrateSoundArea : MonoBehaviour
{
    [SerializeField]
    private BoxCollider _boxCollider;

    [SerializeField]
    private float _offset;

    private Transform _transform;

    private void Awake()
    {
        _transform = transform;

        if (_boxCollider)
            _boxCollider.center = CalcTriggerCenter(_transform, _offset);
        else
            Debug.LogError("There is no box collider attached to the crate!");
    }

    private void Update()
    {
        if (_transform && _boxCollider )
        {
            _boxCollider.center = CalcTriggerCenter(_transform, _offset);
        }
    }

    private Vector3 CalcTriggerCenter(Transform t, float offset)
    {
        Vector3 res = Vector3.zero;

        if (t)
        {
            res = t.InverseTransformDirection(Vector3.up * offset);
        }

        return res;
    }
}

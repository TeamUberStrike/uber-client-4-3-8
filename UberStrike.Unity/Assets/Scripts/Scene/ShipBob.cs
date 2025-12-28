using UnityEngine;
using System.Collections;

public class ShipBob : MonoBehaviour
{
    [SerializeField]
    private float rotateAmount = 1.0f;
    [SerializeField]
    private float moveAmount = 0.005f;

    private Transform _transform;
    private Vector3 shipRotation;

    void Awake()
    {
        _transform = this.transform;
        shipRotation = _transform.localRotation.eulerAngles;
    }

    void Update()
    {
        _transform.position = new Vector3(_transform.position.x, _transform.position.y + (Mathf.Sin(Time.time) * moveAmount), _transform.position.z);
        float bobMotion = Mathf.Sin(Time.time) * rotateAmount;
        _transform.localRotation = Quaternion.Euler(shipRotation + new Vector3(bobMotion, bobMotion, bobMotion));
    }
}

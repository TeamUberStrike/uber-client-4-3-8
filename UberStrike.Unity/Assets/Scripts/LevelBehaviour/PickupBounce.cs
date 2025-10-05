using UnityEngine;
using System.Collections;

public class PickupBounce : MonoBehaviour
{
    private float origPosY;
    private float startOffset;
    void Awake()
    {
        origPosY = transform.position.y;
        startOffset = Random.value * 3.0f;
    }
    void FixedUpdate()
    {
        transform.Rotate(new Vector3(0, 2.0f, 0));
        transform.position = new Vector3(transform.position.x, origPosY + (Mathf.Sin((startOffset + Time.realtimeSinceStartup) * 4.0f) * 0.08f), transform.position.z);
    }
}

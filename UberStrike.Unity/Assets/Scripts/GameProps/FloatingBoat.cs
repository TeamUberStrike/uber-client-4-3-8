using UnityEngine;
using System.Collections;

public class FloatingBoat : MonoBehaviour
{
    public float Offset = 1;
    public float Force = 400;

    public Transform bug;
    public Transform heck1;
    public Transform heck2;

    Rigidbody rb;
    Transform tf;

    void Start()
    {
        rb = rigidbody;
        tf = transform;
    }

    void OnEnable()
    {
        StopCoroutine("startKeepBoatUpright");
        StartCoroutine("startKeepBoatUpright");
    }

    float torque = 0;
    // Update is called once per frame
    IEnumerator startKeepBoatUpright()
    {
        while (true)
        {
            float tor = tf.localRotation.eulerAngles.z - 180;
            if (Mathf.Abs(tor) < 90)
                torque = Mathf.Lerp(torque, 5 * (90 - tor), 10 * Time.deltaTime);
            else
                torque = 0;

            rb.AddRelativeTorque(new Vector3(0, 0, torque));

            yield return new WaitForFixedUpdate();
        }
    }
}

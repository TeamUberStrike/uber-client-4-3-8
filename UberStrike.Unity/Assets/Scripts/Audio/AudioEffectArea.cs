using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class AudioEffectArea : MonoBehaviour
{
    [SerializeField]
    private GameObject outdoorEnvironment;

    [SerializeField]
    private GameObject indoorEnvironment;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;

        if (indoorEnvironment != null)
            indoorEnvironment.active = true;
        if (outdoorEnvironment != null)
            outdoorEnvironment.active = false;
    }

    void OnTriggerEnter(Collider collider)
    {
        if (collider.tag == "Player")
        {
            if (outdoorEnvironment != null)
                outdoorEnvironment.active = false;
            if (indoorEnvironment != null)
                indoorEnvironment.active = true;

        }
    }

    void OnTriggerExit(Collider collider)
    {
        if (collider.tag == "Player")
        {
            if (outdoorEnvironment != null)
                outdoorEnvironment.active = true;
            if (indoorEnvironment != null)
                indoorEnvironment.active = false;
        }
    }
}
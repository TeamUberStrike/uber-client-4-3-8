using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class CameraComponents : MonoBehaviour
{
    void Awake()
    {
        Camera = GetComponent<Camera>();
        MouseOrbit = GetComponent<MouseOrbit>();
    }

    public Camera Camera { get; private set; }
    public MouseOrbit MouseOrbit { get; private set; }
}
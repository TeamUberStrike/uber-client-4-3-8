using UnityEngine;
using System.Collections;
using System;

public class MouseOrbit : MonoSingleton<MouseOrbit>
{
    [SerializeField]
    private Transform target;

    private const float zoomSpeedFactor = 15;
    private const float zoomMin = 1.3f;
    private const float zoomMax = 5;
    private float zoomDistance = 5;
    private float zoomTarget = 5;

    private const float flingSpeedFactor = 0.1f;
    private const float orbitSpeedFactor = 3;
    private const float yOrbitMin = 10f;
    private const float yOrbitMax = 80f;

    private float xOrbit = 0.0f;
    private float yOrbit = 0.0f;

    private Vector3 mousePos;
    private Vector2 mouseAxisSpin;
    private bool listenToMouseUp = false;
    private bool isMouseDraging = false;

    public static bool IsEnabled { get; set; }

    void Awake()
    {
        enabled = false;
        if (target == null)
            throw new NullReferenceException("MouseOrbit.target not set");
    }

    void Start()
    {
        mouseAxisSpin = Vector2.zero;
        Vector3 angles = transform.eulerAngles;
        xOrbit = angles.y;
        yOrbit = angles.x;
    }

    void OnEnable()
    {
        zoomDistance = zoomTarget = Vector3.Distance(transform.position, target.position);

        xOrbit = transform.rotation.eulerAngles.y;
        yOrbit = transform.rotation.eulerAngles.x;
    }

    void LateUpdate()
    {
        if (!PopupSystem.IsAnyPopupOpen && !PanelManager.IsAnyPanelOpen)
        {
            // Scroll wheel zoom
            if (camera.pixelRect.Contains(Input.mousePosition) && Input.GetAxis("Mouse ScrollWheel") != 0)
            {
                zoomTarget = Mathf.Clamp(zoomDistance - Input.GetAxis("Mouse ScrollWheel") * zoomSpeedFactor, zoomMin, zoomMax);
            }

            zoomDistance = Mathf.Lerp(zoomDistance, zoomTarget, Time.deltaTime * 5);

            // Check if click was within the cameras view
            if (Input.GetMouseButtonDown(0))
            {
                if (GameState.HasCurrentSpace && GameState.CurrentSpace.Camera.pixelRect.Contains(Input.mousePosition))
                {
                    mouseAxisSpin = Vector2.zero;
                    listenToMouseUp = true;
                    isMouseDraging = true;
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                if (listenToMouseUp)
                {
                    float velocity = Mathf.Clamp((Input.mousePosition - mousePos).magnitude, 0, 3);

                    mouseAxisSpin = (Input.mousePosition - mousePos).normalized * velocity;
                }
                else
                {
                    mouseAxisSpin = Vector2.zero;
                }

                listenToMouseUp = false;
            }

            mousePos = Input.mousePosition;

            // Rotate camera based on mouse drag
            if (isMouseDraging && Input.GetMouseButton(0))
            {
                xOrbit += Input.GetAxis("Mouse X") * orbitSpeedFactor;
                yOrbit -= Input.GetAxis("Mouse Y") * orbitSpeedFactor;
            }
            // Spin camera based on mouse speed after release
            else if (mouseAxisSpin.magnitude > 0.1f * flingSpeedFactor)
            {
                mouseAxisSpin = Vector2.Lerp(mouseAxisSpin, Vector2.zero, Time.deltaTime * 2);

                xOrbit += mouseAxisSpin.x * flingSpeedFactor;
                yOrbit -= mouseAxisSpin.y * flingSpeedFactor;
            }
            else
            {
                mouseAxisSpin = Vector2.zero;
            }

            // Make sure we cant rotate upside down in the scene
            yOrbit = Mathf.Clamp(yOrbit, yOrbitMin, yOrbitMax);

            Quaternion rotation = Quaternion.Euler(yOrbit, xOrbit, 0);
            transform.rotation = rotation;
            transform.position = target.position + rotation * new Vector3(0, 0, -zoomDistance);
        }
        else
        {
            listenToMouseUp = false;
            mouseAxisSpin = Vector2.zero;
        }

        if (isMouseDraging && !Input.GetMouseButton(0))
        {
            isMouseDraging = false;
        }
    }

    public static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F)
            angle += 360F;
        if (angle > 360F)
            angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }
}
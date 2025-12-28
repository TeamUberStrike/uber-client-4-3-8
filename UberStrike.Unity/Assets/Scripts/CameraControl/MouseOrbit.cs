using UnityEngine;
using System.Collections;
using System;

public class MouseOrbit : MonoSingleton<MouseOrbit>
{
    [SerializeField]
    private Transform target;

    private const float zoomSpeedFactor = 15;
    private const float zoomTouchSpeedFactor = 0.001f;
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

    private Vector2 mouseAxisSpin;

#if !UNITY_ANDROID && !UNITY_IPHONE
    private Vector3 mousePos;
    private bool listenToMouseUp = false;
    private bool isMouseDragging = false;
#else
    private Queue lastTouchDeltas = new Queue();
    private Queue lastDragDiffs = new Queue();
#endif

    public float MaxX = 0;

    public static bool Disable { get; set; }

    void Awake()
    {
        enabled = false;
        Disable = false;
        if (target == null)
            throw new NullReferenceException("MouseOrbit.target not set");
    }

    void Start()
    {
        mouseAxisSpin = Vector2.zero;
        Vector3 angles = transform.eulerAngles;
        xOrbit = angles.y;
        yOrbit = angles.x;

        MaxX = Screen.width;
    }

    void OnEnable()
    {
        zoomDistance = zoomTarget = Vector3.Distance(transform.position, target.position);

        xOrbit = transform.rotation.eulerAngles.y;
        yOrbit = transform.rotation.eulerAngles.x;
    }

    void LateUpdate()
    {

#if !UNITY_ANDROID && !UNITY_IPHONE
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
                    isMouseDragging = true;
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
            if (isMouseDragging && Input.GetMouseButton(0))
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

        if (isMouseDragging && !Input.GetMouseButton(0))
        {
            isMouseDragging = false;
        }
#else
        
        if (!PopupSystem.IsAnyPopupOpen && !PanelManager.IsAnyPanelOpen && !Disable && Input.touchCount > 0)
        {
            Touch touch = Input.touches[0];

            if (Input.touchCount > 1)
            {
                // pinch zoom
                Touch secondTouch = Input.touches[1];

                if (secondTouch.phase == TouchPhase.Began)
                {
                    lastDragDiffs.Clear();
                }

                // figure out the gesture movement over the last series of frames
                if (lastDragDiffs.Count > 9) lastDragDiffs.Dequeue();
                Vector2 lastDiff = (touch.position - touch.deltaPosition) - (secondTouch.position - secondTouch.deltaPosition);
                Vector2 currDiff = touch.position - secondTouch.position;
                lastDragDiffs.Enqueue(currDiff.sqrMagnitude - lastDiff.sqrMagnitude);

                float avgDiff = 0;
                foreach (float diff in lastDragDiffs)
                {
                    avgDiff += diff;
                }
                avgDiff /= lastDragDiffs.Count;

                float zoomDiff = avgDiff * zoomTouchSpeedFactor;

                zoomTarget = Mathf.Clamp(zoomDistance - zoomDiff, zoomMin, zoomMax);
            }
            else
            {
                // drag
                if (touch.phase == TouchPhase.Began)
                {
                    if (GameState.HasCurrentSpace 
                        && GameState.CurrentSpace.Camera.pixelRect.ContainsTouch(touch.position) 
                        && touch.position.x < MaxX 
                        && touch.position.y < Screen.height - GlobalUIRibbon.HEIGHT)
                    {
                        lastTouchDeltas.Clear();
                        mouseAxisSpin = Vector2.zero;
                    }
                }
                else if (touch.phase == TouchPhase.Ended 
                         || touch.phase == TouchPhase.Canceled 
                         || touch.position.x >= MaxX 
                         || touch.position.y >= Screen.height - GlobalUIRibbon.HEIGHT)
                {
                    // get the average deltas over the recorded frames
                    Vector2 avgDrag = Vector2.zero;
                    foreach (Vector2 delta in lastTouchDeltas)
                    {
                        avgDrag += delta;
                    }

                    lastTouchDeltas.Clear();

                    float velocity = Mathf.Clamp(avgDrag.magnitude, 0, 100);

                    mouseAxisSpin = avgDrag.normalized * velocity;
                }
                else if (touch.position.x < MaxX && touch.position.y < Screen.height - GlobalUIRibbon.HEIGHT)
                {
                    // record the last touch deltas
                    if (lastTouchDeltas.Count > 4) lastTouchDeltas.Dequeue();
                    lastTouchDeltas.Enqueue(touch.deltaPosition);

                    // Rotate camera based on mouse drag
                    xOrbit += touch.deltaPosition.x;
                    yOrbit -= touch.deltaPosition.y;
                }
            }
        }
        else
        {
            // Spin camera based on drag speed after release
            if (mouseAxisSpin.sqrMagnitude > 0.1f * flingSpeedFactor)
            {
                mouseAxisSpin = Vector2.Lerp(mouseAxisSpin, Vector2.zero, Time.deltaTime * 2);

                xOrbit += mouseAxisSpin.x * flingSpeedFactor;
                yOrbit -= mouseAxisSpin.y * flingSpeedFactor;
            }
            else
            {
                mouseAxisSpin = Vector2.zero;
            }
        }

        // Make sure we cant rotate upside down in the scene
        yOrbit = Mathf.Clamp(yOrbit, yOrbitMin, yOrbitMax);

        zoomDistance = Mathf.Lerp(zoomDistance, zoomTarget, Time.deltaTime * 5);

        Quaternion rotation = Quaternion.Euler(yOrbit, xOrbit, 0);
        transform.rotation = rotation;
        transform.position = target.position + rotation * new Vector3(0, 0, -zoomDistance);
#endif
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
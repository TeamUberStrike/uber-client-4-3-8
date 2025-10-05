//using UnityEngine;
//using System.Collections;

//public class ManualCameraRotationController : MonoBehaviour
//{
//    private Quaternion rotation = Quaternion.identity;
//    private Quaternion _editorSetCameraRotation;

//    private Transform _transform;
//    private Vector3 _defaultRotation;

//    private int _time;
//    private const int _maxRotationSpeed = 15;

//    private float _distance;
//    private float _x;
//    private float _y;
//    private float _rotationDirection;
//    private float _rotationForce;

//    private bool _rotate;

//    private Vector3 _originalOrbitCenterPosition = Vector3.zero;
//    private Vector3 _originalCameraPosition = Vector3.zero;
//    private Vector3 _correctedTargetPosition;
//    private ActivePage _activePage;

//    [SerializeField]
//    private Transform _orbitCenter;
//    [SerializeField]
//    private float _horizontalSpeed = 200f;
//    [SerializeField]
//    private float _verticalSpeed = 200f;
//    [SerializeField]
//    private float _zoomSpeed = 1000f;
//    [SerializeField]
//    private float _maxVertical = 20f;
//    [SerializeField]
//    private float _minVertical = -20f;
//    [SerializeField]
//    private bool _lockHorizontalRotation = false;
//    [SerializeField]
//    private float _maxHorizontal = 180f;
//    [SerializeField]
//    private float _minHorizontal = 0f;
//    [SerializeField]
//    private float _maxZoomFOV = 60f;
//    [SerializeField]
//    private float _minZoomFOV = 40f;
//    [SerializeField]
//    private bool _disableHorizontalMovement;
//    [SerializeField]
//    private bool _disableVerticalMovement;
//    [SerializeField]
//    private float _acceleratingSpeedForForceRotation = 100f;
//    [SerializeField]
//    private float _slowingSpeedForForceRotation = 0.5f;

//    void Awake()
//    {
//        _transform = transform;
//        _rotate = false;
//        //_guiOnTop = true;
//        SetCameraPosition();
//        _activePage = ActivePage.None;
//        _rotationDirection = 1f;
//        _rotationForce = 1f;
//        if (_orbitCenter != null)
//            _originalOrbitCenterPosition = _orbitCenter.transform.position;
//        _editorSetCameraRotation = _transform.rotation;

//        this.enabled = false;
//    }

//    // Use this for initialization
//    void Start()
//    {
//        _time = 0;
//    }

//    // Update is called once per frame
//    void Update()
//    {
//        if (_orbitCenter != null)
//        {
//            bool insideAllowedArea = CanUseRotation();
//            float fov = _transform.camera.fieldOfView;
//            switch (_activePage)
//            {

//                case ActivePage.Home:
//                case ActivePage.Shop:
//                    if (insideAllowedArea && CheckGUIonTop())
//                    {
//                        fov = fov - InputManager.GetValue(GameInputKey.NextWeapon) * _zoomSpeed * Time.deltaTime;
//                        fov = fov - InputManager.GetValue(GameInputKey.PrevWeapon) * _zoomSpeed * Time.deltaTime;
//                        fov = Mathf.Clamp(fov, _minZoomFOV, _maxZoomFOV);
//                        _transform.camera.fieldOfView = fov;
//                    }

//                    if (_disableHorizontalMovement)
//                    {
//                        _x = _transform.rotation.eulerAngles.y;
//                    }
//                    else
//                    {
//                        _x += InputManager.GetValue(GameInputKey.HorizontalLook) * _horizontalSpeed * Time.deltaTime;
//                        if (_lockHorizontalRotation)
//                            _x = ClampAngle(_x, _minHorizontal, _maxHorizontal);
//                    }

//                    if (_disableVerticalMovement)
//                    {
//                        _y = _transform.rotation.eulerAngles.x;
//                    }
//                    else
//                    {
//                        _y -= InputManager.GetValue(GameInputKey.VerticalLook) * _verticalSpeed * Time.deltaTime;
//                        _y = ClampAngle(_y, _minVertical, _maxVertical);
//                    }

//                    if (!_rotate && Input.GetMouseButtonDown(0) && insideAllowedArea && CheckGUIonTop())
//                    {
//                        _rotate = true;
//                        _x = _transform.rotation.eulerAngles.y;
//                        _y = _transform.rotation.eulerAngles.x;
//                    }

//                    if (Input.GetMouseButton(0) && _rotate && (!WidgetPanelGUI.Current || !WidgetPanelGUI.Current.IsBusy))
//                    {
//                        rotation = Quaternion.Euler(_y, _x, 0);
//                        Vector3 newRotationAxis = new Vector3(0, 0, -_distance * _distanceFactor);
//                        Vector3 position = rotation * newRotationAxis + _correctedTargetPosition;

//                        transform.rotation = rotation;
//                        transform.position = position;
//                        _time = 0;
//                    }
//                    else
//                    {
//                        _rotate = false;

//                        if (_transform.rotation.eulerAngles.y != _defaultRotation.y)
//                        {
//                            _x = Mathf.LerpAngle(_transform.rotation.eulerAngles.y, _defaultRotation.y, _time * Time.deltaTime);
//                            _y = Mathf.LerpAngle(_transform.rotation.eulerAngles.x, _defaultRotation.x, _time * Time.deltaTime);
//                            _time++;
//                            rotation = Quaternion.Euler(_y, _x, 0);
//                            Vector3 newRotationAxis = new Vector3(0, 0, -_distance * _distanceFactor);
//                            Vector3 position = rotation * newRotationAxis + _correctedTargetPosition;

//                            transform.rotation = rotation;
//                            transform.position = position;
//                        }
//                    }
//                    break;

//                case ActivePage.Stats:
//                    if (insideAllowedArea && CheckGUIonTop())
//                    {
//                        fov = fov - InputManager.GetValue(GameInputKey.NextWeapon) * _zoomSpeed * Time.deltaTime;
//                        fov = fov - InputManager.GetValue(GameInputKey.PrevWeapon) * _zoomSpeed * Time.deltaTime;
//                        fov = Mathf.Clamp(fov, _minZoomFOV, _maxZoomFOV);
//                        _transform.camera.fieldOfView = fov;
//                    }

//                    if (!_rotate && Input.GetMouseButtonDown(0) && insideAllowedArea && CheckGUIonTop())
//                    {
//                        _rotate = true;
//                        _x = _transform.rotation.eulerAngles.y;
//                        _y = _transform.rotation.eulerAngles.x;
//                    }
//                    // player controlling action
//                    if (Input.GetMouseButton(0) && _rotate)
//                    {

//                        float mouseInput = InputManager.GetValue(GameInputKey.HorizontalLook);

//                        if ((mouseInput > 0 && _rotationDirection < 0) || (mouseInput < 0 && _rotationDirection > 0))
//                            _rotationDirection = -_rotationDirection;

//                        if (mouseInput == 0) _rotationForce = 0;
//                        _rotationForce += _acceleratingSpeedForForceRotation * Time.deltaTime * Mathf.Abs(mouseInput);
//                        if (_rotationForce > _maxRotationSpeed)
//                            _rotationForce = _maxRotationSpeed;
//                        _x += (10f) * _rotationDirection * _horizontalSpeed * Time.deltaTime * Mathf.Abs(mouseInput);
//                        _y = _transform.rotation.eulerAngles.x;

//                        rotation = Quaternion.Euler(_y, _x, 0);
//                        Vector3 newRotationAxis = new Vector3(0, 0, -_distance * _distanceFactor);
//                        Vector3 position = rotation * newRotationAxis + _correctedTargetPosition;

//                        transform.rotation = rotation;
//                        transform.position = position;
//                        _time = 0;
//                    }
//                    // player not controlig action
//                    else
//                    {
//                        _rotate = false;

//                        if (_rotationForce != 1)
//                        {
//                            _rotationForce = Mathf.Lerp(_rotationForce, 1, _slowingSpeedForForceRotation * Time.deltaTime);
//                        }

//                        _x += _rotationForce * _rotationDirection * _horizontalSpeed * Time.deltaTime;
//                        _y = _transform.rotation.eulerAngles.x;
//                        rotation = Quaternion.Euler(_y, _x, 0);
//                        Vector3 newRotationAxis = new Vector3(0, 0, -_distance * _distanceFactor);
//                        Vector3 position = rotation * newRotationAxis + _correctedTargetPosition;

//                        transform.rotation = rotation;
//                        transform.position = position;
//                    }
//                    break;
//                default:
//                    Debug.LogError("menu page not set for manual camera rotation");
//                    break;
//            }
//        }
//    }

//    public float _distanceFactor = 0.3f;

//    private bool CanUseRotation()
//    {
//        bool result = false;
//        Rect testRec = new Rect();
//        Vector2 mousePos = Input.mousePosition;

//        switch (_activePage)
//        {
//            case ActivePage.None:
//                Debug.LogError("Active page for manual camera rotation is not set !");
//                break;
//            case ActivePage.Home:
//                testRec = new Rect(0, 55, Screen.width, Screen.height - 55);
//                break;
//            case ActivePage.Stats:
//                testRec = new Rect(0, 55, Screen.width / 2, Screen.height - 55);
//                break;
//            case ActivePage.Shop:
//                testRec = new Rect(0, 55, Screen.width - 580, Screen.height - 55);
//                break;
//        }

//        mousePos.y = Screen.height - mousePos.y;

//        if (testRec.Contains(mousePos) && !PanelManager.Instance.IsMouseInWidgets())
//            result = true;

//        return result;
//    }

//    public void ResetCameraPosition(DistanceAxis axis)
//    {
//        if (_orbitCenter != null)
//        {
//            _transform.position = _originalCameraPosition;
//            _correctedTargetPosition = _transform.position;
//            switch (axis)
//            {
//                case DistanceAxis.x:
//                    _distance = Mathf.Abs(_transform.position.x - _orbitCenter.position.x);
//                    _correctedTargetPosition.x = _orbitCenter.position.x;
//                    break;
//                case DistanceAxis.y:
//                    _distance = Mathf.Abs(_transform.position.y - _orbitCenter.position.y);
//                    _correctedTargetPosition.y = _orbitCenter.position.y;
//                    break;
//                case DistanceAxis.z:
//                    _distance = Mathf.Abs(_transform.position.z - _orbitCenter.position.z);
//                    _correctedTargetPosition.z = _orbitCenter.position.z;
//                    break;
//                case DistanceAxis.xz:
//                    _distance = Mathf.Sqrt(Mathf.Pow(Mathf.Abs(_transform.position.x - _orbitCenter.position.x), 2) + Mathf.Pow(Mathf.Abs(_transform.position.z - _orbitCenter.position.z), 2));
//                    _correctedTargetPosition.x = _orbitCenter.position.x;
//                    _correctedTargetPosition.z = _orbitCenter.position.z;
//                    break;
//            }
//            _transform.LookAt(_correctedTargetPosition);
//        }
//    }

//    public void SetCameraPosition()
//    {
//        _originalCameraPosition = _transform.position;
//    }

//    public void Enable(DistanceAxis axis, ActivePage page)
//    {
//        if (_orbitCenter != null)
//        {
//            _activePage = page;

//            if (_activePage == ActivePage.Stats)
//            {
//                _transform.camera.fieldOfView = ((_maxZoomFOV + _minZoomFOV) / 2f);
//                _orbitCenter.transform.position = _originalOrbitCenterPosition;
//            }

//            InputManager.Instance.IsInputEnabled = true;
//            _defaultRotation = _transform.rotation.eulerAngles;

//            if (page == ActivePage.Shop)
//            {
//                _defaultRotation.y = 172f;
//                _defaultRotation.x = 10f;
//                _transform.rotation = _editorSetCameraRotation;
//                _time = 0;
//            }

//            _correctedTargetPosition = _transform.position;
//            switch (axis)
//            {
//                case DistanceAxis.x:
//                    _distance = Mathf.Abs(_transform.position.x - _orbitCenter.position.x);
//                    _correctedTargetPosition.x = _orbitCenter.position.x;
//                    break;
//                case DistanceAxis.y:
//                    _distance = Mathf.Abs(_transform.position.y - _orbitCenter.position.y);
//                    _correctedTargetPosition.y = _orbitCenter.position.y;
//                    break;
//                case DistanceAxis.z:
//                    _distance = Mathf.Abs(_transform.position.z - _orbitCenter.position.z);
//                    _correctedTargetPosition.z = _orbitCenter.position.z;
//                    break;
//                case DistanceAxis.xz:
//                    _distance = Mathf.Sqrt(Mathf.Pow(Mathf.Abs(_transform.position.x - _orbitCenter.position.x), 2) + Mathf.Pow(Mathf.Abs(_transform.position.z - _orbitCenter.position.z), 2));
//                    _correctedTargetPosition.x = _orbitCenter.position.x;
//                    _correctedTargetPosition.z = _orbitCenter.position.z;
//                    break;
//            }

//            _y = _transform.rotation.eulerAngles.x;
//            _x = _transform.rotation.eulerAngles.y;

//            this.enabled = true;
//        }
//        else
//        {
//            enabled = false;
//            Debug.LogError("Orbit center in 'Manual Camera Rotation Controller' on '" + _transform.gameObject.name + "' has not been assigned !!!");
//        }
//    }

//    public void Disable()
//    {
//        InputManager.Instance.IsInputEnabled = false;
//        this.enabled = false;
//    }

//    private static float ClampAngle(float angle, float min, float max)
//    {
//        if (angle < -360)
//            angle += 360;
//        if (angle > 360)
//            angle -= 360;
//        return Mathf.Clamp(angle, min, max);
//    }

//    private void OnGUI()
//    {
//        GUI.depth = 1000;
//        switch (_activePage)
//        {
//            case ActivePage.Home:
//                if (GUI.Button(new Rect(0, 55, Screen.width, Screen.height - 55), string.Empty, GUIStyle.none))
//                {
//                }
//                break;
//            case ActivePage.Stats:
//                if (GUI.Button(new Rect(0, 55, Screen.width / 2, Screen.height - 55), string.Empty, GUIStyle.none))
//                {
//                }
//                break;
//            case ActivePage.Shop:
//                if (GUI.Button(new Rect(0, 55, Screen.width - 580, Screen.height - 55), string.Empty, GUIStyle.none))
//                {
//                }
//                break;
//        }
//    }

//    private bool CheckGUIonTop()
//    {
//        bool result = false;
//        if (PanelManager.IsInitialized && PopupSystem.IsInitialized)
//        {
//            result = (!PanelManager.IsAnyPanelOpen && !PopupSystem.IsAnyPopupOpen);
//        }
//        else
//        {
//            result = true;
//        }
//        return result;
//    }

//    public enum DistanceAxis
//    {
//        x,
//        y,
//        z,
//        xz,
//    }

//    public enum ActivePage
//    {
//        None,
//        Home,
//        Stats,
//        Shop,
//    }
//}

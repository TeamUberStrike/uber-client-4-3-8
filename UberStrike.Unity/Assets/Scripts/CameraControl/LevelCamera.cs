using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Cmune.Util;
using UnityEngine;

public partial class LevelCamera : MonoSingleton<LevelCamera>, IObserver
{
    private void Awake()
    {
        _transform = transform;

        _currentState = new NoneState();

        _bobManager = new CameraBobManager();

        _ccd = new CameraCollisionDetector();
        _ccd.Offset = 1;
        _ccd.LayerMask = 1;
#if !UNITY_ANDROID && !UNITY_IPHONE
        _lowpassFilter = gameObject.AddComponent<AudioLowPassFilter>();
        if (_lowpassFilter) _lowpassFilter.cutoffFrequency = 755;
#endif
    }

    private void LateUpdate()
    {
        if (_currentMode != CameraMode.SmoothFollow)
        {
            _currentState.Update();
        }
    }

    private void OnDrawGizmos()
    {
        if (_targetTransform != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(_targetTransform.position, 0.1f);
            Gizmos.color = Color.white;
        }
    }

    public void Notify()
    {
        if (_currentMode == CameraMode.SmoothFollow)
        {
            _currentState.Update();
        }
    }

    private void InitUserInput()
    {
        Vector3 angles = UserInput.Rotation.eulerAngles;
        _userInputCache = UserInput.Rotation;

        angles.x = Mathf.Clamp(angles.x, 0, 60);

        _userInputRotation = Quaternion.Euler(angles);
    }

    private void UpdateUserInput()
    {
        Vector3 angles = UserInput.Rotation.eulerAngles;

        float oldX = _userInputCache.eulerAngles.x;
        float newX = UserInput.Rotation.eulerAngles.x;

        if (oldX > 180) oldX = oldX - 360;
        if (newX > 180) newX = newX - 360;

        angles.x = Mathf.Clamp(_userInputRotation.eulerAngles.x + (newX - oldX), 0, 60);

        _userInputCache = UserInput.Rotation;
        _userInputRotation = Quaternion.Euler(angles);
    }

    private void TransformFollowCamera(Vector3 targetPosition, Quaternion targetRotation, float distance, ref float collideDistance)
    {
        // calculate ideal position in follow target's local coordinates
        Vector3 positionInTargetSpace = _userInputRotation * Vector3.back * collideDistance;

        // calculate the matrix to transform from target space to world space
        Matrix4x4 targetToWorldMatrix = Matrix4x4.TRS(targetPosition, targetRotation, Vector3.one);

        Vector3 positionInWorldSpace = targetToWorldMatrix.MultiplyPoint3x4(positionInTargetSpace);
        Quaternion rotationInWorldSpace = Quaternion.LookRotation(targetPosition - positionInWorldSpace);

        Vector3 detectPosition = targetToWorldMatrix.MultiplyPoint3x4(_userInputRotation * Vector3.back * distance);

        if (_ccd.Detect(targetPosition, detectPosition, rotationInWorldSpace * Vector3.right))
        {
            float d = _ccd.Distance;

            if (d < collideDistance)
                collideDistance = Mathf.Clamp(d, 1, distance);
            else
                collideDistance = Mathf.Lerp(collideDistance, d, Time.deltaTime * 3);
        }
        else if (!Mathf.Approximately(collideDistance, distance))
        {
            collideDistance = Mathf.Lerp(collideDistance, distance, Time.deltaTime * 5);
        }
        else
        {
            collideDistance = distance;
        }

        _transform.position = positionInWorldSpace;
        _transform.rotation = rotationInWorldSpace;
    }

    private void TransformDeathCamera(Vector3 targetPosition, Quaternion targetRotation, float distance, ref float collideDistance)
    {
        // calculate ideal position in follow target's local coordinates
        Vector3 positionInTargetSpace = Vector3.back * collideDistance;

        // calculate the matrix to transform from target space to world space
        Matrix4x4 targetToWorldMatrix = Matrix4x4.TRS(targetPosition, targetRotation, Vector3.one);

        Vector3 positionInWorldSpace = targetToWorldMatrix.MultiplyPoint3x4(positionInTargetSpace);
        Quaternion rotationInWorldSpace = Quaternion.LookRotation(targetPosition - positionInWorldSpace);

        Vector3 detectPosition = targetToWorldMatrix.MultiplyPoint3x4(Vector3.back * distance);

        if (_ccd.Detect(targetPosition, detectPosition, rotationInWorldSpace * Vector3.right))
        {
            float d = _ccd.Distance;

            if (d < collideDistance)
                collideDistance = Mathf.Clamp(d, 1, distance);
            else
                collideDistance = Mathf.Lerp(collideDistance, d, Time.deltaTime * 3);
        }
        else if (!Mathf.Approximately(collideDistance, distance))
        {
            collideDistance = Mathf.Lerp(collideDistance, distance, Time.deltaTime * 5);
        }
        else
        {
            collideDistance = distance;
        }

        _transform.position = positionInWorldSpace;
        _transform.rotation = rotationInWorldSpace;
    }

    #region Camera Interface

    public void SetTarget(Transform target)
    {
        _targetTransform = target;
    }

    public void SetLevelCamera(Camera camera, Vector3 position, Quaternion rotation)
    {
        if (camera != MainCamera && camera != null)
        {
            if (MainCamera != null)
            {
                ResetCamera(MainCamera, _cameraConfiguration);
            }

            _cameraConfiguration.Parent = camera.transform.parent;
            _cameraConfiguration.Fov = camera.fov;
            _cameraConfiguration.CullingMask = camera.cullingMask;

            MainCamera = camera;
            ReparentCamera(camera, transform);

            _zoomData.TargetFOV = camera.fov;
            _transform.position = position;
            _transform.rotation = rotation;
        }
    }

    public void ReleaseCamera()
    {
        if (MainCamera != null)
        {
            ResetCamera(MainCamera, _cameraConfiguration);
            MainCamera = null;
        }
    }

    private void ResetCamera(Camera camera, CameraConfiguration config)
    {
        camera.fov = config.Fov;
        camera.cullingMask = config.CullingMask;
        ReparentCamera(camera, config.Parent);
    }

    private void ReparentCamera(Camera camera, Transform parent)
    {
        camera.transform.parent = parent;
        camera.transform.localPosition = Vector3.zero;
        camera.transform.localRotation = Quaternion.identity;
    }

    public void SetMode(CameraMode mode)
    {
        if (mode == _currentMode)
        {
            return;
        }

        _feedback.timeToEnd = 0;
        _currentMode = mode;

        _currentState.Finish();

        if (MainCamera != null)
        {
            switch (mode)
            {
                case CameraMode.FirstPerson:
                    MainCamera.cullingMask = LayerUtil.RemoveFromLayerMask(MainCamera.cullingMask, UberstrikeLayer.LocalPlayer, UberstrikeLayer.Weapons);
                    MainCamera.cullingMask = LayerUtil.AddToLayerMask(MainCamera.cullingMask, UberstrikeLayer.RemoteProjectile);
                    _currentState = new FirstPersonState();
                    break;

                case CameraMode.ThirdPerson:
                    MainCamera.cullingMask = LayerUtil.AddToLayerMask(MainCamera.cullingMask, UberstrikeLayer.LocalPlayer, UberstrikeLayer.Weapons);
                    _currentState = new ThirdPersonState();
                    break;

                case CameraMode.SmoothFollow:
                    MainCamera.cullingMask = LayerUtil.AddToLayerMask(MainCamera.cullingMask, UberstrikeLayer.LocalPlayer);
                    _currentState = new SmoothFollowState();
                    break;

                case CameraMode.OrbitAround:
                    MainCamera.cullingMask = LayerUtil.AddToLayerMask(MainCamera.cullingMask, UberstrikeLayer.LocalPlayer);
                    _currentState = new OrbitAroundState();
                    break;

                case CameraMode.Ragdoll:
                    MainCamera.cullingMask = LayerUtil.AddToLayerMask(MainCamera.cullingMask, UberstrikeLayer.LocalPlayer);
                    _currentState = new RagdollState();
                    break;

                case CameraMode.Spectator:
                    MainCamera.cullingMask = LayerUtil.AddToLayerMask(MainCamera.cullingMask, UberstrikeLayer.LocalPlayer);
                    _currentState = new SpectatorState();
                    break;

                case CameraMode.Death:
                    _currentState = new DeadState();
                    break;

                case CameraMode.FollowTurret:
                    _currentState = new TurretState();
                    break;

                case CameraMode.None:
                    _currentState = new NoneState();
                    break;

                case CameraMode.Overview:
                    _currentState = new OverviewState();
                    break;

                default:
                    Debug.LogError("Camera does not support " + mode.ToString());
                    break;
            }
        }
        //else
        //{
        //    CmuneDebug.LogError("No Camera set");
        //}
    }

    public void SetEyePosition(float x, float y, float z)
    {
        _eyePosition = new Vector3(x, y, z);
    }

    public void SetLookAtHeight(float height)
    {
        _height = height;
    }

    public void SetOrbitDistance(float distance)
    {
        _orbitDistance = distance;
    }

    public void SetOrbitSpeed(float speed)
    {
        _orbitSpeed = speed;
    }

    public static void SetBobMode(BobMode mode)
    {
        if (Exists)
        {
            Instance._bobManager.Mode = mode;

            if (WeaponFeedbackManager.Exists)
                WeaponFeedbackManager.Instance.SetBobMode(mode);
        }
    }

    public void DoFeedback(FeedbackType type, Vector3 direction, float strength, float noise, float timeToPeak, float timeToEnd, float angle, Vector3 axis)
    {
        _feedback.time = 0;
        _feedback.noise = noise / 4f;
        _feedback.strength = strength;
        _feedback.timeToPeak = timeToPeak;
        _feedback.timeToEnd = timeToEnd;
        _feedback.direction = direction;
        _feedback.angle = angle;
        _feedback.rotationAxis = axis;
    }

    public bool DoLandFeedback(bool shake)
    {
        if (_currentMode == CameraMode.FirstPerson && CanDip &&
            (_feedback.time == 0 || _feedback.time >= _feedback.Duration))
        {
            _feedback.time = 0;
            _feedback.angle = JumpFeedback.angle;
            _feedback.noise = shake ? JumpFeedback.noise : 0;
            _feedback.strength = JumpFeedback.strength;
            _feedback.timeToPeak = JumpFeedback.timeToPeak;
            _feedback.timeToEnd = JumpFeedback.timeToEnd;
            _feedback.direction = Vector3.down;
            _feedback.rotationAxis = Vector3.right;

            WeaponFeedbackManager.Instance.LandingDip();

            return true;
        }
        else
        {
            return false;
        }
    }

    public void DoZoomIn(float fov, float speed)
    {
        if (fov < 1 || fov > 100 || speed < 0.001f || speed > 100)
        {
            Debug.LogError("Invalid parameters specified!\n FOV should be >1 & <100, Speed should be >0.001 & <100.\n" + "FOV = " + fov + " Speed = " + speed);
            return;
        }

        if (_isZoomedIn && fov == FOV) return;

        _zoomData.Speed = speed;
        _zoomData.TargetFOV = fov;
        _zoomData.TargetAlpha = 1;
        _isZoomedIn = true;
    }

    public bool IsZoomedIn
    {
        get { return _isZoomedIn; }
        set { _isZoomedIn = false; }
    }

    public void DoZoomOut(float fov, float speed)
    {
        if (fov < 1 || fov > 100 || speed < 0.001f || speed > 100)
        {
            Debug.LogError("Invalid parameters specified!\n FOV should be >1 & <100, Speed should be >0.001 & <100.\n" + "FOV = " + fov + " Speed = " + speed);
            return;
        }

        if (!_isZoomedIn) return;

        _zoomData.Speed = speed;
        _zoomData.TargetFOV = fov;
        _zoomData.TargetAlpha = 0;
        _isZoomedIn = false;

        CmuneEventHandler.Route(new OnCameraZoomOutEvent());
    }

    public void ResetZoom()
    {
        _isZoomedIn = false;

        _zoomData.ResetZoom();
    }

    public Ray ScreenPointToRay(Vector3 point)
    {
        if (MainCamera)
        {
            return MainCamera.ScreenPointToRay(point);
        }
        else
        {
            //CmuneDebug.LogWarning("ScreenPointToRay called but MainCamera is NULL");
            return new Ray();
        }
    }
#if !UNITY_ANDROID && !UNITY_IPHONE
    public void EnableLowPassFilter(bool enabled)
    {
        if (_lowpassFilter)
            _lowpassFilter.enabled = enabled;
    }
#endif

    public void ResetFeedback()
    {
        _feedback.Reset();
    }

    #endregion

    #region Properties

    public static bool HasCamera
    {
        get { return Exists && Instance.MainCamera != null; }
    }

    public Camera MainCamera
    {
        get { return _camera; }
        private set
        {
            _camera = value;
        }
    }

    public Transform TransformCache
    {
        get { return _transform; }
    }

    public float FOV
    {
        get { return MainCamera != null ? MainCamera.fov : 65; }
    }

    public Vector3 EyePosition
    {
        get { return _eyePosition; }
    }

    public float LookAtHeight
    {
        get { return _height; }
    }

    public float OrbitDistance
    {
        get { return _orbitDistance; }
    }

    public float OrbitSpeed
    {
        get { return _orbitSpeed; }
    }

    public CameraMode CurrentMode
    {
        get { return _currentMode; }
    }

    public BobMode CurrentBob
    {
        get { return _bobManager.Mode; }
    }
#if !UNITY_ANDROID && !UNITY_IPHONE
    public bool LowpassFilterEnabled
    {
        get { return _lowpassFilter && _lowpassFilter.enabled; }
    }
#endif
    public bool CanDip
    {
        get;
        set;
    }

    #endregion

    #region Fields

    private CameraConfiguration _cameraConfiguration = new CameraConfiguration();

    public FeedbackData JumpFeedback;

    private Camera _camera;
    private Feedback _feedback;
    private CameraBobManager _bobManager;
    private CameraCollisionDetector _ccd;
    private ZoomData _zoomData;
    private CameraState _currentState;

    private float _height;
    private float _orbitDistance;
    private float _orbitSpeed;
    private bool _isZoomedIn;

    private Vector3 _eyePosition;
    private Transform _transform;
    private Transform _targetTransform;
    private CameraMode _currentMode = CameraMode.None;

    private Quaternion _userInputCache;
    private Quaternion _userInputRotation;
#if !UNITY_ANDROID && !UNITY_IPHONE
    private AudioLowPassFilter _lowpassFilter;
#endif

    #endregion

    #region Helper Classes

    private class CameraConfiguration
    {
        public Transform Parent;
        public int CullingMask;
        public float Fov;
    }

    private class CameraBobManager
    {
        public CameraBobManager()
        {
            _bobData = new Dictionary<BobMode, BobData>();

            //initialize bobdata for all possible states
            foreach (BobMode b in Enum.GetValues(typeof(BobMode)))
            {
                switch (b)
                {
                    case BobMode.Idle: _bobData[b] = new BobData(0.2f, 0.0f, 2.0f); break;
                    case BobMode.Crouch: _bobData[b] = new BobData(0.8f, 0.8f, 12.0f); break;
                    case BobMode.Run: _bobData[b] = new BobData(0.5f, 0.3f, 8.0f); break;
                    case BobMode.Walk: _bobData[b] = new BobData(0.3f, 0.3f, 6.0f); break;
                    default: _bobData[b] = new BobData(0.0f, 0.0f, 0.0f); break;
                }
            }

            _data = _bobData[BobMode.Idle];
        }

        public void Update()
        {
            Transform t = Instance._transform;

            switch (_bobMode)
            {
                case BobMode.Idle:
                    {
                        float bobAngle = Mathf.Sin(Time.time * _data.Frequency);
                        t.rotation = Quaternion.AngleAxis(bobAngle * _data.XAmplitude * _strength, t.right) * Quaternion.AngleAxis(bobAngle * _data.ZAmplitude, t.forward) * t.rotation;
                    } break;
                case BobMode.Walk:
                    {
                        float bobAngle = Mathf.Sin(Time.time * _data.Frequency);
                        t.rotation = Quaternion.AngleAxis(Mathf.Abs(bobAngle * _data.XAmplitude), t.right) * Quaternion.AngleAxis(bobAngle * _data.ZAmplitude, t.forward) * t.rotation;
                    } break;
                case BobMode.Run:
                    {
                        float bobAngle = Mathf.Sin(Time.time * _data.Frequency);
                        t.rotation = Quaternion.AngleAxis(Mathf.Abs(bobAngle * _data.XAmplitude * _strength), t.right) * Quaternion.AngleAxis(bobAngle * _data.ZAmplitude, t.forward) * t.rotation;
                    } break;
                case BobMode.Swim:
                    {
                        float bobAngle = Mathf.Sin(Time.time * _data.Frequency) * _data.ZAmplitude;
                        t.rotation = Quaternion.AngleAxis(bobAngle, t.forward) * t.rotation;
                    } break;
                case BobMode.Fly:
                    {
                        float bobAngle = Mathf.Sin(Time.time * _data.Frequency) * _data.ZAmplitude;
                        t.rotation = Quaternion.AngleAxis(bobAngle, t.forward) * t.rotation;
                    } break;
                case BobMode.Crouch:
                    {
                        float bobAngle = Mathf.Sin(Time.time * _data.Frequency);
                        t.rotation = Quaternion.AngleAxis(Mathf.Abs(bobAngle * _data.XAmplitude), t.right) * Quaternion.AngleAxis(bobAngle * _data.ZAmplitude, t.forward) * t.rotation;
                    } break;
            }

            _strength = Mathf.Clamp01(_strength + Time.deltaTime);
        }

        public BobMode Mode
        {
            get { return _bobMode; }
            set
            {
                if (_bobMode != value)
                {
                    _strength = 0;
                    _bobMode = value;
                    _data = _bobData[value];
                }
            }
        }

        #region Fields

        private float _strength;

        private BobData _data;
        private BobMode _bobMode;

        private readonly Dictionary<BobMode, BobData> _bobData;

        #endregion

        private struct BobData
        {
            private float _xAmplitude;
            private float _zAmplitude;
            private float _frequency;

            public float XAmplitude { get { return _xAmplitude; } }
            public float ZAmplitude { get { return _zAmplitude; } }
            public float Frequency { get { return _frequency; } }

            public BobData(float xamp, float zamp, float freq)
            {
                _xAmplitude = xamp;
                _zAmplitude = zamp;
                _frequency = freq;
            }
        }
    }

    [System.Serializable]
    public class FeedbackData
    {
        public float timeToPeak;
        public float timeToEnd;
        public float noise;
        public float angle;
        public float strength;
    }

    private struct Feedback
    {
        public float time;
        public float noise;
        public float angle;
        public float timeToPeak;
        public float timeToEnd;
        public float strength;

        public Vector3 direction;
        public Vector3 rotationAxis;

        private float _angle;
        private float _currentNoise;

        public float DebugAngle
        {
            get { return _angle; }
        }

        public float Duration
        {
            get { return timeToPeak + timeToEnd; }
        }

        Vector3 shakePos;

        public void HandleFeedback()
        {
            if (Duration == 0)
                return;

            float force = 0;
            float n = UnityEngine.Random.Range(-noise, noise);

            if (time < Duration)
            {
                if (time < timeToPeak)
                {
                    force = strength * Mathf.Sin(time * Mathf.PI * 0.5f / timeToPeak);
                    _angle = Mathf.Lerp(0, angle, time / timeToPeak);
                }
                else
                {
                    float t = (time - timeToPeak) / timeToEnd;

                    force = strength * Mathf.Cos((time - timeToPeak) * Mathf.PI * 0.5f / timeToEnd);
                    _angle = Mathf.Lerp(_angle, 0, t);

                    if (time != 0) n = 0;
                }

                _currentNoise = Mathf.Lerp(noise, 0, time / Duration);

                shakePos = Vector3.Lerp(shakePos, UnityEngine.Random.insideUnitSphere * _currentNoise, Time.deltaTime * 30);

                time += Time.deltaTime;

                Instance._transform.position += (force * direction);// + new Vector3(shakePos.x, shakePos.y, 0));
                Instance._transform.rotation = Instance._transform.rotation * Quaternion.AngleAxis(_angle, rotationAxis) * Quaternion.AngleAxis(n, Instance._transform.forward);
            }
            else
            {
                time = 0;
                timeToEnd = 0;
                timeToPeak = 0;

                _angle = 0;
            }
        }

        public void Reset()
        {
            _angle = 0;

            time = 0;
            timeToEnd = 0;
            timeToPeak = 0;
        }
    }

    private struct ZoomData
    {
        public float TargetAlpha;
        public float TargetFOV;
        public float Speed;

        private float _alpha;

        public bool IsFovChanged
        {
            get { return TargetFOV != Instance.FOV; }
        }

        public void Update()
        {
            _alpha = Mathf.Lerp(_alpha, TargetAlpha, Time.deltaTime * Speed);

            if (Instance.MainCamera)
            {
                Instance.MainCamera.fov = Mathf.Lerp(Instance.MainCamera.fov, TargetFOV, Time.deltaTime * Speed);
            }
        }

        public void ResetZoom()
        {
            if (Instance.MainCamera)
            {
                TargetFOV = 60;
                Instance.MainCamera.fov = TargetFOV;
            }
        }
    }

    #endregion

    #region Enums

    public enum CameraMode
    {
        None = 0,
        Spectator = 1,
        OrbitAround = 2,
        FirstPerson = 3,
        ThirdPerson = 4,
        SmoothFollow = 5,
        FollowTurret = 6,
        Ragdoll = 7,
        Death = 8,
        Overview = 9
    }

    public enum FeedbackType
    {
        JumpLand,
        GetDamage,
        ShootWeapon,
    }

    #endregion
}

public enum BobMode
{
    None,
    Idle,
    Walk,
    Run,
    Fly,
    Swim,
    Crouch
}
using Cmune.Realtime.Common;
using UnityEngine;

[RequireComponent(typeof(Camera))]

/// <summary>
/// This class handles the various camera shake effects when 
/// player quickly moves, lands, gets damage, and so on.
/// </summary>
public class CameraFeedback : MonoBehaviour
{
    public bool DEBUG = true;

    private static CameraFeedback _instance;
    public static CameraFeedback Instance
    {
        get { return _instance; }
    }

    public enum FeedbackType
    {
        Land, Damage, Weapon
    }

    [System.Serializable]
    public class Feedback
    {
        public FeedbackType Type;
        public float Peak;
        public float TimeToPeak;
        public float TimeToEnd;
        public float MaxNoise;
        public float MaxAngle;

        private Vector3 _dir;

        public Feedback(Feedback f)
        {
            _dir = Vector3.zero;

            Type = f.Type;
            Peak = f.Peak;
            TimeToPeak = f.TimeToPeak;
            TimeToEnd = f.TimeToEnd;
        }

        public void SetDirection(Vector3 dir)
        {
            _dir = dir;
        }

        public Vector3 GetDirection()
        {
            return _dir;
        }
    }

    public Feedback[] Feedbacks;

    private Feedback _currentFeedback;
    private Transform _transformCache;
    private Quaternion _tmpRotation;
    private Vector3 _rotationAxis;
    private float _timer;
	private float _angle;

    #region Unity

    void Awake()
    {
        _instance = this;
        _transformCache = transform;
        _currentFeedback = null;
        _tmpRotation = Quaternion.identity;
    }

    void Update()
    {
        // Apply the feedback if there exists one
        if (_currentFeedback == null)
        {
            if (_transformCache.localPosition.sqrMagnitude > 0.001f)
            {
                _transformCache.localPosition = Vector3.Lerp(_transformCache.localPosition, Vector3.zero, Time.deltaTime);
                _transformCache.localRotation = Quaternion.Lerp(_transformCache.localRotation, Quaternion.identity, Time.deltaTime);
            }
        }
        else
        {
            Vector3 pos = _currentFeedback.GetDirection();
            float peak = _currentFeedback.Peak;
            float noise = Random.Range(-_currentFeedback.MaxNoise, _currentFeedback.MaxNoise);
            //float angle = _currentFeedback.MaxAngle;
            
            if (_timer < _currentFeedback.TimeToEnd + _currentFeedback.TimeToPeak)
            {
                if (_timer < _currentFeedback.TimeToPeak)
                {
                    peak *= Mathf.Sin(_timer * Mathf.PI * 0.5f / _currentFeedback.TimeToPeak);
                    noise = Mathf.Lerp(noise, 0, _timer / _currentFeedback.TimeToPeak);
                    _angle = Mathf.Lerp(0, _currentFeedback.MaxAngle, _timer / _currentFeedback.TimeToPeak);
                }
                else
                {
                    float t = (_timer - _currentFeedback.TimeToPeak) / _currentFeedback.TimeToEnd;
                    peak = Mathf.Lerp(peak, 0, t);
                    _angle = Mathf.Lerp(_angle, 0, t);
                    noise = 0;
                }

                _timer += Time.deltaTime;
                _transformCache.localPosition = peak * pos + _transformCache.right * noise + _transformCache.up * noise;
                _tmpRotation = Quaternion.AngleAxis(_angle, _rotationAxis);

                _testAngle = _angle;
            }
            else
            {
                _timer = 0;
                _tmpRotation = Quaternion.identity;
                _currentFeedback = null;
            }
        }
    }

    float _testAngle = 0;

    void OnGUI()
    {
        if (DEBUG) DoApplyFeedback();
    }

    void DoApplyFeedback()
    {
        GUI.Label(new Rect(10, 50, 300, 20), "Camera local position = " + _transformCache.localPosition);
        GUI.Label(new Rect(10, 60, 300, 20), "Camera world position = " + _transformCache.position);
        GUI.Label(new Rect(10, 70, 300, 20), "Rotation Axis = " + _rotationAxis);
        GUI.Label(new Rect(10, 80, 300, 20), "Rotation Angle = " + _testAngle);

        if (GUI.Button(new Rect(10, 100, 60, 25), "Land"))
        {
            onPlayerLand(new PlayerLandEvent());
        }

        if (GUI.Button(new Rect(80, 100, 60, 25), "Damage"))
        {
            onDamage(new GetDamageEvent(Vector3.back));
        }

        if (GUI.Button(new Rect(150, 100, 60, 25), "Weapon"))
        {
            onWeaponShoot(new WeaponShootEvent(Vector3.back, Feedbacks[(int)FeedbackType.Weapon].MaxNoise, Feedbacks[(int)FeedbackType.Weapon].MaxAngle));
        }

        Vector3[] dmgs = { new Vector3(-0.8f, -0.3f, 0.6f), new Vector3(-0.8f, -0.1f, 0.6f), new Vector3(0.5f, -0.7f, 0.5f) };
        for (int i = 0; i < dmgs.Length; i++)
        {
            if (GUI.Button(new Rect(10, 125 + 25 * i, 100, 25), "Damage " + i))
            {
                onDamage(new GetDamageEvent(dmgs[i]));
            }
        }
    }

    void OnDrawGizmos()
    {
        if (!_transformCache) _transformCache = transform;

        Gizmos.color = Color.green;
        Gizmos.DrawRay(_transformCache.position, Feedbacks[(int)FeedbackType.Damage].GetDirection());
    }

    #endregion

    #region Event Handlers

    void onPlayerLand(PlayerLandEvent ev)
    {
        ApplyFeedback(FeedbackType.Land, Vector3.down, Vector3.right);
    }

    void onDamage(GetDamageEvent ev)
    {
        ApplyFeedback(FeedbackType.Damage, ev.Force, Vector3.zero);
    }

    void onWeaponShoot(WeaponShootEvent ev)
    {
        Feedbacks[(int)FeedbackType.Weapon].Peak = 1;
        Feedbacks[(int)FeedbackType.Weapon].MaxNoise = ev.Noise;
        Feedbacks[(int)FeedbackType.Weapon].MaxAngle = ev.Angle;

        ApplyFeedback(FeedbackType.Weapon, ev.Force, Vector3.left);
    }

    #endregion

    public void ApplyFeedback(FeedbackType t, Vector3 dir, Vector3 rotAxis)
    {
        _timer = 0;

        _currentFeedback = Feedbacks[(int)t];

        _currentFeedback.SetDirection(dir);

        _rotationAxis = (rotAxis == Vector3.zero) ? _transformCache.InverseTransformDirection(Vector3.Cross(Vector3.up, dir)) : rotAxis;
    }

    public void ApplyFeedback(Vector3 dir, float noise, float angle)
    {
        Feedbacks[(int)FeedbackType.Weapon].Peak = 1;
        Feedbacks[(int)FeedbackType.Weapon].MaxNoise = noise;
        Feedbacks[(int)FeedbackType.Weapon].MaxAngle = angle;

        ApplyFeedback(FeedbackType.Weapon, dir, Vector3.left);
    }

    public Quaternion GetFeedbackRoation()
    {
        return _tmpRotation;
    }
}

public class PlayerLandEvent//
{
}

public class WeaponShootEvent//
{
    public Vector3 Force;
    public float Noise;
    public float Angle;

    public WeaponShootEvent(Vector3 force, float noise, float angle)
    {
        Force = force;
        Noise = noise;
        Angle = angle;
    }
}

public class GetDamageEvent//
{
    public Vector3 Force;

    public GetDamageEvent(Vector3 force)
    {
        Force = force;
    }
}

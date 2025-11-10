using UnityEngine;

[RequireComponent(typeof(Camera))]
public class WeaponCamera : MonoBehaviour
{
    [SerializeField]
    private float _maxDisplacementDelta = 0.4f;

    [SerializeField]
    private float _maxDisplacement = 0.8f;

    private Transform _transform;
    public Vector2 _currentAngle = Vector2.zero;

    private const float RESET_VELOCITY = 5;
    private const float LERP_DURATION = 0.1f;

    void Awake()
    {
        _transform = transform;
    }

    public void SetCameraEnabled(bool enabled)
    {
        camera.enabled = enabled;
    }

    void LateUpdate()
    {
        if (WeaponFeedbackManager.Exists)
        {
            if (WeaponFeedbackManager.Instance.IsIronSighted)
            {
                _currentAngle = Vector2.Lerp(_currentAngle, Vector2.zero, Time.deltaTime * RESET_VELOCITY);

                MoveWeapon();
            }
            else
            {
                float deltaX = InputManager.Instance.GetValue(GameInputKey.HorizontalLook);
                float deltaY = InputManager.Instance.GetValue(GameInputKey.VerticalLook);

                AddDeltaAngle(deltaX, deltaY);

                MoveWeapon();

                _currentAngle = Vector2.Lerp(_currentAngle, Vector2.zero, Time.deltaTime * RESET_VELOCITY);
            }
        }
    }

    private void AddDeltaAngle(float x, float y)
    {
        Vector2 angle = Vector2.ClampMagnitude(new Vector2(x, y), _maxDisplacementDelta);

        _currentAngle = Vector2.ClampMagnitude(_currentAngle + angle, _maxDisplacement);
    }

    private void MoveWeapon()
    {
        _transform.localRotation = Quaternion.AngleAxis(_currentAngle.x, Vector3.up) * Quaternion.AngleAxis(-_currentAngle.y, Vector3.right);
    }
}
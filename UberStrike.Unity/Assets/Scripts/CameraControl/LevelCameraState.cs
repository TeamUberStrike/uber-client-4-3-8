using UnityEngine;

public partial class LevelCamera
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class CameraState
    {
        /// Calculate the desired position to look at the target.
        /// <param name="target">The target to look at</param>
        /// <param name="rotation">The rotation represents the horizontal and vertical angles</param>
        /// <param name="distance">The distance to the target</param>
        /// <param name="height">The height offset to the target</param>
        /// <returns></returns>
        protected Vector3 LookAtPosition(Transform target, Quaternion lookRot, Quaternion xRot, Quaternion yRot, float distance, float height)
        {
            Vector3 targetSpacePosition = lookRot * Vector3.back * distance;

            return target.up * Instance.LookAtHeight + target.TransformPoint(targetSpacePosition);
        }

        protected Quaternion LookAtRotation(Transform target, Quaternion rotation)
        {
            Vector3 targetSpaceDirection = rotation * Vector3.forward;

            return Quaternion.LookRotation(target.TransformDirection(targetSpaceDirection));
        }

        public abstract void Update();

        public virtual void Finish() { }

        public virtual void OnDrawGizmos() { }

        public override string ToString()
        {
            return "Abstract state";
        }
    }

    /// <summary>
    /// 
    /// </summary>
    private class NoneState : CameraState
    {
        public NoneState() { }

        public override void Update() { }
    }

    /// <summary>
    /// 
    /// </summary>
    private class FirstPersonState : CameraState
    {
        //private float _time = 0;
        private const float _duration = 1;

        bool _handleFeedback = true;

        public FirstPersonState()
        {
            //_time = 0;
        }

        public override void Update()
        {
            Vector3 targetPos = Instance._targetTransform.position + Instance.EyePosition;

            //if (_time < _duration)
            //{
            //    Instance._transform.position = Vector3.Lerp(Instance._transform.position, targetPos, Time.deltaTime * 10);
            //    Instance._transform.rotation = Quaternion.Lerp(Instance._transform.rotation, Instance._targetTransform.rotation, Time.deltaTime * 10);

            //    _time += Time.deltaTime;
            //}
            //else
            //{
            Instance._transform.position = targetPos;
            Instance._transform.rotation = Instance._targetTransform.rotation;

            if (_handleFeedback)
            {
                Instance._feedback.HandleFeedback();
                Instance._bobManager.Update();
            }

            if (Instance._zoomData.IsFovChanged)
            {
                Instance._zoomData.Update();
            }
        }

        public override void Finish()
        {
            Instance._zoomData.ResetZoom();
        }

        public override void OnDrawGizmos()
        {
            Gizmos.DrawRay(Instance._transform.position, Instance._transform.TransformDirection(Instance._feedback.rotationAxis));
        }

        public override string ToString()
        {
            return "FPS state";
        }
    }

    /// <summary>
    /// 
    /// </summary>
    private class ThirdPersonState : CameraState
    {
        private float _right = 1;
        private float _collideDistance;
        private float _distance = 2.5f;

        private CameraCollisionDetector _ccd;

        private Vector3 TargetCheckPoint
        {
            //get { return Instance._targetTransform.position + Vector3.up * Instance.LookAtHeight; }
            get
            {
                //Vector3 angles = Quaternion.LookRotation(Instance._targetTransform.up).eulerAngles;
                //Quaternion rot = Quaternion.Euler(angles.x, 0, 0);
                return Instance._targetTransform.position + Instance._targetTransform.up * Instance.LookAtHeight;
            }
        }

        private Vector3 LeftCheckPoint
        {
            get { return Instance._transform.position - Instance._transform.right * _right; }
        }

        private Vector3 RightCheckPoint
        {
            get { return Instance._transform.position + Instance._transform.right * _right; }
        }

        public ThirdPersonState()
        {
            _collideDistance = _distance / 2;
            _ccd = new CameraCollisionDetector();
            _ccd.Offset = 1;
        }

        public override void Update()
        {
            TransformCamera();

            if (Instance._zoomData.IsFovChanged)
            {
                Instance._zoomData.Update();
            }
        }

        public override void OnDrawGizmos()
        {
            Gizmos.color = Color.red;

            Vector3 updir = Instance._targetTransform.up;

            if (Instance._targetTransform != null)
            {
                Gizmos.DrawWireSphere(TargetCheckPoint, 0.1f);

                Quaternion xRot = Quaternion.Euler(Instance._targetTransform.rotation.eulerAngles.x, 0, 0);
                Quaternion yRot = Quaternion.Euler(0, Instance._targetTransform.rotation.eulerAngles.y, 0);

                // first set the desired position to ideal position
                Vector3 to = LookAtPosition(Instance._targetTransform, Quaternion.identity, xRot, yRot, _distance, Instance.LookAtHeight);

                Gizmos.DrawLine(TargetCheckPoint, to - Instance._targetTransform.right);
                Gizmos.DrawLine(TargetCheckPoint, to + Instance._targetTransform.right);
            }

            _ccd.OnDrawGizmos();

            Gizmos.color = Color.red;
            Gizmos.DrawRay(Instance._targetTransform.position, Instance._targetTransform.right);
            Gizmos.color = Color.green;
            Gizmos.DrawRay(Instance._targetTransform.position, updir);
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(Instance._targetTransform.position, Instance._targetTransform.forward);
        }

        private void TransformCamera()
        {
            Vector3 angles = Instance._targetTransform.rotation.eulerAngles;

            if (angles.x > 90) angles.x = Mathf.Clamp(angles.x, 320, 360);
            else angles.x = Mathf.Clamp(angles.x, 0, 60);

            Quaternion xRot = Quaternion.Euler(angles.x, 0, 0);
            Quaternion yRot = Quaternion.Euler(0, angles.y, 0);

            // first set the desired position to ideal position
            Vector3 to = LookAtPosition(Instance._targetTransform, Quaternion.identity, xRot, yRot, _distance, Instance.LookAtHeight);

            // modify the position if there is a collider between target and camera
            if (_ccd.Detect(TargetCheckPoint, to, Instance._targetTransform.right))
            {
                float collidedDistance = _ccd.Distance;

                if (collidedDistance < _collideDistance)
                    _collideDistance = Mathf.Clamp(collidedDistance, 0.5f, _distance);
                else
                    _collideDistance = Mathf.Lerp(_collideDistance, collidedDistance, Time.deltaTime);
            }
            else
            {
                _collideDistance = Mathf.Lerp(_collideDistance, _distance, Time.deltaTime);
            }

            // calculate the final position of the camera
            Instance._transform.position = LookAtPosition(Instance._targetTransform, Quaternion.identity, xRot, yRot, _collideDistance, Instance.LookAtHeight);
            Instance._transform.rotation = Quaternion.LookRotation(TargetCheckPoint - Instance._transform.position);
        }

        public override string ToString()
        {
            return "3rd person state";
        }
    }

    /// <summary>
    /// 
    /// </summary>
    private class SmoothFollowState : CameraState
    {
        private float _collideDistance;
        private float _distance = 1.5f;

        private const float _zoomSpeed = 40;

        private Quaternion _targetRotationY = Quaternion.identity;

        private Vector3 TargetCheckPoint
        {
            get
            {
                return Instance._targetTransform.position + Instance._targetTransform.up * Instance.LookAtHeight;
            }
        }

        public SmoothFollowState()
        {
            _collideDistance = _distance / 2;

            Instance.InitUserInput();
        }

        public override void Update()
        {
            float v = InputManager.Instance.RawValue(GameInputKey.NextWeapon);
            if (v != 0)
            {
                _distance = _distance - Mathf.Sign(v) * _zoomSpeed * Time.deltaTime;
                _distance = Mathf.Clamp(_distance, 1, 4);
            }

            Vector3 angles = Instance._targetTransform.eulerAngles;

            _targetRotationY = Quaternion.Lerp(_targetRotationY, Quaternion.Euler(0, angles.y, 0), Time.deltaTime * 2);

            // follow target's world space position
            Vector3 targetPosition = Instance._targetTransform.position + Vector3.up * Instance.LookAtHeight;

            Instance.UpdateUserInput();
            Instance.TransformFollowCamera(targetPosition, _targetRotationY, _distance, ref _collideDistance);
        }

        public override string ToString()
        {
            return "Smooth follow state";
        }
    }

    /// <summary>
    /// 
    /// </summary>
    private class OrbitAroundState : CameraState
    {
        private float _distance;
        private float _angle;

        private CameraCollisionDetector _ccd;

        public OrbitAroundState()
        {
            _distance = 1;
            _angle = 0;
            _ccd = new CameraCollisionDetector();
            _ccd.Offset = 1;
        }

        public override void Update()
        {
            Quaternion yRot = Quaternion.Euler(0, _angle += Time.deltaTime * Instance.OrbitSpeed, 0);

            Vector3 from = Instance._targetTransform.position + Vector3.up * Instance.LookAtHeight;
            Vector3 to = LookAtPosition(Instance._targetTransform, Quaternion.identity, Quaternion.identity, yRot, 1, Instance.LookAtHeight);

            if (_ccd.Detect(from, to, Instance._transform.right))
            {
                if (_distance < _ccd.Distance)
                {
                    _distance = _ccd.Distance;
                }
                else
                {
                    _distance = Mathf.Lerp(_distance, _ccd.Distance, Time.deltaTime);
                }
            }
            else
            {
                _distance = Mathf.Lerp(_distance, Instance.OrbitDistance, Time.deltaTime);
            }

            Instance._transform.position = LookAtPosition(Instance._targetTransform, Quaternion.identity, Quaternion.identity, yRot, _distance, Instance.LookAtHeight);
            Instance._transform.rotation = Quaternion.LookRotation(from - Instance._transform.position);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    private class RagdollState : CameraState
    {
        private Vector3 _targetPosition;
        //private Quaternion _targetRotation;
        //private float _time;

        private const float MaxDuration = 1;
        private const float MinimalCameraHeight = -20;

        public RagdollState()
        {
            if (GameState.LocalDecorator != null && GameState.LocalDecorator.CurrentRagdoll != null)
                Instance.SetTarget(GameState.LocalDecorator.CurrentRagdoll.GetBone(BoneIndex.Hips));

            if (Instance._targetTransform != null)
            {
                _targetPosition = Instance._targetTransform.position;
            }
        }

        public override void Update()
        {
            if (Instance._targetTransform != null)
            {
                if (_targetPosition.y > MinimalCameraHeight && Mathf.Abs(_targetPosition.y - Instance._targetTransform.position.y) > 0.2f)
                {
                    _targetPosition = Instance._targetTransform.position;
                }

                Instance._transform.rotation = Quaternion.Slerp(Instance._transform.rotation, Quaternion.LookRotation(_targetPosition - Instance.transform.position), Time.deltaTime * 4);
                Instance._transform.position = Vector3.Lerp(Instance._transform.position, _targetPosition + new Vector3(0, 2, 0) - Instance._transform.forward * 3, Time.deltaTime * 4);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    private class SpectatorState : CameraState
    {
        private const int MaxSpeed = 22;
        private const float verticalSpeed = 0.8f;

        private Vector3 _targetPosition;
        private float _speed = MaxSpeed / 2;

        public SpectatorState()
        {
            Vector3 rotation = Instance._transform.rotation.eulerAngles;
            UserInput.SetRotation(rotation.y, rotation.x);

            _targetPosition = Instance._transform.position;
        }

        public override void Update()
        {
            if (!InGameChatHud.Instance.CanInput && Screen.lockCursor)
            {
                UserInput.UpdateDirections();

                int factor = UserInput.IsWalking ? 6 : 4;
                _speed = Mathf.Lerp(_speed, UserInput.IsWalking ? MaxSpeed : MaxSpeed / 2, Time.deltaTime);

                _targetPosition += (UserInput.Rotation * UserInput.HorizontalDirection + UserInput.VerticalDirection * verticalSpeed) * _speed * Time.deltaTime;
                Instance._transform.position = Vector3.Lerp(Instance._transform.position, _targetPosition, Time.deltaTime * factor);
                Instance._transform.rotation = UserInput.Rotation;
            }
        }
    }

    private class DeadState : CameraState
    {
        private bool _isFollowing;
        private float _distance = 1.5f;
        private const float _zoomSpeed = 40;

        private Vector3 TargetCheckPoint
        {
            get
            {
                return Instance._targetTransform.position + Instance._targetTransform.up * Instance.LookAtHeight;
            }
        }

        public DeadState()
        {
            //if (GameState.LocalPlayer.Killer != null)
            //{
            //    Transform target = GameState.LocalPlayer.Killer.GetBone(BoneIndex.Head);
            //}

            //LevelFXController.Instance.ImageEffectsManager.ApplyMotionBlur(5f, 0.8f);
        }

        public override void Update()
        {
            float LookAtHeight = 1f;

            if (Instance._targetTransform == null) return;

            Vector3 targetPosition = Instance._targetTransform.position + Vector3.up * LookAtHeight;

            Vector3 worldPosition = targetPosition + Vector3.Normalize(Instance._transform.position - targetPosition) * _distance;
            Quaternion worldRotation = Quaternion.LookRotation(targetPosition - Instance._transform.position);

            /* lerp to target first */
            if (!_isFollowing)
            {
                Instance._transform.position = Vector3.Lerp(Instance._transform.position, worldPosition, Time.deltaTime * 4);

                float distance = Vector3.Distance(worldPosition, Instance._transform.position);
                if (distance <= _distance)
                {
                    _isFollowing = true;
                }
            }

            Instance._transform.rotation = Quaternion.Lerp(Instance._transform.rotation, worldRotation, Time.deltaTime * 4);
        }

        public override string ToString()
        {
            return "Smooth follow state";
        }
    }

    private class TurretState : CameraState
    {
        public override void Update()
        {
            Instance._transform.position = Vector3.Lerp(Instance._transform.position, Instance._targetTransform.position, Time.deltaTime * 2);
        }
    }

    // Used to look at local player before join game
    public class OverviewState : CameraState
    {
        public const float InitialDistance = 7;
        private const float FinalDistance = 4;
        private const float InterpolationSpeed = 3;

        public static readonly Vector3 ViewDirection = new Vector3(-0.5f, -0.1f, -1);
        public static readonly Vector3 Offset = new Vector3(0, 1.5f, 0);

        private Quaternion _finalRotation;
        private float _distance;

        public OverviewState()
        {
            if (GameState.LocalDecorator != null)
                Instance.SetTarget(GameState.LocalDecorator.transform);

            if (Instance._targetTransform)
            {
                _distance = InitialDistance;
                _finalRotation = Quaternion.LookRotation(Instance._targetTransform.TransformDirection(ViewDirection));

                Vector3 finalPosition = Instance._targetTransform.TransformPoint(Instance._targetTransform.InverseTransformDirection(_finalRotation * Vector3.back * FinalDistance));
                if (Vector3.Distance(Instance._transform.position, finalPosition) > 1)
                {
                    Quaternion rot = Quaternion.LookRotation(Instance._targetTransform.TransformDirection(new Vector3(-1, -1, 1)));
                    Instance._transform.rotation = rot;
                    Instance._transform.position = Instance._targetTransform.TransformPoint(Instance._targetTransform.InverseTransformDirection(rot * Vector3.back * InitialDistance));
                }
            }
        }

        public override void Update()
        {
            _distance = Mathf.Lerp(_distance, FinalDistance, Time.deltaTime * InterpolationSpeed);

            if (Instance._targetTransform)
            {
                _finalRotation = Quaternion.LookRotation(Instance._targetTransform.TransformDirection(ViewDirection));

                Quaternion rot = Quaternion.Slerp(Instance._transform.rotation, _finalRotation, Time.deltaTime * InterpolationSpeed);
                Vector3 pos = Instance._targetTransform.TransformPoint(Instance._targetTransform.InverseTransformDirection(rot * Vector3.back * _distance)) + Offset;

                Instance._transform.position = Vector3.Lerp(Instance._transform.position, pos, Time.deltaTime * InterpolationSpeed);
                Instance._transform.rotation = rot;

                //if (Mathf.Approximately(0, Quaternion.Angle(rot, _finalRotation)) && Mathf.Approximately(_distance, FinalDistance))
                //{
                //    _cameraMoving = false;
                //}
            }
        }

        public override string ToString()
        {
            return "Overview State";
        }
    }
}
using UnityEngine;

public class BaseGameProp : MonoBehaviour, IShootable
{
    private void FreezeObject(bool b)
    {
        if (b == _isFreezed) return;

        if (b)
        {
            _rbs = GetComponentsInChildren(typeof(Rigidbody));
            _oriMass = new float[_rbs.Length];
            _adrag = new float[_rbs.Length];
            _drag = new float[_rbs.Length];
        }

        for (int i = 0; i < _rbs.Length; i++)
        {
            Rigidbody r = (Rigidbody)_rbs[i];

            r.linearVelocity = Vector3.zero;
            r.angularVelocity = Vector3.zero;

            r.useGravity = !b;
            r.freezeRotation = b;

            if (b)
            {
                _oriMass[i] = r.mass;
                _adrag[i] = r.angularDamping;
                _drag[i] = r.linearDamping;

                r.angularDamping = 10000;
                r.linearDamping = 10000;
                r.mass = 0.1f;
            }
            else// if (!b)
            {
                r.mass = _oriMass[i];        //1
                r.angularDamping = _adrag[i];   //0.5f;
                r.linearDamping = _drag[i];           //0f;
            }
        }

        _isFreezed = b;
    }

    public virtual void ApplyDamage(DamageInfo shot)
    {
        ApplyForce(shot.Hitpoint, shot.Force * 5);
    }

    public virtual bool IsVulnerable
    {
        get { return true; }
    }

    public virtual bool IsLocal
    {
        get { return false; }
    }

    public virtual void ApplyForce(Vector3 position, Vector3 direction)
    {
        if (HasRigidbody)
        {
            Rigidbody.AddForceAtPosition(direction, position);
        }
    }

    public virtual bool CanApplyDamage
    {
        get { return true; }
    }

    #region PROPERTIES
    public Vector3 Scale
    {
        get { return Transform.localScale; }
    }
    public Vector3 Position
    {
        get { return Transform.position; }
    }
    public Quaternion Rotation
    {
        get { return Transform.rotation; }
    }
    public Vector3 Velocity
    {
        get
        {
            if (HasRigidbody) return _rigidbody.linearVelocity;
            else return Vector3.zero;
        }
    }
    public Vector3 AngularVelocity
    {
        get
        {
            if (HasRigidbody) return _rigidbody.angularVelocity;
            else return Vector3.zero;
        }
    }
    public Transform Transform
    {
        get
        {
            if (_transform == null) _transform = transform;
            return _transform;
        }
    }
    public bool HasRigidbody
    {
        get { return (Rigidbody != null); }
    }
    public Rigidbody Rigidbody
    {
        get
        {
            if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody>();
            return _rigidbody;
        }
    }
    public bool IsMoved
    {
        get { return _isMoved; }
        set { _isMoved = value; }
    }
    public bool IsSleeping
    {
        get { return _isSleeping; }
        set
        {
            _isSleeping = value;

            if (HasRigidbody)
            {
                if (_isSleeping)
                {
                    Rigidbody.Sleep();
                }
                else
                {
                    Rigidbody.WakeUp();
                }
            }
        }
    }
    public float Mass
    {
        get { return _originalMass; }
        set
        {
            _originalMass = value;
            if (HasRigidbody)
            {
                Rigidbody.mass = _originalMass;
            }
        }
    }
    public virtual bool IsFreezed
    {
        get { return _isFreezed; }
        set { FreezeObject(value); }
    }
    public virtual bool IsPassiv
    {
        get { return _isPassive; }
        set { _isPassive = value; }
    }
    public bool RecieveProjectileDamage
    {
        get { return _recieveProjectileDamage; }
    }
    #endregion

    #region FIELDS
    protected Transform _transform = null;
    private Rigidbody _rigidbody = null;
    private bool _isMoved = false;
    private bool _isSleeping = false;
    private bool _isFreezed = false;
    private bool _isPassive = false;
    private float _originalMass = 1;

    private Component[] _rbs;
    private float[] _oriMass;
    private float[] _adrag;
    private float[] _drag;
    #endregion

    #region INSPECTOR
    [SerializeField]
    protected bool _recieveProjectileDamage = true;
    #endregion
}

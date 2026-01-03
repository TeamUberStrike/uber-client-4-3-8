using System;
using UberStrike.Core.Types;
using UnityEngine;

public class TouchWeaponChanger : TouchButton
{
    private Texture[] weapons;
    private MeshGUIQuad _quad;
    private MeshGUIQuad _incomingQuad;
    private float _startWeaponSwitch = 0;
    private Rect _leftIconPos;
    private Rect _rightIconPos;
    private Vector2 _touchStartPos;
    private bool _touchUsed;

    private UberstrikeItemClass _currWeaponClass;
    private bool _moveLeft = true;

    public float WeaponSwitchTime = 0.3f;
    public float SwipeThreshold = 4;

    public event Action OnNextWeapon;
    public event Action OnPrevWeapon;

    private Vector2 _position;
    public Vector2 Position
    {
        get
        {
            return _position;
        }
        set
        {
            _position = value;
            if (_quad != null)
            {
                _quad.Position = _position;
            }
            Boundary = new Rect(_position.x - weapons[0].width / 2, _position.y - weapons[0].height / 2, weapons[0].width, weapons[0].height);
            _leftIconPos = new Rect(Boundary.x - MobileIcons.LeftIcon.width, Boundary.y + Boundary.height/2 - MobileIcons.LeftIcon.height/2, MobileIcons.LeftIcon.width, MobileIcons.LeftIcon.height);
            _rightIconPos = new Rect(Boundary.xMax, Boundary.y + (Boundary.height - MobileIcons.RightIcon.height) / 2, MobileIcons.RightIcon.width, MobileIcons.RightIcon.height);
        }
    }

    public override bool Enabled
    {
        get
        {
            return base.Enabled;
        }
        set
        {
            base.Enabled = value;
            if (!base.Enabled)
            {
                if (_quad != null)
                {
                    _quad.FreeObject();
                    _quad = null;
                }
                if (_incomingQuad != null)
                {
                    _incomingQuad.FreeObject();
                    _incomingQuad = null;
                }
            }
            else
            {
                Start();
            }
        }
    }

    public TouchWeaponChanger(Texture[] weaponIcons)
        : base()
    {
        weapons = weaponIcons;

        _position = Vector2.zero;

        OnTouchBegan += TouchWeaponChanger_OnTouchBegan;
        OnTouchMoved += TouchWeaponChanger_OnTouchMoved;
        OnTouchEnded += TouchWeaponChanger_OnTouchEnded;
    }

    void TouchWeaponChanger_OnTouchEnded(Vector2 obj)
    {
        if (!_touchUsed)
        {
            if (OnNextWeapon != null)
            {
                OnNextWeapon();
            }
            GenerateNewQuad(WeaponController.Instance.GetCurrentWeapon().Item.ItemClass, false);
        }
        _touchUsed = true;
    }

    void TouchWeaponChanger_OnTouchMoved(Vector2 pos, Vector2 delta)
    {
        if (_touchUsed) return;

        if (_touchStartPos.x - pos.x > SwipeThreshold)
        {
            _touchUsed = true;
            if (OnPrevWeapon != null)
            {
                OnPrevWeapon();
            }
            GenerateNewQuad(WeaponController.Instance.GetCurrentWeapon().Item.ItemClass, true);
        }
        else if (_touchStartPos.x - pos.x < -SwipeThreshold)
        {
            _touchUsed = true;
            if (OnNextWeapon != null)
            {
                OnNextWeapon();
            }
            GenerateNewQuad(WeaponController.Instance.GetCurrentWeapon().Item.ItemClass, false);
        }
    }

    void TouchWeaponChanger_OnTouchBegan(Vector2 obj)
    {
        _touchStartPos = obj;
        _touchUsed = false;
    }

    protected void Start()
    {
        if (_quad != null) _quad.FreeObject();
        if (_incomingQuad != null)
        {
            _incomingQuad.FreeObject();
            _incomingQuad.QuadMesh.GetComponent<Renderer>().material.mainTextureOffset = Vector2.zero;
            _incomingQuad = null;
        }
        _quad = new MeshGUIQuad(weapons[(int)WeaponController.Instance.GetCurrentWeapon().Item.ItemClass], TextAnchor.MiddleCenter);
        _quad.Position = Position;
        _quad.Scale = new Vector2(1, 1);

        _startWeaponSwitch = 0;
    }

    public override void Draw()
    {
        base.Draw();

        GUI.Label(_leftIconPos, MobileIcons.LeftIcon);
        GUI.Label(_rightIconPos, MobileIcons.RightIcon);
    }

    public void CheckWeaponChanged()
    {
        // did the weapon change external from this control?
        UberstrikeItemClass weaponClass = WeaponController.Instance.GetCurrentWeapon().Item.ItemClass;
        if (weaponClass != _currWeaponClass)
        {
            GenerateNewQuad(weaponClass);
        }
    }

    private void GenerateNewQuad(UberstrikeItemClass weaponClass, bool moveLeft = true)
    {
        _currWeaponClass = weaponClass;

        if (_incomingQuad != null)
        {
            _quad.FreeObject();
            _quad = _incomingQuad;
        }

        _incomingQuad = new MeshGUIQuad(weapons[(int)weaponClass], TextAnchor.MiddleCenter);
        _incomingQuad.Position = Position;
        _incomingQuad.QuadMesh.GetComponent<Renderer>().material.mainTextureOffset = new Vector2(1, 0);
        _incomingQuad.Scale = new Vector2(1, 1);
        _incomingQuad.Alpha = 0;

        _startWeaponSwitch = Time.time;
        _moveLeft = moveLeft;
    }

    public override void FinalUpdate()
    {
        base.FinalUpdate();

        float left = -0.5f;
        float mid = 0;
        float right = 0.5f;
        if (!_moveLeft)
        {
            left = 0.5f;
            right = -0.5f;
        }

        if (_startWeaponSwitch + WeaponSwitchTime > Time.time)
        {
            float t = (Time.time - _startWeaponSwitch) / WeaponSwitchTime;
            _incomingQuad.QuadMesh.GetComponent<Renderer>().material.mainTextureOffset = new Vector2(Mathf.Lerp(left, mid, t), 0);
            _incomingQuad.Alpha = Mathf.Lerp(-1, 1, t); // go negative so only second half of animation is visible
            _quad.QuadMesh.GetComponent<Renderer>().material.mainTextureOffset = new Vector2(Mathf.Lerp(mid, right, t), 0);
            _quad.Alpha = Mathf.Lerp(1, -1, t);
        }
        else if (_incomingQuad != null)
        {
            _incomingQuad.QuadMesh.GetComponent<Renderer>().material.mainTextureOffset = Vector2.zero;
            _incomingQuad.Alpha = 1.0f;

            _quad.FreeObject();
            _quad = _incomingQuad;
            _incomingQuad = null;
        }
    }

}


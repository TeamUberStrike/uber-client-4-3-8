using System;
using UnityEngine;

public class TouchConsumableChanger : TouchButton
{
    private bool _touchUsed;
    private Vector2 _touchStartPos;

    public float TimeBeforeTap = 0.2f;
    public float SwipeThreshold = 4;

    public event Action OnNextConsumable;
    public event Action OnPrevConsumable;
    public event Action OnStartUseConsumable;
    public event Action OnEndUseConsumable;

    public int ConsumablesHeld { get; private set; }

    public void UpdateConsumablesHeld()
    {
        ConsumablesHeld = 0;
        if (LoadoutManager.Instance.HasItemInSlot(LoadoutSlotType.QuickUseItem1)) ConsumablesHeld++;
        if (LoadoutManager.Instance.HasItemInSlot(LoadoutSlotType.QuickUseItem2)) ConsumablesHeld++;
        if (LoadoutManager.Instance.HasItemInSlot(LoadoutSlotType.QuickUseItem3)) ConsumablesHeld++;
    }

    public override Rect Boundary
    {
        get
        {
            return base.Boundary;
        }
        set
        {
            float middle = value.y + value.height / 2;
            _leftArrow = new Rect(value.x - MobileIcons.LeftIcon.width, middle - MobileIcons.LeftIcon.height / 2, MobileIcons.LeftIcon.width, MobileIcons.LeftIcon.height);
            _rightArrow = new Rect(value.xMax, middle - MobileIcons.LeftIcon.height / 2, MobileIcons.LeftIcon.width, MobileIcons.LeftIcon.height);

            // increase boundary to include arrows
            value.x -= _leftArrow.width;
            value.width += _leftArrow.width + _rightArrow.width;

            base.Boundary = value;
        }
    }

    private Rect _leftArrow;
    private Rect _rightArrow;
    private GUIContent _leftArrowContent;
    private GUIContent _rightArrowContent;
    private bool _didStartTap = false;

    public TouchConsumableChanger()
        : base()
    {
        OnTouchBegan += TouchConsumableChanger_OnTouchBegan;
        OnTouchMoved += TouchConsumableChanger_OnTouchMoved;
        OnTouchEnded += TouchConsumableChanger_OnTouchEnded;

        _leftArrowContent = new GUIContent(MobileIcons.LeftIcon);
        _rightArrowContent = new GUIContent(MobileIcons.RightIcon);

        ConsumablesHeld = 0;
    }

    void TouchConsumableChanger_OnTouchEnded(Vector2 obj)
    {
        if (ConsumablesHeld == 0) return;
        if (!_touchUsed)
        {
            if (OnStartUseConsumable != null)
            {
                OnStartUseConsumable();
            }
            _touchUsed = true;
            _didStartTap = true;
        }
        if (_didStartTap)
        {
            if (OnEndUseConsumable != null)
            {
                OnEndUseConsumable();
            }
        }
    }

    void TouchConsumableChanger_OnTouchMoved(Vector2 pos, Vector2 delta)
    {
        if (_touchUsed || ConsumablesHeld == 0) return;

        if (finger.StartTouchTime + TimeBeforeTap < Time.time)
        {
            if (OnStartUseConsumable != null)
            {
                OnStartUseConsumable();
            }
            _touchUsed = true;
            _didStartTap = true;
            return;
        }

        // only need to deal with swipes if we have more than one consumable
        if (ConsumablesHeld <= 1) return;

        if (_touchStartPos.x - pos.x > SwipeThreshold)
        {
            _touchUsed = true;
            if (OnPrevConsumable != null)
            {
                OnPrevConsumable();
            }
        }
        else if (_touchStartPos.x - pos.x < -SwipeThreshold)
        {
            _touchUsed = true;
            if (OnNextConsumable != null)
            {
                OnNextConsumable();
            }
        }
    }

    void TouchConsumableChanger_OnTouchBegan(Vector2 obj)
    {
        if (ConsumablesHeld == 0) return;
        _touchStartPos = obj;
        _touchUsed = false;
        _didStartTap = false;
    }

    public override void Draw()
    {
        base.Draw();

        if (ConsumablesHeld > 1)
        {
            GUI.Label(_leftArrow, _leftArrowContent);
            GUI.Label(_rightArrow, _rightArrowContent);
        }
    }
}


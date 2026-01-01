using UberStrike.Realtime.Common;
using UnityEngine;

public static class UserInput
{
    public static float ZoomSpeed = 1;

    static UserInput()
    {
        Reset();
    }

    public static void Reset()
    {
        Mouse = new Vector2(0, 0);
        VerticalDirection = Vector3.zero;
        HorizontalDirection = Vector3.zero;
        Rotation = Quaternion.identity;
    }

    public static void UpdateDirections()
    {
        ResetDirection();

#if !UNITY_ANDROID && !UNITY_IPHONE
        if ((GameState.LocalCharacter.Keys & KeyState.Left) != 0) HorizontalDirection.x -= 127;
        if ((GameState.LocalCharacter.Keys & KeyState.Right) != 0) HorizontalDirection.x += 127;
        if ((GameState.LocalCharacter.Keys & KeyState.Forward) != 0) HorizontalDirection.z += 127;
        if ((GameState.LocalCharacter.Keys & KeyState.Backward) != 0) HorizontalDirection.z -= 127;

        if ((GameState.LocalCharacter.Keys & KeyState.Jump) != 0) VerticalDirection.y += 127;
        if ((GameState.LocalCharacter.Keys & KeyState.Crouch) != 0) VerticalDirection.y -= 127;

        //important to keep the velocity constant for every direction
        //e.g. (1,0,1) shouldn't result in faster movement than (1,0,0)
        HorizontalDirection.Normalize();
        VerticalDirection.Normalize();
#else
        GameState.LocalCharacter.Keys = 0;
        if (TouchInput.WishJump) GameState.LocalCharacter.Keys |= KeyState.Jump;
        if (TouchInput.WishCrouch) GameState.LocalCharacter.Keys |= KeyState.Crouch;
        if (TouchInput.WishDirection.x > 0.1f) GameState.LocalCharacter.Keys |= KeyState.Right;
        if (TouchInput.WishDirection.x < -0.1f) GameState.LocalCharacter.Keys |= KeyState.Left;
        if (TouchInput.WishDirection.y > 0.1f) GameState.LocalCharacter.Keys |= KeyState.Forward;
        if (TouchInput.WishDirection.y < -0.1f) GameState.LocalCharacter.Keys |= KeyState.Backward;

        HorizontalDirection = new Vector3(TouchInput.WishDirection.x, 0, TouchInput.WishDirection.y);
        VerticalDirection = new Vector3(0, TouchInput.WishCrouch ? -1 : TouchInput.WishJump ? 1 : 0, 0);
#endif
    }

    public static void ResetDirection()
    {
        HorizontalDirection = Vector3.zero;
        VerticalDirection = Vector3.zero;
    }

    public static KeyState GetkeyState(GameInputKey slot)
    {
        switch (slot)
        {
            case GameInputKey.Forward: return KeyState.Forward;
            case GameInputKey.Backward: return KeyState.Backward;
            case GameInputKey.Right: return KeyState.Right;
            case GameInputKey.Left: return KeyState.Left;
            case GameInputKey.Crouch: return KeyState.Crouch;
            case GameInputKey.Jump: return KeyState.Jump;
            default: return KeyState.Still;
        }
    }

    public static void SetRotation(float hAngle = 0, float vAngle = 0)
    {
        UserInput.Mouse = new Vector2(hAngle, -vAngle);

        UpdateMouse();
        UpdateDirections();
    }

    public static void UpdateMouse()
    {
#if !UNITY_ANDROID && !UNITY_IPHONE
        if (Camera.main != null)
        {
            float factor = Mathf.Pow(Camera.main.fieldOfView / ApplicationDataManager.ApplicationOptions.CameraFovMax, 1.1f);

            //TURN AROUND
            Mouse.x += InputManager.Instance.RawValue(GameInputKey.HorizontalLook) * ApplicationDataManager.ApplicationOptions.InputXMouseSensitivity * factor;
            Mouse.x = ClampAngle(Mouse.x, -360f, 360f);

            //UP AND DOWN
            int inv = (ApplicationDataManager.ApplicationOptions.InputInvertMouse) ? -1 : 1;

            Mouse.y += InputManager.Instance.RawValue(GameInputKey.VerticalLook) * ApplicationDataManager.ApplicationOptions.InputXMouseSensitivity * inv * factor;
            Mouse.y = ClampAngle(Mouse.y, -88, 88);
        }

        Rotation = Quaternion.AngleAxis(Mouse.x, Vector3.up) * Quaternion.AngleAxis(Mouse.y, Vector3.left);
#else
        if (Camera.main != null)
        {
            float factor = Mathf.Pow(Camera.main.fieldOfView / ApplicationDataManager.ApplicationOptions.CameraFovMax, 1.1f);

            //TURN AROUND
            Mouse.x += TouchInput.WishLook.x * GameState.Instance.TouchLookSensitivity.x 
                * ApplicationDataManager.ApplicationOptions.TouchLookSensitivity * factor;
            Mouse.x = ClampAngle(Mouse.x, -360f, 360f);

            //UP AND DOWN
            int inv = (ApplicationDataManager.ApplicationOptions.InputInvertMouse) ? -1 : 1;

            Mouse.y += TouchInput.WishLook.y * GameState.Instance.TouchLookSensitivity.y 
                * ApplicationDataManager.ApplicationOptions.TouchLookSensitivity * inv * factor;
            Mouse.y = ClampAngle(Mouse.y, -88, 88);
        }

        Rotation = Quaternion.AngleAxis(Mouse.x, Vector3.up) * Quaternion.AngleAxis(Mouse.y, Vector3.left);
#endif
    }

    public static bool IsPressed(KeyState k)
    {
        return (GameState.LocalCharacter.Keys & k) != 0;
    }

    public static bool IsWalking
    {
        get
        {
            return ((GameState.LocalCharacter.Keys & KeyState.Walking) != 0 &&
                (GameState.LocalCharacter.Keys ^ KeyState.Horizontal) != 0 &&
                (GameState.LocalCharacter.Keys ^ KeyState.Vertical) != 0);
        }
    }

    public static bool IsMouseLooking
    {
        get
        {
            return InputManager.Instance.RawValue(GameInputKey.HorizontalLook) != 0 ||
                InputManager.Instance.RawValue(GameInputKey.VerticalLook) != 0;
        }
    }

    public static bool IsMovingVertically
    {
        get { return (GameState.LocalCharacter.Keys & (KeyState.Crouch | KeyState.Jump)) != 0; }
    }

    public static bool IsMovingUp
    {
        get { return (GameState.LocalCharacter.Keys & KeyState.Jump) != 0; }
    }

    public static bool IsMovingDown
    {
        get { return (GameState.LocalCharacter.Keys & KeyState.Crouch) != 0; }
    }

    public static Quaternion Rotation { get; private set; }

    public static Vector2 Mouse;
    public static Vector3 VerticalDirection;
    public static Vector3 HorizontalDirection;

    #region Helpers

    private static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360)
            angle += 360;
        if (angle > 360)
            angle -= 360;
        //angle = (angle + 360) % 360;
        return Mathf.Clamp(angle, min, max);
    }

    #endregion
}
using UnityEngine;

public class InGameChatPageGUI : ChatPageGUI
{
    public override string Title { get { return "Chat"; } }
    protected override bool IsInGamePanel { get { return true; } }
    public static bool IsOpen { get { return _active && _active.isActiveAndEnabled && GameState.HasCurrentGame; } }

    private void OnEnable()
    {
        _active = this;
        if (GameState.HasCurrentGame)
            GameState.LocalPlayer.Pause();
        InGameChatHud.Instance.CloseForPanel();
        InputManager.Instance.IsInputEnabled = false;
        Screen.lockCursor = false;
        _focusInput = true;
    }

    private void OnDisable()
    {
        if (_active == this) _active = null;
        GUIUtility.keyboardControl = 0;
    }

    public override void DrawGUI(Rect rect)
    {
        DrawInGamePanel(rect);
        if (_focusInput && Event.current.type == EventType.Repaint)
        {
            GUI.FocusControl("@CurrentChatMessage");
            _focusInput = false;
        }
    }

    private static InGameChatPageGUI _active;
    private bool _focusInput;
}

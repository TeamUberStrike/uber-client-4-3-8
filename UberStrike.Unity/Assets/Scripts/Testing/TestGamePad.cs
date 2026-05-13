using UnityEngine;
using Cmune.Util;

class TestGamePad : MonoBehaviour
{
    void Start()
    {
        InputManager.Instance.IsGamepadEnabled = true;

        foreach (var v in Input.GetJoystickNames())
        {
            Debug.Log("Joystick " + v);
        }
    }

    private UserInputMap _targetMap;

    void OnGUI()
    {
        int i = 0;
        foreach (UserInputMap m in InputManager.Instance.KeyMapping.Values)
        {
            bool isEditing = m == _targetMap;
            GUI.Label(new Rect(20, 35 + (i * 20), 140, 20), m.Description);

            if (m.IsConfigurable)
            {
                if (GUI.Toggle(new Rect(180, 35 + (i * 20), 20, 20), isEditing, string.Empty))
                {
                    _targetMap = m;
                    Screen.lockCursor = true;
                }
            }

            if (isEditing)
            {
                GUI.enabled = true;
                GUI.TextField(new Rect(220, 35 + (i * 20), 100, 20), string.Empty);
                GUI.enabled = false;
            }
            else
            {
                GUI.contentColor = (m.Channel != null) ? Color.white : Color.red;
                GUI.Label(new Rect(220, 35 + (i * 20), 150, 20), m.Assignment);
                GUI.contentColor = Color.white;
            }
            i++;
        }

        if (_targetMap != null)
        {
            if (Event.current.type == EventType.Layout && InputManager.Instance.ListenForNewKeyAssignment(_targetMap))
            {
                _targetMap = null;
                Screen.lockCursor = false;

                //avoid that the GUI event trigers any other kind of action
                Event.current.Use();
            }
        }

        if (_targetMap != null)
        {
            if (Event.current.type == EventType.Layout && InputManager.Instance.ListenForNewKeyAssignment(_targetMap))
            {
                _targetMap = null;
                Screen.lockCursor = false;

                //avoid that the GUI event trigers any other kind of action
                Event.current.Use();
            }
        }
    }
}

using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class UserInputMap
{
    public UserInputMap(string description, GameInputKey s, IInputChannel channel = null, bool isConfigurable = true, bool isEventSender = true, KeyCode prefix = KeyCode.None)
    {
        _prefix = prefix;

        IsConfigurable = isConfigurable;
        IsEventSender = isEventSender;
        Channel = channel;
        Slot = s;
        Description = description;
    }

    public override string ToString()
    {
        StringBuilder b = new StringBuilder(Description);

        b.AppendFormat(": {0}", Channel);

        return b.ToString();
    }

    #region String Serizalization

    public int SetChannel(byte[] bytes, int idx)
    {
        byte channel = bytes[idx++];
        if (channel >= 0 && channel <= 3)
        {
            if (channel == 0)
                Channel = new KeyInputChannel(bytes, ref idx);
            else if (channel == 1)
                Channel = new MouseInputChannel(bytes, ref idx);
            else if (channel == 2)
                Channel = new AxisInputChannel(bytes, ref idx);
            else if (channel == 3)
                Channel = new ButtonInputChannel(bytes, ref idx);
        }
        else
        {
            Debug.LogError("KeyMap deserialization failed");
        }

        return idx;
    }

    public byte[] GetChannel()
    {
        List<byte> bytes = new List<byte>();

        if (Channel is KeyInputChannel)
            bytes.Add(0);
        else if (Channel is MouseInputChannel)
            bytes.Add(1);
        else if (Channel is AxisInputChannel)
            bytes.Add(2);
        else if (Channel is ButtonInputChannel)
            bytes.Add(3);
        //else if (Channel is MouseMovementChannel)
        //    bytes.Add(4);
        else
            bytes.Add(255);

        if (Channel != null)
            bytes.AddRange(Channel.GetBytes());

        return bytes.ToArray();
    }

    public string GetPrefString()
    {
        //return TextUtilities.Base64Encode(Instance._encoder.GetString(GetChannel()));
        return WWW.EscapeURL(Encoding.ASCII.GetString(GetChannel()), Encoding.ASCII);
    }

    public void FromPrefString(string pref)
    {
        //SetChannel(Instance._encoder.GetBytes(TextUtilities.Base64Decode(pref)), 0);
        SetChannel(Encoding.ASCII.GetBytes(WWW.UnEscapeURL(pref, Encoding.ASCII)), 0);//
    }

    #endregion

    #region Properties

    public GameInputKey Slot { get; private set; }

    public string Description { get; private set; }

    public string Assignment
    {
        get
        {
            if (Channel == null)
            {
                return "None";
            }
            else
            {
                return _prefix != KeyCode.None ? string.Format("{0} + {1}", PrintKeyCode(_prefix), Channel.Name) : Channel.Name;
            }
        }
    }

    private string PrintKeyCode(KeyCode keyCode)
    {
        switch (keyCode)
        {
            case KeyCode.LeftAlt:
                return "Left Alt";
            case KeyCode.LeftControl:
                return "Left Ctrl";
            case KeyCode.LeftShift:
                return "Left Shift";
            case KeyCode.RightAlt:
                return "Right Alt";
            case KeyCode.RightControl:
                return "Right Ctrl";
            case KeyCode.RightShift:
                return "Right Shift";
            default:
                return keyCode.ToString();
        }
    }

    public IInputChannel Channel { get; set; }

    public bool IsConfigurable { get; set; }

    public float Value
    {
        get
        {
            if (Channel != null)
            {
                bool prefix = _prefix != KeyCode.None ? Input.GetKey(_prefix) : true;
                return prefix ? Channel.Value : 0;
            }
            else
                return 0;
        }
    }

    public float RawValue()
    {
        if (Channel != null)
        {
            if (_prefix == KeyCode.None || Input.GetKey(_prefix))
                return Channel.RawValue();
        }

        return 0;
    }

    public bool IsEventSender { get; private set; }

    #endregion

    #region Fields
    private KeyCode _prefix = KeyCode.None;
    #endregion
}
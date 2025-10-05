using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UberStrike.Realtime.Common;

public class HudAssets : MonoSingleton<HudAssets>
{
    public BitmapFont InterparkBitmapFont
    {
        get { return _interparkBitmapFont; }
    }

    public BitmapFont HelveticaBitmapFont
    {
        get { return _helveticaBitmapFont; }
    }

    [SerializeField]
    private BitmapFont _interparkBitmapFont;
    [SerializeField]
    private BitmapFont _helveticaBitmapFont;
}

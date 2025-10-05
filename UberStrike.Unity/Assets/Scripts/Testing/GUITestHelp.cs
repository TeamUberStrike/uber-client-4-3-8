using UnityEngine;
using System.Collections;

public class GUITestHelp : MonoSingleton<GUITestHelp> 
{
    public Rect debugRect;
    public Color debugColor;

    public void Draw()
    {
        GUI.Button(debugRect, "");
    }
}
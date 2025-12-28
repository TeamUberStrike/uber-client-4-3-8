using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class TestAnyGuiStyle : MonoBehaviour
{
    public GUISkin skin;
    public string style = "label";
    public Vector2 size;
    public string text = "";

    // Update is called once per frame
    void OnGUI()
    {
        GUI.skin = skin;
        GUI.Button(new Rect((Screen.width - size.x) / 2, (Screen.height - size.y) / 2, size.x, size.y), text, style);
    }
}

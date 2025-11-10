using UnityEngine;


public class TestStateTExure : MonoBehaviour
{
    public Texture[] textures;

    private StateTexture2D texture;
    int index = 0;

    void Awake()
    {
        texture = new StateTexture2D(textures);
    }

    void OnGUI()
    {
        if (GUI.Button(new Rect(100, 100, 100, 20), "Change"))
        {
            texture.ChangeState(++index % textures.Length);
        }
        GUI.DrawTexture(new Rect(100, 150, 100, 100), texture.Current);
    }
}
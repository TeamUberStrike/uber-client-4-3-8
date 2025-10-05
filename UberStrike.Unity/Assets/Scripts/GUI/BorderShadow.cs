using Cmune.DataCenter.Common.Entities;
using UnityEngine;

public class BorderShadow : MonoBehaviour
{
    [SerializeField]
    private Texture2D _pageShadowLeft;
    [SerializeField]
    private Texture2D _pageShadowRight;

    void OnGUI()
    {
        // Draw the shadows on the screen edge
        if (!Screen.fullScreen && 
            (ApplicationDataManager.Channel == ChannelType.WebPortal || 
            ApplicationDataManager.Channel == ChannelType.WebFacebook ||
            ApplicationDataManager.Channel == ChannelType.Kongregate))
        {
            GUI.DrawTexture(new Rect(0, 0, 4, Screen.height), _pageShadowLeft);
            GUI.DrawTexture(new Rect(Screen.width - 4, 0, 4, Screen.height), _pageShadowRight);
        }
    }
}
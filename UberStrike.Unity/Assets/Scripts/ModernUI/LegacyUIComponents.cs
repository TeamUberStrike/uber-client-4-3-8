using UnityEngine;

/// <summary>
/// Legacy Text Data - Stores text information for OnGUI rendering
/// </summary>
public class LegacyTextData : MonoBehaviour
{
    public string text = "";
    public int fontSize = 18;
    public Color color = Color.white;
    public TextAnchor alignment = TextAnchor.UpperLeft;
    public Font font;
    
    void Start()
    {
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}

/// <summary>
/// Legacy Button Data - Stores button information for OnGUI rendering
/// </summary>
public class LegacyButtonData : MonoBehaviour
{
    public string text = "";
    public Color backgroundColor = Color.gray;
    public Color textColor = Color.white;
    public Font font;
    public System.Action onClick;
    
    void Start()
    {
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}

/// <summary>
/// Legacy Image Data - Stores image information for OnGUI rendering  
/// </summary>
public class LegacyImageData : MonoBehaviour
{
    public Texture2D texture;
    public Color color = Color.white;
    public ScaleMode scaleMode = ScaleMode.StretchToFill;
}
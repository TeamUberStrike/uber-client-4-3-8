using UnityEngine;
using System;

public static class Texture2DExtension
{
    public static Texture2D ChangeHSV(this Texture2D texture, float hue, float saturation = 0, float value = 0)
    {
        if (IsSupported(texture.format))
        {
            try
            {
                var t = new Texture2D(texture.width, texture.height, texture.format, false);
                var colors = texture.GetPixels();
                for (int i = 0; i < colors.Length; i++)
                {
                    colors[i] = HsvColor.FromColor(colors[i]).Add(hue, saturation, value).ToColor();
                }
                t.SetPixels(colors);
                t.Apply();
                return t;
            }
            catch (Exception e)
            {
                Debug.LogError("ChangeHue failed on '" + texture.name + "' with Exception: " + e.Message);
                return texture;
            }
        }
        else
        {
            Debug.LogError("ChangeHue failed on '" + texture.name + "' because texture format not supported: " + texture.format);
            return texture;
        }
    }

    private static bool IsSupported(TextureFormat format)
    {
        switch (format)
        {
            case TextureFormat.ARGB32:
            case TextureFormat.RGBA32:
            case TextureFormat.RGB24:
            case TextureFormat.Alpha8:
                return true;
            default: return false;
        }
    }
}
using UnityEngine;
using System.Collections;

/// <summary>
/// Class for converting colors across various formats like RGB, Hex and Hue.
/// </summary>
public static class ColorConverter
{
    public static float GetHue(Color c)
    {
        float hue = 0;

        if (c.r == 0)
        {
            hue = 2;
            hue += (c.b < 1) ? c.b : 2 - c.g;
        }
        else
        {
            if (c.g == 0)
            {
                hue = 4;
                hue += (c.r < 1) ? c.r : 2 - c.b;
            }
            else
            {
                hue = 0;
                hue += (c.g < 1) ? c.g : 2 - c.r;
            }
        }

        return hue;
    }

    public static Color GetColor(float hue)
    {
        hue = hue % 6;
        Color c = Color.white;


        if (hue < 1)
        {
            c = new Color(1, hue, 0); //100 -- 110
        }
        else
        {
            if (hue < 2)
            {
                c = new Color(2 - hue, 1, 0); //110 -- 010
            }
            else
            {
                if (hue < 3)
                {
                    c = new Color(0, 1, hue - 2); //010 -- 011
                }
                else
                {
                    if (hue < 4)
                    {
                        c = new Color(0, 4 - hue, 1); //011 -- 001
                    }
                    else
                    {
                        if (hue < 5)
                        {
                            c = new Color(hue - 4, 0, 1); //001 -- 101
                        }
                        else
                        {
                            c = new Color(1, 0, 6 - hue); //101 -- 100
                        }
                    }
                }
            }
        }
        return c;
    }

    public static Color HexToColor(string hexString)
    {
        int r;
        int g;
        int b;
        try
        {
            r = int.Parse(hexString.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        }
        catch
        {
            r = 255;
        }
        try
        {
            g = int.Parse(hexString.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        }
        catch
        {
            g = 255;
        }
        try
        {
            b = int.Parse(hexString.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        }
        catch
        {
            b = 255;
        }

        return new Color(((float)r / 255.0f), ((float)g / 255.0f), ((float)b / 255.0f));
    }

    public static string ColorToHex(Color color)
    {
        string r = ((int)(color.r * 255)).ToString("X2");
        string g = ((int)(color.g * 255)).ToString("X2");
        string b = ((int)(color.b * 255)).ToString("X2");
        return r + g + b;
    }

    public static Color RgbToColor(float r, float g, float b)
    {
        return new Color(r / 255, g / 255, b / 255);
    }

    public static Color RgbaToColor(float r, float g, float b, float a)
    {
        return new Color(r / 255, g / 255, b / 255, a / 255);
    }

    public static HsvColor RgbToHsv(Color color)
    {
        HsvColor ret = new HsvColor(0f, 0f, 0f, color.a);

        float r = color.r;
        float g = color.g;
        float b = color.b;

        float max = Mathf.Max(r, Mathf.Max(g, b));

        if (max <= 0)
        {
            return ret;
        }

        float min = Mathf.Min(r, Mathf.Min(g, b));
        float dif = max - min;

        if (max > min)
        {
            if (g == max)
            {
                ret.h = (b - r) / dif * 60f + 120f;
            }
            else if (b == max)
            {
                ret.h = (r - g) / dif * 60f + 240f;
            }
            else if (b > g)
            {
                ret.h = (g - b) / dif * 60f + 360f;
            }
            else
            {
                ret.h = (g - b) / dif * 60f;
            }
            if (ret.h < 0)
            {
                ret.h = ret.h + 360f;
            }
        }
        else
        {
            ret.h = 0;
        }

        ret.h *= 1f / 360f;
        ret.s = (dif / max) * 1f;
        ret.v = max;

        return ret;
    }

    public static Color HsvToRgb(HsvColor color)
    {
        return HsvToRgb(color.h, color.s, color.v, color.a);
    }

    public static Color HsvToRgb(float hue, float saturation, float value)
    {
        return HsvToRgb(hue, saturation, value, 1);
    }

    public static Color HsvToRgb(float hue, float saturation, float value, float alpha)
    {
        float r = value;
        float g = value;
        float b = value;
        if (saturation != 0)
        {
            float max = value;
            float dif = value * saturation;
            float min = value - dif;

            float h = hue * 360f;

            if (h < 60f)
            {
                r = max;
                g = h * dif / 60f + min;
                b = min;
            }
            else if (h < 120f)
            {
                r = -(h - 120f) * dif / 60f + min;
                g = max;
                b = min;
            }
            else if (h < 180f)
            {
                r = min;
                g = max;
                b = (h - 120f) * dif / 60f + min;
            }
            else if (h < 240f)
            {
                r = min;
                g = -(h - 240f) * dif / 60f + min;
                b = max;
            }
            else if (h < 300f)
            {
                r = (h - 240f) * dif / 60f + min;
                g = min;
                b = max;
            }
            else if (h <= 360f)
            {
                r = max;
                g = min;
                b = -(h - 360f) * dif / 60 + min;
            }
            else
            {
                r = 0;
                g = 0;
                b = 0;
            }
        }

        return new Color(Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b), alpha);
    }
}
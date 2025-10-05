
using UnityEngine;

public struct HsvColor
{
    public float h;
    public float s;
    public float v;
    public float a;

    public HsvColor(float h, float s, float b, float a)
    {
        this.h = h;
        this.s = s;
        this.v = b;
        this.a = a;
    }

    public HsvColor(float h, float s, float b)
    {
        this.h = h;
        this.s = s;
        this.v = b;
        this.a = 1f;
    }

    public HsvColor(Color col)
    {
        HsvColor temp = ColorConverter.RgbToHsv(col);
        h = temp.h;
        s = temp.s;
        v = temp.v;
        a = temp.a;
    }

    public static HsvColor FromColor(Color color)
    {
        return ColorConverter.RgbToHsv(color);
    }

    public Color ToColor()
    {
        return ColorConverter.HsvToRgb(this);
    }

    public HsvColor Add(float hue, float saturation, float value)
    {
        h += hue;
        s += saturation;
        v += value;
        while (h > 1) h -= 1;
        while (h < 0) h += 1;
        return this;
    }

    public HsvColor AddHue(float hue)
    {
        h += hue;
        while (h > 1) h -= 1;
        while (h < 0) h += 1;
        return this;
    }

    public HsvColor AddSaturation(float saturation)
    {
        s += saturation;
        return this;
    }

    public HsvColor AddValue(float value)
    {
        v += value;
        return this;
    }
}